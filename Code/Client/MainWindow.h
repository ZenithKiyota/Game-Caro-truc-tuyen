#pragma once
// Một class MainWindow, phần định nghĩa chia theo panel/người phụ trách (xem PHAN_CONG.md).
// Mỗi panel tự dựng widget của mình trong build...Panel() và xử lý message của mình.
// Con trỏ widget có thể là nullptr khi panel chưa làm xong -> luôn kiểm tra trước khi dùng.

#include <QJsonObject>
#include <QMainWindow>
#include <QSettings>
#include <QTcpSocket>

class QLineEdit;
class QSpinBox;
class QLabel;
class QPushButton;
class QListWidget;
class QTableWidget;
class QTextEdit;
class BoardWidget;

class MainWindow : public QMainWindow {
    Q_OBJECT
public:
    explicit MainWindow(QWidget *parent = nullptr);

private:
    // ---------- Toàn: MainWindow.cpp — khung, kết nối, log ----------
    QTcpSocket socket_;
    QByteArray incoming_;
    QSettings settings_{QStringLiteral("UDM16"), QStringLiteral("CaroOnline")};
    QString myName_;
    QString token_;
    bool wantConnection_ = false;
    bool joined_ = false;
    QLineEdit *hostInput_ = nullptr, *nameInput_ = nullptr;
    QSpinBox *portInput_ = nullptr;
    QLabel *connectionLabel_ = nullptr;
    QPushButton *connectionButton_ = nullptr;
    QTextEdit *logText_ = nullptr;

    void buildUi();
    void startOrStop();
    void startConnection();
    void refreshConnectionUi();
    void sendMessage(const QJsonObject &msg);
    void received();
    void eventMessage(const QJsonObject &msg);
    void onWelcome(const QJsonObject &msg);
    void writeLog(const QString &message);
    void refreshLists();
    QString settingsTokenKey() const;

    // ---------- Minh: LobbyPanel.cpp — online & lời mời ----------
    QListWidget *usersList_ = nullptr;

    QWidget *buildLobbyPanel();
    void onUsers(const QJsonObject &msg);
    void onInviteReceived(const QJsonObject &msg);
    void onInviteResult(const QJsonObject &msg);
    void inviteSelected();

    // ---------- Tiến: GamePanel.cpp — bàn cờ & đồng hồ ----------
    bool active_ = false;
    int roomId_ = -1;
    QString currentTurn_;
    QLabel *gameLabel_ = nullptr, *clockLabel_ = nullptr;
    BoardWidget *board_ = nullptr;

    QWidget *buildGamePanel();
    void applySnapshot(const QJsonObject &msg);
    void playAt(int row, int col);
    void resetGameView(const QString &message);

    // ---------- Tú: RoomsPanel.cpp — phòng & khán giả ----------
    bool spectator_ = false;
    QListWidget *roomsList_ = nullptr;
    QPushButton *leaveButton_ = nullptr;

    QWidget *buildRoomsPanel();
    void onRooms(const QJsonObject &msg);
    void onLeftRoom();
    void spectateSelected();
    void leaveSpectate();

    // ---------- Phát: HistoryPanel.cpp — lịch sử ----------
    QTableWidget *historyTable_ = nullptr;

    QWidget *buildHistoryPanel();
    void onHistory(const QJsonObject &msg);
};
