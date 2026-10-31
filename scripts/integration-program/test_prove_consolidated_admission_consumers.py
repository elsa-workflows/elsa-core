"""Receipt-boundary regressions; no .NET, database, process or network simulation passes as proof."""
from __future__ import annotations

import copy
import hashlib
import unittest

import verify_admission_package_consumers as verify

HEAD = "a" * 40
VERSION = "3.10.0-integration"


def cell(tfm="net8.0", feature="classic"):
    """A structurally complete fixture report, never runtime acceptance evidence."""
    schemas = verify.CONTRACT
    major = tfm[3:-2]
    scenarios = []
    for name in verify.SCENARIOS:
        expected = schemas["scenarios"][name]
        contexts = expected["contexts"]
        known = {
            "SecretsElsaDbContext": ["20260923164247_ManagedSecretOwnership"],
            "ConnectionsElsaDbContext": schemas["migrationRules"]["connectionsIds"],
            "ManagementElsaDbContext": ["20250101000000_InitialManagement"],
            "RuntimeElsaDbContext": ["20250101000000_InitialRuntime"],
            "AdmissionElsaDbContext": schemas["migrationRules"]["admissionIds"],
        }
        shared = sorted(value for context in contexts if context != "AdmissionElsaDbContext" for value in known[context])
        migrations = []
        for context in contexts:
            admission = context == "AdmissionElsaDbContext"
            migrations.append({"context": context, "provider": "Npgsql.EntityFrameworkCore.PostgreSQL", "schema": "Elsa",
                               "historyTable": "__AdmissionMigrationsHistory" if admission else "__EFMigrationsHistory",
                               "knownIds": list(known[context]), "appliedIds": list(known[context]),
                               "historyIds": list(known[context]) if admission else list(shared),
                               "reapplied": True, "populatedPreserved": True})
        scenarios.append({"scenario": name, "databaseIdentitySha256": hashlib.sha256(f"{tfm}-{feature}-{name}".encode()).hexdigest(),
                          "serverVersion": 160013, "assertions": copy.deepcopy(expected["assertions"]),
                          "counters": copy.deepcopy(expected["counters"]), "migrations": migrations,
                          "cleanup": copy.deepcopy(expected["cleanup"])})
    assemblies = [{"name": name, "version": "3.10.0.0",
                   "fullName": name + ", Version=3.10.0.0, Culture=neutral, PublicKeyToken=null",
                   "informationalVersion": VERSION + "+" + HEAD,
                   "location": "/private/fixture/bin/Release/" + tfm + "/" + name + ".dll", "sha256": "b" * 64}
                  for name in ["Elsa", *schemas["requiredAssemblies"]]]
    return {"schemaVersion": 1, "sourceRevision": HEAD, "candidateVersion": VERSION, "tfm": tfm, "feature": feature,
            "targetFramework": f".NETCoreApp,Version=v{major}.0", "frameworkDescription": f".NET {major}.0.8",
            "process": {"pid": 1234, "startIdentitySha256": "c" * 64}, "loadedAssemblies": assemblies, "scenarios": scenarios}


class ReportBoundaryTests(unittest.TestCase):
    def setUp(self):
        self.report = cell()
        self.expected = {"expected_head": HEAD, "version": VERSION, "tfm": "net8.0", "feature": "classic",
                         "process_id": 1234, "database_hashes": {item["scenario"]: item["databaseIdentitySha256"] for item in self.report["scenarios"]}}

    def validate(self, report=None, **overrides):
        return verify.validate_cell(self.report if report is None else report, **(self.expected | overrides))

    def test_complete_report_is_returned_without_mutation(self):
        before = copy.deepcopy(self.report)
        self.assertIs(self.validate(), self.report)
        self.assertEqual(before, self.report)

    def test_every_expected_source_cell_process_and_database_binding_is_enforced(self):
        for key, value in (("expected_head", "d" * 40), ("version", "3.10.1-integration"), ("tfm", "net9.0"),
                           ("feature", "shell"), ("process_id", 5678), ("process_id", True),
                           ("database_hashes", {"admission": "e" * 64, "managed-secret-grant": "f" * 64})):
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.validate(**{key: value})

    def test_private_and_partial_shapes_rejected_at_each_object_boundary(self):
        objects = [self.report, self.report["process"], self.report["loadedAssemblies"][0], self.report["scenarios"][0],
                   self.report["scenarios"][0]["assertions"], self.report["scenarios"][0]["counters"],
                   self.report["scenarios"][0]["cleanup"], self.report["scenarios"][0]["migrations"][0]]
        for obj in objects:
            with self.subTest(keys=list(obj)):
                obj["rawSecret"] = "private-marker-never-echo"
                with self.assertRaises(ValueError) as caught:
                    self.validate()
                self.assertNotIn("private-marker", str(caught.exception))
                del obj["rawSecret"]
                key = next(iter(obj)); value = obj.pop(key)
                with self.assertRaises(ValueError):
                    self.validate()
                obj[key] = value

    def test_no_scenario_can_be_missing_duplicated_unknown_or_skipped(self):
        bad_lists = [[], self.report["scenarios"][:1], [self.report["scenarios"][0]] * 2]
        for rows in bad_lists:
            with self.subTest(rows=len(rows)), self.assertRaises(ValueError):
                self.validate(self.report | {"scenarios": rows})
        self.report["scenarios"][0]["scenario"] = "skipped"
        with self.assertRaises(ValueError): self.validate()

    def test_every_assertion_counter_and_cleanup_value_is_required_and_typed(self):
        for scenario in self.report["scenarios"]:
            for section in ("assertions", "counters", "cleanup"):
                for key, value in list(scenario[section].items()):
                    alternatives = [False, 1] if type(value) is bool else [True, str(value), value + 1, float(value)]
                    for bad in alternatives:
                        scenario[section][key] = bad
                        with self.subTest(section=section, key=key, bad=bad), self.assertRaises(ValueError):
                            self.validate()
                    scenario[section][key] = value

    def test_runtime_metadata_must_match_actual_tfm_and_postgres_major(self):
        for key, value in (("schemaVersion", True), ("targetFramework", ".NETCoreApp,Version=v9.0"),
                           ("frameworkDescription", ".NET 9.0.8"), ("frameworkDescription", ".NET 8.0.8\n")):
            with self.subTest(key=key), self.assertRaises(ValueError): self.validate(self.report | {key: value})
        scenario = self.report["scenarios"][0]
        for bad in (True, "160013", 159999, 170000, 160013.0):
            scenario["serverVersion"] = bad
            with self.subTest(server=bad), self.assertRaises(ValueError): self.validate()

    def test_database_names_cannot_be_reused_or_hashes_malformed(self):
        hashes = self.expected["database_hashes"]
        for bad in ({"admission": hashes["admission"], "managed-secret-grant": hashes["admission"]},
                    hashes | {"extra": "a" * 64}, hashes | {"admission": "A" * 64}):
            with self.assertRaises(ValueError): self.validate(database_hashes=bad)

    def test_loaded_rows_require_all_seven_exact_unique_identities(self):
        rows = self.report["loadedAssemblies"]
        for bad in ([], rows[:-1], rows + [copy.deepcopy(rows[0])]):
            with self.assertRaises(ValueError): self.validate(self.report | {"loadedAssemblies": bad})
        for key, value in (("name", "not-a-library"), ("fullName", "Elsa.Connections"), ("version", True),
                           ("informationalVersion", "raw private text"), ("sha256", "A" * 64),
                           ("location", "https://private.example/secret"), ("location", "/tmp/../Elsa.Connections.dll"),
                           ("location", "/tmp/Elsa.Connections.dll\nprivate-marker")):
            changed = copy.deepcopy(self.report); changed["loadedAssemblies"][0][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.validate(changed)

    def test_process_start_identity_is_a_real_hash_shape(self):
        for value in (None, "", "c" * 63, "C" * 64, "private-marker"):
            with self.assertRaises(ValueError):
                self.validate(self.report | {"process": {"pid": 1234, "startIdentitySha256": value}})

    def test_context_inventory_cannot_repeat_substitute_or_omit_provider(self):
        rows = self.report["scenarios"][0]["migrations"]
        for bad in (rows[:-1], rows + [copy.deepcopy(rows[0])], [rows[0]] + rows[:-1]):
            changed = copy.deepcopy(self.report); changed["scenarios"][0]["migrations"] = bad
            with self.assertRaises(ValueError): self.validate(changed)
        for key, value in (("context", "UnselectedContext"), ("provider", "Microsoft.EntityFrameworkCore.Sqlite"),
                           ("schema", "public"), ("historyTable", "__AdmissionMigrationsHistory"),
                           ("reapplied", False), ("populatedPreserved", 1)):
            changed = copy.deepcopy(self.report); changed["scenarios"][0]["migrations"][0][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.validate(changed)

    def test_migration_history_is_actual_shared_and_complete_not_only_per_context(self):
        rows = self.report["scenarios"][0]["migrations"]
        changed = copy.deepcopy(self.report)
        changed["scenarios"][0]["migrations"][0]["historyIds"] = rows[0]["knownIds"]
        with self.assertRaises(ValueError): self.validate(changed)
        # Even individually valid shared histories must agree on the same raw table.
        changed = copy.deepcopy(self.report)
        changed["scenarios"][0]["migrations"][0]["historyIds"].append("20990101000000_Unexpected")
        with self.assertRaises(ValueError): self.validate(changed)

    def test_exact_connections_and_separate_admission_migrations_cannot_be_fabricated(self):
        for context in ("ConnectionsElsaDbContext", "AdmissionElsaDbContext"):
            changed = copy.deepcopy(self.report)
            row = next(row for row in changed["scenarios"][0]["migrations"] if row["context"] == context)
            row["knownIds"] = row["appliedIds"] = ["20260101000000_Impostor"]
            row["historyIds"] = sorted(row["historyIds"] + row["knownIds"])
            with self.subTest(context=context), self.assertRaises(ValueError): self.validate(changed)
        changed = copy.deepcopy(self.report)
        changed["scenarios"][0]["migrations"][-1]["historyIds"].append("20990101000000_Unselected")
        with self.assertRaises(ValueError): self.validate(changed)

    def test_migration_id_lists_reject_unapplied_duplicate_invalid_or_unsorted_entries(self):
        for key, value in (("appliedIds", []), ("knownIds", ["private-marker"]),
                           ("historyIds", ["20250101000000_Initial", "20250101000000_Initial"]),
                           ("historyIds", ["20990101000000_Last", "20000101000000_First"])):
            changed = copy.deepcopy(self.report); changed["scenarios"][0]["migrations"][0][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.validate(changed)

    def test_normative_contract_rejects_partial_extra_or_nonboolean_success_policy(self):
        for mutate in (lambda data: data.update(proposal="not normative"), lambda data: data.pop("migrationRules"),
                       lambda data: data["scenarios"]["admission"]["assertions"].update(inactiveBootstrapVerified=1),
                       lambda data: data["scenarios"]["admission"].update(contexts=[]),
                       lambda data: data["migrationRules"].update(admissionIds=[])):
            contract = copy.deepcopy(verify.CONTRACT); mutate(contract)
            with self.assertRaises(ValueError): verify._validate_contract(contract)


class MatrixBoundaryTests(unittest.TestCase):
    def setUp(self):
        self.reports = [cell(tfm, feature) for tfm in verify.FRAMEWORKS for feature in verify.FEATURES]

    def test_six_complete_cells_twelve_distinct_scenarios(self):
        self.assertIs(verify.validate_matrix(self.reports), self.reports)

    def test_missing_extra_duplicate_and_failed_cell_rejected(self):
        for bad in ([], self.reports[:-1], self.reports + [self.reports[0]], self.reports[:-1] + [self.reports[0]]):
            with self.assertRaises(ValueError): verify.validate_matrix(bad)
        self.reports[5]["scenarios"][1]["assertions"]["sequentialRestartCompleted"] = False
        with self.assertRaises(ValueError): verify.validate_matrix(self.reports)

    def test_mixed_head_version_or_reused_database_rejected(self):
        for key, value in (("sourceRevision", "d" * 40), ("candidateVersion", "3.10.1-integration")):
            reports = copy.deepcopy(self.reports); reports[-1][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): verify.validate_matrix(reports)
        self.reports[-1]["scenarios"][1]["databaseIdentitySha256"] = self.reports[0]["scenarios"][1]["databaseIdentitySha256"]
        with self.assertRaises(ValueError): verify.validate_matrix(self.reports)


if __name__ == "__main__":
    unittest.main()
