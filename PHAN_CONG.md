# Phân công nhóm – UDM_16 Game Caro trực tuyến

> Bản nháp phân công, nhóm thống nhất lại rồi mới push. Bản mẫu tham khảo: `NoHaji2266666666/CaroOnline-UDM16`.
> Nguyên tắc: **mỗi người làm trọn một tính năng cả Server lẫn Client** và chỉ sửa file mình sở hữu, nên 6 người làm song song mà ít conflict.

## 1. Bảng phân công

| # | Thành viên | Vai trò / tính năng | File sở hữu (Server) | File sở hữu (Client) | Tài liệu, kiểm thử |
|---|---|---|---|---|---|
| 1 | **Nguyễn Đức Huy** | Trưởng nhóm. Lõi Server: TCP, framing NDJSON, điều phối message, log, clock | `Code/Server/CaroServer.cpp`, `main.cpp`, `CMakeLists.txt` (gốc + Server) | – | Review/merge PR, stress & performance test (`Extra/tests/performance_*.py`), mục **Kiến trúc + Protocol** trong báo cáo |
| 2 | **Trần Đức Toàn** | Lõi Client: khung giao diện, kết nối IP/Port, trạng thái kết nối, auto-reconnect, nhật ký | – | `Code/Client/MainWindow.cpp`, `main.cpp`, `Client/CMakeLists.txt` | Mục **Thiết kế GUI**, **ghép file DOCX** báo cáo |
| 3 | **Hứa Hồng Tiến** | Luật chơi: kiểm tra nước đi, thắng/thua/hòa, giới hạn 30 giây/lượt | `Code/Server/GameHandler.cpp` | `GamePanel.cpp`, `BoardWidget.cpp/.h` | Mục **Luật chơi**, ghép **file test case** (theo mẫu sheet) |
| 4 | **Hồ Thanh Anh Minh** | Sảnh: danh sách online, gửi/chấp nhận/từ chối lời mời, tạo nhiều phòng đồng thời | `Code/Server/LobbyHandler.cpp` | `LobbyPanel.cpp` | Mục **Hướng dẫn chạy**, **README.md**, quay **video demo** |
| 5 | **Nguyễn Tuấn Tú** | Khán giả: danh sách phòng, vào xem giữa trận (nhận snapshot đầy đủ), phân quyền, rời phòng | `Code/Server/SpectatorHandler.cpp` | `RoomsPanel.cpp` | Mục **Khán giả & đồng bộ trạng thái**, làm **PPTX** |
| 6 | **Nguyễn An Phát** | Phiên & lịch sử: đăng nhập, token, ngắt kết nối, reconnect 90 giây, SQLite | `Code/Server/SessionHandler.cpp`, `HistoryStore.cpp` | `HistoryPanel.cpp` | Mục **Lưu trữ & reconnect**, **Hạn chế** của sản phẩm |

**File dùng chung** (`CaroServer.h`, `MainWindow.h`, `Code/Shared/*`): chỉ thêm khai báo trong khối của mình. Đổi protocol phải mở PR riêng và báo cả nhóm trước.

## 2. Yêu cầu đề bài → người chịu trách nhiệm

| Yêu cầu UDM_16 | Người |
|---|---|
| Client kết nối Server, xem danh sách người chơi online | Toàn (kết nối) + Minh (danh sách) |
| Gửi, chấp nhận, từ chối lời mời thách đấu | Minh |
| Nhiều trận đồng thời, quản lý trạng thái riêng từng phòng | Minh (tạo phòng) + Huy (điều phối) |
| Server kiểm tra nước đi, xác định thắng/thua/hòa | Tiến |
| Giới hạn thời gian mỗi lượt, luật hết giờ được công bố | Tiến (luật ghi trong `Code/Shared/PROTOCOL.md`) |
| Kết nối lại và tiếp tục trận trong thời gian cho phép | Phát (Server) + Toàn (Client tự kết nối lại) |
| Lưu lịch sử và kết quả trận | Phát |
| Xem danh sách trận đang diễn ra, vào xem với vai trò khán giả | Tú |
| Khán giả vào giữa trận nhận đủ trạng thái, cập nhật bàn cờ + thời gian realtime | Tú |
| Phân biệt người chơi/khán giả; khán giả không được gửi nước đi | Tú (Server từ chối) + Tiến (`handleMove` kiểm tra quyền) |
| Khán giả rời phòng không ảnh hưởng trận | Tú |

Yêu cầu chung của môn: log Server (Huy), timeout (Huy/Phát), validate dữ liệu và trả lỗi rõ ràng (ai sở hữu handler nào thì tự validate handler đó), GUI không treo và có trạng thái kết nối (Toàn).

## 3. Test case mỗi người tự viết và chạy

Ghi theo mẫu sheet test (No, Name/Screen, Description, Input/Scenario, Expected Result, Test Result 1–3, Stage, Proof). Mỗi case cần log, ảnh hoặc video làm bằng chứng.

| Người | Mã | Nội dung chính |
|---|---|---|
| Huy | TC_01–05 | Kết nối nhiều client; JSON sai; gói > 64 KiB; gói bị chia/gộp; Server không sập khi client ngắt đột ngột; **stress/performance ≥ 2 mức tải** (5/20/50 client) |
| Toàn | TC_06–09 | Đổi IP/Port; trạng thái đang kết nối/thành công/thất bại; Server tắt đột ngột thì Client không crash; Client tự kết nối lại |
| Tiến | TC_10–16 | Đánh sai lượt; ô đã có quân; ô ngoài bàn; thắng ngang/dọc/chéo; hòa; hết 30 giây |
| Minh | TC_17–22 | Danh sách online cập nhật; mời → chấp nhận; mời → từ chối; mời người đang bận; lời mời hết hạn 60 giây; 2 phòng chạy đồng thời |
| Tú | TC_23–28 | Danh sách phòng; khán giả vào giữa trận nhận snapshot; nhận cập nhật realtime; khán giả gửi `move` giả bằng script thì bị từ chối; rời phòng thì trận vẫn chạy; khán giả mất mạng |
| Phát | TC_29–34 | Tên không hợp lệ/trùng; reconnect trong 90 giây; quá 90 giây bị xử thua; token sai; lịch sử còn sau khi khởi động lại Server; xem lịch sử |

Script test tự động đặt ở `Extra/tests/`, mỗi người một file `test_<tính năng>.py`.

## 4. Thứ tự và tiến độ (bắt đầu 20/10/2026, chỉnh theo hạn nộp)

GV kiểm tra repo cuối mỗi tuần, nên **tuần nào mỗi người cũng phải có ít nhất 1 commit có nội dung**.

| Tuần | Mục tiêu | Ghi chú phụ thuộc |
|---|---|---|
| 1 (20–26/10) | Huy: `receive/send/process/log/start` chạy được. Toàn: kết nối + `sendMessage/received/eventMessage`. Phát: `hello` + `openDatabase`. Những người khác viết logic + test case nháp | **Huy, Toàn, Phát làm xong trước giữa tuần**, vì những phần khác cần lõi này để test thật |
| 2 (27/10–2/11) | Minh: mời + tạo phòng. Tiến: `handleMove` + `hasWinner`. Tú: `snapshot` + `broadcastRooms`. Các panel Client tương ứng | Sau tuần 2 phải chơi được 1 ván hoàn chỉnh |
| 3 (3–9/11) | Đồng hồ + hết giờ (Tiến), khán giả (Tú), reconnect + lịch sử (Phát), lời mời hết hạn (Minh) | Đủ toàn bộ chức năng bắt buộc |
| 4 (10–16/11) | Mọi người chạy test case của mình; Huy chạy stress/performance; sửa lỗi | Ghi kết quả + bằng chứng vào `Extra/` |
| 5 (17–23/11) | Báo cáo DOCX, PPTX, video demo, README hoàn chỉnh; test LAN nhiều máy | Demo phải có ít nhất 1 tình huống mất kết nối |

## 5. Quy trình Git

```bash
git clone https://github.com/ZenithKiyota/Game-Caro-truc-tuyen.git
git checkout -b feature/<nhánh-của-mình>
# ...code, build, test...
git add <file của mình>
git commit -m "Server: kiểm tra nước đi sai lượt và ô đã có quân"
git push -u origin feature/<nhánh-của-mình>
# Mở Pull Request vào main, Huy review rồi merge
```

| Người | Nhánh |
|---|---|
| Huy | `feature/server-core` |
| Toàn | `feature/client-core` |
| Tiến | `feature/game-rules` |
| Minh | `feature/lobby-invite` |
| Tú | `feature/spectator` |
| Phát | `feature/session-history` |

- Commit bằng **tài khoản GitHub cá nhân**: `git config user.name` và `user.email` phải là của mình.
- Commit message mô tả rõ thay đổi, **không** dùng `update`, `fix`, `final`.
- Trước khi mở PR: `git pull origin main` (hoặc rebase), build lại, chạy test phần của mình.
- Không commit `build/`, `out/`, `.vs/`, `CMakeSettings.json`, `*.sqlite`, log, token.

## 6. Checklist yêu cầu chung trước khi nộp

- [ ] Repo có `Code/`, `DOCX/`, `Extra/`, `PPTX/`, `README.md`, `.gitignore`
- [ ] README: tên + mã đề tài, thành viên, kiến trúc, môi trường, cấu hình, hướng dẫn chạy, **link video**
- [ ] IP/Port cấu hình được, Server và Client là 2 tiến trình riêng
- [ ] Server validate dữ liệu, trả lỗi rõ ràng; ghi log kết nối, ngắt kết nối, lỗi, thao tác chính
- [ ] Xử lý ngắt kết nối đột ngột; timeout cho các thao tác chờ
- [ ] Functional test đủ chức năng, test dữ liệu sai, test mất kết nối, stress/performance ≥ 2 mức tải
- [ ] Báo cáo có: mục tiêu, phạm vi, kiến trúc, protocol, message, GUI, phân công, hướng dẫn, kiểm thử, kết quả, hạn chế
