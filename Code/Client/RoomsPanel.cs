// Phụ trách: Nguyễn Tuấn Tú — danh sách phòng đang đấu, vào xem / rời ghế khán giả.
using System.Text.Json.Nodes;
using CaroOnline.Shared;

namespace CaroOnline.Client;

public sealed partial class MainForm
{
    private bool _spectator;
    private ListBox? _roomsList;
    private Button? _leaveButton;

    private Control BuildRoomsPanel()
    {
        // TODO(Tú): GroupBox "Phòng đang đấu" + _roomsList + nút "Vào xem" + _leaveButton "Rời ghế khán giả".
        return new GroupBox { Text = "Phòng đang đấu (TODO)", Dock = DockStyle.Fill };
    }

    private void OnRooms(JsonObject msg)
    {
        // TODO(Tú): hiển thị "#id X vs O | Xem: n | Tạm dừng", giữ lựa chọn cũ.
    }

    private void OnLeftRoom()
    {
        ResetGameView("Đã rời phòng khán giả.");
        RefreshConnectionUi();
    }

    private void SpectateSelected()
    {
        // TODO(Tú): gửi spectate{room} cho phòng đang chọn.
    }

    private void LeaveSpectate()
    {
        // TODO(Tú): chỉ khi _spectator -> gửi leave_spectate.
    }
}
