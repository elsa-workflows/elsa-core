"""Focused sanitized proof contracts for native JSON roundtrip and DOM interop."""
from __future__ import annotations

import copy
import hashlib
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import run_paired_package_browser_matrix as matrix


def sha(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def assertion(record: dict, name: str, passed: bool) -> None:
    item = next(item for item in record["assertions"] if item["name"] == name)
    item["passed"] = passed
    item["reason_category"] = None if passed else "not_implemented"


def json_proof(*, execution_complete: bool = False) -> dict:
    semantic = sha("candidate semantic graph")
    document = sha("bounded exported JSON")
    expected = sha("candidate output")
    checks = {
        "exported": True,
        "import_chooser_observed": True,
        "imported": True,
        "saved": True,
        "reloaded": True,
        "semantic_preserved": True,
        "published": execution_complete,
        "terminal": execution_complete,
        "output": execution_complete,
        "studio_terminal": execution_complete,
    }
    result = {
        "definition_id_sha256": sha("candidate definition"),
        "root_id_sha256": sha("candidate root"),
        "activity_id_sha256": sha("candidate activity"),
        "expected_value_sha256": expected,
        "source_semantic_sha256": semantic,
        "export_document_sha256": document,
        "export_document_bytes": 512,
        "export_semantic_sha256": semantic,
        "uploaded_document_sha256": document,
        "imported_document_sha256": document,
        "imported_semantic_sha256": semantic,
        "saved_semantic_sha256": semantic,
        "reloaded_semantic_sha256": semantic,
        "checks": checks,
    }
    if execution_complete:
        result.update(instance_id_sha256=sha("candidate instance"), actual_output_sha256=expected)
    return result


def dom_proof(json_value: dict) -> dict:
    return {
        "definition_id_sha256": json_value["definition_id_sha256"],
        "root_id_sha256": json_value["root_id_sha256"],
        "activity_id_sha256": json_value["activity_id_sha256"],
        "expected_value_sha256": json_value["expected_value_sha256"],
        "export_document_sha256": json_value["export_document_sha256"],
        "uploaded_document_sha256": json_value["uploaded_document_sha256"],
        "checks": {
            "import_menu_clicked": True,
            "filechooser_observed": True,
            "import_succeeded": True,
            "save_callback_observed": True,
        },
    }


def clear_import_semantics(json_value: dict) -> None:
    json_value["checks"].update(imported=False, saved=False, reloaded=False, semantic_preserved=False)
    for field in ("imported_semantic_sha256", "saved_semantic_sha256", "reloaded_semantic_sha256"):
        json_value.pop(field)


def clear_saved_semantics(json_value: dict) -> None:
    json_value["checks"].update(saved=False, reloaded=False, semantic_preserved=False)
    for field in ("saved_semantic_sha256", "reloaded_semantic_sha256"):
        json_value.pop(field)


def receipt(host: str = "server") -> dict:
    record = {
        "version": "3.10.0",
        "framework": "net10.0",
        "host": host,
        "result": "incomplete",
        "browser_version": "149.0.7827.55",
        "resources": [],
        "failure_category": None,
        "proof": {},
    }
    record["assertions"] = [
        {"name": name, "passed": False, "reason_category": "not_implemented"}
        for name in sorted(matrix.required_assertions(record))
    ]
    return record


class NativeInteropProofContracts(unittest.TestCase):
    def test_json_roundtrip_pass_requires_native_bytes_semantics_and_run_binding(self):
        record = receipt()
        value = json_proof(execution_complete=True)
        record["proof"].update(
            definition_id_sha256=value["definition_id_sha256"],
            root_id_sha256=value["root_id_sha256"],
            activity_id_sha256=value["activity_id_sha256"],
            value_sha256=value["expected_value_sha256"],
            instance_id_sha256=value["instance_id_sha256"],
            json_roundtrip=value,
        )
        assertion(record, "json_roundtrip", True)
        self.assertEqual(record, matrix.validate_browser_receipt(record, matrix.identity(record)))

    def test_json_roundtrip_rejects_absent_proof_and_incomplete_stages(self):
        record = receipt()
        assertion(record, "json_roundtrip", True)
        with self.assertRaisesRegex(ValueError, "Missing native JSON proof"):
            matrix.validate_browser_receipt(record, matrix.identity(record))

        record = receipt()
        value = json_proof(execution_complete=True)
        record["proof"].update(
            definition_id_sha256=value["definition_id_sha256"], root_id_sha256=value["root_id_sha256"],
            activity_id_sha256=value["activity_id_sha256"], value_sha256=value["expected_value_sha256"],
            instance_id_sha256=value["instance_id_sha256"], json_roundtrip=value,
        )
        assertion(record, "json_roundtrip", True)
        value["checks"]["import_chooser_observed"] = False
        with self.assertRaises(ValueError):
            matrix.validate_browser_receipt(record, matrix.identity(record))

    def test_json_roundtrip_rejects_semantic_or_identity_changes_and_raw_payloads(self):
        base = receipt()
        value = json_proof(execution_complete=True)
        base["proof"].update(
            definition_id_sha256=value["definition_id_sha256"], root_id_sha256=value["root_id_sha256"],
            activity_id_sha256=value["activity_id_sha256"], value_sha256=value["expected_value_sha256"],
            instance_id_sha256=value["instance_id_sha256"], json_roundtrip=value,
        )
        assertion(base, "json_roundtrip", True)
        mutations = [
            lambda r: r["proof"]["json_roundtrip"].update(raw_json="private workflow document"),
            lambda r: r["proof"]["json_roundtrip"].update(export_document_bytes=True),
            lambda r: r["proof"]["json_roundtrip"].update(export_document_bytes=1024 * 1024 + 1),
            lambda r: r["proof"]["json_roundtrip"].update(uploaded_document_sha256=sha("other bytes")),
            lambda r: r["proof"]["json_roundtrip"].update(reloaded_semantic_sha256=sha("changed semantics")),
            lambda r: r["proof"]["json_roundtrip"].update(expected_value_sha256=sha("other value")),
            lambda r: r["proof"]["json_roundtrip"].update(actual_output_sha256=sha("other output")),
            lambda r: r["proof"].update(root_id_sha256=sha("other root")),
            lambda r: r["proof"].update(instance_id_sha256=sha("other run")),
        ]
        for mutate in mutations:
            changed = copy.deepcopy(base)
            mutate(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                matrix.validate_browser_receipt(changed, matrix.identity(changed))

    def test_dom_interop_requires_observed_native_chooser_import_and_callback(self):
        record = receipt()
        json_value = json_proof()
        record["proof"].update(
            definition_id_sha256=json_value["definition_id_sha256"],
            root_id_sha256=json_value["root_id_sha256"],
            activity_id_sha256=json_value["activity_id_sha256"],
            value_sha256=json_value["expected_value_sha256"],
            json_roundtrip=json_value,
            dom_interop=dom_proof(json_value),
        )
        assertion(record, "dom_interop", True)
        self.assertEqual(record, matrix.validate_browser_receipt(record, matrix.identity(record)))

    def test_dom_interop_rejects_missing_chooser_failed_import_unbound_ids_and_raw_payloads(self):
        base = receipt()
        json_value = json_proof()
        base["proof"].update(
            definition_id_sha256=json_value["definition_id_sha256"],
            root_id_sha256=json_value["root_id_sha256"],
            activity_id_sha256=json_value["activity_id_sha256"],
            value_sha256=json_value["expected_value_sha256"],
            json_roundtrip=json_value,
            dom_interop=dom_proof(json_value),
        )
        assertion(base, "dom_interop", True)
        mutations = [
            lambda r: r["proof"]["dom_interop"]["checks"].update(filechooser_observed=False),
            lambda r: r["proof"]["dom_interop"]["checks"].update(import_succeeded=False),
            lambda r: r["proof"]["dom_interop"]["checks"].update(save_callback_observed=False),
            lambda r: r["proof"]["dom_interop"].update(uploaded_document_sha256=sha("different upload")),
            lambda r: r["proof"]["dom_interop"].update(definition_id_sha256=sha("different definition")),
            lambda r: r["proof"]["dom_interop"].update(raw_value="private"),
            lambda r: r["proof"].pop("json_roundtrip"),
            lambda r: clear_import_semantics(r["proof"]["json_roundtrip"]),
            lambda r: clear_saved_semantics(r["proof"]["json_roundtrip"]),
        ]
        for mutate in mutations:
            changed = copy.deepcopy(base)
            mutate(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                matrix.validate_browser_receipt(changed, matrix.identity(changed))

    def test_native_json_proofs_cannot_be_attached_to_custom_elements_cell(self):
        record = receipt("custom-elements")
        value = json_proof()
        record["proof"].update(
            definition_id_sha256=value["definition_id_sha256"], root_id_sha256=value["root_id_sha256"],
            activity_id_sha256=value["activity_id_sha256"], value_sha256=value["expected_value_sha256"],
            json_roundtrip=value,
        )
        with self.assertRaisesRegex(ValueError, "Unexpected native JSON roundtrip"):
            matrix.validate_browser_receipt(record, matrix.identity(record))


if __name__ == "__main__":
    unittest.main()
