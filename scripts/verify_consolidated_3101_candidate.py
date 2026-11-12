#!/usr/bin/env python3
"""Fetch and verify the pinned unpublished 3.10.1 candidate before any feed upload. Never publishes.

    retrieve --output DIR     (needs GH_TOKEN) fetch run/artifact/compare metadata and the artifact zip
                              into DIR, then run every check below so a bad input fails early.
    verify --transport DIR [--stage DIR]
                              (refuses to run with a GitHub token in the environment) re-run every check on
                              the files in DIR and, with --stage, extract the sealed .nupkg/.snupkg files.
    count --dir DIR --expect N
                              fail unless DIR holds exactly N .nupkg files.

Every check raises SystemExit with a reason; a failed check means nothing may be pushed.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import zipfile
from pathlib import Path, PurePosixPath

sys.path.insert(0, str(Path(__file__).resolve().parent))

import consolidated_3101_pin as pin

TOKEN_NAMES = ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")
SEAL_NAME = "preupload-manifest.json"
PACKAGE_FILE = re.compile(r"(?P<id>.+)\.(?P<version>[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.]+)?)\.(?P<kind>nupkg|snupkg)")
TRANSPORT_FILES = ("candidate.zip", "run.json", "artifact.json", "compare.json")
REPOSITORY = "elsa-workflows/elsa-core"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def sha256_file(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def check_run_id(run_id: int) -> None:
    require(run_id not in pin.FORBIDDEN_RUNS,
            f"Run {run_id} is the staged 3.10.0 candidate run and must never be promoted.")
    require(run_id == pin.RUN_ID, f"Run {run_id} is not the pinned 3.10.1 candidate run {pin.RUN_ID}.")


def check_artifact_id(artifact_id: int) -> None:
    require(artifact_id not in pin.FORBIDDEN_ARTIFACTS,
            f"Artifact {artifact_id} is the staged 3.10.0 candidate artifact and must never be promoted.")
    require(artifact_id == pin.ARTIFACT_ID,
            f"Artifact {artifact_id} is not the pinned 3.10.1 candidate artifact {pin.ARTIFACT_ID}.")


def check_source(commit: str, where: str) -> None:
    require(commit not in pin.FORBIDDEN_SOURCES, f"{where} names the 3.10.0 source commit {commit}.")
    require(commit == pin.SOURCE_COMMIT, f"{where} {commit!r} is not the pinned source {pin.SOURCE_COMMIT}.")


def check_run(run: dict) -> None:
    check_run_id(run.get("id"))
    require(run.get("run_attempt") == pin.RUN_ATTEMPT, "Candidate run attempt is not the pinned attempt.")
    check_source(run.get("head_sha"), "Candidate run head_sha")
    require(run.get("conclusion") == "success" and run.get("status") == "completed", "Candidate run did not succeed.")
    require(run.get("event") == "workflow_dispatch" and run.get("path") == pin.PRODUCER_WORKFLOW,
            "Candidate run was not produced by the 3.10.1 candidate workflow.")


def check_artifact(artifact: dict) -> None:
    check_artifact_id(artifact.get("id"))
    require(artifact.get("name") == pin.ARTIFACT_NAME, "Candidate artifact name mismatch.")
    require(artifact.get("expired") is False, "Candidate artifact has expired.")
    require(artifact.get("digest") == f"sha256:{pin.ARCHIVE_SHA256}", "Candidate artifact digest does not match the pin.")
    workflow_run = artifact.get("workflow_run") or {}
    check_run_id(workflow_run.get("id"))
    check_source(workflow_run.get("head_sha"), "Candidate artifact workflow_run.head_sha")


def check_ancestry(compare: dict) -> None:
    """compare is GET compare/release/3.10.0...<source>; 'behind' or 'identical' means source is on the line."""
    require(compare.get("status") in ("behind", "identical"),
            f"Source {pin.SOURCE_COMMIT} is not an ancestor of {pin.RELEASE_LINE} (compare status {compare.get('status')!r}).")
    require((compare.get("merge_base_commit") or {}).get("sha") == pin.SOURCE_COMMIT,
            "Compare merge base is not the pinned source commit.")


def check_archive(archive: Path) -> None:
    require(archive.is_file() and not archive.is_symlink(), "Candidate archive missing.")
    require(sha256_file(archive) == pin.ARCHIVE_SHA256, "Candidate archive sha256 does not match the pin.")


def read_manifest(archive: Path) -> dict:
    with zipfile.ZipFile(archive) as zipped:
        require(SEAL_NAME in zipped.namelist(), "Candidate seal missing.")
        data = zipped.read(SEAL_NAME)
    require(hashlib.sha256(data).hexdigest() == pin.MANIFEST_SHA256, "Candidate manifest sha256 does not match the pin.")
    manifest = json.loads(data)
    require(manifest.get("schema") == 1 and manifest.get("published") is False and manifest.get("version") == pin.VERSION
            and manifest.get("run_id") == pin.RUN_ID and manifest.get("run_attempt") == pin.RUN_ATTEMPT,
            "Candidate manifest identity does not match the pin.")
    check_source(manifest.get("source_commit"), "Candidate manifest source_commit")
    return manifest


def sealed_packages(manifest: dict) -> dict[str, str]:
    """{file name: sha256} for every sealed .nupkg/.snupkg; all must be 3.10.1."""
    sealed: dict[str, str] = {}
    for row in manifest.get("files") or []:
        name = PurePosixPath(row["path"]).name
        match = PACKAGE_FILE.fullmatch(name)
        if match is None or not row["path"].startswith("artifacts/"):
            continue
        require(match.group("version") == pin.VERSION, f"Sealed package {name} is not version {pin.VERSION}.")
        require(name not in sealed, f"Duplicate sealed package {name}.")
        sealed[name] = row["sha256"]
    nupkgs = [name for name in sealed if name.endswith(".nupkg")]
    require(len(nupkgs) == pin.FEEDZ_PACKAGE_COUNT, f"Candidate seals {len(nupkgs)} .nupkg files, expected {pin.FEEDZ_PACKAGE_COUNT}.")
    require(len(sealed) == 2 * pin.FEEDZ_PACKAGE_COUNT, "Candidate seals an unexpected number of .snupkg files.")
    return sealed


def stage_packages(archive: Path, sealed: dict[str, str], output: Path) -> list[Path]:
    """Extract exactly the sealed packages, flat, checking each sha256 against the seal."""
    output.mkdir(parents=True, exist_ok=True)
    staged = []
    with zipfile.ZipFile(archive) as zipped:
        for member in zipped.infolist():
            name = PurePosixPath(member.filename).name
            if member.is_dir() or not member.filename.startswith("artifacts/") or PACKAGE_FILE.fullmatch(name) is None:
                continue
            require(".." not in PurePosixPath(member.filename).parts and name in sealed, f"Unsealed candidate entry {member.filename}.")
            destination = output / name
            require(not destination.exists(), f"Duplicate candidate package {name}.")
            digest = hashlib.sha256()
            with zipped.open(member) as reader, destination.open("xb") as writer:
                for chunk in iter(lambda: reader.read(1 << 20), b""):
                    digest.update(chunk)
                    writer.write(chunk)
            require(digest.hexdigest() == sealed[name], f"Candidate package hash mismatch: {name}.")
            staged.append(destination)
    require({path.name for path in staged} == set(sealed), "Staged packages do not match the candidate seal.")
    return staged


def verify_transport(transport: Path, stage: Path | None = None) -> list[Path]:
    require(transport.is_dir(), "Transport directory missing.")
    for name in TRANSPORT_FILES:
        require((transport / name).is_file() and not (transport / name).is_symlink(), f"Transport file {name} missing.")
    check_run(json.loads((transport / "run.json").read_text()))
    check_artifact(json.loads((transport / "artifact.json").read_text()))
    check_ancestry(json.loads((transport / "compare.json").read_text()))
    archive = transport / "candidate.zip"
    check_archive(archive)
    sealed = sealed_packages(read_manifest(archive))
    return stage_packages(archive, sealed, stage) if stage else []


def count_nupkgs(directory: Path, expected: int) -> None:
    found = sorted(path.name for path in directory.glob("*.nupkg"))
    require(len(found) == expected, f"Found {len(found)} .nupkg files in {directory}, expected {expected}.")
    print(f"{len(found)} .nupkg files, as expected.")


def gh_api(endpoint: str, destination: Path) -> None:
    with destination.open("wb") as out:
        subprocess.run(["gh", "api", endpoint], check=True, stdout=out)


def retrieve(output: Path) -> None:
    require(any(os.environ.get(name) for name in TOKEN_NAMES), "Retrieve requires GH_TOKEN.")
    output.mkdir(parents=True, exist_ok=True)
    base = f"repos/{REPOSITORY}"
    gh_api(f"{base}/actions/runs/{pin.RUN_ID}/attempts/{pin.RUN_ATTEMPT}", output / "run.json")
    gh_api(f"{base}/actions/artifacts/{pin.ARTIFACT_ID}", output / "artifact.json")
    gh_api(f"{base}/compare/{pin.RELEASE_LINE}...{pin.SOURCE_COMMIT}", output / "compare.json")
    # The checks run before the (large) zip is fetched, so a wrong run, artifact or source fails cheaply.
    check_run(json.loads((output / "run.json").read_text()))
    check_artifact(json.loads((output / "artifact.json").read_text()))
    check_ancestry(json.loads((output / "compare.json").read_text()))
    gh_api(f"{base}/actions/artifacts/{pin.ARTIFACT_ID}/zip", output / "candidate.zip")
    verify_transport(output)
    print(f"Fetched and verified candidate {pin.VERSION} run {pin.RUN_ID} artifact {pin.ARTIFACT_ID} source {pin.SOURCE_COMMIT}.")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("retrieve").add_argument("--output", type=Path, required=True)
    verify = sub.add_parser("verify")
    verify.add_argument("--transport", type=Path, required=True)
    verify.add_argument("--stage", type=Path)
    count = sub.add_parser("count")
    count.add_argument("--dir", type=Path, required=True)
    count.add_argument("--expect", type=int, required=True)
    args = parser.parse_args(argv)
    if args.command == "retrieve":
        retrieve(args.output)
    elif args.command == "verify":
        require(not any(os.environ.get(name) for name in TOKEN_NAMES), "A GitHub token must not be present while verifying or staging.")
        staged = verify_transport(args.transport, args.stage)
        print(f"Verified candidate {pin.VERSION}; staged {len([p for p in staged if p.suffix == '.nupkg'])} .nupkg files." if args.stage
              else f"Verified candidate {pin.VERSION}.")
    else:
        count_nupkgs(args.dir, args.expect)
    return 0


if __name__ == "__main__":
    sys.exit(main())
