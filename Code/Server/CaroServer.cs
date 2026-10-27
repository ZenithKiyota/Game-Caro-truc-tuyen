// Phụ trách: Nguyễn Đức Huy — lõi TCP, framing NDJSON, điều phối message, log, clock.
//
// CaroServer là một partial class, mỗi người sở hữu một file (xem PHAN_CONG.md).
// Trạng thái dùng chung khai báo ở đây. Cần thêm trường/hàm dùng chung thì mở PR nhỏ riêng.
//
// Mô hình luồng: mỗi client có một task đọc riêng, nhưng MỌI thao tác trên trạng thái
// (_sessions, _rooms, _invites) phải nằm trong lock (_gate) — process() và clockTick() lo việc này,
// nên các handler của từng người không cần tự lock.
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CaroOnline.Shared;
using Microsoft.Data.Sqlite;

namespace CaroOnline.Server;

public sealed partial class CaroServer : IDisposable
{
    // ---------- Trạng thái dùng chung ----------
    private sealed class ClientConnection
    {
        public required TcpClient Client { get; init; }
        public required NetworkStream Stream { get; init; }
        public object SendGate { get; } = new();
        public string Remote => Client.Client.RemoteEndPoint?.ToString() ?? "?";
        public bool Connected => Client.Connected;
    }

    private sealed class Session
    {
        public required string Name { get; init; }
        public required string Token { get; init; }
        public ClientConnection? Connection { get; set; }
        public long OfflineSince { get; set; }
        public int RoomId { get; set; } = -1;
        public bool Spectator { get; set; }
    }

    private sealed class Room
    {
        public int Id { get; init; }
        public required string X { get; init; }
        public required string O { get; init; }
        public int[] Board { get; } = new int[Protocol.BoardSize * Protocol.BoardSize]; // 0 trống, 1 = X, 2 = O
        public List<string> Spectators { get; } = new();
        public int Turn { get; set; } = 1;
        public int LastRow { get; set; } = -1;
        public int LastCol { get; set; } = -1;
        public long StartedAt { get; set; }
        public long Deadline { get; set; }
        public long EndedAt { get; set; }
        public long PausedRemaining { get; set; }
        public bool Paused { get; set; }
        public bool Active { get; set; } = true;
        public int LastBroadcastSecond { get; set; } = -1;
        public string Winner { get; set; } = "";
        public string Reason { get; set; } = "";
        public JsonArray Moves { get; } = new();
    }

    private sealed record Invite(string From, string To, long ExpiresAt);

    private readonly object _gate = new();
    private readonly Dictionary<string, Session> _sessions = new();
    private readonly Dictionary<int, Room> _rooms = new();
    private readonly List<Invite> _invites = new();
    private TcpListener? _listener;
    private Timer? _clock;
    private SqliteConnection? _database;
    private readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CaroServer", "server.log");
    private int _nextRoomId = 1;

    // ---------- Huy: lõi mạng, điều phối, log ----------
    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string Timestamp(long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).ToString("O");

    private void Log(string message)
    {
        string line = $"{DateTimeOffset.Now:O} | {message}";
        try { Console.WriteLine(line); } catch (IOException) { }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            lock (_logPath) File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            try { Console.Error.WriteLine($"{DateTimeOffset.Now:O} | LOG_ERROR | {ex.Message}"); } catch (IOException) { }
        }
    }

    public bool Start(IPAddress address, int port)
    {
        if (port is < 1 or > 65535) { Log($"START_ERROR invalid port={port}"); return false; }
        if (!OpenDatabase()) return false;
        try
        {
            _listener = new TcpListener(address, port);
            _listener.Start();
            _clock = new Timer(_ => ClockTick(), null, 250, 250);
            Log($"SERVER_START address={address} port={port}");
            return true;
        }
        catch (Exception ex) when (ex is SocketException or InvalidOperationException or UnauthorizedAccessException)
        {
            Log($"START_ERROR {ex.GetType().Name}: {ex.Message}");
            _listener?.Stop(); _listener = null;
            return false;
        }
    }

    public async Task RunAsync(CancellationToken cancel)
    {
        TcpListener listener = _listener ?? throw new InvalidOperationException("Server chưa được khởi động.");
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(cancel).ConfigureAwait(false);
                client.NoDelay = true;
                var connection = new ClientConnection { Client = client, Stream = client.GetStream() };
                Log($"CLIENT_CONNECTED remote={connection.Remote}");
                _ = HandleClientAsync(connection, cancel);
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (cancel.IsCancellationRequested) { }
        catch (SocketException) when (cancel.IsCancellationRequested) { }
    }

    private async Task HandleClientAsync(ClientConnection connection, CancellationToken cancel)
    {
        byte[] buffer = new byte[8192];
        using var frame = new MemoryStream();
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                int count = await connection.Stream.ReadAsync(buffer, cancel).ConfigureAwait(false);
                if (count == 0) break;
                for (int i = 0; i < count; i++)
                {
                    byte b = buffer[i];
                    if (b == (byte)'\n')
                    {
                        byte[] bytes = frame.ToArray(); frame.SetLength(0);
                        int length = bytes.Length > 0 && bytes[^1] == (byte)'\r' ? bytes.Length - 1 : bytes.Length;
                        if (length == 0) { Error(connection, "Gói tin rỗng."); continue; }
                        try
                        {
                            using JsonDocument doc = JsonDocument.Parse(bytes.AsMemory(0, length));
                            if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException();
                            JsonObject? msg = JsonNode.Parse(bytes.AsSpan(0, length)) as JsonObject;
                            if (msg is null) throw new JsonException();
                            lock (_gate) Process(connection, msg);
                        }
                        catch (JsonException) { Error(connection, "JSON không hợp lệ."); }
                        continue;
                    }
                    if (frame.Length >= Protocol.MaxFrameBytes)
                    {
                        Error(connection, "Gói tin vượt quá giới hạn 64 KiB.");
                        return;
                    }
                    frame.WriteByte(b);
                }
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        catch (IOException ex) { Log($"CLIENT_IO_ERROR remote={connection.Remote} error={ex.Message}"); }
        catch (SocketException ex) { Log($"CLIENT_SOCKET_ERROR remote={connection.Remote} error={ex.Message}"); }
        finally
        {
            lock (_gate) Disconnected(connection);
            connection.Client.Dispose();
        }
    }

    private string? UserOf(ClientConnection connection)
    {
        foreach (var pair in _sessions)
            if (ReferenceEquals(pair.Value.Connection, connection)) return pair.Key;
        return null;
    }

    private void Process(ClientConnection connection, JsonObject msg)
    {
        if (msg["type"] is not JsonValue typeValue || !typeValue.TryGetValue(out string? type) || string.IsNullOrWhiteSpace(type))
        { Error(connection, "Thiếu trường type hợp lệ."); return; }
        if (type == Msg.Hello) { Hello(connection, msg); return; }
        string? me = UserOf(connection);
        if (me is null) { Error(connection, "Cần đăng nhập trước."); return; }
        switch (type)
        {
            case Msg.ListUsers: BroadcastUsers(); break;
            case Msg.Invite: HandleInvite(connection, me, msg); break;
            case Msg.InviteReply: HandleInviteReply(connection, me, msg); break;
            case Msg.Move: HandleMove(connection, me, msg); break;
            case Msg.ListRooms: BroadcastRooms(connection); break;
            case Msg.Spectate: HandleSpectate(connection, me, msg); break;
            case Msg.LeaveSpectate: HandleLeaveSpectate(connection, me); break;
            case Msg.History: SendHistory(me); break;
            default: Error(connection, "Loại yêu cầu không được hỗ trợ."); break;
        }
    }

    private void Send(ClientConnection? connection, JsonObject msg)
    {
        if (connection is null || !connection.Client.Connected) return;
        try
        {
            byte[] payload = Encoding.UTF8.GetBytes(msg.ToJsonString() + "\n");
            lock (connection.SendGate) connection.Stream.Write(payload, 0, payload.Length);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            Log($"SEND_ERROR remote={connection.Remote} error={ex.Message}");
            try { connection.Client.Dispose(); } catch (SocketException) { }
        }
    }

    private void SendTo(string user, JsonObject msg)
    {
        if (_sessions.TryGetValue(user, out Session? session)) Send(session.Connection, msg);
    }

    private void Error(ClientConnection connection, string message) =>
        Send(connection, new JsonObject { ["type"] = Msg.Error, ["message"] = message });

    private void Notice(string user, string message) =>
        SendTo(user, new JsonObject { ["type"] = Msg.Notice, ["message"] = message });

    private void ClockTick()
    {
        try
        {
            lock (_gate)
            {
                long now = NowMs();
                ExpireInvites(now);
                foreach (Room room in _rooms.Values.ToArray())
                {
                    if (!room.Active)
                    {
                        if (now - room.EndedAt >= 5 * 60_000) _rooms.Remove(room.Id);
                        continue;
                    }
                    if (CheckReconnectGrace(room, now)) continue;
                    CheckTurnTimeout(room, now);
                }
                RemoveExpiredSessions(now);
            }
        }
        catch (Exception ex) { Log($"CLOCK_ERROR {ex.GetType().Name}: {ex.Message}"); }
    }

    public void Dispose()
    {
        _clock?.Dispose(); _clock = null;
        _listener?.Stop(); _listener = null;
        lock (_gate)
        {
            foreach (Session session in _sessions.Values)
                try { session.Connection?.Client.Dispose(); } catch (SocketException) { }
            _sessions.Clear();
            _database?.Dispose(); _database = null;
        }
        Log("SERVER_STOP");
    }
}
