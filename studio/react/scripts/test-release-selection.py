"""Release selection rejects publication without the explicit cutover and exact tag."""

import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("selection", Path(__file__).with_name("validate-release-selection.py"))
selection = importlib.util.module_from_spec(spec)
spec.loader.exec_module(selection)


class ReleaseSelectionTests(unittest.TestCase):
    def test_prepare_preview_and_plain_versions_without_authority(self):
        for version in ("3.11.0-preview.1", "3.11.0", "3.12.1-preview.2"):
            selection.validate(version, "false", "", "refs/heads/proof", "contributor/fork")

    def test_reject_versions_outside_the_release_stream(self):
        for version in ("3.10.9", "3.10.0-preview.1", "4.0.0", "3.11.0-rc.1", "v3.11.0", "3.011.0", "3.11.0\nversion=bad"):
            with self.subTest(version=version), self.assertRaises(ValueError):
                selection.validate(version, "false", "", "refs/heads/proof", "elsa-workflows/elsa-core")

    def test_publication_requires_cutover_repository_and_matching_tag(self):
        for enabled, ref, repository in (("", "refs/tags/3.11.0", "elsa-workflows/elsa-core"),
                                         ("true", "refs/heads/main", "elsa-workflows/elsa-core"),
                                         ("true", "refs/tags/3.11.1", "elsa-workflows/elsa-core"),
                                         ("true", "refs/tags/3.11.0", "contributor/fork")):
            with self.subTest(enabled=enabled, ref=ref, repository=repository), self.assertRaises(ValueError):
                selection.validate("3.11.0", "true", enabled, ref, repository)
        selection.validate("3.11.0", "true", "true", "refs/tags/3.11.0", "elsa-workflows/elsa-core")


if __name__ == "__main__":
    unittest.main()
