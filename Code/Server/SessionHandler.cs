// Phụ trách: Nguyễn An Phát — đăng nhập, phiên (token), ngắt kết nối, tạm dừng & reconnect.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    
private void Hello(ClientConnection connection, JsonObject msg)
{
    // Không cho một kết nối đăng nhập thành nhiều tài khoản.
    foreach (var item in _sessions.Values)
    {
        if (ReferenceEquals(item.Connection, connection))
        {
            Error(connection, "Kết nối này đã đăng nhập.");
            return;
        }
    }

    // Kiểm tra tên đăng nhập.
    if (msg["name"] is not JsonValue nameValue ||
        !nameValue.TryGetValue<string>(out string? name) ||
        string.IsNullOrEmpty(name) ||
        !System.Text.RegularExpressions.Regex.IsMatch(
            name, @"\A[A-Za-z0-9_]{3,20}\z"))
    {
        Error(connection, "Tên phải có 3-20 ký tự, chỉ gồm chữ, số hoặc dấu _.");
        return;
    }

    // Lấy token từ Client, lần đăng nhập đầu tiên có thể bỏ trống.
    string token = "";

    if (msg["token"] is not null)
    {
        if (msg["token"] is not JsonValue tokenValue ||
            !tokenValue.TryGetValue<string>(out string? receivedToken))
        {
            Error(connection, "Token không hợp lệ.");
            return;
        }

        token = receivedToken ?? "";
    }

    bool resumed = false;
    Session session;

    if (_sessions.TryGetValue(name, out Session? existing))
    {
        // Tên đã có người sử dụng.
        if (existing.Connection is not null)
        {
            Error(connection, "Tên đăng nhập đang được sử dụng.");
            return;
        }

        // Chỉ cho kết nối lại bằng token cũ.
        if (string.IsNullOrEmpty(token) ||
            !string.Equals(existing.Token, token, StringComparison.Ordinal))
        {
            Error(connection, "Token không hợp lệ hoặc tên đã tồn tại.");
            return;
        }

        // Không cho tiếp tục phiên đã hết hạn 90 giây.
        if (existing.OfflineSince > 0 &&
            NowMs() - existing.OfflineSince >= Protocol.ReconnectGraceMs)
        {
            Error(connection, "Phiên kết nối lại đã hết hạn.");
            return;
        }

        existing.Connection = connection;
        existing.OfflineSince = 0;

        session = existing;
        resumed = true;
    }
    else
    {
        // Token cũ không được dùng để tạo tài khoản mới.
        if (!string.IsNullOrEmpty(token))
        {
            Error(connection, "Không tìm thấy phiên để kết nối lại.");
            return;
        }

        session = new Session
        {
            Name = name,
            Token = Guid.NewGuid().ToString("N"),
            Connection = connection,
            OfflineSince = 0
        };

        _sessions.Add(name, session);
    }

    // Thông báo đăng nhập thành công cho Client.
    Send(connection, new JsonObject
    {
        ["type"] = Msg.Welcome,
        ["name"] = session.Name,
        ["token"] = session.Token,
        ["resumed"] = resumed
    });

    // Nếu reconnect vào trận đang chơi, khôi phục trạng thái.
    if (resumed &&
        session.RoomId >= 0 &&
        _rooms.TryGetValue(session.RoomId, out Room? room) &&
        room.Active)
    {
        PauseIfNeeded(room);
        BroadcastSnapshot(room);
    }

    SendHistory(session.Name);
    BroadcastUsers();
    BroadcastRooms();

    Log(resumed
        ? $"RECONNECT: {session.Name}"
        : $"LOGIN: {session.Name}");
}


    
private void Disconnected(ClientConnection connection)
{
    Session? session = null;

    // Tìm phiên đang sử dụng kết nối vừa bị ngắt.
    foreach (var item in _sessions.Values)
    {
        if (ReferenceEquals(item.Connection, connection))
        {
            session = item;
            break;
        }
    }

    // Kết nối chưa đăng nhập hoặc đã được thay thế.
    if (session is null)
        return;

    session.Connection = null;
    session.OfflineSince = NowMs();

    // Nếu người dùng đang ở trong phòng.
    if (session.RoomId >= 0 &&
        _rooms.TryGetValue(session.RoomId, out Room? room))
    {
        if (session.Spectator)
        {
            // Khán giả mất kết nối thì rời phòng.
            room.Spectators.Remove(session.Name);
            session.RoomId = -1;
            session.Spectator = false;

            if (room.Active)
                BroadcastSnapshot(room);
        }
        else if (room.Active)
        {
            // Người chơi mất kết nối thì tạm dừng trận.
            PauseIfNeeded(room);
            BroadcastSnapshot(room);
        }
    }
    else if (session.Spectator)
    {
        session.RoomId = -1;
        session.Spectator = false;
    }

    Log($"DISCONNECTED: {session.Name}");

    BroadcastUsers();
    BroadcastRooms();
}


    private void PauseIfNeeded(Room room)
    {
        // TODO(Phát): thiếu người chơi online -> Paused, lưu PausedRemaining;
        // đủ người -> tiếp tục với Deadline = now + PausedRemaining.
    }

    private bool CheckReconnectGrace(Room room, long now)
    {
        // TODO(Phát): người chơi offline >= Protocol.ReconnectGraceMs -> Finish() xử thua, trả true.
        return false;
    }

    private void RemoveExpiredSessions(long now)
    {
        // TODO(Phát): xóa session offline quá hạn và không còn trong phòng.
    }
}
