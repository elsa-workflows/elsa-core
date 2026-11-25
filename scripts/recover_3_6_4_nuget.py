#!/usr/bin/env python3
"""Verify and stage the original Elsa 3.6.4 GitHub release artifact."""

import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from zipfile import ZipFile

REPOSITORY = "elsa-workflows/elsa-core"
VERSION = "3.6.4"
SOURCE = "874e670e4ca405359a058bfee2ed9ef2a859d98c"
TAG_OBJECT = "2120bde3eae5e1bde84bee36d6d8f41c56ef998e"
RELEASE_RUN = 45546881553
FAILED_JOB = 138982310359
ARTIFACT = {
    "id": 14993170259,
    "name": "elsa-nuget-packages",
    "digest": "sha256:73da3a79cfd77afc81eddb1f8a948371a7937c48e35a63d64322a2716b3fb427",
    "size_in_bytes": 9721999,
}


def github(endpoint):
    return json.loads(subprocess.check_output(["gh", "api", f"repos/{REPOSITORY}/{endpoint}"]))


def require_fields(record, **expected):
    for key, value in expected.items():
        if record.get(key) != value:
            raise ValueError(f"Unexpected GitHub metadata for {key}")


def verify_original_release():
    release = github(f"releases/tags/{VERSION}")
    require_fields(release, id=457652093, tag_name=VERSION, target_commitish=SOURCE, prerelease=False, draft=False)
    reference = github(f"git/ref/tags/{VERSION}")
    require_fields(reference["object"], sha=TAG_OBJECT, type="tag")
    require_fields(github(f"git/tags/{TAG_OBJECT}")["object"], sha=SOURCE, type="commit")

    attempt = f"actions/runs/{RELEASE_RUN}/attempts/1"
    require_fields(
        github(attempt),
        id=RELEASE_RUN, run_attempt=1, head_sha=SOURCE, head_branch=VERSION,
        event="release", path=".github/workflows/packages.yml", conclusion="failure",
    )
    jobs = github(f"{attempt}/jobs")["jobs"]
    expected = {
        "Test with coverage": "success",
        "Build packages": "success",
        "Publish to feedz.io": "success",
        "Deploy coverage to GitHub Pages": "skipped",
        "Publish release to nuget.org": "failure",
    }
    failed = [job["id"] for job in jobs if job["conclusion"] == "failure"]
    if len(jobs) != len(expected) or {job["name"]: job["conclusion"] for job in jobs} != expected or failed != [FAILED_JOB]:
        raise ValueError("The original release did not have exactly the expected NuGet publication failure")

    metadata = github(f"actions/artifacts/{ARTIFACT['id']}")
    require_fields(metadata, **ARTIFACT, expired=False)
    require_fields(metadata["workflow_run"], id=RELEASE_RUN, head_sha=SOURCE, head_branch=VERSION)


def stage_packages(archive_path, destination):
    digest = hashlib.sha256(archive_path.read_bytes()).hexdigest()
    if archive_path.stat().st_size != ARTIFACT["size_in_bytes"] or f"sha256:{digest}" != ARTIFACT["digest"]:
        raise ValueError("The downloaded release archive does not match the original artifact")

    with ZipFile(archive_path) as archive:
        entries = archive.namelist()
        packages = [name for name in entries if name.endswith(".nupkg")]
        expected_entries = set(packages) | {f"{name[:-6]}.snupkg" for name in packages}
        if len(packages) != 44 or len(entries) != 88 or set(entries) != expected_entries:
            raise ValueError("Expected exactly 44 original NuGet and symbol package pairs")

        package_ids = []
        for name in packages:
            if Path(name).name != name or "\\" in name:
                raise ValueError("Unexpected package path in original release archive")

            with ZipFile(io.BytesIO(archive.read(name))) as package:
                nuspecs = [path for path in package.namelist() if path.endswith(".nuspec") and "/" not in path]
                if len(nuspecs) != 1:
                    raise ValueError(f"Expected one nuspec in {name}")
                metadata = ET.fromstring(package.read(nuspecs[0])).find("{*}metadata")
                package_id = metadata.findtext("{*}id")
                repository = metadata.find("{*}repository")
                if (not package_id or name != f"{package_id}.{VERSION}.nupkg"
                        or metadata.findtext("{*}version") != VERSION or repository is None
                        or repository.get("commit") != SOURCE
                        or repository.get("url") != f"https://github.com/{REPOSITORY}"):
                    raise ValueError(f"Unexpected package identity or source in {name}")
                package_ids.append(package_id)

        if len({package_id.lower() for package_id in package_ids}) != len(packages):
            raise ValueError("Duplicate package IDs in original release artifact")

        if destination is not None:
            for name in sorted(expected_entries):
                with archive.open(name) as source, (destination / name).open("xb") as output:
                    shutil.copyfileobj(source, output)

    return sorted(package_ids)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify-only", action="store_true", help="Check live provenance and package bytes without staging")
    args = parser.parse_args()
    if not args.verify_only:
        require_fields(os.environ, GITHUB_REPOSITORY=REPOSITORY, GITHUB_REF="refs/heads/main", GITHUB_EVENT_NAME="workflow_dispatch")
        if any(Path.cwd().glob("*.nupkg")) or any(Path.cwd().glob("*.snupkg")):
            raise ValueError("The publisher workspace already contains packages")

    verify_original_release()
    with tempfile.TemporaryDirectory(prefix="elsa-3.6.4-recovery-") as temporary:
        archive_path = Path(temporary) / "original.zip"
        with archive_path.open("wb") as output:
            subprocess.run(["gh", "api", f"repos/{REPOSITORY}/actions/artifacts/{ARTIFACT['id']}/zip"], stdout=output, check=True)

        package_ids = stage_packages(archive_path, None if args.verify_only else Path.cwd())

    target = {"registry": "nuget.org", "version": VERSION, "package_ids": package_ids}
    if not args.verify_only:
        receipt = {
            "schema": 1,
            "repository": REPOSITORY,
            "version": VERSION,
            "original_source_commit": SOURCE,
            "original_release_run_id": RELEASE_RUN,
            "original_artifact": {**ARTIFACT, "run_id": RELEASE_RUN},
            "recovery_run_id": int(os.environ["GITHUB_RUN_ID"]),
            "recovery_workflow_sha": os.environ["GITHUB_SHA"],
            "target": target,
        }
        (Path(os.environ["RUNNER_TEMP"]) / "recovery-receipt.json").write_text(
            json.dumps(receipt, indent=2) + "\n", encoding="utf-8"
        )
    print(f"Verified {len(package_ids)} package and symbol pairs from artifact {ARTIFACT['id']}")


if __name__ == "__main__":
    main()
