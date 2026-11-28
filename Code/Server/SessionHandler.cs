// Phụ trách: Nguyễn An Phát — đăng nhập, phiên (token), ngắt kết nối, tạm dừng & reconnect.
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private void Hello(ClientConnection connection, JsonObject msg)
    {
        var name = msg["name"]?.GetValue<string>();
        var token = msg["token"]?.GetValue<string>() ?? "";
        if (name is null || !Regex.IsMatch(name, "^[A-Za-z0-9_]{3,20}$"))
        {
            Error(connection, "Tên phải gồm 3–20 ký tự chữ, số hoặc _. ");
            return;
        }

        var resumed = false;
        if (_sessions.TryGetValue(name, out var existing))
        {
            if (existing.Connection is not null && existing.Connection != connection)
            {
                Error(connection, "Tên người dùng đang được sử dụng.");
                return;
            }
            if (string.IsNullOrEmpty(token) || !string.Equals(token, existing.Token, StringComparison.Ordinal))
            {
                Error(connection, "Token không hợp lệ.");
                return;
            }
            existing.Connection = connection;
            existing.OfflineSince = 0;
            resumed = true;
            if (existing.RoomId >= 0 && _rooms.TryGetValue(existing.RoomId, out var resumedRoom))
            {
                resumedRoom.Paused = false;
                resumedRoom.Deadline = NowMs() + Protocol.TurnSeconds * 1000L;
                resumedRoom.LastBroadcastSecond = Protocol.TurnSeconds;
                BroadcastSnapshot(resumedRoom);
            }
        }
        else
        {
            existing = new Session
            {
                Name = name,
                Token = Guid.NewGuid().ToString("N"),
                Connection = connection
            };
            _sessions[name] = existing;
        }

        Send(connection, new JsonObject
        {
            ["type"] = Msg.Welcome,
            ["name"] = name,
            ["token"] = existing.Token,
            ["resumed"] = resumed
        });
        SendHistory(name);
        BroadcastUsers();
        BroadcastRooms();
    }

    private void Disconnected(ClientConnection connection)
    {
        var name = UserOf(connection);
        if (name is null || !_sessions.TryGetValue(name, out var session)) return;
        session.Connection = null;
        session.OfflineSince = NowMs();
        if (session.Spectator && session.RoomId >= 0 && _rooms.TryGetValue(session.RoomId, out var spectatorRoom))
        {
            spectatorRoom.Spectators.Remove(name);
            session.RoomId = -1;
            session.Spectator = false;
        }
        else if (session.RoomId >= 0 && _rooms.TryGetValue(session.RoomId, out var room))
        {
            PauseIfNeeded(room);
            BroadcastSnapshot(room);
        }
        Log($"CLIENT_DISCONNECT user={name}");
        BroadcastUsers();
        BroadcastRooms();
    }

    private void PauseIfNeeded(Room room)
    {
        var xOnline = _sessions.TryGetValue(room.X, out var x) && x.Connection is not null;
        var oOnline = _sessions.TryGetValue(room.O, out var o) && o.Connection is not null;
        if (!xOnline || !oOnline)
        {
            room.PausedRemaining = Math.Max(0, room.Deadline - NowMs());
            room.Paused = true;
        }
    }

    private bool CheckReconnectGrace(Room room, long now)
    {
        if (!room.Active || !room.Paused) return false;
        var xOffline = _sessions.TryGetValue(room.X, out var x) && x.Connection is null && x.OfflineSince > 0;
        var oOffline = _sessions.TryGetValue(room.O, out var o) && o.Connection is null && o.OfflineSince > 0;
        if (xOffline && now - x!.OfflineSince >= Protocol.ReconnectGraceMs)
        {
            Finish(room, room.O, "Người chơi X mất kết nối quá 90 giây");
            return true;
        }
        if (oOffline && now - o!.OfflineSince >= Protocol.ReconnectGraceMs)
        {
            Finish(room, room.X, "Người chơi O mất kết nối quá 90 giây");
            return true;
        }
        return false;
    }

    private void RemoveExpiredSessions(long now)
    {
        foreach (var pair in _sessions.ToArray())
        {
            if (pair.Value.Connection is null && pair.Value.RoomId < 0 &&
                pair.Value.OfflineSince > 0 && now - pair.Value.OfflineSince >= Protocol.ReconnectGraceMs)
                _sessions.Remove(pair.Key);
        }
    }
}
