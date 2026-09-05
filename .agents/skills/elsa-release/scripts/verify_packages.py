#!/usr/bin/env python3
"""Verify release package artifacts and their published provenance.

The verifier deliberately uses only the Python standard library.  It validates
the package files first, then downloads the exact package from every configured
NuGet feed and queries npm's JSON API for every configured npm package.  A
report is written even when validation fails so a delayed feed or malformed
artifact leaves useful evidence for the next attempt.
"""

from __future__ import annotations

import argparse
import base64
import hashlib
import http.client
import io
import json
import posixpath
import sys
import tarfile
import urllib.error
import urllib.parse
import urllib.request
import zipfile
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path
from typing import Any, Iterable


DEFAULT_TIMEOUT = 25.0
DEFAULT_MAX_BYTES = 256 * 1024 * 1024
CHUNK_SIZE = 64 * 1024
MAX_ARCHIVE_MEMBERS = 10_000
MAX_JSON_BYTES = 16 * 1024 * 1024


class VerificationError(Exception):
    """A bounded, user-facing verification failure."""


def _local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def _is_prerelease(version: str) -> bool:
    return "-" in version


def _version_for(entry: dict[str, Any], default: str) -> str:
    version = entry.get("version", default)
    if not isinstance(version, str) or not version.strip():
        raise VerificationError("package version must be a non-empty string")
    return version.strip()


def _safe_archive_name(name: str) -> bool:
    """Reject archive paths that could escape if a caller later extracts them."""

    normalized = posixpath.normpath(name.replace("\\", "/"))
    return not (normalized == ".." or normalized.startswith("../") or name.startswith("/"))


def _read_limited(stream: Any, max_bytes: int) -> bytes:
    content = bytearray()
    while True:
        chunk = stream.read(min(CHUNK_SIZE, max_bytes + 1 - len(content)))
        if not chunk:
            return bytes(content)
        content.extend(chunk)
        if len(content) > max_bytes:
            raise VerificationError(f"response exceeds max bytes ({max_bytes})")


def _json_bytes(data: bytes, description: str) -> dict[str, Any]:
    if len(data) > MAX_JSON_BYTES:
        raise VerificationError(f"{description} exceeds JSON size limit")
    try:
        value = json.loads(data.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise VerificationError(f"{description} is not valid JSON: {exc}") from exc
    if not isinstance(value, dict):
        raise VerificationError(f"{description} must be a JSON object")
    return value


def _archive_payload_hash(data: bytes, max_bytes: int) -> str:
    """Hash uncompressed package payload, excluding NuGet's signing entry."""

    digest = hashlib.sha256()
    total = 0
    try:
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            infos = archive.infolist()
            if len(infos) > MAX_ARCHIVE_MEMBERS:
                raise VerificationError("nupkg contains too many archive members")
            members: list[tuple[str, bytes]] = []
            for info in infos:
                if not _safe_archive_name(info.filename):
                    raise VerificationError(f"unsafe nupkg member path: {info.filename}")
                if info.is_dir() or info.filename.lower().endswith(".signature.p7s"):
                    continue
                if info.file_size > max_bytes or total + info.file_size > max_bytes:
                    raise VerificationError("nupkg uncompressed payload exceeds max bytes")
                payload = archive.read(info)
                total += len(payload)
                members.append((info.filename, payload))
    except (zipfile.BadZipFile, OSError, RuntimeError) as exc:
        raise VerificationError(f"invalid nupkg zip: {exc}") from exc

    for name, payload in sorted(members, key=lambda pair: pair[0]):
        # Include names and lengths so concatenated files cannot collide.
        encoded_name = name.encode("utf-8")
        digest.update(len(encoded_name).to_bytes(8, "big"))
        digest.update(encoded_name)
        digest.update(len(payload).to_bytes(8, "big"))
        digest.update(payload)
    return digest.hexdigest()


def _embedded_content_errors(data: bytes, max_bytes: int, expectations: dict[str, Any] | None, package_id: str) -> list[str]:
    """Validate embedded template projects without extracting an untrusted archive."""

    if not expectations or package_id.lower() != str(expectations.get("package_id", "")).lower():
        return []
    expected_files = expectations.get("files", [])
    archive_prefix = str(expectations.get("archive_prefix", ""))
    expected_version = str(expectations.get("expected_version", ""))
    known_ids = {str(value).lower() for value in expectations.get("known_published_ids", [])}
    errors: list[str] = []
    try:
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            infos = archive.infolist()
            if len(infos) > MAX_ARCHIVE_MEMBERS:
                raise VerificationError("nupkg contains too many archive members")
            total_bytes = 0
            for info in infos:
                if not _safe_archive_name(info.filename):
                    raise VerificationError(f"unsafe nupkg member path: {info.filename}")
                if info.file_size > max_bytes:
                    raise VerificationError("embedded template exceeds max bytes")
                total_bytes += info.file_size
                if total_bytes > max_bytes:
                    raise VerificationError("nupkg uncompressed payload exceeds max bytes")
            names = [info.filename for info in infos if not info.is_dir()]
            duplicate_names = sorted(name for name, count in Counter(names).items() if count > 1)
            if duplicate_names:
                errors.extend(f"duplicate nupkg member: {name}" for name in duplicate_names)
            name_set = set(names)
            template_projects = {
                name[len(archive_prefix):] if archive_prefix and name.startswith(archive_prefix) else name
                for name in name_set
                if name.lower().endswith(".csproj") and (not archive_prefix or name.startswith(archive_prefix))
            }
            expected_paths = {str(item.get("path", "")) for item in expected_files}
            for missing in sorted(expected_paths - template_projects):
                errors.append(f"embedded template project missing from nupkg: {missing}")
            for extra in sorted(template_projects - expected_paths):
                errors.append(f"unexpected embedded template project in nupkg: {extra}")
            expected_configs = {str(value) for value in expectations.get("template_configs", [])}
            if expected_configs:
                template_configs = {
                    name[len(archive_prefix):] if archive_prefix and name.startswith(archive_prefix) else name
                    for name in names
                    if name.lower().endswith("/.template.config/template.json")
                    and (not archive_prefix or name.startswith(archive_prefix))
                }
                for missing in sorted(expected_configs - template_configs):
                    errors.append(f"template configuration missing from nupkg: {missing}")
                for extra in sorted(template_configs - expected_configs):
                    errors.append(f"unexpected template configuration in nupkg: {extra}")
            for expected in expected_files:
                relative = str(expected.get("path", ""))
                candidates = [
                    name for name in names
                    if name == archive_prefix + relative
                ]
                if len(candidates) != 1:
                    errors.append(f"embedded template project missing or duplicated in nupkg: {relative}")
                    continue
                info = archive.getinfo(candidates[0])
                try:
                    document = ET.fromstring(archive.read(info))
                except ET.ParseError as exc:
                    errors.append(f"embedded template project is invalid XML: {relative}: {exc}")
                    continue
                observed = []
                for node in document.iter():
                    if _local_name(node.tag) != "PackageReference":
                        continue
                    package = (node.attrib.get("Include") or "").strip()
                    if not any(package.lower().startswith(str(prefix).lower()) for prefix in expectations.get("package_prefixes", ["Elsa"])):
                        continue
                    version = (node.attrib.get("Version") or "").strip()
                    observed.append({"id": package, "version": version})
                    if package.lower() not in known_ids:
                        errors.append(f"{relative}: embedded package {package} is not a published upstream package")
                    if expected_version and version != expected_version:
                        errors.append(f"{relative}: embedded package {package} version {version!r} != {expected_version!r}")
                if sorted(observed, key=lambda item: (item["id"].lower(), item["version"])) != sorted(
                    expected.get("references", []), key=lambda item: (item["id"].lower(), item["version"])
                ):
                    errors.append(f"{relative}: embedded Elsa PackageReferences differ from the bound source")
    except (zipfile.BadZipFile, OSError, RuntimeError) as exc:
        raise VerificationError(f"invalid nupkg zip: {exc}") from exc
    return errors


def _parse_nuspec(data: bytes, max_bytes: int, content_expectations: dict[str, Any] | None = None) -> dict[str, Any]:
    try:
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            infos = archive.infolist()
            if len(infos) > MAX_ARCHIVE_MEMBERS:
                raise VerificationError("nupkg contains too many archive members")
            nuspec_infos = [
                info
                for info in infos
                if not info.is_dir() and info.filename.lower().endswith(".nuspec")
            ]
            if len(nuspec_infos) != 1:
                raise VerificationError("nupkg must contain exactly one nuspec")
            info = nuspec_infos[0]
            if not _safe_archive_name(info.filename) or info.file_size > max_bytes:
                raise VerificationError("nuspec is unsafe or exceeds max bytes")
            nuspec = archive.read(info)
    except (zipfile.BadZipFile, OSError, RuntimeError) as exc:
        raise VerificationError(f"invalid nupkg zip: {exc}") from exc

    try:
        root = ET.fromstring(nuspec)
    except ET.ParseError as exc:
        raise VerificationError(f"invalid nuspec XML: {exc}") from exc

    metadata = next((node for node in root.iter() if _local_name(node.tag) == "metadata"), None)
    if metadata is None:
        raise VerificationError("nuspec has no metadata element")

    values: dict[str, Any] = {
        "id": None,
        "version": None,
        "repository_commit": None,
        "dependencies": [],
        "nuspec": info.filename,
    }
    for child in metadata:
        name = _local_name(child.tag)
        if name in {"id", "version"}:
            values[name] = (child.text or "").strip()
        elif name == "repository":
            values["repository_commit"] = (child.attrib.get("commit") or "").strip() or None

    for dependency in metadata.iter():
        if _local_name(dependency.tag) != "dependency":
            continue
        dep_id = (dependency.attrib.get("id") or "").strip()
        dep_version = (dependency.attrib.get("version") or "").strip()
        if dep_id:
            values["dependencies"].append({"id": dep_id, "version": dep_version})

    if not values["id"] or not values["version"]:
        raise VerificationError("nuspec metadata must contain id and version")
    values["content_errors"] = _embedded_content_errors(data, max_bytes, content_expectations, values["id"])
    values["payload_sha256"] = _archive_payload_hash(data, max_bytes)
    return values


def _parse_nuget_artifact(path: Path, max_bytes: int, content_expectations: dict[str, Any] | None = None) -> dict[str, Any]:
    try:
        size = path.stat().st_size
    except OSError as exc:
        raise VerificationError(f"cannot stat artifact: {exc}") from exc
    if size > max_bytes:
        raise VerificationError(f"artifact exceeds max bytes ({max_bytes})")
    try:
        data = path.read_bytes()
    except OSError as exc:
        raise VerificationError(f"cannot read artifact: {exc}") from exc
    parsed = _parse_nuspec(data, max_bytes, content_expectations)
    parsed.update({"path": str(path), "file": path.name})
    return parsed


def _package_json_from_tgz(data: bytes, max_bytes: int) -> dict[str, Any]:
    if len(data) > max_bytes:
        raise VerificationError(f"npm tarball exceeds max bytes ({max_bytes})")
    try:
        with tarfile.open(fileobj=io.BytesIO(data), mode="r:*") as archive:
            members = archive.getmembers()
            if len(members) > MAX_ARCHIVE_MEMBERS:
                raise VerificationError("npm tarball contains too many archive members")
            total = 0
            for member in members:
                if not _safe_archive_name(member.name):
                    raise VerificationError(f"unsafe npm tarball member path: {member.name}")
                if member.size < 0 or member.size > max_bytes or total + member.size > max_bytes:
                    raise VerificationError("npm tarball uncompressed payload exceeds max bytes")
                total += member.size
            package_member = next(
                (member for member in members if member.name == "package/package.json"), None
            )
            if package_member is None or not package_member.isfile():
                raise VerificationError("npm tarball has no regular package/package.json")
            if not _safe_archive_name(package_member.name) or package_member.size > max_bytes:
                raise VerificationError("npm package metadata is unsafe or exceeds max bytes")
            extracted = archive.extractfile(package_member)
            if extracted is None:
                raise VerificationError("cannot read npm package metadata")
            return _json_bytes(_read_limited(extracted, max_bytes), "npm package metadata")
    except (tarfile.TarError, OSError, KeyError, ValueError) as exc:
        raise VerificationError(f"invalid npm tarball: {exc}") from exc


def _parse_npm_artifact(path: Path, max_bytes: int) -> dict[str, Any]:
    try:
        size = path.stat().st_size
        if size > max_bytes:
            raise VerificationError(f"artifact exceeds max bytes ({max_bytes})")
        data = path.read_bytes()
    except OSError as exc:
        raise VerificationError(f"cannot read artifact: {exc}") from exc
    package = _package_json_from_tgz(data, max_bytes)
    return {
        "path": str(path),
        "file": path.name,
        "name": package.get("name"),
        "version": package.get("version"),
        "package": package,
        "sha512_integrity": "sha512-"
        + base64.b64encode(hashlib.sha512(data).digest()).decode("ascii"),
    }


def _request(
    url: str,
    *,
    method: str,
    timeout: float,
    max_bytes: int,
    accept: str = "*/*",
) -> tuple[int, dict[str, str], bytes]:
    request = urllib.request.Request(
        url,
        method=method,
        headers={"Accept": accept, "User-Agent": "elsa-release-package-verifier/1"},
    )
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            headers = {key.lower(): value for key, value in response.headers.items()}
            if method == "HEAD":
                return response.status, headers, b""
            length = headers.get("content-length")
            if length:
                try:
                    if int(length) > max_bytes:
                        raise VerificationError(f"response exceeds max bytes ({max_bytes})")
                except ValueError:
                    pass
            return response.status, headers, _read_limited(response, max_bytes)
    except urllib.error.HTTPError:
        raise
    except (urllib.error.URLError, TimeoutError, OSError, http.client.HTTPException) as exc:
        raise VerificationError(f"request failed: {type(exc).__name__}: {exc}") from exc


def _http_error_record(exc: BaseException) -> dict[str, Any]:
    if isinstance(exc, urllib.error.HTTPError):
        exc.close()
    return {
        "error": type(exc).__name__,
        "status": getattr(exc, "code", None),
        "message": str(exc),
    }


def _download_nupkg(url: str, timeout: float, max_bytes: int, content_expectations: dict[str, Any] | None = None) -> tuple[dict[str, Any], bytes]:
    """Download a published nupkg, using HEAD only as a bounded preflight."""

    try:
        status, _, _ = _request(
            url, method="HEAD", timeout=timeout, max_bytes=max_bytes, accept="application/octet-stream"
        )
        if status < 200 or status >= 400:
            raise VerificationError(f"unexpected HEAD status {status}")
    except urllib.error.HTTPError as exc:
        if exc.code not in {405, 501}:
            raise
        exc.close()
        # Feedz and a number of private NuGet feeds reject HEAD.  A complete
        # GET is the authoritative availability and provenance check.
    status, headers, data = _request(
        url, method="GET", timeout=timeout, max_bytes=max_bytes, accept="application/octet-stream"
    )
    if status < 200 or status >= 300:
        raise VerificationError(f"unexpected GET status {status}")
    return _parse_nuspec(data, max_bytes, content_expectations), data


def _download_json(url: str, timeout: float, max_bytes: int) -> dict[str, Any]:
    status, _, data = _request(url, method="GET", timeout=timeout, max_bytes=max_bytes, accept="application/json")
    if status < 200 or status >= 300:
        raise VerificationError(f"unexpected JSON status {status}")
    return _json_bytes(data, url)


def _download_bytes(url: str, timeout: float, max_bytes: int) -> bytes:
    status, _, data = _request(
        url, method="GET", timeout=timeout, max_bytes=max_bytes, accept="application/octet-stream"
    )
    if status < 200 or status >= 300:
        raise VerificationError(f"unexpected tarball status {status}")
    return data


def _nuget_url(base_url: str, package_id: str, version: str) -> str:
    base = base_url.rstrip("/")
    package = urllib.parse.quote(package_id.lower(), safe="")
    encoded_version = urllib.parse.quote(version.lower(), safe="")
    filename = urllib.parse.quote(f"{package_id.lower()}.{version.lower()}.nupkg", safe="")
    return f"{base}/{package}/{encoded_version}/{filename}"


def _npm_url(registry: str, name: str, suffix: str = "") -> str:
    # npm's JSON API uses an encoded scoped package path.  quote leaves no
    # slash in the package name, which also prevents path confusion.
    package = urllib.parse.quote(name, safe="")
    base = registry.rstrip("/")
    return f"{base}/{package}{suffix}"


def _validate_manifest(raw: Any) -> dict[str, Any]:
    if not isinstance(raw, dict):
        raise VerificationError("manifest must be a JSON object")
    version = raw.get("version")
    source_commit = raw.get("source_commit")
    if not isinstance(version, str) or not version.strip():
        raise VerificationError("manifest version must be a non-empty string")
    if not isinstance(source_commit, str) or not source_commit.strip():
        raise VerificationError("manifest source_commit must be a non-empty string")
    result = dict(raw)
    result["version"] = version.strip()
    result["source_commit"] = source_commit.strip()
    for field in ("nuget", "feeds", "npm"):
        value = raw.get(field, [])
        if not isinstance(value, list):
            raise VerificationError(f"manifest {field} must be an array")
        result[field] = value
    if not result["nuget"] and not result["npm"]:
        raise VerificationError("manifest must contain at least one expected package")

    expected_dependencies = raw.get("expected_dependencies", {})
    if not isinstance(expected_dependencies, dict):
        raise VerificationError("manifest expected_dependencies must be an object")
    for dependency_id, dependency_version in expected_dependencies.items():
        if (
            not isinstance(dependency_id, str)
            or not dependency_id.strip()
            or not isinstance(dependency_version, str)
            or not dependency_version.strip()
        ):
            raise VerificationError("manifest expected_dependencies values must be non-empty strings")
    result["expected_dependencies"] = expected_dependencies

    content = raw.get("content_expectations")
    if content is not None:
        if not isinstance(content, dict):
            raise VerificationError("manifest content_expectations must be an object")
        for field in ("package_id", "expected_version", "archive_prefix"):
            if not isinstance(content.get(field), str) or not content[field].strip():
                raise VerificationError(f"manifest content_expectations requires {field}")
        if content["expected_version"] != result["version"]:
            raise VerificationError("manifest content_expectations version must match the release version")
        if not any(
            str(item.get("id", "")).lower() == content["package_id"].lower()
            for item in raw.get("nuget", []) if isinstance(item, dict)
        ):
            raise VerificationError("manifest content_expectations package_id is not an expected NuGet package")
        for field in ("known_published_ids", "files", "template_configs"):
            if not isinstance(content.get(field), list):
                raise VerificationError(f"manifest content_expectations {field} must be an array")
            if not content[field]:
                raise VerificationError(f"manifest content_expectations {field} must not be empty")
        for config_path in content["template_configs"]:
            if not isinstance(config_path, str) or not config_path.strip():
                raise VerificationError("manifest content_expectations template configs require paths")
        for package_id in content["known_published_ids"]:
            if not isinstance(package_id, str) or not package_id.strip():
                raise VerificationError("manifest content_expectations known IDs must be non-empty strings")
        for file in content["files"]:
            if not isinstance(file, dict) or not isinstance(file.get("path"), str) or not file["path"].strip():
                raise VerificationError("manifest content_expectations files require paths")
            if not isinstance(file.get("references"), list):
                raise VerificationError("manifest content_expectations file references must be arrays")
            for reference in file["references"]:
                if (
                    not isinstance(reference, dict)
                    or not isinstance(reference.get("id"), str)
                    or not isinstance(reference.get("version"), str)
                    or not reference["id"].strip()
                    or not reference["version"].strip()
                ):
                    raise VerificationError("manifest embedded references require id and version")
        result["content_expectations"] = content

    for item in result["nuget"]:
        if not isinstance(item, dict) or not isinstance(item.get("id"), str) or not item["id"].strip():
            raise VerificationError("each nuget entry requires a non-empty id")
        item["id"] = item["id"].strip()
        has_explicit_version = "version" in item
        item["version"] = _version_for(item, result["version"])
        verify_published = item.get("verify_published", True)
        if not isinstance(verify_published, bool):
            raise VerificationError(f"nuget {item['id']} verify_published must be boolean")
        item["verify_published"] = verify_published
        if not verify_published:
            reason = item.get("reason")
            if not has_explicit_version:
                raise VerificationError(
                    f"nuget {item['id']} publication skips require an explicit fixed version"
                )
            if not isinstance(reason, str) or not reason.strip():
                raise VerificationError(f"nuget {item['id']} publication skips require a reason")
            if item["version"] == result["version"]:
                raise VerificationError(
                    f"release-version nuget {item['id']} must have verify_published=true"
                )
            item["reason"] = reason.strip()

    for item in result["feeds"]:
        if (
            not isinstance(item, dict)
            or not isinstance(item.get("name"), str)
            or not item["name"].strip()
            or not isinstance(item.get("base_url"), str)
            or not item["base_url"].strip()
        ):
            raise VerificationError("each feed requires non-empty name and base_url")
        item["name"] = item["name"].strip()
        item["base_url"] = item["base_url"].strip()

    for item in result["npm"]:
        if not isinstance(item, dict) or not isinstance(item.get("name"), str) or not item["name"].strip():
            raise VerificationError("each npm entry requires a non-empty name")
        if not isinstance(item.get("dist_tag"), str) or not item["dist_tag"].strip():
            raise VerificationError(f"npm {item['name']} requires a non-empty dist_tag")
        item["name"] = item["name"].strip()
        item["dist_tag"] = item["dist_tag"].strip()
        item["version"] = _version_for(item, result["version"])
        if "registry" in item and (
            not isinstance(item["registry"], str) or not item["registry"].strip()
        ):
            raise VerificationError(f"npm {item['name']} registry must be a non-empty string")
        item["registry"] = item.get("registry", "https://registry.npmjs.org").strip()

    for field, key in (("nuget", "id"), ("npm", "name")):
        names = [item[key].lower() for item in result[field]]
        duplicate_names = sorted(name for name, count in Counter(names).items() if count > 1)
        if duplicate_names:
            raise VerificationError(f"manifest {field} has duplicate entries: {duplicate_names}")
    for name in result["feeds"]:
        if sum(1 for item in result["feeds"] if item["name"].lower() == name["name"].lower()) > 1:
            raise VerificationError(f"manifest feeds has duplicate name: {name['name']}")
    return result


def _check_nuget_metadata(
    metadata: dict[str, Any],
    expected_id: str,
    expected_version: str,
    source_commit: str,
    manifest: dict[str, Any],
    *,
    artifact_label: str,
) -> list[str]:
    errors: list[str] = []
    errors.extend(str(error) for error in metadata.get("content_errors", []))
    if str(metadata.get("id", "")).lower() != expected_id.lower():
        errors.append(f"{artifact_label}: nuspec id {metadata.get('id')!r} != {expected_id!r}")
    if metadata.get("version") != expected_version:
        errors.append(
            f"{artifact_label}: nuspec version {metadata.get('version')!r} != {expected_version!r}"
        )
    commit = metadata.get("repository_commit")
    if commit is None or commit.lower() != source_commit.lower():
        errors.append(f"{artifact_label}: repository commit {commit!r} != {source_commit!r}")

    monitored: dict[str, str] = {
        str(item["id"]).lower(): str(item["version"])
        for item in manifest["nuget"]
    }
    monitored.update({str(key).lower(): str(value) for key, value in manifest["expected_dependencies"].items()})
    stable = not _is_prerelease(manifest["version"])
    for dependency in metadata.get("dependencies", []):
        dep_id = str(dependency.get("id", ""))
        dep_version = str(dependency.get("version", ""))
        normalized_dep_id = dep_id.lower()
        is_internal_elsa = normalized_dep_id == "elsa" or normalized_dep_id.startswith("elsa.")
        if not is_internal_elsa and normalized_dep_id not in {
            str(key).lower() for key in manifest["expected_dependencies"]
        }:
            continue
        expected_dependency = monitored.get(normalized_dep_id)
        if expected_dependency is None:
            continue
        if stable and _is_prerelease(dep_version):
            errors.append(f"{artifact_label}: stable dependency {dep_id} is prerelease {dep_version}")
        if expected_dependency and dep_version != expected_dependency:
            errors.append(
                f"{artifact_label}: dependency {dep_id} version {dep_version!r} != {expected_dependency!r}"
            )
    return errors


def _discover_artifacts(artifacts_dir: Path, suffix: str) -> list[Path]:
    if not artifacts_dir.is_dir():
        raise VerificationError(f"artifacts directory does not exist: {artifacts_dir}")
    return sorted(path for path in artifacts_dir.rglob(f"*{suffix}") if path.is_file())


def _verify_nuget(
    manifest: dict[str, Any], artifacts_dir: Path, timeout: float, max_bytes: int
) -> tuple[dict[str, Any], list[str]]:
    entries = {item["id"].lower(): item for item in manifest["nuget"]}
    result: dict[str, Any] = {
        "expected": [item["id"] for item in manifest["nuget"]],
        "artifacts": [],
        "missing": [],
        "extra": [],
        "duplicates": [],
        "feeds": [],
    }
    errors: list[str] = []
    artifacts_by_id: dict[str, list[dict[str, Any]]] = {}
    for path in _discover_artifacts(artifacts_dir, ".nupkg"):
        try:
            artifact = _parse_nuget_artifact(path, max_bytes, manifest.get("content_expectations"))
            artifact_errors = _check_nuget_metadata(
                artifact,
                str(artifact.get("id")),
                str(artifact.get("version")),
                manifest["source_commit"],
                manifest,
                artifact_label=str(path),
            )
            artifact["errors"] = artifact_errors
            errors.extend(artifact_errors)
            key = str(artifact.get("id", "")).lower()
            artifacts_by_id.setdefault(key, []).append(artifact)
        except VerificationError as exc:
            record = {"path": str(path), "file": path.name, "errors": [str(exc)]}
            result["artifacts"].append(record)
            errors.append(f"{path}: {exc}")
    for key, records in artifacts_by_id.items():
        result["artifacts"].extend(records)
        if len(records) > 1:
            result["duplicates"].append(records[0].get("id", key))
    for key, entry in entries.items():
        records = artifacts_by_id.get(key, [])
        if not records:
            result["missing"].append(entry["id"])
            errors.append(f"missing NuGet artifact: {entry['id']}")
            continue
        for artifact in records:
            comparison_errors = _check_nuget_metadata(
                artifact,
                entry["id"],
                entry["version"],
                manifest["source_commit"],
                manifest,
                artifact_label=artifact["path"],
            )
            artifact["errors"] = sorted(set(artifact.get("errors", []) + comparison_errors))
            errors.extend(comparison_errors)
        if len(records) > 1:
            errors.append(f"duplicate NuGet artifact: {entry['id']}")
    for key, records in artifacts_by_id.items():
        if key not in entries:
            name = str(records[0].get("id", key))
            result["extra"].append(name)
            errors.append(f"unexpected NuGet artifact: {name}")

    for feed in manifest["feeds"]:
        feed_result: dict[str, Any] = {"name": feed["name"], "base_url": feed["base_url"], "packages": []}
        for entry in manifest["nuget"]:
            package_result: dict[str, Any] = {
                "id": entry["id"],
                "version": entry["version"],
                "url": _nuget_url(feed["base_url"], entry["id"], entry["version"]),
                "verified": False,
            }
            if not entry["verify_published"]:
                package_result.update({"skipped": True, "reason": entry["reason"], "verified": True})
                feed_result["packages"].append(package_result)
                continue
            records = artifacts_by_id.get(entry["id"].lower(), [])
            if len(records) != 1:
                package_result["errors"] = ["published comparison requires exactly one local artifact"]
                errors.append(f"{feed['name']}: cannot compare {entry['id']} without one artifact")
                feed_result["packages"].append(package_result)
                continue
            local = records[0]
            try:
                published, _ = _download_nupkg(
                    package_result["url"], timeout=timeout, max_bytes=max_bytes,
                    content_expectations=manifest.get("content_expectations")
                )
                package_result.update(
                    {
                        "status": 200,
                        "published_payload_sha256": published["payload_sha256"],
                        "local_payload_sha256": local.get("payload_sha256"),
                        "payload_matches": published["payload_sha256"] == local.get("payload_sha256"),
                    }
                )
                metadata_errors = _check_nuget_metadata(
                    published,
                    entry["id"],
                    entry["version"],
                    manifest["source_commit"],
                    manifest,
                    artifact_label=f"{feed['name']}:{entry['id']}",
                )
                package_result["metadata"] = {
                    "id": published.get("id"),
                    "version": published.get("version"),
                    "repository_commit": published.get("repository_commit"),
                    "dependencies": published.get("dependencies", []),
                }
                package_result["errors"] = metadata_errors
                package_result["verified"] = not metadata_errors and package_result["payload_matches"]
                if not package_result["verified"]:
                    errors.extend(metadata_errors)
                    if not package_result["payload_matches"]:
                        errors.append(
                            f"{feed['name']}:{entry['id']}: published payload differs from local artifact"
                        )
            except (urllib.error.HTTPError, VerificationError) as exc:
                package_result.update(_http_error_record(exc))
                package_result["errors"] = [str(exc)]
                errors.append(f"{feed['name']}:{entry['id']}: {exc}")
            feed_result["packages"].append(package_result)
        feed_result["verified"] = all(package["verified"] for package in feed_result["packages"])
        result["feeds"].append(feed_result)
    if any(entry["verify_published"] for entry in manifest["nuget"]) and not manifest["feeds"]:
        errors.append("NuGet publication verification requires at least one feed")
    result["verified"] = not errors and all(feed["verified"] for feed in result["feeds"])
    return result, errors


def _verify_npm(
    manifest: dict[str, Any], artifacts_dir: Path, timeout: float, max_bytes: int
) -> tuple[dict[str, Any], list[str]]:
    entries = {item["name"].lower(): item for item in manifest["npm"]}
    result: dict[str, Any] = {
        "expected": [item["name"] for item in manifest["npm"]],
        "artifacts": [],
        "missing": [],
        "extra": [],
        "duplicates": [],
        "packages": [],
    }
    errors: list[str] = []
    artifacts_by_name: dict[str, list[dict[str, Any]]] = {}
    for path in _discover_artifacts(artifacts_dir, ".tgz"):
        try:
            artifact = _parse_npm_artifact(path, max_bytes)
            result["artifacts"].append(artifact)
            key = str(artifact.get("name", "")).lower()
            artifacts_by_name.setdefault(key, []).append(artifact)
        except VerificationError as exc:
            record = {"path": str(path), "file": path.name, "errors": [str(exc)]}
            result["artifacts"].append(record)
            errors.append(f"{path}: {exc}")
    for key, records in artifacts_by_name.items():
        if len(records) > 1:
            result["duplicates"].append(records[0].get("name", key))
            errors.append(f"duplicate npm artifact: {records[0].get('name', key)}")
    for key, entry in entries.items():
        records = artifacts_by_name.get(key, [])
        if not records:
            result["missing"].append(entry["name"])
            errors.append(f"missing npm artifact: {entry['name']}")
            continue
        for artifact in records:
            artifact_errors: list[str] = []
            if artifact.get("name") != entry["name"]:
                artifact_errors.append(
                    f"npm artifact name {artifact.get('name')!r} != {entry['name']!r}"
                )
            if artifact.get("version") != entry["version"]:
                artifact_errors.append(
                    f"npm artifact {entry['name']} version {artifact.get('version')!r} != {entry['version']!r}"
                )
            artifact["errors"] = sorted(set(artifact.get("errors", []) + artifact_errors))
            errors.extend(artifact_errors)
        if len(records) > 1:
            continue

        package_result: dict[str, Any] = {
            "name": entry["name"],
            "version": entry["version"],
            "dist_tag": entry["dist_tag"],
            "registry": entry["registry"],
            "verified": False,
        }
        try:
            version_metadata_url = _npm_url(entry["registry"], entry["name"], f"/{urllib.parse.quote(entry['version'], safe='')}")
            packument_url = _npm_url(entry["registry"], entry["name"])
            published = _download_json(version_metadata_url, timeout, max_bytes)
            packument = _download_json(packument_url, timeout, max_bytes)
            dist = published.get("dist")
            if not isinstance(dist, dict):
                raise VerificationError("published npm metadata has no dist object")
            published_name = published.get("name")
            published_version = published.get("version")
            if published_name != entry["name"]:
                raise VerificationError(f"published npm name {published_name!r} != {entry['name']!r}")
            if published_version != entry["version"]:
                raise VerificationError(
                    f"published npm version {published_version!r} != {entry['version']!r}"
                )
            dist_tags = packument.get("dist-tags")
            if not isinstance(dist_tags, dict) or dist_tags.get(entry["dist_tag"]) != entry["version"]:
                raise VerificationError(
                    f"npm dist-tag {entry['dist_tag']!r} does not point to {entry['version']!r}"
                )
            if _is_prerelease(entry["version"]) and dist_tags.get("latest") == entry["version"]:
                raise VerificationError("npm prerelease must not occupy the latest dist-tag")
            tarball_url = dist.get("tarball")
            integrity = dist.get("integrity")
            if not isinstance(tarball_url, str) or not tarball_url:
                raise VerificationError("published npm metadata has no tarball URL")
            if not isinstance(integrity, str) or not integrity.startswith("sha512-"):
                raise VerificationError("published npm metadata has no sha512 integrity")
            published_bytes = _download_bytes(tarball_url, timeout, max_bytes)
            published_integrity = "sha512-" + base64.b64encode(hashlib.sha512(published_bytes).digest()).decode("ascii")
            if published_integrity != integrity:
                raise VerificationError("published npm tarball does not match dist.integrity")
            local = records[0]
            if local.get("sha512_integrity") != integrity:
                raise VerificationError("local npm artifact does not match published dist.integrity")
            published_package = _package_json_from_tgz(published_bytes, max_bytes)
            if published_package.get("name") != entry["name"] or published_package.get("version") != entry["version"]:
                raise VerificationError("published npm tarball package metadata does not match manifest")
            package_result.update(
                {
                    "version_metadata_url": version_metadata_url,
                    "packument_url": packument_url,
                    "tarball_url": tarball_url,
                    "integrity": integrity,
                    "dist_tags": dist_tags,
                    "verified": True,
                }
            )
        except (urllib.error.HTTPError, VerificationError) as exc:
            package_result.update(_http_error_record(exc))
            package_result["errors"] = [str(exc)]
            errors.append(f"{entry['name']}: {exc}")
        result["packages"].append(package_result)
    for key, records in artifacts_by_name.items():
        if key not in entries:
            name = str(records[0].get("name", key))
            result["extra"].append(name)
            errors.append(f"unexpected npm artifact: {name}")
    result["verified"] = not errors and all(package["verified"] for package in result["packages"])
    return result, errors


def verify(manifest_path: Path, artifacts_dir: Path, timeout: float, max_bytes: int) -> dict[str, Any]:
    report: dict[str, Any] = {
        "verified": False,
        "version": None,
        "source_commit": None,
        "nuget": {},
        "npm": {},
        "errors": [],
    }
    try:
        raw = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest = _validate_manifest(raw)
        report["version"] = manifest["version"]
        report["source_commit"] = manifest["source_commit"]
        nuget_result, nuget_errors = _verify_nuget(manifest, artifacts_dir, timeout, max_bytes)
        npm_result, npm_errors = _verify_npm(manifest, artifacts_dir, timeout, max_bytes)
        report["nuget"] = nuget_result
        report["npm"] = npm_result
        report["errors"] = nuget_errors + npm_errors
        report["verified"] = not report["errors"] and nuget_result["verified"] and npm_result["verified"]
    except (OSError, UnicodeError, json.JSONDecodeError, VerificationError) as exc:
        report["errors"] = [str(exc)]
    return report


def _write_report(path: Path, report: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--artifacts", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--timeout", type=float, default=DEFAULT_TIMEOUT)
    parser.add_argument("--max-bytes", "--maxbytes", dest="max_bytes", type=int, default=DEFAULT_MAX_BYTES)
    args = parser.parse_args(argv)
    if args.timeout <= 0:
        parser.error("--timeout must be greater than zero")
    if args.max_bytes <= 0:
        parser.error("--max-bytes must be greater than zero")
    report = verify(args.manifest, args.artifacts, args.timeout, args.max_bytes)
    try:
        _write_report(args.output, report)
    except OSError as exc:
        print(json.dumps({"verified": False, "errors": [f"cannot write report: {exc}"]}))
        return 1
    summary = {
        "verified": report["verified"],
        "version": report["version"],
        "source_commit": report["source_commit"],
        "errors": len(report["errors"]),
    }
    print(json.dumps(summary, sort_keys=True))
    return 0 if report["verified"] else 1


if __name__ == "__main__":
    sys.exit(main())
