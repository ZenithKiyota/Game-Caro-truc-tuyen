cmake_minimum_required(VERSION 3.21)
project(CaroOnline VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTORCC ON)

find_package(Qt6 6.2 REQUIRED COMPONENTS Core Network Widgets Sql)

# Bảo đảm MSVC biên dịch chuỗi tiếng Việt trong mã nguồn UTF-8.
if(MSVC)
    add_compile_options(/utf-8)
endif()

set(CARO_SHARED_DIR "${CMAKE_SOURCE_DIR}/Code/Shared")

add_subdirectory(Code/Server)
add_subdirectory(Code/Client)

set_target_properties(CaroServer CaroClient PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")
