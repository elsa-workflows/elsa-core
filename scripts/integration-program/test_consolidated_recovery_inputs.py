from datetime import datetime, timedelta, timezone
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile

import consolidated_candidate_input as candidate_input
import consolidated_recovery_inputs as recovery
import prepare_consolidated_release_candidate as candidate
import prove_consolidated_package_consumers as consumers
import prove_consolidated_packages as packages


ROOT = Path(__file__).resolve().parents[2]
BASELINE = json.loads((ROOT / "scripts/integration-program/consolidated-candidate-inventory-baseline.json").read_text())
FRAMEWORKS = ("net8.0", "net9.0", "net10.0")


def json_bytes(value: dict) -> bytes:
    return (json.dumps(value, sort_keys=True, separators=(",", ":")) + "\n").encode("utf-8")


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


class RecoveryInputTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.inputs = self.root / "transport"
        self.inputs.mkdir()
        self.content = self.root / "candidate-content"
        self.artifacts = self.content / "artifacts"
        self.artifacts.mkdir(parents=True)
        self.destination = self.root / "verified"
        self.manifest = self.make_manifest()
        self.write_package_artifacts()
        (self.content / "verified-artifacts.json").write_bytes(json_bytes(self.manifest))
        self.write_transport()
        self.rewrite_outer_archive()
        self.patch_constants()
        self.verify_patch = patch.object(candidate_input, "verify_candidate_inputs", side_effect=self.fake_verify)
        self.verify_mock = self.verify_patch.start()
        self.addCleanup(self.verify_patch.stop)

    def make_manifest(self):
        rows = []
        for index, source_row in enumerate(sorted(BASELINE["packages"], key=lambda row: row["project"])):
            package_id = source_row["id"]
            frameworks = list(FRAMEWORKS) if package_id in consumers.REQUIRED_PACKAGES else ["net8.0"]
            assembly_name = f"RecoveryAssembly{index:03d}"
            framework_properties = {
                framework: {
                    "assembly_name": assembly_name,
                    "package_version": candidate_input.PRODUCER["version"],
                    "include_build_output": True,
                    "manifest_required": False,
                    "manifest_path": "",
                }
                for framework in frameworks
            }
            expected_groups = [
                {"framework": framework, "dependencies": []}
                for framework in sorted(frameworks)
            ]
            rows.append({
                "id": package_id,
                "project": source_row["project"],
                "assembly_name": assembly_name,
                "frameworks": frameworks,
                "include_build_output": True,
                "include_symbols": True,
                "is_tool": False,
                "symbol_format": "snupkg",
                "nupkg": f"{package_id}.{candidate_input.PRODUCER['version']}.nupkg",
                "snupkg": f"{package_id}.{candidate_input.PRODUCER['version']}.snupkg",
                "framework_properties": framework_properties,
                "expected_dependency_groups": expected_groups,
                "expected_symbol_dependency_groups": expected_groups,
            })
        projects = [row["project"] for row in rows] + [row["project"] for row in BASELINE["exclusions"]]
        return {
            "mode": "candidate",
            "configuration": "Release",
            "version": candidate_input.PRODUCER["version"],
            "source_commit": candidate_input.PRODUCER["source_commit"],
            "repository_url": packages.CORE_URL,
            "published": False,
            "packages": rows,
            "exclusions": copy.deepcopy(BASELINE["exclusions"]),
            "evaluations": [{"project": project} for project in projects],
            "external_package_exceptions": dict(packages.EXTERNAL_PACKAGES),
            "icon_sha256": sha256(b"fixture-icon"),
            "browser_assets": [],
            "build_inputs": {"configuration": "Release", "mode": "candidate"},
        }

    def nuspec(self, row):
        root = ET.Element("package")
        metadata = ET.SubElement(root, "metadata")
        for name, value in (("id", row["id"]), ("version", self.manifest["version"]),
                            ("projectUrl", packages.CORE_URL), ("icon", "icon.png")):
            ET.SubElement(metadata, name).text = value
        ET.SubElement(metadata, "repository", {
            "type": "git", "url": packages.CORE_URL, "commit": self.manifest["source_commit"],
        })
        dependencies = ET.SubElement(metadata, "dependencies")
        for framework in sorted(row["frameworks"]):
            ET.SubElement(dependencies, "group", {"targetFramework": framework})
        return ET.tostring(root)

    def write_package_artifacts(self):
        for row in self.manifest["packages"]:
            nuspec = self.nuspec(row)
            with zipfile.ZipFile(self.artifacts / row["nupkg"], "w", zipfile.ZIP_DEFLATED) as archive:
                archive.writestr(f"{row['id']}.nuspec", nuspec)
                archive.writestr("icon.png", b"fixture-icon")
                for framework in row["frameworks"]:
                    archive.writestr(f"lib/{framework}/{row['assembly_name']}.dll", b"assembly")
            with zipfile.ZipFile(self.artifacts / row["snupkg"], "w", zipfile.ZIP_DEFLATED) as archive:
                archive.writestr(f"{row['id']}.symbols.nuspec", nuspec)
                for framework in row["frameworks"]:
                    archive.writestr(f"lib/{framework}/{row['assembly_name']}.pdb", b"symbols")
            nupkg = (self.artifacts / row["nupkg"]).read_bytes()
            snupkg = (self.artifacts / row["snupkg"]).read_bytes()
            row["nupkg_sha256"] = sha256(nupkg)
            row["nupkg_sha512"] = hashlib.sha512(nupkg).hexdigest()
            row["snupkg_sha256"] = sha256(snupkg)

    def write_transport(self):
        for name, value in (
            ("artifact.json", {"id": candidate_input.PRODUCER["artifact_id"]}),
            ("producer-run.json", {"id": candidate_input.PRODUCER["run_id"]}),
            ("live-retrieval.json", {
                "schema": 1,
                "artifact_id": candidate_input.PRODUCER["artifact_id"],
                "archive_sha256": "0" * 64,
                "retrieved_at": datetime.now(timezone.utc).isoformat(),
            }),
        ):
            (self.inputs / name).write_bytes(json_bytes(value))

    def rewrite_outer_archive(self):
        manifest_rows = []
        for path in sorted(self.content.rglob("*")):
            if path.is_file() and path.name != candidate.MANIFEST:
                data = path.read_bytes()
                manifest_rows.append({"path": path.relative_to(self.content).as_posix(),
                                      "size": len(data), "sha256": sha256(data)})
        preupload = {
            "schema": 1, "published": False, "version": candidate_input.PRODUCER["version"],
            "source_commit": candidate_input.PRODUCER["source_commit"],
            "run_id": candidate_input.PRODUCER["run_id"],
            "run_attempt": candidate_input.PRODUCER["run_attempt"], "files": manifest_rows,
        }
        (self.content / candidate.MANIFEST).write_bytes(json_bytes(preupload))
        archive_path = self.inputs / "candidate.zip"
        with zipfile.ZipFile(archive_path, "w", zipfile.ZIP_DEFLATED) as archive:
            for path in sorted(self.content.rglob("*")):
                if path.is_file():
                    archive.write(path, path.relative_to(self.content).as_posix())
        self.archive_sha256 = sha256(archive_path.read_bytes())
        self.archive_size = archive_path.stat().st_size
        self.manifest_sha256 = sha256((self.content / candidate.MANIFEST).read_bytes())
        original_retrieved_at = (datetime.now(timezone.utc) - timedelta(seconds=2)).isoformat()
        envelope = {
            **candidate_input.PRODUCER,
            "artifact_name": f"consolidated-candidate-{candidate_input.PRODUCER['source_commit']}-"
                             f"{candidate_input.PRODUCER['run_id']}-{candidate_input.PRODUCER['run_attempt']}",
            "archive_sha256": self.archive_sha256,
            "archive_size": self.archive_size,
            "preupload_manifest_sha256": self.manifest_sha256,
            "retrieved_at": original_retrieved_at,
            "expires_at": (datetime.now(timezone.utc) + timedelta(days=30)).isoformat(),
        }
        self.original_envelope = self.root / "original-envelope.json"
        self.original_envelope.write_bytes(json_bytes(envelope))
        self.original_envelope_sha256 = sha256(self.original_envelope.read_bytes())
        metadata = {
            "id": candidate_input.PRODUCER["artifact_id"], "name": envelope["artifact_name"],
            "expired": False, "expires_at": envelope["expires_at"],
            "size_in_bytes": self.archive_size, "digest": "sha256:" + self.archive_sha256,
            "workflow_run": {"id": candidate_input.PRODUCER["run_id"],
                             "head_sha": candidate_input.PRODUCER["source_commit"]},
        }
        (self.inputs / "artifact.json").write_bytes(json_bytes(metadata))
        run = {
            "id": candidate_input.PRODUCER["run_id"],
            "run_attempt": candidate_input.PRODUCER["run_attempt"],
            "head_sha": candidate_input.PRODUCER["source_commit"],
            "conclusion": "success", "event": "push",
            "path": ".github/workflows/prepare-consolidated-release-candidate.yml",
        }
        (self.inputs / "producer-run.json").write_bytes(json_bytes(run))
        retrieval = {
            "schema": 1, "artifact_id": candidate_input.PRODUCER["artifact_id"],
            "archive_sha256": self.archive_sha256,
            "retrieved_at": datetime.now(timezone.utc).isoformat(),
        }
        (self.inputs / "live-retrieval.json").write_bytes(json_bytes(retrieval))

    def patch_constants(self):
        for name, value in (
            ("ORIGINAL_ENVELOPE", self.original_envelope),
            ("ENVELOPE_SHA256", self.original_envelope_sha256),
            ("ARCHIVE_SHA256", self.archive_sha256),
            ("MANIFEST_SHA256", self.manifest_sha256),
        ):
            context = patch.object(candidate_input, name, value)
            context.start()
            self.addCleanup(context.stop)

    def fake_verify(self, archive, metadata_path, producer_run_path, retrieval_path, destination, **kwargs):
        self.assertEqual(candidate_input.PRODUCER, kwargs["producer"])
        self.assertEqual(self.archive_sha256, kwargs["archive_sha256"])
        destination.mkdir(parents=True)
        with zipfile.ZipFile(archive) as source:
            for member in source.infolist():
                if member.is_dir():
                    (destination / member.filename).mkdir(parents=True, exist_ok=True)
                else:
                    path = destination / member.filename
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_bytes(source.read(member))
        return {
            "candidate_producer": dict(candidate_input.PRODUCER),
            "original_envelope_sha256": self.original_envelope_sha256,
            "preupload_manifest_sha256": self.manifest_sha256,
            "live_retrieval_sha256": sha256(retrieval_path.read_bytes()),
        }

    def test_planner_identity_git_child_receives_no_credentials(self):
        environment = {"PATH": "/usr/bin:/bin", "ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY": "synthetic-publisher",
                       "ELSA_CONSOLIDATED_METADATA_READ_TOKEN": "synthetic-metadata", "GH_TOKEN": "synthetic-artifact"}
        with patch.dict(os.environ, environment, clear=True), \
                patch.object(recovery.subprocess, "check_output", return_value="a" * 40) as child:
            recovery._validate_planner_identity("a" * 40, 99, 1)
        self.assertEqual(child.call_args.kwargs["env"], {"PATH": "/usr/bin:/bin"})
        self.assertEqual(child.call_args.kwargs["timeout"], 10)

    def run_prepare(self, **kwargs):
        planner_source = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        return recovery.prepare_recovery_inputs(self.inputs, self.destination,
                                                planner_source=planner_source, **kwargs)

    def rebuild_with_current_manifest(self):
        (self.content / "verified-artifacts.json").write_bytes(json_bytes(self.manifest))
        self.rewrite_outer_archive()
        self.patch_constants()

    def test_valid_inputs_recheck_all_225_pairs_and_emit_bound_provenance(self):
        root, provenance = self.run_prepare(planner_run=99, planner_attempt=2)
        self.assertEqual(self.destination, root)
        self.assertEqual(225, len(json.loads((root / "verified-artifacts.json").read_text())["packages"]))
        self.assertEqual(candidate_input.PRODUCER, provenance["candidate_producer"])
        self.assertEqual(self.archive_sha256, provenance["archive_sha256"])
        self.assertEqual(self.archive_size, provenance["archive_size"])
        self.assertEqual(self.original_envelope_sha256, provenance["original_envelope_sha256"])
        self.assertEqual(self.manifest_sha256, provenance["preupload_manifest_sha256"])
        self.assertEqual(sha256((root / "verified-artifacts.json").read_bytes()), provenance["verified_artifacts_sha256"])
        self.assertEqual({"source_commit": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
                          "run_id": 99, "run_attempt": 2}, provenance["planner"])

    def test_missing_symbol_pair_is_rejected_and_extraction_is_removed(self):
        self.manifest["packages"][0]["snupkg"] = None
        self.rebuild_with_current_manifest()
        with self.assertRaisesRegex(ValueError, "pair is incomplete"):
            self.run_prepare()
        self.assertFalse(self.destination.exists())

    def test_inventory_change_is_rejected_and_extraction_is_removed(self):
        self.manifest["exclusions"][0]["reason"] = "changed"
        self.rebuild_with_current_manifest()
        with self.assertRaisesRegex(ValueError, "differ from the accepted inventory"):
            self.run_prepare()
        self.assertFalse(self.destination.exists())

    def test_recomputed_archive_hash_must_match_retained_manifest(self):
        self.manifest["packages"][0]["nupkg_sha256"] = "0" * 64
        self.rebuild_with_current_manifest()
        with self.assertRaisesRegex(ValueError, "differ from retained nupkg_sha256"):
            self.run_prepare()
        self.assertFalse(self.destination.exists())

    def test_transport_json_is_bounded_before_generic_validation(self):
        (self.inputs / "artifact.json").write_text(json.dumps({"padding": "x" * (recovery.MAX_TRANSPORT_JSON_BYTES + 10)}))
        with self.assertRaisesRegex(ValueError, "JSON size limit"):
            self.run_prepare()
        self.verify_mock.assert_not_called()
        self.assertFalse(self.destination.exists())

    def test_duplicate_transport_json_keys_are_rejected(self):
        (self.inputs / "artifact.json").write_text('{"id":1,"id":2}')
        with self.assertRaisesRegex(ValueError, "not valid UTF-8 JSON"):
            self.run_prepare()
        self.verify_mock.assert_not_called()

    def test_planner_source_must_be_exact_checkout_head_and_run_identity_is_paired(self):
        with self.assertRaisesRegex(ValueError, "does not match the exact checkout HEAD"):
            recovery.prepare_recovery_inputs(self.inputs, self.destination,
                                             planner_source="f" * 40)
        with self.assertRaisesRegex(ValueError, "supplied together"):
            self.run_prepare(planner_run=12)
        with self.assertRaisesRegex(ValueError, "positive integers"):
            self.run_prepare(planner_run=True, planner_attempt=1)
        self.assertFalse(self.destination.exists())

    def test_destination_symlink_or_input_overlap_is_rejected(self):
        planner_source = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        link = self.root / "broken"
        link.symlink_to(self.root / "missing")
        with self.assertRaisesRegex(ValueError, "symlink"):
            recovery.prepare_recovery_inputs(self.inputs, link, planner_source=planner_source)
        with self.assertRaisesRegex(ValueError, "overlap"):
            recovery.prepare_recovery_inputs(self.inputs, self.inputs / "nested", planner_source=planner_source)
        self.assertFalse((self.inputs / "nested").exists())
        alias = self.root / "alias"
        alias.symlink_to(self.root, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, "parent traversal"):
            recovery.prepare_recovery_inputs(alias / ".." / "transport", self.root / "elsewhere",
                                             planner_source=planner_source)

    def test_outer_archive_member_and_uncompressed_bounds_fail_before_extraction(self):
        with patch.object(recovery, "MAX_ARCHIVE_MEMBERS", 2):
            with self.assertRaisesRegex(ValueError, "too many members"):
                self.run_prepare()
        with patch.object(recovery, "MAX_ARCHIVE_BYTES", 1):
            with self.assertRaisesRegex(ValueError, "compressed size limit"):
                self.run_prepare()
        with patch.object(recovery, "MAX_TOTAL_UNCOMPRESSED_BYTES", 1):
            with self.assertRaisesRegex(ValueError, "total uncompressed size limit"):
                self.run_prepare()
        self.assertFalse(self.destination.exists())


if __name__ == "__main__":
    unittest.main()
