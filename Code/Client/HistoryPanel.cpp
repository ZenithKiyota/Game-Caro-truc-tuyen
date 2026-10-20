// Phụ trách: Nguyễn An Phát — bảng lịch sử & kết quả các trận đấu.
#include "MainWindow.h"
#include "Protocol.h"
#include <QGroupBox>

QWidget *MainWindow::buildHistoryPanel() {
    // TODO(Phát): QGroupBox "Lịch sử trận" + historyTable_ (Phòng, X vs O, Thắng, Lý do, Kết thúc)
    // + nút "Tải lại lịch sử" gửi history.
    return new QGroupBox(QStringLiteral("Lịch sử trận (TODO)"));
}

void MainWindow::onHistory(const QJsonObject &msg) {
    // TODO(Phát): đổ history_result.matches vào historyTable_.
    Q_UNUSED(msg);
}
