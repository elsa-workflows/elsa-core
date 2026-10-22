import hashlib
import io
import json
import os
import subprocess
import sys
import tempfile
import unittest
import unittest.mock
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parent / "integration-program"))

import consolidated_candidate_input as candidate_input
import stage_consolidated_candidate_nupkgs as stage

SCRIPT = Path(__file__).with_name("stage_consolidated_candidate_nupkgs.py")


def _manifest_bytes(packages: dict[str, bytes]) -> bytes:
    files = [
        {"path": f"artifacts/{name}", "size": len(data), "sha256": hashlib.sha256(data).hexdigest()}
        for name, data in packages.items()
    ]
    return json.dumps(
        {"schema": 1, "published": False, "version": "3.10.0", "files": files},
        separators=(",", ":"),
    ).encode()


def write_sealed_archive(archive: Path, packages: dict[str, bytes], extra: dict[str, bytes] | None = None) -> str:
    manifest = _manifest_bytes(packages)
    with zipfile.ZipFile(archive, "w") as zipped:
        zipped.writestr(stage.SEAL_NAME, manifest)
        for name, data in packages.items():
            zipped.writestr(f"artifacts/{name}", data)
        for name, data in (extra or {}).items():
            zipped.writestr(name, data)
    return hashlib.sha256(manifest).hexdigest()


class StageCandidateTests(unittest.TestCase):
    def test_pinned_identity_matches_the_candidate_input(self):
        self.assertEqual(f"refs/tags/{candidate_input.PRODUCER['version']}", stage.PINNED_TAG)
        self.assertEqual("3.10.0", candidate_input.PRODUCER["version"])
        self.assertEqual(
            f"consolidated-candidate-{candidate_input.PRODUCER['source_commit']}-"
            f"{candidate_input.PRODUCER['run_id']}-{candidate_input.PRODUCER['run_attempt']}",
            stage.CANDIDATE_NAME,
        )
        envelope = json.loads(candidate_input.ORIGINAL_ENVELOPE.read_text(encoding="utf-8"))
        self.assertEqual(envelope["archive_size"], stage.CANDIDATE_SIZE)
        self.assertEqual(envelope["expires_at"], stage.CANDIDATE_EXPIRES)

    def test_only_the_pinned_stable_tag_is_accepted(self):
        stage.require_pinned_stable_tag("refs/tags/3.10.0")
        for ref in ("refs/tags/3.10.1", "refs/tags/3.10.0-rc1", "refs/tags/3.8.5", "refs/heads/main"):
            with self.subTest(ref=ref), self.assertRaises(SystemExit):
                stage.require_pinned_stable_tag(ref)

    def test_extract_copies_nupkgs_and_snupkgs_and_ignores_other_members(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / "candidate.zip"
            output = root / "packages"
            packages = {
                "Elsa.3.10.0.nupkg": b"elsa",
                "Elsa.Studio.DomInterop.3.10.0.nupkg": b"dom",
                "Elsa.3.10.0.snupkg": b"symbols",
            }
            write_sealed_archive(
                archive,
                packages,
                extra={"artifacts/notes.txt": b"no"},
            )
            staged = stage.extract_nupkgs(archive, output)
            names = sorted(path.name for path in staged)
            self.assertEqual(
                ["Elsa.3.10.0.nupkg", "Elsa.3.10.0.snupkg", "Elsa.Studio.DomInterop.3.10.0.nupkg"],
                names,
            )
            self.assertEqual(b"elsa", (output / "Elsa.3.10.0.nupkg").read_bytes())
            self.assertEqual(b"symbols", (output / "Elsa.3.10.0.snupkg").read_bytes())
            self.assertFalse((output / "notes.txt").exists())

    def test_seal_hash_and_nupkg_hashes_must_match(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / "candidate.zip"
            packages = {"Elsa.3.10.0.nupkg": b"elsa"}
            digest = write_sealed_archive(archive, packages)
            sealed = stage.sealed_nupkg_hashes(archive, digest)
            self.assertEqual({"Elsa.3.10.0.nupkg": hashlib.sha256(b"elsa").hexdigest()}, sealed)
            staged = [root / "Elsa.3.10.0.nupkg"]
            staged[0].write_bytes(b"elsa")
            stage.verify_nupkg_hashes(staged, sealed)

            with self.assertRaises(SystemExit):
                stage.sealed_nupkg_hashes(archive, "0" * 64)

            staged[0].write_bytes(b"tampered")
            with self.assertRaises(SystemExit):
                stage.verify_nupkg_hashes(staged, sealed)

    def test_stage_command_refuses_a_token_and_requires_the_pinned_tag(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / "candidate.zip"
            packages = {"Elsa.3.10.0.nupkg": b"elsa"}
            digest = write_sealed_archive(archive, packages)
            env = {
                key: value for key, value in os.environ.items()
                if key not in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")
            }
            env.update(REF="refs/tags/3.10.0", CANDIDATE_ARCHIVE=str(archive), PACKAGES_DIR=str(root / "out"))
            with unittest.mock.patch.dict(os.environ, env, clear=True), unittest.mock.patch.object(
                candidate_input, "MANIFEST_SHA256", digest
            ):
                self.assertEqual(0, stage.main(["stage"]))
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

    def test_stage_command_rejects_a_tampered_nupkg(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / "candidate.zip"
            packages = {"Elsa.3.10.0.nupkg": b"elsa"}
            digest = write_sealed_archive(archive, packages)
            with zipfile.ZipFile(archive, "w") as zipped:
                zipped.writestr(stage.SEAL_NAME, _manifest_bytes(packages))
                zipped.writestr("artifacts/Elsa.3.10.0.nupkg", b"tampered")
            env = {
                key: value for key, value in os.environ.items()
                if key not in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")
            }
            env.update(REF="refs/tags/3.10.0", CANDIDATE_ARCHIVE=str(archive), PACKAGES_DIR=str(root / "out"))
            with unittest.mock.patch.dict(os.environ, env, clear=True), unittest.mock.patch.object(
                candidate_input, "MANIFEST_SHA256", digest
            ):
                with self.assertRaises(SystemExit) as raised:
                    stage.main(["stage"])
            self.assertIn("hash mismatch", str(raised.exception))

    def test_stage_summary_counts_only_nupkgs(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / "candidate.zip"
            packages = {
                "Elsa.3.10.0.nupkg": b"elsa",
                "Elsa.3.10.0.snupkg": b"symbols",
            }
            digest = write_sealed_archive(archive, packages)
            env = {
                key: value for key, value in os.environ.items()
                if key not in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN")
            }
            env.update(REF="refs/tags/3.10.0", CANDIDATE_ARCHIVE=str(archive), PACKAGES_DIR=str(root / "out"))
            stdout = io.StringIO()
            with unittest.mock.patch.dict(os.environ, env, clear=True), unittest.mock.patch.object(
                candidate_input, "MANIFEST_SHA256", digest
            ), unittest.mock.patch.object(sys, "stdout", stdout):
                self.assertEqual(0, stage.main(["stage"]))
            self.assertIn("Staged 1 candidate nupkgs", stdout.getvalue())
            self.assertNotIn("Staged 2 candidate nupkgs", stdout.getvalue())


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
        self.assertNotIn("CONSOLIDATED_RELEASE_CANDIDATE", source)
        self.assertNotIn("build_studio_clientlibs.sh", source)
        retrieve = source.find("name: Retrieve verified candidate archive")
        login = source.find("name: NuGet login (OIDC)")
        self.assertGreater(retrieve, 0)
        self.assertGreater(login, retrieve)


if __name__ == "__main__":
    unittest.main()
