#pragma once
// Hằng số và tên message dùng chung giữa Server và Client.
// Mọi thay đổi ở file này phải cập nhật PROTOCOL.md và báo cả nhóm (PR riêng).
#include <QString>
#include <QtGlobal>

namespace Caro {

constexpr quint16 kDefaultPort = 45454;
constexpr int kBoardSize = 15;
constexpr int kWinLength = 5;
constexpr int kTurnSeconds = 30;
constexpr qint64 kReconnectGraceMs = 90000;
constexpr qint64 kInviteTimeoutMs = 60000;
constexpr qint64 kMaxFrameBytes = 65536;

namespace Msg {
// Client -> Server
constexpr QLatin1String Hello("hello");
constexpr QLatin1String Invite("invite");
constexpr QLatin1String InviteReply("invite_reply");
constexpr QLatin1String Move("move");
constexpr QLatin1String ListUsers("list_users");
constexpr QLatin1String ListRooms("list_rooms");
constexpr QLatin1String History("history");
constexpr QLatin1String Spectate("spectate");
constexpr QLatin1String LeaveSpectate("leave_spectate");

// Server -> Client
constexpr QLatin1String Welcome("welcome");
constexpr QLatin1String Error("error");
constexpr QLatin1String Notice("notice");
constexpr QLatin1String Users("users");
constexpr QLatin1String InviteReceived("invite_received");
constexpr QLatin1String InviteResult("invite_result");
constexpr QLatin1String Rooms("rooms");
constexpr QLatin1String Snapshot("snapshot");
constexpr QLatin1String HistoryResult("history_result");
constexpr QLatin1String LeftRoom("left_room");
} // namespace Msg

} // namespace Caro
