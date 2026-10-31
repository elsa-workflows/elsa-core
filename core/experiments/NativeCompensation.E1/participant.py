#!/usr/bin/env python3
"""Independent synthetic participant for #8930. Delete with the E1 experiment."""
import argparse
import json
import sqlite3
from http.server import BaseHTTPRequestHandler, HTTPServer

parser = argparse.ArgumentParser()
parser.add_argument("database")
parser.add_argument("--no-dedupe", action="store_true")
parser.add_argument("--fail-undo", action="store_true")
options = parser.parse_args()
db = sqlite3.connect(options.database)
db.execute("PRAGMA journal_mode=WAL")
db.execute("PRAGMA synchronous=FULL")
db.executescript("""
CREATE TABLE IF NOT EXISTS operations (
    operation_key TEXT PRIMARY KEY, reservation_id TEXT NOT NULL, closed INTEGER NOT NULL DEFAULT 0,
    reserve_effects INTEGER NOT NULL DEFAULT 0, undo_effects INTEGER NOT NULL DEFAULT 0,
    undo_key TEXT, slot INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS calls (
    sequence INTEGER PRIMARY KEY AUTOINCREMENT, direction TEXT NOT NULL, operation_key TEXT NOT NULL,
    undo_key TEXT, disposition TEXT NOT NULL);
""")
db.commit()


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def do_POST(self):
        if self.headers.get("Transfer-Encoding", "").lower() == "chunked":
            chunks = []
            while True:
                size = int(self.rfile.readline().split(b";", 1)[0], 16)
                if size == 0:
                    while self.rfile.readline().strip():
                        pass
                    break
                chunks.append(self.rfile.read(size))
                if self.rfile.read(2) != b"\r\n":
                    raise ValueError("Invalid HTTP chunk terminator")
            payload = b"".join(chunks)
        else:
            payload = self.rfile.read(int(self.headers["Content-Length"]))
        body = json.loads(payload)
        key = body["operationKey"]
        direction = self.path.strip("/")
        status = 200
        with db:
            row = db.execute("SELECT * FROM operations WHERE operation_key=?", (key,)).fetchone()
            disposition = "deduplicated"
            if direction == "lookup":
                result = {"found": row is not None, "closed": bool(row[2]) if row else False}
            elif direction == "reserve":
                if row and row[2]:
                    status, disposition = 409, "closed-refused"
                    result = {"error": "closed"}
                else:
                    receipt = "reservation-" + key[:16]
                    if row is None:
                        db.execute("INSERT INTO operations VALUES (?, ?, 0, 1, 0, NULL, ?)", (key, receipt, body["request"]["Slot"]))
                        disposition = "effect"
                    elif options.no_dedupe:
                        db.execute("UPDATE operations SET reserve_effects=reserve_effects+1 WHERE operation_key=?", (key,))
                        disposition = "effect-dedupe-disabled"
                    result = {"reservationId": receipt, "operationKey": key}
            elif direction == "undo":
                if options.fail_undo:
                    status, disposition = 503, "synthetic-recovery-failure"
                    result = {"error": "synthetic recovery failure"}
                elif row is None or row[1] != body.get("reservationId"):
                    status, disposition = 409, "capture-mismatch"
                    result = {"error": "captured reservation required"}
                else:
                    if not row[2] or options.no_dedupe:
                        db.execute("UPDATE operations SET closed=1, undo_effects=undo_effects+1, undo_key=? WHERE operation_key=?", (body["undoKey"], key))
                        disposition = "effect" if not row[2] else "effect-dedupe-disabled"
                    elif row[5] != body["undoKey"]:
                        status, disposition = 409, "undo-key-mismatch"
                    result = {"reservationId": row[1], "closed": True, "undoKey": body["undoKey"]}
            else:
                status, disposition, result = 400, "unsupported", {"error": "unsupported"}
            db.execute("INSERT INTO calls(direction, operation_key, undo_key, disposition) VALUES (?, ?, ?, ?)", (direction, key, body.get("undoKey"), disposition))
        payload = json.dumps(result).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)


server = HTTPServer(("127.0.0.1", 0), Handler)
print(json.dumps({"port": server.server_port}), flush=True)
server.serve_forever()
