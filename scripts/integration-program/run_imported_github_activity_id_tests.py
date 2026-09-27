#!/usr/bin/env python3
"""Run the focused GitHub activity-ID tests inside the history-preserving import.

run_github_activity_id_compatibility.py replays the reviewed patch against a
pinned Extensions checkout and remains the reproduction path for that evidence.
This runner targets the imported layout instead: it proves the committed files
are exactly the reviewed patch after the documented path mapping, then runs the
focused tests in place. It never applies, rewrites or stages source.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Any

from run_github_activity_id_compatibility import (
    EXPECTED_TESTS,
    PATCH_PATH,
    REPOSITORY,
    ProofError,
    git_text,
    output_directory,
    read_test_counts,
    resolve_dotnet,
    sha256,
)


# test/extensions/modules/Directory.Build.props pins this single target framework.
FRAMEWORK = "net10.0"
BOM = b"\xef\xbb\xbf"
PATH_MAP = (
    ("src/modules/devops/", "src/extensions/devops/"),
    ("test/modules/devops/", "test/extensions/modules/devops/"),
)
# The imported test folder is one directory deeper than the Extensions layout.
REFERENCE_MAP = (
    b'Include="../../../../src/modules/devops/Elsa.DevOps.GitHub/Elsa.DevOps.GitHub.csproj"',
    b'Include="../../../../../src/extensions/devops/Elsa.DevOps.GitHub/Elsa.DevOps.GitHub.csproj"',
)
TEST_PROJECT = "test/extensions/modules/devops/Elsa.DevOps.GitHub.UnitTests/Elsa.DevOps.GitHub.UnitTests.csproj"
TRX_NAME = "github-activity-id.trx"


def imported_path(path: str) -> str:
    for old, new in PATH_MAP:
        if path.startswith(old):
            return new + path[len(old):]
    raise ProofError(f"Reviewed patch touches an unmapped path: {path}")


def reviewed_files(patch: bytes) -> dict[str, bytes]:
    """Return every file the reviewed patch creates, keyed by its imported path."""
    files: dict[str, bytes] = {}
    for section in re.split(rb"^diff --git ", patch, flags=re.M)[1:]:
        header, separator, body = section.partition(b"\n@@ -0,0 +1")
        lines = header.splitlines()
        if not separator or b"new file mode 100644" not in lines or b"--- /dev/null" not in lines:
            raise ProofError("The reviewed patch may only add new regular files")
        targets = [line[len(b"+++ b/"):].decode("utf-8") for line in lines if line.startswith(b"+++ b/")]
        if len(targets) != 1:
            raise ProofError("Cannot identify the file created by a reviewed patch section")
        hunk_header, _, hunk = body.partition(b"\n")
        expected_lines = int(hunk_header.split(b" @@")[0].lstrip(b",") or b"1")
        content: list[bytes] = []
        final_newline = True
        for line in hunk.split(b"\n"):
            if line.startswith(b"+"):
                content.append(line[1:])
            elif line == b"\\ No newline at end of file":
                final_newline = False
            elif line:
                raise ProofError(f"Unexpected line in new-file hunk for {targets[0]}")
        if len(content) != expected_lines:
            raise ProofError(f"Hunk line count mismatch for {targets[0]}")
        data = b"\n".join(content) + (b"\n" if final_newline else b"")
        path = imported_path(targets[0])
        if path.endswith(".csproj"):
            old, new = REFERENCE_MAP
            if data.count(old) != 1:
                raise ProofError(f"Expected exactly one mapped GitHub ProjectReference in {path}")
            data = data.replace(old, new)
        if path in files:
            raise ProofError(f"Reviewed patch creates {path} more than once")
        files[path] = data
    if not files:
        raise ProofError("Reviewed patch creates no files")
    return files


def port_differences(source: Path, expected: dict[str, bytes]) -> list[dict[str, Any]]:
    """Compare imported bytes with reviewed content; only a leading UTF-8 BOM may differ."""
    rows: list[dict[str, Any]] = []
    for path, content in sorted(expected.items()):
        file = source / path
        if file.is_symlink() or not file.is_file():
            rows.append({"path": path, "matches": False, "reason": "missing"})
            continue
        actual = file.read_bytes()
        bom = actual.startswith(BOM)
        rows.append({
            "path": path,
            "sha256": sha256(file),
            "bom": bom,
            "matches": (actual[len(BOM):] if bom else actual) == content,
        })
    return rows


def run(args: argparse.Namespace) -> int:
    source = args.source_tree.resolve(strict=True)
    output = output_directory(args.output_dir.expanduser(), (source,))
    receipt: dict[str, Any] = {
        "schemaVersion": 1,
        "scope": "Focused GitHub activity-ID tests in the imported source layout; no package publication",
        "status": "failed",
        "test": {"framework": FRAMEWORK, "expectedCount": EXPECTED_TESTS},
        "limitations": [
            "The imported test project targets net10.0 only; net8.0/net9.0 V2 contracts are covered by the activity compatibility probe, not by these tests.",
            "Restore uses the imported tree's NuGet configuration and the ambient package cache; this is not a fresh-cache or feed-provenance attestation.",
            "No GitHub API call, credential, package pack or publisher is used.",
        ],
    }
    log_path = output / "net10-dotnet-test.log"
    results_dir = output / "net10-results"
    try:
        status = git_text(source, "status", "--porcelain", "--untracked-files=all")
        if status:
            raise ProofError(f"Imported source has uncommitted changes:\n{status}")
        receipt["source"] = {"head": git_text(source, "rev-parse", "HEAD"), "cleanBeforeTest": True}
        patch = PATCH_PATH.read_bytes()
        port = port_differences(source, reviewed_files(patch))
        receipt["port"] = {
            "patch": {
                "path": PATCH_PATH.relative_to(REPOSITORY).as_posix(),
                "sha256": hashlib.sha256(patch).hexdigest(),
            },
            "pathMap": [list(pair) for pair in PATH_MAP],
            "projectReferenceMap": [value.decode("utf-8") for value in REFERENCE_MAP],
            "files": port,
        }
        if not all(row["matches"] for row in port):
            raise ProofError("Imported GitHub activity-ID files differ from the reviewed patch")
        dotnet = resolve_dotnet(args.dotnet)
        results_dir.mkdir()
        command = [
            str(dotnet), "test", TEST_PROJECT, "--framework", FRAMEWORK,
            "--logger", f"trx;LogFileName={TRX_NAME}", "--results-directory", str(results_dir),
        ]
        with log_path.open("xb") as log_file:
            try:
                exit_code = subprocess.run(
                    command, cwd=source, stdout=log_file, stderr=subprocess.STDOUT,
                    check=False, timeout=args.timeout_seconds,
                ).returncode
            except subprocess.TimeoutExpired:
                exit_code = 124
                log_file.write(f"\nTimed out after {args.timeout_seconds} seconds.\n".encode())
        receipt["test"].update({
            "command": ["$DOTNET", *command[1:-1], "<output>/net10-results"],
            "exitCode": exit_code,
            "logPath": log_path.name,
            "logSha256": sha256(log_path),
        })
        trx_path = results_dir / TRX_NAME
        if trx_path.is_file():
            receipt["test"].update(read_test_counts(trx_path))
            receipt["test"]["trxPath"] = trx_path.relative_to(output).as_posix()
            receipt["test"]["trxSha256"] = sha256(trx_path)
        after = git_text(source, "status", "--porcelain", "--untracked-files=all")
        receipt["source"]["cleanAfterTest"] = not after
        if exit_code == 0 and receipt["test"].get("passed") is True and not after:
            receipt["status"] = "passed"
    except (OSError, ProofError, subprocess.SubprocessError) as error:
        receipt["error"] = str(error)

    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"status": receipt["status"], "receipt": str(receipt_path)}, sort_keys=True))
    if receipt["status"] != "passed":
        if "error" in receipt:
            print(receipt["error"], file=sys.stderr)
        return 1
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-tree", type=Path, required=True, help="Clean checkout of the history-preserving import")
    parser.add_argument("--output-dir", type=Path, required=True, help="New output directory outside the source tree")
    parser.add_argument("--dotnet", default="dotnet", help="dotnet executable or path")
    parser.add_argument("--timeout-seconds", type=int, default=1800)
    args = parser.parse_args(argv)
    if args.timeout_seconds < 1:
        parser.error("--timeout-seconds must be positive")
    return run(args)


if __name__ == "__main__":
    raise SystemExit(main())
