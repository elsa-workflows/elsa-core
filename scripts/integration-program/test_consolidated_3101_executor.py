"""Offline failure-injection tests for the distinct 3.10.1 executor prototype.

Owning issue: https://github.com/elsa-workflows/elsa-core/issues/8220
Delete with the prototype when a reviewed 3.10.1 executor supersedes it or
the 3.10.1 Feedz path is abandoned.

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
        self.assertEqual(operation.key_presence[symbol.key], "bytes_observed_unverified_inputs")
        self.assertEqual(operation.content_presence, "all_required_observed")
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
        holding_get = threading.Event()
        release_get = threading.Event()
        put_while_get_held = threading.Event()

        def handler(method, url, **kwargs):
            if method == "PUT":
                if holding_get.is_set() and not release_get.is_set():
                    put_while_get_held.set()
            with inflight["lock"]:
                inflight["n"] += 1
                inflight["max"] = max(inflight["max"], inflight["n"])
            try:
                if method == "GET":
                    holding_get.set()
                    self.assertFalse(put_while_get_held.wait(0.05),
                                     "second PUT entered while GET still held the named lock")
                    release_get.set()
                    return ok(200, archive)
                return ok(201)
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
        self.assertFalse(put_while_get_held.is_set())


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
        self.assertEqual(operation.failure_category, "transport_failed")
        self.assertEqual(operation.state, "acceptance_unknown")
        self.assertTrue(any(row.get("event") == "nupkg_put_get" for row in proto_exec.receipt.rows))
        self.assertTrue(any(row.get("id") == "Elsa" for row in proto_exec.receipt.rows))
        self.assertEqual(coordinator.outstanding_proof, 0)
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


class ArchitectR2CounterexampleTests(unittest.TestCase):
    def test_queued_admissions_cannot_oversubscribe_aggregate_time(self):
        coordinator = proto.Coordinator(proto.ImmutableDeadline(start=0.0), clock=Clock(0.0))
        accepted = 0
        while True:
            result = coordinator.admit_put(f"p{accepted}", proof_slots=1)
            if isinstance(result, str):
                break
            accepted += 1
            require_bound = accepted * 50 + 30
            self.assertLessEqual(require_bound, proto.MAX_SECONDS)
        self.assertGreater(accepted, 0)
        self.assertLess(accepted, 462)
        self.assertEqual(result, "readback_budget_exhausted")

    def test_send_boundary_refuses_put_when_remaining_cannot_cover_proof_and_cleanup(self):
        clock = Clock(0.0)
        proto_exec, coordinator, transport = harness(lambda **kwargs: ok(201), now=clock)
        reservation = coordinator.admit_put("Elsa", proof_slots=1)
        coordinator.start_put(reservation)
        clock.now = 1769  # remaining 31s
        result = transport.request(
            "PUT", proto.PACKAGE_PUBLISH,
            headers={"X-NuGet-ApiKey": "k", "Content-Type": "application/octet-stream"},
            data=b"x", credential_role="publish", limit=4096)
        self.assertEqual(result.disposition, proto.DEFINITELY_NOT_SENT)
        self.assertEqual(len(transport.exchange.calls), 0)

    def test_independent_get_cannot_consume_committed_proof_time(self):
        clock = Clock(0.0)
        coordinator = proto.Coordinator(proto.ImmutableDeadline(start=0.0), clock=clock)
        reservation = coordinator.admit_put("a", proof_slots=1)
        self.assertNotIsInstance(reservation, str)
        clock.now = 1720  # remaining 80s; PUT+proof+cleanup already 80
        self.assertEqual(coordinator.admit_get("index"), "not_attempted")
        coordinator.start_put(reservation)
        coordinator.finish_put(reservation, disposition=proto.RESPONSE_COMPLETE)
        self.assertNotIsInstance(coordinator.admit_proof_get(reservation), str)

    def test_halt_stops_admitted_unsent_put_waiting_on_semaphore(self):
        held = threading.Event()
        release = threading.Event()

        class Hold:
            def acquire(self, timeout=None):
                held.set()
                return release.wait(30)

            def release(self):
                return None

        proto_exec, coordinator, transport = harness(lambda **kwargs: ok(201))
        coordinator._exchange = Hold()
        operation = proto.Operation("Elsa", "nupkg", proto.sha(b"x"), b"x")

        def run():
            proto_exec.put_and_prove_nupkg(operation, publish_key="k")

        thread = threading.Thread(target=run)
        thread.start()
        self.assertTrue(held.wait(2))
        coordinator.halt_puts()
        release.set()
        thread.join()
        self.assertEqual(operation.state, "not_attempted")
        self.assertEqual(operation.upload_outcome, "not_attempted")
        self.assertEqual(transport.exchange.calls, [])

    def test_get_prototype_error_does_not_strand_proving_or_keep_not_attempted(self):
        archive = b"x"

        def handler(method, url, **kwargs):
            if method == "PUT":
                return ok(201)
            raise proto.PrototypeError("unsafe_request")

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.upload_outcome, "accepted_pending_proof")
        self.assertNotEqual(operation.state, "not_attempted")
        self.assertEqual(coordinator.outstanding_proof, 0)
        self.assertEqual(coordinator.inflight_gets, 0)

    def test_multipdb_presence_keeps_per_key_and_aggregate(self):
        digest_a = proto.sha(b"pdb-a")
        digest_b = proto.sha(b"pdb-b")
        key_a = key("a.pdb", digest_a)
        key_b = key("b.pdb", digest_b)
        uploaded = {"n": 0}

        def handler(method, url, **kwargs):
            if method == "PUT":
                uploaded["n"] += 1
                return ok(201)
            if uploaded["n"] == 0:
                return ok(404, b"")
            if key_a in url:
                return ok(200, b"pdb-a")
            return ok(200, b"wrong")

        proto_exec, _, _ = harness(handler)
        proto_exec.preflight_keys([
            proto.SymbolKey(key_a, digest_a, "Elsa", b"pdb-a"),
            proto.SymbolKey(key_b, digest_b, "Elsa", b"pdb-b"),
        ])
        operation = proto.Operation("Elsa", "snupkg", proto.sha(b"s"), b"s")
        proto_exec.put_snupkg_serialized(operation, publish_key="k")
        self.assertEqual(operation.key_presence[key_a], "bytes_observed_unverified_inputs")
        self.assertEqual(operation.key_presence[key_b], "conflicting")
        self.assertEqual(operation.content_presence, "conflicting")
        self.assertNotEqual(operation.content_presence, "bytes_observed_unverified_inputs")

    def test_finalize_reaps_even_if_receipt_is_full(self):
        proto_exec, coordinator, _ = harness(lambda **kwargs: ok(200, b"{}"))
        proto_exec.receipt.max_bytes = len(json.dumps({"event": "final"}))
        proto_exec.receipt.size = proto_exec.receipt.max_bytes
        reservation = coordinator.admit_put("queued", proof_slots=1)
        self.assertNotIsInstance(reservation, str)
        result = proto_exec.finalize()
        self.assertTrue(coordinator.abandoned)
        self.assertEqual(reservation.state, "not_attempted")
        self.assertTrue(proto_exec.receipt.overflowed)
        self.assertFalse(result["durable_io_implemented"])
        self.assertFalse(result["process_cancel_reap_implemented"])
        self.assertFalse(proto.BoundedReceipt.durable_io_implemented)

    def test_snupkg_pre_observation_unverifiable_fail_closed(self):
        digest = proto.sha(b"pdb")
        symbol = key("elsa.pdb", digest)
        puts = {"n": 0}

        def handler(method, url, **kwargs):
            if method == "PUT":
                puts["n"] += 1
                return ok(201)
            return proto.ExchangeResult(None, b"", False, "timeout", proto.DEFINITELY_NOT_SENT)

        proto_exec, coordinator, _ = harness(handler)
        proto_exec.preflight_keys([proto.SymbolKey(symbol, digest, "Elsa", b"pdb")])
        operation = proto.Operation("Elsa", "snupkg", proto.sha(b"s"), b"s")
        proto_exec.put_snupkg_serialized(operation, publish_key="k")
        self.assertEqual(operation.state, "pre_observation_unverified")
        self.assertEqual(puts["n"], 0)
        self.assertTrue(coordinator.writes_halted)

    def test_metadata_role_cannot_target_feedz(self):
        proto_exec, _, transport = harness(lambda **kwargs: ok(200, b"{}"))
        with self.assertRaisesRegex(proto.PrototypeError, "unsafe_request"):
            transport.request("GET", proto.FEED_INDEX,
                              headers={"Authorization": "Bearer t"}, credential_role="metadata")

    def test_failure_category_rejects_complete_201(self):
        archive = b"x"

        def handler(method, url, **kwargs):
            if method == "PUT":
                return ok(201, complete=True, failure="throttled")
            return ok(200, archive)

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertNotEqual(operation.upload_outcome, "accepted_pending_proof")
        self.assertEqual(operation.failure_category, "throttled")
        self.assertTrue(coordinator.put_admission_stopped)


class ArchitectR3CounterexampleTests(unittest.TestCase):
    def test_delayed_proof_get_timeout_is_capped_by_remaining_minus_cleanup(self):
        clock = Clock(0.0)
        archive = b"x"
        timeouts = []

        def handler(method, url, **kwargs):
            if method == "PUT":
                clock.now = 1769  # remaining 31s
                return ok(201)
            timeouts.append(kwargs["timeout"])
            return ok(200, archive)

        proto_exec, _, transport = harness(handler, now=clock)
        proto_exec.put_and_prove_nupkg(
            proto.Operation("Elsa", "nupkg", proto.sha(archive), archive), publish_key="k")
        self.assertEqual(timeouts, [1])
        get_calls = [call for call in transport.exchange.calls if call["method"] == "GET"]
        self.assertEqual(len(get_calls), 1)
        self.assertEqual(get_calls[0]["timeout"], 1)
        self.assertLess(get_calls[0]["timeout"], proto.HTTP_CAP_SECONDS)
        self.assertLessEqual(get_calls[0]["timeout"], proto.CLEANUP_MARGIN_SECONDS)

    def test_nonpositive_usable_slice_refuses_get(self):
        clock = Clock(0.0)
        archive = b"x"

        def handler(method, url, **kwargs):
            if method == "PUT":
                clock.now = 1770  # remaining 30s, usable 0
                return ok(201)
            return ok(200, archive)

        proto_exec, coordinator, transport = harness(handler, now=clock)
        proto_exec.put_and_prove_nupkg(
            proto.Operation("Elsa", "nupkg", proto.sha(archive), archive), publish_key="k")
        self.assertIsNone(coordinator.exchange_timeout())
        self.assertEqual([call["method"] for call in transport.exchange.calls], ["PUT"])

    def test_nupkg_admission_refusal_full_receipt_releases_named_lock(self):
        proto_exec, coordinator, _ = harness(lambda **kwargs: ok(201))
        coordinator.used = 4999
        proto_exec.receipt.max_bytes = 8
        proto_exec.receipt.size = 8
        operation = proto.Operation("Elsa", "nupkg", proto.sha(b"x"), b"x")
        try:
            proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        except proto.PrototypeError as error:
            self.assertEqual(str(error), "receipt_limit")
        lock = proto_exec._lock_for("nupkg:Elsa")
        self.assertTrue(lock.acquire(timeout=0), "named lock left held after admission refusal")
        lock.release()

    def test_snupkg_admission_refusal_full_receipt_releases_named_lock(self):
        proto_exec, coordinator, _ = harness(lambda **kwargs: ok(404, b""))
        coordinator.used = 4999
        proto_exec.receipt.max_bytes = 8
        proto_exec.receipt.size = 8
        operation = proto.Operation("Elsa", "snupkg", proto.sha(b"s"), b"s")
        try:
            proto_exec.put_snupkg_serialized(operation, publish_key="k")
        except proto.PrototypeError as error:
            self.assertEqual(str(error), "receipt_limit")
        lock = proto_exec._lock_for("snupkg:Elsa")
        self.assertTrue(lock.acquire(timeout=0), "named lock left held after admission refusal")
        lock.release()


class ArchitectR4ReceiptOverflowTests(unittest.TestCase):
    def test_deadline_seconds_must_equal_max_seconds(self):
        with self.assertRaisesRegex(proto.PrototypeError, "deadline_invalid"):
            proto.Coordinator(proto.ImmutableDeadline(start=0.0, seconds=1799), clock=Clock(0.0))

    def test_pre_send_size_check_is_definitely_not_sent(self):
        puts = []

        def handler(method, url, **kwargs):
            puts.append(method)
            return ok(201) if method == "PUT" else ok(200, b"x")

        proto_exec, coordinator, _ = harness(handler)
        archive = b"x" * (proto.MAX_BYTES + 1)
        operation = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(operation, publish_key="k")
        self.assertEqual(operation.state, "not_attempted")
        self.assertEqual(operation.failure_category, "unsafe_request")
        self.assertIsNone(operation.upload_outcome)
        self.assertEqual(puts, [])
        self.assertFalse(coordinator.writes_halted)

    def test_snupkg_without_indexed_keys_does_not_put(self):
        puts = []

        def handler(method, url, **kwargs):
            puts.append(method)
            return ok(201)

        proto_exec, coordinator, _ = harness(handler)
        operation = proto.Operation("Elsa", "snupkg", proto.sha(b"s"), b"s")
        proto_exec.put_snupkg_serialized(operation, publish_key="k")
        self.assertEqual(operation.failure_category, "snupkg_keys_missing")
        self.assertEqual(operation.content_presence, "snupkg_archive_unverified")
        self.assertEqual(puts, [])
        self.assertTrue(coordinator.writes_halted)

    def test_nupkg_receipt_overflow_keeps_receipt_limit_and_halts_second_put(self):
        puts = []
        archive = b"x"

        def handler(method, url, **kwargs):
            if method == "PUT":
                puts.append(url)
                return ok(201)
            return ok(200, archive)

        proto_exec, coordinator, _ = harness(handler)
        proto_exec.receipt.max_bytes = 1
        first = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(first, publish_key="k")
        self.assertEqual(first.put_outcome, "accepted_pending_proof")
        self.assertEqual(first.content_presence, "matching")
        self.assertEqual(first.failure_category, "receipt_limit")
        self.assertNotEqual(first.failure_category, "transport_failed")
        self.assertTrue(coordinator.writes_halted)
        self.assertTrue(coordinator.put_admission_stopped)
        self.assertTrue(proto_exec.receipt.overflowed)
        self.assertEqual(len(proto_exec.receipt.rows), 0)
        self.assertEqual(puts, [proto.PACKAGE_PUBLISH])
        self.assertEqual(coordinator.outstanding_proof, 0)
        second = proto.Operation("Elsa.Other", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(second, publish_key="k")
        self.assertEqual(second.state, "not_attempted")
        self.assertEqual(second.failure_category, "receipt_limit")
        self.assertEqual(puts, [proto.PACKAGE_PUBLISH])
        self.assertEqual(len(proto_exec.receipt.rows), 0)

    def test_snupkg_receipt_overflow_keeps_receipt_limit_and_halts_second_put(self):
        digest = proto.sha(b"pdb")
        symbol = key("elsa.pdb", digest)
        puts = []
        uploaded = {"n": 0}

        def handler(method, url, **kwargs):
            if method == "PUT":
                puts.append(url)
                uploaded["n"] += 1
                return ok(201)
            if uploaded["n"]:
                return ok(200, b"pdb")
            return ok(404, b"")

        proto_exec, coordinator, _ = harness(handler)
        proto_exec.receipt.max_bytes = 1
        proto_exec.preflight_keys([proto.SymbolKey(symbol, digest, "Elsa", b"pdb")])
        first = proto.Operation("Elsa", "snupkg", proto.sha(b"s"), b"s")
        proto_exec.put_snupkg_serialized(first, publish_key="k")
        self.assertEqual(first.put_outcome, "accepted_pending_proof")
        self.assertEqual(first.failure_category, "receipt_limit")
        self.assertNotEqual(first.failure_category, "transport_failed")
        self.assertTrue(coordinator.writes_halted)
        self.assertEqual(puts, [proto.SYMBOL_PUBLISH])
        self.assertEqual(len(proto_exec.receipt.rows), 0)
        second = proto.Operation("Other", "snupkg", proto.sha(b"t"), b"t")
        proto_exec.put_snupkg_serialized(second, publish_key="k")
        self.assertEqual(second.state, "not_attempted")
        self.assertEqual(second.failure_category, "receipt_limit")
        self.assertEqual(puts, [proto.SYMBOL_PUBLISH])

    def test_receipt_overflow_retains_queued_admission_proofs_and_reap_drains_them(self):
        archive = b"x"
        puts = []

        def handler(method, url, **kwargs):
            if method == "PUT":
                puts.append(url)
                return ok(201)
            return ok(200, archive)

        proto_exec, coordinator, _ = harness(handler)
        proto_exec.receipt.max_bytes = 1
        queued = coordinator.admit_put("queued", proof_slots=1)
        self.assertNotIsInstance(queued, str)
        self.assertEqual(queued.state, "reserved")
        self.assertEqual(queued.remaining_proofs, 1)
        first = proto.Operation("Elsa", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(first, publish_key="k")
        self.assertEqual(first.failure_category, "receipt_limit")
        self.assertTrue(coordinator.writes_halted)
        self.assertEqual(queued.state, "reserved")
        self.assertEqual(queued.remaining_proofs, 1)
        self.assertEqual(coordinator.outstanding_proof, 1)
        self.assertEqual(puts, [proto.PACKAGE_PUBLISH])
        later = proto.Operation("Later", "nupkg", proto.sha(archive), archive)
        proto_exec.put_and_prove_nupkg(later, publish_key="k")
        self.assertEqual(later.state, "not_attempted")
        self.assertEqual(puts, [proto.PACKAGE_PUBLISH])
        result = proto_exec.finalize()
        self.assertEqual(queued.state, "not_attempted")
        self.assertEqual(queued.remaining_proofs, 0)
        self.assertEqual(result["unresolved_proofs"], 0)
        self.assertEqual(coordinator.outstanding_proof, 0)

    def test_pre_acquire_validation_is_definitely_not_sent(self):
        def handler(method, url, **kwargs):
            return ok(201)

        proto_exec, _, transport = harness(handler)
        oversized = proto_exec._put_exchange(proto.PACKAGE_PUBLISH, b"x" * (proto.MAX_BYTES + 1), "k")
        self.assertEqual(oversized.disposition, proto.DEFINITELY_NOT_SENT)
        self.assertEqual(oversized.failure_category, "unsafe_request")
        self.assertEqual(transport.exchange.calls, [])


class MeasurementTests(unittest.TestCase):
    def test_reconciled_dag_uses_archive_puts_as_u_and_withdraws_2529(self):
        report = proto.offline_measurement_report()
        self.assertEqual(report["architect_465"], 465)
        self.assertEqual(report["architect_U"], 462)
        self.assertEqual(report["archive_puts_U"], 462)
        self.assertEqual(report["associations_S"], 688)
        self.assertEqual(report["architect_total"], 2303)
        self.assertEqual(report["kind"], "baseline_formula_only")
        self.assertFalse(report["dag_complete"])
        self.assertFalse(report["orchestrator"])
        self.assertEqual(report["unique_symbol_keys"], "unknown")
        self.assertEqual(report["github_metadata_pages"], "unknown")
        self.assertEqual(report["overlap_rereads"], "unknown")
        self.assertEqual(report["independent_pair_counterexample"]["seconds"], 5800)
        self.assertFalse(report["independent_pair_counterexample"]["full_cold_upper_bound"])
        self.assertFalse(report["fit_claim"])
        self.assertFalse(report["safe_1800_demonstrated"])
        self.assertTrue(report["ceil_work_over_4_invalid_with_locks"])
        receipt = json.loads(
            Path(__file__).with_name("consolidated-3101-executor-measurement.json").read_text(encoding="utf-8"))
        self.assertFalse(receipt["fit_claim"])
        self.assertEqual(receipt["dag"]["architect_total"], 2303)
        self.assertFalse(receipt["dag"]["dag_complete"])
        self.assertEqual(receipt["dag"]["kind"], "baseline_formula_only")
        self.assertEqual(receipt["dag"]["withdrawn_2529"], "invalid; treated unique PDB bytes as U")
        self.assertFalse(receipt["dag"]["independent_pair_counterexample"]["full_cold_upper_bound"])
        plan = Path(__file__).with_name("consolidated-3100-readonly-get-sample-plan.md").read_text(encoding="utf-8")
        self.assertIn("TEMPLATE", plan)
        self.assertIn("No measurement in this document", plan)
        self.assertIn("3.10.0 original.zip is not in this workspace", plan)

    def test_prototype_files_declare_issue_8220_and_removal_condition(self):
        owner = "https://github.com/elsa-workflows/elsa-core/issues/8220"
        receipt = json.loads(
            Path(__file__).with_name("consolidated-3101-executor-measurement.json").read_text(encoding="utf-8"))
        self.assertEqual(receipt["owner_issue"], owner)
        self.assertIn("reviewed 3.10.1 executor", receipt["removal_condition"])
        self.assertIn("supersedes", receipt["removal_condition"])
        self.assertIn("3.10.1 Feedz path is abandoned", receipt["removal_condition"])
        self.assertFalse(receipt["fit_claim"])
        texts = [
            Path(proto.__file__).read_text(encoding="utf-8"),
            Path(__file__).read_text(encoding="utf-8"),
            Path(__file__).with_name("consolidated-3101-executor-measurement.md").read_text(encoding="utf-8"),
            Path(__file__).with_name("consolidated-3100-readonly-get-sample-plan.md").read_text(encoding="utf-8"),
        ]
        for text in texts:
            collapsed = text.replace("\n", " ")
            self.assertIn(owner, text)
            self.assertIn("reviewed 3.10.1 executor", collapsed)
            self.assertIn("3.10.1 Feedz path is abandoned", collapsed)

    def test_zero_latency_mock_is_not_a_fit_proof(self):
        archive = b"x"
        proto_exec, _, _ = harness(
            lambda method, url, **_: ok(201) if method == "PUT" else ok(200, archive))
        proto_exec.put_and_prove_nupkg(
            proto.Operation("Elsa", "nupkg", proto.sha(archive), archive), publish_key="k")
        self.assertFalse(proto.offline_measurement_report()["fit_claim"])


if __name__ == "__main__":
    unittest.main()
