#!/usr/bin/env python3
"""Post an approved Discord announcement with safe, resumable delivery."""

from __future__ import annotations

import argparse
import errno
# The release workflow currently targets macOS/Linux; fcntl gives crash-safe advisory locks there.
import fcntl
import hashlib
import json
import os
import socket
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Any


API_BASE_URL = "https://discord.com/api/v10"
STATE_VERSION = 1
SUPPRESS_EMBEDS = 4
CROSSPOSTED = 1
MAX_CONTENT_LENGTH = 2000


class AnnouncementError(Exception):
    """An expected, actionable announcement failure."""


class AmbiguousRequest(AnnouncementError):
    """The server may have accepted a request before the client lost its response."""


class FileLock:
    def __init__(self, path: Path):
        self.path = path
        self._file: Any | None = None

    def __enter__(self) -> "FileLock":
        try:
            self._file = self.path.open("a+")
            fcntl.flock(self._file.fileno(), fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError as exc:
            if self._file is not None:
                self._file.close()
                self._file = None
            if exc.errno in {errno.EACCES, errno.EAGAIN}:
                raise AnnouncementError(
                    f"state lock is held: {self.path}; inspect the other publisher before retrying"
                ) from exc
            raise AnnouncementError(f"could not acquire state lock {self.path}: {exc}") from exc
        return self

    def __exit__(self, *_: object) -> None:
        if self._file is not None:
            fcntl.flock(self._file.fileno(), fcntl.LOCK_UN)
            self._file.close()
            self._file = None


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        return run(args)
    except AnnouncementError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1


def run(args: argparse.Namespace) -> int:
    content = read_message(args.message_file)
    mode, webhook_url = resolve_mode(args)
    target = resolve_target(args, mode, webhook_url)
    content_hash = hashlib.sha256(content.encode("utf-8")).hexdigest()
    operation_nonce = operation_nonce_for(mode, target, args.crosspost, content_hash)
    payload = {
        "content": content,
        "allowed_mentions": {"parse": []},
        "flags": SUPPRESS_EMBEDS,
        "nonce": operation_nonce,
        "enforce_nonce": True,
    }

    if not args.execute:
        print(
            json.dumps(
                {
                    "dryRun": True,
                    "mode": mode,
                    "target": target,
                    "operationNonce": operation_nonce,
                    "crosspost": args.crosspost,
                    "payload": payload,
                },
                indent=2,
                sort_keys=True,
            )
        )
        return 0

    if not args.state_file:
        raise AnnouncementError("--state-file is required with --execute")

    token = resolve_token(args, mode)
    state_path = Path(args.state_file).expanduser().resolve()
    state_path.parent.mkdir(parents=True, exist_ok=True)
    with FileLock(lock_path(state_path)):
        state_was_new = not state_path.exists()
        state = load_or_initialize_state(
            state_path,
            mode=mode,
            target=target,
            crosspost=args.crosspost,
            content=content,
            content_hash=content_hash,
            operation_nonce=operation_nonce,
        )
        message_id = state.get("messageId")
        if not state_was_new and state["status"] in {"intent", "ambiguous"}:
            state, message_id = reconcile_ambiguous_create(
                state_path,
                state,
                mode=mode,
                target=target,
                token=token,
            )

        if not message_id:
            state["status"] = "intent"
            state.pop("lastError", None)
            atomic_write_json(state_path, state)
            try:
                response = create_message(
                    mode=mode,
                    target=target,
                    webhook_url=webhook_url,
                    token=token,
                    payload=payload,
                )
            except AmbiguousRequest as exc:
                state["status"] = "ambiguous"
                state["lastError"] = str(exc)
                atomic_write_json(state_path, state)
                raise
            except AnnouncementError as exc:
                state["status"] = "failed"
                state["lastError"] = str(exc)
                atomic_write_json(state_path, state)
                raise

            message_id = require_message_id(response)
            state["messageId"] = message_id
            state["channelId"] = response.get("channel_id") or target.get("channelId")
            if not state["channelId"]:
                state["status"] = "ambiguous"
                state["lastError"] = "Discord did not return a channel id; the create may have completed"
                atomic_write_json(state_path, state)
                raise AmbiguousRequest("Discord did not return a channel id; the create may have completed")
            state["status"] = "created"
            state.pop("lastError", None)
            # This write must happen before any crosspost request.
            atomic_write_json(state_path, state)

        initial_message = verify_message(
            mode=mode,
            target=target,
            webhook_url=webhook_url,
            token=token,
            message_id=str(message_id),
            state=state,
            content=content,
            require_crosspost=False,
        )

        if args.crosspost and not state.get("crossposted", False):
            if int(initial_message.get("flags", 0)) & CROSSPOSTED:
                state["crossposted"] = True
                state["status"] = "crossposted"
                state.pop("lastError", None)
                atomic_write_json(state_path, state)
            else:
                try:
                    crosspost_message(str(state["channelId"]), str(message_id), token)
                except AnnouncementError as exc:
                    state["status"] = "created"
                    state["lastError"] = str(exc)
                    atomic_write_json(state_path, state)
                    raise
                state["crossposted"] = True
                state["status"] = "crossposted"
                state.pop("lastError", None)
                atomic_write_json(state_path, state)

        verify_message(
            mode=mode,
            target=target,
            webhook_url=webhook_url,
            token=token,
            message_id=str(message_id),
            state=state,
            content=content,
            require_crosspost=args.crosspost,
        )
        state["status"] = "verified"
        state.pop("lastError", None)
        atomic_write_json(state_path, state)

    print(
        json.dumps(
            {
                "status": "verified",
                "mode": mode,
                "messageId": str(message_id),
                "operationNonce": operation_nonce,
                "crossposted": bool(args.crosspost),
            },
            sort_keys=True,
        )
    )
    return 0


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--message-file", required=True, help="Approved Discord message file.")
    parser.add_argument("--webhook-url", help="Discord webhook URL. Prefer DISCORD_RELEASE_WEBHOOK_URL.")
    parser.add_argument("--channel-id", help="Discord channel ID for direct bot mode.")
    parser.add_argument(
        "--bot-token-env",
        default="DISCORD_BOT_TOKEN_SUPPORT",
        help="Environment variable containing the bot token (default: DISCORD_BOT_TOKEN_SUPPORT).",
    )
    parser.add_argument("--bot-token", help="Bot token override; never written to state.")
    parser.add_argument("--crosspost", action="store_true", help="Publish the message from an Announcement Channel.")
    parser.add_argument("--state-file", help="JSON checkpoint file; required with --execute.")
    parser.add_argument("--execute", action="store_true", help="Post the approved message.")
    return parser.parse_args(argv)


def read_message(path_value: str) -> str:
    try:
        content = Path(path_value).expanduser().read_text(encoding="utf-8").strip()
    except OSError as exc:
        raise AnnouncementError(f"could not read message file: {exc}") from exc
    if not content:
        raise AnnouncementError("message file is empty")
    if len(content) > MAX_CONTENT_LENGTH:
        raise AnnouncementError(f"Discord content is {len(content)} characters; the maximum is {MAX_CONTENT_LENGTH}")
    return content


def resolve_mode(args: argparse.Namespace) -> tuple[str, str | None]:
    if args.channel_id and args.webhook_url:
        raise AnnouncementError("choose either --channel-id or --webhook-url, not both")
    if args.channel_id:
        return "bot", None
    webhook_url = args.webhook_url or os.getenv("DISCORD_RELEASE_WEBHOOK_URL")
    if webhook_url:
        return "webhook", webhook_url
    raise AnnouncementError("provide --channel-id or --webhook-url/DISCORD_RELEASE_WEBHOOK_URL")


def resolve_target(args: argparse.Namespace, mode: str, webhook_url: str | None) -> dict[str, str]:
    if mode == "bot":
        channel_id = str(args.channel_id).strip()
        if not channel_id:
            raise AnnouncementError("--channel-id cannot be empty")
        return {"kind": "channel", "channelId": channel_id}
    assert webhook_url is not None
    return {
        "kind": "webhook",
        "fingerprint": hashlib.sha256(webhook_url.encode("utf-8")).hexdigest(),
    }


def resolve_token(args: argparse.Namespace, mode: str) -> str | None:
    if not args.execute:
        return None
    token = args.bot_token or os.getenv(args.bot_token_env)
    needs_token = mode == "bot" or args.crosspost
    if needs_token and not token:
        raise AnnouncementError(f"provide --bot-token or set {args.bot_token_env}")
    return token


def operation_nonce_for(mode: str, target: dict[str, str], crosspost: bool, content_hash: str) -> str:
    canonical_target = json.dumps(target, sort_keys=True, separators=(",", ":"))
    material = f"{mode}|{canonical_target}|{int(crosspost)}|{content_hash}"
    return "elsa-release-" + hashlib.sha256(material.encode("utf-8")).hexdigest()[:32]


def load_or_initialize_state(
    path: Path,
    *,
    mode: str,
    target: dict[str, str],
    crosspost: bool,
    content: str,
    content_hash: str,
    operation_nonce: str,
) -> dict[str, Any]:
    if not path.exists():
        state: dict[str, Any] = {
            "schemaVersion": STATE_VERSION,
            "status": "intent",
            "mode": mode,
            "target": target,
            "crosspost": crosspost,
            "contentSha256": content_hash,
            "contentLength": len(content),
            "operationNonce": operation_nonce,
            "messageId": None,
            "channelId": target.get("channelId"),
            "crossposted": False,
        }
        atomic_write_json(path, state)
        return state

    try:
        state = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise AnnouncementError(f"could not read state file {path}: {exc}") from exc
    if not isinstance(state, dict):
        raise AnnouncementError(f"state file {path} must contain a JSON object")
    expected = {
        "schemaVersion": STATE_VERSION,
        "mode": mode,
        "target": target,
        "crosspost": crosspost,
        "contentSha256": content_hash,
        "operationNonce": operation_nonce,
    }
    for key, value in expected.items():
        if state.get(key) != value:
            raise AnnouncementError(f"state file {path} does not match the current payload or target ({key})")
    if state.get("status") not in {"intent", "ambiguous", "failed", "created", "crossposted", "verified"}:
        raise AnnouncementError(f"state file {path} has unsupported status {state.get('status')!r}")
    return state


def reconcile_ambiguous_create(
    path: Path,
    state: dict[str, Any],
    *,
    mode: str,
    target: dict[str, str],
    token: str | None,
) -> tuple[dict[str, Any], str | None]:
    if mode != "bot":
        raise AnnouncementError(
            f"state file {path} records an unresolved create; webhook mode cannot list messages by nonce. "
            "Inspect Discord before deleting the state file; no repost was attempted."
        )
    assert token is not None
    url = channel_messages_url(target["channelId"]) + "?limit=100"
    try:
        messages = request_json(url, method="GET", token=token)
    except AnnouncementError as exc:
        raise AnnouncementError(f"could not reconcile nonce {state['operationNonce']}: {exc}") from exc
    if not isinstance(messages, list):
        raise AnnouncementError("Discord returned an invalid message list while reconciling the unresolved create")
    matches = [message for message in messages if str(message.get("nonce")) == state["operationNonce"]]
    if len(matches) > 1:
        raise AnnouncementError(
            f"multiple Discord messages match nonce {state['operationNonce']}; inspect before retrying"
        )
    if not matches:
        raise AnnouncementError(
            f"state file {path} records an unresolved create and no message with nonce {state['operationNonce']} "
            "was found. Inspect Discord and reconcile manually; no repost was attempted."
        )
    message = matches[0]
    message_hash = hashlib.sha256(str(message.get("content", "")).encode("utf-8")).hexdigest()
    if message_hash != state["contentSha256"] or message.get("channel_id") != target["channelId"]:
        raise AnnouncementError("the message found by nonce does not match the saved target or content")
    state["messageId"] = require_message_id(message)
    state["channelId"] = target["channelId"]
    state["status"] = "created"
    state.pop("lastError", None)
    atomic_write_json(path, state)
    return state, str(state["messageId"])


def create_message(
    *, mode: str, target: dict[str, str], webhook_url: str | None, token: str | None, payload: dict[str, Any]
) -> dict[str, Any]:
    if mode == "bot":
        response = request_json(
            channel_messages_url(target["channelId"]), method="POST", token=token, payload=payload, ambiguous=True
        )
    else:
        assert webhook_url is not None
        response = request_json(
            append_query(webhook_url, {"wait": "true"}), method="POST", token=None, payload=payload, ambiguous=True
        )
    if not isinstance(response, dict) or not response.get("id"):
        raise AmbiguousRequest("Discord create response had no message id; the create may have completed")
    return response


def crosspost_message(channel_id: str, message_id: str, token: str | None) -> dict[str, Any]:
    if not token:
        raise AnnouncementError("a bot token is required for crossposting")
    return request_json(crosspost_url(channel_id, message_id), method="POST", token=token, payload={})


def verify_message(
    *,
    mode: str,
    target: dict[str, str],
    webhook_url: str | None,
    token: str | None,
    message_id: str,
    state: dict[str, Any],
    content: str,
    require_crosspost: bool,
) -> dict[str, Any]:
    if mode == "bot" or require_crosspost:
        message_url = channel_message_url(str(state["channelId"]), message_id)
        message = request_json(message_url, method="GET", token=token)
    else:
        assert webhook_url is not None
        message = request_json(webhook_message_url(webhook_url, message_id), method="GET", token=None)
    if not isinstance(message, dict):
        raise AnnouncementError("Discord returned an invalid message while verifying the announcement")
    if message.get("content") != content:
        raise AnnouncementError("Discord message content does not match the approved payload")
    if message.get("channel_id") != state.get("channelId"):
        raise AnnouncementError("Discord message target channel does not match the saved target")
    try:
        flags = int(message.get("flags", 0))
    except (TypeError, ValueError) as exc:
        raise AnnouncementError("Discord returned invalid message flags") from exc
    if not flags & SUPPRESS_EMBEDS:
        raise AnnouncementError("Discord message is missing the required suppress-embeds flag")
    if require_crosspost and not flags & CROSSPOSTED:
        raise AnnouncementError("Discord message is not crossposted")
    return message


def request_json(
    url: str,
    *,
    method: str,
    token: str | None = None,
    payload: dict[str, Any] | None = None,
    ambiguous: bool = False,
) -> Any:
    data = None if payload is None else json.dumps(payload, separators=(",", ":")).encode("utf-8")
    headers = {"User-Agent": "ElsaReleaseAnnouncements/2.0", "Accept": "application/json"}
    if data is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bot {token}"
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            raw = response.read()
            status = response.status
    except urllib.error.HTTPError as exc:
        if ambiguous and exc.code >= 500:
            raise AmbiguousRequest(f"Discord returned HTTP {exc.code}; the create may have completed") from exc
        raise AnnouncementError(f"Discord returned HTTP {exc.code}") from exc
    except (urllib.error.URLError, TimeoutError, socket.timeout, ConnectionError) as exc:
        if ambiguous:
            raise AmbiguousRequest(f"Discord connection ended before create completion: {type(exc).__name__}") from exc
        raise AnnouncementError(f"Discord request failed: {type(exc).__name__}") from exc
    if not raw:
        return {}
    try:
        return json.loads(raw.decode("utf-8"))
    except json.JSONDecodeError as exc:
        if ambiguous:
            raise AmbiguousRequest("Discord returned invalid create JSON; the create may have completed") from exc
        raise AnnouncementError(f"Discord returned invalid JSON (HTTP {status})") from exc


def require_message_id(message: Any) -> str:
    if not isinstance(message, dict) or not message.get("id"):
        raise AnnouncementError("Discord did not return a message id")
    return str(message["id"])


def api_url(path: str) -> str:
    return API_BASE_URL.rstrip("/") + path


def channel_messages_url(channel_id: str) -> str:
    return api_url(f"/channels/{urllib.parse.quote(channel_id, safe='')}/messages")


def channel_message_url(channel_id: str, message_id: str) -> str:
    return channel_messages_url(channel_id) + f"/{urllib.parse.quote(message_id, safe='')}"


def crosspost_url(channel_id: str, message_id: str) -> str:
    return channel_message_url(channel_id, message_id) + "/crosspost"


def webhook_message_url(webhook_url: str, message_id: str) -> str:
    parts = urllib.parse.urlsplit(webhook_url)
    path = parts.path.rstrip("/") + f"/messages/{urllib.parse.quote(message_id, safe='')}"
    return urllib.parse.urlunsplit((parts.scheme, parts.netloc, path, "", ""))


def append_query(url: str, query: dict[str, str]) -> str:
    parts = urllib.parse.urlsplit(url)
    existing = dict(urllib.parse.parse_qsl(parts.query))
    existing.update(query)
    return urllib.parse.urlunsplit(
        (parts.scheme, parts.netloc, parts.path, urllib.parse.urlencode(existing), parts.fragment)
    )


def lock_path(state_path: Path) -> Path:
    return state_path.with_name(state_path.name + ".lock")


def atomic_write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary: str | None = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w", encoding="utf-8", dir=path.parent, prefix=f".{path.name}.", suffix=".tmp", delete=False
        ) as handle:
            temporary = handle.name
            json.dump(value, handle, indent=2, sort_keys=True)
            handle.write("\n")
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(temporary, path)
        temporary = None
        try:
            directory_fd = os.open(path.parent, os.O_RDONLY)
        except OSError:
            directory_fd = None
        if directory_fd is not None:
            try:
                os.fsync(directory_fd)
            finally:
                os.close(directory_fd)
    except OSError as exc:
        raise AnnouncementError(f"could not atomically write state file {path}: {exc}") from exc
    finally:
        if temporary:
            try:
                os.unlink(temporary)
            except FileNotFoundError:
                pass


if __name__ == "__main__":
    raise SystemExit(main())
