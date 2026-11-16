"""Small local Wildfire MQTT client and two-process duplex acceptance test.

Uses the IM server's MQTT 3.1.1 extension. No credentials are printed or saved.
"""

import argparse
import base64
import json
import os
import queue
import socket
import struct
import subprocess
import sys
import threading
import time
import uuid
from pathlib import Path

import requests
from cryptography.hazmat.primitives import padding
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes


APP_CONFIG = Path(r"D:\BMS-Communication-Lab\app-server\config\application.properties")
ROOT_KEY = bytes.fromhex("001122334455667778797A7B7C7D7E7F")
APP_URL = "http://127.0.0.1:8888"
IM_HOST = "127.0.0.1"
IM_PORT = 18883


def decrypt(ciphertext, key):
    k = key[:16]
    dec = Cipher(algorithms.AES(k), modes.CBC(k)).decryptor()
    padded = dec.update(ciphertext) + dec.finalize()
    unpad = padding.PKCS7(128).unpadder()
    plain = unpad.update(padded) + unpad.finalize()
    hours = struct.unpack("<I", plain[:4])[0]
    now = (int(time.time()) - 1514736000) // 3600
    if abs(hours - now) > 24:
        raise ValueError("token or message clock outside 24 hours")
    return plain[4:]


def encrypt(plain, key):
    k = key[:16]
    hours = (int(time.time()) - 1514736000) // 3600
    pad = padding.PKCS7(128).padder()
    data = pad.update(struct.pack("<I", hours) + plain) + pad.finalize()
    enc = Cipher(algorithms.AES(k), modes.CBC(k)).encryptor()
    return enc.update(data) + enc.finalize()


def varint(number):
    out = bytearray()
    while number > 127:
        out.append((number & 127) | 128)
        number >>= 7
    out.append(number)
    return bytes(out)


def pb_num(field, value):
    return varint(field << 3) + varint(value)


def pb_data(field, value):
    if isinstance(value, str):
        value = value.encode("utf-8")
    return varint((field << 3) | 2) + varint(len(value)) + value


def pb_fields(data):
    pos = 0
    result = {}
    while pos < len(data):
        tag, pos = read_varint(data, pos)
        field, kind = tag >> 3, tag & 7
        if kind == 0:
            value, pos = read_varint(data, pos)
        elif kind == 2:
            length, pos = read_varint(data, pos)
            value = data[pos:pos + length]
            pos += length
        else:
            raise ValueError(f"unsupported protobuf wire type {kind}")
        result.setdefault(field, []).append(value)
    return result


def read_varint(data, pos):
    value = 0
    shift = 0
    while True:
        byte = data[pos]
        pos += 1
        value |= (byte & 127) << shift
        if byte < 128:
            return value, pos
        shift += 7


def mqtt_str(data):
    if isinstance(data, str):
        data = data.encode("utf-8")
    return struct.pack(">H", len(data)) + data


def packet(kind, body):
    return bytes([kind]) + varint(len(body)) + body


def read_exact(sock, length):
    data = b""
    while len(data) < length:
        chunk = sock.recv(length - len(data))
        if not chunk:
            raise ConnectionError("MQTT connection closed")
        data += chunk
    return data


def read_packet(sock):
    kind = read_exact(sock, 1)[0]
    length = 0
    multiplier = 1
    while True:
        b = read_exact(sock, 1)[0]
        length += (b & 127) * multiplier
        if not b & 128:
            break
        multiplier *= 128
    return kind, read_exact(sock, length)


def get_super_code():
    for line in APP_CONFIG.read_text(encoding="utf-8-sig").splitlines():
        if line.strip().startswith("sms.super_code="):
            code = line.split("=", 1)[1].strip()
            if code:
                return code
    raise RuntimeError("local test login code is not configured")


def login(label, mobile):
    client_id = "wfc-local-" + label.lower() + "-" + uuid.uuid4().hex[:20]
    body = {"mobile": mobile, "code": get_super_code(), "clientId": client_id, "platform": 3}
    response = requests.post(APP_URL + "/login", json=body, timeout=20)
    response.raise_for_status()
    result = response.json()
    if result.get("code") != 0 or not result.get("result", {}).get("token"):
        raise RuntimeError(f"app login failed for {label}, business code {result.get('code')}")
    return {"label": label, "mobile": mobile, "clientId": client_id,
            "userId": result["result"]["userId"], "token": result["result"]["token"]}


def unpack_token(encoded):
    plain = decrypt(base64.b64decode(encoded), ROOT_KEY)
    parts = plain.decode("utf-8").split("|")
    if len(parts) != 3:
        raise ValueError("IM token has an unexpected format")
    return parts[0].encode("utf-8"), parts[1].encode("utf-8")


def emit(**event):
    print(json.dumps(event, ensure_ascii=False), flush=True)


def worker(identity):
    token, secret = unpack_token(identity["token"])
    sock = socket.create_connection((IM_HOST, IM_PORT), timeout=10)
    sock.settimeout(1)
    connect = (mqtt_str("MQTT") + bytes([4, 0xC2]) + struct.pack(">H", 30)
               + mqtt_str(identity["clientId"]) + mqtt_str(identity["userId"])
               + mqtt_str(encrypt(token, secret)))
    sock.sendall(packet(0x10, connect))
    kind, body = read_packet(sock)
    if kind != 0x20 or len(body) < 2 or body[1] != 0:
        raise RuntimeError(f"MQTT login rejected: packet={kind:#x}, code={body[1] if len(body)>1 else -1}")
    ack = pb_fields(body[2:])
    cursor = ack.get(1, [0])[0]
    emit(event="connected", label=identity["label"], pid=os.getpid(), userId=identity["userId"], head=cursor)
    commands = queue.Queue()

    def input_loop():
        for line in sys.stdin:
            try:
                commands.put(json.loads(line))
            except json.JSONDecodeError:
                pass
        commands.put({"action": "close"})

    threading.Thread(target=input_loop, daemon=True).start()
    pending = {}
    packet_id = 1
    last_ping = time.monotonic()
    try:
        while True:
            try:
                command = commands.get_nowait()
            except queue.Empty:
                command = None
            if command:
                if command["action"] == "close":
                    break
                if command["action"] == "send":
                    conv = pb_num(1, 0) + pb_data(2, command["to"]) + pb_num(3, 0)
                    content = pb_num(1, 1) + pb_data(2, command["text"]) + pb_num(8, 3)
                    msg = pb_data(1, conv) + pb_data(2, identity["userId"]) + pb_data(3, content)
                    body = mqtt_str("MS") + struct.pack(">H", packet_id) + encrypt(msg, secret)
                    sock.sendall(packet(0x32, body))
                    pending[packet_id] = "send"
                    packet_id = (packet_id % 65535) + 1
            try:
                kind, body = read_packet(sock)
            except socket.timeout:
                kind, body = 0, b""
            if kind >> 4 == 3:
                topic_len = struct.unpack(">H", body[:2])[0]
                topic = body[2:2 + topic_len].decode()
                payload = body[2 + topic_len:]
                if topic == "MN":
                    notice = pb_fields(payload)
                    emit(event="notification", label=identity["label"], head=notice.get(2, [0])[0])
                    request = pb_num(1, cursor) + pb_num(2, 0)
                    body = mqtt_str("MP") + struct.pack(">H", packet_id) + encrypt(request, secret)
                    sock.sendall(packet(0x32, body))
                    pending[packet_id] = "pull"
                    packet_id = (packet_id % 65535) + 1
            elif kind == 0x40:
                received_id = struct.unpack(">H", body[:2])[0]
                purpose = pending.pop(received_id, "unknown")
                result_code = body[2] if len(body) > 2 else -1
                if result_code != 0:
                    emit(event="error", label=identity["label"], operation=purpose, code=result_code)
                    continue
                data = decrypt(body[3:], secret) if len(body) > 3 else b""
                if purpose == "send":
                    message_id = struct.unpack(">Q", data[:8])[0] if len(data) >= 8 else 0
                    emit(event="sent", label=identity["label"], messageId=message_id)
                elif purpose == "pull":
                    pull = pb_fields(data)
                    cursor = pull.get(2, [cursor])[0]
                    for raw in pull.get(1, []):
                        msg = pb_fields(raw)
                        content = pb_fields(msg[3][0])
                        text = content.get(2, [b""])[0].decode("utf-8")
                        emit(event="message", label=identity["label"], fromUser=msg[2][0].decode(),
                             text=text, messageId=msg.get(4, [0])[0])
            if time.monotonic() - last_ping > 12:
                sock.sendall(packet(0xC0, b""))
                last_ping = time.monotonic()
    finally:
        try:
            sock.sendall(packet(0xE0, b""))
        except OSError:
            pass
        sock.close()


class ClientProcess:
    def __init__(self, identity):
        self.label = identity["label"]
        self.process = subprocess.Popen([sys.executable, __file__, "--worker"],
                                        stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                        stderr=subprocess.PIPE, text=True, encoding="utf-8",
                                        creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0)
        self.events = queue.Queue()
        self.send(identity)

        def reader():
            for line in self.process.stdout:
                try:
                    self.events.put(json.loads(line))
                except json.JSONDecodeError:
                    pass

        threading.Thread(target=reader, daemon=True).start()

    def send(self, command):
        self.process.stdin.write(json.dumps(command, ensure_ascii=False) + "\n")
        self.process.stdin.flush()

    def wait(self, name, timeout=20, expected_text=None):
        until = time.monotonic() + timeout
        while time.monotonic() < until:
            try:
                event = self.events.get(timeout=max(0.05, until - time.monotonic()))
            except queue.Empty:
                break
            if event["event"] == "error":
                raise RuntimeError(f"{self.label}: {event['operation']} failed, code {event['code']}")
            if event["event"] == name and (expected_text is None or event.get("text") == expected_text):
                return event
        raise TimeoutError(f"{self.label}: timed out waiting for {name}")

    def close(self):
        if self.process.poll() is None:
            try:
                self.send({"action": "close"})
                self.process.wait(timeout=4)
            except (BrokenPipeError, subprocess.TimeoutExpired):
                self.process.terminate()


def duplex():
    a_identity = login("A", "19900001001")
    b_identity = login("B", "19900001002")
    a = ClientProcess(a_identity)
    b = ClientProcess(b_identity)
    try:
        a_conn = a.wait("connected")
        b_conn = b.wait("connected")
        if a_conn["pid"] == b_conn["pid"] or a_identity["userId"] == b_identity["userId"]:
            raise RuntimeError("clients are not independent")
        print(f"A connected: PID {a_conn['pid']}, user {a_identity['userId']}")
        print(f"B connected: PID {b_conn['pid']}, user {b_identity['userId']}")
        challenge = "A-to-B-" + uuid.uuid4().hex[:12]
        response = "B-to-A-" + uuid.uuid4().hex[:12]
        a.send({"action": "send", "to": b_identity["userId"], "text": challenge})
        sent_a = a.wait("sent")
        received_b = b.wait("message", expected_text=challenge)
        if received_b["fromUser"] != a_identity["userId"]:
            raise RuntimeError("B received message from wrong sender")
        b.send({"action": "send", "to": a_identity["userId"], "text": response})
        sent_b = b.wait("sent")
        received_a = a.wait("message", expected_text=response)
        if received_a["fromUser"] != b_identity["userId"]:
            raise RuntimeError("A received reply from wrong sender")
        print(f"PASS A sent {challenge} (message {sent_a['messageId']}); B received it live")
        print(f"PASS B replied {response} (message {sent_b['messageId']}); A received it live")
        return 0
    finally:
        a.close()
        b.close()


def chat(label, peer):
    mobile = "19900001001" if label == "A" else "19900001002"
    identity = login(label, mobile)
    client = ClientProcess(identity)
    commands = queue.Queue()

    def input_loop():
        for line in sys.stdin:
            commands.put(line.rstrip("\r\n"))
        commands.put("/quit")

    try:
        event = client.wait("connected")
        print(f"{label} logged in to IM. User ID: {event['userId']}")
        print(f"IM: {IM_HOST}:{IM_PORT}. Type a message, /peer USER_ID, or /quit.")
        if peer:
            print(f"Peer: {peer}")
        threading.Thread(target=input_loop, daemon=True).start()
        while True:
            try:
                line = commands.get_nowait()
            except queue.Empty:
                line = None
            if line is not None:
                if line == "/quit":
                    break
                if line.startswith("/peer "):
                    peer = line[6:].strip()
                    print(f"Peer: {peer}")
                elif line and peer:
                    client.send({"action": "send", "to": peer, "text": line})
                elif line:
                    print("Set the peer first with /peer USER_ID")
            try:
                event = client.events.get(timeout=0.2)
            except queue.Empty:
                continue
            kind = event["event"]
            if kind == "message":
                peer = event["fromUser"]
                print(f"Received from {peer}: {event['text']} (message {event['messageId']})", flush=True)
            elif kind == "sent":
                print(f"Sent (message {event['messageId']})", flush=True)
            elif kind == "error":
                print(f"IM error: {event['operation']}, code {event['code']}", flush=True)
    finally:
        client.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--worker", action="store_true")
    parser.add_argument("--chat", choices=["A", "B"])
    parser.add_argument("--peer")
    args = parser.parse_args()
    if args.worker:
        identity = json.loads(sys.stdin.readline())
        worker(identity)
    elif args.chat:
        chat(args.chat, args.peer)
    else:
        return duplex()


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as exc:
        print(f"FAIL {type(exc).__name__}: {exc}", file=sys.stderr)
        sys.exit(1)
