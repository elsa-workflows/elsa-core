import os
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parent / "integration-program"))

import consolidated_candidate_input as candidate_input
import stage_consolidated_candidate_nupkgs as stage

SCRIPT = Path(__file__).with_name("stage_consolidated_candidate_nupkgs.py")


class StageCandidateTests(unittest.TestCase):
    def test_pinned_identity_matches_the_candidate_input(self):
        self.assertEqual(f"refs/tags/{candidate_input.PRODUCER['version']}", stage.PINNED_TAG)
        self.assertEqual("3.10.0", candidate_input.PRODUCER["version"])
        self.assertEqual(
            f"consolidated-candidate-{candidate_input.PRODUCER['source_commit']}-"
            f"{candidate_input.PRODUCER['run_id']}-{candidate_input.PRODUCER['run_attempt']}",
            stage.CANDIDATE_NAME,
        )

    def test_only_the_pinned_stable_tag_is_accepted(self):
        stage.require_pinned_stable_tag("refs/tags/3.10.0")
        for ref in ("refs/tags/3.10.1", "refs/tags/3.10.0-rc1", "refs/tags/3.8.5", "refs/heads/main"):
            with self.subTest(ref=ref), self.assertRaises(SystemExit):
                stage.require_pinned_stable_tag(ref)

    def test_extract_copies_nupkgs_and_ignores_other_members(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / "candidate.zip"
            output = root / "packages"
            with zipfile.ZipFile(archive, "w") as zipped:
                zipped.writestr("artifacts/Elsa.3.10.0.nupkg", b"elsa")
                zipped.writestr("artifacts/Elsa.Studio.DomInterop.3.10.0.nupkg", b"dom")
                zipped.writestr("artifacts/Elsa.3.10.0.snupkg", b"symbols")
                zipped.writestr("preupload-manifest.json", b"{}")
            staged = stage.extract_nupkgs(archive, output)
            names = sorted(path.name for path in staged)
            self.assertEqual(["Elsa.3.10.0.nupkg", "Elsa.Studio.DomInterop.3.10.0.nupkg"], names)
            self.assertEqual(b"elsa", (output / "Elsa.3.10.0.nupkg").read_bytes())
            self.assertFalse((output / "Elsa.3.10.0.snupkg").exists())

    def test_stage_command_refuses_a_token_and_requires_the_pinned_tag(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / "candidate.zip"
            with zipfile.ZipFile(archive, "w") as zipped:
                zipped.writestr("artifacts/Elsa.3.10.0.nupkg", b"elsa")
            env = {
                key: value for key, value in os.environ.items()
                if key not in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")
            }
            env.update(REF="refs/tags/3.10.0", CANDIDATE_ARCHIVE=str(archive), PACKAGES_DIR=str(root / "out"))
            accepted = subprocess.run([sys.executable, str(SCRIPT), "stage"], env=env, capture_output=True, text=True)
            self.assertEqual(0, accepted.returncode, accepted.stderr)
            self.assertTrue((root / "out" / "Elsa.3.10.0.nupkg").is_file())

            rejected_tag = subprocess.run(
                [sys.executable, str(SCRIPT), "stage"],
                env={**env, "REF": "refs/tags/3.10.1"},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, rejected_tag.returncode)
            self.assertIn("pinned to refs/tags/3.10.0", rejected_tag.stderr)

            rejected_token = subprocess.run(
                [sys.executable, str(SCRIPT), "stage"],
                env={**env, "GH_TOKEN": "secret"},
                capture_output=True,
                text=True,
            )
            self.assertEqual(1, rejected_token.returncode)
            self.assertIn("must not enter candidate staging", rejected_token.stderr)


class WorkflowPromotionTests(unittest.TestCase):
    def test_packages_workflow_promotes_candidate_bytes_on_stable_310_nuget(self):
        workflow = Path(__file__).resolve().parents[1] / ".github/workflows/packages.yml"
        source = workflow.read_text(encoding="utf-8")
        self.assertIn("python3 scripts/stage_consolidated_candidate_nupkgs.py retrieve", source)
        self.assertIn("python3 scripts/stage_consolidated_candidate_nupkgs.py stage", source)
        self.assertIn("python3 scripts/validate_packages_id_set.py", source)
        self.assertIn("contains(github.ref_name, '-')", source)
        self.assertIn("startsWith(github.ref, 'refs/tags/3.10.') && contains(github.ref_name, '-')", source)
        self.assertNotIn("candidate_artifact_id:", source)
        self.assertNotIn("candidate_run_id:", source)
        retrieve = source.find("name: Retrieve verified candidate archive")
        login = source.find("name: NuGet login (OIDC)")
        self.assertGreater(retrieve, 0)
        self.assertGreater(login, retrieve)


if __name__ == "__main__":
    unittest.main()
