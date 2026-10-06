import copy
import json
from pathlib import Path
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
            with sqlite3.connect(database) as writer:
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


if __name__ == "__main__":
    unittest.main()
