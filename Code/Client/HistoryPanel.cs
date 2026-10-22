// Phụ trách: Nguyễn An Phát — bảng lịch sử & kết quả các trận đấu.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm
{
    private DataGridView? _historyTable;

    private Control BuildHistoryPanel()
    {
        // TODO(Phát): GroupBox "Lịch sử trận" + _historyTable (Phòng, X vs O, Thắng, Lý do, Kết thúc)
        // + nút "Tải lại lịch sử" gửi history.
        return new GroupBox { Text = "Lịch sử trận (TODO)", Dock = DockStyle.Fill };
    }

    private void OnHistory(JsonObject msg)
    {
        // TODO(Phát): đổ history_result.matches vào _historyTable.
    }
}
