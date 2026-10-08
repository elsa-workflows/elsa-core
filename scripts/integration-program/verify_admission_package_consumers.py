"""Validate complete #8661 fixture reports; package bytes and external bindings remain caller-owned.

The tracked contract is source input, never an artifact-supplied schema. Call validate_cell
with the observed child PID and precreated database hashes before retaining a report.
validate_matrix rechecks content and coverage, but cannot establish those external facts.
Raw locations are private inputs: the caller verifies package/cache/loaded bytes and
normalizes paths before publication. No file writing or success-shaped partial receipts.
"""
from __future__ import annotations

import json
from pathlib import Path, PurePosixPath
import re
from typing import Any

FRAMEWORKS = ("net8.0", "net9.0", "net10.0")
FEATURES = ("classic", "shell")
SCENARIOS = ("admission", "managed-secret-grant")
SHA256 = re.compile(r"[0-9a-f]{64}")
HEAD = re.compile(r"[0-9a-f]{40}")
VERSION = re.compile(r"[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?")
MIGRATION = re.compile(r"[0-9]{14}_[A-Za-z0-9_]+")
NAME = re.compile(r"Elsa(?:\.[A-Za-z0-9_]+)*")


def _require(condition: bool, category: str) -> None:
    if not condition:
        raise ValueError("Admission consumer report rejected: " + category)


def _keys(value: Any, keys: Any, category: str) -> None:
    _require(type(value) is dict and set(value) == set(keys), category)


def _match(value: Any, pattern: re.Pattern[str]) -> bool:
    return type(value) is str and pattern.fullmatch(value) is not None


def _strings(value: Any, *, pattern: re.Pattern[str] | None = None) -> bool:
    return (type(value) is list and 0 < len(value) <= 1024
            and all(type(item) is str and item and (pattern is None or pattern.fullmatch(item)) for item in value)
            and len(set(value)) == len(value))


def _validate_contract(contract: Any) -> dict:
    """Reject malformed/partial tracked schema rather than interpreting it tolerantly."""
    _keys(contract, ("schemaVersion", "rootKeys", "processKeys", "loadedAssemblyKeys", "scenarioKeys",
                     "migrationKeys", "scenarios", "migrationRules", "requiredAssemblies"), "contract shape")
    _require(type(contract["schemaVersion"]) is int and contract["schemaVersion"] == 1, "contract version")
    fields = {
        "rootKeys": "schemaVersion sourceRevision candidateVersion tfm feature targetFramework frameworkDescription process loadedAssemblies scenarios",
        "processKeys": "pid startIdentitySha256",
        "loadedAssemblyKeys": "name fullName version informationalVersion location sha256",
        "scenarioKeys": "scenario databaseIdentitySha256 serverVersion assertions counters migrations cleanup",
        "migrationKeys": "context provider schema historyTable knownIds appliedIds historyIds reapplied populatedPreserved",
    }
    for key, expected in fields.items():
        _require(_strings(contract[key]) and set(contract[key]) == set(expected.split()), "contract fields")
    _keys(contract["scenarios"], SCENARIOS, "contract scenarios")
    for name, scenario in contract["scenarios"].items():
        _keys(scenario, ("assertions", "counters", "contexts", "cleanup"), "contract scenario shape")
        _require(type(scenario["assertions"]) is dict and bool(scenario["assertions"])
                 and all(type(key) is str and re.fullmatch(r"[A-Za-z][A-Za-z0-9]*", key)
                         and value is True for key, value in scenario["assertions"].items()), "contract assertions")
        _require(type(scenario["counters"]) is dict and bool(scenario["counters"])
                 and all(type(key) is str and re.fullmatch(r"[A-Za-z][A-Za-z0-9]*", key)
                         and type(value) is int and value >= 0 for key, value in scenario["counters"].items()), "contract counters")
        expected_contexts = {"SecretsElsaDbContext", "ConnectionsElsaDbContext", "ManagementElsaDbContext", "RuntimeElsaDbContext"}
        if name == "admission":
            expected_contexts.add("AdmissionElsaDbContext")
        _require(_strings(scenario["contexts"]) and set(scenario["contexts"]) == expected_contexts, "contract contexts")
        _keys(scenario["cleanup"], ("hostDisposed", "executorsCreated", "maximumConcurrentExecutors"), "contract cleanup")
        _require(scenario["cleanup"]["hostDisposed"] is True
                 and type(scenario["cleanup"]["executorsCreated"]) is int
                 and scenario["cleanup"]["executorsCreated"] == (1 if name == "admission" else 2)
                 and type(scenario["cleanup"]["maximumConcurrentExecutors"]) is int
                 and scenario["cleanup"]["maximumConcurrentExecutors"] == 1, "contract cleanup values")
    rules = contract["migrationRules"]
    _keys(rules, ("provider", "schema", "connectionsIds", "admissionIds"), "contract migrations")
    _require(rules["provider"] == "Npgsql.EntityFrameworkCore.PostgreSQL" and rules["schema"] == "Elsa", "contract provider")
    _require(rules["connectionsIds"] == ["20260923225653_Initial", "20260924125922_WorkflowCredentialUseGrants", "20260924150000_DueCredentialLifecycleCandidates"]
             and rules["admissionIds"] == ["20261008040000_InitialAdmission"], "contract migration identities")
    expected_assemblies = {
        "Elsa.Connections", "Elsa.Connections.Credentials.Workflows", "Elsa.Connections.Credentials.Persistence.EFCore",
        "Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql", "Elsa.Workflows.Admission",
        "Elsa.Workflows.Admission.Persistence.EFCore", "Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql",
    }
    _require(_strings(contract["requiredAssemblies"]) and set(contract["requiredAssemblies"]) == expected_assemblies, "contract assemblies")
    return contract


CONTRACT = _validate_contract(json.loads(Path(__file__).with_name("admission-package-consumer-contract.json").read_text(encoding="utf-8")))


def _exact_values(actual: Any, expected: dict, category: str) -> None:
    _keys(actual, expected, category)
    _require(all(type(actual[key]) is type(value) and actual[key] == value
                 for key, value in expected.items()), category)


def _assemblies(rows: Any) -> None:
    _require(type(rows) is list and 0 < len(rows) <= 1024, "loaded assemblies")
    names: set[str] = set()
    for row in rows:
        _keys(row, CONTRACT["loadedAssemblyKeys"], "loaded assembly shape")
        _require(_match(row["name"], NAME) and row["name"].casefold() not in names, "loaded assembly name")
        names.add(row["name"].casefold())
        _require(_match(row["version"], re.compile(r"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+"))
                 and type(row["fullName"]) is str
                 and re.fullmatch(re.escape(row["name"] + ", Version=" + row["version"])
                                  + r", Culture=[A-Za-z0-9-]+, PublicKeyToken=(?:null|[0-9a-f]{16})", row["fullName"]) is not None
                 and _match(row["informationalVersion"], VERSION), "loaded assembly identity")
        location = row["location"]
        _require(type(location) is str and 0 < len(location) <= 4096
                 and not any(ord(char) < 32 for char in location) and "\\" not in location
                 and PurePosixPath(location).is_absolute() and ".." not in PurePosixPath(location).parts
                 and PurePosixPath(location).name == row["name"] + ".dll"
                 and _match(row["sha256"], SHA256), "loaded assembly location/hash")
    _require({name.casefold() for name in CONTRACT["requiredAssemblies"]} <= names, "required loaded assemblies")


def _migrations(rows: Any, contexts: list[str]) -> None:
    _require(type(rows) is list and len(rows) == len(contexts), "migration inventory")
    seen: set[str] = set()
    shared_history = None
    shared_known: set[str] = set()
    for row in rows:
        _keys(row, CONTRACT["migrationKeys"], "migration shape")
        context = row["context"]
        _require(type(context) is str and context in contexts and context not in seen, "migration context")
        seen.add(context)
        _require(row["provider"] == CONTRACT["migrationRules"]["provider"] and row["schema"] == "Elsa", "migration provider/schema")
        for key in ("knownIds", "appliedIds", "historyIds"):
            _require(_strings(row[key], pattern=MIGRATION) and row[key] == sorted(row[key]), "migration IDs")
        _require(row["appliedIds"] == row["knownIds"] and set(row["knownIds"]) <= set(row["historyIds"]), "migration application")
        _require(row["reapplied"] is True and row["populatedPreserved"] is True, "migration reapplication")
        if context == "AdmissionElsaDbContext":
            _require(row["historyTable"] == "__AdmissionMigrationsHistory"
                     and row["knownIds"] == CONTRACT["migrationRules"]["admissionIds"]
                     and row["historyIds"] == row["knownIds"], "admission migration history")
        else:
            _require(row["historyTable"] == "__EFMigrationsHistory", "shared migration history table")
            if context == "ConnectionsElsaDbContext":
                _require(row["knownIds"] == CONTRACT["migrationRules"]["connectionsIds"], "connections migration identities")
            if shared_history is not None:
                _require(row["historyIds"] == shared_history, "shared migration history mismatch")
            shared_history = row["historyIds"]
            shared_known.update(row["knownIds"])
    _require(seen == set(contexts) and shared_known <= set(shared_history or []), "migration context coverage")


def validate_cell(data: Any, *, expected_head: str, version: str, tfm: str, feature: str,
                  process_id: int, database_hashes: dict[str, str]) -> dict:
    """Strict report validation with externally observed child/database identities."""
    _require(_match(expected_head, HEAD) and _match(version, VERSION)
             and tfm in FRAMEWORKS and feature in FEATURES and type(process_id) is int and process_id > 0, "expected identity")
    _keys(database_hashes, SCENARIOS, "expected databases")
    _require(all(_match(value, SHA256) for value in database_hashes.values())
             and len(set(database_hashes.values())) == 2, "expected database identities")
    _keys(data, CONTRACT["rootKeys"], "root shape")
    _require(type(data["schemaVersion"]) is int and data["schemaVersion"] == 1
             and data["sourceRevision"] == expected_head and data["candidateVersion"] == version
             and data["tfm"] == tfm and data["feature"] == feature, "cell identity")
    major = tfm[3:-2]
    _require(data["targetFramework"] == f".NETCoreApp,Version=v{major}.0"
             and type(data["frameworkDescription"]) is str
             and re.fullmatch(r"\.NET " + major + r"\.0\.[0-9]+", data["frameworkDescription"]) is not None, "runtime framework")
    _keys(data["process"], CONTRACT["processKeys"], "process shape")
    _require(type(data["process"]["pid"]) is int and data["process"]["pid"] == process_id
             and _match(data["process"]["startIdentitySha256"], SHA256), "process identity")
    _assemblies(data["loadedAssemblies"])
    scenarios = data["scenarios"]
    _require(type(scenarios) is list and len(scenarios) == 2, "scenario inventory")
    seen: set[str] = set()
    for scenario in scenarios:
        _keys(scenario, CONTRACT["scenarioKeys"], "scenario shape")
        name = scenario["scenario"]
        _require(type(name) is str and name in SCENARIOS and name not in seen, "scenario identity")
        seen.add(name)
        _require(scenario["databaseIdentitySha256"] == database_hashes[name]
                 and type(scenario["serverVersion"]) is int and 160000 <= scenario["serverVersion"] <= 169999, "scenario database/server")
        expected = CONTRACT["scenarios"][name]
        for key in ("assertions", "counters", "cleanup"):
            _exact_values(scenario[key], expected[key], "scenario " + key)
        _migrations(scenario["migrations"], expected["contexts"])
    _require(seen == set(SCENARIOS), "scenario coverage")
    return data


def validate_matrix(reports: Any) -> list[dict]:
    """Check complete six-cell/twelve-scenario coverage; external binding is per-cell."""
    _require(type(reports) is list and len(reports) == 6, "matrix inventory")
    cells: set[tuple[str, str]] = set()
    databases: set[str] = set()
    identity = None
    for report in reports:
        _keys(report, CONTRACT["rootKeys"], "matrix cell shape")
        _keys(report["process"], CONTRACT["processKeys"], "matrix process shape")
        scenarios = report["scenarios"]
        _require(type(scenarios) is list and len(scenarios) == 2, "matrix scenarios")
        hashes = {}
        for scenario in scenarios:
            _keys(scenario, CONTRACT["scenarioKeys"], "matrix scenario shape")
            _require(type(scenario["scenario"]) is str and scenario["scenario"] in SCENARIOS
                     and scenario["scenario"] not in hashes, "matrix scenario identity")
            hashes[scenario["scenario"]] = scenario["databaseIdentitySha256"]
        validate_cell(report, expected_head=report["sourceRevision"], version=report["candidateVersion"],
                      tfm=report["tfm"], feature=report["feature"], process_id=report["process"]["pid"], database_hashes=hashes)
        cell = (report["tfm"], report["feature"])
        _require(cell not in cells, "duplicate matrix cell")
        cells.add(cell)
        current_identity = (report["sourceRevision"], report["candidateVersion"])
        _require(identity is None or identity == current_identity, "mixed matrix source/version")
        identity = current_identity
        for digest in hashes.values():
            _require(digest not in databases, "reused matrix database")
            databases.add(digest)
    _require(cells == {(tfm, feature) for tfm in FRAMEWORKS for feature in FEATURES}
             and len(databases) == 12, "matrix coverage")
    return reports
