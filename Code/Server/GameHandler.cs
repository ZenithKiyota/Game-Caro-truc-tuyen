// Phụ trách: Hứa Hồng Tiến — kiểm tra nước đi, xác định thắng/thua/hòa, giới hạn thời gian lượt.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private void HandleMove(ClientConnection connection, string me, JsonObject msg)
    {
        // TODO(Tiến): kiểm tra kiểu dữ liệu room/row/col; me là người chơi (không phải khán giả)
        // của phòng; trận Active, không Paused, chưa hết giờ; đúng lượt; ô hợp lệ và trống.
        // Đặt quân, ghi Moves, Log; thắng -> Finish; đầy bàn -> hòa; ngược lại đổi lượt + BroadcastSnapshot.
    }

    private bool HasWinner(Room room, int row, int col, int color)
    {
        // TODO(Tiến): đếm 4 hướng, >= Protocol.WinLength là thắng.
        return false;
    }

    private static bool IsBoardFull(Room room) => !room.Board.Contains(0);

    private bool CheckTurnTimeout(Room room, long now)
    {
        // TODO(Tiến): nếu now >= Deadline -> Finish(người còn lại, "Hết 30 giây lượt"), trả true;
        // ngược lại BroadcastSnapshot khi số giây còn lại thay đổi (LastBroadcastSecond).
        return false;
    }

    private void Finish(Room room, string winner, string reason)
    {
        // TODO(Tiến): Active=false, ghi Winner/Reason/EndedAt, SaveMatch(room) (Phát),
        // BroadcastSnapshot, trả RoomId=-1 cho người chơi + khán giả, Log MATCH_END,
        // BroadcastUsers/Rooms, SendHistory cho mọi người trong phòng.
    }
}
