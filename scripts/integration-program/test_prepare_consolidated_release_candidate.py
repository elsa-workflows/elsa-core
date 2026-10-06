import copy
from datetime import datetime, timedelta, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile

import prepare_consolidated_release_candidate as candidate
import prove_consolidated_packages as packages

ROOT = Path(__file__).resolve().parents[2]
SOURCE = "a" * 40


class CandidateArchiveTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.output = self.root / "candidate"
        self.output.mkdir()
        (self.output / "artifacts").mkdir()
        (self.output / "artifacts/Elsa.3.10.0.nupkg").write_bytes(b"candidate package")
        (self.output / "receipt.json").write_text('{"published":false}\n')
        with patch.dict(os.environ, GITHUB_RUN_ID="42", GITHUB_RUN_ATTEMPT="2"):
            candidate.seal(self.output, SOURCE)
        self.archive = self.root / "original.zip"
        self.make_archive()
        self.envelope = {"schema": 1, "published": False, "version": "3.10.0", "source_commit": SOURCE,
                         "run_id": 42, "run_attempt": 2, "artifact_id": 123,
                         "retrieved_at": datetime.now(timezone.utc).isoformat(),
                         "artifact_name": f"consolidated-candidate-{SOURCE}-42-2", "retention_days": 30,
                         "expires_at": (datetime.now(timezone.utc) + timedelta(days=30)).isoformat(),
                         "preupload_manifest_sha256": candidate.file_sha256(self.output / candidate.MANIFEST)}
        self.metadata = {"id": 123, "name": self.envelope["artifact_name"], "expired": False,
                         "expires_at": self.envelope["expires_at"], "workflow_run": {"id": 42, "head_sha": SOURCE}}
        self.bind_archive()

    def make_archive(self, *, extra=None):
        with zipfile.ZipFile(self.archive, "w") as archive:
            for path in sorted(self.output.rglob("*")):
                if path.is_file():
                    archive.write(path, path.relative_to(self.output).as_posix())
            if extra:
                archive.writestr(*extra)

    def bind_archive(self):
        digest = candidate.file_sha256(self.archive)
        self.envelope.update(archive_sha256=digest, archive_size=self.archive.stat().st_size)
        self.metadata.update(digest="sha256:" + digest, size_in_bytes=self.archive.stat().st_size)

    def extract(self):
        return candidate.verify_and_extract(self.archive, self.envelope, self.metadata, self.root / "extracted",
                                            source=SOURCE, run_id=42, attempt=2)

    def assert_rejected_before_extraction(self, reason):
        with self.assertRaisesRegex((ValueError, FileNotFoundError), reason):
            self.extract()
        self.assertFalse((self.root / "extracted").exists())

    def test_original_bytes_verified_and_envelope_stays_outside_archive(self):
        manifest = self.extract()
        self.assertEqual(SOURCE, manifest["source_commit"])
        self.assertEqual(b"candidate package", (self.root / "extracted/artifacts/Elsa.3.10.0.nupkg").read_bytes())
        self.assertNotIn(candidate.MANIFEST, {row["path"] for row in manifest["files"]})
        self.assertFalse(any(key in manifest for key in ("artifact_id", "archive_sha256", "archive_size", "expires_at")))
        with self.assertRaisesRegex(ValueError, "reseal"):
            candidate.seal(self.output, SOURCE)

    def test_future_or_timezone_free_retrieval_snapshot_fails(self):
        for value in ((datetime.now(timezone.utc) + timedelta(minutes=1)).isoformat(), "2026-10-06T12:00:00"):
            self.envelope["retrieved_at"] = value
            with self.subTest(value=value):
                self.assert_rejected_before_extraction("retrieval snapshot")

    def test_original_archive_corruption_and_size_fail(self):
        self.archive.write_bytes(self.archive.read_bytes() + b"changed")
        self.assert_rejected_before_extraction("archive hash/size")

    def test_artifact_id_source_run_attempt_and_name_mismatches_fail(self):
        for key, value in (("artifact_id", 999), ("source_commit", "b" * 40), ("run_id", 99),
                           ("run_attempt", 1), ("artifact_name", "proof-42-2"), ("version", "3.10.0-proof.42.2"), ("published", True)):
            original = self.envelope[key]
            with self.subTest(key=key):
                self.envelope[key] = value
                self.assert_rejected_before_extraction("identity|mismatch")
            self.envelope[key] = original

    def test_expired_missing_or_deleted_artifact_fails(self):
        original = copy.deepcopy(self.metadata)
        for delta in ({"expired": True}, {"id": None}, {"workflow_run": {}}, {"digest": None}):
            with self.subTest(delta=delta):
                self.metadata.update(delta)
                self.assert_rejected_before_extraction("mismatch|missing|expired")
            self.metadata = copy.deepcopy(original)
        expired = (datetime.now(timezone.utc) - timedelta(seconds=1)).isoformat()
        self.metadata["expires_at"] = self.envelope["expires_at"] = expired
        self.assert_rejected_before_extraction("expired")

    def test_wrong_retention_manifest_hash_and_metadata_archive_hash_fail(self):
        for key, value in (("retention_days", 7), ("preupload_manifest_sha256", "b" * 64), ("archive_sha256", "c" * 64)):
            original = self.envelope[key]
            with self.subTest(key=key):
                self.envelope[key] = value
                self.assert_rejected_before_extraction("retention|hash|digest")
            self.envelope[key] = original

    def test_file_hash_change_fails_even_with_valid_archive_identity(self):
        (self.output / "receipt.json").write_text("changed")
        self.make_archive()
        self.bind_archive()
        self.assert_rejected_before_extraction("file hash")

    def test_missing_and_unlisted_file_fails_even_with_valid_archive_identity(self):
        self.make_archive(extra=("extra", b"unlisted"))
        self.bind_archive()
        self.assert_rejected_before_extraction("unlisted")
        (self.output / "receipt.json").unlink()
        self.make_archive()
        self.bind_archive()
        self.assert_rejected_before_extraction("Missing/unlisted")

    def test_unsafe_traversal_absolute_duplicate_or_symlink_fails(self):
        symlink = zipfile.ZipInfo("linked")
        symlink.create_system = 3
        symlink.external_attr = (stat.S_IFLNK | 0o777) << 16
        for name in ("../escaped", "/absolute", "a\\b", "artifacts/Elsa.3.10.0.nupkg", symlink):
            with self.subTest(name=name):
                self.make_archive(extra=(name, b"target"))
                self.bind_archive()
                self.assert_rejected_before_extraction("Unsafe|Duplicate")

    def test_duplicate_or_circular_file_manifest_fails(self):
        path = self.output / candidate.MANIFEST
        original = json.loads(path.read_text())
        for row in (original["files"][0], {"path": candidate.MANIFEST, "size": 0, "sha256": "a" * 64}):
            manifest = copy.deepcopy(original)
            manifest["files"].append(row)
            path.write_text(json.dumps(manifest))
            self.envelope["preupload_manifest_sha256"] = candidate.file_sha256(path)
            self.make_archive()
            self.bind_archive()
            with self.subTest(row=row):
                self.assert_rejected_before_extraction("Duplicate or circular")


class CandidateContractsTests(unittest.TestCase):
    def test_workflow_python_imports_preserve_a_fresh_committed_checkout(self):
        workflow = (ROOT / ".github/workflows/prepare-consolidated-release-candidate.yml").read_text()
        self.assertRegex(workflow, r"(?m)^env:\n(?:  #[^\n]*\n)*  PYTHONDONTWRITEBYTECODE: '1'\n")
        with tempfile.TemporaryDirectory() as temporary:
            checkout = Path(temporary)
            for name in ("prepare_consolidated_release_candidate.py", "prove_consolidated_packages.py",
                         "prove_consolidated_package_consumers.py"):
                shutil.copy2(ROOT / "scripts/integration-program" / name, checkout / name)
            def execute(command, **kwargs):
                return subprocess.run(command, cwd=checkout, check=True, capture_output=True, text=True, **kwargs)
            execute(["git", "init", "--quiet"])
            execute(["git", "add", "."])
            execute(["git", "-c", "user.name=Candidate contract", "-c", "user.email=candidate@example.invalid",
                     "commit", "--quiet", "-m", "Clean candidate checkout"])
            # Use the workflow's environment, without the locally used Python -B flag.
            execute([sys.executable, "-c", "from pathlib import Path; import prepare_consolidated_release_candidate; "
                     "import prove_consolidated_package_consumers; import prove_consolidated_packages; "
                     "prove_consolidated_packages.clean_head(Path.cwd())"],
                    env={**os.environ, "PYTHONDONTWRITEBYTECODE": "1"})
            self.assertEqual("", execute(["git", "status", "--porcelain"]).stdout)
            self.assertFalse(list(checkout.rglob("__pycache__")))

    def test_candidate_and_proof_versions_are_distinct(self):
        packages.validate_mode("3.10.0", "candidate")
        packages.validate_mode("3.10.0-proof.1.1", "proof")
        for mode, versions in (("candidate", ("3.10.0-proof.1.1", "3.10.1", "3.10.0-rc1", "1.0.1")),
                               ("proof", ("3.10.0", "3.10.0-proof.0.1", "3.10.0-proof.1.0"))):
            for version in versions:
                with self.subTest(mode=mode, version=version), self.assertRaises(ValueError):
                    packages.validate_mode(version, mode)

    def test_evaluator_sets_common_version_configuration_and_distinct_flags(self):
        for mode in ("candidate", "proof"):
            version = "3.10.0" if mode == "candidate" else "3.10.0-proof.1.1"
            with patch.object(packages, "run", return_value='{"Properties":{}}') as run:
                packages.evaluate(ROOT, ROOT / "example.csproj", version, True, mode=mode)
            command = run.call_args.args[0]
            self.assertIn("-p:Configuration=Release", command)
            self.assertIn(f"-p:Version={version}", command)
            self.assertIn(f"-p:PackageVersion={version}", command)
            self.assertIn(f"-p:ConsolidatedPackageProof={str(mode == 'proof').lower()}", command)
            self.assertIn(f"-p:ConsolidatedReleaseCandidate={str(mode == 'candidate').lower()}", command)

    def test_msbuild_candidate_validates_restore_compile_pack_and_mutual_exclusion(self):
        document = ET.parse(ROOT / "build/ConsolidatedPackageProof.targets")
        proof = document.find(".//Target[@Name='ValidateConsolidatedPackageProofVersion']")
        self.assertIn("proof", proof.find("Error").get("Condition"))
        target = document.find(".//Target[@Name='ValidateConsolidatedReleaseCandidate']")
        self.assertEqual("Restore;PrepareForBuild;Pack;GenerateNuspec", target.get("BeforeTargets"))
        errors = [node.get("Condition") for node in target.findall("Error")]
        self.assertIn("'$(ConsolidatedPackageProof)' == 'true'", errors)
        self.assertIn("'$(Version)' != '3.10.0' or '$(PackageVersion)' != '3.10.0' or '$(Configuration)' != 'Release'", errors)
        build = (ROOT / "build/Build.cs").read_text()
        for start, end in (("RestoreSettings", "CompileSettings"), ("CompileSettings", "PackSettings"), ("PackSettings", "TestProjects")):
            section = build.split(start, 1)[1].split(end, 1)[0]
            self.assertIn('SetProperty("Version", Version)', section)
            self.assertIn('SetProperty("PackageVersion", Version)', section)
            self.assertIn('SetProperty("Configuration", "Release")', section)

    def test_inventory_changes_emit_reviewable_diff_and_fail_before_build(self):
        baseline = json.loads((ROOT / "scripts/integration-program/consolidated-candidate-inventory-baseline.json").read_text())
        manifest = copy.deepcopy(baseline)
        for row in manifest["packages"]:
            row["nupkg"] = row["id"] + ".3.10.0.nupkg"
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "diff.json"
            candidate.compare_inventory(manifest, baseline, output)
            self.assertFalse(any(delta["added"] or delta["removed"] for delta in json.loads(output.read_text())["changes"].values()))
            for category, change in (("packages", lambda data: data["packages"].pop()),
                                     ("packages", lambda data: data["packages"][0].update(id="Elsa.Unexpected")),
                                     ("exclusions", lambda data: data["exclusions"][0].update(reason="new reason"))):
                changed = copy.deepcopy(manifest)
                change(changed)
                with self.subTest(category=category), self.assertRaisesRegex(ValueError, "inventory changed"):
                    candidate.compare_inventory(changed, baseline, output)
                self.assertTrue(json.loads(output.read_text())["changes"][category]["removed"])
            sample = next(row for row in manifest["packages"] if row["id"] == "Elsa.SamplePackage")
            sample["nupkg"] = "Elsa.SamplePackage.1.0.1.nupkg"
            with self.assertRaisesRegex(ValueError, "SamplePackage"):
                candidate.compare_inventory(manifest, baseline, output)

    def test_dedicated_workflow_has_no_publisher_or_privileged_consumer_route(self):
        workflow = (ROOT / ".github/workflows/prepare-consolidated-release-candidate.yml").read_text()
        self.assertNotIn("pull_request:", workflow)
        self.assertIn("permissions: {}", workflow)
        self.assertEqual(["prepare", "retrieve", "consumers"], re.findall(r"^  ([a-z]+):$", workflow.split("jobs:", 1)[1], re.M))
        for denied in ("secrets.", "packages: write", "deployments:", "environment:", "nuget push", "npm publish", "gh release", "publish_preview", "publish_nuget", "workflow_call:", "workflow_run:"):
            self.assertNotIn(denied, workflow)
        retrieve = workflow.split("  retrieve:")[1].split("  consumers:")[0]
        self.assertNotIn("checkout@", retrieve)
        self.assertNotIn("python", retrieve)
        self.assertIn("GH_TOKEN: ${{ github.token }}", retrieve)
        self.assertIn("actions: read", retrieve)
        for section in (workflow.split("  prepare:")[1].split("  retrieve:")[0], workflow.split("  consumers:")[1]):
            self.assertNotIn("GH_TOKEN", section)
            self.assertNotIn("github.token", section)
            self.assertIn("actions: none", section)
            self.assertIn("persist-credentials: false", section)
        self.assertIn("artifact-ids: ${{ needs.retrieve.outputs.transport-id }}", workflow)
        self.assertIn("merge-multiple: true", workflow)
        self.assertIn("retention-days: 30", workflow)
        self.assertIn('>> "$GITHUB_STEP_SUMMARY"', workflow)
        self.assertIn("EXPECTED_ENVELOPE: ${{ needs.retrieve.outputs.envelope }}", workflow)
        self.assertIn("retrieved_at:$observed", workflow)
        runner = (ROOT / "scripts/integration-program/prepare_consolidated_release_candidate.py").read_text()
        self.assertIn('"scope": "retrieval_time_snapshot"', runner)
        self.assertIn("deletion after retrieval is not observed", runner)
        self.assertIn("Before approval or publication, recheck live original artifact ID", runner)

    def test_candidate_profile_is_core_owned_feedz_only_and_maintenance_is_preserved(self):
        references = ROOT / ".agents/skills/elsa-release/references"
        profile = json.loads((references / "consolidated-feedz-profile.json").read_text())
        self.assertTrue(profile["candidate_only"])
        self.assertEqual("3.10.0", profile["version"])
        self.assertEqual(["core"], [repository["name"] for repository in profile["repositories"]])
        self.assertEqual({}, profile["repositories"][0]["fixed_packages"])
        self.assertEqual(["feedz"], [feed["name"] for feed in profile["feeds"]])
        self.assertEqual("undecided", profile["npm_ownership"])
        legacy = json.loads((references / "elsa-profile.json").read_text())
        self.assertEqual(["core", "studio", "extensions", "templates"], [repository["name"] for repository in legacy["repositories"]])
        self.assertEqual("1.0.1", legacy["repositories"][0]["fixed_packages"]["Elsa.SamplePackage"]["version"])


if __name__ == "__main__":
    unittest.main()
