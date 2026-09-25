#!/usr/bin/env python3
"""Compare the frozen legacy-asset ledger with the pinned E96 import receipt."""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import re
import stat
import sys
from pathlib import Path
from typing import Any

from validate_legacy_asset_dispositions import DEFAULT_LEDGER, DEFAULT_RECEIPT, validate_ledger


ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "doc/integration-program/consolidation/current-tip-e96-evidence"
STUDIO_SPEC_REPRESENTATION = ROOT / "doc/integration-program/consolidation/studio-spec-asset-representation.json"
STUDIO_GUIDANCE = "src/studio/AGENTS.md"
ROOT_STUDIO_INSTRUCTION = re.compile(
    r"\[`src/studio/AGENTS\.md`\]\(src/studio/AGENTS\.md\);\s*read it for Studio modules"
)
EXPECTED_RECEIPT_SHA256 = "06cd198a338d5c6d49fa6b0183bbda6b602252f39f622f18084e60342880bb75"


def file_git_identity(path: Path) -> tuple[str, str]:
    content = path.read_bytes()
    blob = hashlib.sha1(f"blob {len(content)}\0".encode() + content).hexdigest()
    mode = "100755" if path.stat().st_mode & stat.S_IXUSR else "100644"
    return blob, mode


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
        blob, mode = file_git_identity(path)
        if blob != row["blob"] or mode != row["mode"]:
            errors.append(f"retained asset differs from import receipt: {destination}")
    return errors


def compare_studio_spec_representation(
    ledger: dict[str, Any], receipt: dict[str, Any], decision: dict[str, Any], core_root: Path = ROOT
) -> tuple[list[str], dict[str, Any]]:
    """Prove which retained Studio tooling assets already exist byte-for-byte in Core."""
    errors: list[str] = []
    rows = [row for row in ledger["assets"] if row["category"] == "studio_agent_specification_tooling"]
    mapped = {(row["repository"], row["source"]): row for row in receipt["mapping"]}
    if decision.get("schemaVersion") != 2 or decision.get("category") != "studio_agent_specification_tooling":
        errors.append("Studio tooling decision schema or category changed")
    if decision.get("sourcePins") != receipt.get("sourceCommits"):
        errors.append("Studio tooling decision source pins differ from the E96 receipt")

    represented: list[str] = []
    different: list[str] = []
    active: dict[str, tuple[str, str]] = {}
    for row in rows:
        source_path = row["original_path"]
        current = mapped.get(("studio", source_path))
        if current is None or current.get("destination") != row["mapped_path"]:
            errors.append(f"Studio tooling source mapping changed: {source_path}")
            continue
        path = core_root / source_path
        if not path.is_file() or path.is_symlink():
            errors.append(f"Active Core tooling file is missing or not regular: {source_path}")
            continue
        blob, mode = file_git_identity(path)
        active[source_path] = (blob, mode)
        (represented if blob == current["blob"] and mode == current["mode"] else different).append(source_path)

    differences = decision.get("sourceDifferences")
    reviewed: list[str] = []
    policy_pending: list[str] = []
    if not isinstance(differences, dict) or set(differences) != set(different):
        errors.append("Studio tooling source-difference paths differ from the source comparison")
    else:
        allowed = {"represented_by_active_core", "represented_by_scoped_studio", "pending_policy"}
        for source_path, record in differences.items():
            if not isinstance(record, dict):
                errors.append(f"Studio tooling reviewed difference changed: {source_path}")
                continue
            status, reason = record.get("status"), record.get("reason")
            if (status not in allowed or not isinstance(reason, str) or not reason.strip()
                    or (record.get("activeBlob"), record.get("activeMode")) != active[source_path]):
                errors.append(f"Studio tooling reviewed difference changed: {source_path}")
                continue
            if status == "represented_by_scoped_studio":
                scoped_path = core_root / STUDIO_GUIDANCE
                root_guidance = core_root / "AGENTS.md"
                if (record.get("scopedPath") != STUDIO_GUIDANCE or not scoped_path.is_file()
                        or scoped_path.is_symlink() or
                        (record.get("scopedBlob"), record.get("scopedMode")) != file_git_identity(scoped_path)):
                    errors.append(f"Studio scoped guidance changed: {source_path}")
                    continue
                if (not root_guidance.is_file() or root_guidance.is_symlink() or
                        not ROOT_STUDIO_INSTRUCTION.search(root_guidance.read_text(encoding="utf-8"))):
                    errors.append(f"Root guidance no longer links to Studio policy: {source_path}")
                    continue
            elif any(key in record for key in ("scopedPath", "scopedBlob", "scopedMode")):
                errors.append(f"Unexpected Studio scoped guidance mapping: {source_path}")
                continue
            (policy_pending if status == "pending_policy" else reviewed).append(source_path)
        if (decision.get("reviewedDifferentCount") != len(reviewed)
                or decision.get("pendingPolicyCount") != len(policy_pending)):
            errors.append("Studio tooling reviewed/pending difference counts changed")
    if decision.get("representedCount") != len(represented) or len(rows) != len(represented) + len(different):
        errors.append("Studio tooling represented count does not match the source comparison")
    return errors, {"total": len(rows), "representedByIdenticalCoreRoot": len(represented),
                    "reviewedDifferentPaths": sorted(reviewed), "pendingPolicyPaths": sorted(policy_pending)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--import-root", type=Path, help="optional materialized history-import tree")
    args = parser.parse_args()
    try:
        ledger = json.loads(DEFAULT_LEDGER.read_text(encoding="utf-8"))
        receipt = load_pinned_receipt()
        profile = json.loads((EVIDENCE / "reviewed-overlays-six.json").read_text(encoding="utf-8"))
        errors, changed = compare_assets(ledger, receipt, profile["sourcePins"])
        studio_decision = json.loads(STUDIO_SPEC_REPRESENTATION.read_text(encoding="utf-8"))
        studio_errors, studio_summary = compare_studio_spec_representation(ledger, receipt, studio_decision)
        errors.extend(studio_errors)
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
        "studioSpecRepresentation": studio_summary,
        "materializedFilesVerified": args.import_root is not None,
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
