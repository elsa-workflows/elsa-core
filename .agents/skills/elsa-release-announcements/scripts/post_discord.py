#!/usr/bin/env python3
"""Post an approved release announcement to Discord via webhook."""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Any


def main() -> int:
    args = parse_args()
    webhook_url = args.webhook_url or os.getenv("DISCORD_RELEASE_WEBHOOK_URL")
    if not webhook_url:
        print("error: provide --webhook-url or DISCORD_RELEASE_WEBHOOK_URL", file=sys.stderr)
        return 1

    message = Path(args.message_file).expanduser().read_text(encoding="utf-8").strip()
    if not message:
        print("error: message file is empty", file=sys.stderr)
        return 1

    bot_token = args.bot_token or os.getenv("DISCORD_BOT_TOKEN")
    if args.crosspost and not bot_token:
        print("error: provide --bot-token or DISCORD_BOT_TOKEN when using --crosspost", file=sys.stderr)
        return 1

    payload = json.dumps({"content": message[:2000], "flags": 4}).encode("utf-8")
    status, body = post_payload(webhook_url, payload, wait=args.crosspost)
    if status not in (200, 204):
        print(f"error: Discord returned HTTP {status}", file=sys.stderr)
        return 1

    if args.crosspost:
        message_data = parse_json_body(body)
        channel_id = message_data.get("channel_id")
        message_id = message_data.get("id")
        if not channel_id or not message_id:
            print("error: Discord did not return a message id for crossposting", file=sys.stderr)
            return 1

        crosspost_status, crosspost_body = crosspost_message(channel_id, message_id, bot_token or "")
        if crosspost_status not in (200, 204):
            print(f"error: Discord crosspost returned HTTP {crosspost_status}", file=sys.stderr)
            if crosspost_body:
                print(crosspost_body, file=sys.stderr)
            return 1

    if args.crosspost:
        print("Posted and published Discord announcement.")
    else:
        print("Posted Discord announcement.")
    return 0


def post_payload(webhook_url: str, payload: bytes, *, wait: bool) -> tuple[int, str]:
    url = append_query(webhook_url, {"wait": "true"}) if wait else webhook_url
    if shutil.which("curl"):
        return post_with_curl(url, payload)

    request = urllib.request.Request(
        url,
        data=payload,
        headers={
            "Content-Type": "application/json",
            "User-Agent": "ElsaReleaseAnnouncements/1.0",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status, response.read().decode("utf-8", errors="replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", errors="replace")


def post_with_curl(webhook_url: str, payload: bytes) -> tuple[int, str]:
    with tempfile.NamedTemporaryFile() as payload_file, tempfile.NamedTemporaryFile() as body_file:
        payload_file.write(payload)
        payload_file.flush()

        # Feed curl its config via stdin so the webhook URL does not appear in
        # command output or shell history.
        curl_config = "\n".join(
            [
                f'url = "{webhook_url}"',
                'request = "POST"',
                'header = "Content-Type: application/json"',
                f'data-binary = "@{payload_file.name}"',
                'write-out = "%{http_code}"',
                "silent",
                "show-error",
                f'output = "{body_file.name}"',
            ]
        )
        result = subprocess.run(
            ["curl", "--config", "-"],
            input=curl_config,
            text=True,
            capture_output=True,
            check=False,
        )
        body_file.seek(0)
        body = body_file.read().decode("utf-8", errors="replace")

    if result.returncode != 0:
        print(result.stderr.strip() or "error: curl failed", file=sys.stderr)
        return 0, ""

    return int(result.stdout.strip()), body


def crosspost_message(channel_id: str, message_id: str, bot_token: str) -> tuple[int, str]:
    url = f"https://discord.com/api/v10/channels/{channel_id}/messages/{message_id}/crosspost"
    if shutil.which("curl"):
        with tempfile.NamedTemporaryFile() as body_file:
            curl_config = "\n".join(
                [
                    f'url = "{url}"',
                    'request = "POST"',
                    f'header = "Authorization: Bot {bot_token}"',
                    'write-out = "%{http_code}"',
                    "silent",
                    "show-error",
                    f'output = "{body_file.name}"',
                ]
            )
            result = subprocess.run(
                ["curl", "--config", "-"],
                input=curl_config,
                text=True,
                capture_output=True,
                check=False,
            )
            body_file.seek(0)
            body = body_file.read().decode("utf-8", errors="replace")

        if result.returncode != 0:
            print(result.stderr.strip() or "error: curl failed", file=sys.stderr)
            return 0, ""
        return int(result.stdout.strip()), body

    request = urllib.request.Request(
        url,
        headers={
            "Authorization": f"Bot {bot_token}",
            "User-Agent": "ElsaReleaseAnnouncements/1.0",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status, response.read().decode("utf-8", errors="replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", errors="replace")


def append_query(url: str, query: dict[str, str]) -> str:
    parts = urllib.parse.urlsplit(url)
    existing = dict(urllib.parse.parse_qsl(parts.query))
    existing.update(query)
    return urllib.parse.urlunsplit(
        (parts.scheme, parts.netloc, parts.path, urllib.parse.urlencode(existing), parts.fragment)
    )


def parse_json_body(body: str) -> dict[str, Any]:
    try:
        parsed = json.loads(body)
    except json.JSONDecodeError:
        return {}
    return parsed if isinstance(parsed, dict) else {}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--message-file", required=True, help="Approved Discord message file.")
    parser.add_argument("--webhook-url", help="Discord webhook URL. Prefer DISCORD_RELEASE_WEBHOOK_URL.")
    parser.add_argument("--crosspost", action="store_true", help="Publish the created message from an Announcement Channel.")
    parser.add_argument("--bot-token", help="Discord bot token for crossposting. Prefer DISCORD_BOT_TOKEN.")
    return parser.parse_args()


if __name__ == "__main__":
    raise SystemExit(main())
