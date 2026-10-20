// Phụ trách: Hứa Hồng Tiến — kiểm tra nước đi, xác định thắng/thua/hòa, giới hạn thời gian lượt.
#include "CaroServer.h"
#include "Protocol.h"

void CaroServer::handleMove(QTcpSocket *socket, const QString &me, const QJsonObject &msg) {
    // TODO(Tiến): kiểm tra kiểu dữ liệu room/row/col; me là người chơi (không phải khán giả)
    // của phòng; trận active, không paused, chưa hết giờ; đúng lượt; ô hợp lệ và trống.
    // Đặt quân, ghi moves, log; thắng -> finish; đầy bàn -> hòa; ngược lại đổi lượt + broadcastSnapshot.
    Q_UNUSED(socket);
    Q_UNUSED(me);
    Q_UNUSED(msg);
}

bool CaroServer::hasWinner(const Room &room, int row, int col, int color) const {
    // TODO(Tiến): đếm 4 hướng, >= Caro::kWinLength là thắng.
    Q_UNUSED(room);
    Q_UNUSED(row);
    Q_UNUSED(col);
    Q_UNUSED(color);
    return false;
}

bool CaroServer::isBoardFull(const Room &room) const {
    return !room.board.contains(0);
}

bool CaroServer::checkTurnTimeout(Room &room, qint64 now) {
    // TODO(Tiến): nếu now >= deadline -> finish(người còn lại, "Hết 30 giây lượt"), trả true;
    // ngược lại broadcastSnapshot khi số giây còn lại thay đổi (lastBroadcastSecond).
    Q_UNUSED(room);
    Q_UNUSED(now);
    return false;
}

void CaroServer::finish(Room &room, const QString &winner, const QString &reason) {
    // TODO(Tiến): active=false, ghi winner/reason/endedAt, saveMatch(room) (Phát),
    // broadcastSnapshot, trả roomId=-1 cho người chơi + khán giả, log MATCH_END,
    // broadcastUsers/Rooms, sendHistory cho mọi người trong phòng.
    Q_UNUSED(room);
    Q_UNUSED(winner);
    Q_UNUSED(reason);
}
