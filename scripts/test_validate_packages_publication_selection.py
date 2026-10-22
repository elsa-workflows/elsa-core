import os
import subprocess
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from validate_packages_publication_selection import selection_errors

SCRIPT = Path(__file__).with_name("validate_packages_publication_selection.py")

ACCEPTED = (
    ("refs/heads/main", set()),
    ("refs/heads/main", {"feedz"}),
    ("refs/heads/feat/new-activity", {"feedz"}),
    ("refs/tags/3.8.4", set()),
    ("refs/tags/3.8.5", {"nuget"}),
    ("refs/tags/3.8.0-rc2", {"nuget"}),
    ("refs/tags/2.17.0", {"nuget"}),
    ("refs/tags/3.10.0", {"nuget"}),
    ("refs/tags/3.10.0-rc1", {"nuget"}),
    ("refs/tags/3.10.0-rc1", {"feedz"}),
    ("refs/tags/3.10.1-preview.2", {"feedz"}),
    ("refs/tags/3.10.1-preview.2", {"nuget"}),
    ("refs/heads/main", {"coverage"}),
    ("refs/heads/release/3.6.1", {"coverage"}),
    ("refs/heads/main", {"feedz", "coverage"}),
)

# Each rejection names the reason it must be rejected for, so a case cannot pass on an unrelated error.
REJECTED = (
    ("refs/heads/main", {"nuget"}, "requires a release tag"),
    ("refs/heads/3.8.5", {"nuget"}, "requires a release tag"),
    ("refs/tags/3.8.5", {"feedz"}, "limited to 3.10.x prerelease"),
    ("refs/tags/3.8.0-rc2", {"feedz"}, "limited to 3.10.x prerelease"),
    ("refs/tags/3.10.0", {"feedz"}, "owned by publish-consolidated-original-candidate"),
    ("refs/tags/3.10.1", {"feedz"}, "owned by publish-consolidated-original-candidate"),
    ("refs/tags/3.10.0", {"feedz", "nuget"}, "owned by publish-consolidated-original-candidate"),
    ("refs/tags/03.10.0", {"nuget"}, "canonical 3.10."),
    ("refs/tags/3.010.0", {"feedz"}, "canonical 3.10."),
    ("refs/tags/3.10.01", {"nuget"}, "canonical patch number"),
    ("refs/tags/3.10.01", {"feedz"}, "canonical patch number"),
    ("refs/tags/3.10.01-rc1", {"nuget"}, "canonical patch number"),
    ("refs/tags/v3.10.0", set(), "not a release version"),
    ("refs/tags/v3.10.0", {"feedz"}, "not a release version"),
    ("refs/tags/v3.8.5", {"nuget"}, "not a release version"),
    ("refs/tags/test-1", set(), "not a release version"),
    ("refs/tags/test-1", {"nuget"}, "not a release version"),
    ("refs/tags/3.8", {"nuget"}, "not a release version"),
    ("refs/tags/3.8.5-", {"nuget"}, "not a release version"),
    ("refs/tags/3.8.5+build.1", {"nuget"}, "not a release version"),
    ("refs/tags/3.8.5-rc..1", {"nuget"}, "not a release version"),
    ("refs/heads/develop/3.6.1", {"coverage"}, "Coverage deployment requires"),
    ("refs/heads/feature/coverage", {"coverage"}, "Coverage deployment requires"),
    ("refs/heads/release/3.8.4", {"coverage"}, "Coverage deployment requires"),
    ("refs/tags/3.8.5", {"coverage"}, "Coverage deployment requires"),
    ("refs/pull/1/merge", set(), "neither a branch nor a tag"),
)


def errors(ref, selected):
    return selection_errors(ref, feedz="feedz" in selected, nuget="nuget" in selected, coverage="coverage" in selected)


class SelectionErrorsTests(unittest.TestCase):
    def test_accepts_matching_selections(self):
        for ref, selected in ACCEPTED:
            with self.subTest(ref=ref, selected=sorted(selected)):
                self.assertEqual([], errors(ref, selected))

    def test_candidate_optins_and_artifact_selectors_have_no_publisher_route(self):
        workflow = (SCRIPT.parents[1] / ".github/workflows/packages.yml").read_text()
        self.assertNotIn("ConsolidatedReleaseCandidate", workflow)
        self.assertNotIn("CONSOLIDATED_RELEASE_CANDIDATE", workflow)
        self.assertNotIn("build_studio_clientlibs.sh", workflow)
        self.assertIn("stage_consolidated_candidate_nupkgs.py", workflow)
        for selector in ("candidate_artifact_id:", "candidate_run_id:", "candidate_workflow:"):
            self.assertNotIn(selector, workflow)
        for selector in ("candidate", "candidate_artifact_id", "candidate_run_id", "candidate_workflow"):
            with self.subTest(selector=selector), self.assertRaises(TypeError):
                selection_errors("refs/tags/3.10.0", **{selector: True})
        self.assertIn("owned by publish-consolidated-original-candidate", " ".join(errors("refs/tags/3.10.0", {"feedz"})))
        self.assertEqual([], errors("refs/tags/3.10.0", {"nuget"}))
        self.assertEqual([], errors("refs/tags/3.10.0-rc1", {"feedz"}))
        self.assertEqual([], errors("refs/tags/3.8.5", {"nuget"}))
        self.assertEqual([], errors("refs/tags/3.9.1", {"nuget"}))

    def test_rejects_mismatched_selections_for_the_stated_reason(self):
        for ref, selected, reason in REJECTED:
            with self.subTest(ref=ref, selected=sorted(selected)):
                self.assertIn(reason, " ".join(errors(ref, selected)))


class SelectionScriptTests(unittest.TestCase):
    """The job runs the script with the ref and inputs in its environment."""

    def run_script(self, ref, feedz="false", nuget="false", coverage="false"):
        env = {
            **os.environ,
            "REF": ref,
            "PUBLISH_PREVIEW_FEEDZ": feedz,
            "PUBLISH_NUGET": nuget,
            "DEPLOY_COVERAGE": coverage,
        }
        return subprocess.run([sys.executable, str(SCRIPT)], env=env, capture_output=True, text=True)

    def test_accepted_selection_exits_zero(self):
        cases = (
            ("refs/tags/3.10.0", {"nuget": "true"}),
            ("refs/tags/3.10.0-rc1", {"feedz": "true"}),
        )
        for ref, kwargs in cases:
            with self.subTest(ref=ref, kwargs=kwargs):
                result = self.run_script(ref, **kwargs)
                self.assertEqual(0, result.returncode, result.stderr)

    def test_rejected_selection_fails_the_step_with_its_reason(self):
        result = self.run_script("refs/tags/3.8.5", feedz="true")
        self.assertEqual(1, result.returncode)
        self.assertIn("limited to 3.10.x", result.stderr)

    def test_input_that_is_not_a_boolean_fails_the_step(self):
        for value in ("", "True", "yes"):
            with self.subTest(value=value):
                result = self.run_script("refs/heads/main", nuget=value)
                self.assertNotEqual(0, result.returncode)
                self.assertIn("PUBLISH_NUGET must be 'true' or 'false'", result.stderr)


if __name__ == "__main__":
    unittest.main()
