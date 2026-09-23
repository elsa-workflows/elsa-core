import sys
import unittest
from pathlib import Path

SCRIPTS = Path(__file__).resolve().parent
REPOSITORY = SCRIPTS.parents[1]
sys.path.insert(0, str(SCRIPTS))

from package_impact import InventoryGraph  # noqa: E402


class PackageImpactTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.graph = InventoryGraph.from_path(
            REPOSITORY / "doc/integration-program/inventory/inventory.json"
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

    def test_explicit_slack_release_unit_keeps_package_scope_independent(self):
        self.assertEqual({"Elsa.Slack"}, self.graph.package_ids([self.slack_project]))
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
