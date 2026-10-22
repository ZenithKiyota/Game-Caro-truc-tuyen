// Hằng số và tên message dùng chung giữa Server và Client.
// Mọi thay đổi ở file này phải cập nhật PROTOCOL.md và báo cả nhóm (PR riêng).
namespace CaroOnline.Shared;

public static class Protocol
{
    public const int DefaultPort = 45454;
    public const int BoardSize = 15;
    public const int WinLength = 5;
    public const int TurnSeconds = 30;
    public const long ReconnectGraceMs = 90_000;
    public const long InviteTimeoutMs = 60_000;
    public const int MaxFrameBytes = 65_536;
}

public static class Msg
{
    // Client -> Server
    public const string Hello = "hello";
    public const string Invite = "invite";
    public const string InviteReply = "invite_reply";
    public const string Move = "move";
    public const string ListUsers = "list_users";
    public const string ListRooms = "list_rooms";
    public const string History = "history";
    public const string Spectate = "spectate";
    public const string LeaveSpectate = "leave_spectate";

    // Server -> Client
    public const string Welcome = "welcome";
    public const string Error = "error";
    public const string Notice = "notice";
    public const string Users = "users";
    public const string InviteReceived = "invite_received";
    public const string InviteResult = "invite_result";
    public const string Rooms = "rooms";
    public const string Snapshot = "snapshot";
    public const string HistoryResult = "history_result";
    public const string LeftRoom = "left_room";
}
