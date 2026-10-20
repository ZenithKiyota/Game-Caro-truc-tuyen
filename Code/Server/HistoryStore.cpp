// Phụ trách: Nguyễn An Phát — lưu & truy vấn lịch sử trận đấu bằng SQLite.
#include "CaroServer.h"
#include "Protocol.h"

bool CaroServer::openDatabase() {
    // TODO(Phát): mở QSQLITE tại AppDataLocation/matches.sqlite, tạo bảng matches
    // (room_id, player_x, player_o, winner, reason, started_at, ended_at, moves_json),
    // đặt nextRoomId_ = MAX(room_id) + 1. Lỗi -> log và trả false.
    return true;
}

void CaroServer::saveMatch(const Room &room) {
    // TODO(Phát): INSERT kết quả trận (prepared statement), log nếu lỗi.
    Q_UNUSED(room);
}

void CaroServer::sendHistory(const QString &user) {
    // TODO(Phát): SELECT 50 trận gần nhất, gửi history_result{matches:[...]}.
    Q_UNUSED(user);
}
