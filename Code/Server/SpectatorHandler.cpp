// Phụ trách: Nguyễn Tuấn Tú — danh sách phòng đang đấu, khán giả, snapshot trạng thái trận.
#include "CaroServer.h"
#include "Protocol.h"

void CaroServer::broadcastRooms(QTcpSocket *recipient) const {
    // TODO(Tú): rooms{rooms:[{id, x, o, spectators, paused}]} cho phòng active;
    // recipient != nullptr -> chỉ gửi cho người yêu cầu, ngược lại gửi tất cả.
    Q_UNUSED(recipient);
}

void CaroServer::handleSpectate(QTcpSocket *socket, const QString &me, const QJsonObject &msg) {
    // TODO(Tú): kiểm tra room hợp lệ, active, me đang rảnh; thêm vào spectators,
    // gắn session.spectator = true; gửi snapshot đầy đủ ngay; broadcastUsers/Rooms; log.
    Q_UNUSED(socket);
    Q_UNUSED(me);
    Q_UNUSED(msg);
}

void CaroServer::handleLeaveSpectate(QTcpSocket *socket, const QString &me) {
    // TODO(Tú): chỉ khán giả mới được rời; gỡ khỏi spectators, gửi left_room,
    // broadcastSnapshot (cập nhật số người xem), broadcastUsers/Rooms. Không đụng tới trận.
    Q_UNUSED(socket);
    Q_UNUSED(me);
}

QJsonObject CaroServer::snapshot(const Room &room) const {
    // TODO(Tú): đóng gói đầy đủ trường snapshot theo PROTOCOL.md (board 15 chuỗi X/O/.).
    return QJsonObject{{QStringLiteral("type"), QString(Caro::Msg::Snapshot)},
                       {QStringLiteral("room"), room.id}};
}

void CaroServer::broadcastSnapshot(const Room &room) const {
    // TODO(Tú): gửi snapshot cho X, O và toàn bộ khán giả.
    Q_UNUSED(room);
}
