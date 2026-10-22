// Phụ trách: Hồ Thanh Anh Minh — danh sách online, gửi/chấp nhận/từ chối lời mời, tạo phòng.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private bool UserAvailable(string name)
    {
        // TODO(Minh): online và chưa ở phòng nào.
        return false;
    }

    private void BroadcastUsers()
    {
        // TODO(Minh): gửi users{users:[{name, busy}]} (sắp xếp theo tên) tới mọi client online.
    }

    private void HandleInvite(ClientConnection connection, string me, JsonObject msg)
    {
        // TODO(Minh): kiểm tra target hợp lệ/rảnh, không tự mời, không mời trùng;
        // lưu Invite (hết hạn sau Protocol.InviteTimeoutMs), gửi invite_received cho target, Notice cho me.
    }

    private void HandleInviteReply(ClientConnection connection, string me, JsonObject msg)
    {
        // TODO(Minh): kiểm tra accept là bool, lời mời còn hạn; gửi invite_result cho người mời;
        // chấp nhận -> CreateMatch(from, me).
    }

    private void ExpireInvites(long now)
    {
        // TODO(Minh): xóa lời mời hết hạn hoặc khi một bên đã bận/thoát; báo người mời.
    }

    private void CreateMatch(string x, string o)
    {
        // TODO(Minh): tạo Room mới (Id = _nextRoomId++), gán RoomId cho 2 session,
        // Deadline = now + Protocol.TurnSeconds; Log MATCH_START; BroadcastSnapshot/Users/Rooms.
    }
}
