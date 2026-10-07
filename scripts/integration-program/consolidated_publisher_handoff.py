#!/usr/bin/env python3
"""Observe reviewed consolidated publisher authority without changing it.

The inventory is a human-reviewed source contract, not a workflow-language
analyzer. Two bounded, fully paged snapshots detect observed races; they do not
lock GitHub or prevent a run starting after the observation window.
"""
from __future__ import annotations

import argparse
import hashlib
from datetime import datetime, timezone
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import tempfile
import time
from typing import Any
from urllib.parse import quote, urlencode

REPOSITORIES = tuple(f"elsa-workflows/elsa-{name}" for name in ("core", "studio", "extensions"))
NONTERMINAL = ("queued", "requested", "waiting", "pending", "in_progress")
RUN_STATES = frozenset((*NONTERMINAL, "completed"))
SHA = re.compile(r"[0-9a-f]{40}\Z")
NAME = re.compile(r"[A-Za-z0-9_.-]{1,200}\Z")
REF = re.compile(r"[A-Za-z0-9_./+@#=,()%-]{1,250}\Z")
BRANCH_POLICY = re.compile(r"[A-Za-z0-9_./+@#=,()%*?\[\]-]{1,250}\Z")
SECRET_NAME = re.compile(r"[A-Za-z_][A-Za-z0-9_]{0,255}\Z")
PENDING_GATES = [
    "protected-artifact-only-core-executor", "reviewed-source-publisher-containment",
    "verified-source-3.8-3.9-maintenance-route", "dedicated-environment-credentials",
    "verified-replacement-maintenance-and-npm-paths-before-credential-retirement",
    "final-fresh-quiescence-and-approved-live-cutover", "exact-candidate-and-compatibility-approval",
    "verified-symbol-upload-and-readback-contract", "intended-reader-feed-access",
    "explicit-publication-approval-and-recovery-boundary",
]


class ObservationError(Exception):
    """Fixed diagnostic codes only; API output is never incorporated."""


def require(condition: bool, code: str) -> None:
    if not condition:
        raise ObservationError(code)


def timestamp(value: Any) -> datetime:
    require(isinstance(value, str), "invalid_timestamp")
    try:
        result = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        raise ObservationError("invalid_timestamp") from None
    require(result.utcoffset() is not None, "invalid_timestamp")
    return result.astimezone(timezone.utc)


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


def iso(value: datetime) -> str:
    return value.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")


def identifier(value: Any) -> int:
    require(type(value) is int and value > 0, "invalid_identity")
    return value


def safe_text(value: Any, pattern: re.Pattern[str] = NAME) -> str:
    require(isinstance(value, str) and pattern.fullmatch(value) is not None, "invalid_metadata")
    require(".." not in value and not value.startswith("/"), "invalid_metadata")
    return value


def source_path(value: Any) -> str:
    require(isinstance(value, str) and REF.fullmatch(value) is not None and "%" not in value, "invalid_source_path")
    parsed = PurePosixPath(value)
    require(not parsed.is_absolute() and ".." not in parsed.parts and parsed.as_posix() == value,
            "invalid_source_path")
    return value


def digest(value: Any) -> str:
    require(isinstance(value, str) and SHA.fullmatch(value) is not None, "invalid_digest")
    return value


def validate_inventory(document: Any, now: datetime) -> dict[str, Any]:
    require(isinstance(document, dict) and document.get("schema_version") == 1, "invalid_inventory")
    review = document.get("review", {})
    require(isinstance(review, dict), "invalid_inventory_review")
    require(isinstance(review.get("scope"), str) and bool(review["scope"])
            and isinstance(review.get("unresolved_paths"), list), "inventory_review_incomplete")
    reviewed_at, valid_until = timestamp(review.get("reviewed_at")), timestamp(review.get("valid_until"))
    require(reviewed_at <= now <= valid_until, "inventory_review_stale")
    repositories = document.get("repositories")
    require(isinstance(repositories, list) and len(repositories) == len(REPOSITORIES), "invalid_repositories")
    require({entry.get("repository") for entry in repositories if isinstance(entry, dict)} == set(REPOSITORIES),
            "invalid_repositories")
    for entry in repositories:
        workflows = entry.get("workflows")
        require(isinstance(workflows, list) and bool(workflows), "invalid_workflow_inventory")
        seen_ids, seen_paths = set(), set()
        for workflow in workflows:
            require(isinstance(workflow, dict), "invalid_workflow_inventory")
            workflow_id, path = identifier(workflow.get("id")), source_path(workflow.get("path"))
            require(workflow_id not in seen_ids and path not in seen_paths, "duplicate_workflow_inventory")
            seen_ids.add(workflow_id)
            seen_paths.add(path)
            kind = workflow.get("identity_kind", "repository")
            require(kind in {"repository", "platform-managed", "retired", "unresolved"}, "invalid_workflow_kind")
            require(kind != "repository" or (path.startswith(".github/workflows/")
                    and path.endswith((".yml", ".yaml"))), "invalid_workflow_path")
            require(workflow.get("role") in {"publisher", "supporting", "unknown"}, "invalid_workflow_role")
            if workflow.get("source_ref") is not None or workflow.get("source_blob_sha") is not None:
                digest(workflow.get("source_ref"))
                digest(workflow.get("source_blob_sha"))
            require(isinstance(workflow.get("trigger_paths"), list) and bool(workflow["trigger_paths"])
                    and all(item in {"push", "release", "workflow_dispatch", "workflow_call", "rerun", "other"}
                            for item in workflow["trigger_paths"]), "invalid_trigger_review")
            require(isinstance(workflow.get("historical_behavior"), str) and bool(workflow["historical_behavior"]),
                    "invalid_historical_review")
        refs = entry.get("refs")
        require(isinstance(refs, list) and bool(refs), "invalid_ref_inventory")
        seen_refs = set()
        for ref in refs:
            require(isinstance(ref, dict), "invalid_ref_inventory")
            name = safe_text(ref.get("ref"), REF)
            require(name.startswith(("heads/", "tags/")) and name not in seen_refs, "invalid_ref_inventory")
            seen_refs.add(name)
            digest(ref.get("expected_sha"))
        files = entry.get("source_files", [])
        require(isinstance(files, list), "invalid_source_inventory")
        for source in files:
            require(isinstance(source, dict), "invalid_source_inventory")
            source_path(source.get("path"))
            digest(source.get("ref"))
            digest(source.get("blob_sha"))
        for source in [*workflows, *files]:
            observations = source.get("observed_refs", [])
            require(isinstance(observations, list), "invalid_source_inventory")
            for observation in observations:
                require(isinstance(observation, dict) and observation.get("ref") in seen_refs,
                        "invalid_source_ref_inventory")
                digest(observation.get("blob_sha"))
        environments = entry.get("required_environments")
        require(isinstance(environments, list) and bool(environments), "invalid_environment_inventory")
        seen_environments = set()
        for environment in environments:
            require(isinstance(environment, dict), "invalid_environment_inventory")
            name = safe_text(environment.get("name"))
            require(name not in seen_environments, "duplicate_environment_inventory")
            seen_environments.add(name)
            require(all(type(environment.get(key)) is bool for key in (
                "required_reviewers", "prevent_self_review", "prevent_admin_bypass", "required_branch_policy")),
                "invalid_environment_requirements")
        names = entry.get("credential_names")
        require(isinstance(names, list) and bool(names), "invalid_credential_inventory")
        for name in names:
            safe_text(name, SECRET_NAME)
    return document


class GhApi:
    """Strict GET-only gh transport with request, response and time bounds."""

    def __init__(self, *, timeout_seconds: int = 20, max_requests: int = 600,
                 max_response_bytes: int = 4_000_000, max_duration_seconds: int = 600):
        self.timeout_seconds = timeout_seconds
        self.max_requests = max_requests
        self.max_response_bytes = max_response_bytes
        self.request_count = 0
        self.max_duration_seconds = max_duration_seconds
        self.deadline = time.monotonic() + max_duration_seconds

    def limit_window(self, seconds: int) -> None:
        self.deadline = min(self.deadline, time.monotonic() + seconds)

    def get(self, endpoint: str) -> Any:
        require(isinstance(endpoint, str) and re.fullmatch(
            r"(?:repos/elsa-workflows/elsa-(?:core|studio|extensions)|orgs/elsa-workflows)/"
            r"[A-Za-z0-9_./%+-]+(?:\?[A-Za-z0-9_=&%+.-]+)?", endpoint) is not None
            and ".." not in endpoint, "unsafe_endpoint")
        require(self.request_count < self.max_requests, "request_limit")
        remaining = self.deadline - time.monotonic()
        require(remaining > 0, "observation_window_exceeded")
        self.request_count += 1
        try:
            with tempfile.TemporaryFile() as output:
                process = subprocess.run(
                    ["gh", "api", "--method", "GET", endpoint], stdout=output,
                    stderr=subprocess.DEVNULL, timeout=min(self.timeout_seconds, remaining), check=False,
                )
                require(process.returncode == 0, "transport_failed")
                output.seek(0)
                body = output.read(self.max_response_bytes + 1)
                require(len(body) <= self.max_response_bytes, "response_limit")
        except subprocess.TimeoutExpired:
            raise ObservationError("transport_timeout") from None
        except OSError:
            raise ObservationError("transport_unavailable") from None
        try:
            return json.loads(body)
        except (ValueError, UnicodeDecodeError, RecursionError):
            raise ObservationError("invalid_json") from None


class Collector:
    def __init__(self, transport: Any, *, max_pages: int = 10, page_size: int = 100,
                 credential_names: set[str] | None = None):
        require(type(max_pages) is int and 1 <= max_pages <= 100, "invalid_page_limit")
        require(type(page_size) is int and 1 <= page_size <= 100, "invalid_page_size")
        self.transport = transport
        self.credential_names = credential_names or set()
        self.max_pages = max_pages
        self.page_size = page_size
        self.pagination: list[dict[str, Any]] = []
        self.diagnostics: list[dict[str, str]] = []
        self.authority_gaps: list[dict[str, str]] = []
        self.inventory_gaps: list[dict[str, str]] = []

    def problem(self, repository: str, scope: str, code: str) -> None:
        self.diagnostics.append({"repository": repository, "scope": scope, "code": code})

    def gap(self, repository: str, scope: str, code: str, *, inventory: bool = False) -> None:
        gap = {"repository": repository, "scope": scope, "code": code}
        self.authority_gaps.append(gap)
        if inventory:
            self.inventory_gaps.append(gap)

    def attempt(self, repository: str, scope: str, operation: Any, default: Any = None) -> Any:
        try:
            return operation()
        except ObservationError as error:
            self.problem(repository, scope, str(error))
            return default
        except (KeyError, TypeError, ValueError, IndexError):
            self.problem(repository, scope, "invalid_response_schema")
            return default

    def pages(self, repository: str, scope: str, endpoint: str, field: str,
              identity: str = "id", *, actions_runs: bool = False) -> list[dict[str, Any]]:
        receipt = {"repository": repository, "scope": scope, "pages": 0,
                   "total_count": None, "observed_count": 0, "complete": False}
        self.pagination.append(receipt)
        rows, seen, expected = [], set(), None
        for page in range(1, self.max_pages + 1):
            separator = "&" if "?" in endpoint else "?"
            response = self.transport.get(f"{endpoint}{separator}per_page={self.page_size}&page={page}")
            require(isinstance(response, dict), "invalid_page")
            count, items = response.get("total_count"), response.get(field)
            require(type(count) is int and count >= 0 and isinstance(items, list)
                    and len(items) <= self.page_size, "invalid_page")
            receipt["pages"] = page
            receipt["total_count"] = count
            require(not actions_runs or count < 1000, "actions_search_limit")
            require(count <= self.max_pages * self.page_size, "pagination_limit")
            require(expected is None or count == expected, "pagination_count_drift")
            expected = count
            for item in items:
                require(isinstance(item, dict), "invalid_page_item")
                key = item.get(identity)
                require(type(key) in {str, int} and key not in seen, "pagination_duplicate_or_invalid_identity")
                seen.add(key)
                rows.append(item)
            receipt["observed_count"] = len(rows)
            require(len(rows) <= count, "pagination_count_drift")
            if len(items) < self.page_size:
                require(len(rows) == count, "pagination_incomplete")
                receipt["complete"] = True
                return rows
        raise ObservationError("pagination_limit")

    def ref_pages(self, repository: str, kind: str, phase: str) -> list[dict[str, str]]:
        receipt = {"repository": repository, "scope": f"{kind}-{phase}", "pages": 0,
                   "total_count": None, "observed_count": 0, "complete": False}
        self.pagination.append(receipt)
        rows, seen = [], set()
        for page in range(1, self.max_pages + 1):
            response = self.transport.get(f"repos/{repository}/{kind}?per_page={self.page_size}&page={page}")
            require(isinstance(response, list) and len(response) <= self.page_size, "invalid_ref_page")
            receipt["pages"] = page
            for item in response:
                require(isinstance(item, dict) and isinstance(item.get("commit"), dict), "invalid_ref_page")
                name = safe_text(item.get("name"), REF)
                require(name not in seen, "pagination_duplicate_or_invalid_identity")
                seen.add(name)
                rows.append({"ref": f"{'heads' if kind == 'branches' else 'tags'}/{name}",
                             "observed_sha": digest(item["commit"].get("sha"))})
            receipt["observed_count"] = len(rows)
            if len(response) < self.page_size:
                receipt["complete"] = True
                return rows
        raise ObservationError("pagination_limit")

    def refs(self, entry: dict[str, Any], phase: str) -> list[dict[str, str]]:
        repository = entry["repository"]
        result = []
        for kind in ("branches", "tags"):
            result.extend(self.attempt(repository, f"refs-{phase}",
                lambda kind=kind: self.ref_pages(repository, kind, phase), []))
        by_name = {item["ref"]: item for item in result}
        expected_names = {item["ref"] for item in entry["refs"]}
        for expected in entry["refs"]:
            observed = by_name.get(expected["ref"])
            if observed is None:
                self.gap(repository, f"refs-{phase}", "required_source_ref_missing", inventory=True)
            elif observed["observed_sha"] != expected["expected_sha"]:
                self.gap(repository, f"refs-{phase}", "ref_changed_from_inventory", inventory=True)
        if by_name.keys() - expected_names:
            self.gap(repository, f"refs-{phase}", "additional_source_refs_unreviewed", inventory=True)
        return sorted(result, key=lambda item: item["ref"])

    def sources(self, entry: dict[str, Any]) -> list[dict[str, Any]]:
        repository, result = entry["repository"], []
        for source in [*entry["workflows"], *entry.get("source_files", [])]:
            if source.get("role") == "unknown":
                self.gap(repository, "source-identities", "workflow_authority_role_unknown", inventory=True)
            if source.get("identity_kind", "repository") != "repository":
                self.gap(repository, "source-identities", "nonrepository_source_authority_unverified", inventory=True)
            if not source.get("observed_refs"):
                self.gap(repository, "source-identities", "mutable_source_refs_unverified", inventory=True)
            if not source.get("source_ref", source.get("ref")):
                self.gap(repository, "source-identities", "immutable_source_unpinned", inventory=True)
                continue
            checks = [{"ref": source.get("source_ref", source.get("ref")),
                       "blob_sha": source.get("source_blob_sha", source.get("blob_sha"))},
                      *source.get("observed_refs", [])]
            for check in checks:
                def observe(source=source, check=check):
                    response = self.transport.get(
                        f"repos/{repository}/contents/{source['path']}?{urlencode({'ref': check['ref']})}")
                    require(isinstance(response, dict) and response.get("type") == "file", "invalid_source_file")
                    observed = digest(response.get("sha"))
                    return {"path": source["path"], "ref": check["ref"], "expected_blob_sha": check["blob_sha"],
                            "observed_blob_sha": observed, "matches": observed == check["blob_sha"]}
                observed = self.attempt(repository, "source-identities", observe)
                if observed:
                    result.append(observed)
                    if not observed["matches"]:
                        self.gap(repository, "source-identities", "source_changed_from_inventory", inventory=True)
        return result

    def workflows(self, entry: dict[str, Any], phase: str) -> list[dict[str, Any]]:
        repository = entry["repository"]
        def observe():
            rows = self.pages(repository, f"workflows-{phase}", f"repos/{repository}/actions/workflows", "workflows")
            result = []
            for item in rows:
                state = item.get("state")
                require(state in {"active", "deleted", "disabled_fork", "disabled_inactivity", "disabled_manually"},
                        "unknown_workflow_state")
                result.append({"id": identifier(item.get("id")), "path": source_path(item.get("path")), "state": state})
            expected = {(workflow["id"], workflow["path"]) for workflow in entry["workflows"]}
            if {(workflow["id"], workflow["path"]) for workflow in result} != expected:
                self.gap(repository, f"workflows-{phase}", "workflow_inventory_changed", inventory=True)
            return sorted(result, key=lambda item: item["id"])
        return self.attempt(repository, f"workflows-{phase}", observe, [])

    def runs(self, entry: dict[str, Any], phase: str) -> list[dict[str, Any]]:
        repository, result, seen = entry["repository"], [], set()
        workflow_ids = {item["id"]: item["path"] for item in entry["workflows"]}
        for state in NONTERMINAL:
            def observe(state=state):
                rows = self.pages(repository, f"runs-{phase}-{state}",
                                  f"repos/{repository}/actions/runs?status={state}", "workflow_runs", actions_runs=True)
                return [self.run(item, workflow_ids, expected_state=state) for item in rows]
            rows = self.attempt(repository, f"runs-{phase}-{state}", observe, [])
            for row in rows:
                if row["id"] in seen:
                    self.problem(repository, f"runs-{phase}", "run_moved_between_status_pages")
                seen.add(row["id"])
                result.append(row)
        return sorted(result, key=lambda item: item["id"])

    @staticmethod
    def run(item: dict[str, Any], workflow_ids: dict[int, str], expected_state: str | None = None) -> dict[str, Any]:
        require(isinstance(item, dict), "invalid_run")
        state = item.get("status")
        require(state in RUN_STATES and (expected_state is None or state == expected_state), "unknown_or_changed_run_state")
        workflow_id = identifier(item.get("workflow_id"))
        require(workflow_id in workflow_ids, "unknown_run_workflow")
        event = item.get("event")
        require(isinstance(event, str) and NAME.fullmatch(event) is not None, "invalid_run_event")
        path = source_path(item.get("path"))
        require(path == workflow_ids[workflow_id], "unknown_run_workflow_path")
        return {"id": identifier(item.get("id")), "run_attempt": identifier(item.get("run_attempt")),
                "workflow_id": workflow_id, "path": path, "head_sha": digest(item.get("head_sha")),
                "head_branch": None if item.get("head_branch") is None else safe_text(item["head_branch"], REF),
                "event": event, "status": state, "created_at": iso(timestamp(item.get("created_at"))),
                "updated_at": iso(timestamp(item.get("updated_at")))}

    def jobs(self, entry: dict[str, Any], runs: list[dict[str, Any]]) -> list[dict[str, Any]]:
        repository, result = entry["repository"], []
        workflow_ids = {item["id"]: item["path"] for item in entry["workflows"]}
        for run in runs:
            def observe(run=run):
                rows = self.pages(repository, "run-jobs", f"repos/{repository}/actions/runs/{run['id']}"
                                  f"/attempts/{run['run_attempt']}/jobs", "jobs")
                jobs = []
                for item in rows:
                    require(item.get("status") in RUN_STATES, "unknown_job_state")
                    require(item.get("run_id") == run["id"] and item.get("run_attempt") == run["run_attempt"],
                            "job_run_identity_changed")
                    jobs.append({"id": identifier(item.get("id")), "run_id": run["id"],
                                 "run_attempt": run["run_attempt"], "status": item["status"],
                                 "head_sha": digest(item.get("head_sha"))})
                    require(jobs[-1]["head_sha"] == run["head_sha"], "job_head_changed")
                current = self.run(self.transport.get(f"repos/{repository}/actions/runs/{run['id']}"), workflow_ids)
                require(current == run, "run_changed_during_job_observation")
                return jobs
            result.extend(self.attempt(repository, "run-jobs", observe, []))
        return sorted(result, key=lambda item: item["id"])

    def relevant_secret(self, repository: str, scope: str, value: Any) -> str | None:
        name = safe_text(value, SECRET_NAME)
        if name in self.credential_names:
            return name
        if "FEEDZ" in name.upper() or "FEEDS" in name.upper():
            self.gap(repository, scope, "unexpected_feed_credential_name")
            return name
        return None

    def secrets(self, repository: str, scope: str, endpoint: str) -> list[dict[str, Any]]:
        def observe():
            rows = self.pages(repository, scope, endpoint, "secrets", identity="name")
            result = []
            for item in rows:
                name = self.relevant_secret(repository, scope, item.get("name"))
                if name is not None:
                    result.append({"name": name, "scope": scope, "credential_permissions": "unknown"})
            return result
        return self.attempt(repository, scope, observe, [])

    def environments(self, entry: dict[str, Any]) -> list[dict[str, Any]]:
        repository = entry["repository"]
        rows = self.attempt(repository, "environments", lambda: self.pages(
            repository, "environments", f"repos/{repository}/environments", "environments"), [])
        result = []
        requirements = {item["name"]: item for item in entry["required_environments"]}
        listed_names = {row.get("name") for row in rows if isinstance(row, dict) and isinstance(row.get("name"), str)}
        for row in rows:
            def observe(row=row):
                name = safe_text(row.get("name"))
                response = self.transport.get(f"repos/{repository}/environments/{quote(name, safe='')}")
                require(isinstance(response, dict) and response.get("name") == name, "invalid_environment")
                environment_id = identifier(response.get("id"))
                require(row.get("id") == environment_id, "environment_identity_changed")
                rules = response.get("protection_rules")
                require(isinstance(rules, list), "unknown_environment_protections")
                reviewers, prevent_self_review = [], None
                for rule in rules:
                    require(isinstance(rule, dict), "unknown_environment_protections")
                    kind = rule.get("type")
                    require(kind in {"required_reviewers", "wait_timer", "branch_policy"},
                            "unknown_environment_protection_rule")
                    if kind == "required_reviewers":
                        require(isinstance(rule.get("reviewers"), list)
                                and type(rule.get("prevent_self_review")) is bool, "unknown_environment_reviewers")
                        prevent_self_review = rule["prevent_self_review"]
                        for reviewer in rule["reviewers"]:
                            require(isinstance(reviewer, dict) and reviewer.get("type") in {"User", "Team"}
                                    and isinstance(reviewer.get("reviewer"), dict), "unknown_environment_reviewers")
                            reviewers.append({"type": reviewer["type"], "id": identifier(reviewer["reviewer"].get("id"))})
                bypass = response.get("can_admins_bypass")
                if type(bypass) is not bool:
                    bypass = None
                    self.gap(repository, "environment-protections", "unknown_admin_bypass")
                policy = response.get("deployment_branch_policy")
                branch_policy = None
                selected = []
                if policy is not None:
                    require(isinstance(policy, dict) and all(type(policy.get(key)) is bool
                            for key in ("protected_branches", "custom_branch_policies")), "unknown_branch_policy")
                    require(policy["protected_branches"] != policy["custom_branch_policies"], "unknown_branch_policy")
                    branch_policy = {key: policy[key] for key in ("protected_branches", "custom_branch_policies")}
                    if policy["custom_branch_policies"]:
                        selected_rows = self.pages(repository, "environment-branch-policies",
                            f"repos/{repository}/environments/{quote(name, safe='')}/deployment-branch-policies",
                            "branch_policies")
                        for selected_row in selected_rows:
                            require(selected_row.get("type") in {"branch", "tag"}, "unknown_branch_policy")
                            selected.append({"id": identifier(selected_row.get("id")),
                                             "name": safe_text(selected_row.get("name"), BRANCH_POLICY),
                                             "type": selected_row["type"]})
                secrets = self.secrets(repository, "environment-secret-names",
                                      f"repos/{repository}/environments/{quote(name, safe='')}/secrets")
                return {"id": environment_id, "name": name, "reviewers": reviewers,
                        "prevent_self_review": prevent_self_review, "can_admins_bypass": bypass,
                        "deployment_branch_policy": branch_policy, "selected_branch_policies": selected,
                        "secrets": secrets, "approval_observed": False}
            observed = self.attempt(repository, "environment-metadata", observe)
            if observed is None:
                continue
            result.append(observed)
            required = requirements.get(observed["name"])
            if required and ((required["required_reviewers"] and not observed["reviewers"])
                    or (required["prevent_self_review"] and observed["prevent_self_review"] is not True)
                    or (required["prevent_admin_bypass"] and observed["can_admins_bypass"] is not False)
                    or (required["required_branch_policy"] and (not observed["deployment_branch_policy"]
                        or (observed["deployment_branch_policy"]["custom_branch_policies"]
                            and not observed["selected_branch_policies"])))):
                self.gap(repository, "environment-protections", "required_environment_unprotected")
        listing_complete = not any(item["repository"] == repository and item["scope"] == "environments"
                                   for item in self.diagnostics)
        if listing_complete:
            for missing in sorted(requirements.keys() - listed_names):
                self.gap(repository, "environment-protections", "required_environment_missing")
        return result

    def org_secrets(self) -> list[dict[str, Any]]:
        def observe():
            rows = self.pages("elsa-workflows", "organization-secret-names", "orgs/elsa-workflows/actions/secrets",
                              "secrets", identity="name")
            result = []
            for item in rows:
                name = self.relevant_secret("elsa-workflows", "organization-secret-names", item.get("name"))
                if name is None:
                    continue
                visibility = item.get("visibility")
                require(visibility in {"all", "private", "selected"}, "unknown_org_secret_visibility")
                selected = []
                if visibility == "selected":
                    selected_rows = self.pages("elsa-workflows", "organization-secret-selected-repositories",
                        f"orgs/elsa-workflows/actions/secrets/{name}/repositories", "repositories")
                    for selected_row in selected_rows:
                        # Retain only the three relevant repositories, while counting all pages.
                        require(isinstance(selected_row.get("full_name"), str), "invalid_selected_repository")
                        if selected_row["full_name"] in REPOSITORIES:
                            selected.append(selected_row["full_name"])
                result.append({"name": name, "scope": "organization", "visibility": visibility,
                               "selected_repositories": sorted(selected), "credential_permissions": "unknown"})
            return result
        return self.attempt("elsa-workflows", "organization-secret-names", observe, [])


def preflight(inventory: Any, transport: Any, *, now: Any = utc_now,
              max_pages: int = 10, page_size: int = 100, max_window_seconds: int = 600) -> dict[str, Any]:
    started = now()
    result: dict[str, Any] = {
        "schema_version": 1, "inventory_sha256": None, "kind": "consolidated-publisher-handoff-read-only-observation",
        "observation_started_at": iso(started), "observation_finished_at": None,
        "publication_performed": False, "live_publisher_changed": False, "publication_ready": False,
        "inventory_verified": False, "observation_complete": False, "snapshot_quiescent": False,
        "quiescence": {"verified": False, "nonterminal_run_count": None, "cross_repository_lock": False,
                       "new_runs_prevented": False, "covered_statuses": list(NONTERMINAL)},
        "credential_permissions_verified": False, "pending_gates": list(PENDING_GATES),
        "repositories": [], "organization_secrets": [], "pagination": [], "diagnostics": [], "authority_gaps": [],
    }
    try:
        validate_inventory(inventory, started)
        require(type(max_window_seconds) is int and 1 <= max_window_seconds <= 3600, "invalid_window_limit")
        if callable(getattr(transport, "limit_window", None)):
            transport.limit_window(max_window_seconds)
        result["inventory_sha256"] = hashlib.sha256(json.dumps(
            inventory, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()
        collector = Collector(transport, max_pages=max_pages, page_size=page_size,
                              credential_names={name for entry in inventory["repositories"]
                                                for name in entry["credential_names"]})
    except (ObservationError, KeyError, TypeError, ValueError, RecursionError) as error:
        code = str(error) if isinstance(error, ObservationError) else "invalid_inventory"
        result["diagnostics"] = [{"scope": "inventory", "code": code}]
        result["observation_finished_at"] = iso(now())
        return result
    if inventory["review"]["unresolved_paths"]:
        collector.gap("elsa-workflows", "inventory", "reviewed_scope_has_unresolved_paths", inventory=True)
    entries = sorted(inventory["repositories"], key=lambda entry: entry["repository"])
    observations = {}
    # All repositories are observed before the second sweep. This detects
    # changes between repositories as well as changes within a status query.
    for entry in entries:
        repository = entry["repository"]
        observations[repository] = {
            "repository": repository, "refs_before": collector.refs(entry, "before"),
            "workflows_before": collector.workflows(entry, "before"), "sources": collector.sources(entry),
            "runs_before": collector.runs(entry, "before"), "environments": collector.environments(entry),
            "repository_secrets": collector.secrets(repository, "repository-secret-names",
                                                    f"repos/{repository}/actions/secrets"),
            "available_organization_secrets": collector.secrets(repository, "available-organization-secret-names",
                                                    f"repos/{repository}/actions/organization-secrets"),
        }
    result["organization_secrets"] = collector.org_secrets()
    for entry in entries:
        repository, observation = entry["repository"], observations[entry["repository"]]
        observation["runs_after"] = collector.runs(entry, "after")
        observation["jobs"] = collector.jobs(entry, observation["runs_after"])
        observation["refs_after"] = collector.refs(entry, "after")
        observation["workflows_after"] = collector.workflows(entry, "after")
        if observation["runs_before"] != observation["runs_after"]:
            collector.problem(repository, "quiescence", "run_snapshot_drift")
        if observation["refs_before"] != observation["refs_after"]:
            collector.problem(repository, "refs", "ref_snapshot_drift")
        if observation["runs_after"]:
            collector.gap(repository, "quiescence", "active_runs_observed")
        if observation["workflows_before"] != observation["workflows_after"]:
            collector.problem(repository, "workflows", "workflow_snapshot_drift")
        for name in sorted(set(entry["credential_names"])):
            repo_metadata_known = not any(item["repository"] == repository and item["scope"] == "repository-secret-names"
                                          for item in collector.diagnostics)
            org_metadata_known = not any(item["scope"] == "organization-secret-names"
                                         for item in collector.diagnostics)
            available_metadata_known = not any(item["repository"] == repository
                and item["scope"] == "available-organization-secret-names" for item in collector.diagnostics)
            available = (any(item["name"] == name for item in observation["available_organization_secrets"])
                         if available_metadata_known else None)
            repo_present = any(item["name"] == name for item in observation["repository_secrets"]) if repo_metadata_known else None
            org_matches = [item for item in result["organization_secrets"] if item["name"] == name]
            environment_names = [environment["name"] for environment in observation["environments"]
                                 if any(item["name"] == name for item in environment["secrets"])]
            observation.setdefault("credential_boundaries", []).append({
                "name": name, "repository_name_present": repo_present,
                "organization_name_present": True if available else bool(org_matches) if org_metadata_known else None,
                "organization_available_to_repository": available,
                "environment_names": environment_names,
                "environment_metadata_complete": not any(item["repository"] == repository and item["scope"] in {
                    "environments", "environment-metadata", "environment-secret-names"} for item in collector.diagnostics),
                "organization_access": [
                    "included" if item["visibility"] == "all" or repository in item["selected_repositories"]
                    else "unknown-repository-visibility" if item["visibility"] == "private" else "excluded"
                    for item in org_matches],
                "repository_or_org_fallback_possible": True if repo_present or available or any(
                    item["visibility"] in {"all", "private"} or repository in item["selected_repositories"]
                    for item in org_matches) else False if repo_metadata_known and org_metadata_known
                        and available_metadata_known else None,
                "encoded_credential_scope": "unknown", "credential_permissions": "unknown",
            })
            boundary = observation["credential_boundaries"][-1]
            if boundary["repository_or_org_fallback_possible"]:
                collector.gap(repository, "credential-boundaries", "repository_or_org_credential_authority_unretired")
            if not boundary["environment_metadata_complete"] or not environment_names:
                collector.gap(repository, "credential-boundaries", "dedicated_environment_credential_unverified")
    finished = now()
    if (finished - started).total_seconds() < 0 or (finished - started).total_seconds() > max_window_seconds:
        collector.problem("elsa-workflows", "observation-window", "observation_window_exceeded")
    if finished > timestamp(inventory["review"]["valid_until"]):
        collector.problem("elsa-workflows", "inventory", "inventory_expired_during_observation")
    result["transport"] = {"method": "GET", "request_count": getattr(transport, "request_count", None),
                           "request_limit": getattr(transport, "max_requests", None),
                           "request_timeout_seconds": getattr(transport, "timeout_seconds", None),
                           "window_limit_seconds": max_window_seconds}
    result.update({"observation_finished_at": iso(finished), "repositories": list(observations.values()),
                   "pagination": collector.pagination, "diagnostics": collector.diagnostics,
                   "authority_gaps": collector.authority_gaps})
    inventory_scopes = {"source-identities", "refs-before", "refs-after", "refs", "workflows-before", "workflows-after", "workflows", "inventory"}
    result["inventory_verified"] = not collector.inventory_gaps and not any(
        item["scope"] in inventory_scopes for item in collector.diagnostics)
    result["observation_complete"] = not collector.diagnostics
    count = sum(len(item["runs_after"]) for item in observations.values())
    result["snapshot_quiescent"] = result["observation_complete"] and count == 0
    result["quiescence"].update({"nonterminal_run_count": count,
                                  "verified": result["snapshot_quiescent"] and result["inventory_verified"]})
    return result


def implementation_identity() -> dict[str, Any]:
    script = Path(__file__).resolve()
    identity: dict[str, Any] = {"script_sha256": hashlib.sha256(script.read_bytes()).hexdigest(),
                                "git_head": None, "source_dirty": None}
    try:
        head = subprocess.run(["git", "-C", str(script.parents[2]), "rev-parse", "HEAD"],
                              stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, timeout=5, check=False)
        if head.returncode == 0:
            value = head.stdout.decode("ascii").strip()
            if SHA.fullmatch(value):
                identity["git_head"] = value
        status = subprocess.run(["git", "-C", str(script.parents[2]), "status", "--porcelain", "--", str(script)],
                                stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, timeout=5, check=False)
        if status.returncode == 0:
            identity["source_dirty"] = bool(status.stdout)
    except (OSError, subprocess.TimeoutExpired, UnicodeDecodeError):
        pass
    return identity


def unique_json_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result = {}
    for key, value in pairs:
        require(key not in result, "duplicate_inventory_key")
        result[key] = value
    return result


def open_receipt_output(output: Path, inventory: Path) -> Any:
    # Reject symlink ancestors as well as the final component. Use a canonical
    # path (for example /private/tmp on macOS) when a system alias is a symlink.
    target = output.absolute()
    require(target.resolve() != inventory.resolve(), "output_overlaps_inventory")
    require(not target.exists() and not target.is_symlink(), "output_already_exists")
    require(target.parent.is_dir(), "output_parent_missing")
    require(not any(parent.is_symlink() for parent in target.parents), "output_symlink_ancestor")
    descriptor = os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    return os.fdopen(descriptor, "w", encoding="utf-8")


def input_failure(code: str) -> dict[str, Any]:
    return {"schema_version": 1, "publication_performed": False, "live_publisher_changed": False,
            "publication_ready": False, "observation_complete": False,
            "diagnostics": [{"scope": "input", "code": code}], "pending_gates": list(PENDING_GATES)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inventory", type=Path, required=True)
    parser.add_argument("--output", type=Path, help="New receipt file; existing files and symlinks are rejected")
    parser.add_argument("--max-pages", type=int, default=10)
    parser.add_argument("--max-requests", type=int, default=600)
    parser.add_argument("--timeout-seconds", type=int, default=20)
    args = parser.parse_args()
    output = None
    if args.output:
        try:
            output = open_receipt_output(args.output, args.inventory)
        except (OSError, ObservationError, RuntimeError) as error:
            code = str(error) if isinstance(error, ObservationError) else "receipt_create_failed"
            print(json.dumps(input_failure(code)))
            return 1
    try:
        with args.inventory.open("rb") as source:
            raw = source.read(1_000_001)
        require(len(raw) <= 1_000_000, "inventory_size_limit")
        inventory = json.loads(raw, object_pairs_hook=unique_json_object)
        require(1 <= args.max_requests <= 2000 and 1 <= args.timeout_seconds <= 60, "invalid_transport_limits")
        result = preflight(inventory, GhApi(timeout_seconds=args.timeout_seconds, max_requests=args.max_requests),
                           max_pages=args.max_pages)
        result["inventory_file_sha256"] = hashlib.sha256(raw).hexdigest()
        result["implementation"] = implementation_identity()
    except (OSError, ValueError, UnicodeDecodeError, ObservationError, RecursionError) as error:
        code = str(error) if isinstance(error, ObservationError) else "input_or_limits_invalid"
        result = input_failure(code)
    encoded = json.dumps(result, indent=2, sort_keys=True) + "\n"
    if output:
        try:
            with output:
                output.write(encoded)
        except OSError:
            print(json.dumps(input_failure("receipt_write_failed")))
            return 1
    else:
        print(encoded, end="")
    return 0 if result.get("observation_complete") and result.get("quiescence", {}).get("verified") else 1


if __name__ == "__main__":
    raise SystemExit(main())
