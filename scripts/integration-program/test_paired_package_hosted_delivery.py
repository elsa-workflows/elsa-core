"""Synthetic binding and tamper contracts; never native acceptance evidence."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import materialize_paired_package_hosts as hosts
import paired_package_hosted_delivery as hosted
import paired_package_react_phase as phases
from test_paired_package_wasm_boot import boot_receipt_fixture


def fixture(framework="net10.0", version="3.10.0"):
    proof, expected, observed = boot_receipt_fixture(framework)
    request = hosts.CellRequest("hosted-wasm", framework, version)
    record = {"schema": 1, "host": request.host, "framework": framework, "version": version,
              "phase": "hosted-delivery", "result": "passed", "reason_category": None,
              "browser_version": "149.0.7827.55", "cleanup_verified": True, "deliveries": []}
    for prefix in ("", "compat"):
        base = "/compat/" if prefix else "/"
        rows = copy.deepcopy(observed)
        # Mixed root/alias resources model the Hosted loader's possible routing.
        if prefix:
            rows[0]["path"] = "/compat" + rows[0]["path"]
        record["deliveries"].append({"route_prefix": prefix, "entry_path": base + "login",
            "document": {"path": base + "login", "status": 200, "content_type": "text/html",
                         "base_href_sha256": hashlib.sha256(base.encode()).hexdigest()},
            "resources": rows, "boot": copy.deepcopy(proof), "interactive_validation_observed": True,
            "checks": dict.fromkeys(hosted.CHECKS, True), "result": "passed"})
    return request, expected, record


class HostedDeliveryContracts(unittest.TestCase):
    def setUp(self):
        self.request, self.expected, self.record = fixture()

    def validate(self, record=None):
        return hosted.validate_receipt(self.record if record is None else record, self.request, self.expected)

    def test_both_routes_all_versions_frameworks_bind_original_resources(self):
        for framework in ("net8.0", "net9.0", "net10.0"):
            for version in ("3.8.4", "3.9.0", "3.10.0"):
                with self.subTest(framework=framework, version=version):
                    request, assets, record = fixture(framework, version)
                    before = copy.deepcopy(record)
                    self.assertEqual(record, hosted.validate_receipt(record, request, assets))
                    self.assertEqual(before, record)
                    summary = hosted.summarize(record)
                    hosted.validate_summary(summary, summary["checks"], (version, framework, "hosted-wasm"))

    def test_missing_route_relabeling_unsafe_fields_and_claim_mutations_reject(self):
        from dataclasses import asdict
        for prefix in (None, False, 0, "compat"):
            with self.subTest(prefix=prefix), self.assertRaises(ValueError):
                hosted.validate_receipt(self.record, dict(asdict(self.request), route_prefix=prefix), self.expected)
        mutations = [lambda r: r["deliveries"].pop(), lambda r: r["deliveries"].reverse(),
            lambda r: r.update(host="wasm"), lambda r: r.update(schema=True),
            lambda r: r.update(raw_error="private"), lambda r: r.update(browser_version="private"),
            lambda r: r["deliveries"][0]["document"].update(status=True),
            lambda r: r["deliveries"][0]["document"].update(path="/compat/login"),
            lambda r: r["deliveries"][1]["document"].update(base_href_sha256="a" * 64),
            lambda r: r["deliveries"][0].update(interactive_validation_observed=False),
            lambda r: r["deliveries"][0]["checks"].update(observations=False),
            lambda r: r["deliveries"][0]["boot"].update(configuration_sha256="b" * 64),
            lambda r: r.update(cleanup_verified=False)]
        for mutate in mutations:
            changed = copy.deepcopy(self.record); mutate(changed)
            with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                self.validate(changed)

    def test_every_observed_alias_requires_original_bytes_mime_and_exact_path(self):
        mutations = [{"path": "/tenant/_framework/blazor.webassembly.js"},
            {"path": "/compat/../_framework/blazor.webassembly.js"}, {"sha256": "b" * 64},
            {"bytes": True}, {"status": 304}, {"status": True}, {"content_type": "text/html"},
            {"owner": "fixture"}, {"requested": False}, {"raw_body": "private"}]
        for mutation in mutations:
            changed = copy.deepcopy(self.record)
            changed["deliveries"][1]["resources"][0].update(mutation)
            with self.subTest(mutation=mutation), self.assertRaises(ValueError):
                self.validate(changed)
        changed = copy.deepcopy(self.record)
        changed["deliveries"][0]["resources"][0]["path"] = "/compat" + changed["deliveries"][0]["resources"][0]["path"]
        with self.assertRaises(ValueError):
            self.validate(changed)

    def test_identical_root_alias_pair_is_accepted_but_duplicate_actual_path_rejects(self):
        changed = copy.deepcopy(self.record)
        first = changed["deliveries"][1]["resources"][0]
        changed["deliveries"][1]["resources"].append(dict(first, path=first["path"].removeprefix("/compat")))
        self.validate(changed)
        changed["deliveries"][1]["resources"].append(dict(first))
        with self.assertRaises(ValueError):
            self.validate(changed)

    def test_failed_base_is_retainable_without_certifying_delivery(self):
        row = self.record["deliveries"][1]
        row["document"]["base_href_sha256"] = hashlib.sha256(b"/").hexdigest()
        row["checks"]["base"] = False
        row["result"] = "failed"
        self.record.update(result="failed", reason_category="delivery_failed")
        self.validate()
        self.assertFalse(hosted.summarize(self.record)["checks"]["prefixed_delivery"])

    def test_missing_boot_and_stopped_second_attempt_remain_failed(self):
        row = self.record["deliveries"][1]
        row.update(document={"path": None, "status": None, "content_type": None, "base_href_sha256": None},
                   resources=[], boot=None, interactive_validation_observed=False,
                   checks=dict.fromkeys(hosted.CHECKS, False), result="failed")
        self.record.update(result="failed", reason_category="delivery_failed", cleanup_verified=False)
        self.validate()
        self.assertFalse(any(hosted.summarize(self.record)["checks"].values()))

    def test_host_assertions_require_independent_phase_and_cannot_appear_on_other_hosts(self):
        from test_paired_package_react_phase import phase_fixture
        _, original, _, _ = phase_fixture("hosted-wasm")
        key = (original["version"], original["framework"], original["host"])
        for name in hosted.ASSERTIONS:
            record = copy.deepcopy(original)
            next(row for row in record["assertions"] if row["name"] == name)["passed"] = True
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, "Missing independent Hosted"):
                hosted.browser.validate_browser_receipt(record, key)
        summary = hosted.summarize(self.record)
        for host in ("server", "wasm", "custom-elements"):
            with self.subTest(host=host), self.assertRaises(ValueError):
                hosted.validate_summary(summary, summary["checks"], (key[0], key[1], host))

    def test_retention_binds_exact_child_bytes_execution_and_matrix_claim(self):
        from verify_paired_browser_retention import verify_retained_inventory
        key = (self.request.version, self.request.framework, self.request.host)
        summary = hosted.summarize(self.record)
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            cell = root / "cells" / "-".join(key); cell.mkdir(parents=True)
            (root / "matrix.json").write_text(json.dumps({"cells": []}))
            path = cell / "hosted-delivery.json"
            path.write_text(json.dumps(self.record, sort_keys=True, indent=2) + "\n")
            evidence = dict(zip(("version", "framework", "host"), key), result="failed", stage="browser_execution",
                            resource_inventory={"assets": self.expected}, hosted_delivery=summary)
            (cell / "execution.json").write_text(json.dumps(evidence))
            self.assertEqual(3, len(verify_retained_inventory(root)))
            from verify_paired_browser_retention import _verify_hosted_delivery
            combined = dict(zip(("version", "framework", "host"), key), proof={"hosted_delivery": summary},
                            assertions=[{"name": name, "passed": passed} for name, passed in summary["checks"].items()])
            _verify_hosted_delivery(root, key, evidence, {"cells": [combined]})
            changed = copy.deepcopy(combined)
            changed["proof"]["hosted_delivery"]["phase_receipt_sha256"] = "f" * 64
            with self.assertRaisesRegex(ValueError, "matrix claim differs"):
                _verify_hosted_delivery(root, key, evidence, {"cells": [changed]})
            path.write_text(path.read_text() + " ")
            with self.assertRaisesRegex(ValueError, "bytes"):
                verify_retained_inventory(root)

    def test_transport_uses_private_input_and_rejects_exit_or_duplicate_json(self):
        handle = SimpleNamespace(request=self.request, studio_url="http://127.0.0.1:1", backend_url="http://127.0.0.1:2/elsa/api",
                                 username="PRIVATE", password="PRIVATE", safe_ids={})
        completed = SimpleNamespace(stdout=json.dumps(self.record), returncode=0)
        with patch.object(hosted.browser, "_run_browser_process", return_value=completed) as run:
            self.assertEqual(self.record, hosted.run_delivery(handle, self.request, self.expected))
            self.assertNotIn("PRIVATE", str(run.call_args.args))
            self.assertEqual("hosted-delivery", json.loads(run.call_args.kwargs["input"])["phase"])
            completed.returncode = 1
            with self.assertRaisesRegex(ValueError, "exit"):
                hosted.run_delivery(handle, self.request, self.expected)
            completed.stdout = '{"schema":1,"schema":1}'
            with self.assertRaises(ValueError):
                hosted.run_delivery(handle, self.request, self.expected)
