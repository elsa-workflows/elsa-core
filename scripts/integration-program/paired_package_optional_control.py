"""Private bounded socket control for owned optional-feature browser probes.

No control bytes are portable evidence. Callbacks may observe metadata or stop
the captured backend; only their separately verified results enter receipts.
"""
from contextlib import contextmanager
import json
import secrets
import socket
import threading
import time


def _object(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise ValueError("Duplicate private control field")
        value[key] = item
    return value


class OptionalProbeControl:
    def __init__(self, scenario, on_event, *, timeout=240):
        from paired_package_optional_features import SCENARIOS
        if scenario not in SCENARIOS or not callable(on_event) or not 0 < timeout <= 300:
            raise ValueError("Invalid optional control configuration")
        self.events = (("disconnect-ready", "begin-native-action") if scenario == "disconnect"
                       else ("begin-native-action",))
        self.parent, self.child = socket.socketpair(socket.AF_UNIX, socket.SOCK_STREAM)
        self.parent.settimeout(0.2)
        self.nonce = secrets.token_hex(32)
        self.on_event, self.timeout = on_event, timeout
        self.completed_events = []
        self.failure = False
        self.complete = False
        self.started = False
        self.stop = threading.Event()

    def descriptor(self):
        if self.started or self.child.fileno() < 3:
            raise RuntimeError("Optional control descriptor unavailable")
        return {"fd": self.child.fileno(), "nonce": self.nonce}

    def _read_line(self, deadline):
        value = bytearray()
        while not self.stop.is_set() and time.monotonic() < deadline:
            try:
                chunk = self.parent.recv(1)
            except socket.timeout:
                continue
            if not chunk:
                if value:
                    raise ValueError("Truncated optional control message")
                return None
            value.extend(chunk)
            if len(value) > 512:
                raise ValueError("Optional control message exceeded bound")
            if chunk == b"\n":
                return json.loads(value.decode("utf-8"), object_pairs_hook=_object)
        raise TimeoutError("Optional control did not finish")

    def _serve(self, process):
        deadline = time.monotonic() + self.timeout
        try:
            for sequence, event in enumerate(self.events, 1):
                message = self._read_line(deadline)
                expected = {"schema": 1, "nonce": self.nonce, "seq": sequence, "event": event}
                if (not isinstance(message, dict) or message != expected
                        or type(message.get("schema")) is not int or type(message.get("seq")) is not int
                        or process.poll() is not None):
                    raise ValueError("Unexpected optional control message")
                self.on_event(event)
                if self.stop.is_set() or time.monotonic() >= deadline or process.poll() is not None:
                    raise RuntimeError("Optional control owner ended before acknowledgement")
                self.parent.sendall((json.dumps(expected | {"ack": True}, separators=(",", ":")) + "\n").encode())
                self.completed_events.append(event)
            # Require peer closure: extra, duplicated or unfinished exchanges fail.
            if self._read_line(deadline) is not None:
                raise ValueError("Unexpected trailing optional control message")
            self.complete = True
        except Exception:
            self.failure = True
            self._shutdown()

    def _shutdown(self):
        try:
            self.parent.shutdown(socket.SHUT_RDWR)
        except OSError:
            pass

    @contextmanager
    def session(self, process):
        if self.started:
            raise RuntimeError("Optional control is one-shot")
        self.started = True
        # Popen has inherited the child endpoint. Close the parent's duplicate,
        # otherwise a dead child would never produce EOF on the control socket.
        self.child.close()
        worker = threading.Thread(target=self._serve, args=(process,), daemon=True)
        worker.start()
        body_failed = True
        try:
            yield
            body_failed = False
        finally:
            if body_failed:
                self.stop.set()
                self._shutdown()
            # Callback implementations are bounded; no acknowledgement or portable
            # success may escape while an unfinished callback could still mutate.
            worker.join(timeout=60)
            self.stop.set()
            self._shutdown()
            self.parent.close()
            if worker.is_alive() or self.failure or not self.complete:
                raise RuntimeError("Optional control or cleanup was not verified") from None

    def close(self):
        self.stop.set()
        self._shutdown()
        self.parent.close()
        self.child.close()
