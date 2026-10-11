import copy
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from apps_consumer_inventory import git
from current_import_impact import (
    CORE_PROJECT,
    git_output,
    partition_source_graph,
    reject_unmapped_elsa_packages,
    restored_test_projects,
    select_scenarios,
)


SLACK_PROJECT = 'extensions/src/communication/Elsa.Slack/Elsa.Slack.csproj'
SLACK_TEST = 'extensions/test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj'
CORE_TEST = 'core/test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj'


class FakeGraph:
    def __init__(self, shared_tests, slack_framework="net10.0"):
        self.shared_tests = shared_tests
        self.slack_framework = slack_framework

    def affected_tests(self, roots):
        project = roots[0][1].split("@@", 1)[0]
        if project == SLACK_PROJECT:
            return {("elsa-core", f"{SLACK_TEST}@@{self.slack_framework}")}
        if project == CORE_PROJECT:
            return self.shared_tests
        raise AssertionError(project)


def assets():
    return {
        CORE_PROJECT: {"project": {"restore": {"frameworks": {"net8.0": {}, "net9.0": {}, "net10.0": {}}}}},
        SLACK_PROJECT: {"project": {"restore": {"frameworks": {"net8.0": {}, "net9.0": {}, "net10.0": {}}}}},
        SLACK_TEST: {
            "project": {"restore": {"frameworks": {"net10.0": {}}}},
            "libraries": {"Microsoft.NET.Test.Sdk/18.0.1": {}},
        },
        CORE_TEST: {
            "project": {"restore": {"frameworks": {"net10.0": {}}}},
            "libraries": {"Microsoft.NET.Test.Sdk/18.0.1": {}},
        },
    }


def unit():
    return {
        "id": "elsa-slack",
        "package_id": "Elsa.Slack",
        "source": {
            "test_projects": [{
                "project_path": 'core/test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj',
                "target_frameworks": ["net10.0"],
            }],
        },
        "mapped": {
            "repository": "elsa-core",
            "project_path": SLACK_PROJECT,
            "test_projects": [{
                "source_project_path": 'core/test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj',
                "project_path": SLACK_TEST,
            }],
        },
    }


class CurrentImportedImpactTests(unittest.TestCase):
    def test_git_head_and_status_use_selected_repository_despite_ambient_routing(self):
        with tempfile.TemporaryDirectory() as temporary:
            roots = [Path(temporary) / name for name in ("selected", "ambient")]
            for root in roots:
                root.mkdir()
                git(root, "init", "--quiet")
                (root / "marker").write_text(root.name)
                git(root, "add", "marker")
                git(root, "-c", "user.name=Git fixture", "-c", "user.email=git-fixture@example.invalid",
                    "commit", "--quiet", "-m", root.name)
            selected, ambient = roots
            (selected / "marker").write_text("reviewed working change")
            expected_head = git(selected, "rev-parse", "HEAD")
            expected_status = git(selected, "status", "--porcelain")
            self.assertNotEqual(expected_head, git(ambient, "rev-parse", "HEAD"))
            self.assertTrue(expected_status)
            with patch.dict(os.environ, GIT_DIR=str(ambient / ".git"), GIT_WORK_TREE=str(ambient),
                            GIT_INDEX_FILE=str(ambient / ".git/index"), GIT_CONFIG_COUNT="1",
                            GIT_CONFIG_KEY_0="core.worktree", GIT_CONFIG_VALUE_0=str(ambient)):
                self.assertEqual(expected_head, git_output(selected, "rev-parse", "HEAD"))
                self.assertEqual(expected_status, git_output(selected, "status", "--porcelain"))

    def test_workflow_triggers_for_selector_graph_dependencies(self):
        workflow = Path(__file__).resolve().parents[2] / ".github/workflows/current-import-impact.yml"
        text = workflow.read_text(encoding="utf-8")
        input_roots = (".github/**", "build/**", "Elsa.Apps.slnf", 'core/samples/**', "scripts/integration-program/**", 'core/src/**', 'core/test/**')
        for input_root in input_roots:
            with self.subTest(input_root=input_root):
                self.assertIn(f"      - '{input_root}'", text)

    def test_rejects_unmapped_elsa_package_type_dependencies(self):
        with self.assertRaisesRegex(ValueError, "unmapped Elsa package dependency Elsa.Workflows.Core"):
            reject_unmapped_elsa_packages({
                SLACK_TEST: {
                    "libraries": {
                        "Elsa.Workflows.Core/3.8.0": {"type": "package"},
                        "Elsa.Platform.PackageManifest.Generator/0.0.1-preview.50": {"type": "package"},
                    },
                },
            })

        reject_unmapped_elsa_packages({
            SLACK_TEST: {
                "libraries": {
                    "Elsa.Platform.PackageManifest.Generator/0.0.1-preview.50": {"type": "package"},
                },
            },
        })

    def test_evaluates_test_projects_and_includes_relocated_studio_paths(self):
        documents = assets()
        documents['studio/src/Elsa.Studio.Core.Tests.csproj'] = {
            "libraries": {"Microsoft.NET.Test.Sdk/18.0.1": {}}
        }
        documents['core/test/performance/Benchmarks.csproj'] = {"libraries": {}}

        documents['studio/src/Elsa.Studio.Core.Tests.csproj']["project"] = {
            "restore": {"frameworks": {"net10.0": {}}},
        }
        with patch("current_import_impact._is_test_project", return_value=True):
            self.assertEqual(
                restored_test_projects(Path("."), documents),
                {SLACK_TEST, CORE_TEST, 'studio/src/Elsa.Studio.Core.Tests.csproj'},
            )

    def test_excludes_test_sdk_worker_when_msbuild_says_not_test_project(self):
        documents = assets()
        worker = 'core/test/workers/Elsa.Connections.Credentials.WorkerProcess/Elsa.Connections.Credentials.WorkerProcess.csproj'
        documents[worker] = {
            "project": {"restore": {"frameworks": {"net10.0": {}}}},
            "libraries": {"Microsoft.NET.Test.Sdk/18.0.1": {}},
        }

        def is_test_project(root, project_path, framework):
            return project_path != worker

        with patch("current_import_impact._is_test_project", side_effect=is_test_project):
            self.assertEqual(restored_test_projects(Path("."), documents), {SLACK_TEST, CORE_TEST})

    def test_rejects_inconsistent_test_project_framework_flags(self):
        documents = {
            SLACK_TEST: {
                "project": {"restore": {"frameworks": {"net9.0": {}, "net10.0": {}}}},
                "libraries": {"Microsoft.NET.Test.Sdk/18.0.1": {}},
            },
        }
        with patch("current_import_impact._is_test_project", side_effect=[True, False]):
            with self.assertRaisesRegex(ValueError, "differs across restored frameworks"):
                restored_test_projects(Path("."), documents)

    def test_slack_only_packs_one_unit_while_shared_core_expands_tests(self):
        graph = FakeGraph({
            ("elsa-core", f"{SLACK_TEST}@@net10.0"),
            ("elsa-core", f"{CORE_TEST}@@net10.0"),
        })

        result = select_scenarios(graph, assets(), unit())

        self.assertEqual(result["packageIdsToPack"], ["Elsa.Slack"])
        self.assertFalse(result["unchangedMqttSelectedForPack"])
        self.assertEqual(result["slackOnly"]["tests"], [{"project": SLACK_TEST, "framework": "net10.0"}])
        self.assertEqual(len(result["sharedCore"]["tests"]), 2)
        self.assertFalse(result["testExecutionPerformed"])
        self.assertFalse(result["publicationPerformed"])

    def test_mapped_slack_tests_must_map_each_source_test_once(self):
        for mutate in (
            lambda configured: configured["source"]["test_projects"].append({
                "project_path": 'core/test/modules/slack/Extra.Tests/Extra.Tests.csproj',
                "target_frameworks": ["net10.0"],
            }),
            lambda configured: configured["mapped"]["test_projects"].append(
                configured["mapped"]["test_projects"][0].copy()
            ),
        ):
            with self.subTest(mutate=mutate):
                configured = unit()
                mutate(configured)
                graph = FakeGraph({
                    ("elsa-core", f"{SLACK_TEST}@@net10.0"),
                    ("elsa-core", f"{CORE_TEST}@@net10.0"),
                })

                with self.assertRaisesRegex(ValueError, "map each source test exactly once"):
                    select_scenarios(graph, assets(), configured)

    def test_slack_test_framework_must_match_mapped_source_manifest(self):
        graph = FakeGraph(
            {("elsa-core", f"{CORE_TEST}@@net10.0")},
            slack_framework="net9.0",
        )

        with self.assertRaisesRegex(ValueError, "differs from the release-unit manifest"):
            select_scenarios(graph, assets(), unit())

    def test_missing_slack_test_in_shared_core_closure_fails(self):
        graph = FakeGraph({("elsa-core", f"{CORE_TEST}@@net10.0")})

        with self.assertRaisesRegex(ValueError, "omits the Slack"):
            select_scenarios(graph, assets(), unit())

    def test_release_unit_manifest_mismatch_fails(self):
        configured = unit()
        configured["mapped"]["test_projects"] = [{
            "source_project_path": 'core/test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj',
            "project_path": CORE_TEST,
        }]
        graph = FakeGraph({
            ("elsa-core", f"{SLACK_TEST}@@net10.0"),
            ("elsa-core", f"{CORE_TEST}@@net10.0"),
        })

        with self.assertRaisesRegex(ValueError, "differs from the release-unit manifest"):
            select_scenarios(graph, assets(), configured)


class IndependentAppsImpactTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        filter_path = Path(__file__).resolve().parents[2] / "Elsa.Apps.slnf"
        self.documents = assets()
        self.apps = set(json.loads(filter_path.read_text())["solution"]["projects"])
        for path in self.apps:
            self.documents[path] = {
                "project": {"restore": {"frameworks": {"net10.0": {}}}},
                "libraries": {"Elsa.Workflows.Core/3.9.0": {"type": "package"}},
            }
        self.names = {path: Path(path).stem for path in self.documents}
        inventory = patch("current_import_impact.current_apps_projects", return_value=[self.root / path for path in self.apps])
        self.inventory = inventory.start()
        self.addCleanup(inventory.stop)
        self.solution = set(self.names)

    def partition(self):
        return partition_source_graph(self.root, self.names, self.documents, self.solution)

    def test_registered_consumers_are_explicitly_separate_from_source_graph(self):
        names, documents, independent = self.partition()
        self.assertEqual(set(independent), self.apps)
        self.assertEqual(set(names), set(assets()))
        self.assertEqual(set(documents), set(assets()))
        reject_unmapped_elsa_packages(documents)

    def test_new_current_registered_consumer_is_accounted_for(self):
        path = "apps/src/NewHost/NewHost.csproj"
        self.apps.add(path)
        self.names[path] = "NewHost"
        self.documents[path] = copy.deepcopy(self.documents[next(iter(self.apps - {path}))])
        self.solution.add(path)
        self.inventory.return_value = [self.root / project for project in self.apps]
        names, documents, independent = self.partition()
        self.assertIn(path, independent)
        self.assertNotIn(path, names)
        reject_unmapped_elsa_packages(documents)

    def test_missing_or_unregistered_apps_solution_member_is_rejected(self):
        original = self.solution.copy()
        for mutate in (
            lambda: self.solution.remove(next(iter(self.apps))),
            lambda: self.solution.add("apps/src/Unexpected/Unexpected.csproj"),
        ):
            with self.subTest(mutate=mutate):
                self.solution = original.copy()
                mutate()
                with self.assertRaisesRegex(ValueError, "current tracked/filter/solution"):
                    self.partition()

    def test_project_references_cannot_cross_the_consumer_boundary(self):
        app = next(iter(self.apps))
        original = copy.deepcopy(self.documents)
        for source, target in ((CORE_PROJECT, app), (app, CORE_PROJECT)):
            with self.subTest(source=source):
                self.documents = copy.deepcopy(original)
                framework = next(iter(self.documents[source]["project"]["restore"]["frameworks"].values()))
                framework["projectReferences"] = {str(self.root / target): {}}
                with self.assertRaisesRegex(ValueError, "crosses independent Apps boundary"):
                    self.partition()

    def test_source_packages_remain_fail_closed_after_partition(self):
        self.documents[CORE_PROJECT]["libraries"] = {"Elsa.Workflows.Core/3.9.0": {"type": "package"}}
        _, documents, _ = self.partition()
        with self.assertRaisesRegex(ValueError, "unmapped Elsa package dependency"):
            reject_unmapped_elsa_packages(documents)


if __name__ == "__main__":
    unittest.main()
