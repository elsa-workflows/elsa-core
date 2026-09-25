import copy
import gzip
import hashlib
import json
import subprocess
import tempfile
import unittest
from pathlib import Path

from refresh_canonical_dependency_graph import (
    CURRENT_TIP_PATCH_PATHS,
    CURRENT_TIP_PROFILES,
    CURRENT_TIP_SOURCE_COMMITS,
    E96_SOURCE_COMMITS,
    _classify_test_observation,
    _validate_overlay_build_receipt,
    _validate_overlay_receipt,
    _validate_test_profile_pins,
    _verify_prepared_source_files,
    _assets_for_project,
    _framework_graph,
    _include_restored_project_references,
    _node_key,
    _parse_test_run_evidence,
    _validated_framework_summary,
    graph_from_edges,
    parse_solution,
)


class CanonicalDependencyGraphTests(unittest.TestCase):
    def test_reviewed_source_profiles_pin_committed_receipt_bytes(self):
        repository = Path(__file__).resolve().parents[2]
        for name, profile in CURRENT_TIP_PROFILES.items():
            with self.subTest(profile=name):
                directory = repository / "doc/integration-program/consolidation" / profile["evidenceDirectory"]
                for receipt, digest_key in (
                    ("import-receipt.json.gz", "importReceiptSha256"),
                    ("consolidated-build-receipt.json.gz", "preparationReceiptSha256"),
                ):
                    raw = gzip.decompress((directory / receipt).read_bytes())
                    self.assertEqual(profile[digest_key], hashlib.sha256(raw).hexdigest())
                    self.assertEqual(profile["sourceCommits"], json.loads(raw)["sourceCommits"])

        e96 = repository / "doc/integration-program/consolidation/current-tip-e96-evidence"
        for receipt, expected_sha256 in (
            ("full-suite-nuke-test-evidence.json.gz", "be4926862ba5457d6ee6fdb91d9611f4aa4e99045e7a654c39ad76fa2361e20a"),
            ("canonical-dependency-closure-tested.json.gz", "263692b07bff0d2851e11e762bb628b3593359caa9c8ecac9c42b3d14eda4f89"),
        ):
            with self.subTest(receipt=receipt):
                raw = gzip.decompress((e96 / receipt).read_bytes())
                self.assertEqual(expected_sha256, hashlib.sha256(raw).hexdigest())

    def test_current_tip_explicit_skip_requires_matching_zero_execution_trx(self):
        path = "test/extensions/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj"
        counters = {"total": 1, "executed": 0, "passed": 0, "failed": 0}
        explanation = "Selected; retained TRX records 1 case(s) with zero executed tests; NUKE summary was Skipped!."
        evidence = {
            "runByPathFramework": {},
            "trxByPathFramework": {(path, "net10.0"): counters},
            "selectedWithoutPass": {path: explanation},
        }
        observed = _classify_test_observation(path, "net10.0", evidence, ["Microsoft.NET.Test.Sdk"])
        self.assertEqual("selected-but-skipped", observed["status"])
        self.assertEqual(1, observed["skipped"])

        evidence["selectedWithoutPass"][path] = explanation.replace("Skipped!", "not emitted")
        with self.assertRaisesRegex(ValueError, "no matching passing summary or explicit skip"):
            _classify_test_observation(path, "net10.0", evidence, ["Microsoft.NET.Test.Sdk"])

        evidence["selectedWithoutPass"][path] = explanation
        counters["passed"] = 1
        with self.assertRaisesRegex(ValueError, "no matching passing summary or explicit skip"):
            _classify_test_observation(path, "net10.0", evidence, ["Microsoft.NET.Test.Sdk"])

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
        evidence["invocation"]["workingDirectory"] = "/tmp/recorded-rehearsal"
        evidence["observedTestResults"]["retainedTrx"]["rows"][0]["codeBases"] = [
            "/private/tmp/recorded-rehearsal/test/A.Tests/bin/Debug/net10.0/A.Tests.dll"
        ]
        aliased = _parse_test_run_evidence(evidence, Path("/relocated"), [(project["name"], project["path"])])
        self.assertEqual(parsed["frameworkSummaryTotals"], aliased["frameworkSummaryTotals"])
        evidence["invocation"]["workingDirectory"] = "/unused"
        evidence["observedTestResults"]["retainedTrx"]["rows"][0]["codeBases"] = original_code_bases
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

    def test_restored_off_solution_project_is_in_dependency_graph_but_not_test_selection(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp).resolve()
            test_path = "test/Consumer.Tests/Consumer.Tests.csproj"
            helper_path = "src/Helper/Helper.csproj"
            test_file = root / test_path
            helper_file = root / helper_path
            test_file.parent.mkdir(parents=True)
            helper_file.parent.mkdir(parents=True)
            test_file.touch()
            helper_file.touch()
            test_assets = {
                "project": {"restore": {"frameworks": {
                    "net10.0": {"projectReferences": {str(helper_file): {"projectPath": str(helper_file)}}},
                }}},
                "targets": {"net10.0": {"Helper/1.0.0": {
                    "type": "project", "framework": ".NETCoreApp,Version=v10.0",
                }}},
                "libraries": {"Helper/1.0.0": {
                    "type": "project", "msbuildProject": "../../src/Helper/Helper.csproj",
                }},
            }
            helper_assets = {
                "project": {"restore": {
                    "projectPath": str(helper_file),
                    "frameworks": {"net10.0": {"projectReferences": {}}},
                }},
                "targets": {"net10.0": {}},
                "libraries": {},
            }
            for project_file, document in (
                (test_file, test_assets),
                (helper_file, helper_assets),
            ):
                assets_path = project_file.parent / "obj/project.assets.json"
                assets_path.parent.mkdir(parents=True)
                assets_path.write_text(json.dumps(document), encoding="utf-8")

            names = {test_path: "Consumer.Tests"}
            documents = {test_path: test_assets}
            _include_restored_project_references(root, names, documents)
            graph, _, _ = _framework_graph(root, names, documents, {test_path})

            self.assertIn(helper_path, names)
            self.assertEqual(
                {("elsa-core", _node_key(test_path, "net10.0"))},
                graph.affected_tests([("elsa-core", _node_key(helper_path, "net10.0"))]),
            )

    def test_missing_restore_assets_fail_closed(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            project = root / "src/Core/Core.csproj"
            project.parent.mkdir(parents=True)
            project.touch()
            with self.assertRaisesRegex(ValueError, "missing project.assets.json"):
                _assets_for_project(root, "src/Core/Core.csproj")

    def test_current_tip_overlay_receipt_pins_reviewed_source_and_patch_bytes(self):
        rows = [
            {"name": name, "sha256": hashlib.sha256(
                (Path(__file__).resolve().parents[2] / patch_path).read_bytes()
            ).hexdigest()}
            for name, patch_path in CURRENT_TIP_PATCH_PATHS.items()
        ]
        with tempfile.TemporaryDirectory() as temp:
            receipt_path = Path(temp) / "overlays.json"
            receipt_path.write_text(json.dumps({
                "sourcePins": CURRENT_TIP_SOURCE_COMMITS,
                "reviewedOverlayReceipt": rows,
                "publicationAuthorized": False,
            }), encoding="utf-8")
            self.assertEqual(rows, _validate_overlay_receipt(receipt_path))
            e96_receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
            e96_receipt["sourcePins"] = E96_SOURCE_COMMITS
            e96_path = Path(temp) / "e96-overlays.json"
            e96_path.write_text(json.dumps(e96_receipt), encoding="utf-8")
            self.assertEqual(rows, _validate_overlay_receipt(e96_path, E96_SOURCE_COMMITS))
            with self.assertRaisesRegex(ValueError, "does not pin the selected source profile"):
                _validate_overlay_receipt(e96_path)

            for incomplete in (rows[:-1], rows[::-1]):
                receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
                receipt["reviewedOverlayReceipt"] = incomplete
                invalid_path = Path(temp) / "incomplete.json"
                invalid_path.write_text(json.dumps(receipt), encoding="utf-8")
                with self.assertRaisesRegex(ValueError, "complete current-tip patch set in order"):
                    _validate_overlay_receipt(invalid_path)

            receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
            receipt["sourcePins"]["core"] = "0" * 40
            receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "does not pin the selected source profile"):
                _validate_overlay_receipt(receipt_path)

    def test_current_tip_source_verifier_rejects_non_overlay_imported_edit(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            unchanged = root / "src/Unchanged.csproj"
            prepared = root / "src/Prepared.csproj"
            unchanged.parent.mkdir()
            unchanged.write_text("<Project />\n", encoding="utf-8")
            prepared.write_text("<Project />\n", encoding="utf-8")
            subprocess.run(["git", "init", "-q", str(root)], check=True)
            subprocess.run(["git", "-C", str(root), "add", "."], check=True)
            subprocess.run(["git", "-C", str(root), "-c", "user.name=Source Test",
                            "-c", "user.email=source-test@example.invalid", "commit", "-qm", "source"], check=True)
            prepared.write_text("<Project Sdk=\"Microsoft.NET.Sdk\" />\n", encoding="utf-8")
            files = [{"path": "src/Prepared.csproj", "sha256": hashlib.sha256(prepared.read_bytes()).hexdigest()}]
            _verify_prepared_source_files(root, files, set())

            unchanged.write_text("<Project TargetFramework=\"net9.0\" />\n", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "outside reviewed preparation"):
                _verify_prepared_source_files(root, files, set())
            unchanged.write_text("<Project />\n", encoding="utf-8")
            prepared.write_text("<Project TargetFramework=\"net9.0\" />\n", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "differs from its accepted receipt"):
                _verify_prepared_source_files(root, files, set())

    def test_current_tip_overlay_build_receipt_must_prove_same_overlay_set(self):
        overlays = [{"name": "patch.patch", "sha256": "a" * 64}]
        receipt = {
            "sourcePins": CURRENT_TIP_SOURCE_COMMITS,
            "syntheticRehearsalCommit": "b" * 40,
            "canonicalImportBuilt": False,
            "fullCombinedTestSuiteVerified": False,
            "builds": {"overlaid": {"exitCode": 0, "errorCount": 0, "appliedOverlays": overlays}},
        }
        with tempfile.TemporaryDirectory() as temp:
            receipt_path = Path(temp) / "mapped-solution-build.json"
            receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
            self.assertEqual(receipt, _validate_overlay_build_receipt(receipt_path, overlays, "b" * 40))
            receipt["builds"]["overlaid"]["errorCount"] = 1
            receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "does not record a passing overlaid build"):
                _validate_overlay_build_receipt(receipt_path, overlays, "b" * 40)

    def test_current_tip_test_evidence_must_pin_receipts_and_overlay_hashes(self):
        source_receipts = {
            "preparationReceiptSha256": "a" * 64,
            "importReceiptSha256": "b" * 64,
            "sourceIntegrationPatchSha256": "c" * 64,
        }
        overlays = [{"name": "workbench-canonical-secrets.patch", "sha256": "d" * 64}]
        evidence = {"profile": {
            **CURRENT_TIP_SOURCE_COMMITS,
            "rawRehearsalCommit": "e" * 40,
            "canonicalSolution": "Elsa.sln",
            "sourceReceipts": source_receipts,
            "supplementalPatches": overlays,
            "overlayReceiptSha256": "f" * 64,
        }}
        self.assertEqual(source_receipts, _validate_test_profile_pins(
            evidence, CURRENT_TIP_SOURCE_COMMITS, "e" * 40, source_receipts, overlays, "f" * 64,
        ))
        evidence["profile"]["supplementalPatches"] = []
        with self.assertRaisesRegex(ValueError, "differ from the reviewed overlay receipt"):
            _validate_test_profile_pins(evidence, CURRENT_TIP_SOURCE_COMMITS, "e" * 40, source_receipts, overlays, "f" * 64)
        evidence["profile"]["supplementalPatches"] = overlays
        evidence["profile"]["overlayReceiptSha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "does not hash the reviewed overlay receipt"):
            _validate_test_profile_pins(evidence, CURRENT_TIP_SOURCE_COMMITS, "e" * 40, source_receipts, overlays, "f" * 64)

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
