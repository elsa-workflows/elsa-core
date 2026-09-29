#!/usr/bin/env python3
"""Verify the reviewed #8497 publisher-gate merge and the current publisher-workflow bytes it owns at HEAD.

The older source-tip receipts (r1, r2, r5, r6 by chaining, and r3, r4 directly) each used to compare
.github/workflows/packages.yml and update-wiki.yml against the live HEAD. This receipt is the reviewed
publisher-gate delta (#8497) that intentionally changed packages.yml, so it now owns the HEAD comparison
for those two paths; the older verifiers only check them at their own reviewed/integration commit and
defer to this one for HEAD.
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path
from typing import Any

import verify_import_source_tip_refresh_r3 as r3
import verify_import_source_tip_refresh_r4 as r4
import verify_import_source_tip_refresh_r6 as r6
from verify_import_source_tip_refresh_r2 import LANDED_IMPORT, blob_and_mode, git


ROOT = Path(__file__).resolve().parents[2]
RECEIPT = ROOT / "doc/integration-program/consolidation/source-tip-refresh-2026-09-27-r7.json"
BASE_IMPORT_HEAD = "ecd2a77613405efe98a4adb9f9587a8d668c31e7"
MERGED_MAIN_COMMIT = "d0ea5b039c2525774b14592d3322078a645b993c"
REVIEWED_GATE_COMMIT = "79d6c41e09069927abb7825f16b5ba0a168cea38"
MERGE_COMMIT = "852a7ca7160670cd24ad3c9ca76a1bdc637c1d77"
SOURCE_PULL_REQUEST = "https://github.com/elsa-workflows/elsa-core/pull/8497"
PUBLISHERS = (".github/workflows/packages.yml", ".github/workflows/update-wiki.yml")
GATE_VALIDATOR = "scripts/validate_packages_workflow_gates.py"
# Prior receipt -> the module whose own (now HEAD-deferring) publisher check it owns.
SUPERSEDED_RECEIPTS = {
    str(r3.RECEIPT.relative_to(ROOT)): r3,
    str(r4.RECEIPT.relative_to(ROOT)): r4,
    str(r6.RECEIPT.relative_to(ROOT)): r6,
}


def verify(receipt: dict[str, Any], root: Path = ROOT) -> None:
    if receipt.get("schemaVersion") != 7 or receipt.get("issue") != 8497 or receipt.get("story") != 8286:
        raise ValueError("Seventh source-tip receipt schema or issue changed")
    if receipt.get("publicationPerformed") is not False:
        raise ValueError("Source-tip receipt must not claim a package publication")
    if receipt.get("sourcePullRequest") != SOURCE_PULL_REQUEST:
        raise ValueError("Reviewed publisher-gate pull request changed")
    if (
        receipt.get("baseImportHead"),
        receipt.get("mergedMainCommit"),
        receipt.get("reviewedGateCommit"),
        receipt.get("mergeCommit"),
    ) != (BASE_IMPORT_HEAD, MERGED_MAIN_COMMIT, REVIEWED_GATE_COMMIT, MERGE_COMMIT):
        raise ValueError("Reviewed publisher-gate merge commits changed")

    if git("rev-list", "--parents", "-n", "1", MERGE_COMMIT, root=root).split() != [
        MERGE_COMMIT, BASE_IMPORT_HEAD, MERGED_MAIN_COMMIT
    ]:
        raise ValueError("Publisher-gate merge does not have the exact reviewed parents")
    for commit in (BASE_IMPORT_HEAD, MERGED_MAIN_COMMIT, REVIEWED_GATE_COMMIT, MERGE_COMMIT):
        subprocess.run(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=root, check=True)
    subprocess.run(
        ["git", "merge-base", "--is-ancestor", REVIEWED_GATE_COMMIT, MERGED_MAIN_COMMIT], cwd=root, check=True
    )

    superseded = receipt.get("supersededPublisherReceipts")
    if not isinstance(superseded, list) or {item["path"] for item in superseded} != set(SUPERSEDED_RECEIPTS):
        raise ValueError("Superseded publisher receipts changed")
    for item in superseded:
        content = (root / item["path"]).read_bytes()
        if r6.sha256(content) != item["sha256"]:
            raise ValueError(f"Superseded publisher receipt changed: {item['path']}")
        # Each superseded verifier still checks its own reviewed/integration commit; this receipt owns HEAD.
        SUPERSEDED_RECEIPTS[item["path"]].verify(json.loads(content), root)

    workflows = receipt.get("publisherWorkflows")
    if not isinstance(workflows, dict) or set(workflows) != {"before", "after"}:
        raise ValueError("Recorded publisher-workflow bytes changed shape")
    before, after = workflows["before"], workflows["after"]
    if set(before) != set(PUBLISHERS) or set(after) != set(PUBLISHERS):
        raise ValueError("Recorded publisher-workflow bytes must cover both publisher workflows")
    for path in PUBLISHERS:
        if before[path] != blob_and_mode(BASE_IMPORT_HEAD, path, root):
            raise ValueError(f"Recorded before-bytes differ from the reviewed import head: {path}")
    if before[".github/workflows/packages.yml"] == after[".github/workflows/packages.yml"]:
        raise ValueError("Reviewed publisher-gate merge did not change the root packages.yml")
    if before[".github/workflows/update-wiki.yml"] != after[".github/workflows/update-wiki.yml"]:
        raise ValueError("Reviewed publisher-gate merge unexpectedly changed update-wiki.yml")
    for path in PUBLISHERS:
        for commit in (REVIEWED_GATE_COMMIT, MERGED_MAIN_COMMIT, MERGE_COMMIT, LANDED_IMPORT):
            if blob_and_mode(commit, path, root) != after[path]:
                raise ValueError(f"Publisher-gate merge changed active publisher workflow at {commit}: {path}")

    if receipt.get("gateValidator") != GATE_VALIDATOR:
        raise ValueError("Recorded packages gate validator path changed")
    result = subprocess.run(
        [sys.executable, str(root / GATE_VALIDATOR)], cwd=root, capture_output=True, text=True,
    )
    if result.returncode != 0:
        raise ValueError(f"Packages gate validator failed: {(result.stderr or result.stdout).strip()}")


def main() -> int:
    try:
        verify(json.loads(RECEIPT.read_text(encoding="utf-8")))
    except (OSError, subprocess.CalledProcessError, ValueError, KeyError, TypeError) as error:
        print(f"Invalid seventh source-tip refresh: {error}", file=sys.stderr)
        return 1
    print("Verified the reviewed #8497 publisher-gate merge, superseded prior publisher pins and the landed import")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
