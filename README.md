# UDM_16 -- Game Caro trực tuyến

Game Caro nhiều người chơi qua Server, hỗ trợ phòng đấu, khán giả và đồng bộ trạng thái trận đấu.
Ứng dụng desktop C# (.NET 10, WinForms), mô hình Client–Server qua TCP (NDJSON), lưu lịch sử bằng SQLite.

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
  Client/   # CaroClient: giao diện WinForms
  Shared/   # Protocol.cs + PROTOCOL.md dùng chung
DOCX/       # Báo cáo
Extra/      # Script kiểm thử, kết quả, bằng chứng
PPTX/       # Slide thuyết trình
```

## Môi trường

- Windows 10/11, [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2026 (workload *.NET desktop development*) hoặc VS Code + C# Dev Kit
- SQLite qua NuGet `Microsoft.Data.Sqlite` (tự tải khi build)
- Python 3 (chạy script kiểm thử)

## Build & chạy

Visual Studio: mở `CaroOnline.slnx`, **Build Solution**. Hoặc dòng lệnh:

```bash
dotnet build CaroOnline.slnx
dotnet run --project Code/Server -- --port 45454
dotnet run --project Code/Client
```

1. Chạy Server trước (mặc định `0.0.0.0:45454`; đổi bằng `--address`, `--port`).
2. Mở từ 2 Client trở lên, nhập IP/Port của Server và tên khác nhau.

## Video demo

_(cập nhật link sau)_
