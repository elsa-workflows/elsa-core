"""Offline failure-injection tests for the distinct 3.10.1 executor prototype.

No network PUT. Fit against 1800s is not claimed.
"""
from __future__ import annotations

from pathlib import Path
import json
import threading
import unittest

import consolidated_3101_executor as proto
import consolidated_package_executor as old


def key(name, digest):
    return f"{name}/{digest[:32]}FFFFFFFF/{name}"


def ok(status=201, body=b"", *, transmitted=True, complete=True, failure=None):
    return proto.ExchangeResult(status, body, complete, failure, transmitted)


class Clock:
    def __init__(self, now=0.0):
        self.now = now

    def __call__(self):
        return self.now


def harness(handler, *, now=0.0, concurrency=4):
    clock = now if callable(now) else Clock(now)
    deadline = proto.ImmutableDeadline(start=0.0)
    coordinator = proto.Coordinator(deadline, clock=clock, concurrency=concurrency)
    transport = proto.SharedTransport(coordinator, proto.InjectedExchange(handler))
    return proto.Prototype3101(coordinator, transport, proto.SymbolIndex()), coordinator, transport


class DeadlineAndTransportTests(unittest.TestCase):
    def test_deadline_is_frozen_and_covers_the_reviewed_bound(self):
        deadline = proto.ImmutableDeadline(start=10.0)
        self.assertEqual(deadline.seconds, 1800)
        self.assertEqual(deadline.remaining(10.0), 1800)
        with self.assertRaises(Exception):
            deadline.start = 0  # type: ignore[misc]
        coordinator = proto.Coordinator(deadline, clock=lambda: 10.0)
        self.assertIs(coordinator.deadline, deadline)
        self.assertNotEqual(coordinator.deadline.seconds, 120)

    def test_shared_transport_has_no_mutable_deadline_and_caps_http(self):
        clock = Clock(0.0)
        proto_exec, coordinator, transport = harness(lambda *args, **kwargs: ok(), now=clock)
        self.assertFalse(hasattr(transport, "deadline"))
        proto_exec.metadata_get("https://example.test/meta", "token")
        self.assertEqual(transport.exchange.calls[0]["timeout"], 25)
        clock.now = 1790
        proto_exec.metadata_get("https://example.test/meta2", "token")
        self.assertEqual(transport.exchange.calls[1]["timeout"], 10)

    def test_old_github_api_clock_is_not_imported_or_inherited(self):
        self.assertIn("HttpTransport(seconds=120, max_requests=100)",
                      Path(old.__file__).read_text(encoding="utf-8"))
        self.assertNotIn("seconds=120", Path(proto.__file__).read_text(encoding="utf-8"))
        self.assertNotIn("max_requests=100", Path(proto.__file__).read_text(encoding="utf-8"))
        self.assertNotIn("reader.deadline", Path(proto.__file__).read_text(encoding="utf-8"))

    def test_credential_separation(self):
        def handler(method, url, *, headers, data, timeout):
            return ok(200, b"{}")
        proto_exec, _, transport = harness(handler)
        proto_exec.metadata_get("https://example.test/meta", "meta-token")
        archive = b"nupkg-bytes"
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, "https://example.test/pkg", publish_key="publish-key")
        meta, put, get = transport.exchange.calls
        self.assertEqual(meta["header_names"], ("Authorization",))
        self.assertEqual(put["header_names"], ("Content-Type", "X-NuGet-ApiKey"))
        self.assertEqual(get["header_names"], ())
        with self.assertRaisesRegex(proto.PrototypeError, "credential_separation"):
            transport.request("PUT", "https://example.test/x",
                              headers={"Authorization": "Bearer x", "X-NuGet-ApiKey": "y"},
                              credential_role="publish")


class AdmissionAndBudgetTests(unittest.TestCase):
    def test_simultaneous_put_admission_is_atomic(self):
        clock = Clock(0.0)
        deadline = proto.ImmutableDeadline(start=0.0)
        coordinator = proto.Coordinator(deadline, clock=clock, concurrency=1)
        barrier = threading.Barrier(16)
        results = []

        def worker(index):
            barrier.wait()
            results.append(coordinator.admit_put(f"p{index}"))

        threads = [threading.Thread(target=worker, args=(i,)) for i in range(16)]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()
        accepted = [item for item in results if not isinstance(item, str)]
        refused = [item for item in results if isinstance(item, str)]
        self.assertEqual(len(accepted) + len(refused), 16)
        self.assertEqual(len(accepted), coordinator.queued_puts)
        self.assertEqual(coordinator.used, len(accepted))
        self.assertEqual(coordinator.outstanding_proof, len(accepted))

    def test_readback_reserve_stops_put_and_preserves_proof_capacity(self):
        clock = Clock(0.0)
        deadline = proto.ImmutableDeadline(start=0.0)
        coordinator = proto.Coordinator(deadline, clock=clock, concurrency=1)
        first = coordinator.admit_put("keep")
        coordinator.start_put(first)
        coordinator.finish_put(first, transmitted=True)
        clock.now = 901  # remaining 899 <= reserve 900
        refused = coordinator.admit_put("late")
        self.assertEqual(refused, "readback_budget_exhausted")
        self.assertTrue(coordinator.put_admission_stopped)
        proof = coordinator.admit_proof_get(first)
        self.assertNotIsInstance(proof, str)
        coordinator.finish_proof_get(proof)
        self.assertEqual(coordinator.outstanding_proof, 0)

    def test_request_exhaustion_is_pretransmission(self):
        clock = Clock(0.0)
        coordinator = proto.Coordinator(proto.ImmutableDeadline(start=0.0), clock=clock)
        coordinator.used = 4999
        self.assertEqual(coordinator.admit_put("x"), "readback_budget_exhausted")
        self.assertEqual(coordinator.outstanding_proof, 0)

    def test_expired_deadline_is_not_attempted(self):
        clock = Clock(1800.0)
        coordinator = proto.Coordinator(proto.ImmutableDeadline(start=0.0), clock=clock)
        self.assertEqual(coordinator.admit_put("x"), "not_attempted")
        self.assertEqual(coordinator.admit_get("y"), "not_attempted")


class PutGetClassificationTests(unittest.TestCase):
    def test_accepted_pending_proof_and_matching_get_are_separate(self):
        archive = b"original-nupkg"
        store = {}

        def handler(method, url, *, headers, data, timeout):
            if method == "PUT":
                store["put"] = data
                return ok(201)
            return ok(200, store["put"])

        proto_exec, _, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, "https://example.test/Elsa", publish_key="k")
        self.assertEqual(operation.put_outcome, "accepted_pending_proof")
        self.assertEqual(operation.put_status, 201)
        self.assertEqual(operation.get_classification, "matching")
        self.assertEqual(operation.state, "matching")

    def test_200_201_202_are_accepted_pending_proof(self):
        archive = b"x"
        for status in (200, 201, 202):
            def handler(method, url, *, headers, data, timeout, status=status):
                return ok(status) if method == "PUT" else ok(200, archive)

            proto_exec, _, _ = harness(handler)
            operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
            proto_exec.put_and_prove_nupkg(operation, "https://example.test/Elsa", publish_key="k")
            self.assertEqual(operation.put_outcome, "accepted_pending_proof", status)

    def test_204_and_409_are_not_duplicate_success(self):
        for status in (204, 409):
            archive = b"x"
            proto_exec, _, _ = harness(lambda *args, **kwargs: ok(status))
            operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
            proto_exec.put_and_prove_nupkg(operation, "https://example.test/Elsa", publish_key="k")
            self.assertEqual(operation.put_outcome, "not_duplicate_success")
            self.assertEqual(operation.state, "not_duplicate_success")
            self.assertIsNone(operation.get_classification)

    def test_transmission_timeout_is_acceptance_unknown_and_stops_put(self):
        archive = b"x"

        def handler(method, url, *, headers, data, timeout):
            return proto.ExchangeResult(None, b"", False, "timeout", False)

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, "https://example.test/Elsa", publish_key="k")
        self.assertEqual(operation.put_outcome, "acceptance_unknown")
        self.assertEqual(operation.state, "acceptance_unknown")
        self.assertTrue(coordinator.put_admission_stopped)
        self.assertEqual(coordinator.outstanding_proof, 0)

    def test_no_blind_retry_on_failed_put(self):
        calls = {"n": 0}

        def handler(method, url, *, headers, data, timeout):
            calls["n"] += 1
            return ok(500)

        proto_exec, _, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(b"x"), b"x")
        proto_exec.put_and_prove_nupkg(operation, "https://example.test/Elsa", publish_key="k")
        self.assertEqual(calls["n"], 1)
        self.assertEqual(operation.put_outcome, "acceptance_unknown")

    def test_pre_get_is_not_post_put_proof(self):
        archive = b"original"
        seen = []

        def handler(method, url, *, headers, data, timeout):
            seen.append(method)
            if method == "GET" and not data:
                return ok(404, b"")
            return ok(201)

        proto_exec, _, _ = harness(handler)
        observed = proto_exec.pre_get_nupkg("https://example.test/Elsa", archive)
        self.assertEqual(observed["classification"], "missing")
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, "https://example.test/Elsa", publish_key="k")
        self.assertEqual(operation.put_outcome, "accepted_pending_proof")
        self.assertNotEqual(operation.state, "matching")

    def test_normalized_payload_is_not_archive_proof(self):
        original = b"full-nupkg-bytes"

        def handler(method, url, *, headers, data, timeout):
            if method == "PUT":
                return ok(200)
            return ok(200, original[4:])

        proto_exec, _, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(original), original)
        proto_exec.put_and_prove_nupkg(operation, "https://example.test/Elsa", publish_key="k")
        self.assertEqual(operation.put_outcome, "accepted_pending_proof")
        self.assertEqual(operation.get_classification, "conflicting")
        self.assertEqual(operation.failure_category, "archive_sha_mismatch")

    def test_never_repack_originals(self):
        proto_exec, _, _ = harness(lambda **_: ok(201))
        with self.assertRaisesRegex(proto.PrototypeError, "original_archive_changed"):
            proto_exec.put_and_prove_nupkg(
                proto.Operation("Elsa", "nupkg", proto.sha(b"a"), b"b"),
                "https://example.test/Elsa", publish_key="k")

    def test_slow_malformed_get_is_unverifiable(self):
        archive = b"original"

        def handler(method, url, *, headers, data, timeout):
            if method == "PUT":
                return ok(201)
            return proto.ExchangeResult(200, b"partial", False, "incomplete_response", True)

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, "https://example.test/Elsa", publish_key="k")
        self.assertEqual(operation.get_classification, "unverifiable")
        self.assertEqual(coordinator.outstanding_proof, 0)
        self.assertEqual(coordinator.inflight_gets, 0)


class OverlapConflictTests(unittest.TestCase):
    def test_same_key_different_bytes_is_conflict(self):
        proto_exec, _, _ = harness(lambda **_: ok())
        digest_a = proto.sha(b"pdb-a")
        digest_b = proto.sha(b"pdb-b")
        proto_exec.preflight_keys([
            proto.SymbolKey(key("elsa.pdb", digest_a), digest_a, "Elsa", b"pdb-a"),
        ])
        with self.assertRaisesRegex(proto.PrototypeError, "duplicate_symbol_key_conflict"):
            proto_exec.preflight_keys([
                proto.SymbolKey(key("elsa.pdb", digest_a), digest_b, "Elsa.Other", b"pdb-b"),
            ])

    def test_overlapping_snupkg_upload_and_readback_are_serialized(self):
        inflight = {"n": 0, "max": 0, "lock": threading.Lock()}
        digest = proto.sha(b"shared-pdb")
        shared = key("shared.pdb", digest)

        def handler(method, url, *, headers, data, timeout):
            with inflight["lock"]:
                inflight["n"] += 1
                inflight["max"] = max(inflight["max"], inflight["n"])
            try:
                if method == "PUT":
                    return ok(201)
                return ok(200, b"shared-pdb")
            finally:
                with inflight["lock"]:
                    inflight["n"] -= 1

        proto_exec, _, _ = harness(handler)
        proto_exec.preflight_keys([
            proto.SymbolKey(shared, digest, "A", b"shared-pdb"),
            proto.SymbolKey(shared, digest, "B", b"shared-pdb"),
        ])
        barrier = threading.Barrier(2)

        def run(package_id):
            barrier.wait()
            archive = package_id.encode()
            proto_exec.put_snupkg_serialized(
                proto.Operation(package_id, "snupkg", proto.sha(archive), archive),
                f"https://example.test/{package_id}", publish_key="k")

        threads = [threading.Thread(target=run, args=("A",)), threading.Thread(target=run, args=("B",))]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()
        self.assertEqual(inflight["max"], 1)
        self.assertFalse(proto_exec.remote_snupkg_archive_verified)

    def test_pdb_association_is_not_snupkg_archive_proof(self):
        digest = proto.sha(b"pdb")
        symbol = key("elsa.pdb", digest)

        def handler(method, url, *, headers, data, timeout):
            if method == "PUT":
                return ok(201)
            return ok(200, b"pdb")

        proto_exec, _, _ = harness(handler)
        proto_exec.preflight_keys([proto.SymbolKey(symbol, digest, "Elsa", b"pdb")])
        archive = b"original-snupkg"
        operation = proto.Operation("Elsa", "snupkg", proto.sha(archive), archive)
        proto_exec.put_snupkg_serialized(operation, "https://example.test/Elsa.snupkg", publish_key="k")
        self.assertEqual(operation.put_outcome, "accepted_pending_proof")
        self.assertEqual(operation.get_classification, "pdb_association_matching")
        self.assertFalse(proto_exec.remote_snupkg_archive_verified)
        association = proto_exec.prove_pdb_association(symbol, b"pdb")
        self.assertEqual(association["classification"], "matching")
        self.assertFalse(association["remote_snupkg_archive_verified"])
        self.assertFalse(proto_exec.remote_snupkg_archive_verified)


class MeasurementTests(unittest.TestCase):
    def test_measured_counts_differ_from_assumed_u_and_do_not_claim_fit(self):
        report = proto.offline_measurement_report()
        self.assertEqual(report["two_nupkg_passes_plus_three"], 465)
        self.assertEqual(report["architect_assumed"], {"S": 688, "U": 462, "total": 2303})
        self.assertEqual(report["measured_counts"]["symbol_associations_S"], 688)
        self.assertEqual(report["measured_counts"]["unique_pdb_bytes_U"], 688)
        self.assertEqual(report["measured_formula_465_2S_U"], 2529)
        self.assertGreater(report["worst_case_seconds_before_observation_c4"], proto.MAX_SECONDS)
        self.assertGreater(report["worst_case_seconds_put_then_get_c4"], proto.MAX_SECONDS)
        self.assertFalse(report["fits_worst_case_1800"])
        self.assertFalse(report["concurrency_4_worst_case_guarantee"])
        self.assertFalse(report["safe_1800_demonstrated"])
        self.assertFalse(report["fit_claim"])
        receipt = json.loads(
            (Path(__file__).with_name("consolidated-3101-executor-measurement.json")).read_text(encoding="utf-8"))
        self.assertFalse(receipt["fit_claim"])
        self.assertFalse(receipt["safe_1800_demonstrated"])
        self.assertFalse(receipt["network_put_authorized"])
        self.assertEqual(receipt["measured"]["measured_formula_465_2S_U"], 2529)
        self.assertEqual(receipt["measured"]["measured_http_total"], 2304)
        self.assertEqual(receipt["architect_assumed"]["total"], 2303)
        self.assertEqual(receipt["worst_case_at_http_cap_25s"]["before_observation_seconds_c4"],
                         report["worst_case_seconds_before_observation_c4"])
        self.assertEqual(receipt["worst_case_at_http_cap_25s"]["put_then_get_seconds_c4"],
                         report["worst_case_seconds_put_then_get_c4"])
        self.assertLess(receipt["worst_case_at_http_cap_25s"]["margin_seconds_before_observation_vs_1800"], 0)

    def test_zero_latency_mock_is_not_a_fit_proof(self):
        archive = b"x"
        proto_exec, coordinator, _ = harness(
            lambda method, url, **_: ok(201) if method == "PUT" else ok(200, archive))
        proto_exec.put_and_prove_nupkg(
            proto.Operation("Elsa", "nupkg", proto.sha(archive), archive),
            "https://example.test/Elsa", publish_key="k")
        self.assertLess(coordinator.deadline.remaining(coordinator.clock()), proto.MAX_SECONDS + 1)
        self.assertFalse(proto.offline_measurement_report()["fit_claim"])


if __name__ == "__main__":
    unittest.main()
