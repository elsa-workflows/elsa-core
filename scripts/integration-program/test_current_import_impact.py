import unittest

from current_import_impact import (
    CORE_PROJECT,
    reject_unmapped_elsa_packages,
    restored_test_projects,
    select_scenarios,
)


SLACK_PROJECT = "src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj"
SLACK_TEST = "test/extensions/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj"
CORE_TEST = "test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj"


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
        SLACK_TEST: {"libraries": {"Microsoft.NET.Test.Sdk/18.0.1": {}}},
        CORE_TEST: {"libraries": {"Microsoft.NET.Test.Sdk/18.0.1": {}}},
    }


def unit():
    return {
        "id": "elsa-slack",
        "package_id": "Elsa.Slack",
        "source": {
            "test_projects": [{
                "project_path": "test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj",
                "target_frameworks": ["net10.0"],
            }],
        },
        "mapped": {
            "repository": "elsa-core",
            "project_path": SLACK_PROJECT,
            "test_projects": [{
                "source_project_path": "test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj",
                "project_path": SLACK_TEST,
            }],
        },
    }


class CurrentImportedImpactTests(unittest.TestCase):
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

    def test_uses_restored_test_sdk_identity_including_relocated_studio_paths(self):
        documents = assets()
        documents["src/studio/Elsa.Studio.Core.Tests.csproj"] = {
            "libraries": {"Microsoft.NET.Test.Sdk/18.0.1": {}}
        }
        documents["test/performance/Benchmarks.csproj"] = {"libraries": {}}

        self.assertEqual(
            restored_test_projects(documents),
            {SLACK_TEST, CORE_TEST, "src/studio/Elsa.Studio.Core.Tests.csproj"},
        )

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
                "project_path": "test/modules/slack/Extra.Tests/Extra.Tests.csproj",
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
            "source_project_path": "test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj",
            "project_path": CORE_TEST,
        }]
        graph = FakeGraph({
            ("elsa-core", f"{SLACK_TEST}@@net10.0"),
            ("elsa-core", f"{CORE_TEST}@@net10.0"),
        })

        with self.assertRaisesRegex(ValueError, "differs from the release-unit manifest"):
            select_scenarios(graph, assets(), configured)


if __name__ == "__main__":
    unittest.main()
