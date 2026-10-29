"""TC_10-TC_16: kiểm thử luật chơi Caro qua giao thức TCP/NDJSON.

Chạy Server trước, sau đó chạy:
    python3 Extra/tests/test_game_rules.py --host 127.0.0.1 --port 45454

Mỗi test tạo một phòng mới bằng hai tài khoản tạm thời. Script chỉ dùng thư
viện chuẩn Python và không thay đổi dữ liệu của Server.
"""

from __future__ import annotations

import argparse
import json
import socket
import time
import uuid
from collections.abc import Callable


BOARD_SIZE = 15
TURN_SECONDS = 30


class ProtocolError(RuntimeError):
    """Server trả về thông báo không đúng với điều kiện test."""


class Client:
    def __init__(self, host: str, port: int, name: str) -> None:
        self.name = name
        self.socket = socket.create_connection((host, port), timeout=5)
        self.socket.settimeout(1)
        self.buffer = b""

    def close(self) -> None:
        try:
            self.socket.close()
        except OSError:
            pass

    def send(self, message_type: str, **fields: object) -> None:
        message = {"type": message_type, **fields}
        self.socket.sendall((json.dumps(message, ensure_ascii=False) + "\n").encode())

    def receive(self, timeout: float = 5) -> dict[str, object]:
        deadline = time.monotonic() + timeout
        while True:
            newline = self.buffer.find(b"\n")
            if newline >= 0:
                line, self.buffer = self.buffer[:newline], self.buffer[newline + 1 :]
                return json.loads(line.decode("utf-8"))

            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise TimeoutError(f"Không nhận được message từ {self.name}")
            self.socket.settimeout(min(1, remaining))
            try:
                chunk = self.socket.recv(65536)
            except socket.timeout:
                continue
            if not chunk:
                raise ConnectionError(f"Server đóng kết nối của {self.name}")
            self.buffer += chunk

    def wait_for(self, predicate: Callable[[dict[str, object]], bool], timeout: float = 5) -> dict[str, object]:
        deadline = time.monotonic() + timeout
        while True:
            message = self.receive(max(0.1, deadline - time.monotonic()))
            if predicate(message):
                return message

    def wait_type(self, message_type: str, timeout: float = 5) -> dict[str, object]:
        return self.wait_for(lambda message: message.get("type") == message_type, timeout)


def unique_name(prefix: str) -> str:
    return (prefix + uuid.uuid4().hex[:8])[:20]


def start_match(host: str, port: int) -> tuple[Client, Client, int]:
    player_x = Client(host, port, unique_name("TestX"))
    player_o = Client(host, port, unique_name("TestO"))
    try:
        player_x.send("hello", name=player_x.name, token="")
        player_o.send("hello", name=player_o.name, token="")
        player_x.wait_type("welcome")
        player_o.wait_type("welcome")

        player_x.send("invite", target=player_o.name)
        player_o.wait_type("invite_received")
        player_o.send("invite_reply", **{"from": player_x.name, "accept": True})
        snapshot_x = player_x.wait_type("snapshot")
        player_o.wait_type("snapshot")
        room = snapshot_x.get("room")
        if not isinstance(room, int):
            raise ProtocolError(f"Snapshot không có room hợp lệ: {snapshot_x}")
        return player_x, player_o, room
    except Exception:
        player_x.close()
        player_o.close()
        raise


def finish_match(player_x: Client, player_o: Client) -> None:
    # Chờ snapshot kết thúc nếu test chưa đọc nó, rồi đóng kết nối.
    player_x.close()
    player_o.close()


def move(client: Client, room: int, row: int, col: int) -> None:
    client.send("move", room=room, row=row, col=col)


def expect_error(client: Client, text: str) -> None:
    message = client.wait_type("error")
    actual = str(message.get("message", ""))
    if text not in actual:
        raise ProtocolError(f"Lỗi mong đợi chứa {text!r}, nhận được {actual!r}")


def expect_active_snapshot(client: Client, active: bool = True) -> dict[str, object]:
    snapshot = client.wait_type("snapshot")
    if snapshot.get("active") is not active:
        raise ProtocolError(f"active mong đợi {active}, nhận được {snapshot}")
    return snapshot


def test_tc10_wrong_turn(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        move(o, room, 0, 0)
        expect_error(o, "Chưa đến lượt")
    finally:
        finish_match(x, o)


def test_tc11_occupied_cell(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        move(x, room, 0, 0)
        expect_active_snapshot(o)
        move(o, room, 0, 0)
        expect_error(o, "đã có quân")
    finally:
        finish_match(x, o)


def test_tc12_out_of_board(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        move(x, room, -1, 0)
        expect_error(x, "ngoài bàn")
        move(x, room, BOARD_SIZE, BOARD_SIZE)
        expect_error(x, "ngoài bàn")
    finally:
        finish_match(x, o)


def play_sequence(x: Client, o: Client, room: int, cells: list[tuple[int, int]]) -> dict[str, object]:
    """Đánh xen kẽ X/O; các ô O được chọn từ cells[1], cells[3], ..."""
    for index, (row, col) in enumerate(cells):
        current = x if index % 2 == 0 else o
        other = o if current is x else x
        move(current, room, row, col)
        snapshot = expect_active_snapshot(other, active=index < len(cells) - 1)
        if not snapshot.get("active", True):
            return snapshot
    return snapshot


def test_tc13_win_horizontal_vertical_diagonal(host: str, port: int) -> None:
    directions = {
        "ngang": [(0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2), (0, 3), (1, 3), (0, 4)],
        "dọc": [(0, 0), (0, 1), (1, 0), (1, 1), (2, 0), (2, 1), (3, 0), (3, 1), (4, 0)],
        "chéo": [(0, 0), (0, 1), (1, 1), (0, 2), (2, 2), (0, 3), (3, 3), (0, 4), (4, 4)],
    }
    for direction, cells in directions.items():
        x, o, room = start_match(host, port)
        try:
            result = play_sequence(x, o, room, cells)
            if result.get("winner") != x.name:
                raise ProtocolError(f"Thắng {direction}: winner không đúng: {result}")
        finally:
            finish_match(x, o)


def draw_pattern() -> list[tuple[int, int]]:
    """Tạo bàn đầy mà không có 5 quân liền nhau cho mỗi màu."""
    cells_by_color: list[list[tuple[int, int]]] = [[], []]
    for row in range(BOARD_SIZE):
        for col in range(BOARD_SIZE):
            # Mẫu chu kỳ 4 theo hai hướng, tránh chuỗi dài 5 ngang/dọc/chéo.
            color = 0 if (row + 2 * col) % 4 < 2 else 1
            cells_by_color[color].append((row, col))

    result: list[tuple[int, int]] = []
    for index in range(max(map(len, cells_by_color))):
        for color in (0, 1):
            if index < len(cells_by_color[color]):
                result.append(cells_by_color[color][index])
    return result


def test_tc14_draw(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        result = play_sequence(x, o, room, draw_pattern())
        if result.get("active") is not False or result.get("winner") not in ("", None):
            raise ProtocolError(f"Bàn đầy phải hòa, nhận được: {result}")
        if "đầy" not in str(result.get("reason", "")).lower():
            raise ProtocolError(f"Lý do hòa không đúng: {result}")
    finally:
        finish_match(x, o)


def test_tc15_timeout(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        result = x.wait_for(lambda message: message.get("type") == "snapshot" and message.get("active") is False,
                            timeout=TURN_SECONDS + 5)
        if result.get("winner") != o.name or "30" not in str(result.get("reason", "")):
            raise ProtocolError(f"Hết giờ phải xử X thua, nhận được: {result}")
    finally:
        finish_match(x, o)


def test_tc16_invalid_move_payload(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        x.send("move", room=room, row=0)  # Thiếu col.
        expect_error(x, "room, row và col")
        x.send("move", room="sai-kieu", row=0, col=0)
        expect_error(x, "room, row và col")
    finally:
        finish_match(x, o)


TESTS: list[tuple[str, Callable[[str, int], None]]] = [
    ("TC_10 sai lượt", test_tc10_wrong_turn),
    ("TC_11 ô đã có quân", test_tc11_occupied_cell),
    ("TC_12 ô ngoài bàn", test_tc12_out_of_board),
    ("TC_13 thắng ngang/dọc/chéo", test_tc13_win_horizontal_vertical_diagonal),
    ("TC_14 hòa", test_tc14_draw),
    ("TC_15 hết 30 giây", test_tc15_timeout),
    ("TC_16 dữ liệu nước đi sai định dạng", test_tc16_invalid_move_payload),
]


def main() -> int:
    parser = argparse.ArgumentParser(description="Kiểm thử luật chơi Caro TC_10-TC_16")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=45454)
    parser.add_argument("--only", nargs="*", help="Chỉ chạy mã, ví dụ: TC_10 TC_13")
    args = parser.parse_args()
    selected = set(args.only or [name.split()[0] for name, _ in TESTS])

    passed = 0
    for name, test in TESTS:
        code = name.split()[0]
        if code not in selected:
            continue
        try:
            test(args.host, args.port)
            print(f"PASS {name}")
            passed += 1
        except Exception as error:  # noqa: BLE001 - báo từng case và chạy tiếp case sau.
            print(f"FAIL {name}: {error}")

    total = len([name for name, _ in TESTS if name.split()[0] in selected])
    print(f"Kết quả: {passed}/{total} test case đạt")
    return 0 if passed == total else 1


if __name__ == "__main__":
    raise SystemExit(main())
