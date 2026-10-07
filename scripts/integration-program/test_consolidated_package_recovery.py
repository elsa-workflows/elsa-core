"""Synthetic read-only contracts: no live feed or publication is exercised."""
from contextlib import contextmanager
from datetime import datetime, timezone
from email.message import Message
import io
import json
from pathlib import Path
import tempfile
import threading
import unittest
from unittest.mock import patch
import zipfile
import warnings

import consolidated_package_recovery as recovery
from run_consolidated_recovery_fixture import SimulatedFeed


def package_bytes(package_id, *, version=recovery.VERSION, source=recovery.SOURCE, dependency="1.0.0", payload=b"original", signature=False):
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, "w") as archive:
        archive.writestr(f"{package_id}.nuspec", f'''<package><metadata><id>{package_id}</id><version>{version}</version><repository commit="{source}"/><dependencies><group targetFramework="net8.0"><dependency id="External.Library" version="{dependency}"/></group></dependencies></metadata></package>''')
        archive.writestr("lib/net8.0/Synthetic.dll", payload)
        if signature:
            # Synthetic marker tests payload exclusion; this does not verify a signature.
            archive.writestr(".signature.p7s", b"synthetic-signature")
    return stream.getvalue()


def provenance(root):
    return {"candidate_producer": {"version": recovery.VERSION, "source_commit": recovery.SOURCE, "run_id": 37456860080, "run_attempt": 1, "artifact_id": 11412848210},
            "artifact_name": f"consolidated-candidate-{recovery.SOURCE}-37456860080-1", "archive_sha256": "a" * 64, "archive_size": 92659861,
            "original_envelope_sha256": "b" * 64, "preupload_manifest_sha256": recovery._sha((root / "preupload-manifest.json").read_bytes()),
            "verified_artifacts_sha256": recovery._sha((root / "verified-artifacts.json").read_bytes()), "inventory_sha256": recovery._sha(json.dumps(__import__("prepare_consolidated_release_candidate").inventory_identity(json.loads((root / "verified-artifacts.json").read_text())), sort_keys=True, separators=(",", ":")).encode()),
            "live_retrieval_sha256": "c" * 64, "observed_at": datetime.now(timezone.utc).isoformat(),
            "planner": {"source_commit": "d" * 40, "run_id": None, "run_attempt": None}}


class RecoveryContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        temporary = tempfile.TemporaryDirectory()
        cls.addClassCleanup(temporary.cleanup)
        cls.root = Path(temporary.name)
        (cls.root / "artifacts").mkdir()
        rows, files = [], []
        for index in range(recovery.PACKAGE_COUNT):
            package_id = "Elsa.SamplePackage" if index == 0 else f"Elsa.Synthetic{index:03}"
            data, row = package_bytes(package_id), {"id": package_id, "project": f"src/{package_id}.csproj"}
            for extension in ("nupkg", "snupkg"):
                name = f"{package_id}.{recovery.VERSION}.{extension}"
                (cls.root / "artifacts" / name).write_bytes(data)
                row[extension], row[f"{extension}_sha256"] = name, recovery._sha(data)
                files.append({"path": f"artifacts/{name}", "size": len(data), "sha256": recovery._sha(data)})
            rows.append(row)
        cls.manifest = {"source_commit": recovery.SOURCE, "version": recovery.VERSION, "published": False, "packages": rows,
                        "exclusions": [{"id": "Synthetic.Excluded", "project": "relative.csproj", "reason": "nonpackable"}]}
        (cls.root / "verified-artifacts.json").write_text(json.dumps(cls.manifest))
        (cls.root / "preupload-manifest.json").write_text(json.dumps({"files": files}))
        (cls.root / "inventory.json").write_text("{}")

    def setUp(self):
        self.feed, self.provenance = SimulatedFeed(self.root), provenance(self.root)

    def plan(self, authorization=None):
        return recovery.plan_recovery(self.root, self.provenance, transport=self.feed, authorization=authorization)

    @contextmanager
    def changed_file(self, path, data):
        old = path.read_bytes()
        try:
            path.write_bytes(data)
            yield
        finally:
            path.write_bytes(old)

    def test_all_missing_full_set_is_visibility_only(self):
        result = self.plan()
        self.assertEqual(result["counts"], {"missing": 225, "matching": 0, "conflicting": 0, "unverifiable": 0})
        self.assertTrue(result["classification_complete"])
        self.assertFalse(result["reconciliation_blocked"])
        for key in ("publication_ready", "publication_performed", "remote_symbols_verified", "content_converged"):
            self.assertFalse(result[key])
        self.assertEqual(result["access_mode"], "anonymous")
        self.assertEqual(result["absence_scope"], "declared_access_visibility_only")
        self.assertEqual(len(self.feed.calls), 226)
        self.assertEqual(len({row["id"] for row in result["packages"]}), 225)
        self.assertIn("intended_reader_access", result["pending_gates"])

    def test_all_matching_signature_aware_payload_and_distinct_raw_digest(self):
        self.feed.outcome = 200
        self.feed.overrides["elsa.samplepackage"] = recovery.ReadResult(200, package_bytes("Elsa.SamplePackage", signature=True), True)
        result = self.plan()
        self.assertTrue(result["content_converged"])
        self.assertEqual(result["counts"]["matching"], 225)
        first = result["packages"][0]
        self.assertNotEqual(first["remote"]["archive_sha256"], first["local_nupkg_sha256"])
        self.assertEqual(first["remote"]["payload_sha256"], first["local_payload_sha256"])
        self.assertFalse(result["publication_ready"])

    def test_partial_and_later_visibility_requery_remote_truth(self):
        self.feed.overrides["elsa.samplepackage"] = recovery.ReadResult(200, package_bytes("Elsa.SamplePackage"), True)
        first = self.plan()
        self.assertEqual(first["counts"]["matching"], 1)
        self.assertEqual(first["counts"]["missing"], 224)
        self.feed.outcome = 200
        self.assertTrue(self.plan()["content_converged"])
        self.assertEqual(len(self.feed.calls), 452)

    def test_each_content_mismatch_blocks_reconciliation(self):
        for category, arguments in [("identity", {"version": "3.10.1"}), ("source", {"source": "f" * 40}), ("dependencies", {"dependency": "2.0.0"}), ("payload", {"payload": b"changed"})]:
            with self.subTest(category=category):
                self.feed.overrides["elsa.samplepackage"] = recovery.ReadResult(200, package_bytes("Elsa.SamplePackage", **arguments), True)
                result = self.plan()
                self.assertTrue(result["classification_complete"])
                self.assertTrue(result["reconciliation_blocked"])
                self.assertEqual(result["packages"][0]["failure_category"], f"{category}_mismatch")
                self.assertEqual(result["counts"]["conflicting"], 1)

    def test_read_failures_never_become_absence_or_leak_bodies(self):
        cases = [recovery.ReadResult(404, b"", False), recovery.ReadResult(401, b"credential echo", True), recovery.ReadResult(403, b"", True), recovery.ReadResult(429, b"", True), recovery.ReadResult(500, b"", True), recovery.ReadResult(302, b"", True), recovery.ReadResult(200, b"bad zip", True), recovery.ReadResult(None, b"", True), recovery.ReadResult(404, b"", True, "timeout"), recovery.ReadResult(True, b"", True), recovery.ReadResult(404, b"", "yes"), recovery.ReadResult(404, b"", True, "raw-secret-error")]
        for response in cases:
            with self.subTest(response=response):
                self.feed.overrides["elsa.samplepackage"] = response
                result = self.plan()
                self.assertEqual(result["packages"][0]["classification"], "unverifiable")
                self.assertTrue(result["reconciliation_blocked"])
                self.assertFalse(result["classification_complete"])
                self.assertNotIn("credential echo", json.dumps(result))
                self.assertNotIn("raw-secret-error", json.dumps(result))

    def test_ambiguous_and_foreign_index_fail_whole_set(self):
        for bases in ([self.feed.base, self.feed.base], ["https://example.invalid/nuget/"], [self.feed.base + "?secret=value"], [self.feed.base + "../"]):
            with self.subTest(bases=bases):
                self.feed.calls.clear()
                self.feed.index = recovery.ReadResult(200, json.dumps({"version": "3.0.0", "resources": [{"@type": "PackageBaseAddress/3.0.0", "@id": base} for base in bases]}).encode(), True)
                self.assertEqual(self.plan()["counts"]["unverifiable"], 225)
                self.assertEqual(len(self.feed.calls), 1)

    def test_duplicate_index_json_keys_fail_closed(self):
        self.feed.index = recovery.ReadResult(200, b'{"version":"3.0.0","resources":[],"resources":[]}', True)
        self.assertEqual(self.plan()["counts"]["unverifiable"], 225)

    def test_last_local_symbol_pin_checked_before_first_get(self):
        path = self.root / "artifacts" / self.manifest["packages"][-1]["snupkg"]
        with self.changed_file(path, b"changed last symbols"):
            result = self.plan()
        self.assertEqual(self.feed.calls, [])
        self.assertEqual(result["counts"]["unverifiable"], 225)
        self.assertEqual({row["failure_category"] for row in result["packages"]}, {"local_input_invalid"})

    def test_changed_manifest_duplicate_and_subset_rejected_before_get(self):
        path = self.root / "verified-artifacts.json"
        for rows in (self.manifest["packages"][:-1], [self.manifest["packages"][0]] * 225):
            with self.changed_file(path, json.dumps(dict(self.manifest, packages=rows)).encode()):
                with self.assertRaisesRegex(recovery.RecoveryError, "local_input_invalid"):
                    self.plan()
                self.provenance = provenance(self.root)
                with self.assertRaisesRegex(recovery.RecoveryError, "local_input_invalid"):
                    self.plan()
            self.provenance = provenance(self.root)
        self.assertEqual(self.feed.calls, [])

    def test_interruption_retains_completed_rows_and_stops_new_gets(self):
        self.feed.overrides["elsa.synthetic002"] = KeyboardInterrupt()
        result = self.plan()
        self.assertEqual(result["counts"]["missing"], 2)
        self.assertEqual(result["counts"]["unverifiable"], 223)
        self.assertEqual(len(self.feed.calls), 4)
        self.assertEqual(result["packages"][-1]["failure_category"], "interrupted")

    def test_existing_credentials_neither_establish_scope_nor_leak(self):
        secret = "Bearer private-existing-test-value"
        self.feed.overrides["elsa.samplepackage"] = RuntimeError(secret)
        result = self.plan(secret)
        self.assertEqual(result["access_mode"], "credential_present_unverified_scope")
        self.assertNotIn(secret, json.dumps(result))
        self.assertFalse(result["publication_ready"])
        self.assertTrue(all(isinstance(call, str) and secret not in call for call in self.feed.calls))

    def test_nested_signature_suffix_cannot_hide_extra_payload(self):
        stream = io.BytesIO(package_bytes("Elsa.SamplePackage"))
        with zipfile.ZipFile(stream, "a") as archive:
            archive.writestr("lib/ignored.signature.p7s", b"extra executable payload")
        with self.assertRaises(ValueError):
            recovery._parse_package(stream.getvalue(), recovery._verifier())

    def test_unsafe_or_duplicate_remote_zip_members_are_unverifiable(self):
        for name in ("lib/net8.0/Synthetic.dll", "../outside", "C:/outside", "folder\\outside"):
            with self.subTest(name=name):
                stream = io.BytesIO(package_bytes("Elsa.SamplePackage"))
                with zipfile.ZipFile(stream, "a") as archive:
                    with warnings.catch_warnings():
                        warnings.simplefilter("ignore", UserWarning)
                        archive.writestr(name, b"ambiguous")
                self.feed.overrides["elsa.samplepackage"] = recovery.ReadResult(200, stream.getvalue(), True)
                row = self.plan()["packages"][0]
                self.assertEqual(row["classification"], "unverifiable")
                self.assertEqual(row["failure_category"], "malformed_package")

    def test_extra_private_provenance_and_relabelled_identity_rejected(self):
        for value in (dict(self.provenance, credential="secret"), dict(self.provenance, candidate_producer=dict(self.provenance["candidate_producer"], run_attempt=2)), dict(self.provenance, candidate_producer=dict(self.provenance["candidate_producer"], run_attempt=True))):
            with self.assertRaisesRegex(recovery.RecoveryError, "local_input_invalid"):
                recovery.plan_recovery(self.root, value, transport=self.feed)
        self.assertEqual(self.feed.calls, [])


class FakeResponse:
    def __init__(self, status=200, body=b"safe", headers=None, url=recovery.FEED_INDEX):
        self.code, self.body, self.url, self.closed = status, io.BytesIO(body), url, False
        self.headers = Message()
        for key, value in headers if headers is not None else [("Content-Length", str(len(body)))]:
            self.headers[key] = value

    def geturl(self):
        return self.url

    def read1(self, count):
        return self.body.read(count)

    def close(self):
        self.closed = True


class CliPathContracts(unittest.TestCase):
    def test_candidate_overlap_and_symlinked_ancestor_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            source, destination, output = root / "inputs", root / "verified", root / "receipt.json"
            recovery.validate_cli_paths(source, destination, output)
            for target in (source, source / "nested", root):
                with self.assertRaises(recovery.RecoveryError):
                    recovery.validate_cli_paths(source, target, output)
            for receipt in (source / "receipt.json", destination / "receipt.json", source, root):
                with self.assertRaises(recovery.RecoveryError):
                    recovery.validate_cli_paths(source, destination, receipt)
            linked = root / "linked"
            linked.symlink_to(root, target_is_directory=True)
            with self.assertRaises(recovery.RecoveryError):
                recovery.validate_cli_paths(source, destination, linked / "receipt.json")


class TransportContracts(unittest.TestCase):
    def read(self, response, *, authorization=None, limit=1024):
        transport = recovery.FeedzTransport()
        with patch.object(transport._opener, "open", return_value=response) as opened:
            result = transport.get(recovery.FEED_INDEX, authorization=authorization, max_bytes=limit, timeout=1)
        if opened.called:
            self.assertTrue(response.closed)
        return result, opened

    def test_get_and_credentials_restricted_to_intended_origin(self):
        result, opened = self.read(FakeResponse(), authorization="Bearer existing")
        request = opened.call_args.args[0]
        self.assertEqual(request.get_method(), "GET")
        self.assertEqual(request.get_header("Authorization"), "Bearer existing")
        self.assertEqual(result, recovery.ReadResult(200, b"safe", True))
        transport = recovery.FeedzTransport()
        with patch.object(transport._opener, "open") as opened:
            for url in ("https://evil.invalid/", "https://f.feedz.io.evil.invalid/", "http://f.feedz.io/", "https://user@f.feedz.io/"):
                self.assertEqual(transport.get(url, authorization="secret", max_bytes=1024, timeout=1).failure_category, "redirect_rejected")
            opened.assert_not_called()

    def test_redirect_not_followed_or_body_read(self):
        response = FakeResponse(302)
        response.read1 = lambda _: self.fail("redirect body read")
        result, _ = self.read(response)
        self.assertEqual(result.failure_category, "redirect_rejected")
        self.assertIsNone(recovery._NoRedirect().redirect_request(None, None, 302, "", {}, "https://evil.invalid"))

    def test_ambiguous_headers_oversize_and_incomplete(self):
        cases = [(FakeResponse(headers=[("Content-Length", "4"), ("Content-Length", "4")]), "ambiguous_response"), (FakeResponse(headers=[("Content-Length", "4"), ("Transfer-Encoding", "chunked")]), "ambiguous_response"), (FakeResponse(headers=[("Content-Encoding", "gzip")]), "ambiguous_response"), (FakeResponse(headers=[("Content-Length", "999999")]), "oversized_response"), (FakeResponse(body=b"ab", headers=[("Content-Length", "4")]), "incomplete_response"), (FakeResponse(body=b"large", headers=[("Transfer-Encoding", "chunked")]), "oversized_response")]
        for response, expected in cases:
            with self.subTest(expected=expected):
                result, _ = self.read(response, limit=4)
                self.assertFalse(result.complete)
                self.assertEqual(result.failure_category, expected)
                self.assertEqual(result.body, b"")

    def test_timeout_closes_response_without_error_text(self):
        response = FakeResponse()
        def fail(_):
            raise TimeoutError("private server message")
        response.read1 = fail
        result, _ = self.read(response)
        self.assertEqual(result.failure_category, "timeout")
        self.assertEqual(result.body, b"")

    def test_404_requires_complete_get(self):
        result, opened = self.read(FakeResponse(404, b"not found"))
        self.assertEqual(result, recovery.ReadResult(404, b"not found", True))
        self.assertEqual(opened.call_args.args[0].get_method(), "GET")

    def test_close_failure_does_not_escape_or_mask_complete_body_verdict(self):
        response = FakeResponse()
        def fail_close():
            raise OSError("private close error")
        response.close = fail_close
        transport = recovery.FeedzTransport()
        with patch.object(transport._opener, "open", return_value=response):
            self.assertEqual(transport.get(recovery.FEED_INDEX, authorization=None, max_bytes=1024, timeout=1),
                             recovery.ReadResult(200, b"safe", True))

    def test_total_deadline_bounds_blocked_open_without_spawning_more_workers(self):
        transport = recovery.FeedzTransport()
        released, finished = threading.Event(), threading.Event()
        response = FakeResponse()
        def delayed_open(*args, **kwargs):
            released.wait(2)
            finished.set()
            return response
        try:
            with patch.object(transport._opener, "open", side_effect=delayed_open) as opened:
                result = transport.get(recovery.FEED_INDEX, authorization=None, max_bytes=1024, timeout=0.01)
                self.assertEqual(result.failure_category, "timeout")
                second = transport.get(recovery.FEED_INDEX, authorization=None, max_bytes=1024, timeout=0.01)
                self.assertEqual(second.failure_category, "timeout")
                self.assertEqual(opened.call_count, 1)
                released.set()
                self.assertTrue(finished.wait(1))
        finally:
            released.set()

    def test_header_injection_not_sent(self):
        result, opened = self.read(FakeResponse(), authorization="Bearer secret\r\nOther: injected")
        opened.assert_not_called()
        self.assertEqual(result.failure_category, "authentication_failed")


if __name__ == "__main__":
    unittest.main()
