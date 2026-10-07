import copy
from datetime import datetime, timedelta, timezone
import json
import io
from contextlib import redirect_stdout
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
from urllib.parse import parse_qs, urlsplit

sys.path.insert(0, str(Path(__file__).resolve().parent))
from consolidated_publisher_handoff import (
    Collector, GhApi, NONTERMINAL, ObservationError, REPOSITORIES, main, preflight,
)

NOW = datetime(2026, 10, 7, 12, tzinfo=timezone.utc)
SHA = "a" * 40
BLOB = "b" * 40


def inventory():
    return {
        "schema_version": 1,
        "review": {"reviewed_at": "2026-10-07T11:00:00Z", "valid_until": "2026-10-07T13:00:00Z",
                   "scope": "Synthetic complete reviewed source paths", "unresolved_paths": []},
        "repositories": [{
            "repository": repository,
            "workflows": [{"id": index + 1, "path": ".github/workflows/publish.yml", "role": "publisher",
                           "source_ref": SHA, "source_blob_sha": BLOB,
                           "trigger_paths": ["push", "release", "workflow_dispatch", "workflow_call", "rerun"],
                           "historical_behavior": "Synthetic reviewed historic rerun and maintenance boundaries",
                           "observed_refs": [{"ref": "heads/main", "blob_sha": BLOB}]}],
            "refs": [{"ref": "heads/main", "expected_sha": SHA}], "source_files": [],
            "required_environments": [{"name": "feedz-publish", "required_reviewers": True,
                "prevent_self_review": True, "prevent_admin_bypass": True, "required_branch_policy": True}],
            "credential_names": ["FEEDZ_API_KEY", "FEEDZ_BASE64_TOKEN"],
        } for index, repository in enumerate(REPOSITORIES)],
    }


def run(run_id=123, *, status="queued", workflow_id=1):
    return {"id": run_id, "run_attempt": 1, "workflow_id": workflow_id,
            "path": ".github/workflows/publish.yml", "head_sha": SHA, "head_branch": "main",
            "event": "workflow_dispatch", "status": status,
            "created_at": "2026-10-07T10:00:00Z", "updated_at": "2026-10-07T10:01:00Z"}


def environment():
    return {"id": 100, "name": "feedz-publish", "can_admins_bypass": False,
            "protection_rules": [{"type": "required_reviewers", "prevent_self_review": True,
                                 "reviewers": [{"type": "User", "reviewer": {"id": 99}}]}],
            "deployment_branch_policy": {"protected_branches": True, "custom_branch_policies": False}}


class FakeApi:
    """Fixture routes preserve real REST page shapes and stateful races."""
    def __init__(self):
        self.calls = []
        self.runs = {state: [] for state in NONTERMINAL}
        self.job_rows = []
        self.environment = environment()
        self.repo_secrets = []
        self.org_secrets = []
        self.available_org_secrets = []
        self.environment_secrets = [{"name": "FEEDZ_API_KEY"}, {"name": "FEEDZ_BASE64_TOKEN"}]
        self.overrides = {}
        self.counts = {}
        self.selected_repositories = [{"id": 55, "full_name": REPOSITORIES[0]}]

    def get(self, endpoint):
        self.calls.append(endpoint)
        parsed = urlsplit(endpoint)
        path, params = parsed.path, parse_qs(parsed.query)
        self.counts[endpoint] = self.counts.get(endpoint, 0) + 1
        if path in self.overrides:
            override = self.overrides[path]
            if callable(override):
                return override(endpoint, params, self.counts[endpoint])
            if isinstance(override, Exception):
                raise override
            return copy.deepcopy(override)
        repository = "/".join(path.split("/")[1:3])
        workflow_id = REPOSITORIES.index(repository) + 1 if repository in REPOSITORIES else 1
        if path.endswith("/branches"):
            return self.page(params, "unused", [{"name": "main", "commit": {"sha": SHA}}])["unused"]
        if path.endswith("/tags"):
            return []
        if "/contents/" in path:
            # Content itself is deliberately poison: never receipt evidence.
            return {"type": "file", "sha": BLOB, "content": "PRIVATE_TOKEN /Users/private/worktree"}
        if path.endswith("/actions/workflows"):
            return self.page(params, "workflows", [{"id": workflow_id, "path": ".github/workflows/publish.yml", "state": "active"}])
        if path.endswith("/actions/runs"):
            rows = self.runs[params["status"][0]] if repository == REPOSITORIES[0] else []
            return self.page(params, "workflow_runs", rows)
        if path.endswith("/jobs"):
            return self.page(params, "jobs", self.job_rows)
        if "/actions/runs/" in path:
            return next(copy.deepcopy(row) for rows in self.runs.values() for row in rows
                        if row["id"] == int(path.rsplit("/", 1)[1]))
        if path.endswith("/environments"):
            return self.page(params, "environments", [self.environment] if self.environment else [])
        if "/environments/" in path and path.endswith("/secrets"):
            return self.page(params, "secrets", self.environment_secrets)
        if "/environments/" in path and path.endswith("/deployment-branch-policies"):
            return self.page(params, "branch_policies", [{"id": 101, "type": "branch", "name": "main"}])
        if "/environments/" in path:
            return copy.deepcopy(self.environment)
        if path == "orgs/elsa-workflows/actions/secrets":
            return self.page(params, "secrets", self.org_secrets)
        if path.endswith("/repositories"):
            return self.page(params, "repositories", self.selected_repositories)
        if path.endswith("/actions/secrets"):
            return self.page(params, "secrets", self.repo_secrets)
        if path.endswith("/actions/organization-secrets"):
            return self.page(params, "secrets", self.available_org_secrets)
        raise AssertionError(f"Unmapped fixture route: {endpoint}")

    @staticmethod
    def page(params, field, rows):
        page = int(params.get("page", [1])[0])
        size = int(params.get("per_page", [100])[0])
        return {"total_count": len(rows), field: copy.deepcopy(rows[(page - 1) * size:page * size])}


class PreflightTests(unittest.TestCase):
    def setUp(self):
        self.inventory = inventory()
        self.api = FakeApi()

    def observe(self, **kwargs):
        return preflight(self.inventory, self.api, now=lambda: NOW, **kwargs)

    def assert_closed(self, result):
        for flag in ("publication_performed", "live_publisher_changed", "publication_ready"):
            self.assertIs(result[flag], False)
        self.assertFalse(result["quiescence"]["verified"])
        self.assertTrue(result["pending_gates"])

    def codes(self, result, field="diagnostics"):
        return {item["code"] for item in result[field]}

    def test_complete_synthetic_snapshot_still_cannot_authorize_publication(self):
        result = self.observe()
        self.assertTrue(result["observation_complete"])
        self.assertTrue(result["inventory_verified"])
        self.assertTrue(result["snapshot_quiescent"])
        self.assertTrue(result["quiescence"]["verified"])
        self.assertFalse(result["publication_ready"])
        self.assertFalse(result["quiescence"]["cross_repository_lock"])
        self.assertFalse(result["quiescence"]["new_runs_prevented"])
        self.assertFalse(result["credential_permissions_verified"])
        self.assertEqual(set(REPOSITORIES), {item["repository"] for item in result["repositories"]})
        self.assertTrue(all(item["complete"] for item in result["pagination"]))
        for repository in REPOSITORIES:
            for state in NONTERMINAL:
                requests = [url for url in self.api.calls if url.startswith(f"repos/{repository}/actions/runs?status={state}&")]
                self.assertEqual(2, len(requests))

    def test_every_nonterminal_status_blocks_quiescence(self):
        for state in NONTERMINAL:
            with self.subTest(state=state):
                self.api = FakeApi()
                self.api.runs[state] = [run(status=state)]
                result = self.observe()
                self.assertTrue(result["observation_complete"])
                self.assertEqual(1, result["quiescence"]["nonterminal_run_count"])
                self.assertIn("active_runs_observed", self.codes(result, "authority_gaps"))
                self.assert_closed(result)

    def test_complete_run_and_job_pagination_and_attempt_identities(self):
        self.api.runs["waiting"] = [run(123, status="waiting"), run(124, status="waiting"), run(125, status="waiting")]
        self.api.job_rows = []
        def jobs(endpoint, params, call):
            run_id = int(urlsplit(endpoint).path.split("/")[-4])
            rows = [{"id": run_id * 10 + index, "run_id": run_id, "run_attempt": 1,
                     "status": "queued", "head_sha": SHA} for index in range(3)]
            return self.api.page(params, "jobs", rows)
        for row in self.api.runs["waiting"]:
            self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/runs/{row['id']}/attempts/1/jobs"] = jobs
        result = self.observe(page_size=2)
        self.assertTrue(result["observation_complete"])
        self.assertEqual(9, len(result["repositories"][0]["jobs"]))
        run_pages = [item for item in result["pagination"] if item["scope"] == "runs-before-waiting"
                     and item["repository"] == REPOSITORIES[0]]
        self.assertEqual(2, run_pages[0]["pages"])
        self.assertEqual(3, run_pages[0]["observed_count"])

    def test_run_created_between_cross_repository_sweeps_fails_closed(self):
        def runs(endpoint, params, call):
            rows = [run()] if params["status"][0] == "queued" and call > 1 else []
            return self.api.page(params, "workflow_runs", rows)
        self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/runs"] = runs
        self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/runs/123"] = run()
        result = self.observe()
        self.assertIn("run_snapshot_drift", self.codes(result))
        self.assert_closed(result)

    def test_unknown_run_state_is_not_silently_terminal(self):
        self.api.runs["queued"] = [run(status="new_unknown_status")]
        result = self.observe()
        self.assertIn("unknown_or_changed_run_state", self.codes(result))
        self.assert_closed(result)

    def test_rerun_attempt_changed_during_job_observation_fails_closed(self):
        self.api.runs["queued"] = [run()]
        changed = run()
        changed["run_attempt"] = 2
        self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/runs/123"] = changed
        result = self.observe()
        self.assertIn("run_changed_during_job_observation", self.codes(result))
        self.assert_closed(result)

    def test_unknown_job_state_fails_closed(self):
        self.api.runs["queued"] = [run()]
        self.api.job_rows = [{"id": 99, "run_id": 123, "run_attempt": 1, "head_sha": SHA, "status": "unknown"}]
        result = self.observe()
        self.assertIn("unknown_job_state", self.codes(result))
        self.assert_closed(result)

    def test_job_head_or_attempt_change_fails_closed(self):
        for change in ({"head_sha": "c" * 40}, {"run_attempt": 2}):
            with self.subTest(change=change):
                self.api = FakeApi()
                self.api.runs["queued"] = [run()]
                self.api.job_rows = [{"id": 99, "run_id": 123, "run_attempt": 1, "head_sha": SHA,
                                      "status": "queued", **change}]
                self.assert_closed(self.observe())

    def test_status_transition_during_pagination_is_detected(self):
        self.api.runs["queued"] = [run()]
        self.api.runs["in_progress"] = [run(status="in_progress")]
        result = self.observe()
        self.assertIn("run_moved_between_status_pages", self.codes(result))
        self.assert_closed(result)

    def test_changed_source_and_refs_are_inventory_gaps(self):
        self.api.overrides[f"repos/{REPOSITORIES[0]}/branches"] = [{"name": "main", "commit": {"sha": "c" * 40}}]
        self.api.overrides[f"repos/{REPOSITORIES[0]}/contents/.github/workflows/publish.yml"] = {"type": "file", "sha": "d" * 40}
        result = self.observe()
        self.assertTrue(result["observation_complete"])
        self.assertFalse(result["inventory_verified"])
        self.assertIn("source_changed_from_inventory", self.codes(result, "authority_gaps"))
        self.assertIn("ref_changed_from_inventory", self.codes(result, "authority_gaps"))
        self.assert_closed(result)

    def test_ref_moving_between_snapshots_fails_closed(self):
        self.api.overrides[f"repos/{REPOSITORIES[0]}/branches"] = lambda endpoint, params, call: [
            {"name": "main", "commit": {"sha": SHA if call == 1 else "c" * 40}}]
        result = self.observe()
        self.assertIn("ref_snapshot_drift", self.codes(result))
        self.assert_closed(result)

    def test_unknown_or_changed_workflow_identity_is_a_gap(self):
        self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/workflows"] = {"total_count": 1,
            "workflows": [{"id": 333, "path": ".github/workflows/unknown.yml", "state": "active"}]}
        result = self.observe()
        self.assertTrue(result["observation_complete"])
        self.assertFalse(result["inventory_verified"])
        self.assertIn("workflow_inventory_changed", self.codes(result, "authority_gaps"))
        self.assert_closed(result)

    def test_platform_managed_and_retired_sources_are_explicit_unknowns(self):
        for kind, path in (("platform-managed", "dynamic/copilot"), ("retired", ".github/workflows/old.yml")):
            with self.subTest(kind=kind):
                document = self.inventory["repositories"][0]["workflows"][0]
                document.update({"identity_kind": kind, "path": path})
                document.pop("source_ref", None)
                document.pop("source_blob_sha", None)
                document["observed_refs"] = []
                self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/workflows"] = {"total_count": 1,
                    "workflows": [{"id": 1, "path": path, "state": "active"}]}
                result = self.observe()
                self.assertTrue(result["observation_complete"])
                self.assertIn("immutable_source_unpinned", self.codes(result, "authority_gaps"))
                self.assertFalse(result["inventory_verified"])
                self.assert_closed(result)

    def test_omitting_mutable_source_checks_cannot_verify_inventory(self):
        self.inventory["repositories"][0]["workflows"][0]["observed_refs"] = []
        result = self.observe()
        self.assertIn("mutable_source_refs_unverified", self.codes(result, "authority_gaps"))
        self.assert_closed(result)

    def test_unresolved_historical_paths_remain_gaps(self):
        self.inventory["review"]["unresolved_paths"] = ["Old release tag workflow not contained"]
        result = self.observe()
        self.assertIn("reviewed_scope_has_unresolved_paths", self.codes(result, "authority_gaps"))
        self.assert_closed(result)

    def test_stale_inventory_makes_no_api_requests(self):
        self.inventory["review"]["valid_until"] = "2026-10-07T11:30:00Z"
        result = self.observe()
        self.assertIn("inventory_review_stale", self.codes(result))
        self.assertEqual([], self.api.calls)
        self.assert_closed(result)

    def test_missing_environment_is_observed_authority_gap(self):
        self.api.environment = None
        result = self.observe()
        self.assertTrue(result["observation_complete"])
        self.assertIn("required_environment_missing", self.codes(result, "authority_gaps"))
        self.assertFalse(result["publication_ready"])

    def test_unprotected_environment_is_observed_authority_gap(self):
        self.api.environment["protection_rules"] = []
        self.api.environment["can_admins_bypass"] = True
        self.api.environment["deployment_branch_policy"] = None
        result = self.observe()
        self.assertTrue(result["observation_complete"])
        self.assertIn("required_environment_unprotected", self.codes(result, "authority_gaps"))
        self.assertFalse(result["publication_ready"])

    def test_undisclosed_bypass_policy_is_unknown_not_disabled(self):
        del self.api.environment["can_admins_bypass"]
        result = self.observe()
        self.assertTrue(result["observation_complete"])
        self.assertIn("unknown_admin_bypass", self.codes(result, "authority_gaps"))
        self.assertIsNone(result["repositories"][0]["environments"][0]["can_admins_bypass"])

    def test_custom_branch_policy_and_reviewers_are_recorded_as_metadata_only(self):
        self.api.environment["deployment_branch_policy"] = {"protected_branches": False, "custom_branch_policies": True}
        result = self.observe()
        observed = result["repositories"][0]["environments"][0]
        self.assertEqual([{"id": 101, "type": "branch", "name": "main"}], observed["selected_branch_policies"])
        self.assertEqual([{"id": 99, "type": "User"}], observed["reviewers"])
        self.assertFalse(observed["approval_observed"])

    def test_repository_and_org_credentials_preserve_fallback_and_encoded_ambiguity(self):
        self.api.repo_secrets = [{"name": "FEEDZ_API_KEY"}]
        self.api.org_secrets = [{"name": "FEEDZ_BASE64_TOKEN", "visibility": "selected"}]
        result = self.observe()
        boundaries = result["repositories"][0]["credential_boundaries"]
        self.assertTrue(all(item["repository_or_org_fallback_possible"] for item in boundaries))
        encoded = next(item for item in boundaries if item["name"] == "FEEDZ_BASE64_TOKEN")
        self.assertEqual("unknown", encoded["encoded_credential_scope"])
        self.assertEqual("unknown", encoded["credential_permissions"])
        self.assertEqual(["included"], encoded["organization_access"])
        self.assertFalse(result["credential_permissions_verified"])
        self.assertIn("repository_or_org_credential_authority_unretired", self.codes(result, "authority_gaps"))

    def test_org_private_visibility_does_not_infer_repository_access(self):
        self.api.org_secrets = [{"name": "FEEDZ_API_KEY", "visibility": "private"}]
        result = self.observe()
        boundary = next(item for item in result["repositories"][0]["credential_boundaries"] if item["name"] == "FEEDZ_API_KEY")
        self.assertEqual(["unknown-repository-visibility"], boundary["organization_access"])
        self.assertTrue(boundary["repository_or_org_fallback_possible"])

    def test_repository_org_availability_survives_unavailable_organization_policy(self):
        self.api.available_org_secrets = [{"name": "FEEDZ_BASE64_TOKEN"}, {"name": "UNRELATED_SECRET"}]
        self.api.overrides["orgs/elsa-workflows/actions/secrets"] = ObservationError("transport_failed")
        result = self.observe(page_size=1)
        self.assertFalse(result["observation_complete"])
        for observation in result["repositories"]:
            boundary = next(item for item in observation["credential_boundaries"]
                            if item["name"] == "FEEDZ_BASE64_TOKEN")
            self.assertIs(boundary["organization_available_to_repository"], True)
            self.assertIs(boundary["organization_name_present"], True)
            self.assertIs(boundary["repository_or_org_fallback_possible"], True)
            self.assertEqual("unknown", boundary["credential_permissions"])
            self.assertEqual([], boundary["organization_access"])
        self.assertNotIn("UNRELATED_SECRET", json.dumps(result))
        self.assert_closed(result)

    def test_unavailable_repository_org_metadata_is_not_absence(self):
        self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/organization-secrets"] = ObservationError("transport_failed")
        result = self.observe()
        boundary = result["repositories"][0]["credential_boundaries"][0]
        self.assertIsNone(boundary["organization_available_to_repository"])
        self.assertIsNone(boundary["repository_or_org_fallback_possible"])
        self.assert_closed(result)

    def test_failed_secret_metadata_is_not_an_absent_credential_claim(self):
        self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/secrets"] = ObservationError("transport_failed")
        result = self.observe()
        self.assertFalse(result["observation_complete"])
        self.assert_closed(result)

    def test_receipt_excludes_content_secrets_error_bodies_and_private_paths(self):
        self.api.environment["extra_raw_content"] = "PRIVATE_TOKEN /Users/private/worktree"
        self.api.repo_secrets = [{"name": "FEEDZ_API_KEY", "value": "PRIVATE_TOKEN", "selected_repositories_url": "/Users/private/worktree"}]
        result = self.observe()
        encoded = json.dumps(result)
        self.assertNotIn("PRIVATE_TOKEN", encoded)
        self.assertNotIn("/Users/", encoded)
        self.assertNotIn("extra_raw_content", encoded)

    def test_observation_deadline_and_expiring_review_fail_closed(self):
        times = iter((NOW, NOW + timedelta(hours=2)))
        result = preflight(self.inventory, self.api, now=lambda: next(times))
        self.assertIn("observation_window_exceeded", self.codes(result))
        self.assertIn("inventory_expired_during_observation", self.codes(result))
        self.assert_closed(result)

    def test_malformed_inventory_returns_bounded_diagnostic(self):
        for document in (None, {}, {"schema_version": 1, "review": {}},
                         {**inventory(), "repositories": [{"repository": "/Users/private"}]}):
            with self.subTest(document=document):
                result = preflight(document, self.api, now=lambda: NOW)
                self.assertFalse(result["observation_complete"])
                self.assertNotIn("/Users/private", json.dumps(result))
                self.assert_closed(result)

    def test_complete_ref_pages_detect_unreviewed_branches_and_tags(self):
        branches = [{"name": name, "commit": {"sha": SHA}} for name in ("main", "release/3.8", "release/3.9")]
        tags = [{"name": name, "commit": {"sha": SHA}} for name in ("3.8.4", "3.9.0", "3.7.0")]
        self.api.overrides[f"repos/{REPOSITORIES[0]}/branches"] = lambda endpoint, params, call: self.api.page(params, "rows", branches)["rows"]
        self.api.overrides[f"repos/{REPOSITORIES[0]}/tags"] = lambda endpoint, params, call: self.api.page(params, "rows", tags)["rows"]
        result = self.observe(page_size=2)
        self.assertTrue(result["observation_complete"])
        self.assertFalse(result["inventory_verified"])
        self.assertEqual(6, len(result["repositories"][0]["refs_before"]))
        self.assertIn("additional_source_refs_unreviewed", self.codes(result, "authority_gaps"))
        self.assert_closed(result)

    def test_new_tag_between_snapshots_is_drift(self):
        self.api.overrides[f"repos/{REPOSITORIES[0]}/tags"] = lambda endpoint, params, call: (
            [] if call == 1 else [{"name": "3.10.0", "commit": {"sha": SHA}}])
        result = self.observe()
        self.assertIn("ref_snapshot_drift", self.codes(result))
        self.assert_closed(result)

    def test_missing_required_ref_is_an_inventory_gap(self):
        self.inventory["repositories"][0]["refs"].append({"ref": "tags/3.9.0", "expected_sha": SHA})
        result = self.observe()
        self.assertTrue(result["observation_complete"])
        self.assertIn("required_source_ref_missing", self.codes(result, "authority_gaps"))
        self.assert_closed(result)

    def test_unknown_workflow_path_and_invalid_schema_are_bounded_diagnostics(self):
        for change in ({"path": ".github/workflows/unknown.yml"}, {"status": ["PRIVATE_TOKEN"]}):
            with self.subTest(change=change):
                self.api = FakeApi()
                self.api.runs["queued"] = [{**run(), **change}]
                result = self.observe()
                self.assertFalse(result["observation_complete"])
                self.assertNotIn("PRIVATE_TOKEN", json.dumps(result))
                self.assert_closed(result)

    def test_failed_metadata_preserves_unknown_credential_presence(self):
        self.api.overrides[f"repos/{REPOSITORIES[0]}/actions/secrets"] = ObservationError("transport_failed")
        self.api.overrides["orgs/elsa-workflows/actions/secrets"] = ObservationError("transport_failed")
        result = self.observe()
        boundary = result["repositories"][0]["credential_boundaries"][0]
        self.assertIsNone(boundary["repository_name_present"])
        self.assertIsNone(boundary["organization_name_present"])
        self.assertIsNone(boundary["repository_or_org_fallback_possible"])
        self.assert_closed(result)

    def test_environment_list_failure_is_not_claimed_as_missing_environment(self):
        self.api.overrides[f"repos/{REPOSITORIES[0]}/environments"] = ObservationError("transport_failed")
        result = self.observe()
        self.assertFalse(result["observation_complete"])
        missing = [item for item in result["authority_gaps"] if item["repository"] == REPOSITORIES[0]
                   and item["code"] == "required_environment_missing"]
        self.assertEqual([], missing)
        self.assert_closed(result)

    def test_cli_writes_sanitized_receipt_and_keeps_publication_false(self):
        with tempfile.TemporaryDirectory() as directory:
            input_path, output_path = Path(directory).resolve() / "inventory.json", Path(directory).resolve() / "receipt.json"
            input_path.write_text(json.dumps(self.inventory))
            with patch("sys.argv", ["preflight", "--inventory", str(input_path), "--output", str(output_path)]), \
                    patch("consolidated_publisher_handoff.GhApi", return_value=self.api), \
                    patch("consolidated_publisher_handoff.utc_now", return_value=NOW), \
                    patch("consolidated_publisher_handoff.preflight", wraps=lambda inventory, api, **kwargs:
                        preflight(inventory, api, now=lambda: NOW, **kwargs)):
                self.assertEqual(0, main())
            result = json.loads(output_path.read_text())
        self.assertTrue(result["observation_complete"])
        self.assertFalse(result["publication_ready"])
        self.assertEqual(64, len(result["inventory_sha256"]))

    def test_cli_input_error_omits_private_input_path(self):
        output = io.StringIO()
        with patch("sys.argv", ["preflight", "--inventory", "/Users/private/missing.json"]), redirect_stdout(output):
            self.assertEqual(1, main())
        result = json.loads(output.getvalue())
        self.assertNotIn("/Users/", output.getvalue())
        self.assertFalse(result["publication_ready"])

    def test_unrelated_secret_names_are_counted_but_not_retained_or_policy_queried(self):
        self.api.repo_secrets = [{"name": "OPENAI_API_KEY"}, {"name": "FEEDZ_UNEXPECTED"}]
        self.api.org_secrets = [{"name": "PRIVATE_OTHER_TOKEN", "visibility": "selected"},
                                {"name": "FEEDZ_BASE64_TOKEN", "visibility": "selected"}]
        result = self.observe(page_size=2)
        encoded = json.dumps(result)
        self.assertNotIn("OPENAI_API_KEY", encoded)
        self.assertNotIn("PRIVATE_OTHER_TOKEN", encoded)
        self.assertIn("FEEDZ_UNEXPECTED", encoded)
        self.assertIn("unexpected_feed_credential_name", self.codes(result, "authority_gaps"))
        self.assertFalse(any("PRIVATE_OTHER_TOKEN/repositories" in endpoint for endpoint in self.api.calls))
        pages = [page for page in result["pagination"] if page["scope"] == "repository-secret-names"]
        self.assertTrue(all(page["observed_count"] == 2 for page in pages))

    def test_cli_existing_output_and_inventory_overlap_preserve_files_without_gets(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            input_path, output_path = root / "inventory.json", root / "receipt.json"
            input_path.write_text(json.dumps(self.inventory))
            output_path.write_text("preserve me")
            for target in (output_path, input_path):
                before = target.read_bytes()
                with patch("sys.argv", ["preflight", "--inventory", str(input_path), "--output", str(target)]), \
                        patch("consolidated_publisher_handoff.GhApi") as constructor, redirect_stdout(io.StringIO()):
                    self.assertEqual(1, main())
                    constructor.assert_not_called()
                self.assertEqual(before, target.read_bytes())

    def test_cli_rejects_output_symlink_and_symlink_ancestor_without_gets(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            input_path, real_directory = root / "inventory.json", root / "real"
            input_path.write_text(json.dumps(self.inventory))
            real_directory.mkdir()
            linked_directory = root / "alias"
            linked_directory.symlink_to(real_directory, target_is_directory=True)
            final_link = root / "linked-receipt.json"
            final_link.symlink_to(root / "missing.json")
            for target in (final_link, linked_directory / "receipt.json"):
                with patch("sys.argv", ["preflight", "--inventory", str(input_path), "--output", str(target)]), \
                        patch("consolidated_publisher_handoff.GhApi") as constructor, redirect_stdout(io.StringIO()):
                    self.assertEqual(1, main())
                    constructor.assert_not_called()
            self.assertFalse((real_directory / "receipt.json").exists())
            self.assertFalse((root / "missing.json").exists())

    def test_cli_rejects_duplicate_keys_and_oversized_input_without_gets(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory).resolve() / "inventory.json"
            for content in ('{"schema_version":1,"schema_version":1}', " " * 1_000_001):
                source.write_text(content)
                with patch("sys.argv", ["preflight", "--inventory", str(source)]), \
                        patch("consolidated_publisher_handoff.GhApi") as constructor, redirect_stdout(io.StringIO()):
                    self.assertEqual(1, main())
                    constructor.assert_not_called()

    def test_cli_receipt_file_has_private_mode_and_raw_input_digest(self):
        import hashlib
        import stat
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            source, target = root / "inventory.json", root / "receipt.json"
            raw = json.dumps(self.inventory, indent=3).encode()
            source.write_bytes(raw)
            with patch("sys.argv", ["preflight", "--inventory", str(source), "--output", str(target)]), \
                    patch("consolidated_publisher_handoff.GhApi", return_value=self.api), \
                    patch("consolidated_publisher_handoff.preflight", wraps=lambda inventory, api, **kwargs:
                        preflight(inventory, api, now=lambda: NOW, **kwargs)):
                self.assertEqual(0, main())
            result = json.loads(target.read_text())
            self.assertEqual(hashlib.sha256(raw).hexdigest(), result["inventory_file_sha256"])
            self.assertEqual(0o600, stat.S_IMODE(target.stat().st_mode))
            self.assertEqual(40, len(result["implementation"]["git_head"]))
            self.assertEqual(64, len(result["implementation"]["script_sha256"]))


class PaginationTests(unittest.TestCase):
    def setUp(self):
        self.api = FakeApi()
        self.collector = Collector(self.api, max_pages=4, page_size=2)
        self.endpoint = f"repos/{REPOSITORIES[0]}/actions/runs"

    def pages(self):
        return self.collector.pages(REPOSITORIES[0], "test", self.endpoint, "workflow_runs", actions_runs=True)

    def test_exact_page_boundary_requires_empty_terminal_page(self):
        self.api.overrides[self.endpoint] = lambda endpoint, params, call: self.api.page(params, "workflow_runs", [run(1), run(2)])
        self.assertEqual(2, len(self.pages()))
        self.assertEqual(2, self.collector.pagination[0]["pages"])
        self.assertTrue(self.collector.pagination[0]["complete"])

    def test_duplicate_items_count_drift_truncation_and_cap_fail_closed(self):
        scenarios = {
            "duplicate": lambda endpoint, params, call: {"total_count": 3, "workflow_runs": [run(1), run(2)] if params["page"] == ["1"] else [run(1)]},
            "drift": lambda endpoint, params, call: {"total_count": 3 if params["page"] == ["1"] else 2, "workflow_runs": [run(1), run(2)] if params["page"] == ["1"] else []},
            "truncated": {"total_count": 3, "workflow_runs": [run(1)]},
            "api-cap": {"total_count": 1000, "workflow_runs": []},
            "local-cap": {"total_count": 9, "workflow_runs": []},
        }
        for label, value in scenarios.items():
            with self.subTest(label=label):
                self.api.overrides[self.endpoint] = value
                with self.assertRaises(ObservationError):
                    self.pages()
                self.assertFalse(self.collector.pagination[-1]["complete"])


class TransportTests(unittest.TestCase):
    def test_uses_only_explicit_get_without_credentials_in_arguments(self):
        def execute(command, **kwargs):
            self.assertEqual(["gh", "api", "--method", "GET", f"repos/{REPOSITORIES[0]}/actions/workflows"], command)
            self.assertIs(kwargs["stderr"], subprocess.DEVNULL)
            kwargs["stdout"].write(b'{"total_count":0,"workflows":[]}')
            return subprocess.CompletedProcess(command, 0)
        with patch("consolidated_publisher_handoff.subprocess.run", side_effect=execute):
            result = GhApi().get(f"repos/{REPOSITORIES[0]}/actions/workflows")
        self.assertEqual([], result["workflows"])

    def test_transport_errors_are_fixed_codes(self):
        for error, code in ((OSError("PRIVATE_TOKEN /Users/private"), "transport_unavailable"),
                            (subprocess.TimeoutExpired("PRIVATE_TOKEN /Users/private", 2), "transport_timeout")):
            with self.subTest(code=code), patch("consolidated_publisher_handoff.subprocess.run", side_effect=error):
                with self.assertRaisesRegex(ObservationError, f"^{code}$"):
                    GhApi().get(f"repos/{REPOSITORIES[0]}/actions/workflows")

    def test_invalid_json_response_limit_and_http_failure_are_fixed_codes(self):
        for body, returncode, expected in ((b'PRIVATE_TOKEN', 0, 'invalid_json'), (b'"too large"', 0, 'response_limit'),
                                            (b'PRIVATE_TOKEN /Users/private', 1, 'transport_failed')):
            with self.subTest(expected=expected):
                def execute(command, **kwargs):
                    kwargs['stdout'].write(body)
                    return subprocess.CompletedProcess(command, returncode)
                with patch("consolidated_publisher_handoff.subprocess.run", side_effect=execute):
                    with self.assertRaisesRegex(ObservationError, f"^{expected}$"):
                        GhApi(max_response_bytes=5 if expected == 'response_limit' else 100).get(
                            f"repos/{REPOSITORIES[0]}/actions/workflows")

    def test_total_deadline_caps_each_timeout_and_stops_further_gets(self):
        def execute(command, **kwargs):
            self.assertLessEqual(kwargs["timeout"], 0.75)
            kwargs["stdout"].write(b'{}')
            return subprocess.CompletedProcess(command, 0)
        with patch("consolidated_publisher_handoff.time.monotonic", side_effect=[0, 0.25, 1.01]), \
                patch("consolidated_publisher_handoff.subprocess.run", side_effect=execute) as execute_mock:
            transport = GhApi(max_duration_seconds=1, timeout_seconds=20)
            self.assertEqual({}, transport.get(f"repos/{REPOSITORIES[0]}/actions/workflows"))
            with self.assertRaisesRegex(ObservationError, "^observation_window_exceeded$"):
                transport.get(f"repos/{REPOSITORIES[0]}/actions/workflows")
            self.assertEqual(1, execute_mock.call_count)
            self.assertEqual(1, transport.request_count)

    def test_request_budget_deadline_and_untrusted_endpoints_fail_before_execution(self):
        cases = [(GhApi(max_requests=0), f"repos/{REPOSITORIES[0]}/actions/workflows"),
                 (GhApi(max_duration_seconds=-1), f"repos/{REPOSITORIES[0]}/actions/workflows"),
                 (GhApi(), "https://malicious.test/token"), (GhApi(), "repos/elsewhere/repo/actions/runs"),
                 (GhApi(), "repos/elsa-workflows/elsa-core/../../private")]
        with patch("consolidated_publisher_handoff.subprocess.run") as execute:
            for transport, endpoint in cases:
                with self.subTest(endpoint=endpoint), self.assertRaises(ObservationError):
                    transport.get(endpoint)
            execute.assert_not_called()


if __name__ == "__main__":
    unittest.main()
