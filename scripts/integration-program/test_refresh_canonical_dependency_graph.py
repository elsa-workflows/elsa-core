import copy
import tempfile
import unittest
from pathlib import Path

from refresh_canonical_dependency_graph import (
    _assets_for_project,
    _framework_graph,
    _node_key,
    _parse_test_run_evidence,
    _validated_framework_summary,
    graph_from_edges,
    parse_solution,
)


class CanonicalDependencyGraphTests(unittest.TestCase):
    @staticmethod
    def _summary_fixture(runs):
        totals = {field: sum(run[field] for run in runs) for field in ("passed", "failed", "skipped", "total")}
        return {
            "observedTestResults": {
                "frameworkConsoleSummaries": {
                    "assemblyFrameworkRuns": len(runs),
                    "totalsFromPassedSummaries": totals,
                    "runs": runs,
                },
            },
        }

    @staticmethod
    def _retained_trx_fixture(rows):
        counters = [row["counters"] for row in rows]
        executed = sum(row["executed"] for row in counters)
        total = sum(row["total"] for row in counters)
        return {
            "files": len(rows),
            "passed": sum(row["passed"] for row in counters),
            "failed": sum(row["failed"] for row in counters),
            "skipped": total - executed,
            "executed": executed,
            "totalIncludingSkipped": total,
            "rows": rows,
        }

    def test_framework_summary_is_recomputed_from_per_framework_rows(self):
        evidence = self._summary_fixture([
            {"assembly": "A.Tests.dll", "framework": "net8.0", "status": "Passed!", "passed": 2, "failed": 0, "skipped": 1, "total": 3},
            {"assembly": "B.Tests.dll", "framework": "net10.0", "status": "Passed!", "passed": 3, "failed": 0, "skipped": 0, "total": 3},
        ])

        self.assertEqual(
            {"totals": {"passed": 5, "failed": 0, "skipped": 1, "total": 6}, "runCount": 2},
            _validated_framework_summary(evidence),
        )

    def test_stale_framework_summary_header_is_rejected(self):
        evidence = self._summary_fixture([
            {"assembly": "A.Tests.dll", "framework": "net10.0", "status": "Passed!", "passed": 3, "failed": 0, "skipped": 1, "total": 4},
        ])

        for field, value in (
            ("assemblyFrameworkRuns", 0),
            ("totalsFromPassedSummaries", {"passed": 2, "failed": 0, "skipped": 1, "total": 4}),
        ):
            with self.subTest(field=field):
                tampered = copy.deepcopy(evidence)
                tampered["observedTestResults"]["frameworkConsoleSummaries"][field] = value
                with self.assertRaisesRegex(ValueError, "Framework summary"):
                    _validated_framework_summary(tampered)

    def test_actual_run_evidence_parser_rejects_stale_framework_header(self):
        project = {"name": "A.Tests", "path": "test/A.Tests/A.Tests.csproj"}
        evidence = self._summary_fixture([
            {"assembly": "A.Tests.dll", "framework": "net10.0", "status": "Passed!", "passed": 3, "failed": 0, "skipped": 1, "total": 4},
        ])
        trx_rows = [{
            "file": "A.Tests.trx",
            "counters": {"total": 4, "executed": 3, "passed": 3, "failed": 0, "error": 0, "timeout": 0, "aborted": 0},
            "codeBases": ["/unused/test/A.Tests/bin/Debug/net10.0/A.Tests.dll"],
        }]
        evidence.update({
            "selection": {
                "projects": [project],
                "canonicalSolutionProjectEntries": 1,
                "nukeSelectedTestProjects": 1,
                "commandsObserved": 1,
                "uniqueCommandProjectPaths": 1,
            },
            "invocation": {
                "exitCode": 0,
                "targets": {"restore": "succeeded", "compile": "succeeded", "test": "succeeded"},
            },
        })
        evidence["observedTestResults"].update({
            "retainedTrx": self._retained_trx_fixture(trx_rows),
            "selectedProjectsWithoutPassingTestCases": [],
        })
        parsed = _parse_test_run_evidence(evidence, Path("/unused"), [(project["name"], project["path"])])
        self.assertEqual({"passed": 3, "failed": 0, "skipped": 1, "total": 4}, parsed["frameworkSummaryTotals"])

        evidence["invocation"]["workingDirectory"] = "/unused"
        relocated = _parse_test_run_evidence(evidence, Path("/relocated"), [(project["name"], project["path"])])
        self.assertEqual(parsed["frameworkSummaryTotals"], relocated["frameworkSummaryTotals"])
        original_code_bases = list(evidence["observedTestResults"]["retainedTrx"]["rows"][0]["codeBases"])
        evidence["observedTestResults"]["retainedTrx"]["rows"][0]["codeBases"] = [
            "/elsewhere/test/A.Tests/bin/Debug/net10.0/A.Tests.dll"
        ]
        with self.assertRaisesRegex(ValueError, "escapes recorded rehearsal"):
            _parse_test_run_evidence(evidence, Path("/relocated"), [(project["name"], project["path"])])
        evidence["observedTestResults"]["retainedTrx"]["rows"][0]["codeBases"] = original_code_bases

        evidence["observedTestResults"]["frameworkConsoleSummaries"]["totalsFromPassedSummaries"]["passed"] = 2
        with self.assertRaisesRegex(ValueError, "Framework summary header totals"):
            _parse_test_run_evidence(evidence, Path("/unused"), [(project["name"], project["path"])])

        evidence = copy.deepcopy(evidence)
        evidence["observedTestResults"]["frameworkConsoleSummaries"]["totalsFromPassedSummaries"]["passed"] = 3
        evidence["observedTestResults"]["retainedTrx"]["passed"] = 2
        with self.assertRaisesRegex(ValueError, "Retained TRX summary header totals"):
            _parse_test_run_evidence(evidence, Path("/unused"), [(project["name"], project["path"])])

    def test_invalid_framework_row_totals_and_failed_status_are_rejected(self):
        evidence = self._summary_fixture([
            {"assembly": "A.Tests.dll", "framework": "net10.0", "status": "Passed!", "passed": 2, "failed": 0, "skipped": 0, "total": 3},
        ])
        with self.assertRaisesRegex(ValueError, "run totals do not reconcile"):
            _validated_framework_summary(evidence)

        run = evidence["observedTestResults"]["frameworkConsoleSummaries"]["runs"][0]
        run.update({"status": "Failed!", "total": 2})
        with self.assertRaisesRegex(ValueError, "did not pass"):
            _validated_framework_summary(evidence)

    def test_altered_dependency_changes_selected_impact_closure(self):
        names = {
            "src/Core/Core.csproj": "Elsa.Core",
            "test/Core.Tests/Core.Tests.csproj": "Elsa.Core.Tests",
            "test/Unrelated.Tests/Unrelated.Tests.csproj": "Elsa.Unrelated.Tests",
        }
        original = graph_from_edges(names, {
            "src/Core/Core.csproj": [],
            "test/Core.Tests/Core.Tests.csproj": ["src/Core/Core.csproj"],
            "test/Unrelated.Tests/Unrelated.Tests.csproj": [],
        })
        changed = graph_from_edges(names, {
            "src/Core/Core.csproj": [],
            "test/Core.Tests/Core.Tests.csproj": [],
            "test/Unrelated.Tests/Unrelated.Tests.csproj": [],
        })

        self.assertEqual(
            {("elsa-core", "test/Core.Tests/Core.Tests.csproj")},
            original.affected_tests([("elsa-core", "src/Core/Core.csproj")]),
        )
        self.assertEqual(set(), changed.affected_tests([("elsa-core", "src/Core/Core.csproj")]))

    def test_reference_to_missing_solution_project_fails_closed(self):
        names = {"src/Core/Core.csproj": "Elsa.Core"}
        with self.assertRaisesRegex(ValueError, "absent from Elsa.sln"):
            graph_from_edges(names, {"src/Core/Core.csproj": ["src/Missing/Missing.csproj"]})

    def test_restore_target_framework_metadata_preserves_cross_tfm_edge(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp).resolve()
            core_path = "src/Core/Core.csproj"
            test_path = "test/Core.Tests/Core.Tests.csproj"
            core_file = root / core_path
            test_file = root / test_path
            core_file.parent.mkdir(parents=True)
            test_file.parent.mkdir(parents=True)
            core_file.touch()
            test_file.touch()
            core_abs = str(core_file)
            core_assets = {
                "project": {"restore": {"frameworks": {"net9.0": {"projectReferences": {}}}}},
                "targets": {"net9.0": {}},
                "libraries": {},
            }
            test_assets = {
                "project": {
                    "restore": {
                        "frameworks": {
                            "net10.0": {"projectReferences": {core_abs: {"projectPath": core_abs}}},
                        },
                    },
                },
                "targets": {
                    "net10.0": {
                        "Elsa.Core/1.0.0": {
                            "type": "project",
                            "framework": ".NETCoreApp,Version=v9.0",
                        },
                    },
                },
                "libraries": {
                    "Elsa.Core/1.0.0": {
                        "type": "project",
                        "msbuildProject": "../../src/Core/Core.csproj",
                    },
                },
            }

            graph, node_map, edge_rows = _framework_graph(
                root,
                {core_path: "Elsa.Core", test_path: "Elsa.Core.Tests"},
                {core_path: core_assets, test_path: test_assets},
            )

            self.assertEqual([(core_path, "net9.0")], edge_rows[(test_path, "net10.0")])
            selected = graph.affected_tests([("elsa-core", _node_key(core_path, "net9.0"))])
            self.assertEqual({("elsa-core", _node_key(test_path, "net10.0"))}, selected)
            self.assertEqual((test_path, "net10.0"), node_map[_node_key(test_path, "net10.0")])

    def test_missing_restore_assets_fail_closed(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            project = root / "src/Core/Core.csproj"
            project.parent.mkdir(parents=True)
            project.touch()
            with self.assertRaisesRegex(ValueError, "missing project.assets.json"):
                _assets_for_project(root, "src/Core/Core.csproj")

    def test_solution_allows_same_non_test_display_name_but_rejects_duplicate_test_name(self):
        with tempfile.TemporaryDirectory() as temp:
            solution = Path(temp) / "Elsa.sln"
            solution.write_text(
                'Project("{11111111-1111-1111-1111-111111111111}") = "Elsa.Server.Web", "samples/A/Elsa.Server.Web.csproj", "{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}"\n'
                'Project("{11111111-1111-1111-1111-111111111111}") = "Elsa.Server.Web", "samples/B/Elsa.Server.Web.csproj", "{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}"\n',
                encoding="utf-8",
            )
            self.assertEqual(2, len(parse_solution(solution)))
            solution.write_text(
                'Project("{11111111-1111-1111-1111-111111111111}") = "Example.Tests", "test/A/Example.Tests.csproj", "{AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA}"\n'
                'Project("{11111111-1111-1111-1111-111111111111}") = "Example.Tests", "test/B/Example.Tests.csproj", "{BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB}"\n',
                encoding="utf-8",
            )
            with self.assertRaisesRegex(ValueError, "Duplicate selected test project name"):
                parse_solution(solution)


if __name__ == "__main__":
    unittest.main()
