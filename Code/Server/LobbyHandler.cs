// Phụ trách: Hồ Thanh Anh Minh — danh sách online, gửi/chấp nhận/từ chối lời mời, tạo phòng.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    // Rảnh = đang online và chưa ở phòng nào (chơi hoặc xem).
    private bool UserAvailable(string name) =>
        _sessions.TryGetValue(name, out Session? session)
        && session.Connection is not null
        && session.RoomId < 0;

    private void BroadcastUsers()
    {
        var users = new JsonArray();
        foreach (Session session in _sessions.Values
                     .Where(s => s.Connection is not null)
                     .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
        {
            users.Add(new JsonObject { ["name"] = session.Name, ["busy"] = session.RoomId >= 0 });
        }

        var msg = new JsonObject { ["type"] = Msg.Users, ["users"] = users };
        foreach (Session session in _sessions.Values)
            Send(session.Connection, msg);
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
