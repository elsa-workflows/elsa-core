"""Keep Elsa.sln's grouping and the generated solution filters in line with solution-groups.json."""

from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

import solution_groups as sg


class SolutionGroupsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.manifest = json.loads(sg.MANIFEST.read_text(encoding="utf-8"))
        cls.text = sg.SOLUTION.read_text(encoding="utf-8-sig")
        cls.projects, _, _ = sg.read_solution(cls.text)

    def test_committed_solution_and_filters_match_the_generator(self) -> None:
        for path, content in sg.generate().items():
            with self.subTest(file=path.name):
                self.assertEqual(content, path.read_text(encoding="utf-8-sig"))

    def test_every_filter_selects_existing_solution_projects_once(self) -> None:
        in_solution = {project.path for project in self.projects}
        for path in sorted(sg.ROOT.glob("Elsa.*.slnf")):
            with self.subTest(filter=path.name):
                selected = json.loads(path.read_text(encoding="utf-8-sig"))["solution"]["projects"]
                self.assertEqual(len(selected), len(set(selected)))
                self.assertLessEqual(set(selected), in_solution)
                self.assertTrue(all((sg.ROOT / path).is_file() for path in selected))

    def test_foundation_filter_holds_no_optional_source_project_outside_test_closures(self) -> None:
        groups = sg.classify(self.projects, self.manifest)
        foundation_sources = {project.path for project, group in groups.items()
                              if group == sg.FOUNDATION and not project.is_test}
        selected = set(json.loads((sg.ROOT / "Elsa.Foundation.slnf").read_text(encoding="utf-8"))["solution"]["projects"])
        self.assertLessEqual(foundation_sources, selected)

    def test_unclassified_project_is_rejected(self) -> None:
        stray = sg.SolutionProject("Elsa.Unlisted", "src/elsewhere/Elsa.Unlisted/Elsa.Unlisted.csproj", "0" * 32)
        with self.assertRaisesRegex(ValueError, "missing from scripts/solution/solution-groups.json"):
            sg.classify([*self.projects, stray], self.manifest)

    def test_foundation_may_not_depend_on_an_optional_project(self) -> None:
        manifest = json.loads(json.dumps(self.manifest))
        manifest["foundation"].remove("Elsa.Expressions.Liquid")
        manifest["domains"]["Scripting"]["names"].append("Elsa.Expressions.Liquid")
        with self.assertRaisesRegex(ValueError, "Elsa.Http -> Elsa.Expressions.Liquid"):
            sg.classify(self.projects, manifest)

    def test_tests_follow_the_project_they_cover(self) -> None:
        groups = {project.name: group for project, group in sg.classify(self.projects, self.manifest).items()}
        self.assertEqual("Runtime", groups["Elsa.ServiceBus.MassTransit.UnitTests"])
        self.assertEqual(sg.FOUNDATION, groups["Elsa.Testing.Shared"])

    def test_stale_and_missing_outputs_are_reported(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            current, drifted, missing = (Path(directory) / name for name in ("current.slnf", "drifted.slnf", "missing.slnf"))
            current.write_text("same\n", encoding="utf-8")
            drifted.write_text("old\n", encoding="utf-8")
            outputs = {current: "same\n", drifted: "new\n", missing: "new\n"}
            self.assertEqual([drifted, missing], sg.stale(outputs))

    def test_missing_project_file_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "Elsa.sln").write_text(
                'Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Elsa.Gone", "src\\Elsa.Gone\\Elsa.Gone.csproj", '
                '"{00000000-0000-0000-0000-000000000001}"\nEndProject\n', encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "do not exist: src/Elsa.Gone/Elsa.Gone.csproj"):
                sg.generate(root)


if __name__ == "__main__":
    unittest.main()
