# UDM_16 -- Game Caro trực tuyến

Game Caro nhiều người chơi qua Server, hỗ trợ phòng đấu, khán giả và đồng bộ trạng thái trận đấu.
Ứng dụng desktop C# (.NET 10, WinForms), mô hình Client–Server qua TCP (NDJSON), lưu lịch sử bằng SQLite.

- **Mã đề tài:** UDM_16
- **Repo:** https://github.com/ZenithKiyota/Game-Caro-truc-tuyen

## Danh sách thành viên

| STT | Thành viên | Phụ trách |
| --- | --- | --- |
| 1 | Nguyễn Đức Huy (trưởng nhóm) | Lõi Server: TCP, tách gói NDJSON, điều phối message, log; stress/performance test |
| 2 | Trần Đức Toàn | Lõi Client: giao diện khung, kết nối IP/Port, trạng thái kết nối, tự kết nối lại |
| 3 | Hứa Hồng Tiến | Luật chơi: kiểm tra nước đi, thắng/thua/hòa, giới hạn 30 giây/lượt |
| 4 | Hồ Thanh Anh Minh | Sảnh chờ: danh sách online, mời/chấp nhận/từ chối, nhiều phòng đồng thời; README |
| 5 | Nguyễn Tuấn Tú | Khán giả: danh sách phòng, vào xem giữa trận, rời phòng |
| 6 | Nguyễn An Phát | Phiên & lịch sử: đăng nhập, token, reconnect 90 giây, SQLite |

Bảng phân công, tiến độ và test case (online, bản chính): [Google Sheets – UDM_16 Phân công & Test case](https://docs.google.com/spreadsheets/d/1p3Y53-S2SX1qd8Tqte2zXHcZ4ZW_oZXrQpgQfW0jeuU/edit). Bản sao Excel: [Extra/PhanCong_UDM16.xlsx](Extra/PhanCong_UDM16.xlsx).

Phân công chi tiết: [PHAN_CONG.md](PHAN_CONG.md). Giao thức: [Code/Shared/PROTOCOL.md](Code/Shared/PROTOCOL.md).

## Kiến trúc

- **Server** (`CaroServer`, ứng dụng console) giữ toàn bộ trạng thái: người đang online, lời mời, các phòng đấu, khán giả. Server là nơi duy nhất kiểm tra nước đi và xác định thắng/thua/hòa.
- **Client** (`CaroClient`, WinForms) chỉ gửi yêu cầu và vẽ lại giao diện theo dữ liệu Server gửi về.
- Hai bên nói chuyện qua **TCP**, mỗi gói tin là **một dòng JSON** (NDJSON), tối đa 64 KiB. Danh sách message: [PROTOCOL.md](Code/Shared/PROTOCOL.md).
- Lịch sử trận lưu bằng **SQLite** ở phía Server.

## Cấu trúc thư mục

```text
Code/
  Server/   # CaroServer: TCP, phòng, luật chơi, khán giả, reconnect, SQLite
  Client/   # CaroClient: giao diện WinForms
  Shared/   # Protocol.cs + PROTOCOL.md dùng chung
DOCX/       # Báo cáo
Extra/      # Kết quả kiểm thử, bằng chứng
PPTX/       # Slide thuyết trình
```

## Môi trường

- Windows 10/11, [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2026 (workload *.NET desktop development*) hoặc VS Code + C# Dev Kit
- SQLite qua NuGet `Microsoft.Data.Sqlite` (tự tải khi build, không cần cài riêng)

## Hướng dẫn chạy

### 1. Build

Visual Studio: mở `CaroOnline.slnx`, chọn **Build Solution**. Hoặc dòng lệnh, đứng ở thư mục gốc repo:

```bash
dotnet build CaroOnline.slnx
```

### 2. Chạy Server

```bash
dotnet run --project Code/Server
```

| Tham số | Ý nghĩa | Mặc định |
| --- | --- | --- |
| `--address`, `-a` | Địa chỉ IP Server lắng nghe | `0.0.0.0` (mọi card mạng) |
| `--port`, `-p` | Cổng TCP (1–65535) | `45454` |
| `--help`, `-h` | In hướng dẫn | |

Ví dụ đổi cổng: `dotnet run --project Code/Server -- --port 46000`. Cổng sai (ví dụ `99999`) thì Server báo `Port must be in 1..65535` và thoát.

### 3. Chạy Client

```bash
dotnet run --project Code/Client
```

Mở từ 2 Client trở lên (mỗi lệnh một cửa sổ). Ở thanh trên cùng của mỗi Client:

1. **IP máy chủ:** `127.0.0.1` nếu Server cùng máy, hoặc IP LAN của máy chạy Server.
2. **Cổng:** đúng cổng của Server (mặc định `45454`).
3. **Tên:** 3–20 ký tự, chỉ gồm chữ cái tiếng Anh, chữ số hoặc dấu `_`; mỗi người một tên.
4. Bấm **Kết nối**. Trạng thái chuyển thành "Đã kết nối và đăng nhập".

### 4. Chơi

1. Ở khung **Người chơi đang online**, chọn một người đang "rảnh" rồi bấm **Mời người được chọn**.
2. Người được mời bấm **Yes** để nhận hoặc **No** để từ chối. Lời mời tự hết hạn sau **60 giây**.
3. Khi nhận lời, bàn cờ 15×15 hiện ở cả hai bên: người mời cầm X và đi trước.
4. Mỗi lượt có **30 giây**. Ai có **5 quân liên tiếp** (ngang, dọc, chéo) thắng; kín bàn thì hòa.
5. Mất kết nối giữa trận: Client tự kết nối lại, trận được giữ trong **90 giây**; quá thời gian thì bị xử thua.

### 5. Xem trận và lịch sử

- Khung **Phòng đang đấu** liệt kê các trận đang diễn ra. Chọn một phòng, bấm **Vào xem** để làm khán giả (nhận ngay bàn cờ hiện tại). Khán giả không được đánh. Bấm **Rời ghế khán giả** để thoát mà trận vẫn tiếp tục.
- Khung **Lịch sử trận** hiện 50 trận gần nhất; bấm **Tải lại lịch sử** để cập nhật.

### 6. Chơi qua mạng LAN

1. Máy chạy Server: chạy Server như bước 2, xem IP LAN bằng lệnh `ipconfig` (dòng IPv4 Address).
2. Cho phép cổng qua Windows Firewall (lần đầu chạy Windows sẽ hỏi, chọn **Allow access**).
3. Các máy khác: nhập IP LAN đó và cổng vào Client rồi bấm Kết nối.

### Log và dữ liệu

Server ghi log và cơ sở dữ liệu vào `%AppData%\CaroServer\`:

- `server.log`: mỗi dòng có thời gian, gồm kết nối, ngắt kết nối, đăng nhập, lời mời, bắt đầu/kết thúc trận, lỗi; không ghi token.
- `matches.sqlite`: lịch sử trận.

Hai tệp này không được đưa lên git (đã có trong `.gitignore`).

## Kiểm thử

Test case và kết quả ghi trong trang **Test case** của bảng Google Sheets ở trên. Ảnh, log và số liệu làm bằng chứng lưu trong [Extra/results/](Extra/results/).

## Video demo

_(cập nhật link sau)_
