"""Validate one Socket first-vertical report against the tracked fixture contract.

This is not a matrix or full Socket acceptance gate. The caller owns observed process
and database identity, candidate archive/cache/loaded-byte checks, secret scanning of
private output, and physical container cleanup. Assembly paths remain private until
the caller verifies bytes and normalizes them; this module writes nothing.
"""
from __future__ import annotations

import json
from pathlib import Path
import re
from typing import Any

from verify_admission_package_consumers import (
    FEATURES, FRAMEWORKS, HEAD, MIGRATION, SHA256, VERSION, _assemblies, _match, _strings,
)

CASE_ID = "socket-package-held-commit-watch-resume"
ASSERTIONS = """realManagedGenerationEncrypted inactiveBootstrapVerified definiteActivationCaptured
guardedRuntimeSelected physicalHttpOpenAndHello noAckBeforeCommit noEffectsBeforeCommit
physicalAckAfterCommit ackIndependentOfWorkflowDuration realWatchOutputsPersisted
suspensionAndBookmarkPersisted publicOwnedEntriesDeniedWithoutEffects publicBackgroundAndLifecycleDenied
noImplicitOutboundGrant actualStoredGrantPolicyDenied legitimateResumeCompleted
terminalAndBookmarkConsumptionPersisted migrationsReappliedPreserved providerNotCalled
noSecretMarkers physicalPeerDisposed listenerRetiredPhysicalClient""".split()
COUNTERS = dict(zip("""heldAdmissionCommits httpOpens physicalAcknowledgements watchExecutions
suspensions resumes ownedEntryDenials backgroundDenials lifecycleDenials outboundDenials
providerCalls activeExecutionCycles""".split(), (1, 1, 1, 1, 1, 1, 7, 1, 2, 1, 0, 0)))
CONTEXTS = ("SecretsElsaDbContext", "ConnectionsElsaDbContext", "ManagementElsaDbContext",
            "RuntimeElsaDbContext", "AdmissionElsaDbContext", "SlackSocketReceiptElsaDbContext")
REQUIRED_ASSEMBLIES = (
    "Elsa.Slack.SocketMode", "Elsa.Slack", "Elsa.Connections", "Elsa.Connections.Credentials.Workflows",
    "Elsa.Connections.Credentials.Persistence.EFCore", "Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql",
    "Elsa.Workflows.Admission", "Elsa.Workflows.Admission.Persistence.EFCore",
    "Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql",
)
CONNECTION_IDS = ["20260923225653_Initial", "20260924125922_WorkflowCredentialUseGrants",
                  "20260924150000_DueCredentialLifecycleCandidates"]
DISTINCT_HISTORIES = {
    "AdmissionElsaDbContext": ("__AdmissionMigrationsHistory", ["20261008040000_InitialAdmission"]),
    "SlackSocketReceiptElsaDbContext": ("__SlackSocketReceiptsMigrationsHistory", ["20261008110000_InitialSlackSocketReceipts"]),
}


def _require(condition: bool, category: str) -> None:
    if not condition:
        raise ValueError("Socket consumer report rejected: " + category)


def _keys(value: Any, keys: Any, category: str) -> None:
    _require(type(value) is dict and set(value) == set(keys), category)


def _exact_values(actual: Any, expected: dict, category: str) -> None:
    _keys(actual, expected, category)
    _require(all(type(actual[key]) is type(value) and actual[key] == value
                 for key, value in expected.items()), category)


def _validate_contract(contract: Any) -> dict:
    _keys(contract, """schemaVersion caseId cli mandatoryEnvironment syntheticSecretMarkers topLevelKeys
processKeys loadedAssemblyKeys requiredLoadedAssemblies scenarioKeys assertions counters correlationKeys
orderKeys orderConstraint migrationKeys migrationContexts cleanup scope failureMarkers retention""".split(), "contract shape")
    _require(type(contract["schemaVersion"]) is int and contract["schemaVersion"] == 1
             and contract["caseId"] == CASE_ID, "contract identity")
    fields = {
        "topLevelKeys": "schemaVersion sourceRevision candidateVersion tfm feature targetFramework frameworkDescription process loadedAssemblies scenarios",
        "processKeys": "pid startIdentitySha256",
        "loadedAssemblyKeys": "name fullName version informationalVersion location sha256",
        "scenarioKeys": "scenario databaseIdentitySha256 serverVersion assertions counters correlation order migrations cleanup",
        "correlationKeys": "envelopeSha256 eventSha256 admissionSha256 workflowInstanceSha256 bookmarkSha256 bindingSha256",
        "orderKeys": "admissionCommitted acknowledgementReceived",
        "migrationKeys": "context provider schema historyTable knownIds appliedIds historyIds reapplied populatedPreserved",
    }
    for key, expected in fields.items():
        _require(_strings(contract[key]) and set(contract[key]) == set(expected.split()), "contract fields")
    for key, expected in (("migrationContexts", CONTEXTS), ("requiredLoadedAssemblies", REQUIRED_ASSEMBLIES)):
        _require(_strings(contract[key]) and set(contract[key]) == set(expected), "contract inventory")
    _exact_values(contract["assertions"], dict.fromkeys(ASSERTIONS, True), "contract assertions")
    _exact_values(contract["counters"], COUNTERS, "contract counters")
    _exact_values(contract["cleanup"], {"hostsDisposed": True, "hostsCreated": 3,
                                     "maximumConcurrentHosts": 1, "executionHostsObserved": 1}, "contract cleanup")
    _require(_strings(contract["syntheticSecretMarkers"]), "contract secret markers")
    return contract


# Never accept a report-supplied schema or a caller-selected contract path.
CONTRACT = _validate_contract(json.loads(Path(__file__).with_name("socket-package-consumer")
                                         .joinpath("report-contract.json").read_text(encoding="utf-8")))


def _migrations(rows: Any) -> None:
    _require(type(rows) is list and len(rows) == len(CONTEXTS), "migration inventory")
    seen: set[str] = set()
    shared_history = None
    shared_known: set[str] = set()
    for row in rows:
        _keys(row, CONTRACT["migrationKeys"], "migration shape")
        context = row["context"]
        _require(type(context) is str and context in CONTEXTS and context not in seen, "migration context")
        seen.add(context)
        _require(row["provider"] == "Npgsql.EntityFrameworkCore.PostgreSQL" and row["schema"] == "Elsa", "migration provider/schema")
        for key in ("knownIds", "appliedIds", "historyIds"):
            _require(_strings(row[key], pattern=MIGRATION) and row[key] == sorted(row[key]), "migration IDs")
        _require(row["appliedIds"] == row["knownIds"] and set(row["knownIds"]) <= set(row["historyIds"]), "migration application")
        _require(row["reapplied"] is True and row["populatedPreserved"] is True, "migration reapplication")
        if context in DISTINCT_HISTORIES:
            history, known = DISTINCT_HISTORIES[context]
            _require(row["historyTable"] == history and row["knownIds"] == known
                     and row["historyIds"] == known, "distinct migration history")
        else:
            _require(row["historyTable"] == "__EFMigrationsHistory", "shared migration history table")
            if context == "ConnectionsElsaDbContext":
                _require(row["knownIds"] == CONNECTION_IDS, "connections migration identities")
            _require(shared_history is None or row["historyIds"] == shared_history, "shared migration history mismatch")
            shared_history = row["historyIds"]
            shared_known.update(row["knownIds"])
    _require(seen == set(CONTEXTS) and shared_known <= set(shared_history or []), "migration context coverage")


def validate_cell(data: Any, *, expected_head: str, version: str, tfm: str, feature: str,
                  process_id: int, database_hash: str, server_version: int) -> dict:
    """Validate one executed cell; external observations and byte checks remain caller-owned."""
    _require(_match(expected_head, HEAD) and _match(version, VERSION)
             and type(tfm) is str and tfm in FRAMEWORKS and type(feature) is str and feature in FEATURES
             and type(process_id) is int and process_id > 0 and _match(database_hash, SHA256)
             and type(server_version) is int and 160000 <= server_version <= 169999, "expected identity")
    _keys(data, CONTRACT["topLevelKeys"], "root shape")
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
    try:
        _assemblies(data["loadedAssemblies"], REQUIRED_ASSEMBLIES)
    except ValueError:
        raise ValueError("Socket consumer report rejected: loaded assemblies") from None
    scenarios = data["scenarios"]
    _require(type(scenarios) is list and len(scenarios) == 1, "scenario inventory")
    scenario = scenarios[0]
    _keys(scenario, CONTRACT["scenarioKeys"], "scenario shape")
    _require(scenario["scenario"] == CASE_ID and scenario["databaseIdentitySha256"] == database_hash
             and type(scenario["serverVersion"]) is int and scenario["serverVersion"] == server_version, "scenario identity/database/server")
    for key in ("assertions", "counters", "cleanup"):
        _exact_values(scenario[key], CONTRACT[key], "scenario " + key)
    _keys(scenario["correlation"], CONTRACT["correlationKeys"], "correlation shape")
    _require(all(_match(value, SHA256) for value in scenario["correlation"].values()), "correlation hash")
    _keys(scenario["order"], CONTRACT["orderKeys"], "order shape")
    order = scenario["order"]
    _require(all(type(value) is int for value in order.values())
             and 0 < order["admissionCommitted"] < order["acknowledgementReceived"], "commit/ack order")
    _migrations(scenario["migrations"])
    _require(not any(marker in json.dumps(data) for marker in CONTRACT["syntheticSecretMarkers"]), "secret marker")
    return data
