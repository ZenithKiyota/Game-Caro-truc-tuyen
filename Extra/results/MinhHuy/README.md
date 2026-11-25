# Minh chứng test case – Minh (TC_01–08) và Huy (TC_09–19)

Chạy ngày 18/11/2026 trên app thật (Server + nhiều Client, bản `main` 8f240f5), có AI hỗ trợ thao tác.
Kết quả chi tiết ghi trong file Google Sheets "UDM_16 – Test case (Minh & Huy)".

| Test case | Nội dung | Tệp minh chứng |
| --- | --- | --- |
| TC_01 | Danh sách online cập nhật khi có người vào/ra | `TC_01_a_hai_nguoi_online.png`, `TC_01_b_bob_thoat.png` |
| TC_02 | Mời → chấp nhận, tạo phòng | `TC_02_moi_chap_nhan.png` |
| TC_03 | Mời → từ chối | `TC_03_moi_tu_choi.png` |
| TC_04 | Chặn lời mời không hợp lệ | `TC_04_loi_moi_khong_hop_le.png` |
| TC_05 | Lời mời hết hạn sau 60 giây | `TC_05_loi_moi_het_han.png` |
| TC_06 | Hai phòng chạy đồng thời | `TC_06_hai_phong.png` |
| TC_07 | Server kiểm tra dữ liệu lời mời sai | `TC_07_du_lieu_sai.png` |
| TC_08 | Người được mời thoát / nhận 2 lời mời cùng lúc | `TC_08_huy_va_hai_loi_moi.png` |

Log Server của lượt chạy TC_01–08: `server_log_minh.txt`.
