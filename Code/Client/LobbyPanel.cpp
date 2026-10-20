// Phụ trách: Hồ Thanh Anh Minh — danh sách người chơi online, gửi/chấp nhận/từ chối lời mời.
#include "MainWindow.h"
#include "Protocol.h"
#include <QGroupBox>

QWidget *MainWindow::buildLobbyPanel() {
    // TODO(Minh): QGroupBox "Người chơi đang online" + usersList_ + nút "Mời người được chọn".
    return new QGroupBox(QStringLiteral("Người chơi đang online (TODO)"));
}

void MainWindow::onUsers(const QJsonObject &msg) {
    // TODO(Minh): hiển thị tên + (rảnh/đang bận), giữ lựa chọn cũ.
    Q_UNUSED(msg);
}

void MainWindow::onInviteReceived(const QJsonObject &msg) {
    // TODO(Minh): hỏi Đồng ý/Từ chối (không chặn nhận dữ liệu mạng quá lâu), gửi invite_reply.
    Q_UNUSED(msg);
}

void MainWindow::onInviteResult(const QJsonObject &msg) {
    // TODO(Minh): log "X đã chấp nhận/từ chối lời mời".
    Q_UNUSED(msg);
}

void MainWindow::inviteSelected() {
    // TODO(Minh): gửi invite{target} cho người đang chọn.
}
