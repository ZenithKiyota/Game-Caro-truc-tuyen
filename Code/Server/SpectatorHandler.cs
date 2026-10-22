// Phụ trách: Nguyễn Tuấn Tú — danh sách phòng đang đấu, khán giả, snapshot trạng thái trận.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private void BroadcastRooms(ClientConnection? recipient = null)
    {
        // TODO(Tú): rooms{rooms:[{id, x, o, spectators, paused}]} cho phòng Active;
        // recipient != null -> chỉ gửi cho người yêu cầu, ngược lại gửi tất cả.
    }

    private void HandleSpectate(ClientConnection connection, string me, JsonObject msg)
    {
        // TODO(Tú): kiểm tra room hợp lệ, Active, me đang rảnh; thêm vào Spectators,
        // gắn session.Spectator = true; gửi snapshot đầy đủ ngay; BroadcastUsers/Rooms; Log.
    }

    private void HandleLeaveSpectate(ClientConnection connection, string me)
    {
        // TODO(Tú): chỉ khán giả mới được rời; gỡ khỏi Spectators, gửi left_room,
        // BroadcastSnapshot (cập nhật số người xem), BroadcastUsers/Rooms. Không đụng tới trận.
    }

    private JsonObject Snapshot(Room room)
    {
        // TODO(Tú): đóng gói đầy đủ trường snapshot theo PROTOCOL.md (board 15 chuỗi X/O/.).
        return new JsonObject { ["type"] = Msg.Snapshot, ["room"] = room.Id };
    }

    private void BroadcastSnapshot(Room room)
    {
        // TODO(Tú): gửi Snapshot cho X, O và toàn bộ khán giả.
    }
}
