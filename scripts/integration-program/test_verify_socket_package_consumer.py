"""Adversarial report-only guards; these do not execute the Socket consumer."""
from copy import deepcopy
import unittest

import verify_admission_package_consumers as admission
import verify_socket_package_consumer as verifier

HEAD = "a" * 40
VERSION = "3.10.0-proof.123.1"
DATABASE = "b" * 64
SERVER = 160011
EXPECTED = dict(expected_head=HEAD, version=VERSION, tfm="net10.0", feature="classic",
                process_id=1234, database_hash=DATABASE, server_version=SERVER)


def valid_report():
    contract = verifier.CONTRACT
    known = {
        "SecretsElsaDbContext": ["20260531141856_Initial"],
        "ConnectionsElsaDbContext": verifier.CONNECTION_IDS,
        "ManagementElsaDbContext": ["20240101000000_Management"],
        "RuntimeElsaDbContext": ["20240101000001_Runtime"],
    }
    shared = sorted({value for values in known.values() for value in values})
    migrations = []
    for context in verifier.CONTEXTS:
        if context in verifier.DISTINCT_HISTORIES:
            history_table, ids = verifier.DISTINCT_HISTORIES[context]
            history_ids = ids
        else:
            history_table, ids, history_ids = "__EFMigrationsHistory", known[context], shared
        migrations.append(dict(context=context, provider="Npgsql.EntityFrameworkCore.PostgreSQL", schema="Elsa",
                               historyTable=history_table, knownIds=list(ids), appliedIds=list(ids),
                               historyIds=list(history_ids), reapplied=True, populatedPreserved=True))
    assemblies = [dict(name=name, fullName=name + ", Version=3.10.0.0, Culture=neutral, PublicKeyToken=null",
                       version="3.10.0.0", informationalVersion=VERSION,
                       location="/private/consumer/" + name + ".dll", sha256="c" * 64)
                  for name in verifier.REQUIRED_ASSEMBLIES]
    scenario = dict(scenario=verifier.CASE_ID, databaseIdentitySha256=DATABASE, serverVersion=SERVER,
                    assertions=deepcopy(contract["assertions"]), counters=deepcopy(contract["counters"]),
                    correlation={name: "d" * 64 for name in contract["correlationKeys"]},
                    order=dict(admissionCommitted=1, acknowledgementReceived=2), migrations=migrations,
                    cleanup=deepcopy(contract["cleanup"]))
    return dict(schemaVersion=1, sourceRevision=HEAD, candidateVersion=VERSION, tfm="net10.0", feature="classic",
                targetFramework=".NETCoreApp,Version=v10.0", frameworkDescription=".NET 10.0.9",
                process=dict(pid=1234, startIdentitySha256="e" * 64), loadedAssemblies=assemblies, scenarios=[scenario])


def replace(report, path, value):
    current = report
    for key in path[:-1]:
        current = current[key]
    current[path[-1]] = value


class SocketReportTests(unittest.TestCase):
    def reject(self, report, **overrides):
        with self.assertRaisesRegex(ValueError, r"^Socket consumer report rejected: [A-Za-z /-]+$"):
            verifier.validate_cell(report, **(EXPECTED | overrides))

    def test_first_classic_net10_report_returns_private_paths_unchanged(self):
        report = valid_report()
        before = deepcopy(report)
        self.assertIs(verifier.validate_cell(report, **EXPECTED), report)
        self.assertEqual(report, before)
        self.assertEqual(len(report["scenarios"][0]["assertions"]), 22)
        self.assertEqual(len(report["scenarios"][0]["counters"]), 12)
        self.assertEqual(len(report["loadedAssemblies"]), 9)
        self.assertFalse(hasattr(verifier, "validate_matrix"))

    def test_wrong_external_observations_are_rejected(self):
        for key, bad_values in {
            "expected_head": ["f" * 40, "bad", True], "version": ["3.9.0", "not-a-version", True],
            "tfm": ["net8.0", "net11.0", []], "feature": ["shell", "matrix", []],
            "process_id": [1235, 0, -1, True], "database_hash": ["f" * 64, "bad", None],
            "server_version": [160012, 150011, 170000, True],
        }.items():
            for value in bad_values:
                with self.subTest(key=key, value=value):
                    self.reject(valid_report(), **{key: value})

    def test_identity_runtime_process_and_scenario_are_exact(self):
        changes = [
            (("schemaVersion",), True), (("sourceRevision",), "f" * 40), (("candidateVersion",), "3.9.0"),
            (("tfm",), "net9.0"), (("feature",), "shell"), (("targetFramework",), ".NETCoreApp,Version=v9.0"),
            (("frameworkDescription",), ".NET 9.0.9"), (("process", "pid"), True),
            (("process", "pid"), 4321), (("process", "startIdentitySha256"), "invalid"),
            (("scenarios", 0, "scenario"), "complete-socket-matrix"),
            (("scenarios", 0, "databaseIdentitySha256"), "f" * 64),
            (("scenarios", 0, "serverVersion"), True), (("scenarios", 0, "serverVersion"), 160012),
        ]
        for path, value in changes:
            with self.subTest(path=path):
                report = valid_report()
                replace(report, path, value)
                self.reject(report)

    def test_missing_extra_fields_at_every_level_are_rejected(self):
        paths = [(), ("process",), ("loadedAssemblies", 0), ("scenarios", 0),
                 ("scenarios", 0, "assertions"), ("scenarios", 0, "counters"),
                 ("scenarios", 0, "correlation"), ("scenarios", 0, "order"),
                 ("scenarios", 0, "migrations", 0), ("scenarios", 0, "cleanup")]
        for path in paths:
            for extra in (False, True):
                with self.subTest(path=path, extra=extra):
                    report = valid_report()
                    current = report
                    for key in path:
                        current = current[key]
                    if extra:
                        current["untrustedDetail"] = "synthetic-secret-must-not-appear-in-errors"
                    else:
                        del current[next(iter(current))]
                    self.reject(report)

    def test_wrong_container_types_fail_with_fixed_errors(self):
        self.reject(None)
        for path in (("process",), ("loadedAssemblies",), ("loadedAssemblies", 0), ("scenarios",),
                     ("scenarios", 0), ("scenarios", 0, "assertions"), ("scenarios", 0, "counters"),
                     ("scenarios", 0, "correlation"), ("scenarios", 0, "order"),
                     ("scenarios", 0, "migrations"), ("scenarios", 0, "migrations", 0),
                     ("scenarios", 0, "cleanup")):
            with self.subTest(path=path):
                report = valid_report()
                replace(report, path, None)
                self.reject(report)

    def test_every_assertion_counter_and_cleanup_value_is_strict(self):
        for field in ("assertions", "counters", "cleanup"):
            for key, value in verifier.CONTRACT[field].items():
                wrong = [False, 1] if type(value) is bool else [value + 1, bool(value), str(value)]
                for changed in wrong:
                    with self.subTest(field=field, key=key, value=changed):
                        report = valid_report()
                        report["scenarios"][0][field][key] = changed
                        self.reject(report)

    def test_correlation_hashes_are_required_and_lowercase(self):
        for name in verifier.CONTRACT["correlationKeys"]:
            for value in ("D" * 64, "d" * 63, None, True):
                with self.subTest(name=name, value=value):
                    report = valid_report()
                    report["scenarios"][0]["correlation"][name] = value
                    self.reject(report)

    def test_commit_ack_order_is_positive_strict_and_integral(self):
        for commit, ack in ((0, 1), (-1, 2), (2, 2), (3, 2), (True, 2), (1, True), (1.0, 2), (1, "2")):
            with self.subTest(commit=commit, ack=ack):
                report = valid_report()
                report["scenarios"][0]["order"] = dict(admissionCommitted=commit, acknowledgementReceived=ack)
                self.reject(report)

    def test_loaded_assembly_metadata_and_inventory_are_strict(self):
        for field, values in {
            "name": ["External.Provider", "Elsa/Injected"], "fullName": ["Elsa.Slack, Version=9.0.0.0"],
            "version": ["3.10.0", True], "informationalVersion": ["secret invalid version", None],
            "location": ["relative/Elsa.Slack.SocketMode.dll", "/private/../Elsa.Slack.SocketMode.dll",
                         "/private/wrong.dll", "/private/\nElsa.Slack.SocketMode.dll", "C:\\private\\Elsa.Slack.SocketMode.dll"],
            "sha256": ["C" * 64, "c" * 63, None],
        }.items():
            for value in values:
                with self.subTest(field=field, value=value):
                    report = valid_report()
                    report["loadedAssemblies"][0][field] = value
                    self.reject(report)
        for change in (lambda rows: rows.pop(), lambda rows: rows.append(deepcopy(rows[0])), lambda rows: rows.clear()):
            report = valid_report()
            change(report["loadedAssemblies"])
            self.reject(report)

    def test_all_six_migration_contexts_enforce_provider_schema_and_preservation(self):
        for index in range(6):
            for field, value in (("provider", "Microsoft.EntityFrameworkCore.Sqlite"), ("schema", "public"),
                                 ("reapplied", False), ("populatedPreserved", 1), ("knownIds", []),
                                 ("appliedIds", []), ("historyIds", []), ("context", "ForeignContext")):
                with self.subTest(index=index, field=field):
                    report = valid_report()
                    report["scenarios"][0]["migrations"][index][field] = value
                    self.reject(report)

    def test_migration_histories_cannot_collide_or_omit_known_ids(self):
        for index in range(6):
            report = valid_report()
            report["scenarios"][0]["migrations"][index]["historyTable"] = "__AdmissionMigrationsHistory" if index != 4 else "__EFMigrationsHistory"
            self.reject(report)
        for index in (1, 4, 5):
            report = valid_report()
            row = report["scenarios"][0]["migrations"][index]
            row["knownIds"] = row["appliedIds"] = ["20260101000000_Foreign"]
            row["historyIds"] = sorted(row["historyIds"] + row["knownIds"])
            self.reject(report)
        report = valid_report()
        report["scenarios"][0]["migrations"][0]["historyIds"].append("20270101000000_Unexpected")
        self.reject(report)

    def test_duplicate_unsorted_and_missing_migration_rows_are_rejected(self):
        for change in (lambda rows: rows.pop(), lambda rows: rows.__setitem__(0, deepcopy(rows[1]))):
            report = valid_report()
            change(report["scenarios"][0]["migrations"])
            self.reject(report)
        for value in (["20260101000000_One"] * 2, ["20260102000000_Two", "20260101000000_One"], ["invalid"]):
            report = valid_report()
            report["scenarios"][0]["migrations"][0]["historyIds"] = value
            self.reject(report)

    def test_no_partial_or_matrix_report_is_accepted(self):
        for count in (0, 2, 6):
            report = valid_report()
            report["scenarios"] *= count
            self.reject(report)
        self.reject([valid_report()] * 6)

    def test_secret_marker_in_otherwise_valid_private_path_is_rejected_without_echo(self):
        report = valid_report()
        marker = verifier.CONTRACT["syntheticSecretMarkers"][0]
        report["loadedAssemblies"][0]["location"] = "/private/" + marker + "/Elsa.Slack.SocketMode.dll"
        with self.assertRaises(ValueError) as error:
            verifier.validate_cell(report, **EXPECTED)
        self.assertEqual(str(error.exception), "Socket consumer report rejected: secret marker")
        self.assertNotIn(marker, str(error.exception))

    def test_source_contract_cannot_silently_drop_guards(self):
        for field in ("assertions", "counters", "requiredLoadedAssemblies", "migrationContexts", "correlationKeys"):
            contract = deepcopy(verifier.CONTRACT)
            if type(contract[field]) is dict:
                del contract[field][next(iter(contract[field]))]
            else:
                contract[field].pop()
            with self.subTest(field=field), self.assertRaises(ValueError):
                verifier._validate_contract(contract)

    def test_admission_contract_is_unchanged(self):
        before = deepcopy(admission.CONTRACT)
        verifier.validate_cell(valid_report(), **EXPECTED)
        self.assertEqual(admission.CONTRACT, before)
        self.assertEqual(admission.CONTRACT["requiredAssemblies"], list(verifier.REQUIRED_ASSEMBLIES[2:]))
        self.assertEqual(set(admission.CONTRACT["scenarios"]), {"admission", "managed-secret-grant"})


if __name__ == "__main__":
    unittest.main()
