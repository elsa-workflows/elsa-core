import copy
from datetime import datetime, timedelta, timezone
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
POSTFIX_SOURCE = "b" * 40


def _package_version(include, version, condition=None):
    attribute = f' Condition="{condition}"' if condition else ""
    return f'    <PackageVersion Include="{include}" Version="{version}"{attribute}/>'


def _item_group(entries, condition=None):
    attribute = f' Condition="{condition}"' if condition else ""
    return "<ItemGroup" + attribute + ">\n" + "\n".join(entries) + "\n</ItemGroup>"


def _props(groups):
    return "<Project>\n" + "\n".join(groups) + "\n</Project>\n"


def _tfm_eq(tfm):
    return f"'$(TargetFramework)' == '{tfm}'"


def _tfm_ne(tfm):
    return f"'$(TargetFramework)' != '{tfm}'"


def _net89():
    return f"{_tfm_eq('net8.0')} or {_tfm_eq('net9.0')}"


def _pkcs_asn1_groups():
    per_tfm = []
    for package_id in ("System.Formats.Asn1", "System.Security.Cryptography.Pkcs"):
        per_tfm.extend((_package_version(package_id, "8.0.2", _tfm_eq("net8.0")),
                        _package_version(package_id, "9.0.21", _tfm_eq("net9.0"))))
    return (
        _item_group(per_tfm, _net89()),
        _item_group((_package_version("System.Formats.Asn1", "10.0.13"),
                     _package_version("System.Security.Cryptography.Pkcs", "10.0.13")), _tfm_eq("net10.0")),
    )


def _connector_groups():
    return _item_group((_package_version("MySqlConnector", "2.6.2"),
                        _package_version("SQLitePCLRaw.lib.e_sqlite3", "2.1.14")))


def _signalr_groups():
    return tuple(_item_group((_package_version("Microsoft.AspNetCore.SignalR.Client", version),), _tfm_eq(tfm))
                 for tfm, version in (("net8.0", "8.0.32"), ("net9.0", "9.0.21"), ("net10.0", "10.0.13")))


def _dropins_csproj(*, net8_pkcs="9.0.21", net8_asn1="9.0.21"):
    references = [
        f'    <PackageReference Include="System.Formats.Asn1" Condition="{_tfm_ne("net8.0")}" />',
        f'    <PackageReference Include="System.Formats.Asn1" Condition="{_tfm_eq("net8.0")}" VersionOverride="{net8_asn1}" />',
        f'    <PackageReference Include="System.Security.Cryptography.Pkcs" Condition="{_tfm_ne("net8.0")}" />',
        f'    <PackageReference Include="System.Security.Cryptography.Pkcs" Condition="{_tfm_eq("net8.0")}" VersionOverride="{net8_pkcs}" />',
    ]
    return "<Project>\n<ItemGroup>\n" + "\n".join(references) + "\n</ItemGroup>\n</Project>\n"


def write_pinned_tree(root: Path, *, postfix=True, dropins=True, mutate=None):
    (root / "extensions/src/dropins/Elsa.DropIns").mkdir(parents=True, exist_ok=True)
    (root / "studio/src").mkdir(parents=True, exist_ok=True)
    if postfix:
        core = _props((*_pkcs_asn1_groups(), _connector_groups()))
        extensions = _props((*_pkcs_asn1_groups(), _connector_groups()))
        studio = _props(_signalr_groups())
        dropins_text = _dropins_csproj()
    else:
        core = _props((_item_group((_package_version("MySqlConnector", "2.4.0"),
                                    _package_version("System.Formats.Asn1", "10.0.8"),
                                    _package_version("SQLitePCLRaw.lib.e_sqlite3", "2.1.2"))),))
        extensions = core
        studio = _props(tuple(_item_group((_package_version("Microsoft.AspNetCore.SignalR.Client", version),), _tfm_eq(tfm))
                              for tfm, version in (("net8.0", "8.0.24"), ("net9.0", "9.0.13"), ("net10.0", "10.0.3"))))
        dropins_text = "<Project>\n<ItemGroup>\n    <PackageReference Include=\"System.Formats.Asn1\" />\n</ItemGroup>\n</Project>\n"
    files = {
        "Directory.Packages.props": core,
        "extensions/src/Directory.Packages.props": extensions,
        "studio/src/Directory.Packages.props": studio,
    }
    if dropins:
        files[candidate.DROPINS_PROJECT] = dropins_text
    if mutate:
        mutate(files)
    for relative, text in files.items():
        path = root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text)


def init_release_line(root: Path) -> str:
    def git(*arguments):
        return subprocess.run(["git", "-C", str(root), *arguments], check=True, capture_output=True, text=True)
    git("init", "--quiet")
    git("checkout", "-b", "release/3.10.0")
    git("add", ".")
    git("-c", "user.name=Candidate contract", "-c", "user.email=candidate@example.invalid",
        "commit", "--quiet", "-m", "Reviewed pins")
    sha = git("rev-parse", "HEAD").stdout.strip()
    git("update-ref", "refs/remotes/origin/release/3.10.0", sha)
    return sha


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
                         "prove_consolidated_package_consumers.py", "product_layout.py", "product-layout.json"):
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
        packages.validate_mode("3.10.1", "candidate")
        packages.validate_mode("3.10.0-proof.1.1", "proof")
        self.assertEqual("3.10.0.0", packages.assembly_identity("3.10.0"))
        self.assertEqual("3.10.1.0", packages.assembly_identity("3.10.1"))
        packages.validate_mode("3.10.2", "candidate")
        self.assertEqual("3.10.2.0", packages.assembly_identity("3.10.2"))
        for mode, versions in (("candidate", ("3.10.0-proof.1.1", "3.10.0-rc1", "3.10.01", "3.11.0", "3.9.0", "1.0.1")),
                               ("proof", ("3.10.0", "3.10.1", "3.10.2", "3.10.0-proof.0.1", "3.10.0-proof.1.0"))):
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
        self.assertIn("'$(Version)' != '$(PackageVersion)' or '$(Configuration)' != 'Release' or !$([System.Text.RegularExpressions.Regex]::IsMatch('$(Version)', '^3\\.10\\.(0|[1-9][0-9]*)$'))", errors)
        build = (ROOT / "build/Build.cs").read_text()
        for start, end in (("RestoreSettings", "CompileSettings"), ("CompileSettings", "PackSettings"), ("PackSettings", "TestProjects")):
            section = build.split(start, 1)[1].split(end, 1)[0]
            self.assertIn('SetProperty("Version", Version)', section)
            self.assertIn('SetProperty("PackageVersion", Version)', section)
            self.assertIn('SetProperty("Configuration", "Release")', section)

    def test_inventory_changes_emit_reviewable_diff_and_fail_before_build(self):
        baseline = json.loads((ROOT / "scripts/integration-program/consolidated-candidate-inventory-baseline.json").read_text())
        manifest = copy.deepcopy(baseline)
        for category in ("packages", "exclusions"):
            for row in manifest[category]:
                row["project"] = candidate.map_path(row["project"])
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
            self.assertTrue(any(row["id"] == "Elsa.SamplePackage" for row in baseline["exclusions"]))
            self.assertFalse(any(row["id"] == "Elsa.SamplePackage" for row in baseline["packages"]))
            sample = next(row for row in manifest["exclusions"] if row["id"] == "Elsa.SamplePackage")
            manifest["packages"].append({**sample, "nupkg": "Elsa.SamplePackage.3.10.0.nupkg"})
            manifest["exclusions"] = [row for row in manifest["exclusions"] if row["id"] != "Elsa.SamplePackage"]
            with self.assertRaisesRegex(ValueError, "inventory changed|SamplePackage"):
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

    def test_later_patch_source_rejects_original_sha_and_missing_pins(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_pinned_tree(root, postfix=False)
            with self.assertRaisesRegex(ValueError, "3.10.0 source"):
                candidate.require_candidate_source(root, candidate.ORIGINAL_3100_SOURCE, "3.10.1")
            with self.assertRaisesRegex(ValueError, "MySqlConnector"):
                candidate.require_candidate_source(root, POSTFIX_SOURCE, "3.10.1")
            write_pinned_tree(root, postfix=True)
            candidate.require_candidate_source(root, POSTFIX_SOURCE, "3.10.1")
            candidate.require_candidate_source(root, candidate.ORIGINAL_3100_SOURCE, "3.10.0")

    def test_source_pin_fixtures_cover_pre_and_post_tfm_policy(self):
        cases = (
            ("prefix missing pins", False, None, "MySqlConnector|reviewed pin"),
            ("postfix reviewed pins", True, None, None),
            ("plaintext needles are not pins", True,
             lambda files: files.__setitem__("Directory.Packages.props",
                                             '<Project><!-- Include="MySqlConnector" Version="2.6.2" '
                                             'Include="SQLitePCLRaw.lib.e_sqlite3" Version="2.1.14" --></Project>\n'),
             "MySqlConnector"),
            ("unconditional Pkcs conflicts with net8 pin", True,
             lambda files: files.__setitem__(
                 "Directory.Packages.props",
                 _props([*_pkcs_asn1_groups(), _connector_groups(),
                         _item_group([_package_version("System.Security.Cryptography.Pkcs", "10.0.13")])])
             ),
             "conflicting"),
            ("unpatched net8 Pkcs", True,
             lambda files: files.update({"Directory.Packages.props": files["Directory.Packages.props"].replace(
                 _package_version("System.Security.Cryptography.Pkcs", "8.0.2", _tfm_eq("net8.0")),
                 _package_version("System.Security.Cryptography.Pkcs", "8.0.1", _tfm_eq("net8.0")))}),
             "8.0.2"),
            ("SignalR net8 still 8.0.24", True,
             lambda files: files.update({"studio/src/Directory.Packages.props": files["studio/src/Directory.Packages.props"].replace(
                 'Version="8.0.32"', 'Version="8.0.24"')}),
             "8.0.32"),
            ("DropIns missing net8 override", True,
             lambda files: files.__setitem__(candidate.DROPINS_PROJECT,
                                             "<Project>\n<ItemGroup>\n    <PackageReference Include=\"System.Formats.Asn1\" />\n</ItemGroup>\n</Project>\n"),
             "VersionOverride"),
            ("DropIns net8 override not 9.0.21", True,
             lambda files: files.__setitem__(candidate.DROPINS_PROJECT, _dropins_csproj(net8_pkcs="8.0.2")),
             "9.0.21"),
            ("extensions missing connector pin", True,
             lambda files: files.__setitem__("extensions/src/Directory.Packages.props", _props(_pkcs_asn1_groups())),
             "MySqlConnector"),
        )
        for label, postfix, mutate, pattern in cases:
            with self.subTest(label=label), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                write_pinned_tree(root, postfix=postfix, mutate=mutate)
                if pattern is None:
                    candidate.require_candidate_source(root, POSTFIX_SOURCE, "3.10.1")
                else:
                    with self.assertRaisesRegex(ValueError, pattern):
                        candidate.require_candidate_source(root, POSTFIX_SOURCE, "3.10.1")

    def test_check_source_and_nupkg_helpers_are_tested_functions(self):
        self.assertEqual(Path("/x"), candidate.parse_check_source_args(
            ["check-source", "--version", "3.10.2", "--source", "b" * 40, "--source-root", "/x"]).source_root)
        source = POSTFIX_SOURCE
        for argv in (["--check-source", "--version", "3.10.1", "--source", source],
                     ["check-source", "--version", "3.10.1", "--source", source]):
            args = candidate.parse_check_source_args(argv)
            self.assertEqual("3.10.1", args.version)
            self.assertEqual(source, args.source)
        with tempfile.TemporaryDirectory() as temporary:
            packages_dir = Path(temporary)
            with self.assertRaisesRegex(ValueError, "no nupkg"):
                candidate.require_versioned_nupkgs(packages_dir, "3.10.1")
            (packages_dir / "Elsa.3.10.0.nupkg").write_bytes(b"old")
            with self.assertRaisesRegex(ValueError, "mixed versions"):
                candidate.require_versioned_nupkgs(packages_dir, "3.10.1")
            (packages_dir / "Elsa.3.10.0.nupkg").unlink()
            (packages_dir / "Elsa.3.10.1.nupkg").write_bytes(b"ok")
            (packages_dir / "Elsa.Workflows.Core.3.10.1.nupkg").write_bytes(b"ok")
            nupkgs = candidate.require_versioned_nupkgs(packages_dir, "3.10.1")
            self.assertEqual(["Elsa.3.10.1.nupkg", "Elsa.Workflows.Core.3.10.1.nupkg"], [path.name for path in nupkgs])
            parsed = candidate.parse_check_nupkgs_args(["check-nupkgs", "--packages-dir", str(packages_dir), "--version", "3.10.1"])
            self.assertEqual(packages_dir, parsed.packages_dir)
            self.assertEqual("check-source", candidate.select_prepare_command(["--check-source", "--version", "3.10.1"]))
            self.assertEqual("check-nupkgs", candidate.select_prepare_command(["check-nupkgs"]))
            self.assertEqual("consume", candidate.select_prepare_command(["--consume", "--archive", "x"]))
            self.assertIsNone(candidate.select_prepare_command(["--version", "3.10.1"]))

    def test_dispatch_ref_is_irrelevant_and_source_must_be_on_the_versions_release_line(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_pinned_tree(root, postfix=True)
            sha = init_release_line(root)
            # The tooling runs from main; GITHUB_REF is never consulted.
            for ref in ("refs/heads/main", "refs/heads/release/3.10.0", "refs/heads/cursor/other"):
                with self.subTest(ref=ref), patch.dict(os.environ, GITHUB_REF=ref):
                    for version in ("3.10.1", "3.10.2"):
                        candidate.run_check_source(root, sha, version)
            self.assertEqual("release/3.10.0", candidate.release_line("3.10.2"))
            self.assertEqual("release/3.11.0", candidate.release_line("3.11.4"))
            with self.assertRaises(ValueError):
                candidate.release_line("3.10")
            subprocess.run(["git", "-C", str(root), "-c", "user.name=Candidate contract",
                            "-c", "user.email=candidate@example.invalid", "commit", "--allow-empty",
                            "--quiet", "-m", "off line"], check=True, capture_output=True)
            off_line = subprocess.run(["git", "-C", str(root), "rev-parse", "HEAD"],
                                      check=True, capture_output=True, text=True).stdout.strip()
            subprocess.run(["git", "-C", str(root), "update-ref", "HEAD", sha], check=True)
            with self.assertRaisesRegex(ValueError, "not on the release/3.10.0 line"):
                candidate.require_release_line(root, off_line, "3.10.1")
            with self.assertRaisesRegex(ValueError, "missing the release/3.11.0 line"):
                candidate.require_release_line(root, sha, "3.11.0")

    def test_check_source_runs_against_a_separate_source_root(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary) / "source"
            root.mkdir()
            write_pinned_tree(root, postfix=True)
            sha = init_release_line(root)
            tooling_cwd = Path(temporary) / "tooling"
            tooling_cwd.mkdir()
            script = ROOT / "scripts/integration-program/prepare_consolidated_release_candidate.py"
            ok = subprocess.run([sys.executable, "-B", str(script), "check-source", "--version", "3.10.2", "--source", sha,
                                 "--source-root", str(root)], cwd=tooling_cwd, capture_output=True, text=True,
                                env={**os.environ, "PYTHONDONTWRITEBYTECODE": "1"})
            self.assertEqual(0, ok.returncode, ok.stderr)
            bad = subprocess.run([sys.executable, "-B", str(script), "check-source", "--version", "3.10.2", "--source", "c" * 40,
                                  "--source-root", str(root)], cwd=tooling_cwd, capture_output=True, text=True)
            self.assertNotEqual(0, bad.returncode)
            self.assertIn("not the HEAD of the source checkout", bad.stderr)

    def test_check_source_requires_the_checkout_head_to_be_the_source_commit(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_pinned_tree(root, postfix=True)
            sha = init_release_line(root)
            candidate.run_check_source(root, sha, "3.10.1")
            subprocess.run(["git", "-C", str(root), "-c", "user.name=Candidate contract",
                            "-c", "user.email=candidate@example.invalid", "commit", "--allow-empty",
                            "--quiet", "-m", "later"], check=True, capture_output=True)
            tip = subprocess.run(["git", "-C", str(root), "rev-parse", "HEAD"], check=True,
                                 capture_output=True, text=True).stdout.strip()
            subprocess.run(["git", "-C", str(root), "update-ref", "refs/remotes/origin/release/3.10.0", tip], check=True)
            # sha is still on the release line and still has the reviewed pins, but it is no longer what is checked out.
            candidate.require_release_line(root, sha, "3.10.1")
            candidate.require_candidate_source(root, sha, "3.10.1")
            with self.assertRaisesRegex(ValueError, "not the HEAD of the source checkout"):
                candidate.run_check_source(root, sha, "3.10.1")
            candidate.run_check_source(root, tip, "3.10.1")
            with self.assertRaisesRegex(ValueError, "not the HEAD"):
                candidate.require_checkout_head(root / "missing", sha)

    def test_reviewed_pins_are_minimums_so_a_later_patch_may_move_them_up(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_pinned_tree(root, postfix=True, mutate=lambda files: files.update({
                "studio/src/Directory.Packages.props": files["studio/src/Directory.Packages.props"].replace('Version="8.0.32"', 'Version="8.0.40"')}))
            candidate.require_candidate_source(root, POSTFIX_SOURCE, "3.10.2")

    def test_producer_run_sha_may_be_the_tooling_commit_not_the_source(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "candidate"
            output.mkdir()
            (output / "receipt.json").write_text('{"published":false}\n')
            source, tooling = "b" * 40, "d" * 40
            envelope = {"schema": 1, "published": False, "version": "3.10.2", "source_commit": source,
                        "run_id": 9, "run_attempt": 1, "artifact_id": 5,
                        "retrieved_at": datetime.now(timezone.utc).isoformat(),
                        "artifact_name": f"consolidated-candidate-{source}-9-1", "retention_days": 30,
                        "expires_at": (datetime.now(timezone.utc) + timedelta(days=30)).isoformat(),
                        "archive_sha256": "a" * 64, "archive_size": 1, "preupload_manifest_sha256": "b" * 64}
            metadata = {"id": 5, "name": envelope["artifact_name"], "expired": False, "expires_at": envelope["expires_at"],
                        "digest": "sha256:" + "a" * 64, "size_in_bytes": 1, "workflow_run": {"id": 9, "head_sha": tooling}}
            candidate.validate_envelope(envelope, metadata, source=source, run_id=9, attempt=1, version="3.10.2", run_sha=tooling)
            with self.assertRaisesRegex(ValueError, "source mismatch"):
                candidate.validate_envelope(envelope, metadata, source=source, run_id=9, attempt=1, version="3.10.2")
            with self.assertRaisesRegex(ValueError, "source mismatch"):
                candidate.validate_envelope(envelope, metadata, source=source, run_id=9, attempt=1, version="3.10.2", run_sha="e" * 40)

    def test_later_patch_seal_and_envelope_reject_reused_3100_identity(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "candidate"
            output.mkdir()
            (output / "receipt.json").write_text('{"published":false}\n')
            source = "b" * 40
            with patch.dict(os.environ, GITHUB_RUN_ID="7", GITHUB_RUN_ATTEMPT="1"):
                manifest = candidate.seal(output, source, "3.10.1")
            self.assertEqual("3.10.1", manifest["version"])
            self.assertEqual(source, manifest["source_commit"])
            envelope = {"schema": 1, "published": False, "version": "3.10.1", "source_commit": source,
                        "run_id": 7, "run_attempt": 1, "artifact_id": candidate.ORIGINAL_3100_ARTIFACT,
                        "retrieved_at": datetime.now(timezone.utc).isoformat(),
                        "artifact_name": f"consolidated-candidate-{source}-7-1", "retention_days": 30,
                        "expires_at": (datetime.now(timezone.utc) + timedelta(days=30)).isoformat(),
                        "archive_sha256": "a" * 64, "archive_size": 1, "preupload_manifest_sha256": "b" * 64}
            metadata = {"id": candidate.ORIGINAL_3100_ARTIFACT, "name": envelope["artifact_name"], "expired": False,
                        "expires_at": envelope["expires_at"], "digest": "sha256:" + envelope["archive_sha256"],
                        "size_in_bytes": 1, "workflow_run": {"id": 7, "head_sha": source}}
            with self.assertRaisesRegex(ValueError, "3.10.0 artifact"):
                candidate.validate_envelope(envelope, metadata, source=source, run_id=7, attempt=1, version="3.10.1")
            reused = dict(envelope)
            reused["source_commit"] = candidate.ORIGINAL_3100_SOURCE
            reused["artifact_name"] = f"consolidated-candidate-{candidate.ORIGINAL_3100_SOURCE}-7-1"
            reused_metadata = copy.deepcopy(metadata)
            reused_metadata["name"] = reused["artifact_name"]
            reused_metadata["workflow_run"] = {"id": 7, "head_sha": candidate.ORIGINAL_3100_SOURCE}
            with self.assertRaisesRegex(ValueError, "3.10.0 source"):
                candidate.validate_envelope(reused, reused_metadata, source=candidate.ORIGINAL_3100_SOURCE,
                                            run_id=7, attempt=1, version="3.10.1")

    def test_producer_workflow_runs_tooling_from_main_and_packs_the_source_commit(self):
        workflow = (ROOT / ".github/workflows/prepare-consolidated-release-candidate.yml").read_text()
        prepare = workflow.split("  prepare:")[1].split("  retrieve:")[0]
        self.assertFalse((ROOT / ".github/workflows/prepare-consolidated-3.10.1-release-candidate.yml").exists())
        self.assertIn("permissions: {}", workflow)
        self.assertNotIn("type: choice", workflow)
        for name in ("version:", "source_commit:"):
            self.assertIn(name, workflow)
        # Tooling comes from the workflow's own ref (main); the product source is a second checkout of source_commit.
        self.assertIn("test \"$GITHUB_REF\" = 'refs/heads/main'", prepare)
        self.assertIn("path: tooling", prepare)
        self.assertIn("ref: ${{ github.sha }}", prepare)
        self.assertIn("path: source", prepare)
        self.assertIn("ref: ${{ needs.verify_source.outputs.verified-revision }}", prepare)
        self.assertEqual(2, prepare.count("actions/checkout@"))
        self.assertNotIn("git checkout", prepare)
        self.assertNotIn("release/3.10.0", workflow)
        self.assertNotIn("GITHUB_REF\" = \"refs/heads/$branch", workflow)
        self.assertIn('git -C source merge-base --is-ancestor "$CANDIDATE_SOURCE" "origin/$branch"', prepare)
        self.assertNotRegex(prepare, r"(?m)^\s+python3 scripts/")
        for invocation in re.findall(r"python3 (\S+)", prepare):
            self.assertTrue(invocation.startswith("tooling/") or invocation in ("-m",), invocation)
        self.assertIn("check-source --version \"$VERSION\" --source \"$CANDIDATE_SOURCE\" --source-root source", prepare)
        self.assertIn("--source-root source --output", prepare)
        self.assertIn("check-nupkgs --packages-dir", prepare)
        self.assertIn("validate_packages_id_set.py", prepare)
        self.assertIn('version:$version', workflow)
        self.assertIn('.head_branch == "main"', workflow)
        self.assertIn('.path == ".github/workflows/prepare-consolidated-release-candidate.yml"', workflow)
        self.assertIn('(.event == "workflow_dispatch")', workflow)
        self.assertIn('test "$SOURCE_SHA" != "5cff67228e550b353b3483eb3b37dfdb73a41c74"', workflow)
        self.assertIn('test "$ARTIFACT_ID" != "12664519771"', workflow)
        self.assertIn('--run-sha "$RUN_SHA"', workflow.split("--consume", 1)[1])
        self.assertIn("name: consolidated-candidate-${{ inputs.source_commit }}-", workflow)
        for denied in ("secrets.", "packages: write", "deployments:", "environment:", "nuget push", "npm publish", "gh release", "publish_preview", "publish_nuget", "workflow_call:", "workflow_run:", "pull_request:"):
            self.assertNotIn(denied, workflow)
        self.assertNotIn("python3 -c", workflow.split("jobs:", 1)[1])
        self.assertNotRegex(workflow.split("jobs:", 1)[1], r"--version [0-9]|version:\"[0-9]")

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
