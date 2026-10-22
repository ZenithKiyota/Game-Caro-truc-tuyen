// Phụ trách: Nguyễn An Phát — đăng nhập, phiên (token), ngắt kết nối, tạm dừng & reconnect.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private void Hello(ClientConnection connection, JsonObject msg)
    {
        // TODO(Phát): kiểm tra tên ^[A-Za-z0-9_]{3,20}$, chặn tên trùng đang online,
        // cấp token mới (Guid) hoặc khớp token cũ để resume, gửi welcome{name, token, resumed},
        // nếu đang trong phòng -> PauseIfNeeded + gửi snapshot; SendHistory; BroadcastUsers/Rooms.
    }

    private void Disconnected(ClientConnection connection)
    {
        // TODO(Phát): gỡ connection khỏi session, ghi OfflineSince; khán giả -> rời phòng;
        // người chơi trong trận -> PauseIfNeeded + BroadcastSnapshot; Log; BroadcastUsers/Rooms.
    }

    private void PauseIfNeeded(Room room)
    {
        // TODO(Phát): thiếu người chơi online -> Paused, lưu PausedRemaining;
        // đủ người -> tiếp tục với Deadline = now + PausedRemaining.
    }

    private bool CheckReconnectGrace(Room room, long now)
    {
        // TODO(Phát): người chơi offline >= Protocol.ReconnectGraceMs -> Finish() xử thua, trả true.
        return false;
    }

    private void RemoveExpiredSessions(long now)
    {
        // TODO(Phát): xóa session offline quá hạn và không còn trong phòng.
    }
}
