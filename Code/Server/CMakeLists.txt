qt_add_executable(CaroServer
    main.cpp
    CaroServer.h
    CaroServer.cpp          # Huy  - TCP, framing, dispatch, log, clock
    SessionHandler.cpp      # Phát - đăng nhập, ngắt kết nối, reconnect
    HistoryStore.cpp        # Phát - SQLite lịch sử trận
    LobbyHandler.cpp        # Minh - online, lời mời, tạo phòng
    GameHandler.cpp         # Tiến - nước đi, thắng/thua/hòa, hết giờ
    SpectatorHandler.cpp    # Tú   - danh sách phòng, khán giả, snapshot
)
target_include_directories(CaroServer PRIVATE ${CARO_SHARED_DIR})
target_link_libraries(CaroServer PRIVATE Qt6::Core Qt6::Network Qt6::Sql)
target_compile_definitions(CaroServer PRIVATE QT_NO_CAST_FROM_ASCII)
