#pragma once
// Một class CaroServer duy nhất, nhưng phần định nghĩa hàm được chia ra nhiều file .cpp
// theo người phụ trách (xem PHAN_CONG.md). Chỉ sửa khối của mình; cần thêm hàm/trường
// dùng chung thì mở PR nhỏ riêng và báo nhóm để tránh conflict.

#include <QHash>
#include <QJsonArray>
#include <QJsonObject>
#include <QObject>
#include <QPointer>
#include <QSqlDatabase>
#include <QStringList>
#include <QTcpServer>
#include <QTcpSocket>
#include <QTimer>
#include <QVector>

class CaroServer : public QObject {
    Q_OBJECT
public:
    explicit CaroServer(QObject *parent = nullptr);
    ~CaroServer() override;
    bool start(const QString &address, quint16 port);

private:
    // ---------- Trạng thái dùng chung ----------
    struct Session {
        QString name;
        QString token;
        QPointer<QTcpSocket> socket;
        qint64 offlineSince = 0;
        int roomId = -1;
        bool spectator = false;
    };
    struct Room {
        int id = 0;
        QString x, o;
        QVector<int> board = QVector<int>(225, 0); // 0 trống, 1 = X, 2 = O
        QStringList spectators;
        int turn = 1;
        int lastRow = -1, lastCol = -1;
        qint64 startedAt = 0, deadline = 0, endedAt = 0;
        qint64 pausedRemaining = 0;
        bool paused = false, active = true;
        int lastBroadcastSecond = -1;
        QString winner, reason;
        QJsonArray moves;
    };
    struct Invite {
        QString from, to;
        qint64 expiresAt = 0;
    };

    QTcpServer listener_;
    QHash<QTcpSocket *, QByteArray> input_;
    QHash<QString, Session> sessions_;
    QHash<int, Room> rooms_;
    QList<Invite> invites_;
    QTimer clock_;
    QSqlDatabase database_;
    int nextRoomId_ = 1;

    // ---------- Huy: CaroServer.cpp — lõi mạng, điều phối, log ----------
    static qint64 nowMs();
    static QString timestamp(qint64 ms);
    void log(const QString &message) const;
    void acceptConnection();
    void receive(QTcpSocket *socket);
    void process(QTcpSocket *socket, const QJsonObject &msg);
    QString userOf(QTcpSocket *socket) const;
    void send(QTcpSocket *socket, const QJsonObject &msg) const;
    void sendTo(const QString &user, const QJsonObject &msg) const;
    void error(QTcpSocket *socket, const QString &message) const;
    void notice(const QString &user, const QString &message) const;
    void clockTick();

    // ---------- Phát: SessionHandler.cpp — đăng nhập & reconnect ----------
    void hello(QTcpSocket *socket, const QJsonObject &msg);
    void disconnected(QTcpSocket *socket);
    void pauseIfNeeded(Room &room);
    bool checkReconnectGrace(Room &room, qint64 now); // true nếu trận đã bị kết thúc
    void removeExpiredSessions(qint64 now);

    // ---------- Phát: HistoryStore.cpp — SQLite ----------
    bool openDatabase();
    void saveMatch(const Room &room);
    void sendHistory(const QString &user);

    // ---------- Minh: LobbyHandler.cpp — online, lời mời, tạo phòng ----------
    bool userAvailable(const QString &name) const;
    void broadcastUsers() const;
    void handleInvite(QTcpSocket *socket, const QString &me, const QJsonObject &msg);
    void handleInviteReply(QTcpSocket *socket, const QString &me, const QJsonObject &msg);
    void expireInvites(qint64 now);
    void createMatch(const QString &x, const QString &o);

    // ---------- Tiến: GameHandler.cpp — luật chơi & đồng hồ ----------
    void handleMove(QTcpSocket *socket, const QString &me, const QJsonObject &msg);
    bool hasWinner(const Room &room, int row, int col, int color) const;
    bool isBoardFull(const Room &room) const;
    bool checkTurnTimeout(Room &room, qint64 now); // true nếu trận đã bị kết thúc
    void finish(Room &room, const QString &winner, const QString &reason);

    // ---------- Tú: SpectatorHandler.cpp — phòng & khán giả ----------
    void broadcastRooms(QTcpSocket *recipient = nullptr) const;
    void handleSpectate(QTcpSocket *socket, const QString &me, const QJsonObject &msg);
    void handleLeaveSpectate(QTcpSocket *socket, const QString &me);
    QJsonObject snapshot(const Room &room) const;
    void broadcastSnapshot(const Room &room) const;
};
