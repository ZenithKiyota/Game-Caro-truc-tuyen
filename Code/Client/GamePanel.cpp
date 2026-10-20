// Phụ trách: Hứa Hồng Tiến — hiển thị trận đấu, lượt đi, đồng hồ đếm ngược, kết quả.
#include "MainWindow.h"
#include "BoardWidget.h"
#include "Protocol.h"
#include <QLabel>
#include <QVBoxLayout>

QWidget *MainWindow::buildGamePanel() {
    auto *panel = new QWidget();
    auto *layout = new QVBoxLayout(panel);
    gameLabel_ = new QLabel(QStringLiteral("Chưa có trận. Mời người chơi để bắt đầu."));
    clockLabel_ = new QLabel(QStringLiteral("Đồng hồ: --"));
    board_ = new BoardWidget();
    layout->addWidget(gameLabel_);
    layout->addWidget(clockLabel_);
    layout->addWidget(board_, 1);
    connect(board_, &BoardWidget::cellClicked, this, &MainWindow::playAt);
    return panel;
}

void MainWindow::applySnapshot(const QJsonObject &msg) {
    // TODO(Tiến): cập nhật roomId_, active_, spectator_ (tên mình khác X và O), currentTurn_,
    // bàn cờ + nước cuối, nhãn phòng/vai trò, đồng hồ còn N giây, tạm dừng, kết quả khi kết thúc.
    Q_UNUSED(msg);
}

void MainWindow::playAt(int row, int col) {
    // TODO(Tiến): chỉ gửi move{room,row,col} khi đã đăng nhập, trận active và không phải khán giả.
    Q_UNUSED(row);
    Q_UNUSED(col);
}

void MainWindow::resetGameView(const QString &message) {
    roomId_ = -1;
    active_ = false;
    spectator_ = false;
    if (board_) board_->resetBoard();
    if (gameLabel_) gameLabel_->setText(message);
    if (clockLabel_) clockLabel_->setText(QStringLiteral("Đồng hồ: --"));
}
