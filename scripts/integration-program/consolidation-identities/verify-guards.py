#!/usr/bin/env python3
"""Exercise the identity evaluator's output-path and dirty-input guards."""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import tempfile
from pathlib import Path


def run(command: list[str], cwd: Path | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=cwd, text=True, capture_output=True, check=False, timeout=120)


def audit_command(args: argparse.Namespace, extensions_root: Path, output: Path) -> list[str]:
    return [
        args.dotnet,
        str(args.audit_dll),
        "--consolidated-root",
        str(args.consolidated_root),
        "--extensions-root",
        str(extensions_root),
        "--studio-root",
        str(args.studio_root),
        "--import-receipt",
        str(args.import_receipt),
        "--build-receipt",
        str(args.build_receipt),
        "--inventory",
        str(args.inventory),
        "--output",
        str(output),
        "--configuration",
        "Release",
    ]


def expect_guard(args: argparse.Namespace, extensions_root: Path, output: Path, expected: str) -> None:
    result = run(audit_command(args, extensions_root, output))
    transcript = result.stdout + result.stderr
    if result.returncode == 0:
        raise AssertionError(f"Evaluator unexpectedly succeeded for guard probe: {expected}")
    if expected not in transcript:
        raise AssertionError(f"Expected guard text {expected!r} was not in evaluator output:\n{transcript}")
    if output.exists():
        raise AssertionError(f"A rejected guard probe created an output file: {output}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--audit-dll", required=True, type=Path)
    parser.add_argument("--consolidated-root", required=True, type=Path)
    parser.add_argument("--extensions-root", required=True, type=Path)
    parser.add_argument("--studio-root", required=True, type=Path)
    parser.add_argument("--import-receipt", required=True, type=Path)
    parser.add_argument("--build-receipt", required=True, type=Path)
    parser.add_argument("--inventory", required=True, type=Path)
    parser.add_argument("--dotnet", default=shutil.which("dotnet") or "dotnet")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    for path in (args.audit_dll, args.consolidated_root, args.extensions_root, args.studio_root, args.import_receipt, args.build_receipt, args.inventory):
        if not path.exists():
            raise FileNotFoundError(path)

    with tempfile.TemporaryDirectory(prefix="elsa-identity-guard-probes-") as temporary:
        temporary_root = Path(temporary)
        alias_output = args.extensions_root / f"identity-guard-alias-{temporary_root.name}.json"
        expect_guard(args, args.extensions_root, alias_output, "Output must be outside inspected input root")
        print("PASS: output path beneath a pinned source root was rejected before writing")

        receipt = json.loads(args.import_receipt.read_text(encoding="utf-8"))
        extensions_pin = receipt["sourceCommits"]["extensions"]
        dirty_worktree = temporary_root / "dirty-extensions"
        add_worktree = run(["git", "-C", str(args.extensions_root), "worktree", "add", "--detach", "--quiet", str(dirty_worktree), extensions_pin])
        if add_worktree.returncode != 0:
            raise RuntimeError(f"Could not create disposable source fixture worktree:\n{add_worktree.stderr}")

        try:
            props = dirty_worktree / "Directory.Build.props"
            props.write_bytes(props.read_bytes() + b"\n<!-- identity audit dirty-input guard fixture -->\n")
            dirty_output = temporary_root / "dirty-input-report.json"
            expect_guard(args, dirty_worktree, dirty_output, "Tracked files in extensions are modified")
            print("PASS: dirty Directory.Build.props in disposable source worktree was rejected before evaluation")
        finally:
            remove_worktree = run(["git", "-C", str(args.extensions_root), "worktree", "remove", "--force", str(dirty_worktree)])
            if remove_worktree.returncode != 0:
                raise RuntimeError(f"Could not remove disposable source fixture worktree:\n{remove_worktree.stderr}")
            run(["git", "-C", str(args.extensions_root), "worktree", "prune"])


if __name__ == "__main__":
    main()
