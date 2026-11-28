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
    private int _nextRoomId = 1;

    // ---------- Huy: lõi mạng, điều phối, log ----------
    private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private static string Timestamp(long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).ToString("O");

    private void Log(string message)
    {
        Console.WriteLine($"{DateTimeOffset.Now:O} | {message}");
    }

    public bool Start(IPAddress address, int port)
    {
        if (port is < 1 or > 65535) return false;
        try
        {
            if (!OpenDatabase()) return false;
            _listener = new TcpListener(address, port);
            _listener.Start();
            _clock = new Timer(_ => ClockTick(), null, 250, 250);
            Log($"SERVER_START address={address} port={port}");
            return true;
        }
        catch (Exception error) when (error is SocketException or InvalidOperationException)
        {
            Log($"SERVER_START_FAILED error={error.Message}");
            _listener?.Stop();
            _listener = null;
            return false;
        }
    }

    public async Task RunAsync(CancellationToken cancel)
    {
        if (_listener is null) return;
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancel);
                client.NoDelay = true;
                var connection = new ClientConnection
                {
                    Client = client,
                    Stream = client.GetStream()
                };
                Log($"CLIENT_CONNECT remote={connection.Remote}");
                _ = HandleClientAsync(connection, cancel);
            }
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancel.IsCancellationRequested)
        {
        }
    }

    private async Task HandleClientAsync(ClientConnection connection, CancellationToken cancel)
    {
        var buffer = new List<byte>(Protocol.MaxFrameBytes);
        var readBuffer = new byte[4096];
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var count = await connection.Stream.ReadAsync(readBuffer, cancel);
                if (count == 0) break;

                for (var index = 0; index < count; index++)
                {
                    var value = readBuffer[index];
                    if (value == (byte)'\n')
                    {
                        if (buffer.Count > 0 && buffer[^1] == (byte)'\r') buffer.RemoveAt(buffer.Count - 1);
                        if (buffer.Count > Protocol.MaxFrameBytes)
                        {
                            Error(connection, "Gói tin vượt quá giới hạn 64 KiB.");
                            return;
                        }

                        var line = Encoding.UTF8.GetString(buffer.ToArray());
                        buffer.Clear();
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            if (JsonNode.Parse(line) is not JsonObject message)
                            {
                                Error(connection, "JSON không hợp lệ.");
                                continue;
                            }

                            lock (_gate) Process(connection, message);
                        }
                        catch (JsonException)
                        {
                            Error(connection, "JSON không hợp lệ.");
                        }
                    }
                    else
                    {
                        buffer.Add(value);
                        if (buffer.Count > Protocol.MaxFrameBytes)
                        {
                            Error(connection, "Gói tin vượt quá giới hạn 64 KiB.");
                            return;
                        }
                    }
                }
            }
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_gate) Disconnected(connection);
            connection.Client.Close();
        }
    }

    private string? UserOf(ClientConnection connection)
    {
        return _sessions.Values.FirstOrDefault(session => ReferenceEquals(session.Connection, connection))?.Name;
    }

    private void Process(ClientConnection connection, JsonObject msg)
    {
        var type = msg["type"]?.GetValue<string>();
        if (type == Msg.Hello)
        {
            Hello(connection, msg);
            return;
        }

        var me = UserOf(connection);
        if (me is null)
        {
            Error(connection, "Cần đăng nhập trước.");
            return;
        }

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
        if (connection is null || !connection.Connected) return;
        try
        {
            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg) + "\n");
            lock (connection) connection.Stream.Write(payload, 0, payload.Length);
        }
        catch (IOException)
        {
            connection.Client.Close();
        }
        catch (SocketException)
        {
            connection.Client.Close();
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
        lock (_gate)
        {
            var now = NowMs();
            ExpireInvites(now);
            foreach (var room in _rooms.Values.ToArray())
            {
                CheckReconnectGrace(room, now);
                CheckTurnTimeout(room, now);
                if (!room.Active && room.EndedAt > 0 && now - room.EndedAt > 5 * 60_000)
                    _rooms.Remove(room.Id);
            }
            RemoveExpiredSessions(now);
        }
    }

    public void Dispose()
    {
        _clock?.Dispose();
        _clock = null;
        _listener?.Stop();
        _listener = null;
        lock (_gate)
        {
            foreach (var session in _sessions.Values)
                session.Connection?.Client.Close();
            _sessions.Clear();
            _rooms.Clear();
        }
        _database?.Dispose();
        _database = null;
    }
}
