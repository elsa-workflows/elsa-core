#!/usr/bin/env python3
"""Read-only publisher ownership and simulated cutover preflight."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from release_unit_manifest import (
    DEFAULT_UNIT_ID,
    MANIFEST_PATH,
    get_unit,
    load_manifest,
    validate_publisher_handoff,
)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=MANIFEST_PATH)
    parser.add_argument("--unit", default=DEFAULT_UNIT_ID)
    parser.add_argument("--proposed-repository")
    parser.add_argument("--proposed-workflow")
    parser.add_argument("--receipt", type=Path)
    args = parser.parse_args()

    if bool(args.proposed_repository) != bool(args.proposed_workflow):
        parser.error("--proposed-repository and --proposed-workflow must be supplied together")
    if args.receipt and not args.proposed_repository:
        parser.error("--receipt requires a proposed publisher")

    try:
        unit = get_unit(load_manifest(args.manifest), args.unit)
        proposed = None
        receipt = None
        if args.proposed_repository:
            proposed = {
                "repository": args.proposed_repository,
                "workflow_path": args.proposed_workflow,
            }
        if args.receipt:
            receipt = json.loads(args.receipt.read_text(encoding="utf-8"))
        result = validate_publisher_handoff(unit, proposed, receipt)
    except (OSError, json.JSONDecodeError, ValueError) as error:
        print(f"publisher handoff preflight failed: {error}", file=sys.stderr)
        return 1

    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
