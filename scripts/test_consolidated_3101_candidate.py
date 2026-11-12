import hashlib
import json
import os
import sys
import tempfile
import unittest
import unittest.mock
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import consolidated_3101_pin as pin
import verify_consolidated_3101_candidate as verify

WORKFLOW = Path(__file__).resolve().parents[1] / ".github/workflows/publish-consolidated-3.10.1-candidate.yml"
OTHER_COMMIT = "5cff67228e550b353b3483eb3b37dfdb73a41c74"


def good_run():
    return {"id": pin.RUN_ID, "run_attempt": 1, "head_sha": pin.SOURCE_COMMIT, "status": "completed",
            "conclusion": "success", "event": "workflow_dispatch", "path": pin.PRODUCER_WORKFLOW}


def good_artifact():
    return {"id": pin.ARTIFACT_ID, "name": pin.ARTIFACT_NAME, "expired": False, "digest": f"sha256:{pin.ARCHIVE_SHA256}",
            "workflow_run": {"id": pin.RUN_ID, "head_sha": pin.SOURCE_COMMIT}}


def good_compare(status="behind"):
    return {"status": status, "merge_base_commit": {"sha": pin.SOURCE_COMMIT}}


def build_archive(path: Path, packages: dict[str, bytes], *, version="3.10.1", source=pin.SOURCE_COMMIT, tamper=None) -> str:
    files = [{"path": f"artifacts/{name}", "size": len(data), "sha256": hashlib.sha256(data).hexdigest()}
             for name, data in packages.items()]
    manifest = json.dumps({"schema": 1, "published": False, "version": version, "run_id": pin.RUN_ID, "run_attempt": 1,
                           "source_commit": source, "files": files}).encode()
    with zipfile.ZipFile(path, "w") as zipped:
        zipped.writestr(verify.SEAL_NAME, manifest)
        for name, data in packages.items():
            zipped.writestr(f"artifacts/{name}", (tamper or {}).get(name, data))
    return hashlib.sha256(manifest).hexdigest()


def packages(count=3, version="3.10.1"):
    result = {}
    for index in range(count):
        result[f"Pkg{index}.{version}.nupkg"] = f"nupkg {index}".encode()
        result[f"Pkg{index}.{version}.snupkg"] = f"snupkg {index}".encode()
    return result


class RunAndArtifactTests(unittest.TestCase):
    def test_pinned_identity_is_accepted(self):
        verify.check_run(good_run())
        verify.check_artifact(good_artifact())

    def test_staged_3100_run_is_rejected_by_name(self):
        with self.assertRaisesRegex(SystemExit, "staged 3.10.0 candidate run"):
            verify.check_run_id(40347931480)

    def test_any_other_run_is_rejected(self):
        for run_id in (1, 43236516382, 43236516384, 0):
            with self.subTest(run_id=run_id), self.assertRaisesRegex(SystemExit, "not the pinned 3.10.1"):
                verify.check_run_id(run_id)

    def test_staged_3100_artifact_is_rejected_by_name(self):
        with self.assertRaisesRegex(SystemExit, "staged 3.10.0 candidate artifact"):
            verify.check_artifact_id(12664519771)

    def test_any_other_artifact_is_rejected(self):
        with self.assertRaisesRegex(SystemExit, "not the pinned 3.10.1"):
            verify.check_artifact_id(13955429318)

    def test_run_must_have_the_pinned_source_and_succeed(self):
        for change in ({"head_sha": OTHER_COMMIT}, {"head_sha": "0" * 40}, {"conclusion": "failure"},
                       {"run_attempt": 2}, {"event": "push"}, {"path": ".github/workflows/prepare-consolidated-release-candidate.yml"}):
            with self.subTest(change=change), self.assertRaises(SystemExit):
                verify.check_run({**good_run(), **change})

    def test_artifact_must_match_digest_name_expiry_and_run(self):
        for change in ({"digest": "sha256:" + "0" * 64}, {"name": "other"}, {"expired": True},
                       {"workflow_run": {"id": 40347931480, "head_sha": pin.SOURCE_COMMIT}},
                       {"workflow_run": {"id": pin.RUN_ID, "head_sha": OTHER_COMMIT}}):
            with self.subTest(change=change), self.assertRaises(SystemExit):
                verify.check_artifact({**good_artifact(), **change})


class AncestryTests(unittest.TestCase):
    def test_behind_and_identical_are_on_the_release_line(self):
        verify.check_ancestry(good_compare("behind"))
        verify.check_ancestry(good_compare("identical"))

    def test_ahead_and_diverged_are_rejected(self):
        for status in ("ahead", "diverged", None):
            with self.subTest(status=status), self.assertRaisesRegex(SystemExit, "not an ancestor"):
                verify.check_ancestry({**good_compare(), "status": status})

    def test_merge_base_must_be_the_source(self):
        with self.assertRaisesRegex(SystemExit, "merge base"):
            verify.check_ancestry({"status": "behind", "merge_base_commit": {"sha": OTHER_COMMIT}})


class SourceTests(unittest.TestCase):
    def test_other_and_3100_sources_are_rejected(self):
        for commit in (OTHER_COMMIT, "fdf64a76bc967fe07f01fdb7fa9feb93d3086a82", "", None):
            with self.subTest(commit=commit), self.assertRaises(SystemExit):
                verify.check_source(commit, "x")


class ArchiveTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.dir = Path(self.temp.name)

    def patched(self, archive: Path, manifest_sha: str, count: int):
        archive_sha = hashlib.sha256(archive.read_bytes()).hexdigest()
        return unittest.mock.patch.multiple(pin, ARCHIVE_SHA256=archive_sha, MANIFEST_SHA256=manifest_sha, FEEDZ_PACKAGE_COUNT=count)

    def test_archive_hash_must_match_the_pin(self):
        archive = self.dir / "a.zip"
        build_archive(archive, packages())
        with self.assertRaisesRegex(SystemExit, "archive sha256"):
            verify.check_archive(archive)

    def test_valid_archive_is_staged_with_all_packages(self):
        archive = self.dir / "a.zip"
        manifest_sha = build_archive(archive, packages(3))
        with self.patched(archive, manifest_sha, 3):
            verify.check_archive(archive)
            staged = verify.stage_packages(archive, verify.sealed_packages(verify.read_manifest(archive)), self.dir / "out")
        self.assertEqual(6, len(staged))
        self.assertEqual(3, len(list((self.dir / "out").glob("*.nupkg"))))

    def test_manifest_hash_must_match_the_pin(self):
        archive = self.dir / "a.zip"
        build_archive(archive, packages(3))
        with unittest.mock.patch.object(pin, "MANIFEST_SHA256", "0" * 64), self.assertRaisesRegex(SystemExit, "manifest sha256"):
            verify.read_manifest(archive)

    def test_manifest_identity_and_source_are_checked(self):
        for kwargs in ({"version": "3.10.0"}, {"source": OTHER_COMMIT}):
            archive = self.dir / f"{len(kwargs)}{next(iter(kwargs))}.zip"
            manifest_sha = build_archive(archive, packages(3), **kwargs)
            with self.subTest(kwargs=kwargs), unittest.mock.patch.object(pin, "MANIFEST_SHA256", manifest_sha), \
                    self.assertRaises(SystemExit):
                verify.read_manifest(archive)

    def test_wrong_package_count_is_rejected(self):
        archive = self.dir / "a.zip"
        manifest_sha = build_archive(archive, packages(3))
        with self.patched(archive, manifest_sha, 231), self.assertRaisesRegex(SystemExit, "expected 231"):
            verify.sealed_packages(verify.read_manifest(archive))

    def test_package_with_another_version_is_rejected(self):
        archive = self.dir / "a.zip"
        content = packages(2)
        content.update(packages(1, "3.10.0"))
        manifest_sha = build_archive(archive, content)
        with self.patched(archive, manifest_sha, 3), self.assertRaisesRegex(SystemExit, "not version 3.10.1"):
            verify.sealed_packages(verify.read_manifest(archive))

    def test_tampered_package_bytes_are_rejected(self):
        archive = self.dir / "a.zip"
        content = packages(3)
        manifest_sha = build_archive(archive, content, tamper={"Pkg1.3.10.1.nupkg": b"evil"})
        with self.patched(archive, manifest_sha, 3), self.assertRaisesRegex(SystemExit, "hash mismatch"):
            verify.stage_packages(archive, verify.sealed_packages(verify.read_manifest(archive)), self.dir / "out")

    def test_nupkg_count_check(self):
        out = self.dir / "out"
        out.mkdir()
        for index in range(3):
            (out / f"Pkg{index}.3.10.1.nupkg").write_bytes(b"x")
        verify.count_nupkgs(out, 3)
        with self.assertRaisesRegex(SystemExit, "expected 230"):
            verify.count_nupkgs(out, 230)

    def test_verify_refuses_a_github_token(self):
        for name in verify.TOKEN_NAMES:
            with self.subTest(name=name), unittest.mock.patch.dict(os.environ, {name: "x"}), self.assertRaises(SystemExit):
                verify.main(["verify", "--transport", self.temp.name])


class PinTests(unittest.TestCase):
    def test_pin_values(self):
        self.assertEqual((43236516383, 13955429317), (pin.RUN_ID, pin.ARTIFACT_ID))
        self.assertEqual("1a95b12fa70254831114ea517948604fb62d695b", pin.SOURCE_COMMIT)
        self.assertEqual((231, 230, "Elsa.Platform.Integration"), (pin.FEEDZ_PACKAGE_COUNT, pin.NUGET_PACKAGE_COUNT, pin.NUGET_EXCLUDE_IDS))
        self.assertEqual({40347931480}, pin.FORBIDDEN_RUNS)
        self.assertEqual({12664519771}, pin.FORBIDDEN_ARTIFACTS)


class WorkflowShapeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        try:
            import yaml
        except ImportError:  # the publish workflow's retrieve job runs without PyYAML and without this workflow file
            raise unittest.SkipTest("PyYAML not installed")
        cls.source = WORKFLOW.read_text(encoding="utf-8")
        cls.workflow = yaml.safe_load(cls.source)
        cls.jobs = cls.workflow["jobs"]

    def test_dispatch_only_with_no_inputs_and_no_default_permissions(self):
        self.assertEqual({"workflow_dispatch": None}, self.workflow[True])
        self.assertEqual({}, self.workflow["permissions"])

    def test_environments(self):
        self.assertEqual("feedz-publish", self.jobs["feedz"]["environment"])
        self.assertEqual("nuget-org", self.jobs["nuget"]["environment"])
        self.assertNotIn("environment", self.jobs["retrieve"])

    def test_only_nuget_job_requests_an_id_token(self):
        self.assertEqual("write", self.jobs["nuget"]["permissions"]["id-token"])
        for name in ("retrieve", "feedz"):
            self.assertNotIn("id-token", self.jobs[name]["permissions"])

    def test_verification_precedes_any_secret_or_push(self):
        for name, secret_marker in (("feedz", "secrets.ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY"), ("nuget", "NuGet/login")):
            steps = self.jobs[name]["steps"]
            text = [yaml_step_text(step) for step in steps]
            verify_at = next(i for i, t in enumerate(text) if "verify_consolidated_3101_candidate.py verify" in t)
            guard_at = next(i for i, t in enumerate(text) if "validate_packages_id_set.py" in t)
            secret_at = next(i for i, t in enumerate(text) if secret_marker in t)
            self.assertLess(verify_at, guard_at)
            self.assertLess(guard_at, secret_at)

    def test_only_the_nuget_job_excludes_ids_and_counts_match(self):
        feedz, nuget = (yaml_job_text(self.jobs[name]) for name in ("feedz", "nuget"))
        self.assertNotIn("EXCLUDE_IDS", feedz)
        self.assertIn("--expect 231", feedz)
        self.assertIn('"EXCLUDE_IDS": "Elsa.Platform.Integration"', nuget)
        self.assertIn("--expect 230", nuget)

    def test_runs_from_main_and_does_not_require_or_create_a_tag(self):
        for name in ("retrieve", "feedz", "nuget"):
            self.assertIn("refs/heads/main", yaml_job_text(self.jobs[name]))
        self.assertNotIn("git/ref/tags", self.source)
        self.assertNotIn("git tag", self.source)


def yaml_step_text(step) -> str:
    return json.dumps(step)


def yaml_job_text(job) -> str:
    return json.dumps(job)


if __name__ == "__main__":
    unittest.main()
