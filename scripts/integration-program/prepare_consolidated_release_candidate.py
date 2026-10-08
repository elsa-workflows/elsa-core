#!/usr/bin/env python3
"""Prepare unpublished stable bytes or verify their original immutable CI archive."""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import zipfile

import prove_consolidated_packages as packages
from product_layout import map_path

MANIFEST = "preupload-manifest.json"
LEGACY_SECRETS = {f"Elsa.Secrets.{suffix}" for suffix in ("Api", "Core", "Management", "Models", "Scripting")}


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def file_sha256(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def inventory_identity(manifest: dict) -> dict:
    return {"packages": sorted(({key: row[key] for key in ("id", "project")} for row in manifest["packages"]), key=lambda row: row["project"]),
            "exclusions": sorted(({key: row[key] for key in ("id", "project", "reason")} for row in manifest["exclusions"]), key=lambda row: row["project"])}


def compare_inventory(manifest: dict, baseline: dict, output: Path) -> None:
    actual = inventory_identity(manifest)
    expected = inventory_identity(baseline)
    for rows in expected.values():
        for row in rows:
            row["project"] = map_path(row["project"])
    differences = {}
    for category in actual:
        before = {json.dumps(row, sort_keys=True) for row in expected[category]}
        after = {json.dumps(row, sort_keys=True) for row in actual[category]}
        differences[category] = {"removed": [json.loads(row) for row in sorted(before - after)],
                                 "added": [json.loads(row) for row in sorted(after - before)]}
    packages.write_json(output, {"baseline": baseline.get("evidence"), "changes": differences})
    packages.require(all(not delta[key] for delta in differences.values() for key in ("removed", "added")),
                     f"Candidate inventory changed; review {output} and commit a reviewed baseline update before building")
    included = {row["id"] for row in manifest["packages"]}
    excluded = {row["id"] for row in manifest["exclusions"]}
    packages.require(LEGACY_SECRETS <= excluded and not LEGACY_SECRETS & included, "Legacy Secrets package exclusion changed")
    sample = next((row for row in manifest["packages"] if row["id"] == "Elsa.SamplePackage"), None)
    packages.require(sample is not None and sample["nupkg"] == "Elsa.SamplePackage.3.10.0.nupkg", "SamplePackage must use common candidate 3.10.0")


def seal(root: Path, source: str) -> dict:
    packages.require(not (root / MANIFEST).exists(), "Refusing to reseal a candidate")
    files = []
    for path in sorted(root.rglob("*")):
        packages.require(not path.is_symlink(), "Candidate contains a symlink")
        if path.is_file():
            data = path.read_bytes()
            files.append({"path": path.relative_to(root).as_posix(), "size": len(data), "sha256": sha256(data)})
    manifest = {"schema": 1, "published": False, "version": "3.10.0", "source_commit": source,
                "run_id": int(os.environ.get("GITHUB_RUN_ID", "0")),
                "run_attempt": int(os.environ.get("GITHUB_RUN_ATTEMPT", "0")), "files": files}
    packages.write_json(root / MANIFEST, manifest)
    return manifest


def validate_envelope(envelope: dict, metadata: dict, *, source: str, run_id: int, attempt: int) -> None:
    expected_name = f"consolidated-candidate-{source}-{run_id}-{attempt}"
    packages.require(bool(re.fullmatch(r"[0-9a-f]{40}", source)), "Invalid candidate source SHA")
    workflow = metadata.get("workflow_run", {})
    packages.require(envelope.get("schema") == 1 and envelope.get("published") is False and
                     envelope.get("version") == "3.10.0" and envelope.get("source_commit") == source and
                     envelope.get("run_id") == run_id and envelope.get("run_attempt") == attempt,
                     "Candidate envelope source/run identity mismatch")
    packages.require(type(envelope.get("artifact_id")) is int and envelope["artifact_id"] > 0 and
                     envelope["artifact_id"] == metadata.get("id") and envelope.get("artifact_name") == expected_name and
                     metadata.get("name") == expected_name and workflow.get("id") == run_id and workflow.get("head_sha") == source,
                     "Candidate artifact ID/name/source mismatch or missing artifact")
    packages.require(metadata.get("expired") is False and envelope.get("expires_at") == metadata.get("expires_at"),
                     "Candidate artifact is missing or expired")
    observed = datetime.fromisoformat(envelope["retrieved_at"].replace("Z", "+00:00"))
    packages.require(observed.tzinfo is not None and observed <= datetime.now(timezone.utc), "Invalid retrieval snapshot timestamp")
    expiry = datetime.fromisoformat(envelope["expires_at"].replace("Z", "+00:00"))
    packages.require(expiry.tzinfo is not None and expiry > datetime.now(timezone.utc), "Candidate artifact expired")
    packages.require(envelope.get("retention_days") == 30, "Candidate retention must be 30 days")
    for key in ("archive_sha256", "preupload_manifest_sha256"):
        packages.require(isinstance(envelope.get(key), str) and bool(re.fullmatch(r"[0-9a-f]{64}", envelope[key])), "Missing candidate digest")
    packages.require(metadata.get("digest") == "sha256:" + envelope["archive_sha256"] and
                     type(envelope.get("archive_size")) is int and envelope["archive_size"] > 0 and
                     metadata.get("size_in_bytes") == envelope["archive_size"], "Artifact digest/size metadata mismatch")


def safe_file_name(name: str) -> None:
    path = PurePosixPath(name)
    packages.require(bool(name) and not path.is_absolute() and ".." not in path.parts and
                     "\\" not in name and ":" not in name and str(path) == name.rstrip("/"), "Unsafe candidate archive entry")


def verify_and_extract(archive: Path, envelope: dict, metadata: dict, destination: Path,
                       *, source: str, run_id: int, attempt: int) -> dict:
    validate_envelope(envelope, metadata, source=source, run_id=run_id, attempt=attempt)
    packages.require(archive.is_file() and not archive.is_symlink(), "Original candidate archive missing")
    packages.require(archive.stat().st_size == envelope["archive_size"] and
                     file_sha256(archive) == envelope["archive_sha256"], "Original candidate archive hash/size mismatch")
    packages.require(not destination.exists(), "Refusing to overwrite extracted candidate")
    with zipfile.ZipFile(archive) as zipped:
        members = zipped.infolist()
        names = set()
        files = {}
        for member in members:
            safe_file_name(member.filename)
            mode = member.external_attr >> 16
            packages.require(not stat.S_ISLNK(mode) and (stat.S_IFMT(mode) in (0, stat.S_IFREG, stat.S_IFDIR)), "Unsafe candidate archive entry type")
            packages.require(member.filename.casefold() not in names, "Duplicate candidate archive entry")
            names.add(member.filename.casefold())
            if not member.is_dir():
                files[member.filename] = member
        packages.require(MANIFEST in files, "Pre-upload manifest missing")
        manifest_data = zipped.read(files[MANIFEST])
        packages.require(sha256(manifest_data) == envelope["preupload_manifest_sha256"], "Pre-upload manifest hash mismatch")
        manifest = json.loads(manifest_data)
        packages.require(manifest.get("schema") == 1 and manifest.get("published") is False and
                         manifest.get("version") == "3.10.0" and manifest.get("source_commit") == source and
                         manifest.get("run_id") == run_id and manifest.get("run_attempt") == attempt, "Pre-upload manifest identity mismatch")
        expected = {}
        for row in manifest["files"]:
            safe_file_name(row["path"])
            packages.require(row["path"].casefold() not in {name.casefold() for name in expected} and row["path"] != MANIFEST,
                             "Duplicate or circular pre-upload file manifest")
            expected[row["path"]] = row
        packages.require(set(files) == set(expected) | {MANIFEST}, "Missing/unlisted candidate files")
        for name, row in expected.items():
            with zipped.open(files[name]) as reader:
                digest = hashlib.file_digest(reader, "sha256").hexdigest()
            packages.require(files[name].file_size == row["size"] and digest == row["sha256"], f"Candidate file hash mismatch: {name}")
        # Every byte and member is checked before the first filesystem extraction.
        destination.mkdir(parents=True)
        for name, member in files.items():
            path = destination / name
            path.parent.mkdir(parents=True, exist_ok=True)
            with zipped.open(member) as reader, path.open("xb") as writer:
                shutil.copyfileobj(reader, writer)
    return manifest


def main() -> None:
    if "--consume" not in os.sys.argv:
        packages.main(mode="candidate")
        return
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--consume", action="store_true")
    parser.add_argument("--archive", required=True, type=Path)
    parser.add_argument("--envelope", required=True, type=Path)
    parser.add_argument("--metadata", required=True, type=Path)
    parser.add_argument("--source", required=True)
    parser.add_argument("--run-id", required=True, type=int)
    parser.add_argument("--attempt", required=True, type=int)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    for token in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN"):
        packages.require(not os.environ.get(token), "Artifact-reading token must not enter candidate consumers")
    envelope = json.loads(args.envelope.read_text())
    verify_and_extract(args.archive, envelope, json.loads(args.metadata.read_text()), args.output,
                       source=args.source, run_id=args.run_id, attempt=args.attempt)
    manifest = json.loads((args.output / "verified-artifacts.json").read_text())
    packages.require(manifest["version"] == "3.10.0" and manifest["source_commit"] == args.source and manifest["published"] is False,
                     "Verified inventory does not identify candidate")
    from prove_consolidated_package_consumers import prove
    result = prove(args.output / "artifacts", manifest, args.output.parent / "candidate-consumers")
    packages.write_json(args.output.parent / "candidate-consumer-receipt.json", {"result": "passed", "published": False,
                        "envelope": envelope, "consumers": result,
                        "availability": {"scope": "retrieval_time_snapshot", "observed_at": envelope["retrieved_at"],
                                         "consumer_scope": "exact_transferred_original_bytes_with_expiry_checked_before_execution"},
                        "limits": ["Representative consumers only; persisted compatibility against these exact stable bytes remains required.",
                                   "Earlier proof bytes and future approved publication are separate identities and gates.",
                                   "Original artifact availability is a retrieval-time snapshot; deletion after retrieval is not observed by token-minimal consumers.",
                                   "Before approval or publication, recheck live original artifact ID, digest, source SHA, run identity and expiry; deleted or expired artifacts invalidate approval."]})


if __name__ == "__main__":
    main()
