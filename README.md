# UDM_16 -- Game Caro trực tuyến

Game Caro nhiều người chơi qua Server, hỗ trợ phòng đấu, khán giả và đồng bộ trạng thái trận đấu.
Ứng dụng desktop C++17 / Qt 6, mô hình Client–Server qua TCP (NDJSON), lưu lịch sử bằng SQLite.

# Danh Sách Thành Viên
1. Nguyễn Đức Huy
2. Trần Đức Toàn
3. Hứa Hồng Tiến
4. Hồ Thanh Anh Minh
5. Nguyễn Tuấn Tú
6. Nguyễn An Phát

Phân công chi tiết: [PHAN_CONG.md](PHAN_CONG.md). Giao thức: [Code/Shared/PROTOCOL.md](Code/Shared/PROTOCOL.md).

## Cấu trúc

```text
Code/
  Server/   # CaroServer: TCP, phòng, luật chơi, khán giả, reconnect, SQLite
  Client/   # CaroClient: giao diện Qt Widgets
  Shared/   # Protocol.h + PROTOCOL.md dùng chung
DOCX/       # Báo cáo
Extra/      # Script kiểm thử, kết quả, bằng chứng
PPTX/       # Slide thuyết trình
```

## Môi trường

- Windows 10/11, Visual Studio 2022 (Desktop development with C++)
- Qt 6 (MSVC 2022 64-bit), các module Core, Network, Widgets, Sql
- CMake ≥ 3.21, Python 3 (chạy script kiểm thử)

## Build & chạy

1. Visual Studio: **File > Open > Folder**, chọn thư mục repo.
2. Thêm tham số CMake trỏ tới Qt, ví dụ `-DCMAKE_PREFIX_PATH=C:/Qt/6.x.x/msvc2022_64`.
3. **Build All**, rồi chạy `CaroServer.exe` (mặc định `0.0.0.0:45454`; đổi bằng `--address`, `--port`).
4. Mở từ 2 `CaroClient.exe` trở lên, nhập IP/Port của Server và tên khác nhau.

## Video demo

_(cập nhật link sau)_
