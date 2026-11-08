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


def ok(status=201, body=b"", *, complete=True, failure=None, disposition=proto.RESPONSE_COMPLETE):
    return proto.ExchangeResult(status, body, complete, failure, disposition)


class Clock:
    def __init__(self, now=0.0):
        self.now = now

    def __call__(self):
        return self.now


def harness(handler, *, now=0.0):
    clock = now if callable(now) else Clock(now)
    deadline = proto.ImmutableDeadline(start=0.0)
    coordinator = proto.Coordinator(deadline, clock=clock)
    transport = proto.SharedTransport(coordinator, proto.InjectedExchange(handler))
    return proto.Prototype3101(coordinator, transport, proto.SymbolIndex()), coordinator, transport


def meta_url():
    return ("https://api.github.com/repos/elsa-workflows/elsa-core/"
            "environments/feedz-publish/secrets?per_page=100&page=1")


class SlotReservationTests(unittest.TestCase):
    def test_admit_put_commits_proof_slots_so_a_second_put_cannot_steal_them(self):
        coordinator = proto.Coordinator(proto.ImmutableDeadline(start=0.0), clock=Clock(0.0))
        coordinator.used = 4997
        first = coordinator.admit_put("a", proof_slots=1)
        second = coordinator.admit_put("b", proof_slots=1)
        self.assertNotIsInstance(first, str)
        self.assertEqual(second, "readback_budget_exhausted")
        self.assertEqual(coordinator.used, 4999)
        self.assertEqual(coordinator.reserved_proof, 1)
        independent = coordinator.admit_get("index")
        self.assertNotIsInstance(independent, str)
        self.assertEqual(coordinator.used, 5000)
        starved = coordinator.admit_get("extra")
        self.assertEqual(starved, "not_attempted")
        coordinator.start_put(first)
        coordinator.finish_put(first, disposition=proto.RESPONSE_COMPLETE)
        proof = coordinator.admit_proof_get(first)
        self.assertNotIsInstance(proof, str)
        coordinator.finish_proof_get(proof)
        coordinator.finish_get(independent)

    def test_multi_proof_slots_are_committed_not_proof_slots_minus_one(self):
        coordinator = proto.Coordinator(proto.ImmutableDeadline(start=0.0), clock=Clock(0.0))
        coordinator.used = 4996
        reservation = coordinator.admit_put("s", proof_slots=3)
        self.assertNotIsInstance(reservation, str)
        self.assertEqual(coordinator.used, 5000)
        self.assertEqual(coordinator.reserved_proof, 3)
        self.assertEqual(coordinator.admit_get("steal"), "not_attempted")
        coordinator.start_put(reservation)
        coordinator.finish_put(reservation, disposition=proto.RESPONSE_COMPLETE)
        for _ in range(3):
            proof = coordinator.admit_proof_get(reservation)
            self.assertNotIsInstance(proof, str)
            coordinator.finish_proof_get(proof)
        self.assertEqual(coordinator.outstanding_proof, 0)

    def test_independent_get_cannot_spend_reserved_proof_when_budget_is_full(self):
        coordinator = proto.Coordinator(proto.ImmutableDeadline(start=0.0), clock=Clock(0.0))
        coordinator.used = 4998
        reservation = coordinator.admit_put("a", proof_slots=1)
        self.assertEqual(coordinator.used, 5000)
        self.assertEqual(coordinator.admit_get("index"), "not_attempted")
        coordinator.start_put(reservation)
        coordinator.finish_put(reservation, disposition=proto.MAY_HAVE_SENT)
        self.assertGreater(coordinator.outstanding_proof, 0)
        self.assertNotIsInstance(coordinator.admit_proof_get(reservation), str)


class TransmissionModelTests(unittest.TestCase):
    def test_may_have_sent_preserves_proof_and_still_gets(self):
        archive = b"x"
        calls = []

        def handler(method, url, **kwargs):
            calls.append(method)
            if method == "PUT":
                return proto.ExchangeResult(None, b"", False, "timeout", proto.MAY_HAVE_SENT)
            return ok(404, b"")

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.upload_outcome, "acceptance_unknown")
        self.assertIn("GET", calls)
        self.assertEqual(operation.content_presence, "missing")
        self.assertTrue(coordinator.put_admission_stopped)
        self.assertEqual(coordinator.outstanding_proof, 0)

    def test_definitely_not_sent_releases_proof_and_does_not_get(self):
        archive = b"x"
        calls = []

        def handler(method, url, **kwargs):
            calls.append(method)
            return proto.ExchangeResult(None, b"", False, "not_attempted", proto.DEFINITELY_NOT_SENT)

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.state, "not_attempted")
        self.assertNotIn("GET", calls)
        self.assertEqual(coordinator.outstanding_proof, 0)

    def test_complete_500_halts_put_but_still_gets_presence(self):
        archive = b"x"
        calls = []

        def handler(method, url, **kwargs):
            calls.append(method)
            if method == "PUT":
                return ok(500, b"", complete=True)
            return ok(200, archive)

        proto_exec, coordinator, _ = harness(handler)
        first = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(first, publish_key="k")
        self.assertEqual(first.upload_outcome, "acceptance_unknown")
        self.assertEqual(first.content_presence, "matching")
        self.assertTrue(coordinator.put_admission_stopped)
        second = proto.Operation("Elsa.Other", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(second, publish_key="k")
        self.assertEqual(second.state, "not_attempted")
        self.assertEqual(calls.count("PUT"), 1)

    def test_incomplete_201_is_not_accepted_pending_proof(self):
        archive = b"x"

        def handler(method, url, **kwargs):
            if method == "PUT":
                return ok(201, b"", complete=False, failure="incomplete_response")
            return ok(200, archive)

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertNotEqual(operation.upload_outcome, "accepted_pending_proof")
        self.assertEqual(operation.content_presence, "matching")
        self.assertTrue(coordinator.put_admission_stopped)

    def test_204_and_409_are_not_duplicate_success_and_still_get(self):
        archive = b"x"
        for status in (204, 409):
            def handler(method, url, **kwargs):
                if method == "PUT":
                    return ok(status)
                return ok(200, archive)

            proto_exec, _, _ = harness(handler)
            operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
            proto_exec.put_and_prove_nupkg(operation, publish_key="k")
            self.assertEqual(operation.upload_outcome, "not_duplicate_success")
            self.assertEqual(operation.content_presence, "matching")


class OverlapAndAssociationTests(unittest.TestCase):
    def test_second_overlapping_archive_reevaluates_and_skips_put(self):
        digest = proto.sha(b"shared-pdb")
        shared = key("shared.pdb", digest)
        uploaded = {"n": 0}

        def handler(method, url, **kwargs):
            if method == "PUT":
                uploaded["n"] += 1
                return ok(201)
            if uploaded["n"]:
                return ok(200, b"shared-pdb")
            return ok(404, b"")

        proto_exec, _, _ = harness(handler)
        proto_exec.preflight_keys([
            proto.SymbolKey(shared, digest, "A", b"shared-pdb"),
            proto.SymbolKey(shared, digest, "B", b"shared-pdb"),
        ])
        first = proto.Operation("A", "snupkg", proto.sha(b"A"), b"A")
        second = proto.Operation("B", "snupkg", proto.sha(b"B"), b"B")
        proto_exec.put_snupkg_serialized(first, publish_key="k")
        proto_exec.put_snupkg_serialized(second, publish_key="k")
        self.assertEqual(uploaded["n"], 1)
        self.assertEqual(second.state, "pdbs_observed_unverified_skip_put")
        self.assertFalse(proto_exec.remote_snupkg_archive_verified)

    def test_conflicting_readback_stops_later_writes(self):
        digest = proto.sha(b"pdb-a")
        shared = key("shared.pdb", digest)

        def handler(method, url, **kwargs):
            if method == "PUT":
                return ok(201)
            return ok(200, b"other-bytes")

        proto_exec, coordinator, _ = harness(handler)
        proto_exec.preflight_keys([
            proto.SymbolKey(shared, digest, "A", b"pdb-a"),
            proto.SymbolKey(shared, digest, "B", b"pdb-a"),
        ])
        first = proto.Operation("A", "snupkg", proto.sha(b"A"), b"A")
        proto_exec.put_snupkg_serialized(first, publish_key="k")
        self.assertEqual(first.state, "symbol_overlap_conflict")
        self.assertTrue(coordinator.writes_halted)
        second = proto.Operation("B", "snupkg", proto.sha(b"B"), b"B")
        proto_exec.put_snupkg_serialized(second, publish_key="k")
        self.assertEqual(second.state, "not_attempted")

    def test_unverified_pdb_bytes_are_not_association_matching(self):
        digest = proto.sha(b"pdb")
        symbol = proto.SymbolKey(key("elsa.pdb", digest), digest, "Elsa", b"pdb")
        uploaded = {"n": 0}

        def handler(method, url, **kwargs):
            if method == "PUT":
                uploaded["n"] += 1
                return ok(201)
            if "symbols/" in url:
                return ok(200, b"pdb") if uploaded["n"] else ok(404, b"")
            return ok(404, b"")

        proto_exec, _, _ = harness(handler)
        proto_exec.preflight_keys([symbol])
        operation = proto.Operation("Elsa", "snupkg", proto.sha(b"snupkg"), b"snupkg")
        proto_exec.put_snupkg_serialized(operation, publish_key="k")
        self.assertEqual(operation.get_classification, "bytes_observed_unverified_inputs")
        self.assertNotEqual(operation.get_classification, "pdb_association_matching")
        self.assertFalse(proto_exec.remote_snupkg_archive_verified)
        observed = proto_exec.prove_pdb_association(symbol)
        self.assertEqual(observed["classification"], "bytes_observed_unverified_inputs")
        self.assertFalse(observed["remote_snupkg_archive_verified"])

    def test_trusted_symbol_key_requires_inspector_manifest_bind(self):
        digest = proto.sha(b"pdb")
        with self.assertRaisesRegex(proto.PrototypeError, "symbol_inspector_unbound"):
            proto.validate_symbol_key(proto.SymbolKey(
                key("elsa.pdb", digest), digest, "Elsa", b"pdb", input_trust=proto.TRUSTED))
        with self.assertRaisesRegex(proto.PrototypeError, "symbol_key_unverified_hash"):
            proto.validate_symbol_key(proto.SymbolKey(
                key("elsa.pdb", digest), proto.sha(b"other"), "Elsa", b"pdb"))

    def test_nupkg_resource_lock_serializes_same_id(self):
        inflight = {"n": 0, "max": 0, "lock": threading.Lock()}
        archive = b"nupkg"

        def handler(method, url, **kwargs):
            with inflight["lock"]:
                inflight["n"] += 1
                inflight["max"] = max(inflight["max"], inflight["n"])
            try:
                return ok(201) if method == "PUT" else ok(200, archive)
            finally:
                with inflight["lock"]:
                    inflight["n"] -= 1

        proto_exec, _, _ = harness(handler)
        barrier = threading.Barrier(2)

        def run():
            barrier.wait()
            proto_exec.put_and_prove_nupkg(
                proto.Operation("Elsa", "nupkg", proto.sha(archive), archive), publish_key="k")

        threads = [threading.Thread(target=run) for _ in range(2)]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()
        self.assertEqual(inflight["max"], 1)


class TransportAndReceiptTests(unittest.TestCase):
    def test_url_allowlist_rejects_unlisted_hosts(self):
        proto_exec, _, transport = harness(lambda **kwargs: ok())
        with self.assertRaisesRegex(proto.PrototypeError, "unsafe_request"):
            transport.request("GET", "https://example.test/Elsa")

    def test_redirect_is_rejected(self):
        archive = b"x"

        def handler(method, url, **kwargs):
            return proto.ExchangeResult(302, b"", False, "redirect_rejected", proto.MAY_HAVE_SENT, True)

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.failure_category, "redirect_rejected")
        self.assertTrue(coordinator.put_admission_stopped)

    def test_response_cap_rejects_oversized_body(self):
        archive = b"x"

        def handler(method, url, **kwargs):
            if method == "PUT":
                return ok(201)
            return ok(200, b"n" * (proto.MAX_BYTES + 1))

        proto_exec, _, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.content_presence, "unverifiable")
        self.assertEqual(operation.failure_category, "oversized_response")

    def test_deadline_recheck_after_queue_does_not_send(self):
        clock = Clock(0.0)
        proto_exec, coordinator, _ = harness(lambda **kwargs: ok(), now=clock)

        class Jump:
            def acquire(self, timeout=None):
                clock.now = 1780
                return True

            def release(self):
                return None

        coordinator._exchange = Jump()
        operation = proto.Operation("Elsa", "nupkg", proto.sha(b"x"), b"x")
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.state, "not_attempted")

    def test_exception_updates_state_and_writes_receipt(self):
        archive = b"x"

        def handler(method, url, **kwargs):
            if method == "PUT":
                raise RuntimeError("boom")
            return ok(200, archive)

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.upload_outcome, "acceptance_unknown")
        self.assertTrue(any(row.get("event") == "nupkg_put_get" or row.get("id") == "Elsa"
                            for row in proto_exec.receipt.rows))
        self.assertTrue(coordinator.put_admission_stopped)

    def test_finalize_reaps_and_records_deadline(self):
        proto_exec, coordinator, _ = harness(lambda **kwargs: ok(200, b"{}"))
        proto_exec.metadata_get(meta_url(), "token")
        proto_exec.finalize()
        self.assertTrue(coordinator.abandoned)
        self.assertEqual(proto_exec.receipt.rows[-1]["event"], "final")

    def test_no_publish_authority(self):
        self.assertFalse(proto.Prototype3101.authority)
        self.assertFalse(proto.SharedTransport.authority)

    def test_old_executor_untouched(self):
        text = Path(old.__file__).read_text(encoding="utf-8")
        self.assertIn("HttpTransport(seconds=120, max_requests=100)", text)
        self.assertIn("reader.deadline = min(reader.deadline, inspector.deadline)", text)


class ExistingContractTests(unittest.TestCase):
    def test_accepted_pending_proof_separate_from_matching_get(self):
        archive = b"original-nupkg"
        store = {}

        def handler(method, url, **kwargs):
            if method == "PUT":
                store["put"] = True
                return ok(201)
            return ok(200, archive)

        proto_exec, _, transport = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.put_outcome, "accepted_pending_proof")
        self.assertEqual(operation.content_presence, "matching")
        self.assertEqual(transport.exchange.calls[0]["url"], proto.PACKAGE_PUBLISH)
        self.assertTrue(transport.exchange.calls[1]["url"].startswith(proto.PACKAGE_BASE))

    def test_200_201_202_complete_are_accepted_pending_proof(self):
        archive = b"x"
        for status in (200, 201, 202):
            def handler(method, url, **kwargs):
                return ok(status) if method == "PUT" else ok(200, archive)

            proto_exec, _, _ = harness(handler)
            operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
            proto_exec.put_and_prove_nupkg(operation, publish_key="k")
            self.assertEqual(operation.put_outcome, "accepted_pending_proof", status)

    def test_never_repack_originals(self):
        proto_exec, _, _ = harness(lambda **kwargs: ok())
        with self.assertRaisesRegex(proto.PrototypeError, "original_archive_changed"):
            proto_exec.put_and_prove_nupkg(
                proto.Operation("Elsa", "nupkg", proto.sha(b"a"), b"b"), publish_key="k")

    def test_normalized_payload_is_not_archive_proof(self):
        original = b"full-nupkg-bytes"

        def handler(method, url, **kwargs):
            return ok(200) if method == "PUT" else ok(200, original[4:])

        proto_exec, _, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(original), original)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.content_presence, "conflicting")

    def test_credential_separation(self):
        def handler(method, url, **kwargs):
            return ok(200, b"{}") if method == "GET" and "github" in url else (
                ok(201) if method == "PUT" else ok(200, b"nupkg-bytes"))

        proto_exec, _, transport = harness(handler)
        proto_exec.metadata_get(meta_url(), "meta-token")
        proto_exec.put_and_prove_nupkg(
            proto.Operation("Elsa", "nupkg", proto.sha(b"nupkg-bytes"), b"nupkg-bytes"),
            publish_key="publish-key")
        meta, put, get = transport.exchange.calls
        self.assertEqual(meta["header_names"], ("Authorization",))
        self.assertEqual(put["header_names"], ("Content-Type", "X-NuGet-ApiKey"))
        self.assertEqual(get["header_names"], ())

    def test_pre_get_is_not_post_put_proof(self):
        archive = b"original"

        def handler(method, url, **kwargs):
            if method == "GET" and method:
                if not hasattr(handler, "put"):
                    return ok(404, b"")
            if method == "PUT":
                handler.put = True
                return ok(201)
            return ok(201)

        proto_exec, _, _ = harness(handler)
        observed = proto_exec.pre_get_nupkg("Elsa", archive)
        self.assertEqual(observed["classification"], "missing")
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.put_outcome, "accepted_pending_proof")
        self.assertNotEqual(operation.content_presence, "matching")


class MeasurementTests(unittest.TestCase):
    def test_reconciled_dag_uses_archive_puts_as_u_and_withdraws_2529(self):
        report = proto.offline_measurement_report()
        self.assertEqual(report["architect_465"], 465)
        self.assertEqual(report["architect_U"], 462)
        self.assertEqual(report["archive_puts_U"], 462)
        self.assertEqual(report["associations_S"], 688)
        self.assertEqual(report["architect_total"], 2303)
        self.assertEqual(report["independent_pair_counterexample"]["seconds"], 5800)
        self.assertFalse(report["independent_pair_counterexample"]["full_cold_upper_bound"])
        self.assertFalse(report["fit_claim"])
        self.assertFalse(report["safe_1800_demonstrated"])
        self.assertTrue(report["ceil_work_over_4_invalid_with_locks"])
        receipt = json.loads(
            Path(__file__).with_name("consolidated-3101-executor-measurement.json").read_text(encoding="utf-8"))
        self.assertFalse(receipt["fit_claim"])
        self.assertEqual(receipt["dag"]["architect_total"], 2303)
        self.assertEqual(receipt["dag"]["withdrawn_2529"], "invalid; treated unique PDB bytes as U")
        self.assertFalse(receipt["dag"]["independent_pair_counterexample"]["full_cold_upper_bound"])
        plan = Path(__file__).with_name("consolidated-3100-readonly-get-sample-plan.md").read_text(encoding="utf-8")
        self.assertIn("No measurement in this document", plan)
        self.assertIn("not** put latency", plan.lower())

    def test_zero_latency_mock_is_not_a_fit_proof(self):
        archive = b"x"
        proto_exec, _, _ = harness(
            lambda method, url, **_: ok(201) if method == "PUT" else ok(200, archive))
        proto_exec.put_and_prove_nupkg(
            proto.Operation("Elsa", "nupkg", proto.sha(archive), archive), publish_key="k")
        self.assertFalse(proto.offline_measurement_report()["fit_claim"])


if __name__ == "__main__":
    unittest.main()
