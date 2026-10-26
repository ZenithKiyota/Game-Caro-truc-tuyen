// Phụ trách: Trần Đức Toàn — khung giao diện, kết nối TCP, framing, auto-reconnect, nhật ký Client.
// MainForm là partial class, chia theo panel/người phụ trách (xem PHAN_CONG.md).
// Giao diện được dựng bằng code để các thành viên tránh conflict file Designer.
// Dữ liệu mạng nhận trên luồng nền; phải BeginInvoke về luồng UI trước khi gọi EventMessage().
// Control trong panel khác có thể null khi panel chưa hoàn thiện, nên các handler cần kiểm tra trước khi dùng.
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm : Form
{
    // ---------- Toàn: kết nối, framing, reconnect và trạng thái Client ----------
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly object _sendLock = new();
    private readonly System.Windows.Forms.Timer _reconnectTimer;
    private string _myName = "";
    private string _token = "";
    private bool _wantConnection;
    private bool _joined;
    private bool _connecting;
    private bool _stopping;
    private TextBox? _hostInput, _nameInput;
    private NumericUpDown? _portInput;
    private Label? _connectionLabel;
    private Button? _connectionButton;
    private TextBox? _logText;

    private static readonly Regex ValidName = new("^[A-Za-z0-9_]{3,20}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public MainForm()
    {
        BuildUi();
        // TODO(Toàn): đã hoàn thiện bộ hẹn giờ thử kết nối lại mỗi 2 giây khi người dùng vẫn muốn kết nối.
        _reconnectTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _reconnectTimer.Tick += async (_, _) =>
        {
            if (_wantConnection && !_connecting && _client is null)
                await StartConnectionAsync();
        };
        _reconnectTimer.Start();
        FormClosing += (_, _) =>
        {
            _stopping = true;
            _wantConnection = false;
            _reconnectTimer.Stop();
            CloseConnection();
        };
    }

    private void BuildUi()
    {
        Text = "Caro Online | UDM_16";
        Size = new Size(1320, 810);
        MinimumSize = new Size(1000, 650);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        // TODO(Toàn): thanh kết nối IP/Port/tên, nút kết nối và trạng thái đã được dựng tại đây.
        var connectionBar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Padding = new Padding(0, 0, 0, 8) };
        connectionBar.Controls.Add(new Label { Text = "IP máy chủ:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        _hostInput = new TextBox { Width = 150, Text = "127.0.0.1" };
        connectionBar.Controls.Add(_hostInput);
        connectionBar.Controls.Add(new Label { Text = "Cổng:", AutoSize = true, Margin = new Padding(8, 8, 3, 3) });
        _portInput = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = Protocol.DefaultPort, Width = 85 };
        connectionBar.Controls.Add(_portInput);
        connectionBar.Controls.Add(new Label { Text = "Tên:", AutoSize = true, Margin = new Padding(8, 8, 3, 3) });
        _nameInput = new TextBox { Width = 130, MaxLength = 20 };
        connectionBar.Controls.Add(_nameInput);
        _connectionButton = new Button { Text = "Kết nối", AutoSize = true };
        _connectionButton.Click += (_, _) => StartOrStop();
        connectionBar.Controls.Add(_connectionButton);
        _connectionLabel = new Label { Text = "Chưa kết nối", AutoSize = true, Margin = new Padding(10, 8, 3, 3) };
        connectionBar.Controls.Add(_connectionLabel);
        root.Controls.Add(connectionBar, 0, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        root.Controls.Add(body, 0, 1);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        left.Controls.Add(BuildLobbyPanel(), 0, 0); // Minh: sảnh, danh sách online và lời mời.
        left.Controls.Add(BuildRoomsPanel(), 0, 1); // Tú: danh sách phòng và thao tác khán giả.
        body.Controls.Add(left, 0, 0);
        body.Controls.Add(BuildGamePanel(), 1, 0); // Tiến: bàn cờ và trạng thái trận.

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        right.Controls.Add(BuildHistoryPanel(), 0, 0); // Phát: lịch sử trận.
        var logGroup = new GroupBox { Text = "Nhật ký Client", Dock = DockStyle.Fill, Padding = new Padding(8) };
        _logText = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        logGroup.Controls.Add(_logText);
        right.Controls.Add(logGroup, 0, 1);
        body.Controls.Add(right, 2, 0);
        RefreshConnectionUi();
    }

    private void WriteLog(string message)
    {
        // TODO(Toàn): ghi nhật ký có thời gian; nếu được gọi từ luồng mạng thì chuyển về luồng UI.
        if (_logText is null || _logText.IsDisposed) return;
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
        if (_logText.InvokeRequired)
        {
            if (!_logText.IsHandleCreated) return;
            try { _logText.BeginInvoke(new Action(() => AppendLog(line))); }
            catch (InvalidOperationException) { }
            return;
        }
        AppendLog(line);
    }

    private void AppendLog(string line)
    {
        if (_logText is null || _logText.IsDisposed) return;
        _logText.AppendText(line);
    }

    private void StartOrStop()
    {
        // TODO(Toàn): kiểm tra tên, nạp token đã lưu và bắt đầu/dừng kết nối theo lựa chọn người dùng.
        if (_wantConnection)
        {
            _wantConnection = false;
            _joined = false;
            CloseConnection();
            WriteLog("Đã ngắt kết nối theo yêu cầu.");
            RefreshConnectionUi();
            return;
        }

        string name = _nameInput?.Text.Trim() ?? "";
        if (!ValidName.IsMatch(name))
        {
            MessageBox.Show(this, "Tên phải gồm 3–20 ký tự: chữ cái tiếng Anh, chữ số hoặc dấu gạch dưới.", "Tên không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!TryResolveHost(_hostInput?.Text.Trim() ?? "", out _, out string error))
        {
            MessageBox.Show(this, error, "Địa chỉ máy chủ không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _myName = name;
        _token = LoadToken(_myName);
        _wantConnection = true;
        WriteLog($"Bắt đầu kết nối tới {_hostInput?.Text}:{_portInput?.Value}.");
        RefreshConnectionUi();
        _ = StartConnectionAsync();
    }

    private async Task StartConnectionAsync()
    {
        // TODO(Toàn): kết nối tới IP/Port nhập trên giao diện, gửi hello và khởi động vòng nhận bất đồng bộ.
        if (!_wantConnection || _connecting || _stopping) return;
        _connecting = true;
        RefreshConnectionUi();
        TcpClient? candidate = null;
        try
        {
            string hostText = _hostInput?.Text.Trim() ?? "";
            if (!TryResolveHost(hostText, out IPAddress? address, out string hostError))
                throw new InvalidOperationException(hostError);
            int port = (int)(_portInput?.Value ?? Protocol.DefaultPort);
            candidate = new TcpClient(address!.AddressFamily) { NoDelay = true };
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await candidate.ConnectAsync(address!, port, timeout.Token);
            if (!_wantConnection || _stopping) return;

            _client = candidate;
            _stream = candidate.GetStream();
            candidate = null;
            _joined = false;
            RefreshConnectionUi("Đã kết nối TCP, đang đăng nhập...");
            WriteLog("Đã mở kết nối TCP; gửi yêu cầu đăng nhập.");
            SendMessage(new JsonObject
            {
                ["type"] = Msg.Hello,
                ["name"] = _myName,
                ["token"] = _token
            });
            await ReceiveLoopAsync();
        }
        catch (OperationCanceledException)
        {
            if (_wantConnection) WriteLog("Kết nối quá thời gian chờ (8 giây).");
        }
        catch (Exception ex) when (ex is SocketException or IOException or InvalidOperationException or ObjectDisposedException or ArgumentException)
        {
            if (_wantConnection) WriteLog($"Kết nối thất bại: {ex.Message}");
        }
        finally
        {
            candidate?.Dispose();
            if (_client is not null) CloseConnection();
            _joined = false;
            _connecting = false;
            if (!_stopping)
            {
                RefreshConnectionUi(_wantConnection ? "Mất kết nối; sẽ thử lại..." : null);
                if (_wantConnection) WriteLog("Sẽ tự thử kết nối lại sau 2 giây.");
            }
        }
    }

    private async Task ReceiveLoopAsync()
    {
        // TODO(Toàn): gom buffer TCP, tách frame NDJSON theo newline, parse JSON rồi chuyển sự kiện về UI.
        NetworkStream stream = _stream ?? throw new IOException("Luồng mạng chưa sẵn sàng.");
        byte[] readBuffer = new byte[4096];
        var frame = new List<byte>();
        while (_wantConnection && !_stopping)
        {
            int count = await stream.ReadAsync(readBuffer.AsMemory());
            if (count == 0) break;
            for (int i = 0; i < count; i++)
            {
                byte value = readBuffer[i];
                if (value == (byte)'\n')
                {
                    if (frame.Count == 0) continue;
                    string json = Encoding.UTF8.GetString(frame.ToArray());
                    frame.Clear();
                    try
                    {
                        if (JsonNode.Parse(json) is JsonObject message)
                        {
                            if (IsHandleCreated && !IsDisposed)
                                BeginInvoke(new Action(() => EventMessage(message)));
                        }
                        else WriteLog("Bỏ qua phản hồi không phải JSON object.");
                    }
                    catch (JsonException)
                    {
                        WriteLog("Bỏ qua một phản hồi JSON không hợp lệ.");
                    }
                    continue;
                }

                frame.Add(value);
                if (frame.Count > Protocol.MaxFrameBytes)
                    throw new IOException($"Phản hồi vượt giới hạn {Protocol.MaxFrameBytes} byte.");
            }
        }
        if (frame.Count > 0) WriteLog("Kết nối đóng khi một thông điệp còn chưa hoàn chỉnh; đã bỏ phần dữ liệu dang dở.");
        if (_wantConnection) WriteLog("Kết nối tới Server đã đóng.");
    }

    private void RefreshConnectionUi(string? statusOverride = null)
    {
        // TODO(Toàn): đồng bộ nhãn trạng thái, nút và khả năng sửa thông tin kết nối.
        if (InvokeRequired)
        {
            if (!IsHandleCreated || IsDisposed) return;
            try { BeginInvoke(new Action(() => RefreshConnectionUi(statusOverride))); }
            catch (InvalidOperationException) { }
            return;
        }
        if (_connectionLabel is null || _connectionButton is null) return;
        bool connected = _client?.Connected == true;
        _connectionLabel.Text = statusOverride ?? (connected ? (_joined ? "Đã kết nối và đăng nhập" : "Đang đăng nhập...") : (_wantConnection ? (_connecting ? "Đang kết nối..." : "Đang chờ kết nối lại...") : "Chưa kết nối"));
        _connectionButton.Text = _wantConnection ? "Ngắt kết nối" : "Kết nối";
        bool editable = !_wantConnection;
        if (_hostInput is not null) _hostInput.Enabled = editable;
        if (_portInput is not null) _portInput.Enabled = editable;
        if (_nameInput is not null) _nameInput.Enabled = editable;
    }

    private void SendMessage(JsonObject msg)
    {
        // TODO(Toàn): gửi JSON UTF-8 dạng NDJSON, kiểm tra kết nối và giới hạn kích thước gói.
        NetworkStream? stream = _stream;
        if (stream is null || _client?.Connected != true)
        {
            WriteLog("Chưa kết nối Server; không gửi được yêu cầu.");
            return;
        }
        try
        {
            byte[] json = Encoding.UTF8.GetBytes(msg.ToJsonString(JsonOptions));
            if (json.Length > Protocol.MaxFrameBytes) throw new IOException("Thông điệp gửi vượt giới hạn 64 KiB.");
            byte[] frame = new byte[json.Length + 1];
            Buffer.BlockCopy(json, 0, frame, 0, json.Length);
            frame[^1] = (byte)'\n';
            lock (_sendLock) stream.Write(frame, 0, frame.Length);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            WriteLog($"Gửi thông điệp thất bại: {ex.Message}");
            CloseConnection();
        }
    }

    private void EventMessage(JsonObject msg)
    {
        // TODO(Toàn): phân phối message Server tới đúng handler của từng thành viên/panel.
        string? type = msg["type"]?.GetValue<string>();
        switch (type)
        {
            // Toàn xử lý kết nối, đăng nhập và thông báo chung.
            case Msg.Welcome: OnWelcome(msg); break;
            case Msg.Error: WriteLog($"Lỗi từ Server: {msg["message"]?.GetValue<string>() ?? "Không có nội dung"}"); break;
            case Msg.Notice: WriteLog(msg["message"]?.GetValue<string>() ?? "Thông báo từ Server."); break;
            // Minh phụ trách danh sách người chơi và lời mời.
            case Msg.Users: OnUsers(msg); break;
            case Msg.InviteReceived: OnInviteReceived(msg); break;
            case Msg.InviteResult: OnInviteResult(msg); break;
            // Tiến phụ trách snapshot, bàn cờ và trạng thái trận.
            case Msg.Snapshot: ApplySnapshot(msg); break;
            // Tú phụ trách danh sách phòng và trạng thái khán giả.
            case Msg.Rooms: OnRooms(msg); break;
            case Msg.LeftRoom: OnLeftRoom(); break;
            // Phát phụ trách lịch sử.
            case Msg.HistoryResult: OnHistory(msg); break;
            default: WriteLog($"Server gửi loại thông điệp không nhận diện: {type ?? "(thiếu type)"}."); break;
        }
    }

    private void OnWelcome(JsonObject msg)
    {
        // TODO(Toàn): xác nhận đăng nhập, lưu token reconnect và tải lại các danh sách ban đầu.
        string receivedToken = msg["token"]?.GetValue<string>() ?? "";
        if (receivedToken.Length == 0)
        {
            WriteLog("Server gửi welcome nhưng thiếu token; không thể khôi phục phiên.");
            return;
        }
        _token = receivedToken;
        _joined = true;
        SaveToken(_myName, _token);
        bool resumed = msg["resumed"]?.GetValue<bool>() ?? false;
        WriteLog(resumed ? "Đăng nhập thành công; phiên cũ đã được khôi phục." : "Đăng nhập thành công.");
        RefreshConnectionUi();
        RefreshLists();
    }

    private void RefreshLists()
    {
        // TODO(Toàn): sau đăng nhập yêu cầu danh sách người chơi, phòng và lịch sử.
        SendMessage(new JsonObject { ["type"] = Msg.ListUsers });
        SendMessage(new JsonObject { ["type"] = Msg.ListRooms });
        SendMessage(new JsonObject { ["type"] = Msg.History });
    }

    private static bool TryResolveHost(string hostText, out IPAddress? address, out string error)
    {
        if (IPAddress.TryParse(hostText, out address)) { error = ""; return true; }
        try
        {
            address = Dns.GetHostAddresses(hostText).FirstOrDefault(a => a.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);
            if (address is not null) { error = ""; return true; }
        }
        catch (SocketException) { }
        error = "Không phân giải được địa chỉ IP hoặc tên máy chủ.";
        return false;
    }

    private static string TokenPath(string name) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CaroOnline", $"{name}.token");

    private static string LoadToken(string name)
    {
        try { return File.Exists(TokenPath(name)) ? File.ReadAllText(TokenPath(name)).Trim() : ""; }
        catch (IOException) { return ""; }
        catch (UnauthorizedAccessException) { return ""; }
    }

    private static void SaveToken(string name, string token)
    {
        try
        {
            string path = TokenPath(name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, token);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void CloseConnection()
    {
        lock (_sendLock)
        {
            try { _stream?.Close(); } catch (IOException) { }
            try { _client?.Close(); } catch (SocketException) { }
            _stream = null;
            _client = null;
        }
    }
}
