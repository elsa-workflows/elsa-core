"""Verify the accepted original candidate independently of any consumer fixture."""
from __future__ import annotations

from datetime import datetime, timezone
import json
import os
from pathlib import Path

import prepare_consolidated_release_candidate as candidate

SOURCE = "5cff67228e550b353b3483eb3b37dfdb73a41c74"
RUN = 40347931480
ATTEMPT = 1
ARTIFACT = 12664519771
ARCHIVE_SHA256 = "2dd253e1f2946ddcb2b8e75ef397596001757ddbcb0a91f717e9505bbf28b4f6"
MANIFEST_SHA256 = "b6a1750f1f796f68a441bef93f67386b4a862dbca94d498ecf64536b102c2994"
ENVELOPE_SHA256 = "9c1840b8f6c68c70338390453995b9382f21feadf7d577b257dd7a95e8b1417b"
ORIGINAL_ENVELOPE = Path(__file__).resolve().parent / "persisted-workflow-upgrade/stable-original-envelope.json"
# Frozen candidate pin inventory (231 packages). Recovery and the executor
# use this snapshot so they still accept the real pinned artifact.
PIN_INVENTORY = Path(__file__).resolve().parent / "consolidated-pin-inventory-baseline.json"
PRODUCER = {"version": "3.10.0", "source_commit": SOURCE, "run_id": RUN, "run_attempt": ATTEMPT, "artifact_id": ARTIFACT}
_PIN = json.loads(PIN_INVENTORY.read_text(encoding="utf-8"))
_ENVELOPE = json.loads(ORIGINAL_ENVELOPE.read_text(encoding="utf-8"))
PIN_PACKAGE_COUNT = len(_PIN["packages"])
PIN_EXCLUSION_COUNT = len(_PIN["exclusions"])
PIN_EXCLUSION_ID_COUNT = len({row["id"] for row in _PIN["exclusions"]})
ARCHIVE_SIZE = _ENVELOPE["archive_size"]
EXPIRES_AT = _ENVELOPE["expires_at"]
if any(_ENVELOPE.get(key) != value for key, value in (
        ("source_commit", SOURCE), ("run_id", RUN), ("run_attempt", ATTEMPT),
        ("artifact_id", ARTIFACT), ("archive_sha256", ARCHIVE_SHA256),
        ("preupload_manifest_sha256", MANIFEST_SHA256))):
    raise RuntimeError("Pinned envelope identity does not match candidate input constants")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def validate_transport(directory: Path) -> None:
    expected = {"candidate.zip", "artifact.json", "producer-run.json", "live-retrieval.json"}
    require(directory.is_dir() and not directory.is_symlink() and
            {path.name for path in directory.iterdir()} == expected and
            all((directory / name).is_file() and not (directory / name).is_symlink() for name in expected),
            "Same-run transport layout mismatch")


def verify_candidate_inputs(archive: Path, metadata_path: Path, producer_run_path: Path,
                            retrieval_path: Path, destination: Path, *,
                            producer: dict = PRODUCER, original_envelope: Path = ORIGINAL_ENVELOPE,
                            envelope_sha256: str = ENVELOPE_SHA256, archive_sha256: str = ARCHIVE_SHA256,
                            manifest_sha256: str = MANIFEST_SHA256) -> dict:
    """Check reviewed identity and original bytes before extraction; return provenance."""
    require(not any(os.environ.get(key) for key in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")),
            "Artifact-reading token must not enter verification/execution")
    require(candidate.file_sha256(original_envelope) == envelope_sha256, "Original accepted envelope bytes changed")
    envelope = json.loads(original_envelope.read_text())
    require(all(envelope.get(key) == value for key, value in producer.items()) and
            envelope.get("archive_sha256") == archive_sha256 and
            envelope.get("preupload_manifest_sha256") == manifest_sha256,
            "Original accepted candidate identity changed")
    metadata = json.loads(metadata_path.read_text())
    producer_run = json.loads(producer_run_path.read_text())
    require(producer_run.get("id") == producer["run_id"] and
            producer_run.get("run_attempt") == producer["run_attempt"] and
            producer_run.get("head_sha") == producer["source_commit"] and
            producer_run.get("conclusion") == "success" and
            producer_run.get("event") in ("push", "workflow_dispatch") and
            producer_run.get("path") == ".github/workflows/prepare-consolidated-release-candidate.yml",
            "Original producer run identity mismatch")
    retrieval = json.loads(retrieval_path.read_text())
    require(set(retrieval) == {"schema", "artifact_id", "archive_sha256", "retrieved_at"} and
            retrieval.get("schema") == 1 and retrieval.get("artifact_id") == producer["artifact_id"] and
            retrieval.get("archive_sha256") == archive_sha256, "Live retrieval identity mismatch")
    observed = datetime.fromisoformat(retrieval["retrieved_at"].replace("Z", "+00:00"))
    original = datetime.fromisoformat(envelope["retrieved_at"].replace("Z", "+00:00"))
    require(observed.tzinfo is not None and original <= observed <= datetime.now(timezone.utc),
            "Invalid live retrieval observation")
    candidate.verify_and_extract(archive, envelope, metadata, destination,
                                 source=producer["source_commit"], run_id=producer["run_id"],
                                 attempt=producer["run_attempt"])
    return {"candidate_producer": dict(producer), "original_envelope_sha256": envelope_sha256,
            "preupload_manifest_sha256": manifest_sha256,
            "live_retrieval_sha256": candidate.file_sha256(retrieval_path)}
