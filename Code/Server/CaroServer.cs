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
        // TODO(Huy): ghi "thời gian | sự kiện" ra Console và file server.log
        // (Environment.SpecialFolder.ApplicationData/CaroServer). Không ghi token/dữ liệu bí mật.
    }

    public bool Start(IPAddress address, int port)
    {
        // TODO(Huy): OpenDatabase() (Phát), tạo TcpListener và Start(), Log,
        // _clock = new Timer(_ => ClockTick(), null, 250, 250).
        return false;
    }

    public async Task RunAsync(CancellationToken cancel)
    {
        // TODO(Huy): vòng AcceptTcpClientAsync; mỗi client -> _ = HandleClientAsync(...).
        await Task.CompletedTask;
    }

    private async Task HandleClientAsync(ClientConnection connection, CancellationToken cancel)
    {
        // TODO(Huy): đọc stream, gom buffer, tách theo '\n', chặn gói > Protocol.MaxFrameBytes,
        // JSON sai -> Error("JSON không hợp lệ."), JSON đúng -> lock(_gate) Process().
        // Khi kết thúc/ngắt kết nối: lock(_gate) Disconnected(connection) (Phát), đóng socket.
        await Task.CompletedTask;
    }

    private string? UserOf(ClientConnection connection)
    {
        // TODO(Huy): tên người dùng đang gắn với connection, null nếu chưa đăng nhập.
        return null;
    }

    private void Process(ClientConnection connection, JsonObject msg)
    {
        // TODO(Huy): định tuyến theo msg["type"]:
        //   hello          -> Hello()               (Phát)
        //   (chưa đăng nhập -> Error "Cần đăng nhập trước.")
        //   list_users     -> BroadcastUsers()      (Minh)
        //   invite         -> HandleInvite()        (Minh)
        //   invite_reply   -> HandleInviteReply()   (Minh)
        //   move           -> HandleMove()          (Tiến)
        //   list_rooms     -> BroadcastRooms(conn)  (Tú)
        //   spectate       -> HandleSpectate()      (Tú)
        //   leave_spectate -> HandleLeaveSpectate() (Tú)
        //   history        -> SendHistory()         (Phát)
        //   khác           -> Error "Loại yêu cầu không được hỗ trợ."
    }

    private void Send(ClientConnection? connection, JsonObject msg)
    {
        // TODO(Huy): bỏ qua nếu null/mất kết nối; ghi JSON compact (UTF-8) + '\n'; lỗi ghi -> đóng kết nối.
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
        // TODO(Huy): lock(_gate), mỗi 250 ms:
        //   ExpireInvites(now)                          (Minh)
        //   với từng phòng: xóa phòng đã kết thúc > 5 phút,
        //     CheckReconnectGrace(room, now)            (Phát)
        //     CheckTurnTimeout(room, now)               (Tiến) — gồm broadcast đồng hồ mỗi giây
        //   RemoveExpiredSessions(now)                  (Phát)
    }

    public void Dispose()
    {
        // TODO(Huy): dừng clock, listener; đóng mọi client và _database.
    }
}
