import copy
import json
from pathlib import Path
import re
import sqlite3
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import run_persisted_workflow_upgrade_fixture as fixture
import prove_consolidated_package_consumers as packages


class UpgradeContracts(unittest.TestCase):
    def setUp(self):
        self.spec = fixture.spec_for("3.8.4", "net8.0", Path("/tmp/cell"))
        self.state = {
            **{key: self.spec[key] for key in ("instance_id", "definition_id", "definition_version_id", "correlation_id")},
            "definition_version": 1, "definition_json_sha256": "a" * 64, "input": self.spec["expected"].copy(),
            "properties": {}, "sentinel": self.spec["text"],
            "status": "Running", "sub_status": "Suspended", "state_bookmark_count": 1, "stored_bookmark_count": 1,
            "bookmark": {"Id": "bookmark-1", "Hash": "hash-1", "ActivityId": "upgrade-event"},
            "stored_bookmark": {"Id": "bookmark-1", "Hash": "hash-1"},
        }

    def test_original_suspension_is_valid(self):
        fixture.check_state(self.state, self.spec, False)

    def test_tampered_baseline_identity_input_sentinel_or_bookmark_fails(self):
        for key, value in (("instance_id", "other"), ("input", {}), ("sentinel", "other"), ("bookmark", {})):
            with self.subTest(key=key):
                state = {**self.state, key: value}
                with self.assertRaises(RuntimeError):
                    fixture.check_state(state, self.spec, False)

    def test_boolean_is_not_an_integer(self):
        state = copy.deepcopy(self.state)
        state["input"]["number"] = True
        with self.assertRaisesRegex(RuntimeError, "Typed input"):
            fixture.check_state(state, self.spec, False)

    def test_bookmark_tampering_between_processes_fails(self):
        state = copy.deepcopy(self.state)
        state["bookmark"]["Id"] = state["stored_bookmark"]["Id"] = "replacement"
        with self.assertRaisesRegex(RuntimeError, "Original bookmark"):
            fixture.check_state(state, self.spec, False, self.state)

    def test_omitted_duplicate_and_failed_cells_fail_closed(self):
        cells = [{"baseline": b, "framework": t, "passed": True} for b, t in fixture.MATRIX]
        fixture.check_matrix(cells)
        for changed in (cells[:-1], cells + [cells[0]], [dict(cell, passed=False) for cell in cells]):
            with self.assertRaises(RuntimeError):
                fixture.check_matrix(changed)

    def test_wrong_missing_unknown_and_excluded_candidate_packages_fail(self):
        assets = {"targets": {"net8.0": {"Elsa/3.10.0": {"type": "package"}}}}
        with self.assertRaisesRegex(RuntimeError, "proof version"):
            packages.validate_project_assets(assets, ["net8.0"], {"Elsa"}, fixture.PROOF_VERSION, set())
        for package_id, excluded in (("Elsa.Unknown", set()), ("Elsa.Excluded", {"Elsa.Excluded"})):
            with self.subTest(package_id=package_id), self.assertRaises(RuntimeError):
                packages.validate_project_assets({"targets": {"net8.0": {f"{package_id}/{fixture.PROOF_VERSION}": {"type": "package"}}}}, ["net8.0"], {"Elsa"}, fixture.PROOF_VERSION, set(), excluded)
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "verified-artifacts.json").write_text("{}")
            with patch.object(fixture, "verify_archive"), self.assertRaises(ValueError):
                fixture.verify_artifact(root, root / "proof.zip")

    def test_snapshot_includes_committed_wal_and_reader_cannot_write(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            database, snapshot = root / "state.db", root / "snapshot.db"
            with fixture.closing(sqlite3.connect(database)) as writer, writer:
                writer.execute("PRAGMA journal_mode=WAL")
                writer.execute('CREATE TABLE "__EFMigrationsHistory" (MigrationId TEXT, ProductVersion TEXT)')
                writer.execute('INSERT INTO "__EFMigrationsHistory" VALUES ("migration-1", "version")')
                writer.commit()
                evidence = fixture.inspect_database(database, snapshot)
                self.assertEqual(evidence["history"], fixture.inspect_database(snapshot)["history"])
                with fixture.closing(fixture.readonly(database)) as reader, self.assertRaises(sqlite3.OperationalError):
                    reader.execute('DELETE FROM "__EFMigrationsHistory"')

    def test_archive_digest_extraction_and_replacement_directory_fail_closed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / "proof.zip"
            with zipfile.ZipFile(archive, "w") as package:
                package.writestr("receipt.json", "original")
            with self.assertRaisesRegex(RuntimeError, "digest mismatch"):
                fixture.extract_archive(archive, root / "extracted")
            with patch.object(fixture, "PROOF_ARCHIVE_SHA256", fixture.sha256(archive)):
                fixture.extract_archive(archive, root / "extracted")
                fixture.verify_archive(root / "extracted", archive)
                (root / "extracted" / "receipt.json").write_text("replacement")
                with self.assertRaisesRegex(RuntimeError, "differs"):
                    fixture.verify_archive(root / "extracted", archive)

    def test_archive_path_traversal_and_symlinks_fail(self):
        with tempfile.TemporaryDirectory() as temporary:
            archive = Path(temporary) / "proof.zip"
            for name, attributes in (("../escape", 0), ("link", 0o120777 << 16)):
                with zipfile.ZipFile(archive, "w") as package:
                    member = zipfile.ZipInfo(name)
                    member.external_attr = attributes
                    package.writestr(member, "target")
                with zipfile.ZipFile(archive) as package, self.assertRaisesRegex(RuntimeError, "Unsafe"):
                    fixture.safe_members(package)

    def test_metadata_expiration_and_identity_fail(self):
        metadata = {"id": fixture.PROOF_ARTIFACT, "name": f"consolidated-proof-{fixture.PROOF_RUN}-1", "expired": False,
                    "workflow_run": {"id": fixture.PROOF_RUN, "head_sha": fixture.PROOF_SOURCE}}
        fixture.validate_artifact_metadata(metadata)
        for key, value in (("expired", True), ("id", 1), ("workflow_run", {})):
            with self.assertRaises(RuntimeError):
                fixture.validate_artifact_metadata({**metadata, key: value})

    def test_failed_migrations_and_default_state_fail(self):
        migrations = {name: {"applied": ["migration"], "pending": [], "data_source": self.spec[name]} for name in ("management", "runtime")}
        receipt = {"passed": True, "phase": "suspend", "tenant_id": "", "state": self.state, "migrations_before": copy.deepcopy(migrations), "migrations_after": copy.deepcopy(migrations)}
        fixture.check_phase(receipt, self.spec, "suspend")
        receipt["migrations_after"]["management"]["pending"] = ["unapplied"]
        with self.assertRaisesRegex(RuntimeError, "migrations"):
            fixture.check_phase(receipt, self.spec, "suspend")
        with self.assertRaises(RuntimeError):
            fixture.check_state({}, self.spec, False)

    def test_public_receipt_excludes_paths_rows_errors_and_keeps_failed_identity(self):
        raw = {key: "identity" for key in ("proof_run", "proof_artifact", "candidate_source", "candidate_version", "manifest_sha256", "original_receipt_sha256", "fixture_sha256", "archive_sha256")}
        raw.update(passed=False, complete_matrix=False, published=False, error="/private/secret stack trace",
                   cells=[{"baseline": "3.8.4", "framework": "net8.0", "passed": False,
                           "error": "/private/secret", "databases": {"row": "secret"}, "properties": "secret"}])
        public = fixture.public_receipt(raw)
        serialized = json.dumps(public)
        self.assertNotIn("secret", serialized)
        self.assertNotIn("private", serialized)
        self.assertNotIn("properties", serialized)
        self.assertEqual(len(public["cells"]), 6)
        self.assertEqual(next(cell for cell in public["cells"] if cell["baseline"] == "3.8.4" and cell["framework"] == "net8.0")["result"], "failed")

    def test_checkout_output_rejected_before_any_directory_creation(self):
        output = Path(fixture.__file__).resolve().parents[2] / "forbidden-proof-output"
        self.assertFalse(output.exists())
        with patch.object(fixture, "verify_artifact") as verify, self.assertRaisesRegex(RuntimeError, "outside checkout"):
            fixture.run(Path("/missing"), Path("/missing.zip"), output, [])
        verify.assert_not_called()
        self.assertFalse(output.exists())

    def test_retention_preserves_failed_synthetic_database_and_sanitizes_logs(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            cell = root / "3.8.4-net8.0"
            cell.mkdir()
            (cell / "management.db").write_bytes(b"synthetic original database")
            (cell / "suspend.log").write_text("System.InvalidOperationException: private-secret\nerror CS1234 /private/path\n")
            (cell / "suspend.json").write_text(json.dumps({"phase": "suspend", "passed": False, "error": "secret stack"}))
            (cell / "NuGet.Config").write_text("credential-secret")
            (cell / "management.db-shm").write_text("excluded")
            (root / "public-upgrade-proof.json").write_text('{"passed":false}')
            result = {"passed": False, "complete_matrix": False, "cells": [{"baseline": "3.8.4", "framework": "net8.0", "passed": False,
                       "commands": [{"command": ["dotnet", "/private/Consumer.dll", "suspend"], "log": str(cell / "suspend.log"), "exit_code": 1, "timed_out": False}]}]}
            fixture.stage_evidence(root, result)
            staged = root / "retained-evidence"
            self.assertEqual((staged / "3.8.4-net8.0/management.db").read_bytes(), (cell / "management.db").read_bytes())
            execution = json.loads((staged / "3.8.4-net8.0/execution.json").read_text())
            self.assertEqual(execution["phases"], [{"phase": "suspend", "runner_passed": False, "validation_passed": False, "failure_category": "execution_or_validation_failed"}])
            self.assertEqual(execution["commands"][0]["exit_code"], 1)
            self.assertEqual(execution["commands"][0]["raw_log_sha256"], fixture.sha256(cell / "suspend.log"))
            self.assertEqual(execution["commands"][0]["diagnostic_codes"], ["CS1234"])
            serialized = json.dumps(execution)
            self.assertNotIn("secret", serialized)
            self.assertNotIn("/private", serialized)
            self.assertFalse((staged / "3.8.4-net8.0/NuGet.Config").exists())
            self.assertFalse((staged / "3.8.4-net8.0/suspend.log").exists())
            self.assertFalse((staged / "3.8.4-net8.0/management.db-shm").exists())
            self.assertFalse(json.loads((staged / "retention-manifest.json").read_text())["complete_matrix"])

    def test_retention_rejects_database_symlink_and_outside_log(self):
        for kind in ("database", "log"):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                cell = root / "3.8.4-net8.0"
                cell.mkdir()
                outside = root / "outside"
                outside.write_text("secret")
                (root / "public-upgrade-proof.json").write_text("{}")
                record = {"baseline": "3.8.4", "framework": "net8.0", "passed": False, "commands": []}
                if kind == "database":
                    (cell / "management.db").symlink_to(outside)
                else:
                    record["commands"] = [{"command": ["dotnet", "restore"], "log": str(outside), "exit_code": 1, "timed_out": False}]
                with self.assertRaisesRegex(RuntimeError, "allowlist"):
                    fixture.stage_evidence(root, {"passed": False, "complete_matrix": False, "cells": [record]})

    def test_invalid_phase_receipt_is_retained_as_a_bounded_failure(self):
        for text in (b"broken", b"[]", b'{"phase":"wrong","passed":true}', b"\xff"):
            with self.subTest(text=text), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                cell = root / "3.8.4-net8.0"
                cell.mkdir()
                (cell / "suspend.json").write_bytes(text)
                (root / "public-upgrade-proof.json").write_text("{}")
                fixture.stage_evidence(root, {"passed": False, "complete_matrix": False, "cells": [{"baseline": "3.8.4", "framework": "net8.0", "passed": False}]})
                diagnostic = json.loads((root / "retained-evidence/3.8.4-net8.0/execution.json").read_text())
                self.assertEqual(diagnostic["phases"][0]["phase"], "suspend")
                self.assertFalse(diagnostic["phases"][0]["runner_passed"])
                self.assertEqual(diagnostic["phases"][0]["failure_category"], "receipt_validation_failed")


class WorkflowBoundaryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = Path(__file__).resolve().parents[2]
        cls.workflow = (root / ".github/workflows/persisted-workflow-upgrade-proof.yml").read_text()
        cls.triggers, jobs = cls.workflow.split("\njobs:\n", 1)
        cls.retrieve, cls.proof = jobs.split("\n  proof:\n", 1)

    def test_only_repository_write_push_and_manual_triggers_with_no_default_permissions(self):
        self.assertNotIn("pull_request", self.workflow)
        self.assertIn("  push:\n", self.triggers)
        self.assertIn("  workflow_dispatch:\n", self.triggers)
        branches = self.triggers.split("    branches:\n", 1)[1].split("    paths:\n", 1)[0]
        self.assertEqual(re.findall(r"^      - '([^']+)'$", branches, re.MULTILINE), ["codex/**"])
        self.assertIn("\npermissions: {}\n", self.triggers)

    def test_retrieval_token_has_no_checkout_or_repository_execution(self):
        self.assertIn("    permissions:\n      actions: read\n", self.retrieve)
        self.assertNotIn("contents:", self.retrieve)
        for forbidden in ("checkout@", "scripts/", "working-directory:", "uses: ./", "github.event."):
            with self.subTest(forbidden=forbidden):
                self.assertNotIn(forbidden, self.retrieve)
        self.assertNotRegex(self.retrieve, r"\b(?:python3?|node|source|eval)\s")
        self.assertEqual(self.workflow.count("GH_TOKEN:"), 1)
        self.assertEqual(self.workflow.count("${{ github.token }}"), 1)
        self.assertNotIn("secrets.", self.workflow)

    def test_trusted_retrieval_identity_matches_runner_without_importing_it(self):
        for endpoint in (f"gh api repos/elsa-workflows/elsa-core/actions/artifacts/{fixture.PROOF_ARTIFACT} >",
                         f"gh api repos/elsa-workflows/elsa-core/actions/artifacts/{fixture.PROOF_ARTIFACT}/zip >",
                         f".id == {fixture.PROOF_ARTIFACT}", f".workflow_run.id == {fixture.PROOF_RUN}",
                         f'.name == "consolidated-proof-{fixture.PROOF_RUN}-1"',
                         f'.workflow_run.head_sha == "{fixture.PROOF_SOURCE}"',
                         f"'{fixture.PROOF_ARCHIVE_SHA256}'", ".expired == false", "sha256sum --check --status"):
            with self.subTest(endpoint=endpoint):
                self.assertIn(endpoint, self.retrieve)

    def test_bridge_transfers_only_two_original_files_by_immutable_same_run_id(self):
        files = re.search(r"          path: \|\n((?:            .*\n)+)", self.retrieve).group(1).splitlines()
        self.assertEqual([path.strip() for path in files], ["${{ runner.temp }}/upgrade-input/proof.zip",
                                                          "${{ runner.temp }}/upgrade-input/artifact.json"])
        self.assertIn("input-artifact-id: ${{ steps.transfer.outputs.artifact-id }}", self.retrieve)
        self.assertIn("if-no-files-found: error", self.retrieve)
        self.assertIn("retention-days: 1", self.retrieve)
        self.assertIn("needs: retrieve", self.proof)
        self.assertIn("artifact-ids: ${{ needs.retrieve.outputs.input-artifact-id }}", self.proof)
        self.assertIn("actions/download-artifact@d3f86a106a0bac45b974a628896c90dbdf5c8093", self.proof)
        self.assertNotIn("github-token:", self.proof)
        self.assertNotIn("run-id:", self.proof)
        self.assertNotIn("repository:", self.proof)

    def test_single_transfer_download_places_original_files_at_extractor_root(self):
        download = self.proof.split("      - uses: actions/checkout@", 1)[0]
        self.assertIn("artifact-ids: ${{ needs.retrieve.outputs.input-artifact-id }}", download)
        self.assertIn("merge-multiple: true", download)
        self.assertIn("path: ${{ runner.temp }}/upgrade-input", download)
        self.assertIn('--artifact-metadata "$RUNNER_TEMP/upgrade-input/artifact.json"', self.proof)
        self.assertIn('--proof-archive "$RUNNER_TEMP/upgrade-input/proof.zip"', self.proof)

    def test_fixture_job_checks_exact_head_without_artifact_read_permission_or_token(self):
        self.assertIn("    permissions:\n      contents: read\n      actions: none\n", self.proof)
        self.assertIn("ref: ${{ github.sha }}", self.proof)
        self.assertIn("persist-credentials: false", self.proof)
        for forbidden in ("GH_TOKEN", "github.token", "actions: read", "gh api"):
            with self.subTest(forbidden=forbidden):
                self.assertNotIn(forbidden, self.proof)
        self.assertIn("--extract-only --artifact-metadata", self.proof)
        self.assertIn('--proof-archive "$RUNNER_TEMP/upgrade-input/proof.zip"', self.proof)


if __name__ == "__main__":
    unittest.main()
