#!/usr/bin/env python3
"""Compare the frozen legacy-asset ledger with the pinned E96 import receipt."""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import stat
import sys
from pathlib import Path
from typing import Any

from validate_legacy_asset_dispositions import DEFAULT_LEDGER, DEFAULT_RECEIPT, validate_ledger


ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "doc/integration-program/consolidation/current-tip-e96-evidence"
EXPECTED_RECEIPT_SHA256 = "06cd198a338d5c6d49fa6b0183bbda6b602252f39f622f18084e60342880bb75"


def load_pinned_receipt() -> dict[str, Any]:
    raw = gzip.decompress((EVIDENCE / "import-receipt.json.gz").read_bytes())
    if hashlib.sha256(raw).hexdigest() != EXPECTED_RECEIPT_SHA256:
        raise ValueError("E96 import receipt differs from its reviewed SHA-256")
    return json.loads(raw)


def compare_assets(ledger: dict[str, Any], receipt: dict[str, Any], pins: dict[str, str]) -> tuple[list[str], list[dict[str, str]]]:
    errors = validate_ledger(ledger, json.loads(DEFAULT_RECEIPT.read_text(encoding="utf-8")))
    if receipt.get("sourceCommits") != pins:
        errors.append("E96 import receipt source commits differ from the reviewed source pins")

    old = {(row["original_repository"], row["original_path"]): row for row in ledger["assets"]}
    current: dict[tuple[str, str], dict[str, Any]] = {}
    for row in receipt.get("mapping", []):
        if not isinstance(row, dict) or not str(row.get("destination", "")).endswith(".source"):
            continue
        key = (row.get("repository"), row.get("source"))
        if key in current:
            errors.append(f"duplicate current asset: {key}")
        current[key] = row

    if set(current) != set(old):
        errors.append(f"asset paths changed: added={sorted(set(current) - set(old))}, missing={sorted(set(old) - set(current))}")

    changed: list[dict[str, str]] = []
    for key in sorted(set(current) & set(old)):
        previous, now = old[key], current[key]
        if now.get("destination") != previous["mapped_path"] or now.get("mode") != previous["mode"]:
            errors.append(f"mapped path or mode changed: {key[0]}/{key[1]}")
        if now.get("blob") != previous["blob"]:
            changed.append({
                "repository": key[0],
                "path": key[1],
                "mappedPath": previous["mapped_path"],
                "previousBlob": previous["blob"],
                "currentBlob": str(now.get("blob", "")),
                "ledgerStatus": previous["status"],
            })
    return errors, changed


def verify_mapped_files(import_root: Path, receipt: dict[str, Any]) -> list[str]:
    """Check retained file bytes and executable bits in a materialized import tree."""
    errors: list[str] = []
    for row in receipt["mapping"]:
        destination = row["destination"]
        if not destination.endswith(".source"):
            continue
        path = import_root / destination
        if not path.is_file() or path.is_symlink():
            errors.append(f"missing or non-regular retained asset: {destination}")
            continue
        content = path.read_bytes()
        blob = hashlib.sha1(f"blob {len(content)}\0".encode() + content).hexdigest()
        mode = "100755" if path.stat().st_mode & stat.S_IXUSR else "100644"
        if blob != row["blob"] or mode != row["mode"]:
            errors.append(f"retained asset differs from import receipt: {destination}")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--import-root", type=Path, help="optional materialized history-import tree")
    args = parser.parse_args()
    try:
        ledger = json.loads(DEFAULT_LEDGER.read_text(encoding="utf-8"))
        receipt = load_pinned_receipt()
        profile = json.loads((EVIDENCE / "reviewed-overlays-six.json").read_text(encoding="utf-8"))
        errors, changed = compare_assets(ledger, receipt, profile["sourcePins"])
        if args.import_root:
            errors.extend(verify_mapped_files(args.import_root, receipt))
    except (OSError, ValueError, KeyError, TypeError, gzip.BadGzipFile) as error:
        print(f"Invalid evidence: {error}", file=sys.stderr)
        return 2
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1
    print(json.dumps({
        "sourceCommits": receipt["sourceCommits"],
        "retainedAssets": len(ledger["assets"]),
        "changedBlobs": changed,
        "materializedFilesVerified": args.import_root is not None,
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
