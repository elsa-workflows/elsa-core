"""Historical import evidence and current development remain separate and fail closed."""

import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

from apps_consumer_inventory import declared_package_versions, git, git_environment, current_apps_projects
from verify_apps_source_import import verify_project_evaluation, verify_restore_graph

ROOT = Path(__file__).resolve().parents[2]
VERIFIER = Path("scripts/integration-program/verify_apps_source_import.py")
HELPER = Path("scripts/integration-program/apps_consumer_inventory.py")
MANIFEST = Path("docs/integration-program/apps-source-import.json")


class SourceImportTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.fixture = Path(temporary.name).resolve()
        subprocess.run(["git", "init", "--quiet", str(self.fixture)], env=git_environment(), check=True)
        git(self.fixture, "config", "core.autocrlf", "input")
        common = Path(git(ROOT, "rev-parse", "--git-common-dir"))
        if not common.is_absolute():
            common = ROOT / common
        (self.fixture / ".git/objects/info/alternates").write_text(str(common.resolve() / "objects") + "\n")
        (self.fixture / ".git/HEAD").write_text(git(ROOT, "rev-parse", "HEAD") + "\n")
        git(self.fixture, "read-tree", "HEAD")
        for path in (VERIFIER, HELPER, MANIFEST):
            target = self.fixture / path
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / path, target)
        self.manifest = json.loads((self.fixture / MANIFEST).read_text())
        self.environment = {**os.environ, "PYTHONDONTWRITEBYTECODE": "1"}

    def run_verifier(self):
        (self.fixture / MANIFEST).write_text(json.dumps(self.manifest))
        return [subprocess.run([sys.executable, *optimization, str(self.fixture / VERIFIER)],
                               cwd=self.fixture, env=self.environment, text=True, capture_output=True)
                for optimization in ([], ["-O"])]

    def assert_rejected(self, reason):
        for result in self.run_verifier():
            self.assertNotEqual(result.returncode, 0, result.stdout)
            self.assertIn(reason, result.stderr)
            self.assertNotIn("Apps import verified", result.stdout)

    def materialize_current_apps(self):
        paths = git(self.fixture, "ls-files", "--", "apps/").splitlines()
        git(self.fixture, "checkout-index", "--force", "--", *paths, "Elsa.sln", "Elsa.Apps.slnf")
        # Exercise the pending verifier/helper rather than their previous committed versions.
        for path in (VERIFIER, HELPER):
            shutil.copyfile(ROOT / path, self.fixture / path)

    def commit(self, *paths):
        git(self.fixture, "add", "--", *paths)
        git(self.fixture, "-c", "user.name=Apps fixture", "-c", "user.email=apps-fixture@example.invalid",
            "commit", "--quiet", "-m", "Normal Apps development fixture")

    def test_corrupted_historical_mapping_is_rejected(self):
        self.manifest["assets"][0]["destinationPath"] = "outside-apps/file"
        self.assert_rejected("Unexpected mapping")

    def test_historical_blob_corruption_is_rejected(self):
        self.manifest["assets"][0]["sourceBlob"] = "0" * 40
        self.assert_rejected("Source metadata changed")

    def test_current_file_symlink_is_rejected(self):
        destination = self.fixture / self.manifest["assets"][0]["destinationPath"]
        destination.parent.mkdir(parents=True)
        target = self.fixture / "regular-file"
        target.write_text("Must fail before hashing.")
        destination.symlink_to(target)
        self.assert_rejected("traverses a symlink")

    def test_current_parent_symlink_is_rejected(self):
        external = tempfile.TemporaryDirectory()
        self.addCleanup(external.cleanup)
        destination = self.fixture / self.manifest["assets"][0]["destinationPath"]
        for target in (self.fixture / "linked-assets", Path(external.name)):
            with self.subTest(inside=target.is_relative_to(self.fixture)):
                target.mkdir(parents=True, exist_ok=True)
                (target / destination.name).write_text("Original bytes cannot authorize a symlink.")
                destination.parent.symlink_to(target, target_is_directory=True)
                try:
                    self.assert_rejected("traverses a symlink")
                finally:
                    destination.parent.unlink()

    def test_ordinary_source_edit_and_current_version_change_are_accepted(self):
        self.materialize_current_apps()
        # Select an actual source file rather than adding a historical ledger entry.
        runtime = next((self.fixture / "apps/src").rglob("*.cs"))
        runtime.write_bytes(runtime.read_bytes() + b"\n// A normal reviewed source edit.\n")
        props = self.fixture / "apps/Directory.Packages.props"
        props.write_text(props.read_text().replace("3.9.0", "3.10.0"))
        self.commit(str(runtime.relative_to(self.fixture)), "apps/Directory.Packages.props")
        self.assertEqual({"3.10.0"}, set(declared_package_versions(self.fixture).values()))
        for result in self.run_verifier():
            self.assertEqual(0, result.returncode, result.stderr)

    def test_normal_new_project_with_solution_and_filter_maintenance_is_accepted(self):
        self.materialize_current_apps()
        relative = "apps/src/NewHost/NewHost.csproj"
        project = self.fixture / relative
        project.parent.mkdir()
        project.write_text('<Project Sdk="Microsoft.NET.Sdk" />')
        solution = self.fixture / "Elsa.sln"
        project_entry = (f'Project("{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}") = "NewHost", '
                         f'"{relative}", "{{11C95AC6-82A5-49D0-8465-344027ECF8ED}}"\nEndProject\n')
        original_solution = solution.read_text()
        solution.write_text(original_solution.replace("\nGlobal\n", "\n" + project_entry + "Global\n", 1))
        filter_path = self.fixture / "Elsa.Apps.slnf"
        filtered = json.loads(filter_path.read_text())
        filtered["solution"]["projects"].append(relative)
        filter_path.write_text(json.dumps(filtered))
        self.commit(relative)
        # A tracked project without the coordinated root/filter registration fails closed.
        saved_filter = filter_path.read_text()
        filtered["solution"]["projects"].remove(relative)
        filter_path.write_text(json.dumps(filtered))
        self.assert_rejected("Apps filter must match")
        filter_path.write_text(saved_filter)
        saved_solution = solution.read_text()
        solution.write_text(original_solution)
        self.assert_rejected("Root solution must match")
        solution.write_text(saved_solution)
        self.commit("Elsa.sln", "Elsa.Apps.slnf")
        for result in self.run_verifier():
            self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(10, len(current_apps_projects(self.fixture)))

    def test_missing_current_tracked_asset_is_rejected(self):
        self.materialize_current_apps()
        next((self.fixture / "apps/src").rglob("*.cs")).unlink()
        self.assert_rejected("Missing mapped asset")

    def test_current_version_policy_accepts_updates_and_rejects_version_mismatch(self):
        self.materialize_current_apps()
        props = self.fixture / "apps/Directory.Packages.props"
        props.write_text(props.read_text().replace("3.9.0", "3.10.0"))
        versions = declared_package_versions(self.fixture)
        project = current_apps_projects(self.fixture)[0]
        evaluation = {"Properties": {"TargetFramework": "net10.0", "UseProjectReferences": "false",
            "IsPackable": "false", "RestoreConfigFile": str(self.fixture / "apps/NuGet.Config"),
            "ManagePackageVersionsCentrally": "true",
            "DirectoryPackagesPropsPath": str(self.fixture / "apps/Directory.Packages.props"), **versions},
            "Items": {"ProjectReference": [], "PackageReference": []}}
        verify_project_evaluation(self.fixture, project, evaluation, versions, set(current_apps_projects(self.fixture)))
        evaluation["Properties"]["ElsaVersion"] = "3.9.0"
        with self.assertRaises(ValueError):
            verify_project_evaluation(self.fixture, project, evaluation, versions, set(current_apps_projects(self.fixture)))

    def test_preserved_crlf_blob_is_checked_without_git_clean_filters(self):
        self.materialize_current_apps()
        path = self.fixture / "apps/ElsaApps.sln"
        indexed = git(self.fixture, "rev-parse", "HEAD:apps/ElsaApps.sln")
        self.assertEqual(indexed, git(self.fixture, "hash-object", "--no-filters", str(path)))
        self.assertNotEqual(indexed, git(self.fixture, "hash-object", str(path)))

    def test_ambient_git_routing_and_config_cannot_replace_the_fixture_repository(self):
        self.materialize_current_apps()
        self.environment.update(GIT_DIR="/nonexistent/hostile", GIT_WORK_TREE="/outside-checkout",
                                GIT_INDEX_FILE="/nonexistent/index", GIT_CONFIG_COUNT="1",
                                GIT_CONFIG_KEY_0="core.worktree", GIT_CONFIG_VALUE_0="/outside-checkout")
        for result in self.run_verifier():
            self.assertEqual(0, result.returncode, result.stderr)


class RootRestoreGraphTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.path = Path(temporary.name) / "restore-graph.json"
        self.projects = current_apps_projects(ROOT)

    def write_graph(self, configs):
        self.path.write_text(json.dumps({"projects": {
            str(project): {"restore": {"configFilePaths": configs}} for project in self.projects}}))

    def test_root_context_requires_only_the_scoped_apps_config(self):
        self.write_graph([str(ROOT / "apps/NuGet.Config")])
        verify_restore_graph(self.path, self.projects)

    def test_inherited_or_root_only_mapping_is_rejected(self):
        for configs in ([str(ROOT / "NuGet.Config")],
                        [str(ROOT / "apps/NuGet.Config"), str(ROOT / "NuGet.Config")]):
            with self.subTest(configs=configs):
                self.write_graph(configs)
                with self.assertRaisesRegex(ValueError, "only Apps NuGet.Config"):
                    verify_restore_graph(self.path, self.projects)

    def test_missing_restore_project_is_rejected(self):
        self.write_graph([str(ROOT / "apps/NuGet.Config")])
        graph = json.loads(self.path.read_text())
        graph["projects"].pop(str(self.projects[0]))
        self.path.write_text(json.dumps(graph))
        with self.assertRaisesRegex(ValueError, "exactly the current Apps"):
            verify_restore_graph(self.path, self.projects)


if __name__ == "__main__":
    unittest.main()
