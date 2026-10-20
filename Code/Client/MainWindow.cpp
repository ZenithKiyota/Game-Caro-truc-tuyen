// Phụ trách: Trần Đức Toàn — khung giao diện, kết nối TCP, framing, auto-reconnect, nhật ký Client.
#include "MainWindow.h"
#include "Protocol.h"
#include <QHBoxLayout>
#include <QLabel>
#include <QVBoxLayout>
#include <QWidget>

MainWindow::MainWindow(QWidget *parent) : QMainWindow(parent) {
    buildUi();
    // TODO(Toàn): nối signal socket_ (connected -> gửi hello{name, token}, readyRead -> received,
    // disconnected/errorOccurred -> log + refreshConnectionUi); timer 2 giây tự reconnect khi wantConnection_.
}

void MainWindow::buildUi() {
    setWindowTitle(QStringLiteral("Caro Online | UDM_16"));
    resize(1320, 810);
    auto *root = new QWidget(this);
    setCentralWidget(root);
    auto *column = new QVBoxLayout(root);

    // TODO(Toàn): thanh trên cùng: IP, Port, Tên, nút Kết nối, nhãn trạng thái (đang kết nối / thành công / thất bại).
    column->addWidget(new QLabel(QStringLiteral("TODO: thanh kết nối")));

    auto *body = new QHBoxLayout();
    column->addLayout(body, 1);

    auto *left = new QVBoxLayout();
    left->addWidget(buildLobbyPanel(), 1); // Minh
    left->addWidget(buildRoomsPanel(), 1); // Tú
    body->addLayout(left, 1);

    body->addWidget(buildGamePanel(), 3); // Tiến

    auto *right = new QVBoxLayout();
    right->addWidget(buildHistoryPanel(), 3); // Phát
    // TODO(Toàn): khung "Nhật ký Client" (logText_).
    body->addLayout(right, 2);
}

void MainWindow::writeLog(const QString &message) {
    // TODO(Toàn): thêm dòng [HH:mm:ss] message vào logText_.
    Q_UNUSED(message);
}

QString MainWindow::settingsTokenKey() const {
    return QStringLiteral("tokens/") + myName_;
}

void MainWindow::startOrStop() {
    // TODO(Toàn): kiểm tra tên hợp lệ, đọc token đã lưu, bật/tắt wantConnection_.
}

void MainWindow::startConnection() {
    // TODO(Toàn): connectToHost(IP, Port nhập trên giao diện) — không hard-code.
}

void MainWindow::refreshConnectionUi() {
    // TODO(Toàn): cập nhật nhãn trạng thái, nút, enable/disable ô nhập.
}

void MainWindow::sendMessage(const QJsonObject &msg) {
    // TODO(Toàn): kiểm tra đã kết nối, ghi JSON compact + '\n'.
    Q_UNUSED(msg);
}

void MainWindow::received() {
    // TODO(Toàn): gom buffer, tách theo '\n', parse JSON, gọi eventMessage().
}

void MainWindow::eventMessage(const QJsonObject &msg) {
    // TODO(Toàn): định tuyến theo msg["type"]:
    //   welcome          -> onWelcome()          (Toàn)
    //   error / notice   -> writeLog()           (Toàn)
    //   users            -> onUsers()            (Minh)
    //   invite_received  -> onInviteReceived()   (Minh)
    //   invite_result    -> onInviteResult()     (Minh)
    //   snapshot         -> applySnapshot()      (Tiến)
    //   rooms            -> onRooms()            (Tú)
    //   left_room        -> onLeftRoom()         (Tú)
    //   history_result   -> onHistory()          (Phát)
    Q_UNUSED(msg);
}

void MainWindow::onWelcome(const QJsonObject &msg) {
    // TODO(Toàn): joined_ = true, lưu token vào settings_ để reconnect, refreshLists().
    Q_UNUSED(msg);
}

void MainWindow::refreshLists() {
    // TODO(Toàn): gửi list_users, list_rooms, history.
}
