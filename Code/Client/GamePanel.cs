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
        // TODO(Tiến): cập nhật _roomId, _active, _spectator (tên mình khác X và O), _currentTurn,
        // bàn cờ + nước cuối, nhãn phòng/vai trò, đồng hồ còn N giây, tạm dừng, kết quả khi kết thúc.
    }

    private void PlayAt(int row, int col)
    {
        // TODO(Tiến): chỉ gửi move{room,row,col} khi đã đăng nhập, trận active và không phải khán giả.
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
