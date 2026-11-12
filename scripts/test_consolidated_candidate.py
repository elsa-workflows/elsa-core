import dataclasses
import hashlib
import json
import os
import re
import sys
import tempfile
import unittest
import unittest.mock
import zipfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import verify_consolidated_candidate as verify

PIN = verify.load_pin("3.10.1")
WORKFLOW = Path(__file__).resolve().parents[1] / ".github/workflows/packages.yml"
OTHER_COMMIT = "5cff67228e550b353b3483eb3b37dfdb73a41c74"


def good_run():
    return {"id": PIN.run_id, "run_attempt": 1, "head_sha": PIN.source_sha, "status": "completed",
            "conclusion": "success", "event": "workflow_dispatch", "path": ".github/workflows/prepare-consolidated-3.10.1-release-candidate.yml"}


def good_artifact():
    return {"id": PIN.artifact_id, "name": PIN.artifact_name, "expired": False, "digest": f"sha256:{PIN.archive_sha256}",
            "workflow_run": {"id": PIN.run_id, "head_sha": PIN.source_sha}}


def good_compare(status="behind"):
    return {"status": status, "merge_base_commit": {"sha": PIN.source_sha}}


def build_archive(path: Path, packages: dict[str, bytes], *, version="3.10.1", source=PIN.source_sha, tamper=None) -> str:
    files = [{"path": f"artifacts/{name}", "size": len(data), "sha256": hashlib.sha256(data).hexdigest()}
             for name, data in packages.items()]
    manifest = json.dumps({"schema": 1, "published": False, "version": version, "run_id": PIN.run_id, "run_attempt": 1,
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
        verify.check_run(PIN, good_run())
        verify.check_artifact(PIN, good_artifact())

    def test_staged_3100_run_is_rejected_by_name(self):
        with self.assertRaisesRegex(SystemExit, "staged 3.10.0 candidate run"):
            verify.check_run_id(PIN, 40347931480)

    def test_any_other_run_is_rejected(self):
        for run_id in (1, 43236516382, 43236516384, 0):
            with self.subTest(run_id=run_id), self.assertRaisesRegex(SystemExit, "not the pinned 3.10.1"):
                verify.check_run_id(PIN, run_id)

    def test_staged_3100_artifact_is_rejected_by_name(self):
        with self.assertRaisesRegex(SystemExit, "staged 3.10.0 candidate artifact"):
            verify.check_artifact_id(PIN, 12664519771)

    def test_any_other_artifact_is_rejected(self):
        with self.assertRaisesRegex(SystemExit, "not the pinned 3.10.1"):
            verify.check_artifact_id(PIN, 13955429318)

    def test_run_must_have_the_pinned_source_and_succeed(self):
        for change in ({"head_sha": OTHER_COMMIT}, {"head_sha": "0" * 40}, {"conclusion": "failure"},
                       {"run_attempt": 2}, {"event": "push"}, {"path": ".github/workflows/other.yml"}):
            with self.subTest(change=change), self.assertRaises(SystemExit):
                verify.check_run(PIN, {**good_run(), **change})

    def test_both_recorded_producer_paths_are_accepted(self):
        for path in (".github/workflows/prepare-consolidated-release-candidate.yml",
                     ".github/workflows/prepare-consolidated-3.10.1-release-candidate.yml"):
            with self.subTest(path=path):
                verify.check_run(PIN, {**good_run(), "path": path})

    def test_a_run_from_main_may_pack_a_different_source_commit(self):
        main_head = "d" * 40
        verify.check_run(PIN, {**good_run(), "head_sha": main_head, "head_branch": "main",
                               "path": ".github/workflows/prepare-consolidated-release-candidate.yml"})
        verify.check_artifact(PIN, {**good_artifact(), "workflow_run": {"id": PIN.run_id, "head_sha": main_head, "head_branch": "main"}})
        for change in ({"head_branch": "feature/x"}, {"head_branch": None}, {"head_branch": "release/3.10.0"},
                       {"head_sha": "5cff67228e550b353b3483eb3b37dfdb73a41c74", "head_branch": "main"},
                       {"head_sha": "xyz", "head_branch": "main"}):
            with self.subTest(change=change), self.assertRaises(SystemExit):
                verify.check_run(PIN, {**good_run(), "head_sha": main_head, **change})
            with self.subTest(artifact=change), self.assertRaises(SystemExit):
                verify.check_artifact(PIN, {**good_artifact(), "workflow_run": {"id": PIN.run_id, "head_sha": main_head, **change}})

    def test_transport_run_and_artifact_must_come_from_the_same_commit(self):
        main_head, other_main_head = "d" * 40, "e" * 40
        run = {**good_run(), "head_sha": main_head, "head_branch": "main"}

        def attempt(artifact_head):
            with tempfile.TemporaryDirectory() as temporary:
                transport = Path(temporary)
                artifact = {**good_artifact(), "workflow_run": {"id": PIN.run_id, "head_sha": artifact_head, "head_branch": "main"}}
                for name, document in (("run.json", run), ("artifact.json", artifact), ("compare.json", good_compare("behind"))):
                    (transport / name).write_text(json.dumps(document))
                (transport / "candidate.zip").write_bytes(b"zip")
                with unittest.mock.patch.object(verify, "check_archive"), \
                        unittest.mock.patch.object(verify, "read_manifest"), \
                        unittest.mock.patch.object(verify, "sealed_packages", return_value=[]):
                    return verify.verify_transport(PIN, transport)

        self.assertEqual([], attempt(main_head))
        # Each head is acceptable alone (a commit run from main); only the mismatch between them is rejected.
        verify.check_run(PIN, run)
        verify.check_artifact(PIN, {**good_artifact(), "workflow_run": {"id": PIN.run_id, "head_sha": other_main_head, "head_branch": "main"}})
        with self.assertRaisesRegex(SystemExit, "not produced from the same commit"):
            attempt(other_main_head)

    def test_artifact_must_match_digest_name_expiry_and_run(self):
        for change in ({"digest": "sha256:" + "0" * 64}, {"name": "other"}, {"expired": True},
                       {"workflow_run": {"id": 40347931480, "head_sha": PIN.source_sha}},
                       {"workflow_run": {"id": PIN.run_id, "head_sha": OTHER_COMMIT}}):
            with self.subTest(change=change), self.assertRaises(SystemExit):
                verify.check_artifact(PIN, {**good_artifact(), **change})


class AncestryTests(unittest.TestCase):
    def test_behind_and_identical_are_on_the_release_line(self):
        verify.check_ancestry(PIN, good_compare("behind"))
        verify.check_ancestry(PIN, good_compare("identical"))

    def test_ahead_and_diverged_are_rejected(self):
        for status in ("ahead", "diverged", None):
            with self.subTest(status=status), self.assertRaisesRegex(SystemExit, "not an ancestor"):
                verify.check_ancestry(PIN, {**good_compare(), "status": status})

    def test_merge_base_must_be_the_source(self):
        with self.assertRaisesRegex(SystemExit, "merge base"):
            verify.check_ancestry(PIN, {"status": "behind", "merge_base_commit": {"sha": OTHER_COMMIT}})


class SourceTests(unittest.TestCase):
    def test_other_and_3100_sources_are_rejected(self):
        for commit in (OTHER_COMMIT, "fdf64a76bc967fe07f01fdb7fa9feb93d3086a82", "", None):
            with self.subTest(commit=commit), self.assertRaises(SystemExit):
                verify.check_source(PIN, commit, "x")


class ArchiveTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.dir = Path(self.temp.name)

    def pin_for(self, archive: Path, manifest_sha: str, count: int):
        archive_sha = hashlib.sha256(archive.read_bytes()).hexdigest()
        return dataclasses.replace(PIN, archive_sha256=archive_sha, manifest_sha256=manifest_sha, feedz_count=count, nuget_count=count - 1)

    def test_archive_hash_must_match_the_pin(self):
        archive = self.dir / "a.zip"
        build_archive(archive, packages())
        with self.assertRaisesRegex(SystemExit, "archive sha256"):
            verify.check_archive(PIN, archive)

    def test_valid_archive_is_staged_with_all_packages(self):
        archive = self.dir / "a.zip"
        manifest_sha = build_archive(archive, packages(3))
        pin = self.pin_for(archive, manifest_sha, 3)
        verify.check_archive(pin, archive)
        staged = verify.stage_packages(archive, verify.sealed_packages(pin, verify.read_manifest(pin, archive)), self.dir / "out")
        self.assertEqual(6, len(staged))
        self.assertEqual(3, len(list((self.dir / "out").glob("*.nupkg"))))

    def test_manifest_hash_must_match_the_pin(self):
        archive = self.dir / "a.zip"
        build_archive(archive, packages(3))
        with self.assertRaisesRegex(SystemExit, "manifest sha256"):
            verify.read_manifest(dataclasses.replace(PIN, manifest_sha256="0" * 64), archive)

    def test_manifest_identity_and_source_are_checked(self):
        for kwargs in ({"version": "3.10.0"}, {"source": OTHER_COMMIT}):
            archive = self.dir / f"{len(kwargs)}{next(iter(kwargs))}.zip"
            manifest_sha = build_archive(archive, packages(3), **kwargs)
            with self.subTest(kwargs=kwargs), self.assertRaises(SystemExit):
                verify.read_manifest(dataclasses.replace(PIN, manifest_sha256=manifest_sha), archive)

    def test_wrong_package_count_is_rejected(self):
        archive = self.dir / "a.zip"
        manifest_sha = build_archive(archive, packages(3))
        pin = self.pin_for(archive, manifest_sha, 231)
        with self.assertRaisesRegex(SystemExit, "expected 231"):
            verify.sealed_packages(pin, verify.read_manifest(pin, archive))

    def test_package_with_another_version_is_rejected(self):
        archive = self.dir / "a.zip"
        content = packages(2)
        content.update(packages(1, "3.10.0"))
        manifest_sha = build_archive(archive, content)
        pin = self.pin_for(archive, manifest_sha, 3)
        with self.assertRaisesRegex(SystemExit, "not version 3.10.1"):
            verify.sealed_packages(pin, verify.read_manifest(pin, archive))

    def test_tampered_package_bytes_are_rejected(self):
        archive = self.dir / "a.zip"
        content = packages(3)
        manifest_sha = build_archive(archive, content, tamper={"Pkg1.3.10.1.nupkg": b"evil"})
        pin = self.pin_for(archive, manifest_sha, 3)
        with self.assertRaisesRegex(SystemExit, "hash mismatch"):
            verify.stage_packages(archive, verify.sealed_packages(pin, verify.read_manifest(pin, archive)), self.dir / "out")

    def test_nupkg_count_check(self):
        out = self.dir / "out"
        out.mkdir()
        for index in range(3):
            (out / f"Pkg{index}.3.10.1.nupkg").write_bytes(b"x")
        verify.count_nupkgs(out, 3)
        with self.assertRaisesRegex(SystemExit, "expected 230"):
            verify.count_nupkgs(out, 230)
        # The count is exact in both directions: one extra package is as wrong as a missing one.
        (out / "Extra.3.10.1.nupkg").write_bytes(b"x")
        with self.assertRaisesRegex(SystemExit, "Found 4 .nupkg files .* expected 3"):
            verify.count_nupkgs(out, 3)
        for expected in (2, 5):
            with self.subTest(expected=expected), self.assertRaises(SystemExit):
                verify.count_nupkgs(out, expected)
        verify.count_nupkgs(out, 4)

    def test_verify_refuses_a_github_token(self):
        for name in verify.TOKEN_NAMES:
            with self.subTest(name=name), unittest.mock.patch.dict(os.environ, {name: "x"}), self.assertRaises(SystemExit):
                verify.main(["verify", "--version", "3.10.1", "--transport", self.temp.name])


    def test_verify_allows_blank_token_variables(self):
        blank = {name: "" for name in verify.TOKEN_NAMES}
        with unittest.mock.patch.dict(os.environ, blank), self.assertRaisesRegex(SystemExit, "Transport directory missing"):
            verify.main(["verify", "--version", "3.10.1", "--transport", str(self.dir / "missing")])


class PinTests(unittest.TestCase):
    def test_pin_values(self):
        self.assertEqual((43236516383, 13955429317), (PIN.run_id, PIN.artifact_id))
        self.assertEqual("1a95b12fa70254831114ea517948604fb62d695b", PIN.source_sha)
        self.assertEqual((231, 230, ("Elsa.Platform.Integration",)), (PIN.feedz_count, PIN.nuget_count, PIN.exclude_ids))
        self.assertEqual("release/3.10.0", PIN.ancestry_branch)
        self.assertEqual("9c975053b3113b62eec664fcb498a776e7dedcfbbc578a1df391650541955c19", PIN.archive_sha256)
        self.assertEqual("c20fc5eee0d579055c1fd045f93cf7ef3b24468ed066c3dc124c9d1192e77d6e", PIN.manifest_sha256)
        self.assertEqual({40347931480}, verify.FORBIDDEN_RUNS)
        self.assertEqual({12664519771}, verify.FORBIDDEN_ARTIFACTS)




GOOD_ENTRY = {
    "run_id": 43236516383, "artifact_id": 13955429317, "source_sha": "1a95b12fa70254831114ea517948604fb62d695b",
    "archive_sha256": "9c975053b3113b62eec664fcb498a776e7dedcfbbc578a1df391650541955c19",
    "manifest_sha256": "c20fc5eee0d579055c1fd045f93cf7ef3b24468ed066c3dc124c9d1192e77d6e",
    "feedz_count": 231, "nuget_count": 230, "exclude_ids": ["Elsa.Platform.Integration"], "ancestry_branch": "release/3.10.0",
}


class PinEntryTests(unittest.TestCase):
    def test_the_committed_pin_file_is_data_only_and_valid(self):
        document = json.loads(verify.PIN_FILE.read_text(encoding="utf-8"))
        self.assertEqual({"3.10.1"}, set(document))
        self.assertEqual(GOOD_ENTRY, document["3.10.1"])
        self.assertEqual(set(document["3.10.1"]), set(verify.PIN_FIELDS))
        text = verify.PIN_FILE.read_text(encoding="utf-8")
        for rejected in ("40347931480", "12664519771", "5cff67228"):
            self.assertNotIn(rejected, text)

    def test_a_good_entry_is_accepted(self):
        pin = verify.validate_pin_entry("3.10.1", dict(GOOD_ENTRY))
        self.assertEqual(("3.10.1", 230), (pin.version, pin.nuget_count))

    def test_every_missing_field_is_rejected(self):
        for field in verify.PIN_FIELDS:
            entry = {key: value for key, value in GOOD_ENTRY.items() if key != field}
            with self.subTest(field=field), self.assertRaisesRegex(SystemExit, f"missing: {field}"):
                verify.validate_pin_entry("3.10.1", entry)

    def test_malformed_fields_are_rejected(self):
        bad = {
            "run_id": (0, -1, "1", True, 1.5, None), "artifact_id": (0, "x", False),
            "source_sha": ("1a95b12", "G" * 40, "1A95B12FA70254831114EA517948604FB62D695B", 7, ""),
            "archive_sha256": ("abc", "z" * 64, "A" * 64, None), "manifest_sha256": ("0" * 63, "0" * 65, 5),
            "feedz_count": (0, "231", -3, None), "nuget_count": (0, "230", None),
            "exclude_ids": ("Elsa.Platform.Integration", [1], ["a b"], ["Elsa", "Elsa"], [""], None),
            "ancestry_branch": ("", "a..b", "-x", 5, None),
        }
        for field, values in bad.items():
            for value in values:
                with self.subTest(field=field, value=value), self.assertRaises(SystemExit):
                    verify.validate_pin_entry("3.10.1", {**GOOD_ENTRY, field: value})

    def test_inconsistent_counts_unknown_fields_and_non_objects_are_rejected(self):
        for entry in ({**GOOD_ENTRY, "nuget_count": 231}, {**GOOD_ENTRY, "extra": 1}, [], "x", None):
            with self.subTest(entry=entry), self.assertRaises(SystemExit):
                verify.validate_pin_entry("3.10.1", entry)

    def test_an_entry_naming_the_staged_3100_candidate_is_rejected(self):
        for change in ({"run_id": 40347931480}, {"artifact_id": 12664519771}, {"source_sha": "5cff67228e550b353b3483eb3b37dfdb73a41c74"}):
            with self.subTest(change=change), self.assertRaisesRegex(SystemExit, "never be promoted"):
                verify.validate_pin_entry("3.10.1", {**GOOD_ENTRY, **change})

    def test_unknown_and_malformed_versions_are_rejected(self):
        for version in ("3.10.2", "9.9.9", "", "3.10", "latest", "3.10.1; rm -rf /", "../3.10.1"):
            with self.subTest(version=version), self.assertRaises(SystemExit):
                verify.load_pin(version)

    def test_pin_file_problems_are_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            pin_file = Path(temp) / "pins.json"
            for content in ("not json", "[]", json.dumps({"latest": GOOD_ENTRY}), json.dumps({"3.10.1": {}})):
                pin_file.write_text(content)
                with self.subTest(content=content), self.assertRaises(SystemExit):
                    verify.load_pin("3.10.1", pin_file)
            pin_file.write_text(json.dumps({"3.10.1": GOOD_ENTRY}))
            self.assertEqual(43236516383, verify.load_pin("3.10.1", pin_file).run_id)
            with self.assertRaises(SystemExit):
                verify.load_pin("3.10.1", Path(temp) / "missing.json")

    def test_pin_output_and_counts_come_from_the_entry(self):
        self.assertEqual("exclude_ids=Elsa.Platform.Integration\nexpected_count=230\n", verify.pin_output(PIN, "nuget"))
        self.assertEqual("exclude_ids=\nexpected_count=231\n", verify.pin_output(PIN, "feedz"))
        with self.assertRaises(SystemExit):
            verify.expected_count(PIN, "other")

    def test_validate_pin_command_needs_no_network_and_rejects_unknown_versions(self):
        self.assertEqual(0, verify.main(["validate-pin", "--version", "3.10.1"]))
        with self.assertRaises(SystemExit):
            verify.main(["validate-pin", "--version", "3.10.2"])


PACKAGES = Path(__file__).resolve().parents[1] / ".github/workflows/packages.yml"
WORKFLOWS = PACKAGES.parent
DELETED_WORKFLOWS = ("publish-consolidated-3.10.1-candidate.yml", "prepare-consolidated-3.10.1-release-candidate.yml",
                     "publish-consolidated-original-candidate.yml")
CANDIDATE_JOBS = ("validate_candidate_pin", "retrieve_candidate", "publish_candidate_feedz", "publish_candidate_nuget")


def job_text(job) -> str:
    return json.dumps(job)


def run_text(job) -> str:
    return "\n".join(step.get("run", "") for step in job["steps"])


SOURCE_CHECKOUT_INDEX = 1


class WorkflowShapeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        try:
            import yaml
        except ImportError:  # the retrieve job runs this file without PyYAML
            raise unittest.SkipTest("PyYAML not installed")
        if not PACKAGES.is_file():  # the retrieve job checks out only scripts/ and .github/release-pins/
            raise unittest.SkipTest("workflow files not checked out")
        cls.source = PACKAGES.read_text(encoding="utf-8")
        cls.workflow = yaml.safe_load(cls.source)
        cls.jobs = cls.workflow["jobs"]

    def verify_steps(self, name):
        return [step for step in self.jobs[name]["steps"] if "verify_consolidated_candidate.py verify" in step.get("run", "")]

    def test_dispatch_input_is_a_string_and_empty_by_default(self):
        spec = self.workflow[True]["workflow_dispatch"]["inputs"]["promote_candidate_version"]
        self.assertEqual(("string", ""), (spec["type"], spec["default"]))

    def test_environments(self):
        self.assertEqual("feedz-publish", self.jobs["publish_candidate_feedz"]["environment"])
        self.assertEqual("nuget-org", self.jobs["publish_candidate_nuget"]["environment"])
        for name in ("validate_candidate_pin", "retrieve_candidate"):
            self.assertNotIn("environment", self.jobs[name])

    def test_nonpublishing_jobs_cannot_receive_publisher_credentials(self):
        # Ported from test_consolidated_executor_workflow.py (3.10.0 executor), for the packages.yml candidate jobs.
        self.assertEqual("feedz-publish", self.jobs["publish_candidate_feedz"]["environment"])
        for name in ("validate_candidate_pin", "retrieve_candidate"):
            with self.subTest(job=name):
                self.assertNotIn("environment", self.jobs[name])
                self.assertNotIn("secrets.", job_text(self.jobs[name]))
        self.assertNotIn("secrets.", job_text(self.jobs["publish_candidate_feedz"]).replace("secrets.ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY", ""))
        self.assertNotIn("GH_TOKEN", job_text(self.jobs["publish_candidate_feedz"]).replace('"GH_TOKEN": ""', ""))

    def test_retrieve_has_no_environment_secret_or_id_token(self):
        retrieve = self.jobs["retrieve_candidate"]
        self.assertNotIn("environment", retrieve)
        self.assertNotIn("secrets.", job_text(retrieve))
        self.assertNotIn("id-token", retrieve["permissions"])
        self.assertEqual({"actions": "read", "contents": "read"}, retrieve["permissions"])

    def test_only_nuget_job_requests_an_id_token(self):
        self.assertEqual("write", self.jobs["publish_candidate_nuget"]["permissions"]["id-token"])
        for name in ("validate_candidate_pin", "retrieve_candidate", "publish_candidate_feedz"):
            self.assertNotIn("id-token", self.jobs[name]["permissions"])

    def test_validate_is_first_and_runs_before_any_download(self):
        validate = self.jobs["validate_candidate_pin"]
        self.assertEqual("validate_publication_selection", validate["needs"])
        first_run = next(step for step in validate["steps"] if "run" in step)
        self.assertIn("verify_consolidated_candidate.py validate-pin", first_run["run"])
        self.assertEqual("validate_candidate_pin", self.jobs["retrieve_candidate"]["needs"])
        self.assertEqual("retrieve_candidate", self.jobs["publish_candidate_feedz"]["needs"])
        self.assertIn("retrieve_candidate", self.jobs["publish_candidate_nuget"]["needs"])

    def test_verification_precedes_any_secret_or_push(self):
        for name, secret_marker in (("publish_candidate_feedz", "secrets.ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY"),
                                    ("publish_candidate_nuget", "NuGet/login")):
            text = [job_text(step) for step in self.jobs[name]["steps"]]
            verify_at = next(i for i, t in enumerate(text) if "verify_consolidated_candidate.py verify" in t)
            guard_at = next(i for i, t in enumerate(text) if "validate_packages_id_set.py" in t)
            secret_at = next(i for i, t in enumerate(text) if secret_marker in t)
            self.assertLess(verify_at, guard_at)
            self.assertLess(guard_at, secret_at)

    def test_only_the_nuget_job_excludes_ids_and_counts_come_from_the_pin(self):
        feedz, nuget = (self.jobs[name] for name in ("publish_candidate_feedz", "publish_candidate_nuget"))
        self.assertNotIn("EXCLUDE_IDS", job_text(feedz))
        self.assertIn('count --version "$VERSION" --feed feedz', run_text(feedz))
        self.assertIn('"EXCLUDE_IDS": "${{ steps.pin.outputs.exclude_ids }}"', job_text(nuget))
        self.assertIn('count --version "$VERSION" --feed nuget', run_text(nuget))
        self.assertIn("--feed nuget >> ", run_text(nuget))
        for text in (job_text(feedz), job_text(nuget)):
            self.assertNotIn("--expect", text)
            self.assertNotIn("Elsa.Platform.Integration", text)

    def test_both_verify_steps_blank_every_token_name(self):
        for name in ("publish_candidate_feedz", "publish_candidate_nuget"):
            steps = self.verify_steps(name)
            self.assertEqual(1, len(steps), name)
            env = steps[0].get("env")
            self.assertEqual({"GITHUB_TOKEN": "", "GH_TOKEN": "", "ACTIONS_READ_TOKEN": ""}, {k: v for k, v in env.items() if k != "VERSION"}, name)

    def test_dispatch_is_main_only(self):
        for name in CANDIDATE_JOBS[1:]:
            text = job_text(self.jobs[name])
            self.assertIn("refs/heads/main", text)
            self.assertIn("GITHUB_RUN_ATTEMPT", text)
        self.assertEqual("validate_publication_selection", self.jobs["validate_candidate_pin"]["needs"])
        self.assertIn("PROMOTE_CANDIDATE_VERSION", job_text(self.jobs["validate_publication_selection"]))

    def test_there_are_no_per_version_branches_and_versions_live_only_in_the_pin_file(self):
        for name in CANDIDATE_JOBS:
            job = self.jobs[name]
            for step in job["steps"]:
                self.assertNotIn("if", step, f"{name}: {step.get('name')}")
            self.assertNotRegex(job_text(job), r"[0-9]+\.[0-9]+\.[0-9]+")
        self.assertNotIn("if", self.jobs["retrieve_candidate"])
        self.assertNotIn("3.10.1", self.source.split("  # --- Candidate promotion")[1])

    PUBLISH_TARGETS = {"publish_candidate_feedz": "feedz", "publish_candidate_nuget": "nuget"}

    def publisher_runs(self, job, target, results=None, cancelled=False):
        """Evaluate a publisher's job condition the way GitHub does, for a promote_target and the results of its needs.

        Without a status function GitHub adds an implicit success(): every need must have succeeded. With
        !cancelled() the skip of a need no longer skips the job, so the expression alone decides.
        """
        results = results or {}
        condition = self.jobs[job]["if"].strip()
        expression = condition[3:-2].strip() if condition.startswith("${{") else condition
        # Only these tokens may appear, so the translation below is the whole meaning of the expression.
        self.assertRegex(expression, r"^(\s|!cancelled\(\)|&&|\|\||\(|\)|==|'[a-z]+'|inputs\.promote_target|needs\.[a-z_]+\.result)+$", job)
        needs = self.jobs[job]["needs"]
        needs = [needs] if isinstance(needs, str) else needs
        if "cancelled()" not in expression and any(results.get(n, "success") != "success" for n in needs):
            return False
        python = re.sub(r"needs\.([a-z_]+)\.result", r"results.get('\1', 'success')", expression)
        python = python.replace("!cancelled()", "(not cancelled)").replace("&&", " and ").replace("||", " or ")
        python = python.replace("inputs.promote_target", "target")
        return eval(python, {"__builtins__": {}}, {"target": target, "results": results, "cancelled": cancelled})

    def promote(self, target, pin="success", retrieve="success", feedz_outcome="success", cancelled=False):
        """Which publishers run for one dispatch, following the needs chain pin -> retrieve -> feedz -> nuget."""
        results = {"validate_candidate_pin": pin, "retrieve_candidate": retrieve if pin == "success" else "skipped"}
        feedz_runs = self.publisher_runs("publish_candidate_feedz", target, results, cancelled)
        results["publish_candidate_feedz"] = feedz_outcome if feedz_runs else "skipped"
        nuget_runs = self.publisher_runs("publish_candidate_nuget", target, results, cancelled)
        return {feed for feed, ran in (("feedz", feedz_runs), ("nuget", nuget_runs)) if ran}

    def test_each_publisher_condition_names_no_version_and_no_other_input(self):
        for job in self.PUBLISH_TARGETS:
            condition = self.jobs[job]["if"]
            self.assertEqual({"inputs.promote_target"}, set(re.findall(r"inputs\.[a-z_]+", condition)), job)
            self.assertNotRegex(condition, r"[0-9]+\.[0-9]+|version|github\.|always|failure", job)
        self.assertNotIn("cancelled", self.jobs["publish_candidate_feedz"]["if"])
        self.assertEqual("retrieve_candidate", self.jobs["publish_candidate_feedz"]["needs"])
        self.assertEqual(["validate_candidate_pin", "retrieve_candidate", "publish_candidate_feedz"],
                         self.jobs["publish_candidate_nuget"]["needs"])

    def test_promote_target_all_runs_both_publishers_and_one_target_runs_only_its_own(self):
        for target, expected in (("all", {"feedz", "nuget"}), ("feedz", {"feedz"}), ("nuget", {"nuget"})):
            self.assertEqual(expected, self.promote(target), target)
        for bogus in ("", "both", "NUGET", "nuget,feedz"):
            self.assertEqual(set(), self.promote(bogus), bogus)

    def test_nuget_only_run_still_runs_nuget_when_feedz_is_skipped(self):
        # The 3.10.1 recovery: Feedz is skipped by its own if:, and that skip must not skip nuget.org.
        self.assertEqual({"nuget"}, self.promote("nuget"))

    def test_feedz_failure_on_all_blocks_nuget(self):
        for outcome in ("failure", "cancelled"):
            with self.subTest(feedz=outcome):
                self.assertEqual({"feedz"}, self.promote("all", feedz_outcome=outcome))

    def test_nuget_never_runs_without_a_validated_pin_and_retrieval(self):
        for target in ("all", "nuget"):
            for pin, retrieve in (("skipped", "skipped"), ("failure", "skipped"), ("success", "failure"), ("success", "skipped")):
                with self.subTest(target=target, pin=pin, retrieve=retrieve):
                    self.assertNotIn("nuget", self.promote(target, pin=pin, retrieve=retrieve))
            with self.subTest(target=target, cancelled=True):
                self.assertNotIn("nuget", self.promote(target, cancelled=True))

    def test_promote_target_is_a_choice_defaulting_to_all_and_validation_cannot_be_skipped(self):
        import yaml
        spec = yaml.safe_load(self.source)[True]["workflow_dispatch"]["inputs"]["promote_target"]
        self.assertEqual("choice", spec["type"])
        self.assertEqual(["all", "feedz", "nuget"], spec["options"])
        self.assertEqual("all", spec["default"])
        selection = self.jobs["validate_publication_selection"]
        self.assertIn("${{ inputs.promote_target }}", job_text(selection))
        self.assertNotIn("if", selection["steps"][-1])
        # Retrieval and every verification step run whatever the target is.
        for name in ("validate_candidate_pin", "retrieve_candidate"):
            self.assertNotIn("promote_target", job_text(self.jobs[name]), name)
        for job in self.PUBLISH_TARGETS:
            for step in self.jobs[job]["steps"]:
                self.assertNotIn("if", step)

    def test_no_tag_is_created_or_required_and_docker_is_untouched(self):
        promotion = self.source.split("  # --- Candidate promotion")[1]
        for text in ("git/ref/tags", "git tag", "refs/tags", "git push", "docker"):
            self.assertNotIn(text, promotion.lower() if text == "docker" else promotion)

    def test_normal_push_and_tag_behaviour_is_unchanged(self):
        # The only edits to the build path are the two test jobs skipping while a candidate is promoted.
        for name in ("test_unit_integration", "test_component"):
            self.assertEqual("${{ inputs.promote_candidate_version == '' }}", self.jobs[name]["if"])

    def test_the_per_version_workflows_are_gone(self):
        for name in DELETED_WORKFLOWS:
            self.assertFalse((WORKFLOWS / name).exists(), name)
        for name in ("scripts/consolidated_3101_pin.py", "scripts/verify_consolidated_3101_candidate.py",
                     "scripts/test_consolidated_3101_candidate.py"):
            self.assertFalse((WORKFLOWS.parents[1] / name).exists(), name)

    def test_the_folded_prepare_workflow_takes_a_version_input_and_has_no_version_literals(self):
        import yaml
        source = (WORKFLOWS / "prepare-consolidated-release-candidate.yml").read_text(encoding="utf-8")
        inputs = yaml.safe_load(source)[True]["workflow_dispatch"]["inputs"]
        self.assertEqual({"version", "source_commit"}, set(inputs))
        self.assertEqual("string", inputs["version"]["type"])
        code = "\n".join(line for line in source.splitlines() if not line.lstrip().startswith("#") and "description:" not in line)
        self.assertNotRegex(code, r"--version [0-9]|version:\"[0-9]")
        self.assertNotIn("prepare-consolidated-3.10.1", source)
        self.assertIn(".path == \".github/workflows/prepare-consolidated-release-candidate.yml\"", source)

    def test_the_folded_prepare_workflow_runs_scripts_from_its_own_ref_and_packs_source_commit(self):
        import yaml
        source = (WORKFLOWS / "prepare-consolidated-release-candidate.yml").read_text(encoding="utf-8")
        steps = yaml.safe_load(source)["jobs"]["prepare"]["steps"]
        checkouts = [step for step in steps if str(step.get("uses", "")).startswith("actions/checkout@")]
        self.assertEqual([("tooling", "${{ github.sha }}"), ("source", "${{ needs.verify_source.outputs.verified-revision }}")],
                         [(step["with"]["path"], step["with"]["ref"]) for step in checkouts])
        self.assertEqual(0, checkouts[SOURCE_CHECKOUT_INDEX]["with"].get("fetch-depth"))
        guard = steps[0]["run"]
        self.assertIn("refs/heads/main", guard)
        for step in steps:
            run = step.get("run", "")
            self.assertNotIn("python3 scripts/", run, step.get("name"))
            if "--source-root" in run:
                self.assertIn("--source-root source", run)
        self.assertNotIn("release/3.10.0", source)
        self.assertNotIn("git checkout", source)
        jobs = yaml.safe_load(source)["jobs"]
        prepare_run = "\n".join(step.get("run", "") for step in steps)
        self.assertEqual(2, prepare_run.count('test "$(git -C source rev-parse HEAD)" = "$CANDIDATE_SOURCE"'))
        retrieve = jobs["retrieve"]
        self.assertFalse([step for step in retrieve["steps"] if "uses" in step and "checkout" in step["uses"]])
        self.assertIn('.head_branch == "main"', source)

    def test_the_release_line_is_proven_by_a_checkout_free_job_that_runs_before_prepare(self):
        import yaml
        source = (WORKFLOWS / "prepare-consolidated-release-candidate.yml").read_text(encoding="utf-8")
        jobs = yaml.safe_load(source)["jobs"]
        self.assertEqual(["verify_source", "prepare", "retrieve", "consumers"], list(jobs))
        verify_job = jobs["verify_source"]
        self.assertEqual("verify_source", jobs["prepare"]["needs"])
        self.assertEqual({"contents": "read", "actions": "none"}, verify_job["permissions"])
        self.assertFalse([step for step in verify_job["steps"] if "uses" in step], "the proof job must not run any action")
        script = "\n".join(step.get("run", "") for step in verify_job["steps"])
        self.assertNotRegex(script, r"git (checkout|fetch|clone)|python3|dotnet|npm")
        self.assertIn("refs/heads/main", script)
        self.assertIn('branch="release/$major.$minor.0"', script)
        self.assertIn('compare/$branch...$CANDIDATE_SOURCE', script)
        self.assertIn('(.status == "behind" or .status == "identical")', script)
        self.assertIn(".merge_base_commit.sha == $source", script)
        self.assertIn('test "$proven" = "$CANDIDATE_SOURCE"', script)
        env = next(step for step in verify_job["steps"] if step.get("id") == "line")["env"]
        self.assertEqual("${{ github.token }}", env["GH_TOKEN"])
        # The source is checked out at the value the proof job produced, never at the raw input.
        checkout = next(step for step in jobs["prepare"]["steps"] if step.get("with", {}).get("path") == "source")
        self.assertEqual("${{ needs.verify_source.outputs.verified-revision }}", checkout["with"]["ref"])
        self.assertNotIn("inputs.source_commit", source.split("  prepare:")[1].split("  retrieve:")[0].split("path: source")[0].split("path: tooling")[1])
        # The in-job ancestry check stays as defence in depth.
        self.assertIn('git -C source merge-base --is-ancestor "$CANDIDATE_SOURCE" "origin/$branch"', source)

    def test_no_cache_capable_setup_step_follows_the_source_checkout(self):
        import yaml
        source = (WORKFLOWS / "prepare-consolidated-release-candidate.yml").read_text(encoding="utf-8")
        for name, job in yaml.safe_load(source)["jobs"].items():
            steps = job["steps"]
            positions = [index for index, step in enumerate(steps) if step.get("with", {}).get("path") == "source"]
            for step in steps:
                action = str(step.get("uses", ""))
                if action.startswith(("actions/setup-", "actions/cache")):
                    arguments = step.get("with", {})
                    self.assertFalse(any(key.startswith("cache") for key in arguments), f"{name}: caching enabled on {action}")
                    self.assertFalse(action.startswith("actions/cache"), name)
            for position in positions:
                after = steps[position + 1:]
                self.assertFalse([step for step in after if str(step.get("uses", "")).startswith(("actions/setup-", "actions/cache"))],
                                 f"{name}: a setup or cache action follows the source checkout")
                # After the source checkout only the build and its packaging guards run: no second checkout.
                self.assertFalse([step for step in after if str(step.get("uses", "")).startswith("actions/checkout@")])


if __name__ == "__main__":
    unittest.main()
