#!/usr/bin/env python3
"""Prepare unpublished stable bytes or verify their original immutable CI archive.

This script and the rest of the tooling run from the workflow's own ref (main). The product source being packed is a
separate checkout at the candidate commit, named with --source-root; its release line is release/<major>.<minor>.0.
"""
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
import subprocess
import xml.etree.ElementTree as ET
import zipfile

import prove_consolidated_packages as packages
from product_layout import map_path

MANIFEST = "preupload-manifest.json"
LEGACY_SECRETS = {f"Elsa.Secrets.{suffix}" for suffix in ("Api", "Core", "Management", "Models", "Scripting")}
ORIGINAL_3100_SOURCE = "5cff67228e550b353b3483eb3b37dfdb73a41c74"
ORIGINAL_3100_ARTIFACT = 12664519771
VERSION_PATTERN = re.compile(r"([0-9]+)\.([0-9]+)\.[0-9]+")
CANDIDATE_TFMS = ("net8.0", "net9.0", "net10.0")
TFM_EQ = re.compile(r"\$\(TargetFramework\)['\"]\s*==\s*['\"](net[0-9]+\.[0-9]+)['\"]")
TFM_NE = re.compile(r"\$\(TargetFramework\)['\"]\s*!=\s*['\"](net[0-9]+\.[0-9]+)['\"]")
PKCS_ASN1_BY_TFM = {"net8.0": "8.0.2", "net9.0": "9.0.21", "net10.0": "10.0.13"}
CONNECTOR_BY_TFM = {tfm: "2.6.2" for tfm in CANDIDATE_TFMS}
SQLITE_BY_TFM = {tfm: "2.1.14" for tfm in CANDIDATE_TFMS}
SIGNALR_BY_TFM = {"net8.0": "8.0.32", "net9.0": "9.0.21", "net10.0": "10.0.13"}
REVIEWED_PIN_FLOORS = {
    "Directory.Packages.props": {
        "MySqlConnector": CONNECTOR_BY_TFM,
        "SQLitePCLRaw.lib.e_sqlite3": SQLITE_BY_TFM,
        "System.Security.Cryptography.Pkcs": PKCS_ASN1_BY_TFM,
        "System.Formats.Asn1": PKCS_ASN1_BY_TFM,
    },
    "extensions/src/Directory.Packages.props": {
        "MySqlConnector": CONNECTOR_BY_TFM,
        "SQLitePCLRaw.lib.e_sqlite3": SQLITE_BY_TFM,
        "System.Security.Cryptography.Pkcs": PKCS_ASN1_BY_TFM,
        "System.Formats.Asn1": PKCS_ASN1_BY_TFM,
    },
    "studio/src/Directory.Packages.props": {
        "Microsoft.AspNetCore.SignalR.Client": SIGNALR_BY_TFM,
    },
}
DROPINS_PROJECT = "extensions/src/dropins/Elsa.DropIns/Elsa.DropIns.csproj"
DROPINS_NET8_OVERRIDE = {
    "System.Security.Cryptography.Pkcs": "9.0.21",
    "System.Formats.Asn1": "9.0.21",
}


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
    packages.require("Elsa.SamplePackage" in excluded and "Elsa.SamplePackage" not in included, "SamplePackage must remain unpackable")


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def tfms_for_condition(condition: str | None) -> frozenset[str]:
    text = (condition or "").strip()
    if not text:
        return frozenset(CANDIDATE_TFMS)
    equals = tuple(TFM_EQ.findall(text))
    unequals = tuple(TFM_NE.findall(text))
    packages.require(bool(equals or unequals), f"Unrecognized package condition: {text}")
    allowed = set(CANDIDATE_TFMS)
    if equals:
        allowed &= set(equals)
    for tfm in unequals:
        allowed.discard(tfm)
    return frozenset(allowed)


def iter_conditioned_items(root: ET.Element, tag: str):
    for group in root.iter():
        if local_name(group.tag) != "ItemGroup":
            continue
        group_tfms = tfms_for_condition(group.get("Condition"))
        for item in list(group):
            if local_name(item.tag) != tag:
                continue
            yield item, group_tfms & tfms_for_condition(item.get("Condition"))


def resolve_cpm_versions(path: Path, wanted: dict[str, dict[str, str]]) -> dict[str, dict[str, str]]:
    resolved: dict[str, dict[str, str]] = {}
    for item, tfms in iter_conditioned_items(ET.fromstring(path.read_text(encoding="utf-8")), "PackageVersion"):
        package_id = item.get("Include")
        version = item.get("Version")
        if package_id not in wanted:
            continue
        packages.require(bool(version), f"{path.name} PackageVersion {package_id} is missing Version")
        packages.require(bool(tfms), f"{path.name} PackageVersion {package_id} {version} applies to no candidate TFM")
        bucket = resolved.setdefault(package_id, {})
        for tfm in tfms:
            if tfm in bucket and bucket[tfm] != version:
                raise ValueError(f"{path.name} has conflicting {package_id} pins for {tfm}: {bucket[tfm]} and {version}")
            bucket[tfm] = version
    return resolved


def release_line(version: str) -> str:
    """The branch a version ships from: 3.10.1 and 3.10.2 are both packed from release/3.10.0."""
    match = VERSION_PATTERN.fullmatch(version)
    packages.require(match is not None, f"{version!r} is not a version such as 3.10.1")
    return f"release/{match.group(1)}.{match.group(2)}.0"


def version_key(version: str) -> tuple[int, ...]:
    packages.require(bool(re.fullmatch(r"[0-9]+(\.[0-9]+)*", version)), f"Unsupported pin version {version!r}")
    return tuple(int(part) for part in version.split("."))


def require_dropins_net8_overrides(root: Path) -> None:
    path = root / DROPINS_PROJECT
    packages.require(path.is_file(), f"Candidate source is missing {DROPINS_PROJECT}")
    found: dict[str, str] = {}
    for item, tfms in iter_conditioned_items(ET.fromstring(path.read_text(encoding="utf-8")), "PackageReference"):
        package_id = item.get("Include")
        override = item.get("VersionOverride")
        if package_id not in DROPINS_NET8_OVERRIDE or not override:
            continue
        packages.require(tfms == frozenset({"net8.0"}),
                         f"{DROPINS_PROJECT} {package_id} VersionOverride {override} must be net8.0-only")
        if package_id in found and found[package_id] != override:
            raise ValueError(f"{DROPINS_PROJECT} has conflicting net8 VersionOverride for {package_id}")
        found[package_id] = override
    for package_id, version in DROPINS_NET8_OVERRIDE.items():
        actual = found.get(package_id)
        packages.require(actual is not None and version_key(actual) >= version_key(version),
                         f"Candidate source is missing Elsa.DropIns net8 VersionOverride {package_id} {version}"
                         + (f" (found {actual})" if actual else ""))


def require_reviewed_pins(root: Path) -> None:
    for relative, expected in REVIEWED_PIN_FLOORS.items():
        path = root / relative
        packages.require(path.is_file(), f"Candidate source is missing {relative}")
        resolved = resolve_cpm_versions(path, expected)
        for package_id, tfm_versions in expected.items():
            for tfm, version in tfm_versions.items():
                actual = resolved.get(package_id, {}).get(tfm)
                packages.require(actual is not None and version_key(actual) >= version_key(version),
                                 f"Candidate source is missing reviewed pin {package_id} {version} (or newer) for {tfm} in {relative}"
                                 + (f" (found {actual})" if actual else ""))
    require_dropins_net8_overrides(root)


def git_run(root: Path, *arguments: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(["git", "-C", str(root), *arguments], capture_output=True, text=True)


def require_release_line(root: Path, source: str, version: str) -> None:
    """The candidate commit must be on the version's release branch of the source checkout (--source-root).

    No ref of the running workflow matters: the tooling is dispatched from main and the source is only a commit.
    """
    packages.require(bool(re.fullmatch(r"[0-9a-f]{40}", source)), "Invalid candidate source SHA")
    branch = release_line(version)
    line_ref = next((name for name in (f"origin/{branch}", branch)
                     if git_run(root, "rev-parse", "--verify", "--quiet", name).returncode == 0), None)
    packages.require(line_ref is not None, f"Candidate source checkout is missing the {branch} line")
    packages.require(git_run(root, "merge-base", "--is-ancestor", source, line_ref).returncode == 0,
                     f"Candidate source is not on the {branch} line")


def require_candidate_source(root: Path, source: str, version: str) -> None:
    """Reject a pack of the immutable 3.10.0 pin SHA or a tree without the reviewed dependency pins."""
    packages.validate_mode(version, "candidate")
    packages.require(bool(re.fullmatch(r"[0-9a-f]{40}", source)), "Invalid candidate source SHA")
    if version == "3.10.0":
        return
    packages.require(source != ORIGINAL_3100_SOURCE, "Candidate must not reuse the 3.10.0 source commit")
    require_reviewed_pins(root)


def require_versioned_nupkgs(packages_dir: Path, version: str) -> list[Path]:
    packages.validate_mode(version, "candidate")
    packages.require(packages_dir.is_dir(), "PACKAGES_DIR must be a directory of packed .nupkg files")
    nupkgs = sorted(path for path in packages_dir.glob("*.nupkg") if path.is_file() and not path.is_symlink())
    packages.require(bool(nupkgs), f"{version} candidate produced no nupkg files")
    suffix = f".{version}.nupkg"
    wrong = [path.name for path in nupkgs if not path.name.endswith(suffix)]
    packages.require(not wrong, f"{version} candidate mixed versions: {', '.join(wrong)}")
    return nupkgs


def select_prepare_command(argv: list[str]) -> str | None:
    if argv and argv[0] in {"check-source", "check-nupkgs", "consume"}:
        return argv[0]
    if "--check-source" in argv:
        return "check-source"
    if "--consume" in argv:
        return "consume"
    return None


def parse_check_source_args(argv: list[str]) -> argparse.Namespace:
    tokens = [token for token in argv if token != "--check-source"]
    if tokens and tokens[0] == "check-source":
        tokens = tokens[1:]
    parser = argparse.ArgumentParser(description="Validate the candidate source SHA, its release line and the reviewed pins")
    parser.add_argument("--version", required=True)
    parser.add_argument("--source", required=True)
    parser.add_argument("--source-root", type=Path, default=Path(__file__).resolve().parents[2],
                        help="Checkout of the candidate source commit (default: this checkout)")
    return parser.parse_args(tokens)


def parse_check_nupkgs_args(argv: list[str]) -> argparse.Namespace:
    tokens = argv[1:] if argv and argv[0] == "check-nupkgs" else argv
    parser = argparse.ArgumentParser(description="Validate produced candidate nupkg versions")
    parser.add_argument("--packages-dir", required=True, type=Path)
    parser.add_argument("--version", required=True)
    return parser.parse_args(tokens)


def require_checkout_head(root: Path, source: str) -> None:
    """The checkout under --source-root must be exactly the commit being prepared."""
    head = git_run(root, "rev-parse", "HEAD")
    packages.require(head.returncode == 0 and head.stdout.strip() == source,
                     "Candidate source is not the HEAD of the source checkout")


def run_check_source(root: Path, source: str, version: str) -> None:
    require_candidate_source(root, source, version)
    require_checkout_head(root, source)
    if version != "3.10.0":
        require_release_line(root, source, version)


def seal(root: Path, source: str, version: str = "3.10.0") -> dict:
    packages.validate_mode(version, "candidate")
    packages.require(not (root / MANIFEST).exists(), "Refusing to reseal a candidate")
    files = []
    for path in sorted(root.rglob("*")):
        packages.require(not path.is_symlink(), "Candidate contains a symlink")
        if path.is_file():
            data = path.read_bytes()
            files.append({"path": path.relative_to(root).as_posix(), "size": len(data), "sha256": sha256(data)})
    manifest = {"schema": 1, "published": False, "version": version, "source_commit": source,
                "run_id": int(os.environ.get("GITHUB_RUN_ID", "0")),
                "run_attempt": int(os.environ.get("GITHUB_RUN_ATTEMPT", "0")), "files": files}
    packages.write_json(root / MANIFEST, manifest)
    return manifest


def validate_envelope(envelope: dict, metadata: dict, *, source: str, run_id: int, attempt: int,
                      version: str = "3.10.0", run_sha: str | None = None) -> None:
    """run_sha is the commit the producer run executed (main for a dispatch from main); it defaults to source."""
    expected_name = f"consolidated-candidate-{source}-{run_id}-{attempt}"
    packages.validate_mode(version, "candidate")
    packages.require(bool(re.fullmatch(r"[0-9a-f]{40}", source)), "Invalid candidate source SHA")
    workflow = metadata.get("workflow_run", {})
    packages.require(envelope.get("schema") == 1 and envelope.get("published") is False and
                     envelope.get("version") == version and envelope.get("source_commit") == source and
                     envelope.get("run_id") == run_id and envelope.get("run_attempt") == attempt,
                     "Candidate envelope source/run identity mismatch")
    if version != "3.10.0":
        packages.require(source != ORIGINAL_3100_SOURCE, "Candidate must not reuse the 3.10.0 source commit")
        packages.require(envelope.get("artifact_id") != ORIGINAL_3100_ARTIFACT,
                         "Candidate must not reuse the 3.10.0 artifact identity")
    packages.require(type(envelope.get("artifact_id")) is int and envelope["artifact_id"] > 0 and
                     envelope["artifact_id"] == metadata.get("id") and envelope.get("artifact_name") == expected_name and
                     metadata.get("name") == expected_name and workflow.get("id") == run_id and
                     workflow.get("head_sha") == (run_sha or source),
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
                       *, source: str, run_id: int, attempt: int, version: str = "3.10.0",
                       run_sha: str | None = None) -> dict:
    validate_envelope(envelope, metadata, source=source, run_id=run_id, attempt=attempt, version=version, run_sha=run_sha)
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
                         manifest.get("version") == version and manifest.get("source_commit") == source and
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


def main(argv: list[str] | None = None) -> None:
    argv = list(os.sys.argv[1:] if argv is None else argv)
    command = select_prepare_command(argv)
    if command == "check-source":
        args = parse_check_source_args(argv)
        run_check_source(args.source_root.resolve(strict=True), args.source, args.version)
        print(f"Source {args.source} is acceptable for {args.version}")
        return
    if command == "check-nupkgs":
        args = parse_check_nupkgs_args(argv)
        require_versioned_nupkgs(args.packages_dir, args.version)
        return
    if command != "consume":
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
    parser.add_argument("--version", default=None)
    parser.add_argument("--run-sha", default=None, help="Commit the producer run executed; defaults to --source")
    consume_argv = ["--consume", *argv[1:]] if argv and argv[0] == "consume" else argv
    args = parser.parse_args(consume_argv)
    for token in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN"):
        packages.require(not os.environ.get(token), "Artifact-reading token must not enter candidate consumers")
    envelope = json.loads(args.envelope.read_text())
    version = args.version or envelope.get("version")
    packages.validate_mode(version, "candidate")
    packages.require(envelope.get("version") == version, "Envelope version does not match consume --version")
    verify_and_extract(args.archive, envelope, json.loads(args.metadata.read_text()), args.output,
                       source=args.source, run_id=args.run_id, attempt=args.attempt, version=version,
                       run_sha=args.run_sha)
    manifest = json.loads((args.output / "verified-artifacts.json").read_text())
    packages.require(manifest["version"] == version and manifest["source_commit"] == args.source and manifest["published"] is False,
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
