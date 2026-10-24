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
    // Trận đã kết thúc thì không xử lý tạm dừng.
    if (!room.Active)
        return;

    long now = NowMs();

    // Kiểm tra trạng thái kết nối của hai người chơi.
    bool xOnline =
        _sessions.TryGetValue(room.X, out Session? xSession) &&
        xSession.Connection is not null;

    bool oOnline =
        _sessions.TryGetValue(room.O, out Session? oSession) &&
        oSession.Connection is not null;

    // Nếu thiếu một trong hai người chơi.
    if (!xOnline || !oOnline)
    {
        if (!room.Paused)
        {
            // Giữ lại thời gian của lượt hiện tại.
            room.PausedRemaining =
                Math.Max(0L, room.Deadline - now);

            room.Paused = true;
            room.LastBroadcastSecond = -1;

            Log($"MATCH_PAUSED: phòng {room.Id}");
        }

        return;
    }

    // Khi cả hai người chơi kết nối lại.
    if (room.Paused)
    {
        // Khôi phục thời gian lượt còn lại.
        room.Deadline =
            now + Math.Max(0L, room.PausedRemaining);

        room.Paused = false;
        room.PausedRemaining = 0;
        room.LastBroadcastSecond = -1;

        Log($"MATCH_RESUMED: phòng {room.Id}");
    }
}


    
private bool CheckReconnectGrace(Room room, long now)
{
    // Không xử lý trận đã kết thúc.
    if (!room.Active)
        return false;

    _sessions.TryGetValue(room.X, out Session? xSession);
    _sessions.TryGetValue(room.O, out Session? oSession);

    // Kiểm tra người chơi X mất kết nối quá 90 giây.
    bool xExpired =
        xSession is not null &&
        xSession.Connection is null &&
        xSession.OfflineSince > 0 &&
        now - xSession.OfflineSince >= Protocol.ReconnectGraceMs;

    // Kiểm tra người chơi O mất kết nối quá 90 giây.
    bool oExpired =
        oSession is not null &&
        oSession.Connection is null &&
        oSession.OfflineSince > 0 &&
        now - oSession.OfflineSince >= Protocol.ReconnectGraceMs;

    if (!xExpired && !oExpired)
        return false;

    // Cả hai cùng hết hạn: xét người mất kết nối trước.
    if (xExpired && oExpired)
    {
        if (xSession!.OfflineSince < oSession!.OfflineSince)
        {
            Finish(room, room.O, "X mất kết nối quá 90 giây");
        }
        else if (oSession.OfflineSince < xSession.OfflineSince)
        {
            Finish(room, room.X, "O mất kết nối quá 90 giây");
        }
        else
        {
            // Trường hợp cả hai mất kết nối đúng cùng thời điểm.
            Finish(room, "", "Cả hai mất kết nối quá 90 giây");
        }
    }
    else if (xExpired)
    {
        Finish(room, room.O, "X mất kết nối quá 90 giây");
    }
    else
    {
        Finish(room, room.X, "O mất kết nối quá 90 giây");
    }

    return true;
}


    private void RemoveExpiredSessions(long now)
    {
        // TODO(Phát): xóa session offline quá hạn và không còn trong phòng.
    }
}
