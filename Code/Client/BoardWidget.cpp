// Phụ trách: Hứa Hồng Tiến — vẽ bàn cờ 15x15, đánh dấu nước cuối, chuyển click chuột thành (row, col).
#include "BoardWidget.h"
#include "Protocol.h"
#include <QMouseEvent>
#include <QPaintEvent>

BoardWidget::BoardWidget(QWidget *parent) : QWidget(parent) {
    resetBoard();
}

void BoardWidget::setBoard(const QStringList &board, int lastRow, int lastCol) {
    board_ = board;
    lastRow_ = lastRow;
    lastCol_ = lastCol;
    update();
}

void BoardWidget::resetBoard() {
    setBoard(QStringList(), -1, -1);
}

void BoardWidget::paintEvent(QPaintEvent *event) {
    // TODO(Tiến): vẽ lưới Caro::kBoardSize x kBoardSize, quân X/O, viền nước cuối.
    Q_UNUSED(event);
}

void BoardWidget::mousePressEvent(QMouseEvent *event) {
    // TODO(Tiến): đổi tọa độ chuột -> (row, col) rồi emit cellClicked(row, col).
    Q_UNUSED(event);
}
