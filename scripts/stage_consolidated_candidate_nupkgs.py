#!/usr/bin/env python3
"""Stage the pinned 3.10.0 candidate nupkgs for a nuget.org promotion.

packages.yml publish_nuget calls this for stable 3.10.0 instead of rebuilding.
It never publishes. Retrieve downloads the pinned run/artifact. Stage verifies
each .nupkg sha256 against the candidate seal, then the caller runs the ID-set
and Studio content guards on those bytes. Retrieve uses GH_TOKEN only to
download the archive; stage refuses to run while that token is present.
"""

from __future__ import annotations

import hashlib
import json
import os
import subprocess
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path, PurePosixPath

sys.path.insert(0, str(Path(__file__).resolve().parent / "integration-program"))

import consolidated_candidate_input as candidate_input

from validate_packages_id_set import PACKAGE_FILE, is_stable_310

PINNED_TAG = f"refs/tags/{candidate_input.PRODUCER['version']}"
CANDIDATE_NAME = (
    f"consolidated-candidate-{candidate_input.PRODUCER['source_commit']}-"
    f"{candidate_input.PRODUCER['run_id']}-{candidate_input.PRODUCER['run_attempt']}"
)
CANDIDATE_SIZE = 92659861
CANDIDATE_EXPIRES = "2026-11-05T12:13:30Z"
SEAL_NAME = "preupload-manifest.json"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def require_pinned_stable_tag(ref: str) -> None:
    require(ref == PINNED_TAG, f"Stable 3.10.x nuget.org promotion is pinned to {PINNED_TAG}; got {ref!r}.")
    tag = ref[len("refs/tags/"):]
    require(is_stable_310(tag), f"Tag {tag!r} is not a canonical stable 3.10.x version.")


def extract_nupkgs(archive: Path, output: Path) -> list[Path]:
    """Copy *.nupkg members from the candidate archive into output, flat."""
    require(archive.is_file() and not archive.is_symlink(), "Candidate archive missing.")
    output.mkdir(parents=True, exist_ok=True)
    staged: list[Path] = []
    with zipfile.ZipFile(archive) as zipped:
        for member in zipped.infolist():
            name = PurePosixPath(member.filename).name
            if member.is_dir() or not PACKAGE_FILE.fullmatch(name):
                continue
            require(".." not in PurePosixPath(member.filename).parts, f"Unsafe candidate entry: {member.filename}")
            destination = output / name
            require(not destination.exists(), f"Duplicate candidate package: {name}")
            with zipped.open(member) as reader, destination.open("xb") as writer:
                writer.write(reader.read())
            staged.append(destination)
    require(bool(staged), f"Candidate archive {archive} contains no .nupkg files.")
    return staged


def file_sha256(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def sealed_nupkg_hashes(archive: Path, expected_manifest_sha256: str) -> dict[str, str]:
    """Read {nupkg filename: sha256} from the candidate seal and check the seal hash."""
    require(archive.is_file() and not archive.is_symlink(), "Candidate archive missing.")
    with zipfile.ZipFile(archive) as zipped:
        require(SEAL_NAME in zipped.namelist(), "Candidate seal missing.")
        data = zipped.read(SEAL_NAME)
    require(hashlib.sha256(data).hexdigest() == expected_manifest_sha256, "Candidate seal hash mismatch.")
    manifest = json.loads(data)
    require(
        manifest.get("schema") == 1 and manifest.get("published") is False and manifest.get("version") == "3.10.0",
        "Candidate seal identity mismatch.",
    )
    hashes: dict[str, str] = {}
    for row in manifest.get("files") or []:
        name = PurePosixPath(row["path"]).name
        if not PACKAGE_FILE.fullmatch(name):
            continue
        require(name not in hashes, f"Duplicate sealed package: {name}")
        require(isinstance(row.get("sha256"), str) and bool(row["sha256"]), f"Sealed package {name} is missing sha256.")
        hashes[name] = row["sha256"]
    require(bool(hashes), "Candidate seal lists no .nupkg files.")
    return hashes


def verify_nupkg_hashes(staged: list[Path], sealed: dict[str, str]) -> None:
    """Fail closed when staged bytes differ from the sealed sha256 set."""
    names = {path.name for path in staged}
    missing = sorted(set(sealed) - names)
    extra = sorted(names - set(sealed))
    require(not missing and not extra, "Staged nupkgs do not match the candidate seal.")
    for path in staged:
        digest = file_sha256(path)
        require(digest == sealed[path.name], f"Candidate nupkg hash mismatch: {path.name}")


def retrieve_archive(destination: Path) -> None:
    token_names = ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")
    require(any(os.environ.get(name) for name in token_names), "Retrieve requires GH_TOKEN.")
    destination.parent.mkdir(parents=True, exist_ok=True)
    artifact_id = candidate_input.PRODUCER["artifact_id"]
    run_id = candidate_input.PRODUCER["run_id"]
    attempt = candidate_input.PRODUCER["run_attempt"]
    source = candidate_input.PRODUCER["source_commit"]
    metadata_path = destination.with_name("artifact.json")
    run_path = destination.with_name("producer-run.json")
    _gh_api(f"repos/elsa-workflows/elsa-core/actions/artifacts/{artifact_id}", metadata_path)
    _gh_api(f"repos/elsa-workflows/elsa-core/actions/runs/{run_id}/attempts/{attempt}", run_path)
    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    producer = json.loads(run_path.read_text(encoding="utf-8"))
    require(
        producer.get("id") == run_id
        and producer.get("run_attempt") == attempt
        and producer.get("head_sha") == source
        and producer.get("conclusion") == "success"
        and producer.get("event") in ("push", "workflow_dispatch")
        and producer.get("path") == ".github/workflows/prepare-consolidated-release-candidate.yml",
        "Pinned candidate producer run identity mismatch.",
    )
    require(
        metadata.get("id") == artifact_id
        and metadata.get("name") == CANDIDATE_NAME
        and metadata.get("expired") is False
        and metadata.get("expires_at") == CANDIDATE_EXPIRES
        and metadata.get("digest") == f"sha256:{candidate_input.ARCHIVE_SHA256}"
        and metadata.get("size_in_bytes") == CANDIDATE_SIZE
        and metadata.get("workflow_run", {}).get("id") == run_id
        and metadata.get("workflow_run", {}).get("head_sha") == source,
        "Pinned candidate artifact identity mismatch.",
    )
    expiry = datetime.fromisoformat(CANDIDATE_EXPIRES.replace("Z", "+00:00"))
    require(expiry > datetime.now(timezone.utc), "Pinned candidate artifact expired.")
    _gh_api(f"repos/elsa-workflows/elsa-core/actions/artifacts/{artifact_id}/zip", destination)
    require(destination.stat().st_size == CANDIDATE_SIZE, "Pinned candidate archive size mismatch.")
    with destination.open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    require(digest == candidate_input.ARCHIVE_SHA256, "Pinned candidate archive hash mismatch.")


def _gh_api(path: str, output: Path) -> None:
    with output.open("wb") as handle:
        subprocess.run(["gh", "api", path], check=True, stdout=handle)


def main(argv: list[str] | None = None) -> int:
    args = list(sys.argv[1:] if argv is None else argv)
    require(args and args[0] in ("retrieve", "stage"), "Usage: stage_consolidated_candidate_nupkgs.py retrieve|stage")
    command = args[0]
    require_pinned_stable_tag(os.environ.get("REF", ""))
    archive = Path(os.environ.get("CANDIDATE_ARCHIVE") or "")
    require(bool(os.environ.get("CANDIDATE_ARCHIVE")), "CANDIDATE_ARCHIVE must be set.")
    if command == "retrieve":
        retrieve_archive(archive)
        print(f"Retrieved pinned candidate archive {archive}.")
        return 0
    for token in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN"):
        require(not os.environ.get(token), "Artifact-reading token must not enter candidate staging.")
    packages = Path(os.environ.get("PACKAGES_DIR") or "")
    require(bool(os.environ.get("PACKAGES_DIR")), "PACKAGES_DIR must be set.")
    sealed = sealed_nupkg_hashes(archive, candidate_input.MANIFEST_SHA256)
    staged = extract_nupkgs(archive, packages)
    verify_nupkg_hashes(staged, sealed)
    print(f"Staged {len(staged)} candidate nupkgs into {packages}; each sha256 matched the candidate seal.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
