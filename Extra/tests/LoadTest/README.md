# LoadTest

LoadTest mở số Client ảo chẵn theo cấu hình, đăng nhập từng Client, ghép từng cặp vào một ván ngắn và đánh 6 nước hợp lệ. Sau đó gửi yêu cầu `list_rooms` luân phiên qua các Client, đo độ trễ trung bình, P95 và thông lượng. Cứ khoảng 200 ms công cụ lấy CPU và RAM của tiến trình `CaroServer`, lưu từng mẫu vào CSV và ghi phần tổng kết vào TXT.

## Chạy

Mở hai Terminal tại thư mục gốc repo. Terminal thứ nhất chạy Server:

```powershell
dotnet run --project Code/Server -- --address 127.0.0.1 --port 45454
```

Terminal thứ hai build công cụ rồi chạy mỗi mức tải ba lần. Thư mục kết quả `Extra/results/Huy` được tạo tự động:

```powershell
dotnet build Extra/tests/LoadTest -c Release
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 10 500 "Extra/results/Huy" L10-1
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 10 500 "Extra/results/Huy" L10-2
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 10 500 "Extra/results/Huy" L10-3
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 50 500 "Extra/results/Huy" L50-1
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 50 500 "Extra/results/Huy" L50-2
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 50 500 "Extra/results/Huy" L50-3
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 100 500 "Extra/results/Huy" L100-1
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 100 500 "Extra/results/Huy" L100-2
dotnet run --project Extra/tests/LoadTest -c Release -- 127.0.0.1 45454 100 500 "Extra/results/Huy" L100-3
```

Mỗi lần chạy tạo `perf_<client>client_<nhãn>.txt` và `.csv`. Chạy lần lượt để mỗi lần có số đo riêng. Mức 50/100 tạo nhiều Client TCP, vì vậy hãy đóng ứng dụng khác đang dùng cổng 45454 và không chạy nhiều bài đo cùng lúc.

## Đọc kết quả

- `Average latency (ms)`: thời gian trung bình từ lúc gửi `list_rooms` đến khi nhận `rooms`.
- `P95 latency (ms)`: 95% số yêu cầu phản hồi trong thời gian bằng hoặc nhỏ hơn giá trị này.
- `Throughput (requests/s)`: tổng số yêu cầu chia cho thời gian đo.
- `CPU` được chuẩn hóa theo tổng số bộ xử lý logic của máy; `RAM` là working set của tiến trình CaroServer.
- CSV chứa thời điểm, CPU và RAM từng mẫu. Nếu LoadTest báo lỗi hoặc dừng giữa chừng, ghi lỗi riêng và không đưa số đó vào bảng kết quả như một lượt chạy hoàn tất.

Ghi model CPU và dung lượng RAM máy thật vào báo cáo cùng kết quả. Không dùng số tham khảo từ máy khác làm số đo của Huy.
