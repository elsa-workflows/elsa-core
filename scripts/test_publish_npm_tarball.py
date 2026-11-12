#!/usr/bin/env python3
"""Unit tests for publish_npm_tarball.py.

These tests mock npm so no real registry traffic occurs.
"""
from __future__ import annotations

import os
import sys
import unittest
import unittest.mock
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import publish_npm_tarball as publisher


class CompletedProcess:
    """Lightweight stand-in for subprocess.CompletedProcess."""

    def __init__(self, returncode: int, stdout: str = ""):
        self.returncode = returncode
        self.stdout = stdout


def _make_tarball() -> Path:
    path = Path(__file__).with_name("test_publish_npm_tarball.tgz")
    path.write_bytes(b"fake tarball")
    return path


class PublishNpmTarballTests(unittest.TestCase):
    def setUp(self):
        self.tarball = _make_tarball()
        self.local_integrity = publisher._local_integrity(self.tarball)
        self.env_patcher = unittest.mock.patch.dict(
            os.environ, {"NODE_AUTH_TOKEN": "feedz-token"}, clear=False
        )
        self.env_patcher.start()
        self.run_patcher = unittest.mock.patch.object(publisher.subprocess, "run")
        self.mock_run = self.run_patcher.start()

    def tearDown(self):
        self.run_patcher.stop()
        self.env_patcher.stop()
        if self.tarball.exists():
            self.tarball.unlink()

    def _run(self, argv: list[str]) -> int:
        with unittest.mock.patch.object(sys, "argv", ["publish_npm_tarball.py", *argv]):
            return publisher.main()

    def _publish_result(self, returncode: int, stdout: str = ""):
        self.mock_run.return_value = CompletedProcess(returncode, stdout)

    def _sequence(self, *results: tuple[int, str]):
        self.mock_run.side_effect = [CompletedProcess(rc, out) for rc, out in results]

    def test_successful_publish(self):
        self._publish_result(0, "+ @elsa-workflows/elsa-studio-wasm@3.10.0-rc1")
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 0)
        args = self.mock_run.call_args_list[0][0][0]
        self.assertIn("publish", args)
        self.assertIn(str(self.tarball), args)
        self.assertIn("--tag", args)
        self.assertIn("next", args)

    def test_successful_publish_with_verify(self):
        self._sequence(
            (0, "+ @elsa-workflows/elsa-studio-wasm@3.10.0-rc1"),
            (0, "{\"version\":\"3.10.0-rc1\"}"),
        )
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
            "--verify-published",
        ])
        self.assertEqual(code, 0)
        self.assertEqual(self.mock_run.call_count, 2)

    def test_already_published_integrity_matches(self):
        self._sequence(
            (1, "npm ERR! code EPUBLISHCONFLICT\nnpm ERR! publish fail"),
            (0, f'"{self.local_integrity}"'),
        )
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 0)
        # Second npm call must request dist.integrity.
        self.assertIn("dist.integrity", self.mock_run.call_args_list[1][0][0])

    def test_already_published_integrity_missing(self):
        self._sequence(
            (1, "npm ERR! code EPUBLISHCONFLICT"),
            (0, "null"),
        )
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 1)

    def test_already_published_integrity_mismatch(self):
        self._sequence(
            (1, "npm ERR! code EPUBLISHCONFLICT"),
            (0, '"sha512-differentvalue"'),
        )
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 1)

    def test_already_published_but_not_confirmed(self):
        self._sequence(
            (1, "npm ERR! code EPUBLISHCONFLICT"),
            (1, "npm ERR! 404 Not Found"),
        )
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 1)

    def test_auth_error(self):
        self._publish_result(1, "npm ERR! code E401\nnpm ERR! 401 Unauthorized")
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 1)

    def test_network_error(self):
        self._publish_result(1, "npm ERR! code ENOTFOUND\nnpm ERR! network error")
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 1)

    def test_unexpected_error(self):
        self._publish_result(1, "npm ERR! something unexpected happened")
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 1)

    def test_missing_token(self):
        self.env_patcher.stop()
        os.environ.pop("NODE_AUTH_TOKEN", None)
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
        ])
        self.assertEqual(code, 1)
        self.mock_run.assert_not_called()
        self.env_patcher.start()

    def test_verify_published_retries_then_succeeds(self):
        self._sequence(
            (0, "+ @elsa-workflows/elsa-studio-wasm@3.10.0-rc1"),
            (1, "not yet"),
            (1, "not yet"),
            (0, "{\"version\":\"3.10.0-rc1\"}"),
        )
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
            "--verify-published",
            "--retries", "3",
            "--retry-delay", "0",
        ])
        self.assertEqual(code, 0)
        self.assertEqual(self.mock_run.call_count, 4)

    def test_verify_published_retries_exhausted(self):
        self._sequence(
            (0, "+ @elsa-workflows/elsa-studio-wasm@3.10.0-rc1"),
            (1, "not yet"),
            (1, "not yet"),
            (1, "not yet"),
        )
        code = self._run([
            "--tarball", str(self.tarball),
            "--package", "@elsa-workflows/elsa-studio-wasm",
            "--version", "3.10.0-rc1",
            "--tag", "next",
            "--registry", "https://f.feedz.io/elsa-workflows/elsa-3/npm/",
            "--verify-published",
            "--retries", "2",
            "--retry-delay", "0",
        ])
        self.assertEqual(code, 1)
        self.assertEqual(self.mock_run.call_count, 4)


if __name__ == "__main__":
    unittest.main()
