#!/usr/bin/env python3
"""Prove the imported Mongo atomic-update regression and correction without publishing diagnostics."""

from __future__ import annotations

import argparse
from collections import Counter
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

from run_execution_cycle_proof import digest, execute, test_summary, tracked_input
from run_current_import_affected_tests import assert_clean_source


ROOT = Path(__file__).resolve().parents[2]
BASELINE = "23b852ad6262c4bf07b9f311090c892dbecd740b"
MONGO = "src/extensions/persistence/Elsa.Persistence.MongoDb/Elsa.Persistence.MongoDb.csproj"
MONGO_TEST = "test/extensions/modules/persistence/Elsa.MongoDb.UnitTests/Elsa.MongoDb.UnitTests.csproj"
BPMN_TEST = "test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Elsa.Bpmn.Interchange.IntegrationTests.csproj"
FIXTURE = "test/extensions/modules/persistence/Elsa.MongoDb.UnitTests/MongoWorkflowDefinitionStoreCompareAndSwapTests.cs"
PREFIX = "Elsa.MongoDb.UnitTests.MongoWorkflowDefinitionStoreCompareAndSwapTests."
REGRESSIONS = {
    PREFIX + "TryUpdateLatestAsync_WhenUnlistedMetadataChangesAfterRead_ReturnsConflictAndKeepsConcurrentMetadata":
        "Atomic metadata guard must reject a stale full-document snapshot.",
    PREFIX + "TryUpdateLatestAsync_WhenDraftInsertFails_RollsBackLatestUnmark":
        "Failed draft insertion must leave prior latest unchanged.",
}
MONGO_CASES = {
    "TryUpdateLatestAsync_WhenNothingMatchesTheFilter_ReturnsNotFound": 1,
    "TryUpdateLatestAsync_WhenTheRowDoesNotMatch_ReturnsConflictAndWritesNothing": 1,
    "TryUpdateLatestAsync_WhenTheLoadedRowIsNoLongerLatest_ReturnsConflictAndWritesNothing": 1,
    "TryUpdateLatestAsync_WhenSnapshotMatches_UpdatesOnceUsingPrimaryTransaction": 1,
    "TryUpdateLatestAsync_WhenTwoWritersReadTheSameSnapshot_OnlyOneWins": 1,
    "TryUpdateLatestAsync_WhenUnlistedMetadataChangesAfterRead_ReturnsConflictAndKeepsConcurrentMetadata": 1,
    "TryUpdateLatestAsync_WhenGraphOrNameChangesAfterRead_ReturnsConflictAndKeepsConcurrentWrite": 2,
    "TryUpdateLatestAsync_ClassifiesOnlyWriteConflictCodeAsAbortedConflict": 2,
    "TryUpdateLatestAsync_WhenCallbackThrowsTransientMongoError_PropagatesAndLeavesDocumentUnchanged": 2,
    "TryUpdateLatestAsync_WhenTenantIsNotVisible_ReturnsNotFound_AndTenantAgnosticMutationPreservesOwner": 2,
    "TryUpdateLatestAsync_WhenSharedRowIsVisible_PreservesSharedOwner": 1,
    "TryUpdateLatestAsync_WhenPublishedVersionCreatesDraft_UpdatesBothRowsAndPreservesTenant": 1,
    "TryUpdateLatestAsync_WhenPublishedVersionHasUnknownBsonField_RejectsWithoutMutation": 1,
    "TryUpdateLatestAsync_WhenDraftInsertFails_RollsBackLatestUnmark": 1,
    "TryUpdateLatestAsync_WhenServerIsStandalone_FailsWithoutMutationOrCallbacks": 1,
    "TryUpdateLatestAsync_CannotChangeLogicalDefinitionId": 1,
}
BPMN_PREFIX = "Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Interchange.BpmnDocumentPutCompareAndSwapTests."
BPMN_CASES = {
    "ImportDocumentAsync_WhenASecondWriterSavesAfterTheFirstMatched_RefusesTheFirstAndKeepsTheSecond": 1,
    "ImportDocumentAsync_WhenMetadataIsSavedAfterTheFirstMatched_RefusesRatherThanRevertingTheRename": 1,
    "ImportDocumentAsync_WhenMetadataIsSavedDuringCompareAndSwap_RejectsTheStaleDocumentEdit": 1,
    "ImportDocumentAsync_WhenDraftSavingHandlerEditsDraft_PersistsAllHandlerEdits": 1,
    "ImportDocumentAsync_WhenDraftSavingHandlerRejects_DoesNotPersist": 1,
    "ImportDocumentAsync_WhenLatestIsPublished_ReusesTheAnnouncedDraftIdentity": 1,
}
PROPERTIES = ["-m:1", "-p:UseProjectReferences=true", "-p:IsPackable=false",
              "-p:GeneratePackageOnBuild=false", "-p:CollectCoverage=false"]
TESTS = ((MONGO_TEST, None), (BPMN_TEST, "FullyQualifiedName~BpmnDocumentPutCompareAndSwapTests"))
INPUTS = (MONGO, MONGO_TEST, BPMN_TEST, FIXTURE,
          "src/extensions/persistence/Elsa.Persistence.MongoDb/Common/MongoDbStore.cs",
          "src/extensions/persistence/Elsa.Persistence.MongoDb/Modules/Management/WorkflowDefinitionStore.cs",
          "src/extensions/Directory.Packages.props", "test/extensions/Directory.Packages.props",
          "scripts/integration-program/run_mongo_atomic_proof.py",
          "scripts/integration-program/run_execution_cycle_proof.py",
          "scripts/integration-program/run_current_import_affected_tests.py",
          "scripts/integration-program/package_closure.py")


def git(root: Path, *args: str) -> str:
    return subprocess.check_output(["git", "-C", str(root), *args], text=True,
                                   stderr=subprocess.DEVNULL).strip()


def baseline_summary(trx: Path, exit_code: int) -> dict:
    summary = test_summary(trx, exit_code)
    counts = summary["counters"]
    if (exit_code != 1 or counts["total"] != 2 or counts["executed"] != 2
            or counts["failed"] != 2 or any(counts.get(key, 0) for key in
                ("passed", "error", "timeout", "aborted", "inconclusive", "notRunnable", "notExecuted"))):
        raise ValueError("Baseline must execute exactly the two intended failing regressions")
    if {case["method"] for case in summary["cases"]} != set(REGRESSIONS):
        raise ValueError("Wrong baseline regression identities")
    root = ET.parse(trx).getroot()
    methods = {}
    for test in root.findall(".//{*}UnitTest"):
        method = test.find("{*}TestMethod")
        methods[test.get("id")] = f"{method.get('className')}.{method.get('name')}"
    for result in root.findall(".//{*}UnitTestResult"):
        message = result.find("{*}Output/{*}ErrorInfo/{*}Message")
        expected = REGRESSIONS[methods[result.get("testId")]]
        if result.get("outcome") != "Failed" or message is None or expected not in (message.text or ""):
            raise ValueError("Baseline failed outside the intended behavior assertion")
    summary.update(status="expected-regression-failures", expectedRegressionFailures=True)
    return summary


def check_baseline(root: Path, fixture_hash: str) -> None:
    if git(root, "rev-parse", "HEAD") != BASELINE:
        raise ValueError("Baseline revision changed")
    if git(root, "diff", "--name-only", "HEAD") != FIXTURE:
        raise ValueError("Baseline must differ only by the candidate regression fixture")
    if git(root, "status", "--porcelain", "--untracked-files=normal") != "M " + FIXTURE:
        raise ValueError("Baseline contains unexpected source files or changes")
    if git(root, "diff", "--cached", "--name-only"):
        raise ValueError("Baseline index changed")
    if digest(tracked_input(root, FIXTURE)) != fixture_hash:
        raise ValueError("Baseline fixture changed")


def require_case_coverage(row: dict, prefix: str, expected: dict[str, int]) -> None:
    cases = row["cases"]
    if len({(case["method"], case["caseSha256"]) for case in cases}) != len(cases):
        raise ValueError("Duplicate test case identity")
    actual = Counter(case["method"][len(prefix):] for case in cases
                     if case["method"].startswith(prefix) and case["outcome"] == "Passed")
    if actual != expected:
        raise ValueError("Required case manifest did not pass in full")


def service_identity(output: Path) -> dict:
    observed = {}
    image_ids = set()
    for label, expected_modes in (("baseline-probes", {"replica-set": 1}),
                                  ("candidate-tests-0", {"replica-set": 1, "standalone": 1})):
        directory = output / "service-identity" / label
        modes = Counter()
        for path in directory.glob("*.json"):
            if any(item.is_symlink() for item in (path, *path.parents)) or not path.is_file():
                raise ValueError("Invalid container identity file")
            data = json.loads(path.read_text())
            if set(data) != {"imageId", "replicaSet", "mode"}:
                raise ValueError("Unexpected container identity fields")
            if not re.fullmatch(r"sha256:[0-9a-f]{64}", data["imageId"]):
                raise ValueError("Invalid tested image ID")
            if data["mode"] not in expected_modes or data["replicaSet"] != ("rs1" if data["mode"] == "replica-set" else None):
                raise ValueError("Unexpected observed Mongo topology")
            modes[data["mode"]] += 1
            image_ids.add(data["imageId"])
        if modes != expected_modes:
            raise ValueError("Missing or duplicated tested container identity")
        observed[label] = dict(modes)
    if len(image_ids) != 1:
        raise ValueError("Baseline and candidate tested different Mongo images")
    return {"observedContainers": observed, "image": image_identity(next(iter(image_ids)))}


def image_identity(image_id: str) -> dict:
    data = json.loads(subprocess.check_output(
        ["docker", "image", "inspect", image_id], text=True, stderr=subprocess.DEVNULL))[0]
    sha = re.compile(r"sha256:[0-9a-f]{64}\Z")
    digests = data.get("RepoDigests", [])
    if (data.get("Id") != image_id or not sha.fullmatch(data.get("Id", "")) or not digests
            or any(not re.fullmatch(r"[A-Za-z0-9_./:-]+@sha256:[0-9a-f]{64}", value) for value in digests)
            or data.get("Os") != "linux" or data.get("Architecture") not in ("amd64", "arm64")):
        raise ValueError("Missing or invalid Mongo image identity")
    return {"configuredTag": "mongo:7.0.24", "id": data["Id"], "repoDigests": sorted(digests),
            "os": data["Os"], "architecture": data["Architecture"]}


def run(root: Path, output: Path, head: str) -> dict:
    root, output = root.resolve(), output.resolve()
    if output == root or root in output.parents or (output.exists() and any(output.iterdir())):
        raise ValueError("Evidence requires a fresh directory outside the checkout")
    assert_clean_source(root, head)
    git(root, "merge-base", "--is-ancestor", BASELINE, head)
    hashes = {path: digest(tracked_input(root, path)) for path in INPUTS}
    output.mkdir(parents=True, exist_ok=True)
    baseline = output / "baseline-source"
    receipt = {"schemaVersion": 1, "sourceRevision": head, "baselineRevision": BASELINE,
               "inputSha256": hashes, "publicationPerformed": False, "verificationComplete": False,
               "testBuilds": [], "tests": [], "builds": []}

    def save() -> None:
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")

    def compile_project(checkout: Path, project: str, framework: str, label: str) -> dict:
        row = {"project": project, "framework": framework}
        row.update(execute(["dotnet", "build", project, "-c", "Release", "-f", framework,
                            *PROPERTIES], checkout, output / f"{label}.log"))
        return row

    def test_project(checkout: Path, project: str, test_filter: str | None, label: str,
                     expect_failure: bool = False) -> dict:
        trx = output / f"{label}.trx"
        command = ["dotnet", "test", project, "-c", "Release", "-f", "net10.0", *PROPERTIES,
                   "--no-build", "--no-restore", "--logger", f"trx;LogFileName={trx.name}",
                   "--results-directory", str(output)]
        if test_filter:
            command.extend(["--filter", test_filter])
        row = {"project": project, "framework": "net10.0", "filter": test_filter}
        proof_directory = output / "service-identity" / label
        proof_directory.mkdir(parents=True)
        previous = os.environ.get("ELSA_MONGO_PROOF_DIRECTORY")
        os.environ["ELSA_MONGO_PROOF_DIRECTORY"] = str(proof_directory)
        try:
            row.update(execute(command, checkout, output / f"{label}.log"))
        finally:
            if previous is None:
                os.environ.pop("ELSA_MONGO_PROOF_DIRECTORY", None)
            else:
                os.environ["ELSA_MONGO_PROOF_DIRECTORY"] = previous
        try:
            row.update((baseline_summary if expect_failure else test_summary)(trx, row["exitCode"]))
        except (OSError, ValueError, KeyError, ET.ParseError):
            row.update(status="failed", evidenceError="Missing or invalid expected test evidence")
        return row

    save()
    git(root, "worktree", "add", "--detach", str(baseline), BASELINE)
    try:
        assert_clean_source(baseline, BASELINE)
        tracked_input(baseline, FIXTURE).write_bytes(tracked_input(root, FIXTURE).read_bytes())
        check_baseline(baseline, hashes[FIXTURE])
        for index, (checkout, project) in enumerate(((baseline, MONGO_TEST), (root, MONGO_TEST), (root, BPMN_TEST))):
            row = compile_project(checkout, project, "net10.0", f"fixture-build-{index}")
            row["source"] = "baseline-with-candidate-fixture" if checkout == baseline else "candidate"
            receipt["testBuilds"].append(row)
            save()
            print(f"Compile fixture {index}: {row['status']}", flush=True)
        if any(row["status"] != "passed" for row in receipt["testBuilds"]):
            return receipt
        probe_filter = "|".join(f"FullyQualifiedName={name}" for name in REGRESSIONS)
        receipt["baseline"] = test_project(baseline, MONGO_TEST, probe_filter, "baseline-probes", True)
        check_baseline(baseline, hashes[FIXTURE])
        save()
        if receipt["baseline"]["status"] != "expected-regression-failures":
            return receipt
        for index, (project, test_filter) in enumerate(TESTS):
            row = test_project(root, project, test_filter, f"candidate-tests-{index}")
            receipt["tests"].append(row)
            save()
            print(f"Test {project}: {row['status']}", flush=True)
            if row["status"] != "passed":
                return receipt
        require_case_coverage(receipt["tests"][0], PREFIX, MONGO_CASES)
        require_case_coverage(receipt["tests"][1], BPMN_PREFIX, BPMN_CASES)
        receipt["mongoService"] = service_identity(output)
        save()
        for framework in ("net8.0", "net9.0", "net10.0"):
            row = compile_project(root, MONGO, framework, f"production-{framework}")
            receipt["builds"].append(row)
            save()
            print(f"Build Mongo {framework}: {row['status']}", flush=True)
            if row["status"] != "passed":
                return receipt
        assert_clean_source(root, head)
        check_baseline(baseline, hashes[FIXTURE])
        if hashes != {path: digest(tracked_input(root, path)) for path in INPUTS}:
            raise ValueError("Verification inputs changed")
        receipt["verificationComplete"] = True
        save()
        return receipt
    finally:
        git(root, "worktree", "remove", "--force", str(baseline))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--expected-head", required=True)
    args = parser.parse_args()
    try:
        result = run(args.root, args.output, args.expected_head)
        raise SystemExit(0 if result["verificationComplete"] else 1)
    except (OSError, ValueError, IndexError, subprocess.SubprocessError):
        print("Mongo atomic verification failed during preflight or evidence validation.")
        raise SystemExit(1)
