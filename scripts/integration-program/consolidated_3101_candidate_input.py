"""Pinned unpublished 3.10.1 candidate identity. Distinct from the 3.10.0 pin.

This module records independently verified identity only. It is not imported by
the 3.10.0 Feedz executor, recovery preflight, or nuget.org stage path. Those
keep `consolidated_candidate_input.py` (artifact `12664519771`). Do not retarget
them here.
"""
from __future__ import annotations

import json
from pathlib import Path

SOURCE = "ba2b801a932646e2771167a8e02a93e2f569a104"
RUN = 42458011137
ATTEMPT = 1
ARTIFACT = 13609452227
ARCHIVE_SHA256 = "76a79c05967f8d5f74cb29e551518d1a6d4a4e74e1fcca924044e252ffee7d29"
MANIFEST_SHA256 = "f2d1a2bdf45aad88a835d827af8d4432df51db4edcc23201f9ebf57520d2444d"
ENVELOPE_SHA256 = "72a463f8a49b6fddfdd668779f45ab5d098a6fa665e1682fd646780652e57bc8"
ORIGINAL_ENVELOPE = Path(__file__).resolve().parent / "persisted-workflow-upgrade/3101-original-envelope.json"
PRODUCER_WORKFLOW = ".github/workflows/prepare-consolidated-3.10.1-release-candidate.yml"
PRODUCER = {
    "version": "3.10.1",
    "source_commit": SOURCE,
    "run_id": RUN,
    "run_attempt": ATTEMPT,
    "artifact_id": ARTIFACT,
}
# Inner archive counts from the independently hashed original zip. Not a second
# inventory snapshot: the 3.10.0 pin inventory IDs match this set (231/135).
PIN_PACKAGE_COUNT = 231
PIN_SNUPKG_COUNT = 231
PIN_EXCLUSION_COUNT = 135
ASSEMBLY_RECEIPT_ENTRIES = 688
ASSEMBLY_VERSION = "3.10.1.0"
# nuget.org promote, after signoff, excludes this testing ID (231 stage / 230 promote).
PROMOTE_EXCLUSION_ID = "Elsa.Testing.Shared.Integration"
PROMOTE_PACKAGE_COUNT = 230
_ENVELOPE = json.loads(ORIGINAL_ENVELOPE.read_text(encoding="utf-8"))
ARCHIVE_SIZE = _ENVELOPE["archive_size"]
EXPIRES_AT = _ENVELOPE["expires_at"]
# Frozen 3.10.0 identity. A silent copy of those constants into this module is a defect.
_STAGED_3100 = {
    "source_commit": "5cff67228e550b353b3483eb3b37dfdb73a41c74",
    "run_id": 40347931480,
    "artifact_id": 12664519771,
}


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


require(SOURCE != _STAGED_3100["source_commit"], "3.10.1 pin reused the staged 3.10.0 source")
require(ARTIFACT != _STAGED_3100["artifact_id"], "3.10.1 pin reused the staged 3.10.0 artifact")
require(RUN != _STAGED_3100["run_id"], "3.10.1 pin reused the staged 3.10.0 producer run")
require(PRODUCER["version"] == "3.10.1", "3.10.1 pin version must stay 3.10.1")
require(
    all(_ENVELOPE.get(key) == value for key, value in (
        ("source_commit", SOURCE), ("run_id", RUN), ("run_attempt", ATTEMPT),
        ("artifact_id", ARTIFACT), ("archive_sha256", ARCHIVE_SHA256),
        ("preupload_manifest_sha256", MANIFEST_SHA256), ("version", "3.10.1"),
        ("published", False)))
    and _ENVELOPE.get("artifact_name") == (
        f"consolidated-candidate-{SOURCE}-{RUN}-{ATTEMPT}"),
    "Pinned 3.10.1 envelope identity does not match candidate input constants")
require(PIN_PACKAGE_COUNT - 1 == PROMOTE_PACKAGE_COUNT, "Promote exclusion must leave 230 packages")
require(PROMOTE_EXCLUSION_ID.endswith("Integration"), "Promote exclusion must be the Integration ID")
