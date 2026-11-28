// Phụ trách: Nguyễn Tuấn Tú — danh sách phòng đang đấu, khán giả, snapshot trạng thái trận.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private void BroadcastRooms(ClientConnection? recipient = null)
    {
        var rooms = new JsonArray();
        foreach (var room in _rooms.Values.Where(item => item.Active))
        {
            rooms.Add(new JsonObject
            {
                ["id"] = room.Id,
                ["x"] = room.X,
                ["o"] = room.O,
                ["spectators"] = room.Spectators.Count,
                ["paused"] = room.Paused
            });
        }
        var message = new JsonObject { ["type"] = Msg.Rooms, ["rooms"] = rooms };
        if (recipient is not null)
        {
            Send(recipient, message);
            return;
        }
        foreach (var session in _sessions.Values.Where(item => item.Connection is not null)) Send(session.Connection, message);
    }

    private void HandleSpectate(ClientConnection connection, string me, JsonObject msg)
    {
        if (!_sessions.TryGetValue(me, out var session) || session.RoomId >= 0 ||
            !TryGetInt(msg, "room", out var roomId) || !_rooms.TryGetValue(roomId, out var room) || !room.Active)
        {
            Error(connection, "Không thể vào xem phòng này.");
            return;
        }
        session.RoomId = roomId;
        session.Spectator = true;
        room.Spectators.Add(me);
        Send(connection, Snapshot(room));
        BroadcastUsers();
        BroadcastRooms();
    }

    private void HandleLeaveSpectate(ClientConnection connection, string me)
    {
        if (!_sessions.TryGetValue(me, out var session) || !session.Spectator ||
            !_rooms.TryGetValue(session.RoomId, out var room))
        {
            Error(connection, "Bạn không phải khán giả.");
            return;
        }
        room.Spectators.Remove(me);
        session.RoomId = -1;
        session.Spectator = false;
        Send(connection, new JsonObject { ["type"] = Msg.LeftRoom });
        BroadcastSnapshot(room);
        BroadcastUsers();
        BroadcastRooms();
    }

    private JsonObject Snapshot(Room room)
    {
        var board = new JsonArray();
        for (var row = 0; row < Protocol.BoardSize; row++)
        {
            var line = new char[Protocol.BoardSize];
            for (var col = 0; col < Protocol.BoardSize; col++)
            {
                var value = room.Board[row * Protocol.BoardSize + col];
                line[col] = value == 1 ? 'X' : value == 2 ? 'O' : '.';
            }
            board.Add(new string(line));
        }
        var secondsLeft = room.Active && !room.Paused
            ? Math.Max(0, (int)Math.Ceiling((room.Deadline - NowMs()) / 1000d))
            : 0;
        return new JsonObject
        {
            ["type"] = Msg.Snapshot,
            ["room"] = room.Id,
            ["x"] = room.X,
            ["o"] = room.O,
            ["board"] = board,
            ["turn"] = room.Turn == 1 ? "X" : "O",
            ["secondsLeft"] = secondsLeft,
            ["paused"] = room.Paused,
            ["onlineX"] = _sessions.TryGetValue(room.X, out var x) && x.Connection is not null,
            ["onlineO"] = _sessions.TryGetValue(room.O, out var o) && o.Connection is not null,
            ["active"] = room.Active,
            ["winner"] = room.Winner,
            ["reason"] = room.Reason,
            ["spectators"] = new JsonArray(room.Spectators.Select(name => (JsonNode?)name).ToArray()),
            ["lastRow"] = room.LastRow,
            ["lastCol"] = room.LastCol
        };
    }

    private void BroadcastSnapshot(Room room)
    {
        var message = Snapshot(room);
        SendTo(room.X, message);
        SendTo(room.O, message);
        foreach (var spectator in room.Spectators) SendTo(spectator, message);
    }

}
