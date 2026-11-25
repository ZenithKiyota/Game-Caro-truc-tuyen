# Nháp kiến trúc và giao thức

## Kiến trúc Client–Server

CaroOnline sử dụng mô hình Client–Server. Server là tiến trình riêng, lắng nghe kết nối TCP tại cổng mặc định `45454`; người chạy Server có thể đổi địa chỉ và cổng bằng tham số dòng lệnh. Mỗi Client mở kết nối TCP tới Server và gửi yêu cầu. Server giữ trạng thái phiên, phòng đấu và lời mời, kiểm tra yêu cầu rồi gửi phản hồi hoặc cập nhật tới các Client liên quan. Các thành phần dùng chung của giao thức và luật được khai báo trong `Code/Shared/Protocol.cs`; Server và Client cùng tham chiếu dự án Shared.

Trong Server, `CaroServer` nhận kết nối và phân luồng message đến các handler theo chức năng. Mỗi kết nối có luồng đọc riêng. Những thay đổi trạng thái chung được xử lý trong khóa `_gate` để tránh hai yêu cầu cùng lúc làm hỏng trạng thái. Các handler riêng xử lý phiên và reconnect, sảnh và lời mời, luật đi, lịch sử, cùng chế độ khán giả.

## Đóng gói và truyền dữ liệu

Ứng dụng truyền dữ liệu bằng TCP với UTF‑8. Mỗi message là một JSON object theo định dạng NDJSON: một object trên một dòng, kết thúc bằng ký tự xuống dòng `\n`. TCP không giữ ranh giới message, nên một lần đọc có thể nhận một phần message hoặc nhiều message nối liền. Server tích lũy byte tới ký tự xuống dòng rồi mới phân tích JSON. Kích thước tối đa của một frame là `65,536` byte (64 KiB); frame quá giới hạn bị từ chối và kết nối đó được đóng.

Cổng mặc định là `45454`. Client và Server trao đổi tên `type` cùng các trường JSON theo bảng dưới đây. Server gửi `error` khi yêu cầu sai định dạng, chưa đăng nhập hoặc không có quyền thực hiện thao tác.

## Các message chính

| Message | Hướng | Trường chính | Ý nghĩa |
|---|---|---|---|
| `hello` | Client → Server | `name`, `token` | Đăng nhập mới hoặc yêu cầu tiếp tục phiên bằng token. |
| `welcome` | Server → Client | `name`, `token`, `resumed` | Xác nhận đăng nhập và trả token phiên. |
| `list_users` | Client → Server | — | Yêu cầu danh sách người chơi online. |
| `users` | Server → Client | `users` | Danh sách người chơi và trạng thái bận. |
| `invite` | Client → Server | `target` | Gửi lời mời đấu tới người chơi đang rảnh. |
| `invite_received` | Server → Client | `from` | Báo có lời mời mới. |
| `invite_reply` | Client → Server | `from`, `accept` | Chấp nhận hoặc từ chối lời mời. |
| `invite_result` | Server → Client | `target`, `accepted` | Trả kết quả lời mời cho người mời. |
| `move` | Client → Server | `room`, `row`, `col` | Yêu cầu đặt quân trong phòng. Server kiểm tra lượt và vị trí. |
| `snapshot` | Server → Client | `room`, `x`, `o`, `board`, `turn`, `secondsLeft`, `active` và các trường trạng thái | Gửi trạng thái hiện tại của ván đấu, gồm bàn cờ và đồng hồ. |
| `list_rooms` | Client → Server | — | Yêu cầu danh sách các phòng đang đấu. |
| `rooms` | Server → Client | `rooms` | Danh sách phòng, hai người chơi, số khán giả và trạng thái tạm dừng. |
| `spectate` | Client → Server | `room` | Yêu cầu vào xem một phòng. |
| `leave_spectate` | Client → Server | — | Yêu cầu rời vai trò khán giả. |
| `history` | Client → Server | — | Yêu cầu lịch sử các trận gần đây. |
| `history_result` | Server → Client | `matches` | Trả danh sách trận đã lưu. |
| `error` / `notice` | Server → Client | `message` | Báo lỗi yêu cầu hoặc thông tin cho người chơi. |

## Thông số giao thức

Các hằng số hiện được khai báo trong `Code/Shared/Protocol.cs`: bàn cờ `15 × 15`, điều kiện thắng là từ 5 quân liên tiếp, mỗi lượt tối đa 30 giây, thời hạn lời mời 60 giây, thời gian cho phép reconnect 90 giây và frame tối đa 65,536 byte. Mô tả chi tiết message và luật nằm trong `Code/Shared/PROTOCOL.md`.

> Nháp này mô tả thiết kế và giao thức theo mã nguồn. Cần đối chiếu lại với bản báo cáo cuối và số liệu kiểm thử thực tế trước khi nộp.

## Kiểm thử hiệu năng

Ngày 25/11/2026, LoadTest chạy trên loopback `127.0.0.1:45454` với máy dùng CPU Intel Core i5‑12450H, 12 bộ xử lý logic và 15,7 GB RAM. Mỗi lượt tạo các Client ảo, đăng nhập, ghép từng cặp vào trận và đánh 6 nước; sau đó gửi 500 yêu cầu `list_rooms` luân phiên qua các Client. Mỗi mức tải chạy ba lần. Bảng dưới dùng **trung vị** của ba lần; khoảng trong ngoặc là giá trị nhỏ nhất–lớn nhất quan sát được.

| Client | Độ trễ TB (ms) | P95 (ms) | Thông lượng (yêu cầu/giây) | CPU Server (% tổng CPU máy) | RAM Server (MB) | Lỗi |
|---:|---:|---:|---:|---:|---:|---:|
| 10 | 1,39 (0,20–1,85) | 3,49 (0,27–5,31) | 720 (539–4.933) | 3,37 (0–5,29) | 56,32 (48,71–56,87) | 0 |
| 50 | 1,63 (0,42–2,03) | 3,96 (0,85–4,19) | 612 (492–2.372) | 5,90 (5,76–8,75) | 57,40 (55,99–57,57) | 0 |
| 100 | 0,57 (0,53–0,76) | 0,78 (0,66–1,43) | 1.737 (1.305–1.890) | 7,14 (6,93–7,19) | 57,89 (56,29–61,88) | 0 |

Cả 9 lượt chạy hoàn tất với 0 lỗi. CPU và RAM trung vị ở các mức tải lần lượt nằm trong khoảng 3,37–7,14% và 56,32–57,89 MB trên máy đo. Các trị số độ trễ và thông lượng dao động giữa các lần chạy và không tăng đều theo số Client; đây là kết quả đo loopback cục bộ, nên không đại diện cho độ trễ khi chơi qua mạng LAN.

Lượt 10 Client đầu kết thúc trước chu kỳ lấy mẫu CPU 200 ms nên CPU lượt đó ghi 0%; vì vậy bảng dùng trung vị của ba lượt và thể hiện khoảng đo đầy đủ. File tổng kết từng lượt và CSV mẫu 200 ms được lưu trong `Extra/results/Huy/`. Đây là số liệu chạy tự động trong phiên này; Huy cần tự chạy lại trên máy của mình và ghi vào cột Test Result 2 theo yêu cầu môn học.
