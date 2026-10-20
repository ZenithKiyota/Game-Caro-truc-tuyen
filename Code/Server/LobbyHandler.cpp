// Phụ trách: Hồ Thanh Anh Minh — danh sách online, gửi/chấp nhận/từ chối lời mời, tạo phòng.
#include "CaroServer.h"
#include "Protocol.h"

bool CaroServer::userAvailable(const QString &name) const {
    // TODO(Minh): online và chưa ở phòng nào.
    Q_UNUSED(name);
    return false;
}

void CaroServer::broadcastUsers() const {
    // TODO(Minh): gửi users{users:[{name, busy}]} (sắp xếp theo tên) tới mọi client online.
}

void CaroServer::handleInvite(QTcpSocket *socket, const QString &me, const QJsonObject &msg) {
    // TODO(Minh): kiểm tra target hợp lệ/rảnh, không tự mời, không mời trùng;
    // lưu Invite (hết hạn sau Caro::kInviteTimeoutMs), gửi invite_received cho target, notice cho me.
    Q_UNUSED(socket);
    Q_UNUSED(me);
    Q_UNUSED(msg);
}

void CaroServer::handleInviteReply(QTcpSocket *socket, const QString &me, const QJsonObject &msg) {
    // TODO(Minh): kiểm tra accept là bool, lời mời còn hạn; gửi invite_result cho người mời;
    // chấp nhận -> createMatch(from, me).
    Q_UNUSED(socket);
    Q_UNUSED(me);
    Q_UNUSED(msg);
}

void CaroServer::expireInvites(qint64 now) {
    // TODO(Minh): xóa lời mời hết hạn hoặc khi một bên đã bận/thoát; báo người mời.
    Q_UNUSED(now);
}

void CaroServer::createMatch(const QString &x, const QString &o) {
    // TODO(Minh): tạo Room mới (id = nextRoomId_++), gán roomId cho 2 session,
    // deadline = now + Caro::kTurnSeconds; log MATCH_START; broadcastSnapshot/Users/Rooms.
    Q_UNUSED(x);
    Q_UNUSED(o);
}
