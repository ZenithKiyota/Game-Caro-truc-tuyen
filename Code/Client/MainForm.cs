// Phụ trách: Trần Đức Toàn — khung giao diện, kết nối TCP, framing, auto-reconnect, nhật ký Client.
//
// MainForm là partial class, chia theo panel/người phụ trách (xem PHAN_CONG.md).
// Giao diện dựng bằng code (không dùng Designer) để tránh conflict file .Designer.cs.
// Dữ liệu mạng đến trên luồng nền -> phải BeginInvoke về luồng UI trước khi gọi EventMessage().
// Control của panel khác có thể là null khi panel đó chưa làm xong -> luôn kiểm tra trước khi dùng.
using System.Net.Sockets;
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm : Form
{
    // ---------- Toàn: kết nối & khung ----------
    private TcpClient? _client;
    private NetworkStream? _stream;
    private string _myName = "";
    private string _token = "";
    private bool _wantConnection;
    private bool _joined;
    private TextBox? _hostInput, _nameInput;
    private NumericUpDown? _portInput;
    private Label? _connectionLabel;
    private Button? _connectionButton;
    private TextBox? _logText;

    public MainForm()
    {
        BuildUi();
        // TODO(Toàn): Timer 2 giây tự kết nối lại khi _wantConnection mà đang mất kết nối.
    }

    private void BuildUi()
    {
        Text = "Caro Online | UDM_16";
        Size = new Size(1320, 810);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        // TODO(Toàn): thanh trên cùng: IP, Port, Tên, nút Kết nối, nhãn trạng thái (đang kết nối / thành công / thất bại).
        root.Controls.Add(new Label { Text = "TODO: thanh kết nối", AutoSize = true }, 0, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        root.Controls.Add(body, 0, 1);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        left.Controls.Add(BuildLobbyPanel(), 0, 0); // Minh
        left.Controls.Add(BuildRoomsPanel(), 0, 1); // Tú
        body.Controls.Add(left, 0, 0);

        body.Controls.Add(BuildGamePanel(), 1, 0); // Tiến

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        right.Controls.Add(BuildHistoryPanel(), 0, 0); // Phát
        // TODO(Toàn): khung "Nhật ký Client" (_logText) ở ô (0, 1).
        body.Controls.Add(right, 2, 0);
    }

    private void WriteLog(string message)
    {
        // TODO(Toàn): thêm dòng [HH:mm:ss] message vào _logText.
    }

    private void StartOrStop()
    {
        // TODO(Toàn): kiểm tra tên hợp lệ, đọc token đã lưu, bật/tắt _wantConnection.
    }

    private async Task StartConnectionAsync()
    {
        // TODO(Toàn): ConnectAsync(IP, Port nhập trên giao diện, có timeout) — không hard-code;
        // gửi hello{name, token}; chạy vòng đọc ReceiveLoopAsync. Không làm treo UI.
        await Task.CompletedTask;
    }

    private async Task ReceiveLoopAsync()
    {
        // TODO(Toàn): gom buffer, tách theo '\n', parse JsonNode, BeginInvoke(() => EventMessage(msg)).
        // Khi mất kết nối: cập nhật trạng thái, chờ timer kết nối lại.
        await Task.CompletedTask;
    }

    private void RefreshConnectionUi()
    {
        // TODO(Toàn): cập nhật nhãn trạng thái, nút, enable/disable ô nhập.
    }

    private void SendMessage(JsonObject msg)
    {
        // TODO(Toàn): kiểm tra đã kết nối, ghi JSON compact (UTF-8) + '\n'.
    }

    private void EventMessage(JsonObject msg)
    {
        // TODO(Toàn): định tuyến theo msg["type"]:
        //   welcome          -> OnWelcome()          (Toàn)
        //   error / notice   -> WriteLog()           (Toàn)
        //   users            -> OnUsers()            (Minh)
        //   invite_received  -> OnInviteReceived()   (Minh)
        //   invite_result    -> OnInviteResult()     (Minh)
        //   snapshot         -> ApplySnapshot()      (Tiến)
        //   rooms            -> OnRooms()            (Tú)
        //   left_room        -> OnLeftRoom()         (Tú)
        //   history_result   -> OnHistory()          (Phát)
    }

    private void OnWelcome(JsonObject msg)
    {
        // TODO(Toàn): _joined = true, lưu token (file trong %APPDATA%) để reconnect, RefreshLists().
    }

    private void RefreshLists()
    {
        // TODO(Toàn): gửi list_users, list_rooms, history.
    }
}
