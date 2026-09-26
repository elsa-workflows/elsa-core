"""Keep product-focused solution filters aligned with the consolidated solution."""

import json
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[2]
SOLUTION = ROOT / "Elsa.sln"
PRODUCTS = {
    "Elsa.Extensions.slnf": ("src/extensions/", "test/extensions/", "samples/extensions/"),
    "Elsa.Studio.slnf": ("src/studio/", "test/studio/", "samples/studio/"),
}


class ProductSolutionFilterTests(unittest.TestCase):
    def test_filters_cover_exactly_their_product_projects_in_root_solution(self):
        solution_text = SOLUTION.read_text(encoding="utf-8")
        root_projects = {
            path.replace("\\", "/")
            for path in re.findall(
                r'^Project\([^\n]+?=\s*"[^"]+",\s*"([^"]+\.csproj)"',
                solution_text,
                re.MULTILINE,
            )
        }

        for name, prefixes in PRODUCTS.items():
            with self.subTest(filter=name):
                document = json.loads((ROOT / name).read_text(encoding="utf-8"))
                self.assertEqual("Elsa.sln", document["solution"]["path"])
                selected = document["solution"]["projects"]
                self.assertEqual(len(selected), len(set(selected)), "Duplicate filter project")
                self.assertEqual(
                    {path for path in root_projects if path.startswith(prefixes)},
                    set(selected),
                )
                self.assertTrue(selected)
                self.assertTrue(all((ROOT / path).is_file() for path in selected))


if __name__ == "__main__":
    unittest.main()
