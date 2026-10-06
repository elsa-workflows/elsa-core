#!/usr/bin/env python3
"""Adapt the accepted stable candidate to the unchanged SQLite lifecycle."""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re

import prepare_consolidated_release_candidate as candidate
import run_persisted_workflow_upgrade_fixture as fixture

SOURCE = "ba5b348aa2414fdf7c19d9d8806e87b7c91a4205"
RUN = 37456860080
ATTEMPT = 1
ARTIFACT = 11412848210
ARCHIVE_SHA256 = "ace85260c3389916fe3dcd7ee9cd5b766e4030dd6cee13c7f89285ada79c944f"
MANIFEST_SHA256 = "df25b11556bbf21210cfc33ef02f3d0841af6316a20ece69422c7b8143a11820"
ENVELOPE_SHA256 = "ef5e772020b151089b64b90b819d36ff371ed5e65212f7fdc7b2934d9860ddda"
FIXTURE_SHA256 = "3a07357429bbff28ffdc4bbdacf2d6a3b4b7a981cdb146c8f969349c4c5d0dba"
ORIGINAL_ENVELOPE = fixture.FIXTURE.parent / "stable-original-envelope.json"
PRODUCER = {"version": "3.10.0", "source_commit": SOURCE, "run_id": RUN, "run_attempt": ATTEMPT, "artifact_id": ARTIFACT}


def validate_transport(directory: Path) -> None:
    expected = {"candidate.zip", "artifact.json", "producer-run.json", "live-retrieval.json"}
    fixture.require(directory.is_dir() and not directory.is_symlink() and
                    {path.name for path in directory.iterdir()} == expected and
                    all((directory / name).is_file() and not (directory / name).is_symlink() for name in expected),
                    "Same-run transport layout mismatch")


def verify_target(archive: Path, metadata_path: Path, producer_run_path: Path, retrieval_path: Path,
                  destination: Path, fixture_source: str, fixture_run: int | None = None,
                  fixture_attempt: int | None = None) -> fixture.ValidatedTarget:
    fixture.require(not any(os.environ.get(key) for key in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")), "Artifact-reading token must not enter verification/execution")
    fixture.require(re.fullmatch(r"[0-9a-f]{40}", fixture_source) is not None and
                    ((fixture_run is None and fixture_attempt is None) or
                     (type(fixture_run) is int and fixture_run > 0 and type(fixture_attempt) is int and fixture_attempt > 0)), "Invalid matrix execution identity")
    fixture.require(fixture.sha256(fixture.FIXTURE) == FIXTURE_SHA256, "Canonical lifecycle fixture changed")
    fixture.require(fixture.sha256(ORIGINAL_ENVELOPE) == ENVELOPE_SHA256, "Original accepted envelope bytes changed")
    envelope = json.loads(ORIGINAL_ENVELOPE.read_text())
    fixture.require(all(envelope.get(key) == value for key, value in PRODUCER.items()) and
                    envelope.get("archive_sha256") == ARCHIVE_SHA256 and envelope.get("preupload_manifest_sha256") == MANIFEST_SHA256,
                    "Original accepted candidate identity changed")
    metadata = json.loads(metadata_path.read_text())
    producer_run = json.loads(producer_run_path.read_text())
    fixture.require(producer_run.get("id") == RUN and producer_run.get("run_attempt") == ATTEMPT and
                    producer_run.get("head_sha") == SOURCE and producer_run.get("conclusion") == "success" and
                    producer_run.get("event") in ("push", "workflow_dispatch") and
                    producer_run.get("path") == ".github/workflows/prepare-consolidated-release-candidate.yml", "Original producer run identity mismatch")
    retrieval = json.loads(retrieval_path.read_text())
    fixture.require(set(retrieval) == {"schema", "artifact_id", "archive_sha256", "retrieved_at"} and
                    retrieval.get("schema") == 1 and retrieval.get("artifact_id") == ARTIFACT and
                    retrieval.get("archive_sha256") == ARCHIVE_SHA256, "Live retrieval identity mismatch")
    observed = datetime.fromisoformat(retrieval["retrieved_at"].replace("Z", "+00:00"))
    original = datetime.fromisoformat(envelope["retrieved_at"].replace("Z", "+00:00"))
    fixture.require(observed.tzinfo is not None and original <= observed <= datetime.now(timezone.utc), "Invalid live retrieval observation")
    candidate.verify_and_extract(archive, envelope, metadata, destination, source=SOURCE, run_id=RUN, attempt=ATTEMPT)
    target = fixture.ValidatedTarget(destination, ARCHIVE_SHA256, "verified-artifacts.json",
                                    fixture.sha256(destination / "verified-artifacts.json"), dict(PRODUCER),
                                    {"candidate_producer": dict(PRODUCER),
                                     "matrix_execution": {"fixture_source_commit": fixture_source, "run_id": fixture_run, "run_attempt": fixture_attempt},
                                     "original_envelope_sha256": ENVELOPE_SHA256,
                                     "preupload_manifest_sha256": MANIFEST_SHA256,
                                     "live_retrieval_sha256": fixture.sha256(retrieval_path)},
                                    (("original-envelope.json", ORIGINAL_ENVELOPE, ENVELOPE_SHA256),
                                     ("live-retrieval.json", retrieval_path, fixture.sha256(retrieval_path))))
    target.verify(destination, archive)
    return target


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("inputs", "candidate-artifacts", "output"):
        parser.add_argument("--" + name, type=Path, required=True)
    parser.add_argument("--fixture-source", required=True)
    parser.add_argument("--fixture-run", type=int)
    parser.add_argument("--fixture-attempt", type=int)
    parser.add_argument("--cell", choices=[f"{baseline}/{tfm}" for baseline, tfm in sorted(fixture.MATRIX)])
    args = parser.parse_args()
    checkout = fixture.packages.subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=Path(__file__).resolve().parents[2], text=True).strip()
    fixture.require(args.fixture_source == checkout, "Fixture source does not match exact checkout")
    validate_transport(args.inputs)
    archive = args.inputs / "candidate.zip"
    target = verify_target(archive, args.inputs / "artifact.json", args.inputs / "producer-run.json", args.inputs / "live-retrieval.json",
                           args.candidate_artifacts, args.fixture_source, args.fixture_run, args.fixture_attempt)
    selected = [tuple(args.cell.split("/"))] if args.cell else sorted(fixture.MATRIX)
    fixture.run(args.candidate_artifacts, archive, args.output, selected, target=target)


if __name__ == "__main__":
    main()
