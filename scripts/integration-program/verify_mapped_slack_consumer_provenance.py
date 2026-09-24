#!/usr/bin/env python3
"""Recheck retained mapped Slack consumer package provenance without building."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from run_mapped_slack_package_proof import (
    MANIFEST_PATH,
    PACKAGE_ID,
    PACKAGE_VERSION,
    TFMS,
    reject_symlink_ancestors,
    sha256_file,
    verify_local_package_consumption,
)
from release_unit_manifest import get_unit, load_manifest


def verify_retained_evidence(evidence_path: Path) -> dict:
    evidence_path = evidence_path.resolve(strict=True)
    proof_root = evidence_path.parent
    evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
    if evidence.get("result") != "passed" or evidence.get("publication_authorized") is not False:
        raise ValueError("Retained evidence is not a successful nonpublishing package proof")
    package_receipt = evidence.get("package")
    if not isinstance(package_receipt, dict):
        raise ValueError("Retained evidence has no package receipt")
    if package_receipt.get("package_id") != PACKAGE_ID or package_receipt.get("package_version") != PACKAGE_VERSION:
        raise ValueError("Retained package identity differs from the current nonpublishable manifest proof identity")

    package = proof_root / "local-feed" / f"{PACKAGE_ID}.{PACKAGE_VERSION}.nupkg"
    if not package.is_file() or sha256_file(package) != package_receipt.get("nupkg_sha256"):
        raise ValueError("Retained local package is missing or its SHA-256 differs from the original evidence")

    consumers = []
    for framework in TFMS:
        consumer_dir = proof_root / "consumers" / framework
        package_cache = proof_root / "package-caches" / "consumers" / framework
        consumers.append({
            "consumer": f"{framework}-package-consumer",
            **verify_local_package_consumption(
                consumer_dir,
                framework,
                package,
                package_cache,
                package.parent,
            ),
        })

    offline_dir = proof_root / "offline-activity-smoke"
    consumers.append({
        "consumer": "net10.0-offline-activity-smoke",
        **verify_local_package_consumption(
            offline_dir,
            "net10.0",
            package,
            proof_root / "package-caches" / "offline-activity-smoke",
            package.parent,
        ),
    })

    return {
        "result": "passed",
        "scope": (
            "retained consumer restore-cache and output-assembly provenance recheck; "
            "no restore, build, pack, or test execution"
        ),
        "original_package_sha256": package_receipt["nupkg_sha256"],
        "release_unit": get_unit(load_manifest(MANIFEST_PATH))["id"],
        "package_id": PACKAGE_ID,
        "package_version": PACKAGE_VERSION,
        "manifest_sha256": sha256_file(MANIFEST_PATH),
        "consumer_count": len(consumers),
        "consumers": consumers,
        "publication_authorized": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", required=True, type=Path, help="Retained mapped package proof evidence.json")
    parser.add_argument(
        "--output",
        required=True,
        type=Path,
        help="New recheck receipt outside the retained proof directory",
    )
    args = parser.parse_args()

    proof_root = args.evidence.resolve(strict=True).parent
    output_argument = args.output.expanduser().absolute()
    reject_symlink_ancestors(output_argument)
    output = output_argument.resolve(strict=False)
    reject_symlink_ancestors(output)
    if output.exists():
        raise ValueError(f"Output receipt already exists: {output}")
    if output == proof_root or proof_root in output.parents:
        raise ValueError("Recheck receipt must be outside the retained proof directory")

    receipt = verify_retained_evidence(args.evidence)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({
        "result": receipt["result"],
        "consumer_count": receipt["consumer_count"],
        "receipt": str(output),
    }, indent=2))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, json.JSONDecodeError, RuntimeError) as error:
        print(f"verify_mapped_slack_consumer_provenance: {error}", file=sys.stderr)
        raise SystemExit(1)
