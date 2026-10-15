"""Verify the accepted original candidate independently of any consumer fixture."""
from __future__ import annotations

from datetime import datetime, timezone
import json
import os
from pathlib import Path

import prepare_consolidated_release_candidate as candidate

SOURCE = "ba5b348aa2414fdf7c19d9d8806e87b7c91a4205"
RUN = 37456860080
ATTEMPT = 1
ARTIFACT = 11412848210
ARCHIVE_SHA256 = "ace85260c3389916fe3dcd7ee9cd5b766e4030dd6cee13c7f89285ada79c944f"
MANIFEST_SHA256 = "df25b11556bbf21210cfc33ef02f3d0841af6316a20ece69422c7b8143a11820"
ENVELOPE_SHA256 = "ef5e772020b151089b64b90b819d36ff371ed5e65212f7fdc7b2934d9860ddda"
ORIGINAL_ENVELOPE = Path(__file__).resolve().parent / "persisted-workflow-upgrade/stable-original-envelope.json"
PRODUCER = {"version": "3.10.0", "source_commit": SOURCE, "run_id": RUN, "run_attempt": ATTEMPT, "artifact_id": ARTIFACT}


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
