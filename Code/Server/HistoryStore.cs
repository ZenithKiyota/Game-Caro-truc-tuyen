// Phụ trách: Nguyễn An Phát — lưu & truy vấn lịch sử trận đấu bằng SQLite (Microsoft.Data.Sqlite).
using CaroOnline.Shared;
using Microsoft.Data.Sqlite;
namespace CaroOnline.Server;

public sealed partial class CaroServer
{
    
private bool OpenDatabase()
{
    try
    {
        // Thư mục lưu database của Server.
        string folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "CaroServer");

        Directory.CreateDirectory(folder);

        string dbPath = Path.Combine(folder, "matches.sqlite");

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath
        }.ToString();

        _database = new SqliteConnection(connectionString);
        _database.Open();

        // Tạo bảng lưu lịch sử trận đấu nếu chưa có.
        using (var command = _database.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS matches (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    room_id INTEGER NOT NULL,
                    player_x TEXT NOT NULL,
                    player_o TEXT NOT NULL,
                    winner TEXT NOT NULL,
                    reason TEXT NOT NULL,
                    started_at TEXT NOT NULL,
                    ended_at TEXT NOT NULL,
                    moves_json TEXT NOT NULL
                );
                """;

            command.ExecuteNonQuery();
        }

        // Tiếp tục đánh số phòng từ dữ liệu đã lưu.
        using (var command = _database.CreateCommand())
        {
            command.CommandText =
                "SELECT COALESCE(MAX(room_id), 0) FROM matches;";

            long lastRoomId = Convert.ToInt64(
                command.ExecuteScalar());

            _nextRoomId = checked((int)lastRoomId + 1);
        }

        Log($"DATABASE OPEN: {dbPath}");
        return true;
    }
    catch (Exception ex)
    {
        Log($"DATABASE ERROR: {ex.Message}");

        _database?.Dispose();
        _database = null;

        return false;
    }
}


    
private void SaveMatch(Room room)
{
    if (_database is null)
    {
        Log($"SAVE MATCH ERROR: Database chưa được mở, phòng {room.Id}");
        return;
    }

    try
    {
        using var command = _database.CreateCommand();

        command.CommandText = """
            INSERT INTO matches (
                room_id,
                player_x,
                player_o,
                winner,
                reason,
                started_at,
                ended_at,
                moves_json
            )
            VALUES (
                $roomId,
                $playerX,
                $playerO,
                $winner,
                $reason,
                $startedAt,
                $endedAt,
                $movesJson
            );
            """;

        command.Parameters.AddWithValue("$roomId", room.Id);
        command.Parameters.AddWithValue("$playerX", room.X);
        command.Parameters.AddWithValue("$playerO", room.O);
        command.Parameters.AddWithValue("$winner", room.Winner);
        command.Parameters.AddWithValue("$reason", room.Reason);
        command.Parameters.AddWithValue(
            "$startedAt", Timestamp(room.StartedAt));
        command.Parameters.AddWithValue(
            "$endedAt", Timestamp(room.EndedAt));
        command.Parameters.AddWithValue(
            "$movesJson", room.Moves.ToJsonString());

        command.ExecuteNonQuery();

        Log($"SAVE MATCH: phòng {room.Id} | thắng: {room.Winner}");
    }
    catch (Exception ex)
    {
        Log($"SAVE MATCH ERROR: phòng {room.Id} | {ex.Message}");
    }
}


    private void SendHistory(string user)
    {
        // TODO(Phát): SELECT 50 trận gần nhất, gửi history_result{matches:[...]}.
    }
}
