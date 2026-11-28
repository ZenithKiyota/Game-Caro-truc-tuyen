"""Kiểm thử luật chơi Caro qua TCP/NDJSON: TC_29 đến TC_35.

Chạy Server trước, sau đó:
    python3 Extra/tests/test_game_rules_tc29.py --host 127.0.0.1 --port 45454
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
    pass


class Client:
    def __init__(self, host: str, port: int, name: str) -> None:
        self.name = name
        self.socket = socket.create_connection((host, port), timeout=5)
        self.socket.settimeout(1)
        self.buffer = b""

    def close(self) -> None:
        self.socket.close()

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

    def wait_type(self, message_type: str, timeout: float = 5) -> dict[str, object]:
        deadline = time.monotonic() + timeout
        while True:
            message = self.receive(max(0.1, deadline - time.monotonic()))
            if message.get("type") == message_type:
                return message


def unique_name(prefix: str) -> str:
    return (prefix + uuid.uuid4().hex[:8])[:20]


def start_match(host: str, port: int) -> tuple[Client, Client, int]:
    x = Client(host, port, unique_name("TC29X"))
    o = Client(host, port, unique_name("TC29O"))
    try:
        x.send("hello", name=x.name, token="")
        o.send("hello", name=o.name, token="")
        x.wait_type("welcome")
        o.wait_type("welcome")
        x.send("invite", target=o.name)
        o.wait_type("invite_received")
        o.send("invite_reply", **{"from": x.name, "accept": True})
        snapshot = x.wait_type("snapshot")
        o.wait_type("snapshot")
        room = snapshot.get("room")
        if not isinstance(room, int):
            raise ProtocolError(f"room không hợp lệ: {snapshot}")
        return x, o, room
    except Exception:
        x.close()
        o.close()
        raise


def move(client: Client, room: int, row: int, col: int) -> None:
    client.send("move", room=room, row=row, col=col)


def expect_error(client: Client, text: str) -> None:
    message = client.wait_type("error")
    if text not in str(message.get("message", "")):
        raise ProtocolError(f"mong đợi {text!r}, nhận được {message}")


def expect_snapshot(client: Client, active: bool | None = True) -> dict[str, object]:
    snapshot = client.wait_type("snapshot")
    if active is not None and snapshot.get("active") is not active:
        raise ProtocolError(f"active mong đợi {active}, nhận được {snapshot}")
    return snapshot


def play(x: Client, o: Client, room: int, cells: list[tuple[int, int]]) -> dict[str, object]:
    result: dict[str, object] = {}
    for index, cell in enumerate(cells):
        current, other = (x, o) if index % 2 == 0 else (o, x)
        move(current, room, *cell)
        result = expect_snapshot(other, index < len(cells) - 1)
        if result.get("active") is False:
            return result
    return result


def close_match(x: Client, o: Client) -> None:
    x.close()
    o.close()


def tc29_wrong_turn(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        move(o, room, 0, 0)
        expect_error(o, "Chưa đến lượt")
    finally:
        close_match(x, o)


def tc30_occupied_cell(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        move(x, room, 0, 0)
        expect_snapshot(o)
        move(o, room, 0, 0)
        expect_error(o, "đã có quân")
    finally:
        close_match(x, o)


def tc31_out_of_board(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        move(x, room, -1, 0)
        expect_error(x, "ngoài bàn")
        move(x, room, BOARD_SIZE, BOARD_SIZE)
        expect_error(x, "ngoài bàn")
    finally:
        close_match(x, o)


def tc32_win_directions(host: str, port: int) -> None:
    directions = (
        [(0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2), (0, 3), (1, 3), (0, 4)],
        [(0, 0), (0, 1), (1, 0), (1, 1), (2, 0), (2, 1), (3, 0), (3, 1), (4, 0)],
        [(0, 0), (0, 1), (1, 1), (0, 2), (2, 2), (0, 3), (3, 3), (0, 4), (4, 4)],
    )
    for cells in directions:
        x, o, room = start_match(host, port)
        try:
            result = play(x, o, room, cells)
            if result.get("winner") != x.name:
                raise ProtocolError(f"winner không đúng: {result}")
        finally:
            close_match(x, o)


def tc33_draw(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        groups = [[], []]
        for row in range(BOARD_SIZE):
            for col in range(BOARD_SIZE):
                groups[0 if (row + 2 * col) % 4 < 2 else 1].append((row, col))
        cells = [cell for index in range(max(map(len, groups))) for group in groups for cell in ([group[index]] if index < len(group) else [])]
        result = play(x, o, room, cells)
        if result.get("active") is not False or result.get("winner") not in ("", None):
            raise ProtocolError(f"bàn đầy phải hòa: {result}")
        if "đầy" not in str(result.get("reason", "")).lower():
            raise ProtocolError(f"lý do hòa không đúng: {result}")
    finally:
        close_match(x, o)


def tc34_timeout(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        deadline = time.monotonic() + TURN_SECONDS + 5
        while True:
            result = x.wait_type("snapshot", max(0.1, deadline - time.monotonic()))
            if result.get("active") is False:
                break
        if result.get("winner") != o.name or "30" not in str(result.get("reason", "")):
            raise ProtocolError(f"timeout không đúng: {result}")
    finally:
        close_match(x, o)


def tc35_win_after_valid_moves(host: str, port: int) -> None:
    x, o, room = start_match(host, port)
    try:
        result = play(x, o, room, [(0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2), (0, 3), (1, 3), (0, 4)])
        if result.get("active") is not False or result.get("winner") != x.name:
            raise ProtocolError(f"thắng sau nước hợp lệ không đúng: {result}")
    finally:
        close_match(x, o)


TESTS: list[tuple[str, Callable[[str, int], None]]] = [
    ("TC_29 sai lượt", tc29_wrong_turn),
    ("TC_30 ô đã có quân", tc30_occupied_cell),
    ("TC_31 ô ngoài bàn", tc31_out_of_board),
    ("TC_32 thắng ngang/dọc/chéo", tc32_win_directions),
    ("TC_33 hòa", tc33_draw),
    ("TC_34 hết 30 giây", tc34_timeout),
    ("TC_35 thắng sau nước hợp lệ", tc35_win_after_valid_moves),
]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=45454)
    parser.add_argument("--only", nargs="*")
    args = parser.parse_args()
    selected = set(args.only or [name.split()[0] for name, _ in TESTS])
    passed = 0
    total = 0
    for name, test in TESTS:
        if name.split()[0] not in selected:
            continue
        total += 1
        try:
            test(args.host, args.port)
            print(f"PASS {name}")
            passed += 1
        except Exception as error:  # noqa: BLE001
            print(f"FAIL {name}: {error}")
    print(f"Kết quả: {passed}/{total} test case đạt")
    return 0 if passed == total else 1


if __name__ == "__main__":
    raise SystemExit(main())
