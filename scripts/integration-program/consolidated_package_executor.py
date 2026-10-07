#!/usr/bin/env python3
"""Execute only the original consolidated candidate; operational admission is unset.

Verification/rehearsal have no publication authority. Publication requires native
same-run GitHub environment approval AND fresh exact reviewed policy metadata.
A JSON receipt, CLI option or dispatch input never supplies that authority.
"""
from __future__ import annotations

import argparse
import base64
import copy
from datetime import datetime, timezone
import hashlib
import io
import json
import os
from pathlib import Path
import re
import selectors
import signal
import subprocess
import sys
import tempfile
import time
import zipfile

import consolidated_candidate_input as candidate_input
import consolidated_package_recovery as recovery
import consolidated_recovery_inputs as inputs
import prove_consolidated_packages as packages

REPOSITORY = "elsa-workflows/elsa-core"
WORKFLOW = ".github/workflows/publish-consolidated-original-candidate.yml"
ENVIRONMENT = "elsa-3-10-feedz"
PUBLISH_SECRET = "ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY"
METADATA_SECRET = "ELSA_CONSOLIDATED_METADATA_READ_TOKEN"
METADATA_PATH = re.compile(r"repos/elsa-workflows/elsa-core/(?:actions/(?:secrets|organization-secrets)|environments/elsa-3-10-feedz/secrets)\?per_page=100&page=[1-9][0-9]*\Z")
PACKAGE_BASE = "https://f.feedz.io/elsa-workflows/elsa-3/nuget/v3/packages"
PACKAGE_PUBLISH = "https://f.feedz.io/elsa-workflows/elsa-3/nuget"
SYMBOL_PUBLISH = "https://f.feedz.io/elsa-workflows/elsa-3/symbols"
EXPIRY = "2026-11-05T12:13:30Z"
# A later separately approved source change must pin real existing IDs, exact
# refs and the operational packet. No CLI/environment/artifact override exists.
REVIEWED_OPERATIONAL_POLICY = None
MAX_BYTES = 32 * 1024 * 1024
MAX_REQUESTS = 5000
MAX_SECONDS = 1800
SHA256 = re.compile(r"[0-9a-f]{64}\Z")
KEY = re.compile(r"([a-z0-9_.-]+\.pdb)/([0-9a-f]{32}FFFFFFFF)/\1\Z")
PENDING = ["approved_live_cutover", "provider_overwrite_controls", "full_compatibility",
           "private_intended_reader_rights", "original_snupkg_archive_readback"]


class ExecutorError(ValueError):
    """Fixed categories only. Never include child output, URLs or credentials."""


def require(condition, code):
    if not condition:
        raise ExecutorError(code)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def now():
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


# Network work lives in a disposable child: DNS, writes and response reads all
# have a hard parent deadline. Stdin carries credentials/bytes, never argv/files.
# The parent bounds stdin and stdout concurrently and always kills/reaps the
# process group on failure. There is no lingering network reader or retry.
HTTP_WORKER = r'''
import base64, http.client, json, socket, sys, urllib.error, urllib.request
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs): return None
result = {"status": None, "complete": False, "failure_category": "transport_failed", "body": ""}
response = None
try:
    value = json.loads(sys.stdin.buffer.read(50 * 1024 * 1024 + 1))
    headers = {"Accept-Encoding": "identity", "User-Agent": "Elsa-Original-Candidate-Executor/1"}
    headers.update(value["headers"])
    data = base64.b64decode(value["data"], validate=True) if value["method"] == "PUT" else None
    request = urllib.request.Request(value["url"], data=data, headers=headers, method=value["method"])
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
    try: response = opener.open(request, timeout=value["timeout"])
    except urllib.error.HTTPError as error: response = error
    result["status"] = response.code
    lengths = response.headers.get_all("Content-Length", [])
    encodings = response.headers.get_all("Content-Encoding", [])
    transfers = response.headers.get_all("Transfer-Encoding", [])
    if response.geturl() != value["url"] or 300 <= response.code < 400:
        result["failure_category"] = "redirect_rejected"
    elif (len(lengths) > 1 or (lengths and not lengths[0].isdigit())
          or (encodings and encodings != ["identity"]) or (transfers and transfers != ["chunked"])
          or (lengths and transfers)):
        result["failure_category"] = "ambiguous_response"
    elif lengths and int(lengths[0]) > value["limit"]:
        result["failure_category"] = "oversized_response"
    else:
        body = response.read(value["limit"] + 1)
        if len(body) > value["limit"]: result["failure_category"] = "oversized_response"
        elif lengths and len(body) != int(lengths[0]): result["failure_category"] = "incomplete_response"
        else:
            result.update(complete=True, failure_category=None,
                          body=base64.b64encode(body).decode() if value["method"] == "GET" else "")
except (TimeoutError, socket.timeout): result["failure_category"] = "timeout"
except http.client.IncompleteRead: result["failure_category"] = "incomplete_response"
except BaseException: pass
finally:
    if response is not None: response.close()
sys.stdout.write(json.dumps(result))
'''


def bounded_child(command, payload, *, deadline, limit, env=None):
    require(os.name == "posix", "unsupported_platform")
    require(time.monotonic() < deadline, "deadline_exceeded")
    with subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                          stderr=subprocess.DEVNULL, start_new_session=True, env=env) as child:
        completed = False
        try:
            output, offset = bytearray(), 0
            os.set_blocking(child.stdout.fileno(), False)
            os.set_blocking(child.stdin.fileno(), False)
            with selectors.DefaultSelector() as selector:
                selector.register(child.stdout, selectors.EVENT_READ)
                if payload:
                    selector.register(child.stdin, selectors.EVENT_WRITE)
                else:
                    child.stdin.close()
                while selector.get_map():
                    remaining = deadline - time.monotonic()
                    require(remaining > 0, "deadline_exceeded")
                    events = selector.select(remaining)
                    require(bool(events), "deadline_exceeded")
                    for key, _ in events:
                        try:
                            if key.fileobj is child.stdin:
                                offset += os.write(child.stdin.fileno(), payload[offset:offset + 65536])
                                if offset == len(payload):
                                    selector.unregister(child.stdin)
                                    child.stdin.close()
                            else:
                                chunk = os.read(child.stdout.fileno(), min(65536, limit + 1 - len(output)))
                                if not chunk:
                                    selector.unregister(child.stdout)
                                else:
                                    output.extend(chunk)
                                    require(len(output) <= limit, "response_limit")
                        except BlockingIOError:
                            continue
            require(child.wait(timeout=max(0.001, deadline-time.monotonic())) == 0, "child_failed")
            completed = True
            return bytes(output)
        except (OSError, subprocess.TimeoutExpired):
            raise ExecutorError("child_failed") from None
        finally:
            if not completed:
                try:
                    os.killpg(child.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                child.wait()


class HttpTransport:
    """Fixed-domain bounded GET/PUT; no credentials discovered, no retries."""

    def __init__(self, *, seconds=MAX_SECONDS, max_requests=MAX_REQUESTS):
        self.deadline = time.monotonic() + seconds
        self.max_requests, self.request_count = max_requests, 0

    def _wire_url(self, url):
        return url

    def request(self, method, url, *, data=b"", headers=None, limit=MAX_BYTES, timeout=25):
        require(method in {"GET", "PUT"}, "unsupported_method")
        headers = headers or {}
        require(set(headers) <= {"Authorization", "X-NuGet-ApiKey", "Content-Type"}, "unsafe_headers")
        if "Authorization" in headers:
            require(method == "GET" and url.startswith("https://api.github.com/")
                    and METADATA_PATH.fullmatch(url[len("https://api.github.com/"):]), "unsafe_metadata_authorization")
        require("X-NuGet-ApiKey" not in headers or method == "PUT" and url in {PACKAGE_PUBLISH, SYMBOL_PUBLISH}, "unsafe_publisher_authorization")
        allowed = (url == recovery.FEED_INDEX or recovery._intended_url(url)
                   or url in {PACKAGE_PUBLISH, SYMBOL_PUBLISH}
                   or (url.startswith(SYMBOL_PUBLISH + "/") and KEY.fullmatch(url[len(SYMBOL_PUBLISH)+1:]))
                   or re.fullmatch(r"https://api.github.com/repos/elsa-workflows/elsa-core/[A-Za-z0-9_./?=&%-]+", url))
        require(allowed and ".." not in url and len(data) <= MAX_BYTES, "unsafe_request")
        require(method != "PUT" or url in {PACKAGE_PUBLISH, SYMBOL_PUBLISH}, "unsafe_upload")
        require(self.request_count < self.max_requests, "request_limit")
        remaining = self.deadline - time.monotonic()
        require(remaining > 0, "deadline_exceeded")
        self.request_count += 1
        value = {"method": method, "url": self._wire_url(url), "data": base64.b64encode(data).decode(),
                 "headers": headers or {}, "limit": limit, "timeout": min(timeout, remaining)}
        try:
            raw = bounded_child([sys.executable, "-I", "-c", HTTP_WORKER], json.dumps(value).encode(),
                                deadline=min(self.deadline, time.monotonic()+timeout),
                                limit=4*((limit+2)//3)+4096, env={"PATH": os.environ.get("PATH", "")})
            result = recovery._json(raw)
            require(set(result) == {"status", "complete", "failure_category", "body"}
                    and type(result["complete"]) is bool
                    and (result["status"] is None or type(result["status"]) is int and 100 <= result["status"] <= 599),
                    "invalid_transport")
            body = base64.b64decode(result["body"], validate=True)
            require(len(body) <= limit, "response_limit")
            failure = result["failure_category"]
            require(failure is None or failure in recovery.FAILURES, "invalid_transport")
            return recovery.ReadResult(result["status"], body, result["complete"], failure)
        except ExecutorError as error:
            category = "timeout" if str(error) == "deadline_exceeded" else "transport_failed"
            return recovery.ReadResult(None, b"", False, category)
        except (ValueError, TypeError, KeyError):
            return recovery.ReadResult(None, b"", False, "transport_failed")

    def get(self, url, *, authorization=None, max_bytes=MAX_BYTES, timeout=25):
        require(authorization is None, "unsupported_reader_credential")
        return self.request("GET", url, limit=max_bytes, timeout=timeout)

    def put(self, kind, data, key):
        require(kind in {"nupkg", "snupkg"} and isinstance(key, str) and 0 < len(key) <= 8192
                and all(32 < ord(c) < 127 for c in key), "invalid_publisher_credential")
        # NuGet PackagePublish uses one multipart form file; bytes are unchanged.
        boundary = "ElsaOriginalCandidate" + sha(data)[:32]
        require(boundary.encode() not in data, "invalid_multipart_boundary")
        body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"package\"; filename=\"original.{kind}\"\r\n"
                "Content-Type: application/octet-stream\r\n\r\n").encode() + data + f"\r\n--{boundary}--\r\n".encode()
        return self.request("PUT", PACKAGE_PUBLISH if kind == "nupkg" else SYMBOL_PUBLISH,
                            data=body, headers={"X-NuGet-ApiKey": key,
                                              "Content-Type": f"multipart/form-data; boundary={boundary}"}, limit=4096)


class GitHubApi:
    def __init__(self):
        self.transport = HttpTransport(seconds=120, max_requests=100)

    def get(self, path):
        url = "https://api.github.com/" + path
        if METADATA_PATH.fullmatch(path):
            # This distinct authority has Secrets(read)/Environments(read), no
            # Actions/Contents/org-admin permissions. Future setup must verify
            # those actual grants; a variable name cannot prove token scope.
            token = os.environ.get(METADATA_SECRET)
            require(isinstance(token, str) and 0 < len(token) <= 8192
                    and all(32 < ord(c) < 127 for c in token), "metadata_credential_missing")
            response = self.transport.request("GET", url, headers={"Authorization": "Bearer " + token}, limit=1024*1024)
        else:
            response = self.transport.get(url, max_bytes=1024*1024)
        require(response.complete and response.status == 200 and response.failure_category is None,
                "admission_metadata_unavailable")
        try:
            return recovery._json(response.body)
        except (ValueError, UnicodeError):
            raise ExecutorError("admission_metadata_invalid") from None


def paged(api, path, field, *, counted=True):
    rows, total = [], None
    for page in range(1, 21):
        value = api.get(f"{path}?per_page=100&page={page}")
        batch = value.get(field) if counted else value
        require(isinstance(batch, list) and len(batch) <= 100, "admission_pagination_invalid")
        if counted:
            count = value.get("total_count")
            require(type(count) is int and 0 <= count <= 1900 and (total is None or count == total),
                    "admission_pagination_changed")
            total = count
        rows.extend(batch)
        if len(batch) < 100:
            require(not counted or len(rows) == total, "admission_pagination_changed")
            return rows
    raise ExecutorError("admission_pagination_limit")


def check_admission(*, publish=False, api=None):
    """No user-supplied policy/approval parameter. API and policy patched only in tests."""
    policy = REVIEWED_OPERATIONAL_POLICY
    require(isinstance(policy, dict), "operational_policy_unconfigured")
    require(set(policy) == {"environment_id", "reviewer_rule_id", "branch_rule_id", "reviewer_ids", "branch_policies",
                            "allowed_ref", "operational_packet_sha256"}, "operational_policy_invalid")
    require(type(policy["environment_id"]) is int and policy["environment_id"] > 0
            and type(policy["reviewer_rule_id"]) is int and policy["reviewer_rule_id"] > 0
            and type(policy["branch_rule_id"]) is int and policy["branch_rule_id"] > 0
            and isinstance(policy["reviewer_ids"], list) and bool(policy["reviewer_ids"])
            and all(type(x) is int and x > 0 for x in policy["reviewer_ids"])
            and len(set(policy["reviewer_ids"])) == len(policy["reviewer_ids"])
            and isinstance(policy["operational_packet_sha256"], str)
            and SHA256.fullmatch(policy["operational_packet_sha256"]), "operational_policy_invalid")
    context = os.environ
    source, run, attempt = context.get("GITHUB_SHA", ""), context.get("GITHUB_RUN_ID", ""), context.get("GITHUB_RUN_ATTEMPT", "")
    require(context.get("GITHUB_ACTIONS") == "true" and context.get("GITHUB_REPOSITORY") == REPOSITORY
            and context.get("GITHUB_EVENT_NAME") == "workflow_dispatch" and re.fullmatch(r"[0-9a-f]{40}", source)
            and re.fullmatch(r"[1-9][0-9]*", run) and attempt == "1", "runtime_not_admitted")
    ref = policy["allowed_ref"]
    require(ref == "refs/heads/main"
            and ".." not in ref and context.get("GITHUB_REF") == ref
            and context.get("GITHUB_WORKFLOW_REF") == f"{REPOSITORY}/{WORKFLOW}@{ref}"
            and context.get("GITHUB_WORKFLOW_SHA") == source, "runtime_ref_mismatch")
    require(not publish or context.get("GITHUB_JOB") == "publish", "runtime_job_mismatch")
    root = Path(__file__).resolve().parents[2]
    git_environment = {"PATH": os.environ.get("PATH", os.defpath)}
    head = subprocess.run(["git", "rev-parse", "HEAD"], cwd=root, capture_output=True, timeout=10, env=git_environment)
    dirty = subprocess.run(["git", "status", "--porcelain", "--untracked-files=no"], cwd=root, capture_output=True, timeout=10, env=git_environment)
    require(head.returncode == dirty.returncode == 0 and head.stdout.decode().strip() == source
            and dirty.stdout == b"", "executor_source_mismatch")
    api = api or GitHubApi()
    prefix = f"repos/{REPOSITORY}"
    runtime = api.get(f"{prefix}/actions/runs/{run}/attempts/1")
    require(isinstance(runtime, dict) and isinstance(runtime.get("repository"), dict)
            and isinstance(runtime.get("head_repository"), dict), "runtime_identity_mismatch")
    require(type(runtime.get("id")) is int and runtime["id"] == int(run)
            and type(runtime.get("run_attempt")) is int and runtime["run_attempt"] == 1 and runtime.get("head_sha") == source
            and runtime.get("event") == "workflow_dispatch" and runtime.get("path") == WORKFLOW
            and runtime.get("head_branch") == "main" and runtime.get("head_repository", {}).get("full_name") == REPOSITORY
            and runtime.get("status") == "in_progress" and runtime.get("repository", {}).get("full_name") == REPOSITORY,
            "runtime_identity_mismatch")
    envelope, envelope_hash = inputs._read_bounded_json(candidate_input.ORIGINAL_ENVELOPE, "Original envelope")
    require(envelope_hash == candidate_input.ENVELOPE_SHA256, "candidate_envelope_changed")
    artifact = api.get(f"{prefix}/actions/artifacts/{candidate_input.ARTIFACT}")
    try:
        candidate_input.candidate.validate_envelope(envelope, artifact, source=candidate_input.SOURCE,
                                                   run_id=candidate_input.RUN, attempt=candidate_input.ATTEMPT)
    except (ValueError, RuntimeError, KeyError, TypeError):
        raise ExecutorError("candidate_unavailable_or_changed") from None
    environment = api.get(f"{prefix}/environments/{ENVIRONMENT}")
    require(isinstance(environment, dict), "environment_identity_mismatch")
    require(type(environment.get("id")) is int and environment["id"] == policy["environment_id"] and environment.get("name") == ENVIRONMENT
            and environment.get("can_admins_bypass") is False, "environment_identity_mismatch")
    rules = environment.get("protection_rules")
    require(isinstance(rules, list) and len(rules) == 2 and all(isinstance(row, dict) for row in rules)
            and {row.get("type") for row in rules} == {"required_reviewers", "branch_policy"},
            "environment_protection_mismatch")
    branch_rule = next(row for row in rules if row["type"] == "branch_policy")
    require(branch_rule.get("id") == policy["branch_rule_id"], "environment_protection_mismatch")
    rule = next(row for row in rules if row["type"] == "required_reviewers")
    reviewers = rule.get("reviewers")
    require(rule.get("type") == "required_reviewers" and rule.get("id") == policy["reviewer_rule_id"]
            and rule.get("prevent_self_review") is True and isinstance(reviewers, list)
            and all(row.get("type") == "User" and type(row.get("reviewer", {}).get("id")) is int for row in reviewers)
            and sorted(row["reviewer"]["id"] for row in reviewers) == sorted(policy["reviewer_ids"]),
            "environment_protection_mismatch")
    require(environment.get("deployment_branch_policy") == {"protected_branches": False, "custom_branch_policies": True},
            "environment_ref_policy_mismatch")
    branches = paged(api, f"{prefix}/environments/{ENVIRONMENT}/deployment-branch-policies", "branch_policies")
    actual = [{key: row.get(key) for key in ("id", "type", "name")} for row in branches]
    expected = policy["branch_policies"]
    require(isinstance(expected, list) and len(expected) == 1 and len(actual) == 1
            and type(expected[0].get("id")) is int and expected[0]["id"] > 0
            and expected[0].get("type") == ("branch" if ref.startswith("refs/heads/") else "tag")
            and expected[0].get("name") == ref.split("/", 2)[2] and actual == expected,
            "environment_ref_policy_mismatch")
    receipt = {"status": "eligible_for_native_approval", "repository": REPOSITORY, "ref": ref,
               "source_commit": source, "environment_name": ENVIRONMENT,
               "environment_id": policy["environment_id"], "reviewer_ids": policy["reviewer_ids"],
               "branch_policies": actual, "operational_packet_sha256": policy["operational_packet_sha256"],
               "policy_sha256": sha(json.dumps(policy, sort_keys=True, separators=(",", ":")).encode()),
               "executor_source": source, "implementation_sha256": sha(Path(__file__).read_bytes()),
               "run_id": int(run), "run_attempt": 1, "observed_at": now(),
               "native_approval_verified": False, "credential_provenance_verified": False,
               "credential_isolation": "pending_authenticated_execution_check",
               "candidate": {**candidate_input.PRODUCER, "archive_sha256": candidate_input.ARCHIVE_SHA256,
                             "preupload_manifest_sha256": candidate_input.MANIFEST_SHA256,
                             "expires_at": EXPIRY, "package_count": 225, "exclusion_count": 124,
                             "excluded_id_count": 123}}
    if publish:
        reviews = api.get(f"{prefix}/actions/runs/{run}/approvals")
        require(isinstance(reviews, list) and len(reviews) < 100, "native_approval_invalid")
        actor = runtime.get("triggering_actor", {}).get("id")
        require(type(actor) is int and actor > 0, "native_approval_invalid")
        applicable = [row for row in reviews if isinstance(row, dict) and isinstance(row.get("environments"), list)
                      and any(env.get("id") == policy["environment_id"] and env.get("name") == ENVIRONMENT
                              for env in row["environments"] if isinstance(env, dict))]
        require(len(applicable) == 1 and applicable[0].get("state") == "approved"
                and applicable[0].get("user", {}).get("id") in policy["reviewer_ids"]
                and applicable[0]["user"]["id"] != actor, "native_approval_missing")
        receipt["native_approval_verified"] = True
        receipt["status"] = "native_approval_verified"
        # Read both snapshots after native approval. Names prove availability in
        # these namespaces, never actual token grants or an organization-wide
        # absence. Complete metadata needs the separately reviewed read token.
        endpoints = {"repository": f"{prefix}/actions/secrets",
                     "organization_available_to_repository": f"{prefix}/actions/organization-secrets",
                     "environment": f"{prefix}/environments/{ENVIRONMENT}/secrets"}
        snapshots = []
        for _ in range(2):
            snapshot = {}
            for scope, path in endpoints.items():
                rows = paged(api, path, "secrets")
                names = [row.get("name") for row in rows]
                require(all(isinstance(name, str) and re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]{0,255}", name)
                            for name in names), "credential_metadata_invalid")
                names = sorted(name.upper() for name in names)
                require(len(set(names)) == len(names), "credential_metadata_invalid")
                snapshot[scope] = names
            snapshots.append(snapshot)
        require(snapshots[0] == snapshots[1], "credential_metadata_changed")
        dedicated = {PUBLISH_SECRET, METADATA_SECRET}
        for scope in ("repository", "organization_available_to_repository"):
            require(not dedicated.intersection(snapshots[1][scope]), "publisher_credential_fallback")
        require(dedicated <= set(snapshots[1]["environment"]), "environment_credential_missing")
        receipt["credential_metadata"] = [
            {"scope": scope, "count": len(names), "names_sha256": sha(json.dumps(names, separators=(",", ":")).encode()),
             "dedicated_names": sorted(dedicated.intersection(names))}
            for scope, names in snapshots[1].items()]
        receipt["credential_metadata_observed_at"] = now()
        receipt["credential_provenance_verified"] = True
        receipt["credential_isolation"] = "both_names_environment_only_metadata_observed"
    return receipt


class Inspector:
    def __init__(self, dll):
        self.dll = Path(dll)
        require(self.dll.is_file() and not self.dll.is_symlink(), "inspector_missing")
        require(0 < self.dll.stat().st_size <= 64*1024*1024, "inspector_size_limit")
        self.digest = sha(self.dll.read_bytes())
        self.source_digest = sha(Path(__file__).with_name("VerifyPackageSymbolPair").joinpath("Program.cs").read_bytes())
        self.deadline = time.monotonic() + MAX_SECONDS
        self.cache = {}

    def inspect(self, assembly, pdb, name):
        require(sha(self.dll.read_bytes()) == self.digest, "inspector_changed")
        key = (sha(assembly), sha(pdb), name)
        if key in self.cache:
            return copy.deepcopy(self.cache[key])
        with tempfile.TemporaryDirectory(prefix="elsa-original-symbol-") as directory:
            root = Path(directory)
            (root / (name[:-4] + ".dll")).write_bytes(assembly)
            (root / name).write_bytes(pdb)
            env = {key: value for key, value in os.environ.items()
                   if key in {"PATH", "DOTNET_ROOT", "HOME", "TMPDIR", "DOTNET_CLI_TELEMETRY_OPTOUT"}}
            try:
                raw = bounded_child(["dotnet", str(self.dll.resolve()), str(root / (name[:-4]+".dll")),
                                     str(root / name), "--inspect-symbols"], b"",
                                    deadline=min(self.deadline, time.monotonic()+30), limit=16*1024*1024, env=env)
                value = recovery._json(raw)
                self.cache[key] = value
                return copy.deepcopy(value)
            except (ExecutorError, ValueError, UnicodeError):
                raise ExecutorError("symbol_inspection_failed") from None


def associations(root, manifest, inspector):
    """Reinspect original DLL/PDBs and bind preserved source evidence to each pair."""
    source_data, _ = inputs._read_bounded_json(root / "receipt.json", "Original proof receipt", 32*1024*1024)
    retained = source_data.get("provenance")
    require(isinstance(retained, list) and len(retained) == recovery.PACKAGE_COUNT, "source_evidence_incomplete")
    source_rows = {row["id"]: row for row in retained}
    require(len(source_rows) == recovery.PACKAGE_COUNT, "source_evidence_incomplete")
    all_rows, unique = [], {}
    for row in manifest["packages"]:
        frames = row.get("assemblies")
        require(isinstance(frames, list), "assembly_coverage_invalid")
        expected_frames = {key for key, value in row["framework_properties"].items() if value["include_build_output"]}
        require({frame.get("framework") for frame in frames} == expected_frames
                and len(frames) == len(expected_frames), "assembly_coverage_invalid")
        sources = source_rows.get(row["id"], {})
        require({frame.get("framework") for frame in sources.get("frameworks", [])} == expected_frames
                and len(sources.get("frameworks", [])) == len(expected_frames),
                "source_evidence_incomplete")
        if not frames:
            require(sources.get("assembly_free") is True, "assembly_coverage_invalid")
            all_rows.append({"id": row["id"], "assembly_free": True, "associations": []})
            continue
        records = []
        with zipfile.ZipFile(root / "artifacts" / row["nupkg"]) as archive, zipfile.ZipFile(root / "artifacts" / row["snupkg"]) as symbols:
            require({name for name in symbols.namelist() if name.endswith(".pdb")} == {frame["pdb"] for frame in frames},
                    "symbol_coverage_invalid")
            for frame in frames:
                pdb, dll = symbols.read(frame["pdb"]), archive.read(frame["assembly"])
                require(len(pdb) <= MAX_BYTES and len(dll) <= MAX_BYTES, "symbol_size_limit")
                value = inspector.inspect(dll, pdb, Path(frame["pdb"]).name)
                require(isinstance(value, dict) and value.get("schema") == 1, "symbol_schema_invalid")
                symbol, details = value.get("symbol", {}), value.get("details", {})
                key = symbol.get("key", "")
                require(isinstance(key, str) and KEY.fullmatch(key), "symbol_key_invalid")
                require(symbol.get("pdb_name") == Path(frame["pdb"]).name.lower()
                        and symbol.get("guid", "").replace("-", "") + "FFFFFFFF" == key.split("/")[1]
                        and type(symbol.get("stamp")) is int and 0 <= symbol["stamp"] <= 0xffffffff
                        and symbol.get("pdb_sha256") == sha(pdb) and symbol.get("pdb_size") == len(pdb)
                        and symbol.get("checksum_algorithm") in {"SHA256", "SHA1"}
                        and symbol.get("declared_checksum") == symbol.get("normalized_checksum")
                        and re.fullmatch(r"[0-9a-f]{64}" if symbol["checksum_algorithm"] == "SHA256" else r"[0-9a-f]{40}", symbol.get("declared_checksum", "")),
                        "symbol_identity_invalid")
                require(details.get("assembly_name") == row["framework_properties"][frame["framework"]]["assembly_name"]
                        and details.get("assembly_version") == "3.10.0.0"
                        and details.get("informational_version") == f"3.10.0+{recovery.SOURCE}", "assembly_identity_invalid")
                maps = details.get("source_link", {}).get("documents", {})
                prefix = f"{packages.RAW_URL}{recovery.SOURCE}/"
                require(isinstance(maps, dict) and bool(maps) and all(isinstance(url, str) and url.startswith(prefix) for url in maps.values()),
                        "source_link_invalid")
                original = next(item for item in sources["frameworks"] if item["framework"] == frame["framework"])
                retained_paths = {item["path"] for item in original["documents"]}
                documents = details.get("documents")
                require(isinstance(documents, list), "source_documents_invalid")
                observed = []
                for document in documents:
                    path = document["path"]
                    url = packages.source_url(path, maps)
                    path = path if path in retained_paths else url[len(prefix):] if url is not None else path
                    require(document.get("embedded_checksum") in {None, document["checksum"]}, "source_documents_invalid")
                    observed.append((path, document["algorithm"], document["checksum"], document.get("embedded_checksum") is not None))
                expected = [(doc["path"], doc["algorithm"], doc["checksum"], doc.get("embedded", False)) for doc in original["documents"]]
                require(len(set(observed)) == len(observed) and sorted(observed) == sorted(expected), "source_documents_changed")
                if not documents:
                    require(original.get("document_coverage") == "not_applicable_interface_only"
                            and packages.only_abstract_methods(details), "source_documents_invalid")
                record = {"framework": frame["framework"], "assembly": frame["assembly"], "pdb": frame["pdb"],
                          **symbol, "source_evidence_preserved": True, "document_count": len(documents)}
                require(key not in unique or unique[key]["pdb"] == pdb, "duplicate_symbol_key_conflict")
                unique.setdefault(key, {"pdb": pdb, "assembly": dll, "name": Path(frame["pdb"]).name, "symbol": symbol})
                records.append(record)
        all_rows.append({"id": row["id"], "assembly_free": False, "associations": records})
    return all_rows, unique


def observe_symbols(transport, expected, inspector):
    rows = []
    for key, item in expected.items():
        row = {"key": key, "classification": "unverifiable", "status": None, "complete": False,
               "pdb_sha256": None, "failure_category": None}
        try:
            result = recovery._read_result(transport, SYMBOL_PUBLISH + "/" + key, None, MAX_BYTES)
            row.update(status=result.status, complete=result.complete)
            if result.failure_category or not result.complete:
                row["failure_category"] = result.failure_category or "incomplete_response"
            elif result.status == 404:
                row["classification"] = "missing"
            elif result.status == 200:
                row["pdb_sha256"] = sha(result.body)
                if result.body != item["pdb"]:
                    row.update(classification="conflicting", failure_category="pdb_bytes_mismatch")
                else:
                    value = inspector.inspect(item["assembly"], result.body, item["name"])
                    require(value["symbol"] == item["symbol"], "remote_symbol_identity_invalid")
                    row["classification"] = "matching"
            else:
                row["failure_category"] = recovery._status_failure(result)
        except Exception:
            row["failure_category"] = "symbol_read_failed"
        rows.append(row)
    return rows


def feed_resources(transport):
    result = recovery._read_result(transport, recovery.FEED_INDEX, None, recovery.MAX_INDEX_BYTES)
    require(result.complete and result.status == 200 and not result.failure_category, "feed_index_unavailable")
    value = recovery._json(result.body)
    require(value.get("version") == "3.0.0", "feed_index_invalid")
    for kind, expected in (("PackageBaseAddress/3.0.0", PACKAGE_BASE), ("PackagePublish/2.0.0", PACKAGE_PUBLISH),
                           ("SymbolPackagePublish/4.9.0", SYMBOL_PUBLISH)):
        require([row.get("@id") for row in value["resources"] if row.get("@type") == kind] == [expected], "feed_resource_mismatch")
    return sha(result.body)


def reconcile(root, provenance, expected, inspector, transport, index_hash, *, observation_mode):
    result = recovery.plan_recovery(root, provenance, transport=transport)
    # The shared planner sees an injected reader in both cases. Only this caller
    # knows whether it constructed the production HTTP reader or received a fake.
    result["observation_mode"] = observation_mode
    symbols = observe_symbols(transport, expected, inspector)
    consistent = result["feed_observation"]["archive_sha256"] == index_hash
    return {"packages": result, "symbols": symbols, "feed_index_consistent": consistent,
            "blocked": not consistent or result["reconciliation_blocked"] or any(row["classification"] in {"conflicting", "unverifiable"} for row in symbols),
            "content_verified": consistent and result["content_converged"] and all(row["classification"] == "matching" for row in symbols)}


def run_verified(root, provenance, inspector, *, mode, transport=None, authorize=None, credential=None, checkpoint=None):
    """Dependencies may be injected for tests; production CLI supplies native admission only."""
    ledger = {"schema": 1, "mode": mode, "scope": "production" if transport is None else "injected_simulation",
              "original_provenance": provenance, "feed": recovery.FEED_INDEX, "version": recovery.VERSION,
              "started_at": now(), "result": "failed", "failure_category": "local_input_invalid",
              "publication_performed": False, "upload_attempted": False, "content_verified": False, "remote_snupkg_archive_verified": False,
              "publication_ready": False, "pending_gates": list(PENDING), "operations": [], "associations": [],
              "admission": None, "before": None, "after": None}
    try:
        require((authorize is None and credential is None) or transport is not None, "simulation_dependencies_invalid")
        ledger["implementation_sha256"] = sha(Path(__file__).read_bytes())
        require(mode in {"verify", "publish"}, "invalid_mode")
        require(datetime.now(timezone.utc) < datetime.fromisoformat(EXPIRY.replace("Z", "+00:00")), "candidate_expired")
        manifest, _ = inputs._read_bounded_json(root / "verified-artifacts.json", "Original manifest", 32*1024*1024)
        # Input preparation already applies the accepted baseline/exclusion checks.
        # Reconciliation repeats archive pins before the first remote request.
        for row in manifest["packages"]:
            for kind in ("nupkg", "snupkg"):
                ledger["operations"].append({"id": row["id"], "kind": kind, "archive_sha256": row[kind+"_sha256"],
                                             "state": "not_attempted", "status": None, "failure_category": None})
        ledger["associations"], expected = associations(root, manifest, inspector)
        ledger["inspector_sha256"] = inspector.digest
        ledger["inspector_source_sha256"] = inspector.source_digest
        ledger.update(result="in_progress", failure_category=None)
        reader = transport or HttpTransport()
        if isinstance(reader, HttpTransport):
            reader.deadline = min(reader.deadline, inspector.deadline)
        ledger["feed_index_sha256"] = feed_resources(reader)
        ledger["before"] = reconcile(root, provenance, expected, inspector, reader, ledger["feed_index_sha256"],
                                     observation_mode=ledger["scope"])
        if checkpoint is not None:
            checkpoint(ledger)
        require(not ledger["before"]["blocked"], "reconciliation_blocked")
        if mode == "verify":
            ledger.update(result="verified", failure_category=None, content_verified=ledger["before"]["content_verified"])
            return ledger
        # Admission is checked after ALL reads and immediately before key lookup.
        ledger["admission"] = authorize() if authorize is not None else check_admission(publish=True)
        if authorize is None:
            require(provenance["planner"] == {"source_commit": ledger["admission"]["source_commit"],
                    "run_id": ledger["admission"]["run_id"], "run_attempt": ledger["admission"]["run_attempt"]},
                    "execution_provenance_mismatch")
        key = credential() if credential is not None else os.environ.get(PUBLISH_SECRET)
        require(isinstance(key, str) and bool(key), "publisher_credential_missing")
        package_rows = {row["id"]: row for row in ledger["before"]["packages"]["packages"]}
        symbol_rows = {row["key"]: row for row in ledger["before"]["symbols"]}
        by_package = {row["id"]: row for row in ledger["associations"]}
        manifest_rows = {row["id"]: row for row in manifest["packages"]}
        uploaded_symbol_keys = set()
        for operation in ledger["operations"]:
            row, kind = manifest_rows[operation["id"]], operation["kind"]
            if kind == "nupkg" and package_rows[row["id"]]["classification"] == "matching":
                operation["state"] = "already_matching"
                continue
            symbols = by_package[row["id"]]["associations"]
            if kind == "snupkg" and not symbols:
                operation["state"] = "assembly_free_archive_unverified"
                continue
            if kind == "snupkg":
                # An accepted archive is not PDB content proof. Re-read overlap
                # before another original archive can repeat those keys; stale
                # preflight absence must never cause an avoidable duplicate PUT.
                overlap = uploaded_symbol_keys.intersection(item["key"] for item in symbols)
                if overlap:
                    readback = observe_symbols(reader, {item: expected[item] for item in sorted(overlap)}, inspector)
                    operation["overlap_readback"] = readback
                    symbol_rows.update({item["key"]: item for item in readback})
                    if not all(item["classification"] == "matching" for item in readback):
                        operation["failure_category"] = "symbol_overlap_unverified"
                        raise ExecutorError("symbol_overlap_unverified")
            if kind == "snupkg" and all(symbol_rows[item["key"]]["classification"] == "matching" for item in symbols):
                operation["state"] = "pdbs_already_matching_archive_unverified"
                continue
            require(datetime.now(timezone.utc) < datetime.fromisoformat(EXPIRY.replace("Z", "+00:00")), "candidate_expired")
            data = (root / "artifacts" / row[kind]).read_bytes()
            require(sha(data) == operation["archive_sha256"] and len(data) <= MAX_BYTES, "original_archive_changed")
            operation.update(state="acceptance_unknown", started_at=now())
            ledger["upload_attempted"] = True
            ledger["publication_performed"] = transport is None
            if checkpoint is not None:
                checkpoint(ledger)
            try:
                response = reader.put(kind, data, key)
                operation.update(status=response.status, finished_at=now())
                require(response.complete is True and response.failure_category is None and type(response.status) is int and response.status in {201, 202}, "upload_acceptance_unknown")
                operation["state"] = "accepted_pending_readback"
                if kind == "snupkg":
                    uploaded_symbol_keys.update(item["key"] for item in symbols)
                if checkpoint is not None:
                    checkpoint(ledger)
            except BaseException:
                operation["failure_category"] = "upload_acceptance_unknown"
                raise ExecutorError("upload_acceptance_unknown") from None
        ledger["after"] = reconcile(root, provenance, expected, inspector, reader, ledger["feed_index_sha256"],
                                    observation_mode=ledger["scope"])
        ledger["content_verified"] = ledger["after"]["content_verified"]
        require(ledger["content_verified"], "readback_incomplete")
        ledger.update(result="content_verified", failure_category=None)
    except ExecutorError as error:
        ledger.update(result="failed", failure_category=str(error))
    except BaseException:
        ledger.update(result="failed", failure_category="execution_failed")
    finally:
        ledger["finished_at"] = now()
        if checkpoint is not None:
            try:
                checkpoint(ledger)
            except BaseException:
                ledger.update(result="failed", failure_category="receipt_write_failed")
    return ledger


class SimulatedTransport:
    """Original-byte model; every request is local and no credentials are retained."""
    def __init__(self, root, manifest, expected):
        self.root, self.manifest, self.expected = root, manifest, expected
        self.accepted, self.symbols, self.calls = {}, {}, []
        self.interrupt_next = False
        self.index = json.dumps({"version": "3.0.0", "resources": [
            {"@type": "PackageBaseAddress/3.0.0", "@id": "https://f.feedz.io/elsa-workflows/elsa-3/nuget/v3/packages"},
            {"@type": "PackagePublish/2.0.0", "@id": PACKAGE_PUBLISH},
            {"@type": "SymbolPackagePublish/4.9.0", "@id": SYMBOL_PUBLISH}]}).encode()

    def get(self, url, **kwargs):
        if url == recovery.FEED_INDEX:
            return recovery.ReadResult(200, self.index, True)
        if url.startswith(SYMBOL_PUBLISH + "/"):
            value = self.symbols.get(url[len(SYMBOL_PUBLISH)+1:])
        else:
            value = self.accepted.get(url.rsplit("/", 1)[1])
        return recovery.ReadResult(404 if value is None else 200, value or b"", True)

    def put(self, kind, data, key):
        package = recovery._parse_package(data, recovery._verifier())
        name = f"{package['id']}.{package['version']}.{kind}".lower()
        self.calls.append({"kind": kind, "archive_sha256": sha(data), "name": name})
        if kind == "nupkg":
            require(name not in self.accepted, "simulated_duplicate_upload")
            self.accepted[name] = data
        else:
            row = next(row for row in self.manifest["packages"] if row["id"] == package["id"])
            with zipfile.ZipFile(io.BytesIO(data)) as archive:
                members = {archive.read(frame["pdb"]) for frame in row["assemblies"]}
                for symbol_key, value in self.expected.items():
                    if value["pdb"] in members:
                        self.symbols[symbol_key] = value["pdb"]
        if self.interrupt_next:
            self.interrupt_next = False
            return recovery.ReadResult(None, b"", False, "timeout")
        return recovery.ReadResult(201, b"", True)


def rehearse(root, provenance, inspector):
    manifest, _ = inputs._read_bounded_json(root / "verified-artifacts.json", "Original manifest", 32*1024*1024)
    _, expected = associations(root, manifest, inspector)
    feed = SimulatedTransport(root, manifest, expected)
    feed.interrupt_next = True
    interrupted = run_verified(root, provenance, inspector, mode="publish", transport=feed,
                               authorize=lambda: {"scope": "simulation_only"}, credential=lambda: "synthetic")
    require(interrupted["failure_category"] == "upload_acceptance_unknown" and len(feed.calls) == 1,
            "rehearsal_interruption_failed")
    resumed = run_verified(root, provenance, inspector, mode="publish", transport=feed,
                           authorize=lambda: {"scope": "simulation_only"}, credential=lambda: "synthetic")
    require(resumed["content_verified"] and len(resumed["associations"]) == 225, "rehearsal_incomplete")
    actual = {(row["kind"], row["archive_sha256"]) for row in feed.calls}
    wanted = {(kind, row[kind+"_sha256"]) for row in manifest["packages"] for kind in ("nupkg", "snupkg")
              if kind == "nupkg" or row["assemblies"]}
    require(actual == wanted and len(feed.calls) == len(wanted), "rehearsal_original_bytes_mismatch")
    return {"schema": 1, "result": "passed", "scope": "local_simulation_only", "publication_performed": False,
            "publication_ready": False, "original_provenance": provenance, "simulated_upload_count": len(feed.calls),
            "interrupted": interrupted, "resumed": resumed}


def output_file(path):
    path = Path(os.path.abspath(path))
    require(not any(part.is_symlink() for part in (path, *path.parents)), "unsafe_output")
    # Reserve the receipt before any network/upload. Never overwrite old evidence.
    path.parent.mkdir(parents=True, exist_ok=True)
    return os.fdopen(os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600), "w")


def atomic_receipt(path, receipt, owner):
    """Durable complete snapshots before each mutation, with bounded disk use.

    An interrupted process leaves the last complete snapshot (possibly an
    acceptance_unknown operation), never a partially overwritten JSON document.
    Only this invocation's exclusively reserved output may be replaced.
    """
    descriptor, name = tempfile.mkstemp(prefix=".elsa-executor-", dir=path.parent)
    temporary = Path(name)
    try:
        with os.fdopen(descriptor, "w") as stream:
            json.dump(receipt, stream, sort_keys=True, indent=2)
            stream.write("\n")
            stream.flush()
            os.fsync(stream.fileno())
        require(not path.is_symlink() and path.stat().st_ino == owner[0], "receipt_ownership_changed")
        os.replace(temporary, path)
        owner[0] = path.stat().st_ino
    finally:
        temporary.unlink(missing_ok=True)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("admit", "verify", "rehearse", "publish"))
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--phase", choices=("schedule", "publish"), default="schedule")
    for name in ("inputs", "verified-root", "inspector"):
        parser.add_argument("--" + name, type=Path)
    parser.add_argument("--executor-source")
    parser.add_argument("--executor-run", type=int)
    parser.add_argument("--executor-attempt", type=int)
    args = parser.parse_args(argv)
    receipt = {"schema": 1, "result": "failed", "failure_category": "local_input_invalid", "publication_performed": False,
               "publication_ready": False, "pending_gates": list(PENDING)}
    try:
        if args.mode in {"verify", "rehearse"}:
            require(not any(os.environ.get(name) for name in (PUBLISH_SECRET, METADATA_SECRET)), "verification_credential_forbidden")
        if args.mode != "admit":
            require(all(value is not None for value in (args.inputs, args.verified_root, args.inspector, args.executor_source)), "missing_inputs")
            recovery.validate_cli_paths(args.inputs, args.verified_root, args.output)
            require(args.output.absolute() != args.inspector.absolute(), "unsafe_output")
        with output_file(args.output):
            pass
        owner = [args.output.stat().st_ino]
        def checkpoint(value):
            atomic_receipt(args.output, value, owner)
        checkpoint(receipt)
        try:
            if args.mode == "admit":
                admission = check_admission(publish=args.phase == "publish")
                receipt.update(result=admission["status"], admission=admission, failure_category=None)
            else:
                root, provenance = inputs.prepare_recovery_inputs(args.inputs, args.verified_root,
                    planner_source=args.executor_source, planner_run=args.executor_run, planner_attempt=args.executor_attempt)
                inspector = Inspector(args.inspector)
                if args.mode == "rehearse":
                    receipt = rehearse(root, provenance, inspector)
                else:
                    receipt = run_verified(root, provenance, inspector, mode=args.mode, checkpoint=checkpoint)
        except ExecutorError as error:
            receipt["failure_category"] = str(error)
        except BaseException:
            receipt["failure_category"] = "verification_failed"
        checkpoint(receipt)
    except (OSError, ValueError):
        print(json.dumps({"result": "failed", "failure_category": "receipt_path_invalid",
                          "publication_performed": receipt.get("publication_performed", False),
                          "upload_attempted": receipt.get("upload_attempted", False)}))
        return 1
    print(json.dumps({key: receipt[key] for key in ("result", "publication_performed", "publication_ready")}))
    return 0 if receipt["result"] != "failed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
