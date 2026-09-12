from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import os
import tempfile
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit


SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "post_discord.py"
SPEC = importlib.util.spec_from_file_location("post_discord", SCRIPT)
assert SPEC and SPEC.loader
post_discord = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(post_discord)


class ServerState:
    def __init__(self) -> None:
        self.messages: dict[str, dict[str, object]] = {}
        self.create_count = 0
        self.list_count = 0
        self.crosspost_count = 0
        self.ambiguous_once = False
        self.create_response_mode: str | None = None
        self.crosspost_failures = 0


class DiscordHandler(BaseHTTPRequestHandler):
    server: "DiscordServer"

    def log_message(self, *_: object) -> None:
        pass

    def _json(self, status: int, value: object) -> None:
        body = json.dumps(value).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _body(self) -> dict[str, object]:
        length = int(self.headers.get("Content-Length", "0"))
        return json.loads(self.rfile.read(length) or b"{}")

    def do_POST(self) -> None:
        path = urlsplit(self.path).path
        state = self.server.state
        body = self._body()
        if path == "/api/v10/channels/123/messages":
            state.create_count += 1
            message = {
                "id": "message-1",
                "channel_id": "123",
                "content": body["content"],
                "flags": body["flags"],
                "nonce": body["nonce"],
            }
            state.messages["message-1"] = message
            if state.ambiguous_once:
                state.ambiguous_once = False
                self._json(500, {"message": "simulated connection ambiguity"})
                return
            if state.create_response_mode == "invalid-json":
                state.create_response_mode = None
                body = b"not-json"
                self.send_response(200)
                self.send_header("Content-Type", "application/json")
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)
                return
            if state.create_response_mode == "missing-id":
                state.create_response_mode = None
                self._json(200, {key: value for key, value in message.items() if key != "id"})
                return
            self._json(200, message)
            return
        if path == "/api/webhooks/123/secret-token":
            state.create_count += 1
            message = {
                "id": "message-1",
                "channel_id": "123",
                "content": body["content"],
                "flags": body["flags"],
                "nonce": body["nonce"],
            }
            state.messages["message-1"] = message
            self._json(200, message)
            return
        if path == "/api/v10/channels/123/messages/message-1/crosspost":
            state.crosspost_count += 1
            if state.crosspost_failures:
                state.crosspost_failures -= 1
                self._json(500, {"message": "simulated crosspost failure"})
                return
            state.messages["message-1"]["flags"] = 5
            self._json(200, state.messages["message-1"])
            return
        self._json(404, {"message": "not found"})

    def do_GET(self) -> None:
        path = urlsplit(self.path).path
        state = self.server.state
        if path == "/api/v10/channels/123/messages":
            state.list_count += 1
            self._json(200, list(state.messages.values()))
            return
        if path == "/api/v10/channels/123/messages/message-1":
            message = state.messages.get("message-1")
            self._json(200 if message else 404, message or {"message": "not found"})
            return
        if path == "/api/webhooks/123/secret-token/messages/message-1":
            message = state.messages.get("message-1")
            self._json(200 if message else 404, message or {"message": "not found"})
            return
        self._json(404, {"message": "not found"})


class DiscordServer(ThreadingHTTPServer):
    def __init__(self) -> None:
        super().__init__(("127.0.0.1", 0), DiscordHandler)
        self.state = ServerState()

    @property
    def api_base(self) -> str:
        return f"http://127.0.0.1:{self.server_port}/api/v10"

    @property
    def webhook_url(self) -> str:
        return f"http://127.0.0.1:{self.server_port}/api/webhooks/123/secret-token"


@contextlib.contextmanager
def running_server() -> DiscordServer:
    server = DiscordServer()
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        yield server
    finally:
        server.shutdown()
        thread.join(timeout=5)
        server.server_close()


class PostDiscordTests(unittest.TestCase):
    def setUp(self) -> None:
        self.original_api_base = post_discord.API_BASE_URL
        self.original_token = os.environ.get("DISCORD_BOT_TOKEN_SUPPORT")
        os.environ["DISCORD_BOT_TOKEN_SUPPORT"] = "test-token"

    def tearDown(self) -> None:
        post_discord.API_BASE_URL = self.original_api_base
        if self.original_token is None:
            os.environ.pop("DISCORD_BOT_TOKEN_SUPPORT", None)
        else:
            os.environ["DISCORD_BOT_TOKEN_SUPPORT"] = self.original_token

    def invoke(self, *args: str) -> tuple[int, str, str]:
        stdout = io.StringIO()
        stderr = io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            result = post_discord.main(list(args))
        return result, stdout.getvalue(), stderr.getvalue()

    def message_file(self, directory: str, content: str = "hello") -> str:
        path = Path(directory) / "announcement.md"
        path.write_text(content, encoding="utf-8")
        return str(path)

    def execute_args(self, message: str, state: str, channel: str = "123") -> tuple[str, ...]:
        return (
            "--message-file",
            message,
            "--channel-id",
            channel,
            "--state-file",
            state,
            "--execute",
        )

    def test_dry_run_is_default_and_has_safe_payload(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            message = self.message_file(directory, "hello @everyone")
            result, output, error = self.invoke("--message-file", message, "--channel-id", "123")
            self.assertEqual(result, 0, error)
            data = json.loads(output)
            self.assertTrue(data["dryRun"])
            self.assertEqual(data["payload"]["allowed_mentions"], {"parse": []})
            self.assertEqual(data["payload"]["flags"], 4)
            self.assertNotIn("@everyone", data["payload"]["allowed_mentions"])
            self.assertNotIn("DISCORD_BOT_TOKEN_SUPPORT", output)

    def test_oversize_message_is_rejected_without_truncation(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            message = self.message_file(directory, "x" * 2001)
            result, output, error = self.invoke("--message-file", message, "--channel-id", "123")
            self.assertEqual(result, 1)
            self.assertEqual(output, "")
            self.assertIn("maximum is 2000", error)

    def test_execute_requires_state_file(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            message = self.message_file(directory)
            result, _, error = self.invoke(
                "--message-file", message, "--channel-id", "123", "--execute"
            )
            self.assertEqual(result, 1)
            self.assertIn("--state-file is required", error)

    def test_held_state_lock_refuses_then_reacquires_after_release(self) -> None:
        with running_server() as server, tempfile.TemporaryDirectory() as directory:
            post_discord.API_BASE_URL = server.api_base
            message = self.message_file(directory)
            state = Path(directory) / "state.json"
            lock = post_discord.FileLock(post_discord.lock_path(state))
            with lock:
                result, _, error = self.invoke(*self.execute_args(message, str(state)))
                self.assertEqual(result, 1)
                self.assertIn("state lock is held", error)
                self.assertEqual(server.state.create_count, 0)
            self.assertTrue(Path(str(state) + ".lock").exists())
            result, _, error = self.invoke(*self.execute_args(message, str(state)))
            self.assertEqual(result, 0, error)
            self.assertEqual(server.state.create_count, 1)

    def test_failed_crosspost_resumes_without_duplicate_create(self) -> None:
        with running_server() as server, tempfile.TemporaryDirectory() as directory:
            post_discord.API_BASE_URL = server.api_base
            server.state.crosspost_failures = 1
            message = self.message_file(directory)
            state = str(Path(directory) / "state.json")
            result, _, error = self.invoke(*self.execute_args(message, state), "--crosspost")
            self.assertEqual(result, 1)
            self.assertIn("HTTP 500", error)
            saved = json.loads(Path(state).read_text(encoding="utf-8"))
            self.assertEqual(saved["messageId"], "message-1")
            self.assertEqual(saved["status"], "created")
            self.assertEqual(server.state.create_count, 1)

            result, output, error = self.invoke(*self.execute_args(message, state), "--crosspost")
            self.assertEqual(result, 0, error)
            self.assertEqual(json.loads(output)["status"], "verified")
            self.assertEqual(server.state.create_count, 1)
            self.assertEqual(server.state.crosspost_count, 2)

    def test_ambiguous_create_reconciles_by_nonce_without_repost(self) -> None:
        with running_server() as server, tempfile.TemporaryDirectory() as directory:
            post_discord.API_BASE_URL = server.api_base
            server.state.ambiguous_once = True
            message = self.message_file(directory)
            state = str(Path(directory) / "state.json")
            result, _, error = self.invoke(*self.execute_args(message, state))
            self.assertEqual(result, 1)
            self.assertIn("may have completed", error)
            self.assertEqual(json.loads(Path(state).read_text(encoding="utf-8"))["status"], "ambiguous")

            result, output, error = self.invoke(*self.execute_args(message, state))
            self.assertEqual(result, 0, error)
            self.assertEqual(json.loads(output)["messageId"], "message-1")
            self.assertEqual(server.state.create_count, 1)
            self.assertGreaterEqual(server.state.list_count, 1)

    def test_ambiguous_create_without_match_stops_without_repost(self) -> None:
        with running_server() as server, tempfile.TemporaryDirectory() as directory:
            post_discord.API_BASE_URL = server.api_base
            server.state.ambiguous_once = True
            message = self.message_file(directory)
            state = str(Path(directory) / "state.json")
            first, _, _ = self.invoke(*self.execute_args(message, state))
            self.assertEqual(first, 1)
            server.state.messages.clear()
            second, _, error = self.invoke(*self.execute_args(message, state))
            self.assertEqual(second, 1)
            self.assertIn("no repost was attempted", error)
            self.assertEqual(server.state.create_count, 1)

    def test_unusable_create_responses_reconcile_without_repost(self) -> None:
        for response_mode in ("invalid-json", "missing-id"):
            with (
                self.subTest(response_mode=response_mode),
                running_server() as server,
                tempfile.TemporaryDirectory() as directory,
            ):
                post_discord.API_BASE_URL = server.api_base
                server.state.create_response_mode = response_mode
                message = self.message_file(directory)
                state = str(Path(directory) / "state.json")
                result, _, error = self.invoke(*self.execute_args(message, state))
                self.assertEqual(result, 1)
                self.assertIn("may have completed", error)
                self.assertEqual(json.loads(Path(state).read_text(encoding="utf-8"))["status"], "ambiguous")

                result, _, error = self.invoke(*self.execute_args(message, state))
                self.assertEqual(result, 0, error)
                self.assertEqual(server.state.create_count, 1)

    def test_changed_payload_or_target_is_rejected(self) -> None:
        with running_server() as server, tempfile.TemporaryDirectory() as directory:
            post_discord.API_BASE_URL = server.api_base
            message = self.message_file(directory, "original")
            state = str(Path(directory) / "state.json")
            result, _, error = self.invoke(*self.execute_args(message, state))
            self.assertEqual(result, 0, error)
            Path(message).write_text("changed", encoding="utf-8")
            result, _, error = self.invoke(*self.execute_args(message, state))
            self.assertEqual(result, 1)
            self.assertIn("does not match", error)
            result, _, error = self.invoke(*self.execute_args(message, state, channel="999"))
            self.assertEqual(result, 1)
            self.assertIn("does not match", error)
            self.assertEqual(server.state.create_count, 1)

    def test_successful_rerun_does_not_create_duplicate(self) -> None:
        with running_server() as server, tempfile.TemporaryDirectory() as directory:
            post_discord.API_BASE_URL = server.api_base
            message = self.message_file(directory)
            state = str(Path(directory) / "state.json")
            first, _, error = self.invoke(*self.execute_args(message, state))
            self.assertEqual(first, 0, error)
            second, _, error = self.invoke(*self.execute_args(message, state))
            self.assertEqual(second, 0, error)
            self.assertEqual(server.state.create_count, 1)

    def test_webhook_dry_run_does_not_expose_webhook_url(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            message = self.message_file(directory)
            webhook = "https://discord.com/api/webhooks/123/secret-token"
            result, output, error = self.invoke("--message-file", message, "--webhook-url", webhook)
            self.assertEqual(result, 0, error)
            self.assertEqual(json.loads(output)["mode"], "webhook")
            self.assertNotIn(webhook, output)
            self.assertNotIn("secret-token", output)

    def test_webhook_execute_reuses_state_without_storing_secret(self) -> None:
        with running_server() as server, tempfile.TemporaryDirectory() as directory:
            message = self.message_file(directory)
            state = Path(directory) / "state.json"
            args = (
                "--message-file",
                message,
                "--webhook-url",
                server.webhook_url,
                "--state-file",
                str(state),
                "--execute",
            )
            result, _, error = self.invoke(*args)
            self.assertEqual(result, 0, error)
            result, _, error = self.invoke(*args)
            self.assertEqual(result, 0, error)
            self.assertEqual(server.state.create_count, 1)
            saved = state.read_text(encoding="utf-8")
            self.assertNotIn("secret-token", saved)
            self.assertNotIn("test-token", saved)


if __name__ == "__main__":
    unittest.main()
