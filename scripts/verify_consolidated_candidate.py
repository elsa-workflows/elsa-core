#!/usr/bin/env python3
"""Fetch and verify a pinned, unpublished consolidated candidate before any feed upload. Never publishes.

The pin for each version lives in .github/release-pins/consolidated.json (data only, one entry per version).
Every command takes --version and fails closed on an unknown version or a missing/malformed pin field.

    validate-pin --version V  check the pin entry only (no network, no download).
    retrieve --version V --output DIR
                              (needs GH_TOKEN) fetch run/artifact/compare metadata and the artifact zip into
                              DIR, then run every check below so a bad input fails early.
    verify --version V --transport DIR [--stage DIR]
                              (refuses to run with a GitHub token in the environment) re-run every check on
                              the files in DIR and, with --stage, extract the sealed .nupkg/.snupkg files.
    pin-output --version V --feed feedz|nuget
                              print exclude_ids and the expected package count as step outputs.
    count --version V --feed feedz|nuget --dir DIR
                              fail unless DIR holds exactly the pinned number of .nupkg files.

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
from dataclasses import dataclass
from pathlib import Path, PurePosixPath

TOKEN_NAMES = ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")
SEAL_NAME = "preupload-manifest.json"
PACKAGE_FILE = re.compile(r"(?P<id>.+)\.(?P<version>[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.]+)?)\.(?P<kind>nupkg|snupkg)")
TRANSPORT_FILES = ("candidate.zip", "run.json", "artifact.json", "compare.json")
REPOSITORY = "elsa-workflows/elsa-core"
PIN_FILE = Path(__file__).resolve().parents[1] / ".github/release-pins/consolidated.json"

# The staged 3.10.0 candidate must never be promoted. These stay hard-coded here, not in the pin file.
FORBIDDEN_RUNS = {40347931480}
FORBIDDEN_ARTIFACTS = {12664519771}
FORBIDDEN_SOURCES = {"5cff67228e550b353b3483eb3b37dfdb73a41c74"}
# Workflow files a candidate run may have been produced by. The 3.10.1 candidate (run 43236516383) was produced
# by the per-version workflow that has since been folded into prepare-consolidated-release-candidate.yml; a
# run records the path it ran from, so both are accepted.
PRODUCER_WORKFLOWS = {
    ".github/workflows/prepare-consolidated-release-candidate.yml",
    ".github/workflows/prepare-consolidated-3.10.1-release-candidate.yml",
}
PIN_FIELDS = ("run_id", "artifact_id", "source_sha", "archive_sha256", "manifest_sha256",
              "feedz_count", "nuget_count", "exclude_ids", "ancestry_branch")
VERSION_KEY = re.compile(r"[0-9]+\.[0-9]+\.[0-9]+")
HEX40 = re.compile(r"[0-9a-f]{40}")
HEX64 = re.compile(r"[0-9a-f]{64}")
PACKAGE_ID = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]*")
BRANCH = re.compile(r"[A-Za-z0-9][A-Za-z0-9._/-]*")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def sha256_file(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


@dataclass(frozen=True)
class Pin:
    version: str
    run_id: int
    artifact_id: int
    source_sha: str
    archive_sha256: str
    manifest_sha256: str
    feedz_count: int
    nuget_count: int
    exclude_ids: tuple[str, ...]
    ancestry_branch: str

    @property
    def artifact_name(self) -> str:
        return f"consolidated-candidate-{self.source_sha}-{self.run_id}-1"


def _is_positive_int(value: object) -> bool:
    return isinstance(value, int) and not isinstance(value, bool) and value > 0


def validate_pin_entry(version: str, entry: object) -> Pin:
    """Check one pin entry field by field and return it; every problem is a SystemExit naming the field."""
    require(isinstance(entry, dict), f"Pin entry for {version} must be an object.")
    missing = [field for field in PIN_FIELDS if field not in entry]
    require(not missing, f"Pin entry for {version} is missing: {', '.join(missing)}.")
    unknown = sorted(set(entry) - set(PIN_FIELDS))
    require(not unknown, f"Pin entry for {version} has unknown fields: {', '.join(unknown)}.")
    for field in ("run_id", "artifact_id", "feedz_count", "nuget_count"):
        require(_is_positive_int(entry[field]), f"Pin {version}.{field} must be a positive integer.")
    require(isinstance(entry["source_sha"], str) and HEX40.fullmatch(entry["source_sha"]),
            f"Pin {version}.source_sha must be 40 lowercase hex characters.")
    for field in ("archive_sha256", "manifest_sha256"):
        require(isinstance(entry[field], str) and HEX64.fullmatch(entry[field]),
                f"Pin {version}.{field} must be 64 lowercase hex characters.")
    exclude = entry["exclude_ids"]
    require(isinstance(exclude, list) and all(isinstance(item, str) and PACKAGE_ID.fullmatch(item) for item in exclude)
            and len(set(exclude)) == len(exclude),
            f"Pin {version}.exclude_ids must be a list of distinct package ID strings.")
    require(isinstance(entry["ancestry_branch"], str) and BRANCH.fullmatch(entry["ancestry_branch"])
            and ".." not in entry["ancestry_branch"],
            f"Pin {version}.ancestry_branch must be a branch name.")
    require(entry["nuget_count"] == entry["feedz_count"] - len(exclude),
            f"Pin {version}.nuget_count must be feedz_count minus the number of exclude_ids.")
    require(entry["run_id"] not in FORBIDDEN_RUNS and entry["artifact_id"] not in FORBIDDEN_ARTIFACTS
            and entry["source_sha"] not in FORBIDDEN_SOURCES,
            f"Pin {version} names the staged 3.10.0 candidate, which must never be promoted.")
    return Pin(version=version, run_id=entry["run_id"], artifact_id=entry["artifact_id"], source_sha=entry["source_sha"],
               archive_sha256=entry["archive_sha256"], manifest_sha256=entry["manifest_sha256"],
               feedz_count=entry["feedz_count"], nuget_count=entry["nuget_count"],
               exclude_ids=tuple(exclude), ancestry_branch=entry["ancestry_branch"])


def load_pin(version: str, pin_file: Path = PIN_FILE) -> Pin:
    require(isinstance(version, str) and VERSION_KEY.fullmatch(version), f"{version!r} is not a version such as 3.10.1.")
    try:
        document = json.loads(pin_file.read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        raise SystemExit(f"Pin file {pin_file} is unreadable: {error}") from None
    require(isinstance(document, dict) and all(isinstance(key, str) and VERSION_KEY.fullmatch(key) for key in document),
            "Pin file must be an object keyed only by version.")
    require(version in document, f"No candidate pin for version {version}; add a reviewed entry to {pin_file.name}.")
    return validate_pin_entry(version, document[version])


def check_run_id(pin: Pin, run_id: int) -> None:
    require(run_id not in FORBIDDEN_RUNS,
            f"Run {run_id} is the staged 3.10.0 candidate run and must never be promoted.")
    require(run_id == pin.run_id, f"Run {run_id} is not the pinned {pin.version} candidate run {pin.run_id}.")


def check_artifact_id(pin: Pin, artifact_id: int) -> None:
    require(artifact_id not in FORBIDDEN_ARTIFACTS,
            f"Artifact {artifact_id} is the staged 3.10.0 candidate artifact and must never be promoted.")
    require(artifact_id == pin.artifact_id,
            f"Artifact {artifact_id} is not the pinned {pin.version} candidate artifact {pin.artifact_id}.")


def check_source(pin: Pin, commit: str, where: str) -> None:
    require(commit not in FORBIDDEN_SOURCES, f"{where} names the 3.10.0 source commit {commit}.")
    require(commit == pin.source_sha, f"{where} {commit!r} is not the pinned source {pin.source_sha}.")


def check_run_head(pin: Pin, head_sha: object, head_branch: object, where: str) -> None:
    """The commit a producer run executed.

    Dispatched from the release branch (3.10.1), that commit is the source itself. Dispatched from main (every later
    version), it is main's tooling commit, and the source is attested by the artifact name, the sealed manifest and the
    ancestry check instead.
    """
    require(head_sha not in FORBIDDEN_SOURCES, f"{where} names the 3.10.0 source commit {head_sha}.")
    require(head_sha == pin.source_sha or (head_branch == "main" and isinstance(head_sha, str) and HEX40.fullmatch(head_sha)),
            f"{where} {head_sha!r} is neither the pinned source {pin.source_sha} nor a commit run from main.")


def check_run(pin: Pin, run: dict) -> None:
    check_run_id(pin, run.get("id"))
    require(run.get("run_attempt") == 1, "Candidate run attempt is not attempt 1.")
    check_run_head(pin, run.get("head_sha"), run.get("head_branch"), "Candidate run head_sha")
    require(run.get("conclusion") == "success" and run.get("status") == "completed", "Candidate run did not succeed.")
    require(run.get("event") == "workflow_dispatch" and run.get("path") in PRODUCER_WORKFLOWS,
            "Candidate run was not produced by a consolidated candidate workflow.")


def check_artifact(pin: Pin, artifact: dict) -> None:
    check_artifact_id(pin, artifact.get("id"))
    require(artifact.get("name") == pin.artifact_name, "Candidate artifact name mismatch.")
    require(artifact.get("expired") is False, "Candidate artifact has expired.")
    require(artifact.get("digest") == f"sha256:{pin.archive_sha256}", "Candidate artifact digest does not match the pin.")
    workflow_run = artifact.get("workflow_run") or {}
    check_run_id(pin, workflow_run.get("id"))
    check_run_head(pin, workflow_run.get("head_sha"), workflow_run.get("head_branch"), "Candidate artifact workflow_run.head_sha")


def check_ancestry(pin: Pin, compare: dict) -> None:
    """compare is GET compare/<ancestry_branch>...<source>; 'behind' or 'identical' means source is on the branch."""
    require(compare.get("status") in ("behind", "identical"),
            f"Source {pin.source_sha} is not an ancestor of {pin.ancestry_branch} (compare status {compare.get('status')!r}).")
    require((compare.get("merge_base_commit") or {}).get("sha") == pin.source_sha,
            "Compare merge base is not the pinned source commit.")


def check_archive(pin: Pin, archive: Path) -> None:
    require(archive.is_file() and not archive.is_symlink(), "Candidate archive missing.")
    require(sha256_file(archive) == pin.archive_sha256, "Candidate archive sha256 does not match the pin.")


def read_manifest(pin: Pin, archive: Path) -> dict:
    with zipfile.ZipFile(archive) as zipped:
        require(SEAL_NAME in zipped.namelist(), "Candidate seal missing.")
        data = zipped.read(SEAL_NAME)
    require(hashlib.sha256(data).hexdigest() == pin.manifest_sha256, "Candidate manifest sha256 does not match the pin.")
    manifest = json.loads(data)
    require(manifest.get("schema") == 1 and manifest.get("published") is False and manifest.get("version") == pin.version
            and manifest.get("run_id") == pin.run_id and manifest.get("run_attempt") == 1,
            "Candidate manifest identity does not match the pin.")
    check_source(pin, manifest.get("source_commit"), "Candidate manifest source_commit")
    return manifest


def sealed_packages(pin: Pin, manifest: dict) -> dict[str, str]:
    """{file name: sha256} for every sealed .nupkg/.snupkg; all must be the pinned version."""
    sealed: dict[str, str] = {}
    for row in manifest.get("files") or []:
        name = PurePosixPath(row["path"]).name
        match = PACKAGE_FILE.fullmatch(name)
        if match is None or not row["path"].startswith("artifacts/"):
            continue
        require(match.group("version") == pin.version, f"Sealed package {name} is not version {pin.version}.")
        require(name not in sealed, f"Duplicate sealed package {name}.")
        sealed[name] = row["sha256"]
    nupkgs = [name for name in sealed if name.endswith(".nupkg")]
    require(len(nupkgs) == pin.feedz_count, f"Candidate seals {len(nupkgs)} .nupkg files, expected {pin.feedz_count}.")
    require(len(sealed) == 2 * pin.feedz_count, "Candidate seals an unexpected number of .snupkg files.")
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


def verify_transport(pin: Pin, transport: Path, stage: Path | None = None) -> list[Path]:
    require(transport.is_dir(), "Transport directory missing.")
    for name in TRANSPORT_FILES:
        require((transport / name).is_file() and not (transport / name).is_symlink(), f"Transport file {name} missing.")
    run = json.loads((transport / "run.json").read_text())
    artifact = json.loads((transport / "artifact.json").read_text())
    check_run(pin, run)
    check_artifact(pin, artifact)
    require((artifact.get("workflow_run") or {}).get("head_sha") == run.get("head_sha"),
            "Candidate artifact and run were not produced from the same commit.")
    check_ancestry(pin, json.loads((transport / "compare.json").read_text()))
    archive = transport / "candidate.zip"
    check_archive(pin, archive)
    sealed = sealed_packages(pin, read_manifest(pin, archive))
    return stage_packages(archive, sealed, stage) if stage else []


def expected_count(pin: Pin, feed: str) -> int:
    require(feed in ("feedz", "nuget"), "Feed must be feedz or nuget.")
    return pin.feedz_count if feed == "feedz" else pin.nuget_count


def count_nupkgs(directory: Path, expected: int) -> None:
    found = sorted(path.name for path in directory.glob("*.nupkg"))
    require(len(found) == expected, f"Found {len(found)} .nupkg files in {directory}, expected {expected}.")
    print(f"{len(found)} .nupkg files, as expected.")


def gh_api(endpoint: str, destination: Path) -> None:
    with destination.open("wb") as out:
        subprocess.run(["gh", "api", endpoint], check=True, stdout=out)


def retrieve(pin: Pin, output: Path) -> None:
    require(any(os.environ.get(name) for name in TOKEN_NAMES), "Retrieve requires GH_TOKEN.")
    output.mkdir(parents=True, exist_ok=True)
    base = f"repos/{REPOSITORY}"
    gh_api(f"{base}/actions/runs/{pin.run_id}/attempts/1", output / "run.json")
    gh_api(f"{base}/actions/artifacts/{pin.artifact_id}", output / "artifact.json")
    gh_api(f"{base}/compare/{pin.ancestry_branch}...{pin.source_sha}", output / "compare.json")
    # The checks run before the (large) zip is fetched, so a wrong run, artifact or source fails cheaply.
    check_run(pin, json.loads((output / "run.json").read_text()))
    check_artifact(pin, json.loads((output / "artifact.json").read_text()))
    check_ancestry(pin, json.loads((output / "compare.json").read_text()))
    gh_api(f"{base}/actions/artifacts/{pin.artifact_id}/zip", output / "candidate.zip")
    verify_transport(pin, output)
    print(f"Fetched and verified candidate {pin.version} run {pin.run_id} artifact {pin.artifact_id} source {pin.source_sha}.")


def pin_output(pin: Pin, feed: str) -> str:
    """Step outputs for a publish job: the pinned exclusions (nuget only) and the expected count."""
    exclude = ",".join(pin.exclude_ids) if feed == "nuget" else ""
    return f"exclude_ids={exclude}\nexpected_count={expected_count(pin, feed)}\n"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    for name in ("validate-pin", "retrieve", "verify", "pin-output", "count"):
        command = sub.add_parser(name)
        command.add_argument("--version", required=True)
        if name == "retrieve":
            command.add_argument("--output", type=Path, required=True)
        elif name == "verify":
            command.add_argument("--transport", type=Path, required=True)
            command.add_argument("--stage", type=Path)
        elif name in ("pin-output", "count"):
            command.add_argument("--feed", required=True)
            if name == "count":
                command.add_argument("--dir", type=Path, required=True)
    args = parser.parse_args(argv)
    pin = load_pin(args.version)
    if args.command == "validate-pin":
        print(f"Pin entry for {pin.version} is valid.")
    elif args.command == "retrieve":
        retrieve(pin, args.output)
    elif args.command == "verify":
        require(not any(os.environ.get(name) for name in TOKEN_NAMES), "A GitHub token must not be present while verifying or staging.")
        staged = verify_transport(pin, args.transport, args.stage)
        print(f"Verified candidate {pin.version}; staged {len([p for p in staged if p.suffix == '.nupkg'])} .nupkg files." if args.stage
              else f"Verified candidate {pin.version}.")
    elif args.command == "pin-output":
        sys.stdout.write(pin_output(pin, args.feed))
    else:
        count_nupkgs(args.dir, expected_count(pin, args.feed))
    return 0


if __name__ == "__main__":
    sys.exit(main())
