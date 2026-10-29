// Phụ trách: Hứa Hồng Tiến — kiểm tra nước đi, xác định thắng/thua/hòa, giới hạn thời gian lượt.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private void HandleMove(ClientConnection connection, string me, JsonObject msg)
    {
        if (!TryGetInt(msg, "room", out var roomId) ||
            !TryGetInt(msg, "row", out var row) ||
            !TryGetInt(msg, "col", out var col))
        {
            Error(connection, "Nước đi không hợp lệ: cần room, row và col là số nguyên.");
            return;
        }

        if (!_sessions.TryGetValue(me, out var session) || session.RoomId != roomId || session.Spectator)
        {
            Error(connection, "Bạn không có quyền đánh trong phòng này.");
            return;
        }

        if (!_rooms.TryGetValue(roomId, out var room))
        {
            Error(connection, "Phòng không tồn tại.");
            return;
        }

        if (!room.Active)
        {
            Error(connection, "Ván đấu đã kết thúc.");
            return;
        }

        if (room.Paused)
        {
            Error(connection, "Ván đấu đang tạm dừng vì người chơi mất kết nối.");
            return;
        }

        var color = me == room.X ? 1 : me == room.O ? 2 : 0;
        if (color == 0)
        {
            Error(connection, "Bạn không phải người chơi trong phòng này.");
            return;
        }

        var now = NowMs();
        if (CheckTurnTimeout(room, now))
        {
            Error(connection, "Bạn đã hết thời gian lượt đi.");
            return;
        }

        if (room.Turn != color)
        {
            Error(connection, "Chưa đến lượt của bạn.");
            return;
        }

        if (row < 0 || row >= Protocol.BoardSize || col < 0 || col >= Protocol.BoardSize)
        {
            Error(connection, "Vị trí nước đi nằm ngoài bàn cờ.");
            return;
        }

        var index = row * Protocol.BoardSize + col;
        if (room.Board[index] != 0)
        {
            Error(connection, "Ô này đã có quân.");
            return;
        }

        room.Board[index] = color;
        room.LastRow = row;
        room.LastCol = col;
        room.Moves.Add(new JsonObject
        {
            ["player"] = me,
            ["row"] = row,
            ["col"] = col,
            ["at"] = Timestamp(now)
        });

        if (HasWinner(room, row, col, color))
        {
            Finish(room, me, "Thắng bằng 5 quân liên tiếp");
            return;
        }

        if (IsBoardFull(room))
        {
            Finish(room, "", "Hòa: bàn cờ đã đầy");
            return;
        }

        room.Turn = color == 1 ? 2 : 1;
        room.Deadline = now + Protocol.TurnSeconds * 1000L;
        room.LastBroadcastSecond = -1;
        BroadcastSnapshot(room);
    }

    private bool HasWinner(Room room, int row, int col, int color)
    {
        if (color == 0 || row < 0 || row >= Protocol.BoardSize || col < 0 || col >= Protocol.BoardSize)
            return false;

        var directions = new[] { (rowStep: 0, colStep: 1), (rowStep: 1, colStep: 0),
            (rowStep: 1, colStep: 1), (rowStep: 1, colStep: -1) };

        foreach (var (rowStep, colStep) in directions)
        {
            var count = 1;
            count += CountDirection(room, row, col, rowStep, colStep, color);
            count += CountDirection(room, row, col, -rowStep, -colStep, color);
            if (count >= Protocol.WinLength) return true;
        }

        return false;
    }

    private static int CountDirection(Room room, int row, int col, int rowStep, int colStep, int color)
    {
        var count = 0;
        var currentRow = row + rowStep;
        var currentCol = col + colStep;
        while (currentRow >= 0 && currentRow < Protocol.BoardSize &&
               currentCol >= 0 && currentCol < Protocol.BoardSize &&
               room.Board[currentRow * Protocol.BoardSize + currentCol] == color)
        {
            count++;
            currentRow += rowStep;
            currentCol += colStep;
        }

        return count;
    }

    private static bool IsBoardFull(Room room) => !room.Board.Contains(0);

    private bool CheckTurnTimeout(Room room, long now)
    {
        if (!room.Active || room.Paused) return false;

        var remainingMs = room.Deadline - now;
        if (remainingMs <= 0)
        {
            var winner = room.Turn == 1 ? room.O : room.X;
            Finish(room, winner, "Hết 30 giây lượt");
            return true;
        }

        var secondsLeft = (int)Math.Ceiling(remainingMs / 1000d);
        if (secondsLeft != room.LastBroadcastSecond)
        {
            room.LastBroadcastSecond = secondsLeft;
            BroadcastSnapshot(room);
        }

        return false;
    }

    private void Finish(Room room, string winner, string reason)
    {
        if (!room.Active) return;

        room.Active = false;
        room.Paused = false;
        room.Winner = winner;
        room.Reason = reason;
        room.EndedAt = NowMs();
        room.LastBroadcastSecond = 0;

        // Gửi snapshot cuối cùng trước khi xóa liên kết phòng trong session, để client
        // vẫn hiển thị được bàn cờ và kết quả ván vừa kết thúc.
        BroadcastSnapshot(room);
        SaveMatch(room);

        var participants = new HashSet<string>(StringComparer.Ordinal) { room.X, room.O };
        participants.UnionWith(room.Spectators);
        foreach (var user in participants)
        {
            if (_sessions.TryGetValue(user, out var session) && session.RoomId == room.Id)
            {
                session.RoomId = -1;
                session.Spectator = false;
            }
            SendHistory(user);
        }

        room.Spectators.Clear();
        Log($"MATCH_END room={room.Id} winner={winner} reason={reason}");
        BroadcastUsers();
        BroadcastRooms();
    }

    private static bool TryGetInt(JsonObject msg, string property, out int value)
    {
        value = 0;
        return msg[property] is JsonValue jsonValue && jsonValue.TryGetValue<int>(out value);
    }
}
