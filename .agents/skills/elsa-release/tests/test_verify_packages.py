from __future__ import annotations

import base64
import hashlib
import io
import json
import subprocess
import sys
import tarfile
import tempfile
import threading
import unittest
import zipfile
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlparse


SKILL_ROOT = Path(__file__).parents[1]
SCRIPT = SKILL_ROOT / "scripts" / "verify_packages.py"
SOURCE_COMMIT = "0123456789abcdef0123456789abcdef01234567"
VERSION = "9.4.0"


def nupkg(package_id: str, version: str, commit: str, *, signed: bool = False, dependency: str | None = None) -> bytes:
    nuspec = f'''<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>{package_id}</id><version>{version}</version>
    <repository type="git" url="https://github.com/example/repo" commit="{commit}" />
    <dependencies><group><dependency id="Elsa.Core" version="{dependency or version}" /></group></dependencies>
  </metadata>
</package>'''.encode()
    output = io.BytesIO()
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr(f"{package_id}.nuspec", nuspec)
        archive.writestr("lib/net8.0/package.dll", b"package payload")
        if signed:
            archive.writestr(".signature.p7s", b"different signing bytes")
    return output.getvalue()


def npm_tgz(name: str, version: str) -> bytes:
    output = io.BytesIO()
    with tarfile.open(fileobj=output, mode="w:gz") as archive:
        package = json.dumps({"name": name, "version": version}).encode()
        info = tarfile.TarInfo("package/package.json")
        info.size = len(package)
        archive.addfile(info, io.BytesIO(package))
    return output.getvalue()


class PackageRegistry:
    def __init__(self) -> None:
        self.nupkgs: dict[tuple[str, str, str], tuple[int, bytes]] = {}
        self.npm: dict[str, dict[str, object]] = {}
        self.tarballs: dict[str, bytes] = {}
        self.head_405_feeds: set[str] = set()
        self.server = ThreadingHTTPServer(("127.0.0.1", 0), self._handler())
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()

    @property
    def base_url(self) -> str:
        return f"http://127.0.0.1:{self.server.server_port}"

    def close(self) -> None:
        self.server.shutdown()
        self.thread.join(timeout=5)
        self.server.server_close()

    def _handler(self):
        registry = self

        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *_args) -> None:
                return

            def do_HEAD(self) -> None:
                path = unquote(urlparse(self.path).path)
                if path.startswith("/feed/"):
                    feed = path.split("/", 3)[2]
                    if feed in registry.head_405_feeds:
                        self.send_error(405)
                        return
                payload, status, content_type = registry.resolve(path)
                self.send_response(status)
                if payload is not None:
                    self.send_header("Content-Length", str(len(payload)))
                    self.send_header("Content-Type", content_type)
                self.end_headers()

            def do_GET(self) -> None:
                path = unquote(urlparse(self.path).path)
                payload, status, content_type = registry.resolve(path)
                self.send_response(status)
                if payload is not None:
                    self.send_header("Content-Length", str(len(payload)))
                    self.send_header("Content-Type", content_type)
                self.end_headers()
                if payload is not None:
                    self.wfile.write(payload)

        return Handler

    def resolve(self, path: str) -> tuple[bytes | None, int, str]:
        parts = path.strip("/").split("/")
        if len(parts) == 5 and parts[0] == "feed" and parts[4].endswith(".nupkg"):
            feed, package_id, version = parts[1], parts[2], parts[3]
            key = (feed, package_id.lower(), version.lower())
            if key not in self.nupkgs:
                return None, 404, "text/plain"
            return self.nupkgs[key][1], self.nupkgs[key][0], "application/octet-stream"
        if len(parts) >= 2 and parts[0] == "npm":
            # npm paths are /npm/<registry-name>/<version> and /npm/<name>.
            name = parts[1]
            if len(parts) == 3 and name in self.npm:
                payload = self.npm[name]["version_metadata"]
                return json.dumps(payload).encode(), 200, "application/json"
            if len(parts) == 2 and name in self.npm:
                payload = self.npm[name]["packument"]
                return json.dumps(payload).encode(), 200, "application/json"
        if len(parts) == 3 and parts[0] == "tarballs":
            payload = self.tarballs.get(parts[1] + "/" + parts[2])
            if payload is None:
                return None, 404, "text/plain"
            return payload, 200, "application/octet-stream"
        return None, 404, "text/plain"

    def add_nuget(self, feed: str, package_id: str, version: str, data: bytes, status: int = 200) -> None:
        self.nupkgs[(feed, package_id.lower(), version.lower())] = (status, data)

    def add_npm(self, name: str, version: str, data: bytes, tags: dict[str, str]) -> None:
        tarball_name = name.replace("@", "at-").replace("/", "-")
        tarball_path = f"{tarball_name}/{version}.tgz"
        self.tarballs[tarball_path] = data
        integrity = "sha512-" + base64.b64encode(hashlib.sha512(data).digest()).decode()
        self.npm[name] = {
            "version_metadata": {
                "name": name,
                "version": version,
                "dist": {"integrity": integrity, "tarball": f"{self.base_url}/tarballs/{tarball_path}"},
            },
            "packument": {"name": name, "dist-tags": tags},
        }


class VerifyPackagesTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.artifacts = self.root / "artifacts"
        self.artifacts.mkdir()
        self.registry = PackageRegistry()

    def tearDown(self) -> None:
        self.registry.close()
        self.temp.cleanup()

    def write_nupkg(self, name: str, data: bytes) -> None:
        (self.artifacts / name).write_bytes(data)

    def write_npm(self, name: str, data: bytes) -> None:
        (self.artifacts / name).write_bytes(data)

    def manifest(self, *, fixed: bool = True) -> dict[str, object]:
        nuget: list[dict[str, object]] = [
            {"id": "Elsa.Core"},
            {
                "id": "Elsa.SamplePackage",
                "version": "1.0.1",
                "verify_published": False,
                "reason": "sample package is intentionally fixed-version",
            },
        ]
        if not fixed:
            nuget.pop()
        return {
            "version": VERSION,
            "source_commit": SOURCE_COMMIT,
            "nuget": nuget,
            "feeds": [
                {"name": "feed-405", "base_url": f"{self.registry.base_url}/feed/feed-405"},
                {"name": "feed-ok", "base_url": f"{self.registry.base_url}/feed/feed-ok"},
            ],
            "npm": [{"name": "elsa-ui", "dist_tag": "latest", "registry": f"{self.registry.base_url}/npm"}],
            "expected_dependencies": {"Elsa.Core": VERSION},
        }

    def run_verify(self, manifest: dict[str, object]) -> tuple[int, dict[str, object]]:
        manifest_path = self.root / "manifest.json"
        report_path = self.root / "report.json"
        manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
        completed = subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--manifest",
                str(manifest_path),
                "--artifacts",
                str(self.artifacts),
                "--output",
                str(report_path),
                "--timeout",
                "3",
            ],
            text=True,
            capture_output=True,
            check=False,
        )
        self.assertTrue(report_path.exists(), completed.stderr)
        return completed.returncode, json.loads(report_path.read_text(encoding="utf-8"))

    def seed_success(self) -> None:
        core = nupkg("Elsa.Core", VERSION, SOURCE_COMMIT)
        sample = nupkg("Elsa.SamplePackage", "1.0.1", SOURCE_COMMIT, signed=True, dependency=VERSION)
        self.write_nupkg("Elsa.Core.9.4.0.nupkg", core)
        self.write_nupkg("Elsa.SamplePackage.1.0.1.nupkg", sample)
        for feed in ("feed-405", "feed-ok"):
            self.registry.add_nuget(feed, "Elsa.Core", VERSION, core)
        npm = npm_tgz("elsa-ui", VERSION)
        self.write_npm("elsa-ui-9.4.0.tgz", npm)
        self.registry.add_npm("elsa-ui", VERSION, npm, {"latest": VERSION, "next": "9.5.0"})
        self.registry.head_405_feeds.add("feed-405")

    def test_success_checks_both_feeds_signed_normalization_and_exact_latest_tag(self) -> None:
        self.seed_success()
        code, report = self.run_verify(self.manifest())
        self.assertEqual(code, 0, report)
        self.assertTrue(report["verified"])
        self.assertEqual(report["version"], VERSION)
        self.assertTrue(all(feed["verified"] for feed in report["nuget"]["feeds"]))
        self.assertTrue(report["npm"]["packages"][0]["verified"])

    def test_missing_artifact_and_unknown_extra_fail_with_partial_report(self) -> None:
        self.seed_success()
        (self.artifacts / "Elsa.Core.9.4.0.nupkg").unlink()
        self.write_nupkg("Unknown.9.4.0.nupkg", nupkg("Unknown", VERSION, SOURCE_COMMIT))
        code, report = self.run_verify(self.manifest())
        self.assertNotEqual(code, 0)
        self.assertFalse(report["verified"])
        self.assertIn("Elsa.Core", report["nuget"]["missing"])
        self.assertIn("Unknown", report["nuget"]["extra"])
        self.assertTrue(report["errors"])

    def test_wrong_commit_and_publish_lag_fail(self) -> None:
        self.seed_success()
        bad = nupkg("Elsa.Core", VERSION, "deadbeef")
        self.write_nupkg("Elsa.Core.9.4.0.nupkg", bad)
        self.registry.nupkgs.pop(("feed-ok", "elsa.core", VERSION.lower()))
        code, report = self.run_verify(self.manifest())
        self.assertNotEqual(code, 0)
        self.assertTrue(any("repository commit" in error for error in report["errors"]))
        feed_ok = next(feed for feed in report["nuget"]["feeds"] if feed["name"] == "feed-ok")
        core = next(package for package in feed_ok["packages"] if package["id"] == "Elsa.Core")
        self.assertEqual(core["status"], 404)

    def test_wrong_version_is_rejected(self) -> None:
        self.seed_success()
        wrong = nupkg("Elsa.Core", "9.3.0", SOURCE_COMMIT)
        (self.artifacts / "Elsa.Core.9.4.0.nupkg").write_bytes(wrong)
        code, report = self.run_verify(self.manifest())
        self.assertNotEqual(code, 0)
        self.assertTrue(any("nuspec version" in error for error in report["errors"]))

    def test_stable_release_rejects_prerelease_internal_dependency(self) -> None:
        self.write_nupkg(
            "Elsa.Core.9.4.0.nupkg",
            nupkg("Elsa.Core", VERSION, SOURCE_COMMIT, dependency="9.4.0-rc1"),
        )
        manifest = self.manifest(fixed=False)
        manifest["feeds"] = []
        code, report = self.run_verify(manifest)
        self.assertNotEqual(code, 0)
        self.assertTrue(any("stable dependency" in error for error in report["errors"]))

    def test_rc_and_preview_use_next_and_cannot_take_latest(self) -> None:
        for suffix in ("rc1", "preview.1"):
            with self.subTest(suffix=suffix):
                for artifact in self.artifacts.iterdir():
                    artifact.unlink()
                version = VERSION + "-" + suffix
                core = nupkg("Elsa.Core", version, SOURCE_COMMIT)
                npm = npm_tgz("elsa-ui", version)
                self.write_nupkg("Elsa.Core.nupkg", core)
                self.write_npm("elsa-ui.tgz", npm)
                for feed in ("feed-405", "feed-ok"):
                    self.registry.add_nuget(feed, "Elsa.Core", version, core)
                self.registry.add_npm("elsa-ui", version, npm, {"next": version, "latest": "9.3.0"})
                manifest = self.manifest(fixed=False)
                manifest["version"] = version
                manifest["expected_dependencies"] = {"Elsa.Core": version}
                for item in manifest["nuget"]:
                    item["version"] = version
                manifest["npm"][0]["dist_tag"] = "next"
                code, report = self.run_verify(manifest)
                self.assertEqual(0, code, report)
                self.registry.add_npm("elsa-ui", version, npm, {"next": version, "latest": version})
                code, report = self.run_verify(manifest)
                self.assertNotEqual(0, code)
                self.assertTrue(any("must not occupy" in error for error in report["errors"]))

    def test_fixed_skip_requires_reason_and_explicit_version(self) -> None:
        manifest = self.manifest(fixed=False)
        manifest["nuget"].append({"id": "Elsa.SamplePackage", "verify_published": False})
        manifest["feeds"] = []
        self.write_nupkg("Elsa.Core.9.4.0.nupkg", nupkg("Elsa.Core", VERSION, SOURCE_COMMIT))
        code, report = self.run_verify(manifest)
        self.assertNotEqual(code, 0)
        self.assertTrue(any("publication skips require" in error for error in report["errors"]))


if __name__ == "__main__":
    unittest.main()
