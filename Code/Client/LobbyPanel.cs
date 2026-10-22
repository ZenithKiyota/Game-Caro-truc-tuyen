// Phụ trách: Hồ Thanh Anh Minh — danh sách người chơi online, gửi/chấp nhận/từ chối lời mời.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm
{
    private ListBox? _usersList;

    private Control BuildLobbyPanel()
    {
        // TODO(Minh): GroupBox "Người chơi đang online" + _usersList + nút "Mời người được chọn".
        return new GroupBox { Text = "Người chơi đang online (TODO)", Dock = DockStyle.Fill };
    }

    private void OnUsers(JsonObject msg)
    {
        // TODO(Minh): hiển thị tên + (rảnh/đang bận), giữ lựa chọn cũ.
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
        // TODO(Minh): gửi invite{target} cho người đang chọn.
    }
}
