"""Keep the current-tip consolidated Studio test patch narrowly scoped."""

from pathlib import Path
import re
import unittest


PATCH = Path(__file__).resolve().parent / "consolidated-build" / "studio-test-layout-current-tip.patch"
EXPECTED_PATHS = {
    "src/studio/framework/Elsa.Studio.Core.Tests/CssContractTestContext.cs",
    "src/studio/modules/Elsa.Studio.Authentication.UI.Tests/LoginThemeCssContractTests.cs",
    "src/studio/modules/Elsa.Studio.Dashboard.Tests/DashboardWidgetRegistrationTests.cs",
}


class StudioTestLayoutPatchTests(unittest.TestCase):
    def test_patch_changes_only_the_three_relocated_test_sources(self):
        patch = PATCH.read_text(encoding="utf-8")
        changed_paths = set(re.findall(r"^diff --git a/(.*?) b/", patch, re.MULTILINE))

        self.assertEqual(EXPECTED_PATHS, changed_paths)
        self.assertNotRegex(patch, r"(?m)^-[^\n]*Assert\.")


if __name__ == "__main__":
    unittest.main()
