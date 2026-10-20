qt_add_executable(CaroClient WIN32
    main.cpp
    MainWindow.h
    MainWindow.cpp      # Toàn - khung giao diện, kết nối, auto-reconnect, log
    LobbyPanel.cpp      # Minh - danh sách online, mời / trả lời lời mời
    GamePanel.cpp       # Tiến - bàn cờ, lượt, đồng hồ, kết quả
    BoardWidget.h
    BoardWidget.cpp     # Tiến - vẽ bàn cờ 15x15
    RoomsPanel.cpp      # Tú   - danh sách phòng, vào xem / rời ghế khán giả
    HistoryPanel.cpp    # Phát - bảng lịch sử trận
)
target_include_directories(CaroClient PRIVATE ${CARO_SHARED_DIR})
target_link_libraries(CaroClient PRIVATE Qt6::Core Qt6::Network Qt6::Widgets)
