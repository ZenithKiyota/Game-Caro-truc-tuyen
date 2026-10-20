# Giao thức CaroOnline v1

TCP, UTF-8, mỗi gói = một JSON object + ký tự newline `\n` (NDJSON). Giới hạn tối đa 64 KiB/gói. TCP có thể chia/gộp gói tùy ý; phải đọc từ buffer tới newline. Gói chưa có `\n` chưa được coi là dữ liệu hoàn chỉnh.

Port mặc định `45454`, cấu hình được: Server `--address/-a`, `--port/-p`; Client nhập IP/Port trên giao diện.

## Client -> Server

| type | Trường | Ý nghĩa | Người phụ trách |
|---|---|---|---|
| `hello` | `name`, `token` | Đăng nhập. Tên 3–20 ký tự `a-z,A-Z,0-9,_`; `token` rỗng lần đầu, gửi lại token cũ khi reconnect | Phát |
| `list_users` | – | Xin danh sách online | Minh |
| `invite` | `target` | Thách đấu người đang rảnh | Minh |
| `invite_reply` | `from`, `accept` (bool) | Trả lời lời mời | Minh |
| `move` | `room`, `row`, `col` | Đặt quân (chỉ người đến lượt) | Tiến |
| `list_rooms` | – | Danh sách phòng đang đấu | Tú |
| `spectate` | `room` | Vào xem với vai trò khán giả | Tú |
| `leave_spectate` | – | Rời ghế khán giả | Tú |
| `history` | – | Lịch sử 50 trận gần nhất | Phát |

## Server -> Client

| type | Trường |
|---|---|
| `welcome` | `name`, `token`, `resumed` |
| `error` / `notice` | `message` |
| `users` | `users: [{name, busy}]` |
| `invite_received` | `from` |
| `invite_result` | `target`, `accepted` |
| `rooms` | `rooms: [{id, x, o, spectators, paused}]` |
| `snapshot` | `room, x, o, board` (15 chuỗi `X/O/.`), `turn` (`X`/`O`), `secondsLeft, paused, onlineX, onlineO, active, winner, reason, spectators, lastRow, lastCol` |
| `history_result` | `matches: [{room, x, o, winner, reason, endedAt}]` (gần nhất trước) |
| `left_room` | – |

Server quyết định toàn bộ lượt đi, thời gian, kết quả và quyền khán giả. Client không được gửi snapshot để thay đổi trạng thái. Mọi request sai định dạng/không có quyền đều nhận `error` với thông báo rõ ràng.

## Luật chơi (nhóm công bố)

- Bàn 15×15, X đi trước. **5 quân trở lên** liên tiếp (ngang/dọc/chéo) là thắng; không xét luật chặn hai đầu.
- Không còn ô trống: **hòa**.
- Mỗi lượt **30 giây**; hết giờ thì **người đang đến lượt bị xử thua**.
- Người chơi rớt mạng: ván **tạm dừng**, giữ thời gian còn lại của lượt; reconnect bằng cùng `token` trong **90 giây** thì chơi tiếp. Quá 90 giây: người mất kết nối bị xử thua.
- Lời mời hết hạn sau 60 giây. Một người chỉ ở một phòng (chơi hoặc xem) tại một thời điểm.
- Khán giả chỉ nhận `snapshot`; mọi `move` từ khán giả bị từ chối. Khán giả rời phòng không ảnh hưởng trận.
