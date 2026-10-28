// Phụ trách: Hồ Thanh Anh Minh — danh sách người chơi online, gửi/chấp nhận/từ chối lời mời.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm
{
    private ListBox? _usersList;

    private sealed record UserItem(string Name, bool Busy, bool IsMe)
    {
        public override string ToString() =>
            $"{Name}{(IsMe ? " (bạn)" : "")} — {(Busy ? "đang bận" : "rảnh")}";
    }

    private Control BuildLobbyPanel()
    {
        var group = new GroupBox { Text = "Người chơi đang online", Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(6) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        _usersList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        _usersList.DoubleClick += (_, _) => InviteSelected();
        var inviteButton = new Button { Text = "Mời người được chọn", Dock = DockStyle.Fill };
        inviteButton.Click += (_, _) => InviteSelected();

        layout.Controls.Add(_usersList, 0, 0);
        layout.Controls.Add(inviteButton, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private void OnUsers(JsonObject msg)
    {
        if (_usersList is null) return;
        string? previous = (_usersList.SelectedItem as UserItem)?.Name;

        _usersList.BeginUpdate();
        _usersList.Items.Clear();
        if (msg["users"] is JsonArray users)
        {
            foreach (JsonNode? node in users)
            {
                if (node is not JsonObject user
                    || user["name"] is not JsonValue nameValue
                    || !nameValue.TryGetValue(out string? name)
                    || string.IsNullOrEmpty(name))
                    continue;
                bool busy = user["busy"] is JsonValue busyValue && busyValue.TryGetValue(out bool b) && b;
                var item = new UserItem(name, busy, name == _myName);
                _usersList.Items.Add(item);
                if (name == previous) _usersList.SelectedItem = item;
            }
        }
        _usersList.EndUpdate();
    }

    private void OnInviteReceived(JsonObject msg)
    {
        // TODO(Minh): hỏi Đồng ý/Từ chối, gửi invite_reply{from, accept}.
    }

    private void OnInviteResult(JsonObject msg)
    {
        // TODO(Minh): WriteLog "X đã chấp nhận/từ chối lời mời".
    }

    private void InviteSelected()
    {
        if (!_joined)
        {
            WriteLog("Cần đăng nhập trước khi mời người chơi.");
            return;
        }
        if (_usersList?.SelectedItem is not UserItem target)
        {
            MessageBox.Show(this, "Hãy chọn một người chơi trong danh sách.", "Chưa chọn người chơi",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (target.IsMe)
        {
            WriteLog("Không thể tự mời chính mình.");
            return;
        }
        if (target.Busy)
        {
            WriteLog($"{target.Name} đang bận, hãy mời người khác.");
            return;
        }
        SendMessage(new JsonObject { ["type"] = Msg.Invite, ["target"] = target.Name });
        WriteLog($"Đã gửi lời mời tới {target.Name}.");
    }
}
