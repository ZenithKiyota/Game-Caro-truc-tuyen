// Phụ trách: Hứa Hồng Tiến — hiển thị trận đấu, lượt đi, đồng hồ đếm ngược, kết quả.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm
{
    private bool _active;
    private int _roomId = -1;
    private string _currentTurn = "";
    private Label? _gameLabel, _clockLabel;
    private BoardControl? _board;

    private Control BuildGamePanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _gameLabel = new Label { Text = "Chưa có trận. Mời người chơi để bắt đầu.", AutoSize = true };
        _clockLabel = new Label { Text = "Đồng hồ: --", AutoSize = true };
        _board = new BoardControl { Dock = DockStyle.Fill };
        _board.CellClicked += PlayAt;
        panel.Controls.Add(_gameLabel, 0, 0);
        panel.Controls.Add(_clockLabel, 0, 1);
        panel.Controls.Add(_board, 0, 2);
        return panel;
    }

    private void ApplySnapshot(JsonObject msg)
    {
         if (!TryGetInt(msg, "room", out var roomId) || msg["board"] is not JsonArray boardJson)
            return;

        var x = msg["x"]?.GetValue<string>() ?? "X";
        var o = msg["o"]?.GetValue<string>() ?? "O";
        _roomId = roomId;
        _active = msg["active"]?.GetValue<bool>() ?? false;
        _spectator = msg["spectators"] is JsonArray spectators &&
                     spectators.Any(item => item?.GetValue<string>() == _myName);
        _currentTurn = msg["turn"]?.GetValue<string>() ?? "";

        var board = boardJson.Select(item => item?.GetValue<string>() ?? "").ToArray();
        _board?.SetBoard(board,
            msg["lastRow"]?.GetValue<int>() ?? -1,
            msg["lastCol"]?.GetValue<int>() ?? -1);

        var paused = msg["paused"]?.GetValue<bool>() ?? false;
        var seconds = msg["secondsLeft"]?.GetValue<int>() ?? 0;
        var role = _spectator ? "khán giả" : _myName == x ? "X" : _myName == o ? "O" : "";
        if (_gameLabel is not null)
        {
            if (_active)
                _gameLabel.Text = $"Phòng #{roomId}: {x} (X) vs {o} (O) | Bạn: {role} | " +
                                  (paused ? "Tạm dừng" : $"Lượt {(_currentTurn == "X" ? x : o)}"));
            else
            {
                var winner = msg["winner"]?.GetValue<string>() ?? "";
                var reason = msg["reason"]?.GetValue<string>() ?? "Ván đấu đã kết thúc";
                _gameLabel.Text = string.IsNullOrWhiteSpace(winner)
                    ? $"Phòng #{roomId}: HÒA — {reason}"
                    : $"Phòng #{roomId}: {winner} thắng — {reason}";
            }
        }

        if (_clockLabel is not null)
            _clockLabel.Text = paused ? "Đồng hồ: tạm dừng" : _active ? $"Đồng hồ: {seconds}s" : "Đồng hồ: đã kết thúc";
    }

    private void PlayAt(int row, int col)
    {
        if (!_joined || !_active || _spectator || _roomId < 0) return;
        SendMessage(new JsonObject
        {
            ["type"] = Msg.Move,
            ["room"] = _roomId,
            ["row"] = row,
            ["col"] = col
        });
    }

    private static bool TryGetInt(JsonObject msg, string property, out int value)
    {
        value = 0;
        return msg[property] is JsonValue jsonValue && jsonValue.TryGetValue<int>(out value);
    }

    private void ResetGameView(string message)
    {
        _roomId = -1;
        _active = false;
        _spectator = false;
        _board?.ResetBoard();
        if (_gameLabel is not null) _gameLabel.Text = message;
        if (_clockLabel is not null) _clockLabel.Text = "Đồng hồ: --";
    }
}
