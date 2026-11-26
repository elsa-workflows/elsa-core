import sys
import unittest
import json
import re
from pathlib import Path

SCRIPTS = Path(__file__).resolve().parent
REPOSITORY = SCRIPTS.parents[1]
sys.path.insert(0, str(SCRIPTS))

sys.path.insert(0, str(REPOSITORY / "scripts/ci"))
from select_pr_suites import pattern_regex

from package_impact import InventoryGraph  # noqa: E402


class PackageImpactTests(unittest.TestCase):
    def test_workflow_filters_cover_every_executed_proof_helper(self):
        workflow = (REPOSITORY / '.github/workflows/pr.yml').read_text()
        workflow = workflow.split('  # Package impact closure rehearsal\n', 1)[1].split('  # Consolidated package proof\n', 1)[0]
        routes = json.loads((REPOSITORY / "scripts/ci/pr-paths.json").read_text())
        filters = [pattern_regex(pattern) for pattern in routes["package-impact-closure"]["paths"]]
        invoked_scripts = set(re.findall(r'python\s+(scripts/integration-program/[\w.-]+\.py)', workflow))

        uncovered = sorted(script for script in invoked_scripts
                           if not any(pattern.fullmatch(script) for pattern in filters))
        self.assertEqual([], uncovered)

    def test_read_only_publisher_preflight_is_part_of_the_pr_lane(self):
        workflow = (REPOSITORY / '.github/workflows/pr.yml').read_text()
        workflow = workflow.split('  # Package impact closure rehearsal\n', 1)[1].split('  # Consolidated package proof\n', 1)[0]
        self.assertIn("Validate current publisher ownership without cutover", workflow)
        self.assertIn("check_publisher_handoff.py", workflow)
        full_workflow = (REPOSITORY / '.github/workflows/pr.yml').read_text()
        self.assertIn("permissions:\n  contents: read", full_workflow)
        self.assertNotIn("permissions:", workflow)

    @classmethod
    def setUpClass(cls):
        cls.graph = InventoryGraph.from_path(
            REPOSITORY / "docs/integration-program/inventory/inventory.json"
        )
        cls.changed_core_elsa = ("elsa-core", "src/modules/Elsa/Elsa.csproj")
        cls.slack_project = (
            "elsa-extensions",
            "src/modules/communication/Elsa.Slack/Elsa.Slack.csproj",
        )
        cls.slack_tests = (
            "elsa-extensions",
            "test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj",
        )

    def test_core_elsa_change_reaches_slack_tests_and_expands_the_test_closure(self):
        affected_tests = self.graph.affected_tests([self.changed_core_elsa])

        self.assertIn(self.slack_tests, affected_tests)
        self.assertGreater(len(affected_tests), 1)

    def test_slack_module_change_reaches_only_its_module_test(self):
        self.assertEqual({self.slack_tests}, self.graph.affected_tests([self.slack_project]))

    def test_slack_release_unit_excludes_unchanged_mqtt_package(self):
        packages = self.graph.package_ids([self.slack_project])
        self.assertEqual({"Elsa.Slack"}, packages)
        self.assertNotIn("Elsa.Mqtt", packages)
        self.assertEqual({"Elsa.Slack"}, self.graph.package_ids(iter([self.slack_project])))

    def test_ambiguous_package_owners_are_all_included_in_impact_closure(self):
        first_owner = ("repo-a", "src/SharedA/SharedA.csproj")
        second_owner = ("repo-b", "src/SharedB/SharedB.csproj")
        first_test = ("repo-c", "test/First.Tests/First.Tests.csproj")
        second_test = ("repo-d", "test/Second.Tests/Second.Tests.csproj")
        inventory = {
            "repositories": {
                "a": {"slug": "repo-a"},
                "b": {"slug": "repo-b"},
                "c": {"slug": "repo-c"},
                "d": {"slug": "repo-d"},
            },
            "project_inventory": {
                "repo-a": [{"path": first_owner[1], "package_id": "Shared", "is_packable": True}],
                "repo-b": [{"path": second_owner[1], "package_id": "Shared", "is_packable": True}],
                "repo-c": [{"path": first_test[1], "is_test_project": True, "package_references": [{"id": "Shared"}]}],
                "repo-d": [{"path": second_test[1], "is_test_project": True, "package_references": [{"id": "Shared"}]}],
            },
        }
        graph = InventoryGraph(inventory)

        self.assertEqual({first_test, second_test}, graph.affected_tests([first_owner]))
        self.assertEqual({first_test, second_test}, graph.affected_tests([second_owner]))
        self.assertEqual(2, len(graph.ambiguous_package_edges))


if __name__ == "__main__":
    unittest.main()
