#!/usr/bin/env python3
"""Read-only reconciliation of the original accepted consolidated NuGet candidate."""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from datetime import datetime, timezone
import hashlib
import http.client
import importlib.util
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import queue
import socket
import time
import threading
from typing import Protocol
import urllib.error
import urllib.parse
import urllib.request
import zipfile

FEED_INDEX = "https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json"
FEED_ORIGIN = "https://f.feedz.io"
VERSION = "3.10.0"
SOURCE = "ba5b348aa2414fdf7c19d9d8806e87b7c91a4205"
PACKAGE_COUNT = 225
MAX_PACKAGE_BYTES = 32 * 1024 * 1024
MAX_PAYLOAD_BYTES = 256 * 1024 * 1024
MAX_INDEX_BYTES = 1024 * 1024
TIMEOUT = 25.0
FAILURES = frozenset({"authentication_failed", "throttled", "server_failed", "unexpected_status",
                      "timeout", "transport_failed", "redirect_rejected", "ambiguous_response",
                      "oversized_response", "incomplete_response", "malformed_index", "malformed_package",
                      "identity_mismatch", "source_mismatch", "dependencies_mismatch", "payload_mismatch",
                      "local_input_invalid", "interrupted"})
PENDING_GATES = ["intended_reader_access", "remote_symbols", "publisher_authority", "publisher_quiescence",
                 "publication_approval", "full_compatibility"]


class RecoveryError(ValueError):
    """A fixed diagnostic category, never an exception body or private path."""


@dataclass(frozen=True)
class ReadResult:
    status: int | None
    body: bytes
    complete: bool
    failure_category: str | None = None


class ReadTransport(Protocol):
    def get(self, url: str, *, authorization: str | None, max_bytes: int, timeout: float) -> ReadResult: ...


class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def _intended_url(url: str) -> bool:
    try:
        parsed = urllib.parse.urlsplit(url)
        return (parsed.scheme == "https" and parsed.netloc == "f.feedz.io" and not parsed.query
                and not parsed.fragment and parsed.path.startswith("/elsa-workflows/elsa-3/nuget/")
                and len(url) <= 2048 and not any(ord(c) <= 32 or ord(c) == 127 for c in url)
                and "%" not in parsed.path and "\\" not in parsed.path and ".." not in parsed.path.split("/"))
    except ValueError:
        return False


class FeedzTransport:
    """Bounded GET only, no proxy discovery, redirects, retries or credential lookup."""

    def __init__(self):
        self._opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), _NoRedirect())
        self._lock = threading.Lock()
        self._abandoned = False

    def get(self, url: str, *, authorization: str | None, max_bytes: int, timeout: float) -> ReadResult:
        # Socket timeouts do not bound DNS. At most one worker can remain outstanding;
        # after a deadline this transport is permanently fail-closed, without retries.
        with self._lock:
            if self._abandoned:
                return ReadResult(None, b"", False, "timeout")
            result_queue = queue.Queue(maxsize=1)
            def read():
                try:
                    result = self._get(url, authorization=authorization, max_bytes=max_bytes, timeout=timeout)
                except Exception:
                    result = ReadResult(None, b"", False, "transport_failed")
                result_queue.put_nowait(result)
            worker = threading.Thread(target=read, daemon=True, name="consolidated-recovery-read")
            worker.start()
            try:
                return result_queue.get(timeout=timeout)
            except queue.Empty:
                self._abandoned = True
                return ReadResult(None, b"", False, "timeout")
            except BaseException:
                self._abandoned = True
                raise

    def _get(self, url: str, *, authorization: str | None, max_bytes: int, timeout: float) -> ReadResult:
        if not _intended_url(url):
            return ReadResult(None, b"", False, "redirect_rejected")
        response = None
        status = None
        started = time.monotonic()
        try:
            headers = {"Accept-Encoding": "identity", "User-Agent": "Elsa-Consolidated-Recovery-Preflight/1"}
            if authorization is not None:
                if not authorization or len(authorization) > 8192 or any(ord(c) < 32 or ord(c) == 127 for c in authorization):
                    return ReadResult(None, b"", False, "authentication_failed")
                headers["Authorization"] = authorization
            request = urllib.request.Request(url, headers=headers, method="GET")
            try:
                response = self._opener.open(request, timeout=timeout)
            except urllib.error.HTTPError as error:
                response = error
            status = response.code
            if response.geturl() != url or 300 <= status <= 399:
                return ReadResult(status, b"", False, "redirect_rejected")
            lengths = response.headers.get_all("Content-Length", [])
            encodings = response.headers.get_all("Content-Encoding", [])
            transfers = response.headers.get_all("Transfer-Encoding", [])
            if (len(lengths) > 1 or (lengths and not re.fullmatch(r"[0-9]+", lengths[0]))
                    or (encodings and encodings != ["identity"]) or (transfers and transfers != ["chunked"])
                    or (lengths and transfers)):
                return ReadResult(status, b"", False, "ambiguous_response")
            expected = int(lengths[0]) if lengths else None
            if expected is not None and expected > max_bytes:
                return ReadResult(status, b"", False, "oversized_response")
            body = bytearray()
            while True:
                remaining = timeout - (time.monotonic() - started)
                if remaining <= 0:
                    return ReadResult(status, b"", False, "timeout")
                # Limit each socket wait to the remaining total GET deadline.
                reader_socket = getattr(getattr(getattr(response, "fp", None), "raw", None), "_sock", None)
                if reader_socket is not None:
                    reader_socket.settimeout(remaining)
                chunk = response.read1(min(65536, max_bytes + 1 - len(body)))
                if not chunk:
                    break
                body.extend(chunk)
                if len(body) > max_bytes:
                    return ReadResult(status, b"", False, "oversized_response")
            if expected is not None and len(body) != expected:
                return ReadResult(status, b"", False, "incomplete_response")
            return ReadResult(status, bytes(body), True)
        except (TimeoutError, socket.timeout):
            return ReadResult(status, b"", False, "timeout")
        except urllib.error.URLError as error:
            category = "timeout" if isinstance(error.reason, (TimeoutError, socket.timeout)) else "transport_failed"
            return ReadResult(status, b"", False, category)
        except http.client.IncompleteRead:
            return ReadResult(status, b"", False, "incomplete_response")
        except (OSError, ValueError, OverflowError):
            return ReadResult(status, b"", False, "transport_failed")
        finally:
            if response is not None:
                try:
                    response.close()
                except Exception:
                    pass


def _sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _json(data: bytes):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("duplicate key")
            result[key] = value
        return result
    return json.loads(data.decode("utf-8"), object_pairs_hook=unique)


def _verifier():
    path = Path(__file__).resolve().parents[2] / ".agents/skills/elsa-release/scripts/verify_packages.py"
    spec = importlib.util.spec_from_file_location("consolidated_recovery_shared_verifier", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _valid_sha(value, length=64):
    return isinstance(value, str) and re.fullmatch(r"[0-9a-f]{%d}" % length, value) is not None


def _provenance(value: dict) -> dict:
    keys = {"candidate_producer", "artifact_name", "archive_sha256", "archive_size", "original_envelope_sha256",
            "preupload_manifest_sha256", "verified_artifacts_sha256", "inventory_sha256", "live_retrieval_sha256",
            "observed_at", "planner"}
    try:
        if not isinstance(value, dict) or set(value) != keys:
            raise ValueError()
        producer = value["candidate_producer"]
        if not isinstance(producer, dict) or any(type(producer.get(key)) is not int for key in ("run_id", "run_attempt", "artifact_id")):
            raise ValueError()
        if producer != {"version": VERSION, "source_commit": SOURCE, "run_id": 37456860080,
                        "run_attempt": 1, "artifact_id": 11412848210}:
            raise ValueError()
        if value["artifact_name"] != f"consolidated-candidate-{SOURCE}-37456860080-1":
            raise ValueError()
        if type(value["archive_size"]) is not int or value["archive_size"] != 92659861:
            raise ValueError()
        for key in keys - {"candidate_producer", "artifact_name", "archive_size", "observed_at", "planner"}:
            if not _valid_sha(value[key]):
                raise ValueError()
        stamp = datetime.fromisoformat(value["observed_at"].replace("Z", "+00:00"))
        if stamp.tzinfo is None or stamp > datetime.now(timezone.utc):
            raise ValueError()
        planner = value["planner"]
        if set(planner) != {"source_commit", "run_id", "run_attempt"} or not _valid_sha(planner["source_commit"], 40):
            raise ValueError()
        if (planner["run_id"], planner["run_attempt"]) != (None, None) and not all(
                type(planner[key]) is int and planner[key] > 0 for key in ("run_id", "run_attempt")):
            raise ValueError()
        return _json(json.dumps(value).encode())
    except (TypeError, ValueError, AttributeError, KeyError):
        raise RecoveryError("local_input_invalid") from None


def _read_result(transport, url, authorization, limit):
    try:
        result = transport.get(url, authorization=authorization, max_bytes=limit, timeout=TIMEOUT)
    except KeyboardInterrupt:
        return ReadResult(None, b"", False, "interrupted")
    except Exception:
        return ReadResult(None, b"", False, "transport_failed")
    if (not isinstance(result, ReadResult) or type(result.complete) is not bool or not isinstance(result.body, bytes)
            or (result.status is not None and (type(result.status) is not int or not 100 <= result.status <= 599))
            or result.failure_category not in FAILURES | {None}):
        return ReadResult(None, b"", False, "ambiguous_response")
    if len(result.body) > limit:
        return ReadResult(result.status, b"", False, "oversized_response")
    if result.failure_category or not result.complete:
        return ReadResult(result.status, b"", False, result.failure_category or "incomplete_response")
    if result.status is None:
        return ReadResult(None, b"", False, "ambiguous_response")
    return result


def _status_failure(result):
    if result.failure_category:
        return result.failure_category
    status = result.status
    if status in (401, 403):
        return "authentication_failed"
    if status == 429:
        return "throttled"
    if 300 <= status <= 399:
        return "redirect_rejected"
    if status >= 500:
        return "server_failed"
    return "unexpected_status"


def _package_base(result):
    if result.failure_category or result.status != 200:
        raise RecoveryError(_status_failure(result))
    try:
        value = _json(result.body)
        resources = value["resources"]
        if value.get("version") != "3.0.0" or not isinstance(resources, list) or len(resources) > 128:
            raise ValueError()
        candidates = [row["@id"] for row in resources if isinstance(row, dict) and row.get("@type") == "PackageBaseAddress/3.0.0"]
        if len(candidates) != 1 or not isinstance(candidates[0], str):
            raise ValueError()
        base = candidates[0]
        if not _intended_url(base):
            raise ValueError()
        # Feedz advertises its package base without a trailing slash. Append the
        # package-relative path without replacing the final base path segment.
        return base if base.endswith("/") else base + "/"
    except (ValueError, TypeError, KeyError, AttributeError, UnicodeError, RecursionError):
        raise RecoveryError("malformed_index") from None


def _parse_package(data, verifier):
    # Narrow structural guard missing from the shared parser; comparison stays shared.
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        infos = archive.infolist()
        if len(infos) > verifier.MAX_ARCHIVE_MEMBERS:
            raise ValueError()
        names = set()
        for info in infos:
            name = info.filename.rstrip("/")
            path = PurePosixPath(name)
            mode = info.external_attr >> 16
            if (not name or path.is_absolute() or ".." in path.parts or "\\" in name or ":" in name
                    or str(path) != name or name.casefold() in names
                    or (mode & 0o170000) == 0o120000 or info.flag_bits & 1
                    or (name.lower().endswith(".signature.p7s") and name.lower() != ".signature.p7s")):
                raise ValueError()
            names.add(name.casefold())
    return verifier._parse_nuspec(data, MAX_PAYLOAD_BYTES)


def _local_inventory(root, provenance, verifier):
    manifest_data = (root / "verified-artifacts.json").read_bytes()
    preupload_data = (root / "preupload-manifest.json").read_bytes()
    if (_sha(manifest_data) != provenance["verified_artifacts_sha256"] or
            _sha(preupload_data) != provenance["preupload_manifest_sha256"]):
        raise RecoveryError("local_input_invalid")
    manifest, preupload = _json(manifest_data), _json(preupload_data)
    from prepare_consolidated_release_candidate import inventory_identity
    inventory_data = json.dumps(inventory_identity(manifest), sort_keys=True, separators=(",", ":")).encode()
    if _sha(inventory_data) != provenance["inventory_sha256"]:
        raise RecoveryError("local_input_invalid")
    rows = manifest["packages"]
    if (manifest["source_commit"] != SOURCE or manifest["version"] != VERSION or manifest["published"] is not False
            or not isinstance(rows, list) or len(rows) != PACKAGE_COUNT):
        raise RecoveryError("local_input_invalid")
    pins = {row["path"]: row for row in preupload["files"]}
    if len(pins) != len(preupload["files"]):
        raise RecoveryError("local_input_invalid")
    seen, local, invalid = set(), [], False
    for row in rows:
        package_id = row["id"]
        if (not isinstance(package_id, str) or not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{0,99}", package_id)
                or package_id.lower() in seen):
            raise RecoveryError("local_input_invalid")
        seen.add(package_id.lower())
        if any(not _valid_sha(row.get(f"{extension}_sha256")) for extension in ("nupkg", "snupkg")):
            raise RecoveryError("local_input_invalid")
        item = {"id": package_id, "version": VERSION, "local_nupkg_sha256": row["nupkg_sha256"],
                "local_snupkg_sha256": row["snupkg_sha256"], "local_payload_sha256": None}
        try:
            for extension in ("nupkg", "snupkg"):
                name = f"{package_id}.{VERSION}.{extension}"
                if row[extension] != name or not _valid_sha(row[f"{extension}_sha256"]):
                    raise ValueError()
                relative = f"artifacts/{name}"
                path = root / relative
                if path.is_symlink() or not path.is_file() or path.stat().st_size > MAX_PACKAGE_BYTES:
                    raise ValueError()
                data = path.read_bytes()
                pin = pins[relative]
                if _sha(data) != row[f"{extension}_sha256"] or pin["sha256"] != _sha(data) or pin["size"] != len(data):
                    raise ValueError()
                metadata = _parse_package(data, verifier)
                if (metadata["id"] != package_id or metadata["version"] != VERSION
                        or metadata["repository_commit"] != SOURCE):
                    raise ValueError()
                if extension == "nupkg":
                    item["metadata"] = metadata
                    item["local_payload_sha256"] = metadata["payload_sha256"]
        except Exception:
            invalid = True
        local.append(item)
    expected = {f"{row['id']}.{VERSION}.{ext}" for row in local for ext in ("nupkg", "snupkg")}
    actual = {path.name for path in (root / "artifacts").iterdir() if path.suffix in (".nupkg", ".snupkg")}
    if expected != actual or "elsa.samplepackage" not in seen:
        raise RecoveryError("local_input_invalid")
    return manifest, local, invalid


def plan_recovery(verified_root: Path, original_provenance: dict, *, transport: ReadTransport | None = None,
                  authorization: str | None = None) -> dict:
    """Call only after immutable-input/full-manifest preflight; recheck all local package pins before GET."""
    provenance = _provenance(original_provenance)
    verifier = _verifier()
    try:
        manifest, local, local_invalid = _local_inventory(verified_root, provenance, verifier)
    except (OSError, ValueError, KeyError, TypeError, UnicodeError):
        raise RecoveryError("local_input_invalid") from None
    reader = transport if transport is not None else FeedzTransport()
    global_failure = "local_input_invalid" if local_invalid else None
    base = None
    index_result = ReadResult(None, b"", False, global_failure)
    if global_failure is None:
        try:
            index_result = _read_result(reader, FEED_INDEX, authorization, MAX_INDEX_BYTES)
            base = _package_base(index_result)
        except RecoveryError as error:
            global_failure = str(error)
    index_failure = global_failure
    receipts = []
    for local_row in local:
        row = {key: value for key, value in local_row.items() if key != "metadata"}
        row.update({"remote": {"status": None, "complete": False, "archive_sha256": None, "payload_sha256": None},
                    "comparison": {"identity": None, "source": None, "dependencies": None, "payload": None},
                    "classification": "unverifiable", "failure_category": global_failure})
        if global_failure is None:
            key = local_row["id"].lower()
            result = _read_result(reader, f"{base}{key}/{VERSION}/{key}.{VERSION}.nupkg", authorization, MAX_PACKAGE_BYTES)
            row["remote"].update({"status": result.status, "complete": result.complete,
                                   "archive_sha256": _sha(result.body) if result.complete else None})
            if result.failure_category is None and result.status == 404:
                row["classification"] = "missing"
            elif result.failure_category is None and result.status == 200:
                try:
                    remote = _parse_package(result.body, verifier)
                    own = local_row["metadata"]
                    # The payload comparison also binds complete nuspec bytes, including dependency groups.
                    comparison = {"identity": remote["id"].lower() == key and remote["version"] == VERSION,
                                  "source": remote["repository_commit"] == SOURCE,
                                  "dependencies": sorted(remote["dependencies"], key=lambda x: (x["id"], x["version"])) == sorted(own["dependencies"], key=lambda x: (x["id"], x["version"])),
                                  "payload": remote["payload_sha256"] == own["payload_sha256"]}
                    row["remote"]["payload_sha256"] = remote["payload_sha256"]
                    row["comparison"] = comparison
                    row["classification"] = "matching" if all(comparison.values()) else "conflicting"
                    row["failure_category"] = next((f"{field}_mismatch" for field, matches in comparison.items() if not matches), None)
                except Exception:
                    row["failure_category"] = "malformed_package"
            else:
                row["failure_category"] = _status_failure(result)
            if result.failure_category == "interrupted":
                global_failure = "interrupted"
        row["observed_at"] = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
        receipts.append(row)
    counts = {name: sum(row["classification"] == name for row in receipts)
              for name in ("missing", "matching", "conflicting", "unverifiable")}
    return {"schema": 1, "original_provenance": provenance, "feed": FEED_INDEX,
            "feed_observation": {"status": index_result.status, "complete": index_result.complete,
                                 "archive_sha256": _sha(index_result.body) if index_result.complete else None,
                                 "failure_category": index_failure},
            "access_mode": "anonymous" if authorization is None else "credential_present_unverified_scope",
            "observation_mode": "production" if transport is None else "injected_simulation",
            "absence_scope": "declared_access_visibility_only", "packages": receipts, "counts": counts,
            "package_count": len(receipts), "exclusion_count": len(manifest["exclusions"]),
            "exclusions_sha256": _sha(json.dumps(manifest["exclusions"], sort_keys=True, separators=(",", ":")).encode()),
            "classification_complete": len(receipts) == PACKAGE_COUNT and counts["unverifiable"] == 0,
            "reconciliation_blocked": bool(counts["conflicting"] or counts["unverifiable"]),
            "content_converged": counts["matching"] == PACKAGE_COUNT, "publication_ready": False,
            "publication_performed": False, "remote_symbols_verified": False, "pending_gates": list(PENDING_GATES)}


def validate_cli_paths(inputs: Path, destination: Path, output: Path) -> None:
    """Reject symlinked paths and input/destination/receipt overlap before any writes."""
    paths = [path.absolute() for path in (inputs, destination, output)]
    for path in paths:
        if any(parent.is_symlink() for parent in (path, *path.parents)):
            raise RecoveryError("local_input_invalid")
    source, target, receipt = [path.resolve() for path in paths]
    if (source.is_relative_to(target) or target.is_relative_to(source)
            or any(receipt.is_relative_to(path) or path.is_relative_to(receipt) for path in (source, target))):
        raise RecoveryError("local_input_invalid")


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("inputs", "verified-root", "output"):
        parser.add_argument(f"--{name}", required=True, type=Path)
    parser.add_argument("--planner-source", required=True)
    parser.add_argument("--planner-run", type=int)
    parser.add_argument("--planner-attempt", type=int)
    parser.add_argument("--authorization-env")
    args = parser.parse_args(argv)
    report = {"schema": 1, "publication_performed": False, "publication_ready": False,
              "classification_complete": False, "reconciliation_blocked": True, "failure_category": "local_input_invalid"}
    try:
        validate_cli_paths(args.inputs, args.verified_root, args.output)
        from consolidated_recovery_inputs import prepare_recovery_inputs
        authorization = None
        if args.authorization_env is not None:
            if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]{0,127}", args.authorization_env):
                raise RecoveryError("authentication_failed")
            authorization = os.environ.get(args.authorization_env)
            if not authorization:
                raise RecoveryError("authentication_failed")
        root, provenance = prepare_recovery_inputs(args.inputs, args.verified_root, planner_source=args.planner_source,
                                                  planner_run=args.planner_run, planner_attempt=args.planner_attempt)
        report = plan_recovery(root, provenance, authorization=authorization)
    except Exception as error:
        # Input verification exceptions may contain private paths or remote error bodies.
        if isinstance(error, RecoveryError) and str(error) in FAILURES:
            report["failure_category"] = str(error)
    try:
        validate_cli_paths(args.inputs, args.verified_root, args.output)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("x", encoding="utf-8") as writer:
            json.dump(report, writer, indent=2, sort_keys=True)
            writer.write("\n")
    except (OSError, RecoveryError):
        print('{"classification_complete":false,"failure_category":"receipt_write_failed","publication_performed":false}')
        return 1
    print(json.dumps({key: report[key] for key in ("classification_complete", "reconciliation_blocked", "publication_performed")}, sort_keys=True))
    return 0 if report["classification_complete"] and not report["reconciliation_blocked"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
