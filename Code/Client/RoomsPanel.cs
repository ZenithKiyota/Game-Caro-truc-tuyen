// Phụ trách: Nguyễn Tuấn Tú — danh sách phòng đang đấu, vào xem / rời ghế khán giả.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm
{
    private bool _spectator;
    private ListBox? _roomsList;
    private Button? _leaveButton;

    // Lớp phụ trợ để lưu thông tin từng phòng trong ListBox
    private sealed class RoomItem
    {
        public int Id { get; init; }
        public string DisplayText { get; init; } = "";
        public override string ToString() => DisplayText;
    }

    private Control BuildRoomsPanel()
    {
        var box = new GroupBox
        {
            Text = "Phòng đang đấu",
            Dock = DockStyle.Fill
        };

        // Danh sách hiển thị các phòng
        _roomsList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false
        };
        // Nhấp đúp chuột vào phòng để vào xem ngay
        _roomsList.DoubleClick += (s, e) => SpectateSelected();

        // Thanh nút bấm bên dưới
        var panelButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4)
        };

        var spectateButton = new Button
        {
            Text = "Vào xem",
            AutoSize = true
        };
        spectateButton.Click += (s, e) => SpectateSelected();

        _leaveButton = new Button
        {
            Text = "Rời ghế khán giả",
            AutoSize = true,
            Enabled = false // Chỉ bật khi đang ở trong phòng xem
        };
        _leaveButton.Click += (s, e) => LeaveSpectate();

        panelButtons.Controls.Add(spectateButton);
        panelButtons.Controls.Add(_leaveButton);

        // Lưu ý thứ tự: add ListBox trước rồi add panelButtons
        box.Controls.Add(_roomsList);
        box.Controls.Add(panelButtons);

        return box;
    }

    private void OnRooms(JsonObject msg)
    {
        if (_roomsList == null) return;

        // Lưu lại id phòng đang được chọn trước đó để không bị mất chọn khi danh sách cập nhật
        int? previousSelectedId = (_roomsList.SelectedItem as RoomItem)?.Id;

        _roomsList.BeginUpdate();
        _roomsList.Items.Clear();

        if (msg["rooms"] is JsonArray roomsArray)
        {
            RoomItem? itemToReselect = null;

            foreach (var node in roomsArray)
            {
                if (node is not JsonObject room) continue;

                int id = room["id"]?.GetValue<int>() ?? 0;
                string x = room["x"]?.GetValue<string>() ?? "?";
                string o = room["o"]?.GetValue<string>() ?? "?";
                int spectators = room["spectators"]?.GetValue<int>() ?? 0;
                bool paused = room["paused"]?.GetValue<bool>() ?? false;

                // Định dạng theo đúng yêu cầu: "#id X vs O | Xem: n | Tạm dừng"
                string text = $"#{id} {x} vs {o} | Xem: {spectators}" + (paused ? " | Tạm dừng" : "");

                var item = new RoomItem { Id = id, DisplayText = text };
                _roomsList.Items.Add(item);

                if (previousSelectedId.HasValue && id == previousSelectedId.Value)
                {
                    itemToReselect = item;
                }
            }

            if (itemToReselect != null)
            {
                _roomsList.SelectedItem = itemToReselect;
            }
        }

        _roomsList.EndUpdate();
    }

    private void OnLeftRoom()
    {
        _spectator = false;
        if (_leaveButton != null) _leaveButton.Enabled = false;

        ResetGameView("Đã rời phòng khán giả.");
        RefreshConnectionUi();
    }

    private void SpectateSelected()
    {
        if (_roomsList?.SelectedItem is not RoomItem selected)
        {
            MessageBox.Show("Vui lòng chọn một phòng trong danh sách để vào xem!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _spectator = true;
        if (_leaveButton != null) _leaveButton.Enabled = true;

        // Gửi gói tin spectate { room: id } lên Server
        SendMessage(new JsonObject
        {
            ["type"] = Msg.Spectate,
            ["room"] = selected.Id
        });
    }

    private void LeaveSpectate()
    {
        // Gửi gói tin leave_spectate lên Server
        SendMessage(new JsonObject
        {
            ["type"] = Msg.LeaveSpectate
        });
    }
}