import copy
import gzip
import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPTS = Path(__file__).resolve().parent
REPOSITORY = SCRIPTS.parents[1]
sys.path.insert(0, str(SCRIPTS))

from github_activity_id_migration import (  # noqa: E402
    LEGACY_TYPES,
    MigrationError,
    encode_json,
    migrate_document,
    parse_json,
)


FIXTURE_PATH = SCRIPTS / "github-activity-id-compatibility/fixtures/released-3.8.4-v1.json"
WORKFLOW_KEY = "github-id-migration-fixture"
ACTIVITY_IDS = {
    "/Root/activities/0": "activity-get-comment",
    "/Root/activities/1": "activity-delete-comment",
    "/Root/activities/2": "activity-update-comment",
    "/Root/activities/3": "activity-get-gist",
}


class GitHubActivityIdMigrationTests(unittest.TestCase):
    def setUp(self):
        self.fixture = json.loads(FIXTURE_PATH.read_text(encoding="utf-8"))
        self.workflow = self.fixture["workflow"]
        self.mapping = mapping_for_document(self.workflow)

    def test_fixture_activities_match_the_pinned_released_serialization_receipt(self):
        source = self.fixture["source"]
        receipt_path = REPOSITORY / source["receipt"]
        receipt_bytes = receipt_path.read_bytes()
        self.assertEqual(source["receipt_sha256"], hashlib.sha256(receipt_bytes).hexdigest())
        receipt = json.loads(gzip.decompress(receipt_bytes))
        expected = {}
        for round_trip in receipt["roundTrips"]:
            activity = json.loads(round_trip["Serialized"])
            if activity.get("type") in LEGACY_TYPES:
                expected[activity["type"]] = activity

        actual = {activity["type"]: activity for activity in self.workflow["Root"]["activities"]}
        self.assertEqual(set(LEGACY_TYPES), set(expected))
        self.assertEqual(expected, actual)

    def test_released_payloads_migrate_at_root_and_nested_locations(self):
        original = copy.deepcopy(self.workflow)
        migrated = migrate_document(self.workflow, self.mapping, WORKFLOW_KEY, self.mapping["workflow_sha256"])

        self.assertEqual(original, self.workflow)
        self.assertEqual("workflow-8325-fixture", migrated["Id"])
        self.assertEqual("preexisting-sequence-id", migrated["Root"]["id"])
        for pointer, activity_id in ACTIVITY_IDS.items():
            node = resolve_pointer(migrated, pointer)
            original_node = resolve_pointer(original, pointer)
            provider_name, _ = LEGACY_TYPES[node["type"]]
            self.assertEqual(activity_id, node["id"])
            self.assertEqual(2, node["version"])
            self.assertEqual(original_node["id"], node[provider_name])
            self.assertNotIn("id", node[provider_name])

        self.assertEqual(
            original["CustomProperties"]["unrelated"],
            migrated["CustomProperties"]["unrelated"],
        )

    def test_migration_does_not_rewrite_version_two_nodes(self):
        workflow = {"root": {"type": "Elsa.GitHub.Comments.GetComment", "version": 2, "id": "stable"}}
        mapping = mapping_for_document(workflow, {})
        migrated = migrate_document(workflow, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])
        self.assertEqual(workflow, migrated)

    def test_both_root_casings_are_not_accepted_together(self):
        workflow = {
            "root": {"type": "Elsa.Sequence", "activities": []},
            "Root": {"type": "Elsa.Sequence", "activities": []},
        }
        mapping = mapping_for_document(workflow, {})
        with self.assertRaisesRegex(MigrationError, "root activity"):
            migrate_document(workflow, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])

    def test_every_legacy_node_requires_exactly_one_mapping(self):
        missing = copy.deepcopy(self.mapping)
        del missing["activity_ids"]["/Root/activities/3"]
        with self.assertRaisesRegex(MigrationError, "Missing explicit activity IDs"):
            migrate_document(self.workflow, missing, WORKFLOW_KEY, missing["workflow_sha256"])

        unused = copy.deepcopy(self.mapping)
        unused["activity_ids"]["/Root/activities/99"] = "unused"
        with self.assertRaisesRegex(MigrationError, "Unused activity ID mappings"):
            migrate_document(self.workflow, unused, WORKFLOW_KEY, unused["workflow_sha256"])

    def test_mapping_is_bound_to_the_supplied_workflow_key(self):
        self.mapping["workflow_key"] = "another-workflow"
        with self.assertRaisesRegex(MigrationError, "does not match"):
            migrate_document(self.workflow, self.mapping, WORKFLOW_KEY, self.mapping["workflow_sha256"])

    def test_mapping_is_bound_to_the_exact_source_bytes(self):
        with self.assertRaisesRegex(MigrationError, "does not match the exact input"):
            migrate_document(self.workflow, self.mapping, WORKFLOW_KEY, "0" * 64)

    def test_duplicate_new_ids_and_existing_activity_id_collisions_are_rejected(self):
        duplicate = copy.deepcopy(self.mapping)
        duplicate["activity_ids"]["/Root/activities/1"] = "activity-get-comment"
        with self.assertRaisesRegex(MigrationError, "must be unique"):
            migrate_document(self.workflow, duplicate, WORKFLOW_KEY, duplicate["workflow_sha256"])

        collision = copy.deepcopy(self.mapping)
        collision["activity_ids"]["/Root/activities/0"] = "preexisting-sequence-id"
        with self.assertRaisesRegex(MigrationError, "collide with existing"):
            migrate_document(self.workflow, collision, WORKFLOW_KEY, collision["workflow_sha256"])

    def test_invalid_version_or_provider_input_is_rejected(self):
        for mutate, expected in (
            (lambda node: node.__setitem__("version", 3), "Unsupported"),
            (lambda node: node.pop("id"), "missing or ambiguous"),
            (lambda node: node.__setitem__("id", "provider-id"), "missing or ambiguous"),
            (lambda node: node["id"].__setitem__("typeName", "String"), "wrapper is invalid"),
            (lambda node: node.__setitem__("commentId", {"typeName": "Int32"}), "already exists"),
        ):
            with self.subTest(expected=expected):
                document = copy.deepcopy(self.workflow)
                mutate(document["Root"]["activities"][0])
                mapping = mapping_for_document(document)
                with self.assertRaisesRegex(MigrationError, expected):
                    migrate_document(document, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])

    def test_unrelated_object_valued_id_fields_are_untouched(self):
        document = {
            "root": {
                "id": "unrelated-activity",
                "type": "Other.Activity",
                "version": 1,
                "customProperties": {
                    "businessData": {
                        "type": "Elsa.GitHub.Comments.GetComment",
                        "id": {"typeName": "Int32", "expression": {"type": "Literal", "value": 19}},
                    }
                },
            },
        }
        mapping = mapping_for_document(document, {})
        migrated = migrate_document(document, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])
        self.assertEqual(document, migrated)

    def test_unsupported_flowchart_topology_is_rejected(self):
        document = {
            "root": {
                "id": "flowchart-id",
                "type": "Elsa.Flowchart",
                "version": 1,
                "nodes": [{"type": "Elsa.GitHub.Comments.GetComment", "version": 1}],
                "connections": [{"source": "node-a", "target": "node-b"}],
            }
        }
        mapping = mapping_for_document(document, {})
        with self.assertRaisesRegex(MigrationError, "only a single root activity and Elsa.Sequence children"):
            migrate_document(document, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])

    def test_for_and_state_machine_nested_activities_fail_before_partial_migration(self):
        nested = copy.deepcopy(self.workflow["Root"]["activities"][0])
        containers = (
            {"type": "Elsa.For", "id": "loop", "body": nested},
            {"type": "Elsa.StateMachine", "id": "state-machine", "states": [{"name": "active", "entry": nested}]},
        )
        for container in containers:
            with self.subTest(container=container["type"]):
                document = copy.deepcopy(self.workflow)
                document["Root"]["activities"].append(container)
                original = copy.deepcopy(document)
                mapping = mapping_for_document(document)
                with self.assertRaisesRegex(MigrationError, "Unsupported workflow container"):
                    migrate_document(document, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])
                self.assertEqual(original, document)
                with tempfile.TemporaryDirectory() as directory:
                    root = Path(directory)
                    source, mapping_path, output = (root / name for name in ("workflow.json", "mapping.json", "output.json"))
                    source.write_text(encode_json(document), encoding="utf-8")
                    mapping_path.write_text(json.dumps(mapping), encoding="utf-8")
                    result = subprocess.run(
                        [sys.executable, str(SCRIPTS / "github_activity_id_migration.py"),
                         "--input", str(source), "--mapping", str(mapping_path),
                         "--workflow-key", WORKFLOW_KEY, "--output", str(output)],
                        check=False, capture_output=True, text=True,
                    )
                    self.assertEqual(2, result.returncode, result.stderr)
                    self.assertFalse(output.exists())

    def test_unrelated_custom_leaf_with_container_short_name_is_preserved(self):
        for type_name in ("Acme.For", "Acme.StateMachine", "Elsa.Custom.For"):
            with self.subTest(type_name=type_name):
                document = copy.deepcopy(self.workflow)
                leaf = {"type": type_name, "id": "custom-leaf", "version": 1}
                document["Root"]["activities"].append(leaf)
                mapping = mapping_for_document(document)
                migrated = migrate_document(document, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])
                self.assertEqual(leaf, migrated["Root"]["activities"][-1])
                self.assertEqual("activity-get-comment", migrated["Root"]["activities"][0]["id"])

    def test_custom_container_cannot_hide_unmigrated_legacy_activity(self):
        document = copy.deepcopy(self.workflow)
        document["Root"]["activities"].append({
            "type": "Acme.For", "id": "custom-container",
            "body": copy.deepcopy(self.workflow["Root"]["activities"][0]),
        })
        original = copy.deepcopy(document)
        mapping = mapping_for_document(document)
        with self.assertRaisesRegex(MigrationError, "Unsupported nested legacy activity"):
            migrate_document(document, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])
        self.assertEqual(original, document)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, mapping_path, output = (root / name for name in ("workflow.json", "mapping.json", "output.json"))
            source.write_text(encode_json(document), encoding="utf-8")
            mapping_path.write_text(json.dumps(mapping), encoding="utf-8")
            result = subprocess.run(
                [sys.executable, str(SCRIPTS / "github_activity_id_migration.py"),
                 "--input", str(source), "--mapping", str(mapping_path),
                 "--workflow-key", WORKFLOW_KEY, "--output", str(output)],
                check=False, capture_output=True, text=True,
            )
            self.assertEqual(2, result.returncode, result.stderr)
            self.assertFalse(output.exists())

    def test_unsupported_references_on_workflow_or_sequence_are_rejected(self):
        workflows = (
            {"root": {"type": "Elsa.Sequence", "activities": []}, "connections": []},
            {"Root": {"type": "Elsa.Sequence", "activities": []}, "Connections": []},
            {"root": {"type": "Elsa.Sequence", "activities": [], "connections": []}},
            {"Root": {"type": "Elsa.Sequence", "activities": [], "Nodes": []}},
        )
        for document in workflows:
            with self.subTest(document=document):
                mapping = mapping_for_document(document, {})
                with self.assertRaisesRegex(MigrationError, "Unsupported workflow topology"):
                    migrate_document(document, mapping, WORKFLOW_KEY, mapping["workflow_sha256"])

    def test_decimal_tokens_are_preserved_without_float_rounding(self):
        raw_number = "9007199254740993.123456789012345678901234567890"
        document = parse_json('{"root":{"type":"Elsa.Sequence","activities":[]},"opaque":' + raw_number + "}")
        encoded = encode_json(document)
        self.assertIn('"opaque": ' + raw_number, encoded)
        self.assertEqual(raw_number, parse_numeric_token(encoded, "opaque"))

    def test_duplicate_json_keys_and_nonstandard_numbers_are_rejected(self):
        with self.assertRaisesRegex(MigrationError, "Duplicate JSON object key"):
            parse_json('{"id":"one","id":"two"}')
        with self.assertRaisesRegex(MigrationError, "Non-standard JSON numeric constant"):
            parse_json('{"number":NaN}')

    def test_cli_writes_new_file_atomically_and_preserves_input(self):
        script = SCRIPTS / "github_activity_id_migration.py"
        original = json.dumps(self.workflow, separators=(",", ":"))
        raw_number = "9007199254740993.123456789012345678901234567890"
        source_text = original[:-1] + ',"opaqueNumeric":' + raw_number + "}"

        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "workflow.json"
            mapping_path = root / "mapping.json"
            output = root / "migrated.json"
            source.write_text(source_text, encoding="utf-8")
            self.mapping["workflow_sha256"] = hashlib.sha256(source_text.encode("utf-8")).hexdigest()
            mapping_path.write_text(json.dumps(self.mapping), encoding="utf-8")
            source_before = source.read_bytes()

            result = subprocess.run(
                [
                    sys.executable,
                    str(script),
                    "--input",
                    str(source),
                    "--mapping",
                    str(mapping_path),
                    "--workflow-key",
                    WORKFLOW_KEY,
                    "--output",
                    str(output),
                ],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual(source_before, source.read_bytes())
            self.assertIn('"opaqueNumeric": ' + raw_number, output.read_text(encoding="utf-8"))

            output_before = output.read_bytes()
            refused_output = root / "refused.json"
            result = subprocess.run(
                [
                    sys.executable,
                    str(script),
                    "--input",
                    str(source),
                    "--mapping",
                    str(mapping_path),
                    "--workflow-key",
                    "wrong-workflow",
                    "--output",
                    str(refused_output),
                ],
                check=False,
                capture_output=True,
                text=True,
            )
            self.assertEqual(2, result.returncode)
            self.assertFalse(refused_output.exists())
            self.assertEqual(output_before, output.read_bytes())

    def test_cli_refuses_output_alias_or_existing_output(self):
        script = SCRIPTS / "github_activity_id_migration.py"
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "workflow.json"
            mapping_path = root / "mapping.json"
            source_text = encode_json(self.workflow)
            source.write_text(source_text, encoding="utf-8")
            self.mapping["workflow_sha256"] = hashlib.sha256(source_text.encode("utf-8")).hexdigest()
            mapping_path.write_text(json.dumps(self.mapping), encoding="utf-8")
            command = [
                sys.executable,
                str(script),
                "--input",
                str(source),
                "--mapping",
                str(mapping_path),
                "--workflow-key",
                WORKFLOW_KEY,
                "--output",
            ]

            alias = subprocess.run(command + [str(source)], check=False, capture_output=True, text=True)
            self.assertEqual(2, alias.returncode)
            self.assertIn("Output already exists", alias.stderr)

            output = root / "existing.json"
            output.write_text("keep this file", encoding="utf-8")
            existing = subprocess.run(command + [str(output)], check=False, capture_output=True, text=True)
            self.assertEqual(2, existing.returncode)
            self.assertEqual("keep this file", output.read_text(encoding="utf-8"))


def resolve_pointer(document, pointer):
    value = document
    if not pointer:
        return value
    for part in pointer.split("/")[1:]:
        part = part.replace("~1", "/").replace("~0", "~")
        value = value[int(part)] if isinstance(value, list) else value[part]
    return value


def parse_numeric_token(text, key):
    parsed = parse_json(text)
    return parsed[key].raw


def mapping_for_document(document, activity_ids=None):
    serialized = encode_json(document)
    return {
        "workflow_key": WORKFLOW_KEY,
        "workflow_sha256": hashlib.sha256(serialized.encode("utf-8")).hexdigest(),
        "activity_ids": dict(ACTIVITY_IDS if activity_ids is None else activity_ids),
    }


if __name__ == "__main__":
    unittest.main()
