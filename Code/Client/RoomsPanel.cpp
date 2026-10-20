// Phụ trách: Nguyễn Tuấn Tú — danh sách phòng đang đấu, vào xem / rời ghế khán giả.
#include "MainWindow.h"
#include "Protocol.h"
#include <QGroupBox>

QWidget *MainWindow::buildRoomsPanel() {
    // TODO(Tú): QGroupBox "Phòng đang đấu" + roomsList_ + nút "Vào xem" + leaveButton_ "Rời ghế khán giả".
    return new QGroupBox(QStringLiteral("Phòng đang đấu (TODO)"));
}

void MainWindow::onRooms(const QJsonObject &msg) {
    // TODO(Tú): hiển thị "#id X vs O | Xem: n | Tạm dừng", giữ lựa chọn cũ.
    Q_UNUSED(msg);
}

void MainWindow::onLeftRoom() {
    resetGameView(QStringLiteral("Đã rời phòng khán giả."));
    refreshConnectionUi();
}

void MainWindow::spectateSelected() {
    // TODO(Tú): gửi spectate{room} cho phòng đang chọn.
}

void MainWindow::leaveSpectate() {
    // TODO(Tú): chỉ khi spectator_ -> gửi leave_spectate.
}
