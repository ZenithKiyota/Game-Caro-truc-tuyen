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
        if (msg["target"] is not JsonValue targetValue
            || !targetValue.TryGetValue(out string? target)
            || string.IsNullOrWhiteSpace(target))
        {
            Error(connection, "Thiếu tên người được mời.");
            return;
        }
        if (target == me) { Error(connection, "Không thể tự mời chính mình."); return; }
        if (!_sessions.TryGetValue(target, out Session? targetSession) || targetSession.Connection is null)
        {
            Error(connection, $"{target} không online.");
            return;
        }
        if (!UserAvailable(me)) { Error(connection, "Bạn đang ở trong phòng, không thể mời người khác."); return; }
        if (!UserAvailable(target)) { Error(connection, $"{target} đang bận."); return; }
        if (_invites.Any(i => i.From == me && i.To == target))
        {
            Error(connection, $"Đã mời {target}, hãy chờ phản hồi.");
            return;
        }
        if (_invites.Any(i => i.From == target && i.To == me))
        {
            Error(connection, $"{target} đã mời bạn, hãy trả lời lời mời đó.");
            return;
        }

        _invites.Add(new Invite(me, target, NowMs() + Protocol.InviteTimeoutMs));
        SendTo(target, new JsonObject { ["type"] = Msg.InviteReceived, ["from"] = me });
        Notice(me, $"Đã mời {target} (hiệu lực {Protocol.InviteTimeoutMs / 1000} giây).");
        Log($"INVITE {me} -> {target}");
    }

    private void HandleInviteReply(ClientConnection connection, string me, JsonObject msg)
    {
        if (msg["from"] is not JsonValue fromValue
            || !fromValue.TryGetValue(out string? from)
            || string.IsNullOrWhiteSpace(from))
        {
            Error(connection, "Thiếu tên người mời.");
            return;
        }
        if (msg["accept"] is not JsonValue acceptValue || !acceptValue.TryGetValue(out bool accept))
        {
            Error(connection, "Phản hồi không hợp lệ: accept phải là true hoặc false.");
            return;
        }

        long now = NowMs();
        int index = _invites.FindIndex(i => i.From == from && i.To == me && i.ExpiresAt > now);
        if (index < 0) { Error(connection, "Lời mời đã hết hạn hoặc không tồn tại."); return; }
        _invites.RemoveAt(index);

        // Kiểm tra trước khi báo kết quả, để người mời không nhận "đã chấp nhận" mà không có trận.
        if (accept && (!UserAvailable(me) || !UserAvailable(from)))
        {
            Error(connection, "Một người chơi đã bận hoặc đã thoát, không thể tạo phòng.");
            Notice(from, $"{me} đã đồng ý nhưng không thể tạo phòng vì một bên đã bận hoặc đã thoát.");
            Log($"INVITE_REPLY {me} -> {from} accepted=True nhưng không tạo được phòng");
            return;
        }

        SendTo(from, new JsonObject { ["type"] = Msg.InviteResult, ["target"] = me, ["accepted"] = accept });
        Log($"INVITE_REPLY {me} -> {from} accepted={accept}");
        if (!accept)
        {
            Notice(me, $"Đã từ chối lời mời từ {from}.");
            return;
        }
        CreateMatch(from, me);
    }

    // Gọi từ ClockTick: bỏ lời mời quá hạn, hoặc khi một bên đã vào phòng khác hay mất kết nối.
    private void ExpireInvites(long now)
    {
        for (int i = _invites.Count - 1; i >= 0; i--)
        {
            Invite invite = _invites[i];
            bool expired = invite.ExpiresAt <= now;
            if (!expired && UserAvailable(invite.From) && UserAvailable(invite.To)) continue;

            _invites.RemoveAt(i);
            if (expired)
            {
                Notice(invite.From, $"Lời mời tới {invite.To} đã hết hạn.");
                Notice(invite.To, $"Lời mời từ {invite.From} đã hết hạn.");
                Log($"INVITE_EXPIRED {invite.From} -> {invite.To}");
            }
            else
            {
                Notice(invite.From, $"Lời mời tới {invite.To} đã bị huỷ vì một bên đang bận hoặc đã thoát.");
            }
        }
    }

    // Người mời cầm X và đi trước. Mỗi phòng có trạng thái riêng trong _rooms nên nhiều trận chạy song song.
    private void CreateMatch(string x, string o)
    {
        if (!UserAvailable(x) || !UserAvailable(o)) return;

        long now = NowMs();
        var room = new Room
        {
            Id = _nextRoomId++,
            X = x,
            O = o,
            StartedAt = now,
            Deadline = now + Protocol.TurnSeconds * 1000L,
        };
        _rooms.Add(room.Id, room);
        foreach (string name in new[] { x, o })
        {
            _sessions[name].RoomId = room.Id;
            _sessions[name].Spectator = false;
        }

        Log($"MATCH_START #{room.Id} X={x} O={o}");
        BroadcastSnapshot(room);
        BroadcastUsers();
        BroadcastRooms();
    }
}
