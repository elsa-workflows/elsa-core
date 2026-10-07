import copy
from dataclasses import replace
from datetime import datetime, timedelta, timezone
import hashlib
import json
from pathlib import Path
import tempfile
import sys
import unittest
from unittest.mock import patch
import zipfile

import consolidated_candidate_input as candidate_input
import prepare_consolidated_release_candidate as candidate
import prove_consolidated_package_consumers as packages
import run_persisted_workflow_upgrade_fixture as fixture
import run_stable_persisted_workflow_upgrade_fixture as stable
from test_consolidated_candidate_clock import AFTER_EXPIRY, candidate_clock

ROOT = Path(__file__).resolve().parents[2]


class StableAdapterContracts(unittest.TestCase):
    def setUp(self):
        self.enterContext(candidate_clock(candidate, candidate_input, sys.modules[__name__]))
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        content = self.root / "content"
        artifacts = content / "artifacts"
        artifacts.mkdir(parents=True)
        manifest = {"version": "3.10.0", "source_commit": stable.SOURCE, "published": False, "packages": [], "exclusions": [],
                    "external_package_exceptions": {name: "Reviewed external package" for name in packages.AUDITED_EXTERNAL_ELSA_IDS}}
        for package_id in fixture.REQUIRED:
            row = {"id": package_id, "frameworks": list(packages.FRAMEWORKS)}
            for suffix in ("nupkg", "snupkg"):
                name = f"{package_id}.3.10.0.{suffix}"
                data = name.encode()
                (artifacts / name).write_bytes(data)
                row[suffix] = name
                row[suffix + "_sha256"] = hashlib.sha256(data).hexdigest()
            manifest["packages"].append(row)
        fixture.write_json(content / "verified-artifacts.json", manifest)
        with patch.dict(candidate.os.environ, GITHUB_RUN_ID=str(stable.RUN), GITHUB_RUN_ATTEMPT=str(stable.ATTEMPT)):
            candidate.seal(content, stable.SOURCE)
        self.archive = self.root / "original.zip"
        with zipfile.ZipFile(self.archive, "w") as archive:
            for path in content.rglob("*"):
                if path.is_file():
                    archive.write(path, path.relative_to(content).as_posix())
        self.envelope = json.loads(stable.ORIGINAL_ENVELOPE.read_text())
        self.envelope.update(archive_sha256=fixture.sha256(self.archive), archive_size=self.archive.stat().st_size,
                             preupload_manifest_sha256=fixture.sha256(content / candidate.MANIFEST))
        self.original = self.root / "envelope.json"
        fixture.write_json(self.original, self.envelope)
        self.metadata = {"id": stable.ARTIFACT, "name": self.envelope["artifact_name"], "expired": False,
                         "expires_at": self.envelope["expires_at"], "size_in_bytes": self.envelope["archive_size"],
                         "digest": "sha256:" + self.envelope["archive_sha256"], "workflow_run": {"id": stable.RUN, "head_sha": stable.SOURCE}}
        self.producer_run = {"id": stable.RUN, "run_attempt": stable.ATTEMPT, "head_sha": stable.SOURCE,
                             "conclusion": "success", "event": "push", "path": ".github/workflows/prepare-consolidated-release-candidate.yml"}
        self.retrieval = {"schema": 1, "artifact_id": stable.ARTIFACT, "archive_sha256": self.envelope["archive_sha256"],
                          "retrieved_at": datetime.now(timezone.utc).isoformat()}
        for name, value in (("artifact", self.metadata), ("producer", self.producer_run), ("retrieval", self.retrieval)):
            fixture.write_json(self.root / (name + ".json"), value)
        for name, value in (("ORIGINAL_ENVELOPE", self.original), ("ENVELOPE_SHA256", fixture.sha256(self.original)),
                            ("ARCHIVE_SHA256", self.envelope["archive_sha256"]), ("MANIFEST_SHA256", self.envelope["preupload_manifest_sha256"])):
            context = patch.object(stable, name, value)
            context.start()
            self.addCleanup(context.stop)

    def verify(self):
        return stable.verify_target(self.archive, self.root / "artifact.json", self.root / "producer.json",
                                    self.root / "retrieval.json", self.root / "extracted", "b" * 40, 100, 2)

    def test_two_package_scope_and_distinct_execution_identity(self):
        target = self.verify()
        manifest, by_id, _, _ = target.verify(target.root, self.archive)
        self.assertEqual({name.casefold() for name in fixture.REQUIRED}, set(by_id))
        with self.assertRaisesRegex(ValueError, "missing required"):
            packages._validated_manifest(manifest)
        self.assertEqual(100, target.receipt_fields["matrix_execution"]["run_id"])
        self.assertEqual(stable.RUN, target.producer["run_id"])
        self.assertEqual(self.original.read_bytes(), target.retained_inputs[0][1].read_bytes())
        malformed = copy.deepcopy(manifest)
        malformed["packages"].append({"id": "Elsa.Unused", "frameworks": []})
        with self.assertRaises(ValueError):
            packages._validated_manifest(malformed, fixture.REQUIRED)

    def test_shared_candidate_verification_does_not_depend_on_sqlite_fixture(self):
        with patch.object(fixture, "FIXTURE", self.root / "no-sqlite-program"):
            provenance = candidate_input.verify_candidate_inputs(
                self.archive, self.root / "artifact.json", self.root / "producer.json",
                self.root / "retrieval.json", self.root / "extracted",
                original_envelope=self.original, envelope_sha256=stable.ENVELOPE_SHA256,
                archive_sha256=stable.ARCHIVE_SHA256, manifest_sha256=stable.MANIFEST_SHA256)
        self.assertEqual(stable.PRODUCER, provenance["candidate_producer"])
        self.assertEqual(stable.ENVELOPE_SHA256, provenance["original_envelope_sha256"])
        self.assertNotIn("matrix_execution", provenance)
        self.assertTrue((self.root / "extracted/verified-artifacts.json").is_file())

    def test_historical_expiry_blocks_under_future_clock_before_extraction(self):
        with candidate_clock(candidate, candidate_input, at=AFTER_EXPIRY), \
                self.assertRaisesRegex(ValueError, "Candidate artifact expired"):
            self.verify()
        self.assertFalse((self.root / "extracted").exists())

    def test_reader_credentials_cannot_enter_consumer_verification(self):
        for token in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN"):
            with self.subTest(token=token), patch.dict(candidate.os.environ, {token: "synthetic-reader-token"}), \
                    self.assertRaisesRegex(RuntimeError, "token must not enter"):
                self.verify()
            self.assertFalse((self.root / "extracted").exists())

    def test_mixed_target_identity_root_archive_manifest_and_package_bytes_fail(self):
        target = self.verify()
        for key, value in (("source_commit", "c" * 40), ("version", fixture.PROOF_VERSION), ("artifact_id", fixture.PROOF_ARTIFACT), ("run_id", fixture.PROOF_RUN)):
            with self.subTest(key=key), self.assertRaises(RuntimeError):
                replace(target, producer={**target.producer, key: value}).verify(target.root, self.archive)
        with self.assertRaises(RuntimeError):
            target.verify(self.root, self.archive)
        with self.assertRaises(RuntimeError):
            replace(target, receipt_fields={**target.receipt_fields, "candidate_producer": {**target.producer, "artifact_id": 1}}).verify(target.root, self.archive)
        with self.assertRaises(RuntimeError):
            replace(target, retained_inputs=(), producer={"version": fixture.PROOF_VERSION, "source_commit": fixture.PROOF_SOURCE,
                    "run_id": fixture.PROOF_RUN, "run_attempt": 1, "artifact_id": fixture.PROOF_ARTIFACT}).verify(target.root, self.archive)
        package = next((target.root / "artifacts").glob("*.nupkg"))
        package.write_bytes(b"changed")
        with self.assertRaisesRegex(RuntimeError, "differs"):
            target.verify(target.root, self.archive)

    def test_wrong_live_identity_expired_input_and_changed_envelope_fail_before_extract(self):
        for name, original, delta in (("artifact", self.metadata, {"expired": True}), ("artifact", self.metadata, {"id": 1}),
                                      ("producer", self.producer_run, {"run_attempt": 2}), ("retrieval", self.retrieval, {"artifact_id": 1}),
                                      ("retrieval", self.retrieval, {"retrieved_at": (datetime.now(timezone.utc) + timedelta(days=1)).isoformat()})):
            with self.subTest(delta=delta):
                path = self.root / (name + ".json")
                fixture.write_json(path, {**original, **delta})
                with self.assertRaises((RuntimeError, ValueError)):
                    self.verify()
                self.assertFalse((self.root / "extracted").exists())
                fixture.write_json(path, original)
        self.original.write_bytes(self.original.read_bytes() + b" ")
        with self.assertRaisesRegex(RuntimeError, "envelope bytes"):
            self.verify()

    def test_changed_loaded_dll_bytes_fail_existing_two_package_guard(self):
        project, cache = self.root / "consumer", self.root / "cache"
        asset = "lib/net8.0/Elsa.dll"
        dll = cache / "elsa/3.10.0" / asset
        dll.parent.mkdir(parents=True)
        dll.write_bytes(b"verified runtime")
        (project / "obj").mkdir(parents=True)
        fixture.write_json(project / "obj/project.assets.json", {"targets": {"net8.0": {"Elsa/3.10.0": {"runtime": {asset: {}}}}}})
        receipt = {"assemblies": [{"name": "Elsa", "sha256": fixture.sha256(dll), "location": str(project / "bin/Elsa.dll")}]}
        fixture.check_assemblies(receipt, project, cache, "net8.0")
        dll.write_bytes(b"changed runtime")
        with self.assertRaisesRegex(RuntimeError, "differs"):
            fixture.check_assemblies(receipt, project, cache, "net8.0")

    def test_candidate_role_does_not_follow_version_string(self):
        with self.assertRaisesRegex(RuntimeError, "Unknown baseline"):
            fixture.package_provenance(self.root, self.root, "net8.0", "3.10.0", self.root, {}, {}, set())

    def test_failure_retention_preserves_original_envelope_and_separate_observation(self):
        target = self.verify()
        output = self.root / "evidence"
        with patch.object(packages, "_run_command", side_effect=RuntimeError("private /host/path")), self.assertRaises(RuntimeError):
            fixture.run(target.root, self.archive, output, sorted(fixture.MATRIX), target=target)
        retained = output / "retained-evidence"
        self.assertEqual(self.original.read_bytes(), (retained / "original-envelope.json").read_bytes())
        public = json.loads((retained / "public-upgrade-proof.json").read_text())
        self.assertFalse(public["passed"])
        self.assertEqual(6, len(public["cells"]))
        self.assertEqual({"not_run"}, {cell["result"] for cell in public["cells"]})
        self.assertNotIn("private", json.dumps(public))
        self.assertNotIn("proof_run", public)



class StableCliContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.real = self.root / "real"
        inputs = self.real / "inputs"
        inputs.mkdir(parents=True)
        alias = self.root / "alias"
        alias.symlink_to(self.real, target_is_directory=True)
        for name in ("candidate.zip", "artifact.json", "producer-run.json", "live-retrieval.json"):
            (inputs / name).write_bytes(b"transport")
        self.source = "a" * 40
        self.argv = ["stable", "--inputs", str(alias / "inputs"), "--candidate-artifacts", str(alias / "candidate"),
                     "--output", str(alias / "evidence"), "--fixture-source", self.source, "--cell", "3.8.4/net8.0"]

    def test_alias_parent_preserves_effective_config_and_failure_log_containment(self):
        def execute(candidate_root, archive, output, selected, *, target):
            output.mkdir()
            cell = output / "3.8.4-net8.0"
            project, cache = cell / "baseline", cell / "baseline-packages"
            (project / "obj").mkdir(parents=True)
            cache.mkdir()
            assets = {"targets": {"net8.0": {}}, "project": {"restore": {"sources": {packages.NUGET_ORG: {}},
                      "configFilePaths": [str((project / "NuGet.Config").resolve())]}},
                      "packageFolders": {str(cache.resolve()): {}}}
            fixture.write_json(project / "obj/project.assets.json", assets)
            # Like NuGet, effective configuration uses the canonical path. The
            # empty package graph deliberately reaches the next strict guard.
            with self.assertRaisesRegex(RuntimeError, "Required fixture packages missing"):
                fixture.package_provenance(project, cache, "net8.0", "3.8.4", candidate_root, {}, {}, set())
            log = project / "restore.log"
            log.write_text("safe diagnostic")
            result = {"complete_matrix": False, "passed": False, "cells": [{"baseline": "3.8.4", "framework": "net8.0",
                      "passed": False, "phases": {}, "commands": [{"command": ["dotnet", "restore"], "log": str(log.resolve()),
                      "exit_code": 0, "timed_out": False}]}]}
            fixture.write_json(output / "public-upgrade-proof.json", {})
            fixture.stage_evidence(output, result)
            retained = json.loads((output / "retained-evidence/retention-manifest.json").read_text())
            self.assertTrue(retained["retention_complete"])

        with patch.object(sys, "argv", self.argv), patch.object(packages.subprocess, "check_output", return_value=self.source + "\n"), \
             patch.object(stable, "verify_target", return_value=object()) as verify, patch.object(fixture, "run", side_effect=execute):
            stable.main()
        arguments = verify.call_args.args
        for path in arguments[:5]:
            self.assertEqual(path, path.resolve())
        self.assertEqual(self.real / "candidate", arguments[4])

    def test_symlinked_transport_root_is_rejected_before_resolution(self):
        linked = self.root / "linked-inputs"
        linked.symlink_to(self.real / "inputs", target_is_directory=True)
        self.argv[2] = str(linked)
        with patch.object(sys, "argv", self.argv), patch.object(packages.subprocess, "check_output", return_value=self.source), \
             patch.object(stable, "verify_target") as verify, self.assertRaisesRegex(RuntimeError, "transport layout"):
            stable.main()
        verify.assert_not_called()


class StableWorkflowContracts(unittest.TestCase):
    def test_fixed_token_boundary_same_run_layout_and_full_matrix(self):
        text = (ROOT / ".github/workflows/stable-persisted-workflow-upgrade-proof.yml").read_text()
        retrieve, execution = text.split("  proof:\n", 1)
        self.assertNotIn("pull_request", text)
        self.assertIn("workflow_dispatch:", text)
        branches = text.split("    branches:\n", 1)[1].split("    paths:\n", 1)[0]
        self.assertEqual(branches.splitlines(), ["      - 'codex/**'"])
        for forbidden in ("actions/checkout", "scripts/", "python", "node", "secrets."):
            self.assertNotIn(forbidden, retrieve.split("  retrieve:\n", 1)[1])
        for value in (str(stable.ARTIFACT), str(stable.RUN), stable.SOURCE, stable.ARCHIVE_SHA256):
            self.assertIn(value, retrieve)
        self.assertIn("actions/runs/37456860080/attempts/1", retrieve)
        self.assertIn(".run_attempt == 1", retrieve)
        self.assertIn("actions: none", execution)
        self.assertIn("ref: ${{ github.sha }}", execution)
        self.assertIn("persist-credentials: false", execution)
        self.assertIn("artifact-ids: ${{ needs.retrieve.outputs.input-artifact-id }}", execution)
        self.assertIn("merge-multiple: true", execution)
        for forbidden in ("github-token:", "GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN", "run-id:", "repository:", "--cell"):
            self.assertNotIn(forbidden, execution)
        for name in ("candidate.zip", "artifact.json", "producer-run.json", "live-retrieval.json"):
            self.assertIn("${{ runner.temp }}/upgrade-input/" + name, retrieve)
        self.assertIn('--fixture-source "${{ github.sha }}"', execution)
        self.assertIn('--fixture-run "${{ github.run_id }}"', execution)
        self.assertIn('--fixture-attempt "${{ github.run_attempt }}"', execution)

    def test_transport_rejects_extra_nested_missing_and_symlinked_entries(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            names = ("candidate.zip", "artifact.json", "producer-run.json", "live-retrieval.json")
            for name in names:
                (directory / name).write_bytes(b"transport")
            stable.validate_transport(directory)
            extra = directory / "extra"
            extra.write_bytes(b"unexpected")
            with self.assertRaisesRegex(RuntimeError, "layout"):
                stable.validate_transport(directory)
            extra.unlink()
            extra.mkdir()
            with self.assertRaisesRegex(RuntimeError, "layout"):
                stable.validate_transport(directory)
            extra.rmdir()
            member = directory / "artifact.json"
            member.unlink()
            with self.assertRaisesRegex(RuntimeError, "layout"):
                stable.validate_transport(directory)
            member.symlink_to(directory / "producer-run.json")
            with self.assertRaisesRegex(RuntimeError, "layout"):
                stable.validate_transport(directory)

    def test_accepted_envelope_and_lifecycle_fixture_are_verbatim(self):
        # Pins here are independent of any test fixture patches.
        self.assertEqual("ef5e772020b151089b64b90b819d36ff371ed5e65212f7fdc7b2934d9860ddda", fixture.sha256(stable.ORIGINAL_ENVELOPE))
        self.assertEqual(stable.FIXTURE_SHA256, fixture.sha256(fixture.FIXTURE))


if __name__ == "__main__":
    unittest.main()
