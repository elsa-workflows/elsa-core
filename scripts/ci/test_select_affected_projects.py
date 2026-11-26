from pathlib import Path
import re
import unittest

from select_affected_projects import AFFECTED_JOB, decide, last_verified_push, pick_base

PR_YML = Path(__file__).parents[2] / ".github/workflows/pr.yml"


def run(run_id, sha):
    return {"id": run_id, "head_sha": sha}


def jobs(conclusion):
    return {"jobs": [{"name": "Select CI suites", "conclusion": "success"},
                     {"name": AFFECTED_JOB, "conclusion": conclusion}]}


class PickBaseTests(unittest.TestCase):
    def test_each_event_uses_its_own_base(self):
        self.assertEqual("pr", pick_base("pull_request", {"pull_request": {"base": {"sha": "pr"}}}, None))
        self.assertEqual("mg", pick_base("merge_group", {"merge_group": {"base_sha": "mg"}}, None))
        self.assertEqual("push", pick_base("push", {}, lambda: "push"))
        self.assertIsNone(pick_base("workflow_dispatch", {}, lambda: "unused"))


class LastVerifiedPushTests(unittest.TestCase):
    def lookup(self, responses):
        return last_verified_push(lambda path: responses[path.split("?")[0]], "o/r", "main")

    def test_skips_green_runs_where_the_affected_job_did_not_run(self):
        self.assertEqual("b", self.lookup({
            "repos/o/r/actions/workflows/pr.yml/runs": {"workflow_runs": [run(2, "docs"), run(1, "b")]},
            "repos/o/r/actions/runs/2/jobs": jobs("skipped"),
            "repos/o/r/actions/runs/1/jobs": jobs("success"),
        }))

    def test_no_qualifying_run_or_a_failed_lookup_builds_everything(self):
        self.assertIsNone(self.lookup({
            "repos/o/r/actions/workflows/pr.yml/runs": {"workflow_runs": [run(1, "docs")]},
            "repos/o/r/actions/runs/1/jobs": jobs("skipped"),
        }))
        self.assertIsNone(self.lookup({}))


class DecideTests(unittest.TestCase):
    def decide(self, *changes, rc=0):
        return decide(list(changes), lambda: rc)[0]

    def test_always_build_files_build_everything(self):
        for path in (".github/workflows/packages.yml", "global.json", "NuGet.Config", "core/nuget.config",
                     ".editorconfig", "core/.editorconfig", "Directory.Build.props", "studio/src/Directory.Packages.props",
                     "build/ConsolidatedPackageProof.targets", "core/src/Shared.props", "Elsa.sln",
                     ".config/dotnet-tools.json", "build/Build.cs"):
            with self.subTest(path):
                self.assertEqual("all", self.decide(("M", path), rc=166))

    def test_affected_and_nothing_affected(self):
        self.assertEqual("affected", self.decide(("M", "core/src/modules/Elsa.Http/Foo.cs"), rc=0))
        self.assertEqual("none", self.decide(("M", "core/src/Foo.cs"), ("A", "docs/x.png"), rc=166))

    def test_a_deletion_builds_everything(self):
        shared = ("D", "core/test/unit/Shared/FastEndpoints/FastEndpointsCollection.cs")
        # Only the deletion: dotnet-affected would report nothing affected.
        self.assertEqual("all", self.decide(shared, rc=166))
        # Deletion plus an edit elsewhere: dotnet-affected would report only the edited project.
        self.assertEqual("all", self.decide(shared, ("M", "core/src/modules/Elsa.Http/Foo.cs"), rc=0))

    def test_a_deletion_is_decided_without_running_dotnet_affected(self):
        def run_affected():
            raise AssertionError("dotnet-affected should not run")
        self.assertEqual("all", decide([("D", "core/src/Foo.cs")], run_affected)[0])

    def test_any_other_exit_code_fails(self):
        with self.assertRaises(SystemExit):
            self.decide(("M", "core/src/Foo.cs"), rc=1)


class GateTests(unittest.TestCase):
    def test_pr_checks_result_needs_every_other_job(self):
        text = PR_YML.read_text()
        jobs = re.findall(r"^  ([A-Za-z0-9_-]+):$", text.split("\njobs:\n", 1)[1], re.MULTILINE)
        gate = text.split("\n  result:\n", 1)[1]
        needs = re.findall(r"^      - ([A-Za-z0-9_-]+)$", gate.split("\n    needs:\n", 1)[1].split("\n    runs-on:", 1)[0], re.MULTILINE)
        self.assertEqual(sorted(job for job in jobs if job != "result"), sorted(needs))
