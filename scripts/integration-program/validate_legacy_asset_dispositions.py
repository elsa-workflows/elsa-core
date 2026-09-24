#!/usr/bin/env python3
"""Validate the retained legacy-asset ledger against an import receipt."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from collections import Counter
from pathlib import Path, PurePosixPath
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_LEDGER = ROOT / "doc/integration-program/consolidation/legacy-asset-dispositions.json"
DEFAULT_RECEIPT = Path(__file__).resolve().parent / "fixtures/legacy_asset_disposition_receipt.json"
EXPECTED_PINS = {
    "core": "076f022cc174d497af26fc8e26414970e61a79b1",
    "extensions": "33fa0bfd28c7585240e3d4f665058c067b17e287",
    "studio": "9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822",
}
EXPECTED_ASSET_COUNTS = {"extensions": 80, "studio": 83}
EXPECTED_ASSET_TOTAL = sum(EXPECTED_ASSET_COUNTS.values())
EXPECTED_SOURCE_RECEIPT_SHA256 = "5716731dfff80733dfd1e9ca1aaa814c237ceec672e9fa60ab8c9af080611a75"
EXPECTED_RECEIPT_FIXTURE_SHA256 = "5465904a66768403844a3ef8ed9e69f49ee7294f116ac7faf3ed657b0c408026"
SUPPORTED_GIT_MODES = {"100644", "100755"}
ALLOWED_PENDING_STATUSES = {
    "candidate_rewritten_in_disposable_build",
    "candidate_scoped_in_disposable_patch",
    "candidate_shared_asset_pending_pack_check",
    "pending_canonical_build_integration",
    "pending_canonical_solution_and_ci",
    "pending_canonical_solution_and_target_set",
    "pending_central_ci_and_publisher_design",
    "pending_compatibility_gate",
    "pending_devex_and_release_review",
    "pending_devex_inventory",
    "pending_devex_review",
    "pending_document_archive",
    "pending_document_path_review",
    "pending_document_refresh",
    "pending_document_relocation",
    "pending_feed_key_and_restore_review",
    "pending_notice_review",
    "pending_package_vs_project_reference_validation",
    "pending_path_scope_review",
    "pending_restore_and_feed_review",
    "pending_root_configuration_merge",
    "pending_root_document_update",
    "pending_root_instruction_merge",
    "pending_template_path_and_usage_check",
    "pending_tooling_configuration_review",
    "publisher_must_remain_inert",
    "ready_for_explicit_retirement",
}
ASSET_FIELDS = (
    "original_repository",
    "original_path",
    "mapped_path",
    "source_commit",
    "blob",
    "mode",
    "category",
    "owning_workstream",
    "proposed_disposition",
    "evidence_or_gate",
    "status",
)
RECEIPT_FIELDS = ("original_repository", "original_path", "mapped_path", "source_commit", "blob", "mode")


def _read_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"{path} must contain a JSON object")
    return value


def _asset_key(row: dict[str, Any]) -> tuple[str, str]:
    return row.get("original_repository", ""), row.get("original_path", "")


def _is_normalized_relative_path(value: Any) -> bool:
    if not isinstance(value, str) or not value or "\\" in value:
        return False
    path = PurePosixPath(value)
    return not path.is_absolute() and path.as_posix() == value and all(part not in {"", ".", ".."} for part in path.parts)


def _receipt_provenance_error(receipt: dict[str, Any]) -> str | None:
    provenance = receipt.get("provenance")
    if provenance is None:
        return None
    if not isinstance(provenance, dict):
        return "receipt provenance must be a JSON object"
    source_sha = provenance.get("sourceReceiptSha256")
    if source_sha != EXPECTED_SOURCE_RECEIPT_SHA256:
        return "frozen receipt fixture does not retain the pinned source receipt SHA-256"
    return None


def validate_ledger(ledger: dict[str, Any], receipt: dict[str, Any] | None = None) -> list[str]:
    errors: list[str] = []
    if ledger.get("schema_version") != 1:
        errors.append("ledger schema_version must be 1")
    if ledger.get("recorded_source_commits") != EXPECTED_PINS:
        errors.append("ledger source pins do not match the recorded Core/Extensions/Studio rehearsal")
    expected_counts = {**EXPECTED_ASSET_COUNTS, "total": EXPECTED_ASSET_TOTAL}
    if ledger.get("asset_counts") != expected_counts:
        errors.append(f"ledger asset_counts are {ledger.get('asset_counts')}; expected {expected_counts}")

    assets = ledger.get("assets")
    if not isinstance(assets, list):
        return errors + ["ledger assets must be a JSON array"]
    if len(assets) != EXPECTED_ASSET_TOTAL:
        errors.append(f"ledger has {len(assets)} assets; expected {EXPECTED_ASSET_TOTAL}")

    counts: Counter[str] = Counter()
    keys: set[tuple[str, str]] = set()
    mapped_paths: set[str] = set()
    for index, row in enumerate(assets):
        if not isinstance(row, dict):
            errors.append(f"assets[{index}] must be an object")
            continue
        missing = [field for field in ASSET_FIELDS if not isinstance(row.get(field), str) or not row.get(field)]
        if missing:
            errors.append(f"assets[{index}] is missing required fields: {', '.join(missing)}")
            continue

        repository = row["original_repository"]
        source_path = row["original_path"]
        mapped_path = row["mapped_path"]
        counts[repository] += 1
        key = _asset_key(row)
        if key in keys:
            errors.append(f"duplicate original asset: {repository}/{source_path}")
        keys.add(key)
        if mapped_path in mapped_paths:
            errors.append(f"duplicate mapped path: {mapped_path}")
        mapped_paths.add(mapped_path)
        if repository not in EXPECTED_PINS or row["source_commit"] != EXPECTED_PINS.get(repository):
            errors.append(f"unexpected source commit for {repository}/{source_path}")
        if not _is_normalized_relative_path(source_path):
            errors.append(f"original path is not a normalized relative Git path: {repository}/{source_path}")
        if not _is_normalized_relative_path(mapped_path):
            errors.append(f"mapped path is not a normalized relative Git path: {mapped_path}")
        expected_destination = f"doc/integration-program/legacy/{repository}/{source_path}.source"
        if mapped_path != expected_destination:
            errors.append(f"unexpected mapped path for {repository}/{source_path}: {mapped_path}")
        if not re.fullmatch(r"[0-9a-f]{40}", row["blob"]):
            errors.append(f"invalid Git blob id for {repository}/{source_path}")
        if row["mode"] not in SUPPORTED_GIT_MODES:
            errors.append(f"unsupported Git mode for {repository}/{source_path}: {row['mode']}")
        if row["status"] not in ALLOWED_PENDING_STATUSES:
            errors.append(f"unsupported non-pending status for {repository}/{source_path}: {row['status']}")

    if dict(counts) != EXPECTED_ASSET_COUNTS:
        errors.append(f"ledger repository counts are {dict(counts)}; expected {EXPECTED_ASSET_COUNTS}")

    if receipt is not None:
        provenance_error = _receipt_provenance_error(receipt)
        if provenance_error:
            errors.append(provenance_error)
        receipt_pins = receipt.get("sourceCommits")
        if receipt_pins != EXPECTED_PINS:
            errors.append("receipt source pins do not match the ledger's recorded rehearsal")
        receipt_rows = receipt.get("mapping")
        if not isinstance(receipt_rows, list):
            errors.append("receipt mapping must be a JSON array")
        else:
            expected: dict[tuple[str, str], dict[str, str]] = {}
            for source_row in receipt_rows:
                if not isinstance(source_row, dict) or not isinstance(source_row.get("destination"), str) or not source_row["destination"].endswith(".source"):
                    continue
                repository = source_row.get("repository", "")
                source_path = source_row.get("source", "")
                if not isinstance(repository, str) or not isinstance(source_path, str):
                    errors.append("receipt mapping rows require string repository and source paths")
                    continue
                if _asset_key({"original_repository": repository, "original_path": source_path}) in expected:
                    errors.append(f"duplicate source asset in receipt: {repository}/{source_path}")
                expected[(repository, source_path)] = {
                    "original_repository": repository,
                    "original_path": source_path,
                    "mapped_path": source_row.get("destination", ""),
                    "source_commit": receipt_pins.get(repository, "") if isinstance(receipt_pins, dict) else "",
                    "blob": source_row.get("blob", ""),
                    "mode": source_row.get("mode", ""),
                }
                if not _is_normalized_relative_path(source_path):
                    errors.append(f"receipt original path is not normalized: {repository}/{source_path}")
                if not _is_normalized_relative_path(source_row.get("destination")):
                    errors.append(f"receipt destination is not a normalized relative path: {source_row.get('destination')}")
                if source_row.get("mode") not in SUPPORTED_GIT_MODES:
                    errors.append(f"receipt contains unsupported Git mode for {repository}/{source_path}: {source_row.get('mode')}")
                if not re.fullmatch(r"[0-9a-f]{40}", str(source_row.get("blob", ""))):
                    errors.append(f"receipt contains invalid Git blob id for {repository}/{source_path}")
            actual = {
                _asset_key(row): {field: row.get(field, "") for field in RECEIPT_FIELDS}
                for row in assets
                if isinstance(row, dict)
            }
            if len(expected) != EXPECTED_ASSET_TOTAL:
                errors.append(f"receipt has {len(expected)} inert assets; expected {EXPECTED_ASSET_TOTAL}")
            if set(actual) != set(expected):
                missing = sorted(set(expected) - set(actual))
                extra = sorted(set(actual) - set(expected))
                errors.append(f"ledger/receipt path mismatch; missing={missing[:3]}, extra={extra[:3]}")
            for key in set(actual) & set(expected):
                if actual[key] != expected[key]:
                    errors.append(f"ledger/receipt metadata mismatch for {key[0]}/{key[1]}")

    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ledger", type=Path, default=DEFAULT_LEDGER, help="disposition JSON ledger")
    parser.add_argument("--receipt", type=Path, help="optional live import-receipt.json; otherwise use the frozen source fixture")
    args = parser.parse_args(argv)

    try:
        ledger = _read_json(args.ledger)
        receipt_path = args.receipt or DEFAULT_RECEIPT
        receipt = _read_json(receipt_path)
    except (OSError, json.JSONDecodeError, ValueError) as error:
        print(f"Invalid input: {error}", file=sys.stderr)
        return 2

    fixture_hash = hashlib.sha256(DEFAULT_RECEIPT.read_bytes()).hexdigest()
    if args.receipt is None and fixture_hash != EXPECTED_RECEIPT_FIXTURE_SHA256:
        print("Invalid input: frozen receipt fixture SHA-256 changed", file=sys.stderr)
        return 2
    errors = validate_ledger(ledger, receipt)
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    assets = ledger["assets"]
    counts = Counter(row["category"] for row in assets)
    summary = {
        "valid": True,
        "assets": len(assets),
        "byRepository": EXPECTED_ASSET_COUNTS,
        "byCategory": dict(sorted(counts.items())),
        "receiptCompared": True,
        "sourceReceiptSha256": receipt.get("provenance", {}).get("sourceReceiptSha256") if isinstance(receipt.get("provenance"), dict) else None,
    }
    print(json.dumps(summary, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
