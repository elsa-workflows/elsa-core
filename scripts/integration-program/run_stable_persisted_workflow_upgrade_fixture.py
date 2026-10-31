#!/usr/bin/env python3
"""Adapt the accepted stable candidate to the unchanged SQLite lifecycle."""
from __future__ import annotations

import argparse
from pathlib import Path
import re

import run_persisted_workflow_upgrade_fixture as fixture
from consolidated_candidate_input import (
    ARCHIVE_SHA256, ARTIFACT, ATTEMPT, ENVELOPE_SHA256, MANIFEST_SHA256, ORIGINAL_ENVELOPE,
    PRODUCER, RUN, SOURCE, validate_transport, verify_candidate_inputs,
)

FIXTURE_SHA256 = "3a07357429bbff28ffdc4bbdacf2d6a3b4b7a981cdb146c8f969349c4c5d0dba"


def verify_target(archive: Path, metadata_path: Path, producer_run_path: Path, retrieval_path: Path,
                  destination: Path, fixture_source: str, fixture_run: int | None = None,
                  fixture_attempt: int | None = None) -> fixture.ValidatedTarget:
    fixture.require(re.fullmatch(r"[0-9a-f]{40}", fixture_source) is not None and
                    ((fixture_run is None and fixture_attempt is None) or
                     (type(fixture_run) is int and fixture_run > 0 and type(fixture_attempt) is int and fixture_attempt > 0)), "Invalid matrix execution identity")
    fixture.require(fixture.sha256(fixture.FIXTURE) == FIXTURE_SHA256, "Canonical lifecycle fixture changed")
    provenance = verify_candidate_inputs(archive, metadata_path, producer_run_path, retrieval_path, destination,
                                         producer=PRODUCER, original_envelope=ORIGINAL_ENVELOPE,
                                         envelope_sha256=ENVELOPE_SHA256, archive_sha256=ARCHIVE_SHA256,
                                         manifest_sha256=MANIFEST_SHA256)
    target = fixture.ValidatedTarget(destination, ARCHIVE_SHA256, "verified-artifacts.json",
                                    fixture.sha256(destination / "verified-artifacts.json"), dict(PRODUCER),
                                    {**provenance,
                                     "matrix_execution": {"fixture_source_commit": fixture_source, "run_id": fixture_run, "run_attempt": fixture_attempt}},
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
    inputs = args.inputs.resolve(strict=True)
    archive = inputs / "candidate.zip"
    artifacts, output = args.candidate_artifacts.resolve(), args.output.resolve()
    target = verify_target(archive, inputs / "artifact.json", inputs / "producer-run.json", inputs / "live-retrieval.json",
                           artifacts, args.fixture_source, args.fixture_run, args.fixture_attempt)
    selected = [tuple(args.cell.split("/"))] if args.cell else sorted(fixture.MATRIX)
    fixture.run(artifacts, archive, output, selected, target=target)


if __name__ == "__main__":
    main()
