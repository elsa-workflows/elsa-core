"""Original history, current build/data boundaries and restore policy fail closed."""

import copy
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET

from apps_consumer_inventory import git, git_environment
from templates_product_inventory import current_templates_projects
from verify_templates_source_import import verify_project_evaluation, verify_restore_graph

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = "docs/integration-program/templates-source-import.json"
INVENTORY = "scripts/solution/solution-groups.json"
SCRIPTS = ("verify_templates_source_import.py", "templates_product_inventory.py", "apps_consumer_inventory.py")


class TemplatesSourceTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="templates-import-test-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        git(self.root, "init", "--quiet")
        common = Path(git(ROOT, "rev-parse", "--git-common-dir"))
        if not common.is_absolute():
            common = ROOT / common
        (self.root / ".git/objects/info/alternates").write_text(str(common.resolve() / "objects") + "\n")
        (self.root / ".git/HEAD").write_text(git(ROOT, "rev-parse", "HEAD") + "\n")
        git(self.root, "read-tree", "HEAD")
        paths = git(self.root, "ls-files", "--", "templates/").splitlines()
        git(self.root, "checkout-index", "--force", "--", *paths, "Elsa.sln", "Elsa.Templates.slnf", INVENTORY)
        for name in SCRIPTS:
            target = self.root / "scripts/integration-program" / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / "scripts/integration-program" / name, target)
        (self.root / MANIFEST).parent.mkdir(parents=True, exist_ok=True)
        self.manifest = json.loads((ROOT / MANIFEST).read_text())
        self.environment = {**os.environ, "PYTHONDONTWRITEBYTECODE": "1"}

    def run_verifier(self):
        (self.root / MANIFEST).write_text(json.dumps(self.manifest))
        return [subprocess.run([sys.executable, *options, str(self.root / "scripts/integration-program/verify_templates_source_import.py")],
                               cwd=self.root, env=self.environment, text=True, capture_output=True)
                for options in ([], ["-O"])]

    def assert_rejected(self, reason):
        for result in self.run_verifier():
            self.assertNotEqual(0, result.returncode, result.stdout)
            self.assertIn(reason, result.stderr)
            self.assertNotIn("Templates import verified", result.stdout)

    def commit(self, *paths):
        git(self.root, "add", "--", *paths)
        git(self.root, "-c", "user.name=Templates fixture", "-c", "user.email=templates-fixture@example.invalid",
            "commit", "--quiet", "-m", "Normal reviewed Templates development")

    def test_original_mapping_and_tag_corruption_are_rejected(self):
        original = copy.deepcopy(self.manifest)
        for field, value, reason in (("destinationPath", "outside/file", "Unexpected mapping"),
                                     ("sourceBlob", "0" * 40, "Source metadata changed")):
            with self.subTest(field=field):
                self.manifest = copy.deepcopy(original)
                self.manifest["assets"][0][field] = value
                self.assert_rejected(reason)
        self.manifest = copy.deepcopy(original)
        tag = next(row for row in self.manifest["sourceRefs"] if row["type"] == "tag")
        tag["sha"] = "0" * 40
        self.assert_rejected("Original annotated tag object changed")

    def test_current_symlink_and_dirty_raw_bytes_are_rejected(self):
        path = self.root / "templates/README.md"
        content = path.read_bytes()
        path.write_bytes(content + b"\nDirty modification\n")
        self.assert_rejected("Current Templates bytes differ")
        path.unlink()
        external = self.root / "external"
        external.write_bytes(content)
        path.symlink_to(external)
        self.assert_rejected("traverses a symlink")

    def test_parent_symlink_cannot_authorize_mapped_assets(self):
        directory = self.root / "templates/docs"
        target = self.root / "linked-docs"
        directory.rename(target)
        directory.symlink_to(target, target_is_directory=True)
        self.assert_rejected("traverses a symlink")

    def test_committed_normal_source_change_is_not_a_historical_ledger_edit(self):
        path = "templates/src/Elsa.Templates/templates/elsa-server/Program.cs"
        file = self.root / path
        file.write_bytes(file.read_bytes() + b"\n// A normal reviewed source edit.\n")
        self.commit(path)
        for result in self.run_verifier():
            self.assertEqual(0, result.returncode, result.stderr)

    def test_ambient_git_routing_and_crlf_filters_cannot_change_provenance(self):
        crlf = self.root / "templates/docs/crlf-fixture.txt"
        crlf.write_bytes(b"A committed CRLF source asset.\r\n")
        git(self.root, "config", "core.autocrlf", "false")
        self.commit(str(crlf.relative_to(self.root)))
        git(self.root, "config", "core.autocrlf", "input")
        self.environment.update(GIT_DIR="/nonexistent/hostile", GIT_WORK_TREE="/outside", GIT_INDEX_FILE="/nonexistent/index",
                                GIT_CONFIG_COUNT="1", GIT_CONFIG_KEY_0="core.worktree", GIT_CONFIG_VALUE_0="/outside")
        for result in self.run_verifier():
            self.assertEqual(0, result.returncode, result.stderr)
        relative = crlf.relative_to(self.root).as_posix()
        committed = git(self.root, "rev-parse", "HEAD:" + relative)
        self.assertEqual(committed, git(self.root, "hash-object", "--no-filters", str(crlf)))
        self.assertNotEqual(committed, git(self.root, "hash-object", str(crlf)))

    def test_starter_data_and_unknown_projects_cannot_enter_build_inventory(self):
        path = self.root / INVENTORY
        original = path.read_text()
        inventory = json.loads(original)
        configuration = inventory["independentProducts"]["templates"]
        configuration["projects"].append(configuration["buildData"][0])
        path.write_text(json.dumps(inventory))
        with self.assertRaisesRegex(ValueError, "unique and disjoint"):
            current_templates_projects(self.root)
        path.write_text(original)
        unknown = self.root / "templates/src/Unknown/Unknown.csproj"
        unknown.parent.mkdir()
        unknown.write_text('<Project Sdk="Microsoft.NET.Sdk" />')
        self.commit(str(unknown.relative_to(self.root)))
        with self.assertRaisesRegex(ValueError, "explicit build/data disposition"):
            current_templates_projects(self.root)

    def test_root_membership_and_filter_disagreement_are_rejected(self):
        path = self.root / "Elsa.Templates.slnf"
        original = path.read_text()
        filtered = json.loads(original)
        filtered["solution"]["path"] = "Elsa.sln"
        path.write_text(json.dumps(filtered))
        with self.assertRaisesRegex(ValueError, "filter must select its product"):
            current_templates_projects(self.root)
        path.write_text(original)
        root_solution = self.root / "Elsa.sln"
        root_solution.write_text(root_solution.read_text() + '\nProject("{TYPE}") = "Templates", "templates/src/Elsa.Templates/Elsa.Templates.csproj", "{ID}"\nEndProject\n')
        with self.assertRaisesRegex(ValueError, "must not enter the root source graph"):
            current_templates_projects(self.root)

    def test_applied_contribution_restores_missing_registration_without_replacing_main(self):
        path = "templates/src/Elsa.Templates/templates/elsa-combined/src/Company.ElsaCombined.Host/Program.cs"
        registration = "options.RootComponents.RegisterCustomElsaStudioElements();"
        self.assertNotIn(registration, git(self.root, "show", self.manifest["importCommit"] + ":" + path))
        applied = self.manifest["appliedContribution"]["appliedCommit"]
        self.assertIn(registration, git(self.root, "show", applied + ":" + path))
        self.assertEqual([path], git(self.root, "diff", "--name-only", applied + "^", applied).splitlines())
        self.assertEqual("gpanayotov <panayotov.g.p@gmail.com>", git(self.root, "show", "-s", "--format=%an <%ae>", applied))


class TemplatesRestoreTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.path = Path(temporary.name) / "graph.json"
        self.projects = current_templates_projects(ROOT)

    def test_only_scoped_config_and_registered_projects_are_accepted(self):
        graph = {"projects": {str(project): {"restore": {"configFilePaths": [str(ROOT / "templates/NuGet.Config")]}}
                              for project in self.projects}}
        self.path.write_text(json.dumps(graph))
        verify_restore_graph(ROOT, self.path, self.projects)
        for change, reason in ((lambda g: g["projects"].pop(str(self.projects[0])), "exactly the registered"),
                               (lambda g: g["projects"][str(self.projects[0])]["restore"]["configFilePaths"].append(str(ROOT / "NuGet.Config")), "only Templates")):
            mutated = copy.deepcopy(graph)
            change(mutated)
            self.path.write_text(json.dumps(mutated))
            with self.assertRaisesRegex(ValueError, reason):
                verify_restore_graph(ROOT, self.path, self.projects)

    def test_effective_pack_policy_preserves_independent_identity(self):
        project = self.projects[0]
        evaluation = {"Properties": {"TargetFramework": "net10.0", "UseProjectReferences": "false", "IsPackable": "true",
            "ManagePackageVersionsCentrally": "true", "RestoreConfigFile": str(ROOT / "templates/NuGet.Config"),
            "DirectoryPackagesPropsPath": str(ROOT / "templates/Directory.Packages.props"),
            "DirectoryBuildPropsPath": str(ROOT / "templates/Directory.Build.props"),
            "DirectoryBuildTargetsPath": str(ROOT / "templates/Directory.Build.targets"),
            "PackageId": "Elsa.Templates", "PackageVersion": "3.9.0", "RepositoryUrl": "https://github.com/elsa-workflows/elsa-core",
            "PackageOutputPath": str(ROOT / "templates/artifacts/package/release")},
            "Items": {"ProjectReference": [], "PackageReference": []}}
        verify_project_evaluation(ROOT, project, evaluation)
        evaluation["Properties"]["PackageVersion"] = "3.10.0-proof.1.1"
        with self.assertRaisesRegex(ValueError, "independently declared"):
            verify_project_evaluation(ROOT, project, evaluation)
        config = ET.parse(ROOT / "templates/NuGet.Config")
        self.assertEqual(["https://api.nuget.org/v3/index.json"], [entry.attrib["value"] for entry in config.findall("./packageSources/add")])


if __name__ == "__main__":
    unittest.main()
