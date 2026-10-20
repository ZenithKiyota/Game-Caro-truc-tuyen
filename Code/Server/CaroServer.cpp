// Phụ trách: Nguyễn Đức Huy — lõi TCP, framing NDJSON, điều phối message, log, clock.
#include "CaroServer.h"
#include "Protocol.h"
#include <QDateTime>

CaroServer::CaroServer(QObject *parent) : QObject(parent) {
    connect(&listener_, &QTcpServer::newConnection, this, &CaroServer::acceptConnection);
    connect(&clock_, &QTimer::timeout, this, &CaroServer::clockTick);
    clock_.setInterval(250);
}

CaroServer::~CaroServer() {
    // TODO(Huy): dừng clock, đóng listener, đóng database_ và removeDatabase.
}

qint64 CaroServer::nowMs() { return QDateTime::currentMSecsSinceEpoch(); }

QString CaroServer::timestamp(qint64 ms) {
    return QDateTime::fromMSecsSinceEpoch(ms, Qt::UTC).toString(Qt::ISODate);
}

void CaroServer::log(const QString &message) const {
    // TODO(Huy): ghi "thời gian | sự kiện" ra stdout và file server.log (AppDataLocation).
    // Không ghi token/dữ liệu bí mật vào log.
    Q_UNUSED(message);
}

bool CaroServer::start(const QString &address, quint16 port) {
    // TODO(Huy): openDatabase() (Phát), listen(address, port), log, clock_.start().
    Q_UNUSED(address);
    Q_UNUSED(port);
    return false;
}

void CaroServer::acceptConnection() {
    // TODO(Huy): nhận socket mới, nối readyRead -> receive, disconnected -> disconnected() (Phát) + deleteLater.
}

void CaroServer::receive(QTcpSocket *socket) {
    // TODO(Huy): gom buffer, tách theo '\n', chặn gói > Caro::kMaxFrameBytes,
    // JSON sai -> error("JSON không hợp lệ."), JSON đúng -> process().
    Q_UNUSED(socket);
}

QString CaroServer::userOf(QTcpSocket *socket) const {
    // TODO(Huy): tìm tên người dùng đang gắn với socket, rỗng nếu chưa đăng nhập.
    Q_UNUSED(socket);
    return QString();
}

void CaroServer::process(QTcpSocket *socket, const QJsonObject &msg) {
    // TODO(Huy): định tuyến theo msg["type"]:
    //   hello          -> hello()               (Phát)
    //   (chưa đăng nhập -> error "Cần đăng nhập trước.")
    //   list_users     -> broadcastUsers()      (Minh)
    //   invite         -> handleInvite()        (Minh)
    //   invite_reply   -> handleInviteReply()   (Minh)
    //   move           -> handleMove()          (Tiến)
    //   list_rooms     -> broadcastRooms(socket)(Tú)
    //   spectate       -> handleSpectate()      (Tú)
    //   leave_spectate -> handleLeaveSpectate() (Tú)
    //   history        -> sendHistory()         (Phát)
    //   khác           -> error "Loại yêu cầu không được hỗ trợ."
    Q_UNUSED(socket);
    Q_UNUSED(msg);
}

void CaroServer::send(QTcpSocket *socket, const QJsonObject &msg) const {
    // TODO(Huy): kiểm tra socket còn kết nối, ghi JSON compact + '\n'.
    Q_UNUSED(socket);
    Q_UNUSED(msg);
}

void CaroServer::sendTo(const QString &user, const QJsonObject &msg) const {
    const auto it = sessions_.constFind(user);
    if (it != sessions_.cend()) send(it->socket, msg);
}

void CaroServer::error(QTcpSocket *socket, const QString &message) const {
    send(socket, QJsonObject{{QStringLiteral("type"), QString(Caro::Msg::Error)},
                             {QStringLiteral("message"), message}});
}

void CaroServer::notice(const QString &user, const QString &message) const {
    sendTo(user, QJsonObject{{QStringLiteral("type"), QString(Caro::Msg::Notice)},
                             {QStringLiteral("message"), message}});
}

void CaroServer::clockTick() {
    // TODO(Huy): mỗi 250 ms:
    //   expireInvites(now)                          (Minh)
    //   với từng phòng: xóa phòng đã kết thúc > 5 phút,
    //     checkReconnectGrace(room, now)            (Phát)
    //     checkTurnTimeout(room, now)               (Tiến) — gồm broadcast đồng hồ mỗi giây
    //   removeExpiredSessions(now)                  (Phát)
}
