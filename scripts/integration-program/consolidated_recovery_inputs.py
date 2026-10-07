"""Validate the original consolidated candidate for bounded recovery planning."""
from __future__ import annotations

import copy
from datetime import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import zipfile

import consolidated_candidate_input as candidate_input
import prepare_consolidated_release_candidate as candidate
import prove_consolidated_package_consumers as consumers
import prove_consolidated_packages as packages


TRANSPORT_FILES = {"candidate.zip", "artifact.json", "producer-run.json", "live-retrieval.json"}
MAX_TRANSPORT_JSON_BYTES = 1 * 1024 * 1024
MAX_ARCHIVE_BYTES = 128 * 1024 * 1024
MAX_TOTAL_UNCOMPRESSED_BYTES = 1024 * 1024 * 1024
MAX_ARCHIVE_MEMBERS = 10_000
MAX_PACKAGE_BYTES = 256 * 1024 * 1024
MAX_VERIFIED_MANIFEST_BYTES = 32 * 1024 * 1024
MAX_PREUPLOAD_MANIFEST_BYTES = 64 * 1024 * 1024
EXPECTED_PACKAGE_COUNT = 225
EXPECTED_EXCLUSION_COUNT = 124
EXPECTED_EXCLUSION_ID_COUNT = 123
PLANNER_SOURCE_PATTERN = re.compile(r"[0-9a-f]{40}\Z")
SHA256_PATTERN = re.compile(r"[0-9a-f]{64}\Z")
SHA512_PATTERN = re.compile(r"[0-9a-f]{128}\Z")


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def _absolute_without_symlink_ancestors(path: Path, label: str) -> Path:
    raw = Path(path)
    _require(".." not in raw.parts, f"{label} path must not contain parent traversal")
    if not raw.is_absolute():
        raw = Path.cwd() / raw
    absolute = Path(os.path.abspath(raw))
    for ancestor in (absolute, *absolute.parents):
        _require(not ancestor.is_symlink(), f"{label} has a symlink path component")
    return absolute


def _regular_file(path: Path, label: str) -> None:
    _require(path.is_file() and not path.is_symlink(), f"{label} must be a regular file")


def _read_bounded_json(path: Path, label: str, max_bytes: int = MAX_TRANSPORT_JSON_BYTES) -> tuple[dict, str]:
    _regular_file(path, label)
    _require(path.stat().st_size <= max_bytes, f"{label} exceeds the JSON size limit")
    with path.open("rb") as stream:
        data = stream.read(max_bytes + 1)
    _require(len(data) <= max_bytes, f"{label} exceeds the JSON size limit")
    def unique_object(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("duplicate JSON object key")
            result[key] = value
        return result

    try:
        value = json.loads(data.decode("utf-8"), object_pairs_hook=unique_object)
    except (UnicodeDecodeError, json.JSONDecodeError, RecursionError, ValueError) as error:
        raise ValueError(f"{label} is not valid UTF-8 JSON") from error
    _require(isinstance(value, dict), f"{label} must be a JSON object")
    return value, hashlib.sha256(data).hexdigest()


def _file_sha256(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def _preflight_outer_archive(archive_path: Path, expected_size: object) -> None:
    _regular_file(archive_path, "candidate archive")
    _require(type(expected_size) is int and expected_size > 0 and archive_path.stat().st_size == expected_size,
             "Candidate archive size differs from the pinned original envelope")
    _require(expected_size <= MAX_ARCHIVE_BYTES, "Candidate archive exceeds the compressed size limit")
    _require(_file_sha256(archive_path) == candidate_input.ARCHIVE_SHA256,
             "Candidate archive differs from the pinned original bytes")
    try:
        with zipfile.ZipFile(archive_path) as archive:
            members = archive.infolist()
            _require(len(members) <= MAX_ARCHIVE_MEMBERS, "Candidate archive contains too many members")
            names: set[str] = set()
            total_uncompressed = 0
            for member in members:
                folded = member.filename.casefold()
                _require(folded not in names, "Candidate archive contains duplicate member names")
                names.add(folded)
                if member.is_dir():
                    continue
                _require(member.file_size >= 0, "Candidate archive contains an invalid member size")
                total_uncompressed += member.file_size
                _require(total_uncompressed <= MAX_TOTAL_UNCOMPRESSED_BYTES,
                         "Candidate archive exceeds the total uncompressed size limit")
                if folded == candidate.MANIFEST.casefold():
                    _require(member.file_size <= MAX_PREUPLOAD_MANIFEST_BYTES,
                             "Pre-upload manifest exceeds the JSON size limit")
                if folded == "verified-artifacts.json":
                    _require(member.file_size <= MAX_VERIFIED_MANIFEST_BYTES,
                             "Verified artifact manifest exceeds the JSON size limit")
                if member.filename.casefold().endswith((".nupkg", ".snupkg")):
                    _require(member.file_size <= MAX_PACKAGE_BYTES,
                             "Candidate package archive exceeds the package size limit")
            _require(candidate.MANIFEST.casefold() in names and "verified-artifacts.json" in names,
                     "Candidate archive is missing a required manifest")
    except (OSError, zipfile.BadZipFile, RuntimeError) as error:
        raise ValueError("Candidate archive is not a readable ZIP") from error


def _preflight_leaf_archives(artifacts: Path) -> None:
    _require(artifacts.is_dir() and not artifacts.is_symlink(), "Extracted artifact directory is missing or unsafe")
    for path in artifacts.iterdir():
        if not path.name.casefold().endswith((".nupkg", ".snupkg")):
            continue
        _regular_file(path, "leaf package archive")
        _require(path.stat().st_size <= MAX_PACKAGE_BYTES, "Leaf package archive exceeds the package size limit")
        try:
            with zipfile.ZipFile(path) as archive:
                members = archive.infolist()
                _require(len(members) <= MAX_ARCHIVE_MEMBERS, "Leaf package archive contains too many members")
                total_uncompressed = 0
                for member in members:
                    if not member.is_dir():
                        total_uncompressed += member.file_size
                        _require(total_uncompressed <= MAX_PACKAGE_BYTES,
                                 "Leaf package archive exceeds the uncompressed size limit")
        except (OSError, zipfile.BadZipFile, RuntimeError) as error:
            raise ValueError(f"Leaf package archive is not a readable ZIP: {path.name}") from error


def _validate_planner_identity(planner_source: str, planner_run: int | None,
                               planner_attempt: int | None) -> dict:
    _require(isinstance(planner_source, str) and PLANNER_SOURCE_PATTERN.fullmatch(planner_source) is not None,
             "Planner source must be a lowercase 40-character Git SHA")
    _require((planner_run is None and planner_attempt is None) or
             (type(planner_run) is int and planner_run > 0 and
              type(planner_attempt) is int and planner_attempt > 0),
             "Planner run and attempt must be supplied together as positive integers")
    root = Path(__file__).resolve().parents[2]
    try:
        head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    except (OSError, subprocess.CalledProcessError) as error:
        raise ValueError("Cannot resolve planner checkout HEAD") from error
    _require(head == planner_source, "Planner source does not match the exact checkout HEAD")
    return {"source_commit": planner_source, "run_id": planner_run, "run_attempt": planner_attempt}


def _validate_inventory(manifest: dict) -> str:
    version = candidate_input.PRODUCER["version"]
    source = candidate_input.PRODUCER["source_commit"]
    _require(manifest.get("version") == version and manifest.get("source_commit") == source,
             "Verified package manifest has the wrong candidate version or source")
    _require(manifest.get("published") is False and manifest.get("mode") == "candidate" and
             manifest.get("configuration") == "Release",
             "Verified package manifest is not an unpublished Release candidate")
    _require(manifest.get("repository_url") == packages.CORE_URL and
             isinstance(manifest.get("browser_assets"), list) and
             isinstance(manifest.get("build_inputs"), dict) and
             isinstance(manifest.get("icon_sha256"), str) and
             SHA256_PATTERN.fullmatch(manifest["icon_sha256"]) is not None,
             "Verified package manifest is missing candidate build identity")

    # Reuse the consumer-side schema checks before inspecting package rows.
    consumers._validated_manifest(manifest)
    rows = manifest.get("packages")
    exclusions = manifest.get("exclusions")
    _require(isinstance(rows, list) and len(rows) == EXPECTED_PACKAGE_COUNT,
             "Verified package manifest package count changed")
    _require(isinstance(exclusions, list) and len(exclusions) == EXPECTED_EXCLUSION_COUNT,
             "Verified package manifest exclusion count changed")

    package_ids: set[str] = set()
    package_projects: set[str] = set()
    for row in rows:
        _require(isinstance(row, dict), "Verified package row is malformed")
        package_id = row.get("id")
        project = row.get("project")
        _require(isinstance(package_id, str) and package_id.strip() and
                 isinstance(project, str) and project.strip(), "Verified package row lacks identity")
        folded_id, folded_project = package_id.casefold(), project.casefold()
        _require(folded_id not in package_ids, "Verified package IDs are duplicated")
        _require(folded_project not in package_projects, "Verified package project paths are duplicated")
        package_ids.add(folded_id)
        package_projects.add(folded_project)
        expected_nupkg = f"{package_id}.{version}.nupkg"
        expected_snupkg = f"{package_id}.{version}.snupkg"
        _require(row.get("nupkg") == expected_nupkg and row.get("snupkg") == expected_snupkg,
                 f"Verified package pair is incomplete or misnamed: {package_id}")

    exclusion_projects: set[str] = set()
    exclusion_ids: set[str] = set()
    for row in exclusions:
        _require(isinstance(row, dict), "Verified exclusion row is malformed")
        package_id, project, reason = row.get("id"), row.get("project"), row.get("reason")
        _require(isinstance(package_id, str) and package_id.strip() and
                 isinstance(project, str) and project.strip() and
                 isinstance(reason, str) and reason.strip(), "Verified exclusion row lacks project identity or reason")
        folded_project = project.casefold()
        _require(folded_project not in exclusion_projects, "Verified exclusion project paths are duplicated")
        _require(folded_project not in package_projects, "A project is both included and excluded")
        exclusion_projects.add(folded_project)
        exclusion_ids.add(package_id.casefold())
    _require(len(exclusion_ids) == EXPECTED_EXCLUSION_ID_COUNT,
             "Verified exclusion package ID count changed")

    baseline_path = Path(__file__).with_name("consolidated-candidate-inventory-baseline.json")
    try:
        baseline = json.loads(baseline_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise ValueError("Candidate inventory baseline is unavailable or invalid") from error
    actual_identity = candidate.inventory_identity(manifest)
    expected_identity = candidate.inventory_identity(baseline)
    _require(actual_identity == expected_identity, "Verified package and exclusion rows differ from the accepted inventory")

    evaluations = manifest.get("evaluations")
    _require(isinstance(evaluations, list) and len(evaluations) == EXPECTED_PACKAGE_COUNT + EXPECTED_EXCLUSION_COUNT,
             "Verified manifest does not retain the complete evaluated project set")
    evaluation_projects: list[str] = []
    for row in evaluations:
        _require(isinstance(row, dict) and isinstance(row.get("project"), str),
                 "Verified evaluated project record is malformed")
        evaluation_projects.append(row["project"].casefold())
    _require(len(set(evaluation_projects)) == len(evaluation_projects) and
             set(evaluation_projects) == package_projects | exclusion_projects,
             "Verified evaluated projects do not match package and exclusion inventory")
    return hashlib.sha256(json.dumps(actual_identity, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()


def _compare_retained_archive_hashes(retained: dict, checked: dict) -> None:
    retained_rows = {row["id"].casefold(): row for row in retained["packages"]}
    checked_rows = {row["id"].casefold(): row for row in checked["packages"]}
    _require(set(retained_rows) == set(checked_rows), "Artifact verifier changed the package inventory")
    for key, row in checked_rows.items():
        original = retained_rows[key]
        for field in ("nupkg_sha256", "nupkg_sha512", "snupkg_sha256"):
            expected = original.get(field)
            actual = row.get(field)
            pattern = SHA512_PATTERN if field == "nupkg_sha512" else SHA256_PATTERN
            _require(isinstance(expected, str) and pattern.fullmatch(expected) is not None,
                     f"Retained package manifest is missing a valid {field}")
            _require(actual == expected, f"Package archive bytes differ from retained {field}: {row['id']}")


def _remove_rejected_destination(destination: Path) -> None:
    if os.path.lexists(destination) and destination.is_dir() and not destination.is_symlink():
        shutil.rmtree(destination)


def prepare_recovery_inputs(inputs: Path, destination: Path, *, planner_source: str,
                            planner_run: int | None = None,
                            planner_attempt: int | None = None) -> tuple[Path, dict]:
    """Verify the pinned original archive and return its extracted evidence root."""
    planner = _validate_planner_identity(planner_source, planner_run, planner_attempt)
    inputs = _absolute_without_symlink_ancestors(Path(inputs), "Inputs")
    destination = _absolute_without_symlink_ancestors(Path(destination), "Destination")
    _require(inputs.is_dir() and not inputs.is_symlink(), "Inputs must be a regular transport directory")
    _require({path.name for path in inputs.iterdir()} == TRANSPORT_FILES,
             "Transport must contain exactly the four candidate input files")
    input_paths = {name: inputs / name for name in TRANSPORT_FILES}
    for name, path in input_paths.items():
        _regular_file(path, f"Transport input {name}")
    _require(not os.path.lexists(destination), "Destination must not already exist")
    _require(destination != inputs and destination not in inputs.parents and inputs not in destination.parents,
             "Destination must not overlap the input directory")

    json_inputs: dict[str, dict] = {}
    json_hashes: dict[str, str] = {}
    for name in ("artifact.json", "producer-run.json", "live-retrieval.json"):
        json_inputs[name], json_hashes[name] = _read_bounded_json(input_paths[name], name)
    original_envelope, original_envelope_sha256 = _read_bounded_json(
        candidate_input.ORIGINAL_ENVELOPE, "Original accepted envelope")
    _require(original_envelope_sha256 == candidate_input.ENVELOPE_SHA256 and
             _file_sha256(candidate_input.ORIGINAL_ENVELOPE) == candidate_input.ENVELOPE_SHA256,
             "Original accepted envelope bytes changed")
    _require(original_envelope.get("archive_sha256") == candidate_input.ARCHIVE_SHA256 and
             original_envelope.get("preupload_manifest_sha256") == candidate_input.MANIFEST_SHA256,
             "Original accepted archive identity changed")
    _preflight_outer_archive(input_paths["candidate.zip"], original_envelope.get("archive_size"))

    try:
        generic_provenance = candidate_input.verify_candidate_inputs(
            input_paths["candidate.zip"], input_paths["artifact.json"],
            input_paths["producer-run.json"], input_paths["live-retrieval.json"], destination,
            producer=candidate_input.PRODUCER,
            original_envelope=candidate_input.ORIGINAL_ENVELOPE,
            envelope_sha256=candidate_input.ENVELOPE_SHA256,
            archive_sha256=candidate_input.ARCHIVE_SHA256,
            manifest_sha256=candidate_input.MANIFEST_SHA256,
        )
        for name in ("artifact.json", "producer-run.json", "live-retrieval.json"):
            _, current_hash = _read_bounded_json(input_paths[name], name)
            _require(current_hash == json_hashes[name], f"Transport input changed during verification: {name}")
        _require(_file_sha256(input_paths["candidate.zip"]) == candidate_input.ARCHIVE_SHA256,
                 "Candidate archive changed during verification")
        _require(input_paths["candidate.zip"].stat().st_size == original_envelope.get("archive_size"),
                 "Candidate archive size differs from the original envelope")
        _require(_file_sha256(candidate_input.ORIGINAL_ENVELOPE) == candidate_input.ENVELOPE_SHA256,
                 "Original accepted envelope changed during verification")

        manifest_path = destination / "verified-artifacts.json"
        manifest, manifest_sha256 = _read_bounded_json(
            manifest_path, "Verified artifact manifest", MAX_VERIFIED_MANIFEST_BYTES)
        inventory_sha256 = _validate_inventory(manifest)

        _preflight_leaf_archives(destination / "artifacts")
        checked_manifest = packages.verify_artifacts(destination / "artifacts", copy.deepcopy(manifest))
        _compare_retained_archive_hashes(manifest, checked_manifest)
        _require(_file_sha256(manifest_path) == manifest_sha256,
                 "Verified artifact manifest changed during package verification")

        retrieval = json_inputs["live-retrieval.json"]
        _require(isinstance(retrieval.get("retrieved_at"), str), "Live retrieval timestamp is missing")
        observed = datetime.fromisoformat(retrieval["retrieved_at"].replace("Z", "+00:00"))
        _require(observed.tzinfo is not None, "Live retrieval timestamp must include a timezone")
        provenance = {
            "candidate_producer": dict(candidate_input.PRODUCER),
            "artifact_name": original_envelope["artifact_name"],
            "archive_sha256": original_envelope["archive_sha256"],
            "archive_size": original_envelope["archive_size"],
            "original_envelope_sha256": generic_provenance["original_envelope_sha256"],
            "preupload_manifest_sha256": generic_provenance["preupload_manifest_sha256"],
            "verified_artifacts_sha256": manifest_sha256,
            "inventory_sha256": inventory_sha256,
            "live_retrieval_sha256": generic_provenance["live_retrieval_sha256"],
            "observed_at": retrieval["retrieved_at"],
            "planner": planner,
        }
        return destination, provenance
    except BaseException:
        try:
            _remove_rejected_destination(destination)
        except OSError as cleanup_error:
            raise RuntimeError("Rejected recovery evidence could not be removed") from cleanup_error
        raise
