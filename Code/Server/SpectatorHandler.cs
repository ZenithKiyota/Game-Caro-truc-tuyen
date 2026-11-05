// Phụ trách: Nguyễn Tuấn Tú - danh sách phòng đang đấu, khán giả, snapshot trạng thái trận.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private void BroadcastRooms(ClientConnection? recipient = null)
    {
        // rooms{rooms:[{id, x, o, spectators, paused}]} cho phòng Active
        var roomsArray = new JsonArray();

        lock (_gate)
        {
            foreach (var room in _rooms.Values)
            {
                if (!room.Active) continue;

                roomsArray.Add(new JsonObject
                {
                    ["id"] = room.Id,
                    ["x"] = room.X,
                    ["o"] = room.O,
                    ["spectators"] = room.Spectators.Count,
                    ["paused"] = room.Paused
                });
            }
        }

        var packet = new JsonObject
        {
            ["type"] = Msg.Rooms,
            ["rooms"] = roomsArray
        };

        if (recipient != null)
        {
            // Chỉ gửi cho người yêu cầu
            Send(recipient, packet);
        }
        else
        {
            // Broadcast cho tất cả client đang kết nối
            lock (_gate)
            {
                foreach (var s in _sessions.Values)
                {
                    if (s.Connection is { Connected: true } conn)
                    {
                        Send(conn, packet);
                    }
                }
            }
        }
    }

    private void HandleSpectate(ClientConnection connection, string me, JsonObject msg)
    {
        int roomId = msg["room"]?.GetValue<int>() ?? 0;
        Room? roomToWatch = null;

        lock (_gate)
        {
            if (!_sessions.TryGetValue(me, out var session))
            {
                Send(connection, new JsonObject { ["type"] = Msg.Error, ["message"] = "Không tìm thấy phiên đăng nhập." });
                return;
            }

            // Kiểm tra người chơi có đang rảnh không
            if (session.RoomId != -1)
            {
                Send(connection, new JsonObject { ["type"] = Msg.Error, ["message"] = "Bạn đang ở trong phòng khác, không thể vào xem." });
                return;
            }

            // Kiểm tra phòng có tồn tại và đang diễn ra không
            if (!_rooms.TryGetValue(roomId, out var room) || !room.Active)
            {
                Send(connection, new JsonObject { ["type"] = Msg.Error, ["message"] = "Phòng không tồn tại hoặc trận đấu đã kết thúc." });
                return;
            }

            // Thêm vào danh sách Spectators của phòng
            if (!room.Spectators.Contains(me))
            {
                room.Spectators.Add(me);
            }

            session.RoomId = room.Id;
            session.Spectator = true;
            roomToWatch = room;
        }

        // Gửi Snapshot đầy đủ ngay lập tức cho riêng khán giả này
        var snapshotMsg = Snapshot(roomToWatch);
        Send(connection, snapshotMsg);

        // Cập nhật lại số lượng người xem cho các đấu thủ và khán giả khác trong phòng
        BroadcastSnapshot(roomToWatch);

        // Cập nhật danh sách người dùng và danh sách phòng ở sảnh
        BroadcastUsers();
        BroadcastRooms();

        Log($"SPECTATE room={roomToWatch.Id} spectator={me} totalSpectators={roomToWatch.Spectators.Count}");
    }

    private void HandleLeaveSpectate(ClientConnection connection, string me)
    {
        Room? leftRoom = null;

        lock (_gate)
        {
            if (!_sessions.TryGetValue(me, out var session) || !session.Spectator || session.RoomId == -1)
            {
                Send(connection, new JsonObject { ["type"] = Msg.Error, ["message"] = "Bạn không ở trong phòng với vai trò khán giả." });
                return;
            }

            if (_rooms.TryGetValue(session.RoomId, out var room))
            {
                room.Spectators.Remove(me);
                leftRoom = room;
            }

            session.RoomId = -1;
            session.Spectator = false;
        }

        // Gửi xác nhận rời phòng an toàn cho khán giả
        Send(connection, new JsonObject { ["type"] = Msg.LeftRoom });

        // Cập nhật lại số lượng người xem cho phòng (không đụng tới trạng thái trận đấu)
        if (leftRoom != null)
        {
            BroadcastSnapshot(leftRoom);
        }

        BroadcastUsers();
        BroadcastRooms();

        Log($"LEAVE_SPECTATE user={me}");
    }

    private JsonObject Snapshot(Room room)
    {
        // Chuyển mảng Board 225 số thành 15 chuỗi ký tự (mỗi chuỗi 15 ký tự X/O/.)
        var boardArray = new JsonArray();
        for (int r = 0; r < Protocol.BoardSize; r++)
        {
            char[] row = new char[Protocol.BoardSize];
            for (int c = 0; c < Protocol.BoardSize; c++)
            {
                int val = room.Board[r * Protocol.BoardSize + c];
                row[c] = val switch { 1 => 'X', 2 => 'O', _ => '.' };
            }
            boardArray.Add(new string(row));
        }

        // Tính số giây còn lại trên đồng hồ
        int secondsLeft = 0;
        if (room.Active)
        {
            if (room.Paused)
            {
                secondsLeft = (int)Math.Max(0, Math.Ceiling(room.PausedRemaining / 1000.0));
            }
            else
            {
                long remainingMs = room.Deadline - NowMs();
                secondsLeft = (int)Math.Max(0, Math.Ceiling(remainingMs / 1000.0));
            }
        }

        // Kiểm tra 2 đấu thủ có đang online hay rớt mạng
        bool onlineX;
        bool onlineO;
        lock (_gate)
        {
            onlineX = _sessions.TryGetValue(room.X, out var sx) && sx.Connection is { Connected: true };
            onlineO = _sessions.TryGetValue(room.O, out var so) && so.Connection is { Connected: true };
        }

        // Đóng gói đầy đủ các trường theo PROTOCOL.md
        return new JsonObject
        {
            ["type"] = Msg.Snapshot,
            ["room"] = room.Id,
            ["x"] = room.X,
            ["o"] = room.O,
            ["board"] = boardArray,
            ["turn"] = room.Turn == 1 ? "X" : "O",
            ["secondsLeft"] = secondsLeft,
            ["paused"] = room.Paused,
            ["onlineX"] = onlineX,
            ["onlineO"] = onlineO,
            ["active"] = room.Active,
            ["winner"] = string.IsNullOrEmpty(room.Winner) ? null : room.Winner,
            ["reason"] = string.IsNullOrEmpty(room.Reason) ? null : room.Reason,
            ["spectators"] = room.Spectators.Count,
            ["lastRow"] = room.LastRow,
            ["lastCol"] = room.LastCol
        };
    }

    private void BroadcastSnapshot(Room room)
    {
        var snapshot = Snapshot(room);

        // Gửi Snapshot cho đấu thủ X và O
        SendTo(room.X, snapshot);
        SendTo(room.O, snapshot);

        // Gửi Snapshot cho toàn bộ khán giả đang xem phòng này
        List<string> spectatorsCopy;
        lock (_gate)
        {
            spectatorsCopy = room.Spectators.ToList();
        }

        foreach (var spectator in spectatorsCopy)
        {
            SendTo(spectator, snapshot);
        }
    }
}