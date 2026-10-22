// Phụ trách: Nguyễn An Phát — lưu & truy vấn lịch sử trận đấu bằng SQLite (Microsoft.Data.Sqlite).
using CaroOnline.Shared;

namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    private bool OpenDatabase()
    {
        // TODO(Phát): mở %APPDATA%/CaroServer/matches.sqlite, tạo bảng matches
        // (room_id, player_x, player_o, winner, reason, started_at, ended_at, moves_json),
        // đặt _nextRoomId = MAX(room_id) + 1. Lỗi -> Log và trả false.
        return true;
    }

    private void SaveMatch(Room room)
    {
        // TODO(Phát): INSERT kết quả trận bằng tham số ($x, $o, ...), Log nếu lỗi.
    }

    private void SendHistory(string user)
    {
        // TODO(Phát): SELECT 50 trận gần nhất, gửi history_result{matches:[...]}.
    }
}
