#!/usr/bin/env python3
"""Run the offline admission proof; retain only validated, source-bound observations.

The reviewed manifest is deliberately incomplete until real fixture identities exist.
Its complete flag alone is insufficient: all eighteen families and runtime observations
must be present. Raw logs/TRX stay in private/, never the uploaded retained/ directory.
"""
from __future__ import annotations

import argparse
from contextlib import contextmanager
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

from run_current_import_affected_tests import assert_clean_source, classify
from run_execution_cycle_proof import digest, execute, test_summary, tracked_input

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = "scripts/integration-program/admission-proof-cases.json"
FAMILIES = ("identity", "admission", "create", "permit", "consume", "entry", "pipeline",
            "binding", "control", "continue", "withdraw", "resolve", "terminal", "time",
            "capacity", "bootstrap", "host", "compat")
FRAMEWORKS = ("net8.0", "net9.0", "net10.0")
PROPERTIES = ("-m:1", "-p:UseProjectReferences=true", "-p:IsPackable=false",
              "-p:GeneratePackageOnBuild=false", "-p:CollectCoverage=false")
TOKEN = re.compile(r"[a-z][a-z0-9-]{0,95}\Z")
FIELD = re.compile(r"[a-z][A-Za-z0-9-]{0,95}\Z")
NAME = re.compile(r"[A-Za-z_][A-Za-z0-9_.+`]{0,511}\Z")
HEX = re.compile(r"[0-9a-f]{64}\Z")
HEAD = re.compile(r"[0-9a-f]{40}\Z")
OBSERVATION_KEYS = {"schemaVersion", "caseId", "method", "parameterId", "sourceRevision",
                    "assertions", "facts", "services", "processes", "fixtureProcess", "cleanup", "liveProviderCalls"}


def matches(pattern: re.Pattern, value: object) -> bool:
    return type(value) is str and pattern.fullmatch(value) is not None


def require(condition: bool, category: str) -> None:
    if not condition:
        raise ValueError(category)


def keys(value: object, expected: set[str]) -> None:
    require(type(value) is dict and set(value) == expected, "unexpected_fields")


def regular(path: Path) -> Path:
    require(not any(p.is_symlink() for p in (path, *path.parents)) and path.is_file(),
            "nonregular_evidence")
    return path


def read_json(path: Path, maximum: int = 1024 * 1024) -> dict:
    regular(path)
    require(path.stat().st_size <= maximum, "oversized_evidence")
    # Duplicate JSON fields must not conceal private or conflicting data.
    def unique(pairs):
        result = {}
        for name, value in pairs:
            require(name not in result, "duplicate_json_field")
            result[name] = value
        return result
    return json.loads(path.read_text(), object_pairs_hook=unique)


def valid_path(value: str, suffix: str = "") -> bool:
    return (type(value) is str and bool(re.fullmatch(r"[A-Za-z0-9_.\-/]+", value))
            and not value.startswith("/") and ".." not in value.split("/")
            and Path(value).as_posix() == value and value.endswith(suffix))


def validate_manifest(data: dict) -> dict:
    keys(data, {"schemaVersion", "complete", "families", "testProjects", "buildProjects", "workerAssemblies", "cases"})
    require(type(data["schemaVersion"]) is int and data["schemaVersion"] == 1,
            "manifest_schema")
    require(data["complete"] is True and data["families"] == list(FAMILIES),
            "manifest_incomplete")
    require(type(data["testProjects"]) is list and bool(data["testProjects"])
            and type(data["buildProjects"]) is list and bool(data["buildProjects"]), "projects_missing")
    projects = set()
    for row in data["testProjects"]:
        keys(row, {"project", "filter", "observations"})
        require(valid_path(row["project"], ".csproj") and row["project"] not in projects
                and type(row["observations"]) is bool
                and type(row["filter"]) is str and bool(re.fullmatch(
                    r"FullyQualifiedName~[A-Za-z0-9_.]+(?:\|FullyQualifiedName~[A-Za-z0-9_.]+)*",
                    row["filter"])), "invalid_test_project")
        projects.add(row["project"])
    require(len(set(data["buildProjects"])) == len(data["buildProjects"])
            and all(valid_path(p, ".csproj") for p in data["buildProjects"]), "invalid_build_projects")
    require(type(data["workerAssemblies"]) is dict and all(matches(TOKEN, role)
            and valid_path(path, ".dll") and path.startswith("test/workers/")
            and "/bin/Release/net10.0/" in path
            for role, path in data["workerAssemblies"].items()), "invalid_worker_assemblies")
    require(type(data["cases"]) is list and bool(data["cases"]), "cases_missing")
    ids, identities, covered, case_projects = set(), set(), set(), set()
    for case in data["cases"]:
        keys(case, {"caseId", "family", "project", "method", "parameterId", "assertions",
                    "facts", "topology", "processRoles", "minimumProcesses", "restartRequired"})
        require(matches(TOKEN, case["caseId"]) and matches(TOKEN, case["parameterId"])
                and case["caseId"] not in ids and matches(NAME, case["method"])
                and case["family"] in FAMILIES, "invalid_case_identity")
        require(case["project"] in projects and next(p["observations"] for p in data["testProjects"]
                if p["project"] == case["project"]), "invalid_case_project")
        identity = (case["project"], case["method"], case["parameterId"])
        require(identity not in identities, "duplicate_case_identity")
        require(type(case["assertions"]) is list and bool(case["assertions"])
                and len(set(case["assertions"])) == len(case["assertions"])
                and all(matches(FIELD, s) for s in case["assertions"]), "invalid_assertions")
        require(type(case["facts"]) is dict and bool(case["facts"]), "missing_facts")
        for name, spec in case["facts"].items():
            require(matches(FIELD, name), "invalid_fact_name")
            validate_fact_spec(spec)
        require(case["topology"] in ("in-process", "postgresql", "postgresql-two-process")
                and type(case["minimumProcesses"]) is int and type(case["restartRequired"]) is bool
                and type(case["processRoles"]) is list
                and len(set(case["processRoles"])) == len(case["processRoles"])
                and all(s in data["workerAssemblies"] for s in case["processRoles"]), "invalid_topology")
        if case["topology"] != "postgresql-two-process":
            require(case["minimumProcesses"] == 0 and not case["processRoles"]
                    and not case["restartRequired"], "invalid_inprocess_topology")
        else:
            require(case["minimumProcesses"] >= 2 and bool(case["processRoles"]), "missing_process_proof")
        ids.add(case["caseId"]); identities.add(identity); covered.add(case["family"])
        case_projects.add(case["project"])
    require(covered == set(FAMILIES), "missing_family")
    # A local execution host can use real PostgreSQL without becoming a second
    # execution process. Require an actual entry case in that single-process lane,
    # retaining PostgreSQL identity/disposal evidence when it is present.
    require(any(c["topology"] == "postgresql-two-process" for c in data["cases"])
            and any(c["family"] == "entry" and c["topology"] in ("in-process", "postgresql")
                    for c in data["cases"]), "missing_required_topology")
    for case in data["cases"]:
        if case["parameterId"] == "default":
            require(sum(c["method"] == case["method"] and c["project"] == case["project"]
                        for c in data["cases"]) == 1, "ambiguous_fact_method")
    require(case_projects == {p["project"] for p in data["testProjects"] if p["observations"]},
            "missing_project_cases")
    return data


def validate_fact_spec(spec: dict) -> None:
    require(type(spec) is dict, "invalid_fact_spec")
    kind = spec.get("kind")
    if kind == "boolean":
        keys(spec, {"kind", "equals"}); require(type(spec["equals"]) is bool, "invalid_boolean_spec")
    elif kind == "integer":
        keys(spec, {"kind", "minimum", "maximum"})
        require(type(spec["minimum"]) is int and type(spec["maximum"]) is int
                and 0 <= spec["minimum"] <= spec["maximum"] <= 10**9, "invalid_integer_spec")
    elif kind == "enum":
        keys(spec, {"kind", "values"})
        require(type(spec["values"]) is list and bool(spec["values"])
                and len(set(spec["values"])) == len(spec["values"])
                and all(matches(NAME, v) for v in spec["values"]), "invalid_enum_spec")
    else:
        keys(spec, {"kind"}); require(kind == "sha256", "invalid_fact_kind")


def validate_fact(value: object, spec: dict) -> None:
    kind = spec["kind"]
    valid = ((kind == "boolean" and type(value) is bool and value == spec["equals"])
             or (kind == "integer" and type(value) is int and spec["minimum"] <= value <= spec["maximum"])
             or (kind == "enum" and type(value) is str and value in spec["values"])
             or (kind == "sha256" and type(value) is str and bool(HEX.fullmatch(value))))
    require(valid, "fact_failed")


def trx_identities(path: Path, exit_code: int) -> tuple[dict, list[tuple[str, str]]]:
    summary = test_summary(regular(path), exit_code)
    require(summary["status"] == "passed", "test_not_passed")
    doc = ET.parse(path).getroot()
    methods = {}
    for test in doc.findall(".//{*}UnitTest"):
        method = test.find("{*}TestMethod")
        methods[test.get("id")] = f"{method.get('className')}.{method.get('name')}"
    identities = []
    for result in doc.findall(".//{*}UnitTestResult"):
        method = methods[result.get("testId")]
        # xUnit's first named InlineData string must be the reviewed safe ID. Do not
        # scrape arbitrary quoted values or accept missing/ambiguous display metadata.
        display = result.get("testName", "")
        if display == method:
            identities.append((method, "default"))
            continue
        match = re.fullmatch(re.escape(method) + r'\((?:caseId|parameterId): "([a-z][a-z0-9-]{0,95})"(?:, [^\r\n]*)?\)', display)
        require(match is not None and not re.search(r', (?:caseId|parameterId):', display),
                "missing_or_ambiguous_parameter")
        identities.append((method, match[1]))
    require(len(set(identities)) == len(identities), "duplicate_trx_identity")
    return summary, identities


def validate_observation(data: dict, case: dict, head: str) -> dict:
    keys(data, OBSERVATION_KEYS)
    require(type(data["schemaVersion"]) is int and data["schemaVersion"] == 1
            and data["sourceRevision"] == head, "observation_source")
    require(all(data[key] == case[key] for key in ("caseId", "method", "parameterId")),
            "observation_case_identity")
    keys(data["fixtureProcess"], {"pid", "startIdentitySha256", "assemblySha256"})
    fixture = data["fixtureProcess"]
    require(type(fixture["pid"]) is int and fixture["pid"] > 0
            and matches(HEX, fixture["startIdentitySha256"])
            and matches(HEX, fixture["assemblySha256"]), "fixture_process")
    keys(data["assertions"], set(case["assertions"]))
    require(all(v is True for v in data["assertions"].values()), "assertion_failed")
    keys(data["facts"], set(case["facts"]))
    for name, spec in case["facts"].items(): validate_fact(data["facts"][name], spec)
    keys(data["cleanup"], {"ownedProcessesStopped", "ownedListenersStopped"})
    require(all(v is True for v in data["cleanup"].values()) and type(data["liveProviderCalls"]) is int
            and data["liveProviderCalls"] == 0, "cleanup_or_live_effect")
    require(type(data["services"]) is list and type(data["processes"]) is list, "invalid_topology_evidence")
    if case["topology"] == "in-process":
        require(not data["services"] and not data["processes"], "unexpected_service_or_process")
        return data
    require(bool(data["services"]) and len(data["processes"]) >= case["minimumProcesses"],
            "missing_actual_topology")
    containers = set()
    for service in data["services"]:
        keys(service, {"kind", "containerId", "imageId", "databaseIdentitySha256", "serverVersion"})
        require(service["kind"] == "postgresql" and matches(HEX, service["containerId"])
                and matches(re.compile(r"sha256:[0-9a-f]{64}"), service["imageId"])
                and matches(HEX, service["databaseIdentitySha256"])
                and matches(re.compile(r"[0-9]+(?:\.[0-9]+){0,3}"), service["serverVersion"])
                and service["containerId"] not in containers, "service_identity")
        containers.add(service["containerId"])
    identities, roles, generations, pids = set(), set(), set(), set()
    for process in data["processes"]:
        keys(process, {"pid", "role", "generation", "startIdentitySha256", "assemblySha256", "exitCode", "exited"})
        require(type(process["pid"]) is int and process["pid"] > 0 and process["role"] in case["processRoles"]
                and type(process["generation"]) is int and process["generation"] > 0
                and matches(HEX, process["startIdentitySha256"]) and matches(HEX, process["assemblySha256"])
                and type(process["exitCode"]) is int and process["exited"] is True, "process_identity")
        identity = (process["pid"], process["startIdentitySha256"])
        require(identity not in identities, "duplicate_process")
        identities.add(identity); pids.add(process["pid"]); roles.add(process["role"])
        generations.add(process["generation"])
    require(len(pids) >= case["minimumProcesses"] and roles == set(case["processRoles"]), "process_role_mismatch")
    require(not case["restartRequired"] or len(generations) >= 2, "restart_not_observed")
    return data


def image_identity(image_id: str) -> dict:
    rows = json.loads(subprocess.check_output(["docker", "image", "inspect", image_id],
                                             text=True, stderr=subprocess.DEVNULL))
    require(type(rows) is list and len(rows) == 1 and type(rows[0]) is dict, "image_inspect_shape")
    data = rows[0]
    digests = data.get("RepoDigests", [])
    require(data.get("Id") == image_id and data.get("Os") == "linux"
            and data.get("Architecture") in ("amd64", "arm64") and bool(digests)
            and all(re.fullmatch(r"[A-Za-z0-9_./:-]+@sha256:[0-9a-f]{64}", v) for v in digests),
            "image_identity")
    return {"id": image_id, "repoDigests": sorted(digests), "os": data["Os"],
            "architecture": data["Architecture"]}


def assembly_path(project: str) -> str:
    path = Path(project)
    return (path.parent / "bin" / "Release" / "net10.0" / (path.stem + ".dll")).as_posix()


def bind_assemblies(record: dict, case: dict, manifest: dict, hashes: dict) -> None:
    require(record["fixtureProcess"]["assemblySha256"] == hashes[assembly_path(case["project"])],
            "fixture_assembly_mismatch")
    for process in record["processes"]:
        require(process["assemblySha256"] == hashes[manifest["workerAssemblies"][process["role"]]],
                "worker_assembly_mismatch")


def validate_evidence(directory: Path, cases: list[dict], identities: list[tuple[str, str]], head: str,
                      manifest: dict, hashes: dict) -> tuple[list[dict], dict | None]:
    require(directory.is_dir() and not directory.is_symlink(), "missing_observations")
    require(set(identities) == {(c["method"], c["parameterId"]) for c in cases}, "trx_manifest_mismatch")
    expected = {c["caseId"] + ".json" for c in cases}
    actual = {p.name for p in directory.iterdir()}
    postgres = any(c["topology"] != "in-process" for c in cases)
    require(actual == expected | ({"fixture.json"} if postgres else set()), "observation_layout")
    records = [validate_observation(read_json(directory / (c["caseId"] + ".json")), c, head) for c in cases]
    for record, case in zip(records, cases):
        bind_assemblies(record, case, manifest, hashes)
    if postgres:
        fixture = read_json(directory / "fixture.json")
        keys(fixture, {"schemaVersion", "sourceRevision", "disposedContainerIds"})
        containers = {s["containerId"] for record in records for s in record["services"]}
        disposed = fixture["disposedContainerIds"]
        require(type(fixture["schemaVersion"]) is int and fixture["schemaVersion"] == 1
                and fixture["sourceRevision"] == head and type(disposed) is list
                and len(set(disposed)) == len(disposed) and set(disposed) == containers, "fixture_cleanup")
        # A successful Docker query distinguishes actual removal from a dead daemon.
        present = subprocess.check_output(["docker", "ps", "-aq", "--no-trunc"], text=True,
                                          stderr=subprocess.DEVNULL).splitlines()
        require(all(HEX.fullmatch(v) for v in present) and not containers.intersection(present),
                "container_not_disposed")
        images = {}
        for record in records:
            for service in record["services"]:
                if service["imageId"] not in images:
                    images[service["imageId"]] = image_identity(service["imageId"])
                service["image"] = images[service["imageId"]]
        return records, {"project": cases[0]["project"], "disposedContainerIds": sorted(containers),
                         "absenceVerified": True}
    return records, None


def source_hashes(root: Path) -> dict[str, str]:
    # Conservative tracked superset includes implementation, workers/migrations, shared
    # props/targets/configuration and proof dependencies, not just csproj references.
    paths = subprocess.check_output(["git", "-C", str(root), "ls-files", "-z", "--", "src", "test",
        "build", "scripts/integration-program", ".github/workflows/admission-proof.yml", ".config",
        "Directory.*", "global.json", "NuGet.Config", "Elsa.sln", ".editorconfig", ".gitignore",
        ".gitattributes"], text=True).split("\0")
    # The single ls-files result already establishes tracking; avoid thousands of
    # redundant Git children while retaining regular-file and symlink checks.
    return {p: digest(regular(root / p)) for p in sorted(set(paths) - {""})}


@contextmanager
def proof_environment(directory: Path, head: str):
    updates = {"ELSA_ADMISSION_PROOF_DIRECTORY": str(directory), "ELSA_ADMISSION_SOURCE_REVISION": head}
    previous = {k: os.environ.get(k) for k in updates}
    os.environ.update(updates)
    try: yield
    finally:
        for key, value in previous.items():
            if value is None: os.environ.pop(key, None)
            else: os.environ[key] = value


def command_receipt(row: dict, *, test: bool = False) -> None:
    expected = {"project", "framework", "exitCode", "durationSeconds", "logSha256",
                "compilerDiagnostics", "status"}
    if test:
        expected |= {"filter", "counters", "cases", "trxSha256", "identities"}
    keys(row, expected)
    require(valid_path(row["project"], ".csproj") and row["framework"] in FRAMEWORKS
            and type(row["exitCode"]) is int and type(row["durationSeconds"]) in (int, float)
            and 0 <= row["durationSeconds"] <= 3600 and matches(HEX, row["logSha256"])
            and row["status"] in ("passed", "failed", "incomplete"), "invalid_command_receipt")
    require(type(row["compilerDiagnostics"]) is list, "invalid_diagnostics")
    for diagnostic in row["compilerDiagnostics"]:
        keys(diagnostic, {"file", "location", "code"})
        require(valid_path(diagnostic["file"])
                and matches(re.compile(r"\(\d+,\d+\)"), diagnostic["location"])
                and matches(re.compile(r"[A-Z]+\d+"), diagnostic["code"]), "invalid_diagnostic")
    if test:
        require(type(row["filter"]) is str and type(row["cases"]) is list
                and type(row["counters"]) is dict and matches(HEX, row["trxSha256"]), "invalid_test_receipt")
        for name, value in row["counters"].items():
            require(matches(NAME, name) and type(value) is int and 0 <= value <= 10**9, "invalid_counter")
        for case in row["cases"]:
            keys(case, {"method", "outcome", "caseSha256"})
            require(matches(NAME, case["method"]) and case["outcome"] in ("Passed", "Failed", "NotExecuted")
                    and matches(HEX, case["caseSha256"]), "invalid_test_case")
        require(classify(row["exitCode"], row["counters"], row["cases"]) == row["status"],
                "false_test_status")
        require(type(row["identities"]) is list, "invalid_test_identities")
        for identity in row["identities"]:
            keys(identity, {"method", "parameterId"})
            require(matches(NAME, identity["method"]) and matches(TOKEN, identity["parameterId"]),
                    "invalid_test_identity")
    else:
        require((row["exitCode"] == 0) == (row["status"] == "passed"), "false_build_status")


def validate_receipt(data: dict, manifest: dict, expected_head: str) -> None:
    keys(data, {"schemaVersion", "sourceRevision", "sourceTree", "runIdentity", "inputSha256",
               "caseManifestSha256", "compiledAssemblySha256", "publicationPerformed", "liveProviderCalls",
               "verificationComplete", "postSourceVerified", "failureCategory", "testBuilds", "tests",
               "builds", "cases", "serviceCleanup"})
    require(type(data["schemaVersion"]) is int and data["schemaVersion"] == 1
            and matches(HEAD, data["sourceRevision"]) and data["sourceRevision"] == expected_head
            and matches(HEAD, data["sourceTree"]), "receipt_source")
    keys(data["runIdentity"], {"repository", "runId", "attempt", "event"})
    run_id = data["runIdentity"]
    require(run_id["repository"] == "elsa-workflows/elsa-core" and type(run_id["runId"]) is int
            and run_id["runId"] > 0 and type(run_id["attempt"]) is int and run_id["attempt"] > 0
            and run_id["event"] in ("pull_request", "workflow_dispatch"), "receipt_run_identity")
    for name in ("inputSha256", "compiledAssemblySha256"):
        require(type(data[name]) is dict and all(valid_path(p) and matches(HEX, value)
                for p, value in data[name].items()), "invalid_hash_inventory")
    expected_assemblies = {assembly_path(p["project"]) for p in manifest["testProjects"]}
    expected_assemblies.update(manifest["workerAssemblies"].values())
    require(not data["compiledAssemblySha256"] or set(data["compiledAssemblySha256"]) == expected_assemblies,
            "unexpected_compiled_inventory")
    require(matches(HEX, data["caseManifestSha256"])
            and data["inputSha256"].get(MANIFEST) == data["caseManifestSha256"]
            and data["publicationPerformed"] is False and type(data["liveProviderCalls"]) is int
            and data["liveProviderCalls"] == 0 and type(data["verificationComplete"]) is bool
            and type(data["postSourceVerified"]) is bool
            and data["failureCategory"] in (None, "incomplete_or_invalid_proof"), "invalid_receipt_flags")
    for name in ("testBuilds", "tests", "builds", "cases", "serviceCleanup"):
        require(type(data[name]) is list, "invalid_receipt_inventory")
    projects = {p["project"]: p for p in manifest["testProjects"]}
    for name in ("testBuilds", "tests", "builds"):
        identities = []
        for row in data[name]:
            command_receipt(row, test=name == "tests")
            require(row["project"] in (manifest["buildProjects"] if name == "builds" else projects),
                    "unexpected_command_project")
            if name != "builds":
                require(row["framework"] == "net10.0", "unexpected_fixture_framework")
            if name == "tests":
                require(row["filter"] == projects[row["project"]]["filter"], "unexpected_test_filter")
                expected = {(c["method"], c["parameterId"]) for c in manifest["cases"]
                            if c["project"] == row["project"]}
                actual = [(i["method"], i["parameterId"]) for i in row["identities"]]
                # An incomplete attempt may retain only its sanitized TRX summary.
                # Final acceptance still requires the complete reviewed identity bijection.
                if actual or data["verificationComplete"] or not projects[row["project"]]["observations"]:
                    require(len(actual) == len(set(actual)) and set(actual) == expected,
                            "receipt_trx_manifest_mismatch")
                    if projects[row["project"]]["observations"]:
                        require(len(actual) == len(row["cases"])
                                and [i["method"] for i in row["identities"]] == [c["method"] for c in row["cases"]],
                                "receipt_trx_identity_mismatch")
            identities.append((row["project"], row["framework"]))
        require(len(identities) == len(set(identities)), "duplicate_command")
    case_map = {c["caseId"]: c for c in manifest["cases"]}
    seen = set()
    for record in data["cases"]:
        require(type(record) is dict and record.get("caseId") in case_map
                and record["caseId"] not in seen, "unexpected_receipt_case")
        seen.add(record["caseId"])
        if set(record) == {"caseId", "status"}:
            require(record["status"] == "not_run" and not data["verificationComplete"], "false_complete_case")
            continue
        # The only enrichment is an independently inspected immutable image identity.
        observation = {**record, "services": []}
        for service in record["services"]:
            keys(service, {"kind", "containerId", "imageId", "databaseIdentitySha256", "serverVersion", "image"})
            image = service["image"]
            keys(image, {"id", "repoDigests", "os", "architecture"})
            require(image["id"] == service["imageId"] and image["os"] == "linux"
                    and image["architecture"] in ("amd64", "arm64")
                    and type(image["repoDigests"]) is list and bool(image["repoDigests"])
                    and all(matches(re.compile(r"[A-Za-z0-9_./:-]+@sha256:[0-9a-f]{64}"), v)
                            for v in image["repoDigests"]), "invalid_retained_image")
            observation["services"].append({k: v for k, v in service.items() if k != "image"})
        validate_observation(observation, case_map[record["caseId"]], expected_head)
        bind_assemblies(record, case_map[record["caseId"]], manifest, data["compiledAssemblySha256"])
    cleanup_projects = set()
    for row in data["serviceCleanup"]:
        keys(row, {"project", "disposedContainerIds", "absenceVerified"})
        require(row["project"] in projects and row["project"] not in cleanup_projects
                and row["absenceVerified"] is True and type(row["disposedContainerIds"]) is list
                and bool(row["disposedContainerIds"])
                and len(set(row["disposedContainerIds"])) == len(row["disposedContainerIds"])
                and all(matches(HEX, v) for v in row["disposedContainerIds"]), "invalid_cleanup_receipt")
        expected = {s["containerId"] for r in data["cases"] if case_map[r["caseId"]]["project"] == row["project"]
                    for s in r.get("services", [])}
        require(expected == set(row["disposedContainerIds"]), "cleanup_service_mismatch")
        cleanup_projects.add(row["project"])
    if data["verificationComplete"]:
        validate_manifest(manifest)
        require(data["failureCategory"] is None and data["postSourceVerified"] is True
                and seen == set(case_map) and set(data["compiledAssemblySha256"]) == expected_assemblies,
                "false_complete")
        for name, expected in (("testBuilds", {(p, "net10.0") for p in projects}),
                               ("tests", {(p, "net10.0") for p in projects}),
                               ("builds", {(p, f) for p in manifest["buildProjects"] for f in FRAMEWORKS})):
            require({(r["project"], r["framework"]) for r in data[name]} == expected
                    and all(r["status"] == "passed" for r in data[name]), "missing_complete_command")
        require(cleanup_projects == {c["project"] for c in manifest["cases"] if c["topology"] != "in-process"},
                "missing_complete_cleanup")


def validate_retained(output: Path, manifest: dict, expected_head: str, root: Path | None = None) -> None:
    require(output.is_dir() and not output.is_symlink() and {p.name for p in output.iterdir()} == {"receipt.json"},
            "retained_layout")
    regular(output / "receipt.json")
    data = (output / "receipt.json").read_bytes()
    require(len(data) <= 16 * 1024 * 1024, "retained_too_large")
    require(not any(marker in data.lower() for marker in (b"synthetic-secret", b"synthetic-access-token",
            b"synthetic-refresh-token", b"access-never-log-8a6f", b"refresh-never-log-1f92",
            b"password=", b"bearer ")), "private_marker")
    receipt = read_json(output / "receipt.json", 16 * 1024 * 1024)
    validate_receipt(receipt, manifest, expected_head)
    if root is not None:
        require(receipt["inputSha256"] == source_hashes(root), "retained_source_changed")
        tree = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD^{tree}"], text=True).strip()
        require(receipt["sourceTree"] == tree, "retained_tree_changed")


def run(root: Path, output: Path, head: str) -> dict:
    require(type(head) is str and HEAD.fullmatch(head), "invalid_head")
    require(not any(p.is_symlink() for p in (output, *output.parents)), "output_symlink")
    root, output = root.resolve(), output.resolve()
    require(output != root and root not in output.parents and not output.exists(), "output_not_fresh")
    assert_clean_source(root, head)
    tree = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD^{tree}"], text=True).strip()
    hashes = source_hashes(root)
    require(MANIFEST in hashes, "untracked_manifest")
    manifest = read_json(tracked_input(root, MANIFEST))
    run_identity = {"repository": os.environ.get("GITHUB_REPOSITORY"),
                    "runId": int(os.environ.get("GITHUB_RUN_ID", "0")),
                    "attempt": int(os.environ.get("GITHUB_RUN_ATTEMPT", "0")),
                    "event": os.environ.get("GITHUB_EVENT_NAME")}
    output.mkdir(mode=0o700, parents=True)
    private, retained = output / "private", output / "retained"
    private.mkdir(mode=0o700); retained.mkdir(mode=0o700)
    receipt = {"schemaVersion": 1, "sourceRevision": head, "sourceTree": tree, "runIdentity": run_identity,
               "inputSha256": hashes, "caseManifestSha256": hashes[MANIFEST],
               "compiledAssemblySha256": {}, "postSourceVerified": False, "serviceCleanup": [],
               "publicationPerformed": False, "liveProviderCalls": 0, "verificationComplete": False,
               "failureCategory": None, "testBuilds": [], "tests": [], "builds": [], "cases": []}
    def save():
        path = retained / "receipt.json"
        path.write_text(json.dumps(receipt, indent=2) + "\n"); path.chmod(0o600)
        validate_retained(retained, manifest, head)
    save()
    try:
        validate_manifest(manifest)
        receipt["cases"] = [{"caseId": c["caseId"], "status": "not_run"} for c in manifest["cases"]]
        for index, row in enumerate(manifest["testProjects"]):
            project = row["project"]; require(project in hashes, "untracked_project")
            result = {"project": project, "framework": "net10.0"}
            result.update(execute(["dotnet", "build", project, "-c", "Release", "-f", "net10.0",
                                   *PROPERTIES], root, private / f"compile-{index}.log"))
            receipt["testBuilds"].append(result); save()
        require(all(row["status"] == "passed" for row in receipt["testBuilds"]), "fixture_compile")
        assemblies = {assembly_path(p["project"]) for p in manifest["testProjects"]}
        assemblies.update(manifest["workerAssemblies"].values())
        receipt["compiledAssemblySha256"] = {p: digest(regular(root / p)) for p in sorted(assemblies)}
        observations = []
        for index, row in enumerate(manifest["testProjects"]):
            trx = private / f"test-{index}.trx"; directory = private / f"observations-{index}"
            directory.mkdir(mode=0o700)
            result = {"project": row["project"], "framework": "net10.0", "filter": row["filter"]}
            with proof_environment(directory, head):
                result.update(execute(["dotnet", "test", row["project"], "-c", "Release", "-f", "net10.0",
                    *PROPERTIES, "--no-build", "--no-restore", "--filter", row["filter"], "--logger",
                    f"trx;LogFileName={trx.name}", "--results-directory", str(private)], root, private / f"test-{index}.log"))
            # Retain safe counts/method outcomes before strict identity/observation
            # validation. A failing suite must remain visible without uploading raw logs.
            result.update(test_summary(regular(trx), result["exitCode"]))
            result["identities"] = []
            receipt["tests"].append(result); save()
            require(result["status"] == "passed", "fixture_test")
            if row["observations"]:
                _, identities = trx_identities(trx, result["exitCode"])
                selected = [c for c in manifest["cases"] if c["project"] == row["project"]]
                records, cleanup = validate_evidence(directory, selected, identities, head,
                                                     manifest, receipt["compiledAssemblySha256"])
                observations.extend(records)
                result["identities"] = [{"method": method, "parameterId": parameter} for method, parameter in identities]
                if cleanup:
                    receipt["serviceCleanup"].append(cleanup)
            else:
                require(not list(directory.iterdir()), "unexpected_supplemental_observations")
            receipt["cases"] = observations + [{"caseId": c["caseId"], "status": "not_run"}
                for c in manifest["cases"] if c["caseId"] not in {r["caseId"] for r in observations}]
            save()
        receipt["cases"] = observations; save()
        for framework in FRAMEWORKS:
            for project in manifest["buildProjects"]:
                require(project in hashes, "untracked_build_project")
                result = {"project": project, "framework": framework}
                result.update(execute(["dotnet", "build", project, "-c", "Release", "-f", framework,
                    *PROPERTIES], root, private / f"build-{len(receipt['builds'])}.log"))
                receipt["builds"].append(result); save(); require(result["status"] == "passed", "production_build")
        assert_clean_source(root, head)
        require(hashes == source_hashes(root), "source_changed")
        require(receipt["compiledAssemblySha256"] == {p: digest(regular(root / p)) for p in sorted(assemblies)},
                "compiled_assemblies_changed")
        receipt["postSourceVerified"] = True
        receipt["verificationComplete"] = True
    except (OSError, ValueError, KeyError, TypeError, ET.ParseError, subprocess.SubprocessError):
        # Categories are fixed; no arbitrary fixture errors, command output or payloads.
        receipt["failureCategory"] = "incomplete_or_invalid_proof"
    save()
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--expected-head", required=True)
    parser.add_argument("--validate-retained", action="store_true")
    args = parser.parse_args()
    try:
        if args.validate_retained:
            assert_clean_source(args.root.resolve(), args.expected_head)
            validate_retained(args.output, read_json(tracked_input(args.root.resolve(), MANIFEST)),
                              args.expected_head, args.root.resolve())
        else:
            result = run(args.root, args.output, args.expected_head)
            raise SystemExit(0 if result["verificationComplete"] else 1)
    except (OSError, ValueError, KeyError, TypeError, ET.ParseError, subprocess.SubprocessError):
        print("Admission verification failed during preflight or retained-evidence validation.")
        raise SystemExit(1)
