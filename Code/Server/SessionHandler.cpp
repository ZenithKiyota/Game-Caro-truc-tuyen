// Phụ trách: Nguyễn An Phát — đăng nhập, phiên (token), ngắt kết nối, tạm dừng & reconnect.
#include "CaroServer.h"
#include "Protocol.h"

void CaroServer::hello(QTcpSocket *socket, const QJsonObject &msg) {
    // TODO(Phát): kiểm tra tên ^[A-Za-z0-9_]{3,20}$, chặn tên trùng đang online,
    // cấp token mới hoặc khớp token cũ để resume, gửi welcome{name, token, resumed},
    // nếu đang trong phòng -> pauseIfNeeded + gửi snapshot; gửi lịch sử; broadcastUsers/Rooms.
    Q_UNUSED(socket);
    Q_UNUSED(msg);
}

void CaroServer::disconnected(QTcpSocket *socket) {
    // TODO(Phát): gỡ socket khỏi session, ghi offlineSince; khán giả -> rời phòng;
    // người chơi trong trận -> pauseIfNeeded + broadcastSnapshot; log; broadcastUsers/Rooms.
    input_.remove(socket);
}

void CaroServer::pauseIfNeeded(Room &room) {
    // TODO(Phát): thiếu người chơi online -> paused, lưu pausedRemaining;
    // đủ người -> tiếp tục với deadline = now + pausedRemaining.
    Q_UNUSED(room);
}

bool CaroServer::checkReconnectGrace(Room &room, qint64 now) {
    // TODO(Phát): người chơi offline >= Caro::kReconnectGraceMs -> finish() xử thua, trả true.
    Q_UNUSED(room);
    Q_UNUSED(now);
    return false;
}

void CaroServer::removeExpiredSessions(qint64 now) {
    // TODO(Phát): xóa session offline quá hạn và không còn trong phòng.
    Q_UNUSED(now);
}
