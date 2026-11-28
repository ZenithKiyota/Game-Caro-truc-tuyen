// Phụ trách: Hồ Thanh Anh Minh — danh sách online, gửi/chấp nhận/từ chối lời mời, tạo phòng.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private bool UserAvailable(string name)
    {
        return _sessions.TryGetValue(name, out var session) &&
               session.Connection is not null && session.RoomId < 0;
    }

    private void BroadcastUsers()
    {
        var users = new JsonArray();
        foreach (var session in _sessions.Values.Where(item => item.Connection is not null).OrderBy(item => item.Name))
        {
            users.Add(new JsonObject
            {
                ["name"] = session.Name,
                ["busy"] = session.RoomId >= 0
            });
        }
        var message = new JsonObject { ["type"] = Msg.Users, ["users"] = users };
        foreach (var session in _sessions.Values.Where(item => item.Connection is not null)) Send(session.Connection, message);
    }

    private void HandleInvite(ClientConnection connection, string me, JsonObject msg)
    {
        var target = msg["target"]?.GetValue<string>();
        if (target is null || target == me || !UserAvailable(me) || !UserAvailable(target))
        {
            Error(connection, "Người chơi không hợp lệ hoặc đang bận.");
            return;
        }
        _invites.RemoveAll(invite => invite.From == me || invite.To == me || invite.From == target || invite.To == target);
        _invites.Add(new Invite(me, target, NowMs() + Protocol.InviteTimeoutMs));
        SendTo(target, new JsonObject { ["type"] = Msg.InviteReceived, ["from"] = me });
    }

    private void HandleInviteReply(ClientConnection connection, string me, JsonObject msg)
    {
        var from = msg["from"]?.GetValue<string>();
        var accept = msg["accept"]?.GetValue<bool>();
        var invite = _invites.FirstOrDefault(item => item.From == from && item.To == me);
        if (from is null || accept is null || invite is null || invite.ExpiresAt < NowMs())
        {
            Error(connection, "Lời mời không hợp lệ hoặc đã hết hạn.");
            return;
        }
        _invites.Remove(invite);
        SendTo(from, new JsonObject { ["type"] = Msg.InviteResult, ["target"] = me, ["accepted"] = accept.Value });
        if (accept.Value) CreateMatch(from, me);
    }

    private void ExpireInvites(long now)
    {
        foreach (var invite in _invites.Where(item => item.ExpiresAt <= now).ToArray())
        {
            _invites.Remove(invite);
            Notice(invite.From, $"Lời mời tới {invite.To} đã hết hạn.");
        }
    }

    private void CreateMatch(string x, string o)
    {
        if (!_sessions.TryGetValue(x, out var xSession) || !_sessions.TryGetValue(o, out var oSession)) return;
        var now = NowMs();
        var room = new Room
        {
            Id = _nextRoomId++,
            X = x,
            O = o,
            StartedAt = now,
            Deadline = now + Protocol.TurnSeconds * 1000L,
            LastBroadcastSecond = Protocol.TurnSeconds
        };
        xSession.RoomId = room.Id;
        oSession.RoomId = room.Id;
        _rooms[room.Id] = room;
        Log($"MATCH_START room={room.Id} x={x} o={o}");
        BroadcastSnapshot(room);
        BroadcastUsers();
        BroadcastRooms();
    }
}
