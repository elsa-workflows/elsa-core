"""Synthetic startup/retention contracts; no native hosts or browser are run."""
from contextlib import ExitStack
import copy
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch
from urllib.error import HTTPError, URLError

import materialize_paired_package_hosts as hosts
import paired_package_runtime_diagnostics as diagnostics


CELL = ("3.8.4", "net8.0", "hosted-wasm")


def failed_evidence(component="hosted-wasm", phase="readiness", code="startup_readiness_timeout"):
    return {"version": CELL[0], "framework": CELL[1], "host": CELL[2],
            "stage": "owned_runtime", "result": "failed",
            "failure_category": "execution_or_evidence_failed",
            "last_startup_operation": {"component": component, "phase": phase},
            "runtime_startup_failure": {"code": code}}


class RuntimeDiagnosticContracts(unittest.TestCase):
    def test_exact_messages_only_never_retain_error_text(self):
        for message, code in diagnostics.MESSAGE_CODES.items():
            self.assertEqual({"code": code}, diagnostics.failure_receipt(RuntimeError(message)))
        class OtherError(RuntimeError):
            pass
        for error in (RuntimeError("PRIVATE-PATH-TOKEN"), OSError("Owned host readiness timed out"),
                      OtherError("Owned host readiness timed out"), RuntimeError("Owned host readiness timed out", "PRIVATE")):
            self.assertEqual({"code": "unknown"}, diagnostics.failure_receipt(error))

    def test_operations_are_host_bound_and_status_cannot_cross_boundary(self):
        evidence = {}
        diagnostics.report_operation(evidence, "hosted-wasm", "backend", "readiness")
        diagnostics.report_status(evidence, "hosted-wasm", "backend", 500)
        self.assertEqual(500, evidence["last_startup_operation"]["http_status"])
        diagnostics.report_operation(evidence, "hosted-wasm", "hosted-wasm", "launch")
        self.assertNotIn("http_status", evidence["last_startup_operation"])
        before = copy.deepcopy(evidence)
        for operation in (("wasm", "readiness"), ("pair", "readiness"), ("PRIVATE", "PRIVATE")):
            with self.assertRaises(ValueError):
                diagnostics.report_operation(evidence, "hosted-wasm", *operation)
            self.assertEqual(before, evidence)
        with self.assertRaises(ValueError):
            diagnostics.report_status(evidence, "hosted-wasm", "backend", 500)

    def test_retention_rejects_raw_unbound_and_invalid_status_diagnostics(self):
        valid = failed_evidence()
        valid["last_startup_operation"]["http_status"] = 500
        diagnostics.validate_evidence(valid, CELL)
        variants = []
        def altered(section, key, value):
            row = copy.deepcopy(valid)
            row[section][key] = value
            variants.append(row)
        for status in (True, False, None, 99, 600, 500.0, "500"):
            altered("last_startup_operation", "http_status", status)
        altered("last_startup_operation", "component", "wasm")
        altered("last_startup_operation", "phase", "launch")
        altered("last_startup_operation", "private_error", "PRIVATE")
        altered("runtime_startup_failure", "code", "PRIVATE")
        altered("runtime_startup_failure", "message", "PRIVATE")
        for key, value in (("stage", "complete"), ("stage", "project_provenance"), ("stage", []),
                           ("result", "passed"), ("host", "backend"), ("failure_category", None)):
            variants.append({**valid, key: value})
        variants.append({key: value for key, value in valid.items() if key != "last_startup_operation"})
        for row in variants:
            with self.subTest(row=row), self.assertRaises(ValueError):
                diagnostics.validate_evidence(row, CELL)
        for status in (100, 200, 599):
            row = copy.deepcopy(valid)
            row["last_startup_operation"]["http_status"] = status
            diagnostics.validate_evidence(row, CELL)

    def test_identity_is_bound_to_the_expected_matrix_cell(self):
        original = failed_evidence()
        diagnostics.validate_evidence(original, CELL)
        variants = [
            {**original, "version": "3.9.0"},
            {**original, "framework": "net9.0"},
            {**original, "version": "3.9.0", "framework": "net9.0", "host": "server",
             "last_startup_operation": {"component": "server", "phase": "readiness"}},
        ]
        for field in ("version", "framework", "host"):
            variants.append({key: value for key, value in original.items() if key != field})
        for row in variants:
            with self.subTest(row=row), self.assertRaisesRegex(ValueError, "matrix cell"):
                diagnostics.validate_evidence(row, CELL)
        # Old receipts have no startup fields and retain their prior contract.
        diagnostics.validate_evidence({}, CELL)

    def test_validation_failure_and_later_success_have_distinct_shapes(self):
        row = failed_evidence("pair", "validation", "startup_input_changed")
        row["stage"] = "project_provenance"
        diagnostics.validate_evidence(row, CELL)
        later = failed_evidence()
        later.pop("runtime_startup_failure")
        later.update(stage="complete", result="passed")
        later.pop("failure_category")
        diagnostics.validate_evidence(later, CELL)
        for code in ("startup_config_symlink", "startup_host_exited"):
            row["runtime_startup_failure"]["code"] = code
            with self.assertRaises(ValueError):
                diagnostics.validate_evidence(row, CELL)


class SyntheticStartupContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.layout = SimpleNamespace(request=SimpleNamespace(host="hosted-wasm"), runtime_root=self.root)
        self.config = self.root / "public-config.json"
        self.config.write_text('{"public":"config"}')
        self.operations, self.statuses = [], []
        self.processes, self.logs = [Mock(), Mock()], [Mock(), Mock()]
        stack = self.enterContext(ExitStack())
        def stub(name, **options):
            return stack.enter_context(patch.object(hosts, name, **options))
        self.validate = stub("_validate_launch")
        stub("_runtime_environment", return_value=("http://127.0.0.1:1", "http://127.0.0.1:2", "PRIVATE", {}, {}))
        stub("_public_configuration", return_value=self.config)
        self.launch = stub("_launch_host", side_effect=list(zip(self.processes, self.logs)))
        self.ready = stub("_wait_ready")
        self.stop = stub("_stop_all")
        stub("_runtime_handle", return_value=SimpleNamespace())
        self.timer = stack.enter_context(patch.object(hosts.threading, "Timer"))

    def pair(self, *, observed=True):
        options = ({"report_startup": lambda component, phase: self.operations.append((component, phase)),
                    "report_readiness_status": lambda component, status: self.statuses.append((component, status))}
                   if observed else {})
        return hosts.start_pair(self.layout, validate_project=lambda _: {}, **options)

    def test_startup_reports_existing_boundaries_and_cleans_up(self):
        def ready(_process, _url, _timeout, *, report_status):
            report_status(200)
        self.ready.side_effect = ready
        with self.pair():
            self.stop.assert_not_called()
        self.assertEqual([("pair", "validation"), ("pair", "configuration"),
                          ("backend", "launch"), ("backend", "readiness"),
                          ("hosted-wasm", "launch"), ("hosted-wasm", "readiness")], self.operations)
        self.assertEqual([("backend", 200), ("hosted-wasm", 200)], self.statuses)
        self.stop.assert_called_once_with(self.processes)
        self.timer.return_value.cancel.assert_called_once()
        for log in self.logs:
            log.close.assert_called_once()
        self.assertEqual(b'{}\n', self.config.read_bytes())

    def test_old_callers_keep_three_argument_readiness_seam(self):
        with self.pair(observed=False):
            pass
        self.assertEqual([], self.operations)
        for call in self.ready.call_args_list:
            self.assertEqual(3, len(call.args))
            self.assertEqual({}, call.kwargs)

    def test_source_failure_preserves_exception_and_never_launches(self):
        error = RuntimeError("Materialized input changed before launch")
        self.validate.side_effect = error
        with self.assertRaises(RuntimeError) as caught:
            with self.pair():
                self.fail("validation failure yielded")
        self.assertIs(error, caught.exception)
        self.assertEqual([("pair", "validation")], self.operations)
        self.launch.assert_not_called()
        self.stop.assert_not_called()

    def test_studio_readiness_failure_still_stops_both_owned_processes(self):
        error = RuntimeError("Owned host readiness timed out")
        self.ready.side_effect = [None, error]
        with self.assertRaises(RuntimeError) as caught:
            with self.pair():
                self.fail("startup failure yielded")
        self.assertIs(error, caught.exception)
        self.assertEqual(("hosted-wasm", "readiness"), self.operations[-1])
        self.stop.assert_called_once_with(self.processes)
        for log in self.logs:
            log.close.assert_called_once()
        self.assertEqual(b'{}\n', self.config.read_bytes())

    def test_cleanup_failure_preserves_original_behavior_and_closes_logs(self):
        error = RuntimeError("PRIVATE-CLEANUP")
        self.stop.side_effect = error
        with self.assertRaises(RuntimeError) as caught:
            with self.pair():
                pass
        self.assertIs(error, caught.exception)
        self.timer.return_value.cancel.assert_called_once()
        for log in self.logs:
            log.close.assert_called_once()
        self.assertEqual(b'{}\n', self.config.read_bytes())


class ReadinessStatusContracts(unittest.TestCase):
    def test_http_error_status_is_observed_without_reading_body(self):
        status = []
        body = Mock()
        error = HTTPError("http://127.0.0.1:1", 500, "PRIVATE", {}, body)
        with patch.object(hosts.time, "monotonic", side_effect=[0, 0, 2]), \
                patch.object(hosts.time, "sleep"), patch.object(hosts, "urlopen", side_effect=error):
            with self.assertRaisesRegex(RuntimeError, "Owned host readiness timed out"):
                hosts._wait_ready(Mock(poll=lambda: None), "http://127.0.0.1:1", 1, report_status=status.append)
        self.assertEqual([500], status)
        body.read.assert_not_called()
        body.close.assert_called_once()

    def test_http_error_closes_even_when_status_observer_fails(self):
        body = Mock()
        error = HTTPError("http://127.0.0.1:1", 500, "PRIVATE", {}, body)
        failure = ValueError("observer failed")
        with patch.object(hosts.time, "monotonic", side_effect=[0, 0]), \
                patch.object(hosts, "urlopen", side_effect=error):
            with self.assertRaises(ValueError) as caught:
                hosts._wait_ready(Mock(poll=lambda: None), "http://127.0.0.1:1", 1,
                                  report_status=Mock(side_effect=failure))
        self.assertIs(failure, caught.exception)
        body.close.assert_called_once()
        body.read.assert_not_called()

    def test_transport_failure_has_no_invented_status(self):
        status = []
        with patch.object(hosts.time, "monotonic", side_effect=[0, 0, 2]), \
                patch.object(hosts.time, "sleep"), patch.object(hosts, "urlopen", side_effect=URLError("PRIVATE")):
            with self.assertRaisesRegex(RuntimeError, "Owned host readiness timed out"):
                hosts._wait_ready(Mock(poll=lambda: None), "http://127.0.0.1:1", 1, report_status=status.append)
        self.assertEqual([], status)


if __name__ == "__main__":
    unittest.main()
