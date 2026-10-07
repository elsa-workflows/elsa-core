"""Synthetic complete inventory and real loopback HTTP; never publish externally."""
import copy
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import io
import json
import os
import shutil
from pathlib import Path
import subprocess
import sys
import tempfile
import threading
import time
import unittest
from unittest.mock import patch
import zipfile

import consolidated_package_executor as executor
import consolidated_package_recovery as recovery
import test_consolidated_package_recovery as recovery_fixtures
from test_consolidated_candidate_clock import AFTER_EXPIRY, candidate_clock, install_candidate_clock
from test_consolidated_package_recovery import package_bytes, provenance


def loopback_server(test, handler):
    server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    def cleanup():
        server.shutdown()
        server.server_close()
        thread.join(2)
    test.addCleanup(cleanup)
    return server, f"http://127.0.0.1:{server.server_port}"


class FakeInspector:
    digest = "f"*64
    source_digest = "e"*64

    def inspect(self, dll, pdb, name):
        digest = executor.sha(pdb)
        guid = f"{digest[:8]}-{digest[8:12]}-{digest[12:16]}-{digest[16:20]}-{digest[20:32]}"
        return {"schema": 1, "symbol": {"key": f"{name.lower()}/{digest[:32]}FFFFFFFF/{name.lower()}",
                "pdb_name": name.lower(), "guid": guid, "stamp": 42, "checksum_algorithm": "SHA256",
                "declared_checksum": "a"*64, "normalized_checksum": "a"*64,
                "pdb_sha256": digest, "pdb_size": len(pdb)},
                "details": {"assembly_name": "Synthetic", "assembly_version": "3.10.0.0",
                "informational_version": f"3.10.0+{recovery.SOURCE}",
                "source_link": {"documents": {"/_/*": f"{executor.packages.RAW_URL}{recovery.SOURCE}/*"}},
                "documents": [{"path": "/_/source.cs", "algorithm": "sha256", "checksum": "b"*64,
                               "embedded_checksum": "b"*64}]}}


class DuplicateRejectingSymbols(executor.SimulatedTransport):
    """A fake service that rejects an original archive containing any known key."""
    def __init__(self, *args):
        super().__init__(*args)
        self.attempts = []

    def put(self, kind, data, key):
        self.attempts.append({"kind": kind, "archive_sha256": executor.sha(data)})
        if kind == "snupkg":
            with zipfile.ZipFile(io.BytesIO(data)) as archive:
                members = {archive.read(name) for name in archive.namelist() if name.endswith(".pdb")}
            keys = {name for name, item in self.expected.items() if item["pdb"] in members}
            if keys.intersection(self.symbols):
                return recovery.ReadResult(409, b"private-duplicate-response", True)
        return super().put(kind, data, key)


class FullInventoryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        temporary = tempfile.TemporaryDirectory()
        cls.addClassCleanup(temporary.cleanup)
        cls.root = Path(temporary.name).resolve()
        (cls.root / "artifacts").mkdir()
        baseline = json.loads(Path(executor.__file__).with_name("consolidated-candidate-inventory-baseline.json").read_text())
        rows, files, sources = [], [], []
        for source in baseline["packages"]:
            package_id = source["id"]
            row = {"id": package_id, "project": source["project"],
                   "framework_properties": {"net8.0": {"assembly_name": "Synthetic", "include_build_output": True}},
                   "assemblies": [{"framework": "net8.0", "assembly": "lib/net8.0/Synthetic.dll", "pdb": "lib/net8.0/Synthetic.pdb"}]}
            for kind in ("nupkg", "snupkg"):
                data = package_bytes(package_id)
                if kind == "snupkg":
                    content = io.BytesIO()
                    with zipfile.ZipFile(io.BytesIO(data)) as original, zipfile.ZipFile(content, "w") as target:
                        for member in original.namelist():
                            if not member.endswith(".dll"):
                                target.writestr(member, original.read(member))
                        target.writestr("lib/net8.0/Synthetic.pdb", b"BSJB-"+package_id.encode())
                    data = content.getvalue()
                name = f"{package_id}.3.10.0.{kind}"
                (cls.root / "artifacts" / name).write_bytes(data)
                row[kind], row[kind+"_sha256"] = name, executor.sha(data)
                files.append({"path": "artifacts/"+name, "size": len(data), "sha256": executor.sha(data)})
            rows.append(row)
            sources.append({"id": package_id, "frameworks": [{"framework": "net8.0", "documents": [
                {"path": "source.cs", "algorithm": "sha256", "checksum": "b"*64, "embedded": True}]}]})
        cls.manifest = {"version": "3.10.0", "source_commit": recovery.SOURCE, "published": False,
                        "packages": rows, "exclusions": baseline["exclusions"]}
        (cls.root / "verified-artifacts.json").write_text(json.dumps(cls.manifest))
        (cls.root / "preupload-manifest.json").write_text(json.dumps({"files": files}))
        (cls.root / "receipt.json").write_text(json.dumps({"provenance": sources}))
        with candidate_clock(recovery_fixtures):
            cls.provenance = provenance(cls.root)

    def setUp(self):
        install_candidate_clock(self, executor, recovery, recovery_fixtures)
        self.inspector = FakeInspector()
        self.associations, self.expected = executor.associations(self.root, self.manifest, self.inspector)
        self.feed = executor.SimulatedTransport(self.root, self.manifest, self.expected)

    def run_executor(self, *, mode="publish", authorize=None):
        return executor.run_verified(self.root, self.provenance, self.inspector, mode=mode,
            transport=self.feed, authorize=authorize or (lambda: {"scope": "simulation_only"}), credential=lambda: "private-synthetic-key")

    def fill_feed(self):
        for row in self.manifest["packages"]:
            self.feed.accepted[row["nupkg"].lower()] = (self.root / "artifacts" / row["nupkg"]).read_bytes()
        self.feed.symbols.update({key: value["pdb"] for key, value in self.expected.items()})

    def test_all_225_pairs_and_124_exclusions_converge(self):
        result = self.run_executor()
        self.assertEqual(result["result"], "content_verified")
        self.assertEqual(len(result["operations"]), 450)
        self.assertEqual(len(result["associations"]), 225)
        self.assertEqual(result["after"]["packages"]["counts"]["matching"], 225)
        self.assertEqual(result["after"]["packages"]["exclusion_count"], 124)
        self.assertEqual(len(result["after"]["symbols"]), 225)
        self.assertEqual(len(self.feed.calls), 450)
        self.assertFalse(result["publication_ready"])
        self.assertFalse(result["remote_snupkg_archive_verified"])
        self.assertEqual(result["scope"], "injected_simulation")
        for phase in ("before", "after"):
            self.assertEqual(result[phase]["packages"]["observation_mode"], "injected_simulation")
        self.assertNotIn("private-synthetic-key", json.dumps(result))
        self.assertTrue(self.run_executor()["content_verified"])
        self.assertEqual(len(self.feed.calls), 450)

    def test_verify_missing_is_complete_observation_not_release_acceptance(self):
        result = self.run_executor(mode="verify", authorize=lambda: self.fail("verification requested authority"))
        self.assertEqual(result["result"], "verified")
        self.assertFalse(result["content_verified"])
        self.assertFalse(result["publication_performed"])
        self.assertEqual(self.feed.calls, [])

    def test_default_reader_preserves_production_observation_provenance(self):
        with patch.object(executor.HttpTransport, "get", side_effect=self.feed.get) as reader, \
                patch.object(self.inspector, "deadline", time.monotonic() + 300, create=True):
            result = executor.run_verified(self.root, self.provenance, self.inspector, mode="verify")
        self.assertEqual(result["result"], "verified")
        self.assertEqual(result["scope"], "production")
        self.assertEqual(result["before"]["packages"]["observation_mode"], "production")
        self.assertEqual(reader.call_count, 452)  # Two index reads, all packages, all PDBs.
        self.assertFalse(result["publication_performed"])
        self.assertFalse(result["upload_attempted"])
        self.assertEqual(self.feed.calls, [])

    def test_package_present_symbol_missing_and_arbitrary_partial(self):
        for partial in (False, True):
            self.fill_feed()
            if partial:
                self.feed.accepted = {key: value for index, (key, value) in enumerate(self.feed.accepted.items()) if index % 3 == 1}
            self.feed.symbols.clear()
            self.feed.calls.clear()
            result = self.run_executor()
            self.assertTrue(result["content_verified"])
            self.assertEqual(len(self.feed.calls), 375 if partial else 225)
            self.assertEqual(sum(row["kind"] == "snupkg" for row in self.feed.calls), 225)

    def test_accepted_then_interrupted_resume_without_duplicate(self):
        self.feed.interrupt_next = True
        result = self.run_executor()
        self.assertEqual(result["failure_category"], "upload_acceptance_unknown")
        self.assertEqual(len(result["operations"]), 450)
        self.assertEqual(len(self.feed.calls), 1)
        self.assertTrue(self.run_executor()["content_verified"])
        self.assertEqual(len(self.feed.calls), 450)

    def test_409_and_uncertain_results_stop_without_retry(self):
        for response in (recovery.ReadResult(409, b"raw-private-body", True), recovery.ReadResult(401, b"secret", True),
                         recovery.ReadResult(None, b"", False, "timeout"), recovery.ReadResult(200, b"", True)):
            with self.subTest(status=response.status), patch.object(self.feed, "put", return_value=response) as put:
                result = self.run_executor()
                self.assertEqual(result["failure_category"], "upload_acceptance_unknown")
                self.assertEqual(put.call_count, 1)
                self.assertNotIn("raw-private-body", json.dumps(result))

    def test_delayed_visibility_does_not_certify_or_retry(self):
        put = self.feed.put
        def invisible(kind, data, key):
            result = put(kind, data, key)
            self.feed.accepted.clear()
            self.feed.symbols.clear()
            return result
        with patch.object(self.feed, "put", side_effect=invisible):
            result = self.run_executor()
        self.assertFalse(result["content_verified"])
        self.assertEqual(result["failure_category"], "readback_incomplete")
        self.assertEqual(len(self.feed.calls), 450)

    def test_package_and_pdb_conflicts_or_unreadable_block_every_upload(self):
        first = self.manifest["packages"][0]
        key = next(iter(self.expected))
        cases = [(first["nupkg"].lower(), recovery.ReadResult(200, package_bytes(first["id"], payload=b"conflict"), True)),
                 (first["nupkg"].lower(), recovery.ReadResult(403, b"private", True)),
                 (key, recovery.ReadResult(200, b"wrong-PDB", True)), (key, recovery.ReadResult(200, b"PK-not-PDB", True)),
                 (key, recovery.ReadResult(403, b"private", True)), (key, recovery.ReadResult(404, b"", False))]
        for suffix, response in cases:
            original = self.feed.get
            def get(url, **kwargs):
                return response if url.endswith(suffix) else original(url, **kwargs)
            with patch.object(self.feed, "get", side_effect=get):
                result = self.run_executor()
            self.assertEqual(result["failure_category"], "reconciliation_blocked")
            self.assertFalse(result["publication_performed"])
            self.assertEqual(self.feed.calls, [])

    def test_changed_or_duplicate_package_base_blocks_before_upload(self):
        original = json.loads(self.feed.index)
        for value in ("https://f.feedz.io/elsa-workflows/elsa-3/nuget/another-tree", None):
            index = copy.deepcopy(original)
            if value is None:
                index["resources"].append(copy.deepcopy(index["resources"][0]))
            else:
                index["resources"][0]["@id"] = value
            self.feed.index = json.dumps(index).encode()
            result = self.run_executor(authorize=lambda: self.fail("changed index requested authority"))
            self.assertEqual(result["failure_category"], "feed_resource_mismatch")
            self.assertEqual(self.feed.calls, [])

    def test_index_drift_blocks_admission_and_final_content_acceptance(self):
        original_get = self.feed.get
        changed = json.loads(self.feed.index)
        changed["comment"] = "changed snapshot"
        for change_after in (1, 2):
            index_reads = 0
            def get(url, **kwargs):
                nonlocal index_reads
                if url == recovery.FEED_INDEX:
                    index_reads += 1
                    if index_reads > change_after:
                        return recovery.ReadResult(200, json.dumps(changed).encode(), True)
                return original_get(url, **kwargs)
            self.feed.accepted.clear()
            self.feed.symbols.clear()
            self.feed.calls.clear()
            with patch.object(self.feed, "get", side_effect=get):
                result = self.run_executor()
            self.assertEqual(result["result"], "failed")
            self.assertFalse(result["content_verified"])
            observed = result["before"] if change_after == 1 else result["after"]
            self.assertFalse(observed["feed_index_consistent"])
            self.assertTrue(observed["blocked"])
            self.assertEqual(len(self.feed.calls), 0 if change_after == 1 else 450)

    def test_authority_failure_never_uses_key(self):
        def blocked():
            raise executor.ExecutorError("operational_policy_unconfigured")
        result = executor.run_verified(self.root, self.provenance, self.inspector, mode="publish", transport=self.feed,
            authorize=blocked, credential=lambda: self.fail("credential read before authority"))
        self.assertEqual(result["failure_category"], "operational_policy_unconfigured")
        self.assertIsNotNone(result["before"])
        self.assertEqual(self.feed.calls, [])

    def test_changed_archive_after_reads_blocks_mutation(self):
        path = self.root / "artifacts" / self.manifest["packages"][0]["nupkg"]
        original = path.read_bytes()
        def mutate():
            path.write_bytes(b"changed")
            return {"scope": "simulation_only"}
        try:
            result = self.run_executor(authorize=mutate)
            self.assertEqual(result["failure_category"], "original_archive_changed")
            self.assertEqual(self.feed.calls, [])
        finally:
            path.write_bytes(original)

    def test_durable_snapshot_precedes_each_upload_and_survives_partial_failure(self):
        snapshots = []
        def checkpoint(ledger):
            snapshots.append(copy.deepcopy(ledger))
        original = self.feed.put
        def put(kind, data, key):
            self.assertEqual(snapshots[-1]["operations"][0]["state"], "acceptance_unknown")
            return original(kind, data, key)
        self.feed.interrupt_next = True
        with patch.object(self.feed, "put", side_effect=put):
            result = executor.run_verified(self.root, self.provenance, self.inspector, mode="publish", transport=self.feed,
                authorize=lambda: {"scope": "simulation_only"}, credential=lambda: "synthetic", checkpoint=checkpoint)
        self.assertEqual(result["failure_category"], "upload_acceptance_unknown")
        self.assertEqual(len(snapshots[-1]["operations"]), 450)
        self.assertEqual(snapshots[-1]["operations"][0]["state"], "acceptance_unknown")

    def test_expiry_blocks_before_network(self):
        with patch.object(executor, "EXPIRY", "2000-01-01T00:00:00Z"), patch.object(self.feed, "get") as get:
            self.assertEqual(self.run_executor()["failure_category"], "candidate_expired")
        get.assert_not_called()

    def test_historical_expiry_blocks_under_future_clock_before_network(self):
        with candidate_clock(executor, at=AFTER_EXPIRY), patch.object(self.feed, "get") as get:
            self.assertEqual(self.run_executor()["failure_category"], "candidate_expired")
        get.assert_not_called()

    def test_wrong_local_guid_stamp_checksum_hash_sourcelink_documents(self):
        mutations = [lambda x: x["symbol"].update(guid="0"*32), lambda x: x["symbol"].update(stamp=-1),
                     lambda x: x["symbol"].update(normalized_checksum="c"*64), lambda x: x["symbol"].update(pdb_sha256="c"*64),
                     lambda x: x["details"].update(source_link={"documents": {"*": "https://wrong.invalid/*"}}),
                     lambda x: x["details"]["documents"][0].update(checksum="c"*64)]
        original = self.inspector.inspect
        for mutate in mutations:
            def inspect(*args):
                value = original(*args)
                mutate(value)
                return value
            with patch.object(self.inspector, "inspect", side_effect=inspect):
                result = self.run_executor()
            self.assertEqual(result["result"], "failed")
            self.assertFalse(result["publication_performed"])
        self.assertEqual(self.feed.calls, [])

    def test_incomplete_framework_coverage(self):
        manifest = copy.deepcopy(self.manifest)
        manifest["packages"][0]["framework_properties"]["net9.0"] = {"include_build_output": True}
        with self.assertRaisesRegex(executor.ExecutorError, "assembly_coverage_invalid"):
            executor.associations(self.root, manifest, self.inspector)

    def test_inconsistent_duplicate_keys(self):
        original = self.inspector.inspect
        first = original(b"", next(iter(self.expected.values()))["pdb"], "Synthetic.pdb")
        def inspect(*args):
            value = original(*args)
            value["symbol"].update(key=first["symbol"]["key"], guid=first["symbol"]["guid"])
            return value
        with patch.object(self.inspector, "inspect", side_effect=inspect), self.assertRaisesRegex(executor.ExecutorError, "duplicate_symbol_key_conflict"):
            executor.associations(self.root, self.manifest, self.inspector)

    def test_remote_guid_stamp_or_checksum_change_is_unknown(self):
        self.fill_feed()
        original = self.inspector.inspect
        for field, value in (("guid", "0"*32), ("stamp", 43), ("normalized_checksum", "d"*64)):
            calls = 0
            def inspect(*args):
                nonlocal calls
                calls += 1
                result = original(*args)
                if calls > 225:
                    result["symbol"][field] = value
                return result
            with patch.object(self.inspector, "inspect", side_effect=inspect):
                result = self.run_executor()
            self.assertEqual(result["failure_category"], "reconciliation_blocked")
            self.assertEqual(self.feed.calls, [])

    def test_consistent_duplicate_keys_retain_each_association(self):
        first, second = self.manifest["packages"][:2]
        path = self.root / "artifacts" / second["snupkg"]
        original = path.read_bytes()
        with zipfile.ZipFile(self.root / "artifacts" / first["snupkg"]) as archive:
            pdb = archive.read(first["assemblies"][0]["pdb"])
        output = io.BytesIO()
        with zipfile.ZipFile(io.BytesIO(original)) as archive, zipfile.ZipFile(output, "w") as target:
            for member in archive.namelist():
                target.writestr(member, pdb if member.endswith(".pdb") else archive.read(member))
        try:
            path.write_bytes(output.getvalue())
            associations, expected = executor.associations(self.root, self.manifest, self.inspector)
            self.assertEqual(len(associations), 225)
            self.assertEqual(sum(len(row["associations"]) for row in associations), 225)
            self.assertEqual(len(expected), 224)
        finally:
            path.write_bytes(original)

    def overlapping_inventory(self, *, mixed=False):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)/"verified"
        shutil.copytree(self.root, root)
        manifest = copy.deepcopy(self.manifest)
        first, second = manifest["packages"][:2]
        with zipfile.ZipFile(root/"artifacts"/first["snupkg"]) as archive:
            shared = archive.read(first["assemblies"][0]["pdb"])
        path = root/"artifacts"/second["snupkg"]
        with zipfile.ZipFile(path) as archive:
            members = {name: archive.read(name) for name in archive.namelist()}
        unique = members[second["assemblies"][0]["pdb"]]
        members[second["assemblies"][0]["pdb"]] = shared
        if mixed:
            frame = copy.deepcopy(second["assemblies"][0])
            frame.update(framework="net9.0", assembly="lib/net9.0/Synthetic.dll", pdb="lib/net9.0/Synthetic.pdb")
            second["assemblies"].append(frame)
            second["framework_properties"]["net9.0"] = copy.deepcopy(second["framework_properties"]["net8.0"])
            members[frame["pdb"]] = unique
            nupkg = root/"artifacts"/second["nupkg"]
            with zipfile.ZipFile(nupkg) as archive:
                assembly_members = {name: archive.read(name) for name in archive.namelist()}
            assembly_members[frame["assembly"]] = assembly_members[second["assemblies"][0]["assembly"]]
            with zipfile.ZipFile(nupkg, "w") as archive:
                for name, data in assembly_members.items():
                    archive.writestr(name, data)
            proof = json.loads((root/"receipt.json").read_text())
            source = next(row for row in proof["provenance"] if row["id"] == second["id"])
            source["frameworks"].append({**copy.deepcopy(source["frameworks"][0]), "framework": "net9.0"})
            (root/"receipt.json").write_text(json.dumps(proof))
        with zipfile.ZipFile(path, "w") as archive:
            for name, data in members.items():
                archive.writestr(name, data)
        preupload = json.loads((root/"preupload-manifest.json").read_text())
        for kind in ("nupkg", "snupkg"):
            path = root/"artifacts"/second[kind]
            second[kind+"_sha256"] = executor.sha(path.read_bytes())
            pin = next(row for row in preupload["files"] if row["path"] == "artifacts/"+second[kind])
            pin.update(sha256=second[kind+"_sha256"], size=path.stat().st_size)
        (root/"preupload-manifest.json").write_text(json.dumps(preupload))
        (root/"verified-artifacts.json").write_text(json.dumps(manifest))
        _, expected = executor.associations(root, manifest, self.inspector)
        feed = DuplicateRejectingSymbols(root, manifest, expected)
        return root, provenance(root), manifest, feed

    def publish_overlap(self, root, original_provenance, feed):
        return executor.run_verified(root, original_provenance, self.inspector, mode="publish", transport=feed,
            authorize=lambda: {"scope": "simulation_only"}, credential=lambda: "synthetic")

    def test_shared_missing_keys_skip_second_archive_only_after_actual_pdb_readback(self):
        root, original_provenance, manifest, feed = self.overlapping_inventory()
        result = self.publish_overlap(root, original_provenance, feed)
        self.assertTrue(result["content_verified"])
        self.assertEqual(len(result["operations"]), 450)
        self.assertEqual(len(result["associations"]), 225)
        self.assertEqual(sum(len(row["associations"]) for row in result["associations"]), 225)
        self.assertEqual(len(feed.attempts), 449)
        operation = result["operations"][3]
        self.assertEqual(operation["archive_sha256"], manifest["packages"][1]["snupkg_sha256"])
        self.assertEqual(operation["state"], "pdbs_already_matching_archive_unverified")
        self.assertIsNone(operation["status"])
        self.assertEqual([row["classification"] for row in operation["overlap_readback"]], ["matching"])
        self.assertEqual(len(result["after"]["symbols"]), 224)
        self.assertFalse(result["remote_snupkg_archive_verified"])
        self.assertFalse(result["publication_ready"])

    def test_missing_delayed_conflicting_or_unreadable_overlap_stops_before_duplicate_put(self):
        responses = (recovery.ReadResult(404, b"", True), recovery.ReadResult(200, b"wrong-PDB", True),
                     recovery.ReadResult(403, b"private-response", True), recovery.ReadResult(429, b"", True),
                     recovery.ReadResult(200, b"", False, "incomplete_response"), recovery.ReadResult(None, b"", False, "timeout"))
        for response in responses:
            with self.subTest(status=response.status):
                root, original_provenance, _, feed = self.overlapping_inventory()
                get = feed.get
                def unreadable(url, **kwargs):
                    if url.startswith(executor.SYMBOL_PUBLISH+"/") and feed.symbols:
                        return response
                    return get(url, **kwargs)
                with patch.object(feed, "get", side_effect=unreadable):
                    result = self.publish_overlap(root, original_provenance, feed)
                self.assertEqual(result["failure_category"], "symbol_overlap_unverified")
                self.assertEqual(len(feed.attempts), 3)
                self.assertEqual(result["operations"][3]["state"], "not_attempted")
                self.assertEqual(result["operations"][3]["failure_category"], "symbol_overlap_unverified")
                self.assertFalse(result["content_verified"])
                self.assertNotIn("private-response", json.dumps(result))

    def test_mixed_shared_and_new_keys_preserve_original_archive_and_stop_on_provider_409(self):
        root, original_provenance, manifest, feed = self.overlapping_inventory(mixed=True)
        result = self.publish_overlap(root, original_provenance, feed)
        self.assertEqual(result["failure_category"], "upload_acceptance_unknown")
        self.assertEqual(len(feed.attempts), 4)
        self.assertEqual(feed.attempts[-1], {"kind": "snupkg", "archive_sha256": manifest["packages"][1]["snupkg_sha256"]})
        self.assertEqual([row["classification"] for row in result["operations"][3]["overlap_readback"]], ["matching"])
        self.assertEqual(result["operations"][3]["status"], 409)
        self.assertEqual(result["operations"][4]["state"], "not_attempted")
        self.assertNotIn("private-duplicate-response", json.dumps(result))

    def test_uncertain_symbol_upload_never_reaches_overlap_or_retry(self):
        root, original_provenance, _, feed = self.overlapping_inventory()
        put = feed.put
        def interrupted(kind, data, key):
            response = put(kind, data, key)
            return recovery.ReadResult(None, b"", False, "timeout") if kind == "snupkg" else response
        with patch.object(feed, "put", side_effect=interrupted):
            result = self.publish_overlap(root, original_provenance, feed)
        self.assertEqual(result["failure_category"], "upload_acceptance_unknown")
        self.assertEqual(len(feed.attempts), 2)
        self.assertNotIn("overlap_readback", result["operations"][3])
        self.assertEqual(result["operations"][2]["state"], "not_attempted")

    def test_assembly_free_accounting_does_not_drop_pair(self):
        manifest = copy.deepcopy(self.manifest)
        manifest["packages"][0]["assemblies"] = []
        manifest["packages"][0]["framework_properties"]["net8.0"]["include_build_output"] = False
        path = self.root / "receipt.json"
        original = path.read_bytes()
        source = json.loads(original)
        source["provenance"][0].update(assembly_free=True, frameworks=[])
        try:
            path.write_text(json.dumps(source))
            associations, expected = executor.associations(self.root, manifest, self.inspector)
            self.assertEqual(len(associations), 225)
            self.assertTrue(associations[0]["assembly_free"])
            self.assertEqual(len(expected), 224)
        finally:
            path.write_bytes(original)

    def test_full_original_rehearsal(self):
        result = executor.rehearse(self.root, self.provenance, self.inspector)
        self.assertEqual(result["result"], "passed")
        self.assertEqual(result["simulated_upload_count"], 450)
        self.assertFalse(result["publication_performed"])
        self.assertTrue(result["resumed"]["content_verified"])


class FakeApi:
    def __init__(self, rows):
        self.rows, self.calls = rows, []
    def get(self, path):
        self.calls.append(path)
        value = self.rows[path]
        if isinstance(value, BaseException):
            raise value
        return copy.deepcopy(value)


class AdmissionTests(unittest.TestCase):
    def setUp(self):
        self.policy = {"environment_id": 11, "reviewer_rule_id": 12, "branch_rule_id": 15, "reviewer_ids": [13],
                       "branch_policies": [{"id": 14, "type": "branch", "name": "main"}],
                       "allowed_ref": "refs/heads/main", "operational_packet_sha256": "a"*64}
        self.context = {"GITHUB_ACTIONS": "true", "GITHUB_REPOSITORY": executor.REPOSITORY,
                        "GITHUB_SHA": "a"*40, "GITHUB_RUN_ID": "99", "GITHUB_RUN_ATTEMPT": "1",
                        "GITHUB_EVENT_NAME": "workflow_dispatch", "GITHUB_JOB": "publish",
                        "GITHUB_REF": self.policy["allowed_ref"], "GITHUB_WORKFLOW_SHA": "a"*40,
                        "GITHUB_WORKFLOW_REF": f"{executor.REPOSITORY}/{executor.WORKFLOW}@{self.policy['allowed_ref']}"}
        self.prefix = f"repos/{executor.REPOSITORY}"
        self.run = self.prefix+"/actions/runs/99/attempts/1"
        self.environment = self.prefix+"/environments/"+executor.ENVIRONMENT
        self.approvals = self.prefix+"/actions/runs/99/approvals"
        self.api = FakeApi({
            self.run: {"id": 99, "run_attempt": 1, "head_sha": "a"*40, "event": "workflow_dispatch",
                       "head_branch": "main", "head_repository": {"full_name": executor.REPOSITORY},
                       "path": executor.WORKFLOW, "status": "in_progress", "repository": {"full_name": executor.REPOSITORY},
                       "triggering_actor": {"id": 20}},
            self.environment: {"id": 11, "name": executor.ENVIRONMENT, "can_admins_bypass": False,
                "protection_rules": [{"type": "required_reviewers", "id": 12, "prevent_self_review": True,
                                      "reviewers": [{"type": "User", "reviewer": {"id": 13}}]},
                                     {"type": "branch_policy", "id": 15}],
                "deployment_branch_policy": {"protected_branches": False, "custom_branch_policies": True}},
            self.environment+"/deployment-branch-policies?per_page=100&page=1": {"total_count": 1, "branch_policies": self.policy["branch_policies"]},
            self.prefix+"/actions/secrets?per_page=100&page=1": {"total_count": 0, "secrets": []},
            self.prefix+"/actions/organization-secrets?per_page=100&page=1": {"total_count": 0, "secrets": []},
            self.environment+"/secrets?per_page=100&page=1": {"total_count": 2, "secrets": [{"name": executor.PUBLISH_SECRET}, {"name": executor.METADATA_SECRET}]},
            self.approvals: [{"state": "approved", "environments": [{"id": 11, "name": executor.ENVIRONMENT}], "user": {"id": 13}}]})
        envelope = json.loads(executor.candidate_input.ORIGINAL_ENVELOPE.read_text())
        self.api.rows[self.prefix+f"/actions/artifacts/{executor.candidate_input.ARTIFACT}"] = {
            "id": envelope["artifact_id"], "name": envelope["artifact_name"], "expired": False,
            "expires_at": envelope["expires_at"], "digest": "sha256:"+envelope["archive_sha256"],
            "size_in_bytes": envelope["archive_size"],
            "workflow_run": {"id": envelope["run_id"], "head_sha": envelope["source_commit"]}}
        def git(command, **kwargs):
            return subprocess.CompletedProcess(command, 0, stdout=(b"a"*40+b"\n") if command[1] == "rev-parse" else b"", stderr=b"")
        self.git = patch.object(executor.subprocess, "run", side_effect=git)
        self.git_calls = self.git.start()
        self.addCleanup(self.git.stop)

    def admit(self, *, publish=True):
        with patch.object(executor, "REVIEWED_OPERATIONAL_POLICY", self.policy), patch.dict(os.environ, self.context, clear=True):
            return executor.check_admission(publish=publish, api=self.api)

    def test_no_boolean_environment_or_receipt_can_override_missing_policy(self):
        with patch.dict(os.environ, {**self.context, "APPROVED": "true", "REVIEWED_OPERATIONAL_POLICY": json.dumps(self.policy)}):
            with self.assertRaisesRegex(executor.ExecutorError, "operational_policy_unconfigured"):
                executor.check_admission(api=self.api)
        self.assertEqual(self.api.calls, [])

    def test_admission_git_children_receive_no_credentials(self):
        self.context.update({executor.PUBLISH_SECRET: "synthetic-publisher", executor.METADATA_SECRET: "synthetic-reader",
                             "GH_TOKEN": "synthetic-artifact", "PATH": "/usr/bin:/bin"})
        self.admit()
        self.assertEqual(self.git_calls.call_count, 2)
        for call in self.git_calls.call_args_list:
            self.assertEqual(call.kwargs["env"], {"PATH": "/usr/bin:/bin"})

    def test_native_approval_and_exact_preexisting_policy(self):
        receipt = self.admit()
        self.assertTrue(receipt["native_approval_verified"])
        self.assertEqual(receipt["run_id"], 99)
        self.assertEqual(receipt["environment_id"], 11)
        self.assertTrue(receipt["credential_provenance_verified"])
        self.assertEqual(receipt["credential_isolation"], "both_names_environment_only_metadata_observed")
        self.assertEqual(len(receipt["credential_metadata"]), 3)
        self.assertLess(self.api.calls.index(self.approvals), self.api.calls.index(self.prefix+"/actions/secrets?per_page=100&page=1"))

    def test_preschedule_requires_policy_but_not_future_approval(self):
        self.api.rows[self.approvals] = []
        receipt = self.admit(publish=False)
        self.assertFalse(receipt["native_approval_verified"])
        self.assertFalse(receipt["credential_provenance_verified"])
        self.assertEqual(receipt["credential_isolation"], "pending_authenticated_execution_check")
        self.assertFalse(any("/secrets?" in path or "/organization-secrets?" in path for path in self.api.calls))
        with self.assertRaisesRegex(executor.ExecutorError, "native_approval_missing"):
            self.admit()

    def test_wrong_runtime_and_rerun_never_reuse_approval(self):
        for field, value in (("GITHUB_RUN_ATTEMPT", "2"), ("GITHUB_REF", "refs/heads/other"),
                             ("GITHUB_JOB", "verify"), ("GITHUB_REPOSITORY", "fork/elsa-core"),
                             ("GITHUB_EVENT_NAME", "pull_request"), ("GITHUB_WORKFLOW_SHA", "b"*40)):
            old = self.context[field]
            self.context[field] = value
            with self.subTest(field=field), self.assertRaises(executor.ExecutorError):
                self.admit()
            self.context[field] = old

    def test_api_run_branch_and_head_repository_are_bound(self):
        original = copy.deepcopy(self.api.rows[self.run])
        for change in ({"head_branch": "other"}, {"head_repository": {"full_name": "fork/elsa-core"}},
                       {"head_repository": None}):
            self.api.rows[self.run] = {**original, **change}
            with self.assertRaisesRegex(executor.ExecutorError, "runtime_identity_mismatch"):
                self.admit()
        self.api.rows[self.run] = original

    def test_missing_changed_or_broad_environment_policy(self):
        original = copy.deepcopy(self.api.rows[self.environment])
        for change in ({"id": 15}, {"can_admins_bypass": True}, {"can_admins_bypass": None}, {"protection_rules": []},
                       {"deployment_branch_policy": {"protected_branches": True, "custom_branch_policies": False}}):
            self.api.rows[self.environment] = {**original, **change}
            with self.assertRaises(executor.ExecutorError):
                self.admit()
        self.api.rows[self.environment] = original
        self.api.rows[self.environment+"/deployment-branch-policies?per_page=100&page=1"]["branch_policies"] = [{"id": 14, "type": "branch", "name": "*"}]
        with self.assertRaisesRegex(executor.ExecutorError, "environment_ref_policy_mismatch"):
            self.admit()

    def test_both_credential_names_reject_repo_or_org_fallback(self):
        for suffix in ("actions/secrets", "actions/organization-secrets"):
            path = self.prefix+"/"+suffix+"?per_page=100&page=1"
            for name in (executor.PUBLISH_SECRET, executor.METADATA_SECRET):
                self.api.rows[path] = {"total_count": 1, "secrets": [{"name": name.lower()}]}
                with self.assertRaisesRegex(executor.ExecutorError, "publisher_credential_fallback"):
                    self.admit()
                self.api.rows[path] = {"total_count": 0, "secrets": []}

    def test_missing_unreadable_and_drifting_credential_metadata_block(self):
        path = self.environment+"/secrets?per_page=100&page=1"
        original = copy.deepcopy(self.api.rows[path])
        for rows in ([{"name": executor.PUBLISH_SECRET}], [{"name": executor.METADATA_SECRET}],
                     [{"name": executor.PUBLISH_SECRET}, {"name": executor.METADATA_SECRET}, {"name": executor.METADATA_SECRET.lower()}]):
            self.api.rows[path] = {"total_count": len(rows), "secrets": rows}
            with self.assertRaises(executor.ExecutorError):
                self.admit()
        self.api.rows[path] = executor.ExecutorError("admission_metadata_unavailable")
        with self.assertRaisesRegex(executor.ExecutorError, "admission_metadata_unavailable"):
            self.admit()
        self.api.rows[path] = original
        get = self.api.get
        seen = 0
        def drifting(key):
            nonlocal seen
            value = get(key)
            if key == path:
                seen += 1
                value["secrets"].append({"name": "UNRELATED_A" if seen == 1 else "UNRELATED_B"})
                value["total_count"] += 1
            return value
        with patch.object(self.api, "get", side_effect=drifting):
            with self.assertRaisesRegex(executor.ExecutorError, "credential_metadata_changed"):
                self.admit()

    def test_real_authenticated_metadata_after_native_gate_without_credential_forwarding(self):
        rows, observed = self.api.rows, []
        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass
            def do_GET(self):
                path = self.path.lstrip("/")
                observed.append((path, self.headers.get("Authorization"), self.headers.get("X-NuGet-ApiKey")))
                body = json.dumps(rows[path]).encode()
                self.send_response(200)
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)
        _, origin = loopback_server(self, Handler)
        class Loopback(executor.HttpTransport):
            def _wire_url(inner, url):
                return origin+"/"+url.removeprefix("https://api.github.com/")
        api = executor.GitHubApi()
        api.transport = Loopback(seconds=10)
        token = "synthetic-narrow-metadata-key"
        with patch.object(executor, "REVIEWED_OPERATIONAL_POLICY", self.policy), patch.dict(os.environ, {**self.context, executor.METADATA_SECRET: token}, clear=True):
            receipt = executor.check_admission(publish=False, api=api)
            self.assertFalse(receipt["credential_provenance_verified"])
            self.assertTrue(all(auth is None and publisher is None for _, auth, publisher in observed))
            observed.clear()
            receipt = executor.check_admission(publish=True, api=api)
        self.assertTrue(receipt["native_approval_verified"])
        self.assertTrue(receipt["credential_provenance_verified"])
        self.assertNotIn(token, json.dumps(receipt))
        self.assertEqual(sum(auth is not None for _, auth, _ in observed), 6)
        for path, auth, publisher in observed:
            self.assertEqual(auth, "Bearer "+token if executor.METADATA_PATH.fullmatch(path) else None)
            self.assertIsNone(publisher)
        observed.clear()
        rows[self.approvals] = []
        with patch.object(executor, "REVIEWED_OPERATIONAL_POLICY", self.policy), patch.dict(os.environ, {**self.context, executor.METADATA_SECRET: token}, clear=True):
            with self.assertRaisesRegex(executor.ExecutorError, "native_approval_missing"):
                executor.check_admission(publish=True, api=api)
        self.assertTrue(all(auth is None for _, auth, _ in observed))

    def test_wrong_actor_environment_or_rejected_approval(self):
        for value in ([], [{"state": "rejected", "environments": [{"id": 11, "name": executor.ENVIRONMENT}], "user": {"id": 13}}],
                      [{"state": "approved", "environments": [{"id": 11, "name": executor.ENVIRONMENT}], "user": {"id": 20}}],
                      [{"state": "approved", "environments": [{"id": 22, "name": executor.ENVIRONMENT}], "user": {"id": 13}}]):
            self.api.rows[self.approvals] = value
            with self.assertRaisesRegex(executor.ExecutorError, "native_approval_missing"):
                self.admit()

    def test_pagination_completion_and_race(self):
        self.api.rows["metadata?per_page=100&page=1"] = {"total_count": 101, "rows": [{}]*100}
        self.api.rows["metadata?per_page=100&page=2"] = {"total_count": 101, "rows": [{}]}
        self.assertEqual(len(executor.paged(self.api, "metadata", "rows")), 101)
        self.api.rows["metadata?per_page=100&page=2"]["total_count"] = 102
        with self.assertRaisesRegex(executor.ExecutorError, "admission_pagination_changed"):
            executor.paged(self.api, "metadata", "rows")


class HttpTransportUrlTests(unittest.TestCase):
    def test_mutated_github_hostnames_rejected_before_child(self):
        reader = executor.HttpTransport()
        with patch.object(executor, "bounded_child", side_effect=AssertionError("Unsafe hostname reached child")) as child:
            for hostname in ("apiXgithub.com", "api.githubXcom", "apiXgithubXcom"):
                with self.subTest(hostname=hostname), self.assertRaisesRegex(executor.ExecutorError, "^unsafe_request$"):
                    reader.get(f"https://{hostname}/repos/elsa-workflows/elsa-core/actions/artifacts/1")
            child.assert_not_called()
        self.assertEqual(reader.request_count, 0)

    def test_exact_github_hostname_reaches_bounded_child(self):
        reader = executor.HttpTransport()
        url = "https://api.github.com/repos/elsa-workflows/elsa-core/actions/artifacts?per_page=100&page=1"
        response = b'{"status":200,"complete":true,"failure_category":null,"body":""}'
        with patch.object(executor, "bounded_child", return_value=response) as child:
            result = reader.get(url)
        child.assert_called_once()
        self.assertEqual(json.loads(child.call_args.args[1])["url"], url)
        self.assertTrue(result.complete)
        self.assertEqual(result.status, 200)
        self.assertEqual(reader.request_count, 1)


class LoopbackTests(unittest.TestCase):
    def setUp(self):
        self.received = []
        received = self.received
        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass
            def do_GET(self):
                if self.path == "/slow":
                    time.sleep(0.6)
                    return
                if self.path == "/redirect":
                    self.send_response(302)
                    self.send_header("Location", "/ok")
                    self.end_headers()
                    return
                body = b"X"*10000 if self.path == "/oversized" else b"bounded-reader"
                self.send_response(200)
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                try:
                    self.wfile.write(body)
                except (BrokenPipeError, ConnectionResetError):
                    pass
            def do_PUT(self):
                body = self.rfile.read(int(self.headers["Content-Length"]))
                received.append((self.path, self.headers.get("X-NuGet-ApiKey"), self.headers.get("Content-Type"), body))
                outcome = getattr(self.server, "outcome", 201)
                if outcome == "disconnect":
                    self.close_connection = True
                    return
                if outcome == "slow":
                    time.sleep(0.6)
                    return
                self.send_response(outcome)
                self.send_header("Content-Length", "0")
                self.end_headers()
        self.server, origin = loopback_server(self, Handler)
        class Loopback(executor.HttpTransport):
            route = "/ok"
            def _wire_url(inner, url):
                return origin + ("/nuget" if url == executor.PACKAGE_PUBLISH else "/symbols" if url == executor.SYMBOL_PUBLISH else inner.route)
        self.reader = Loopback(seconds=5)

    def test_real_reader_and_both_original_byte_uploads(self):
        self.assertEqual(self.reader.get(recovery.FEED_INDEX).body, b"bounded-reader")
        for kind in ("nupkg", "snupkg"):
            body = b"PK-original-"+kind.encode()
            result = self.reader.put(kind, body, "synthetic-private-key")
            self.assertEqual(result.status, 201)
            path, key, content_type, actual = self.received[-1]
            boundary = content_type.split("boundary=")[1].encode()
            transmitted = actual.split(b"\r\n\r\n", 1)[1].rsplit(b"\r\n--"+boundary, 1)[0]
            self.assertEqual(transmitted, body)
            self.assertEqual(key, "synthetic-private-key")
            self.assertEqual(path, "/nuget" if kind == "nupkg" else "/symbols")
            self.assertNotIn("synthetic-private-key", repr(result))

    def test_real_ambiguous_upload_after_acceptance_has_no_retry(self):
        for outcome in (409, "disconnect", "slow"):
            self.server.outcome = outcome
            self.reader.deadline = time.monotonic() + (0.15 if outcome == "slow" else 3)
            before = len(self.received)
            started = time.monotonic()
            result = self.reader.put("nupkg", b"original", "synthetic-private-key")
            self.assertEqual(len(self.received), before + 1)
            self.assertNotIn("synthetic-private-key", repr(result))
            if outcome == 409:
                self.assertEqual(result.status, 409)
            else:
                self.assertFalse(result.complete)
            if outcome == "slow":
                self.assertLess(time.monotonic()-started, 0.5)

    def test_oversized_redirect_and_real_deadline(self):
        for route, category in (("/oversized", "oversized_response"), ("/redirect", "redirect_rejected")):
            self.reader.route = route
            self.assertEqual(self.reader.get(recovery.FEED_INDEX, max_bytes=100).failure_category, category)
        self.reader.route = "/slow"
        started = time.monotonic()
        self.assertEqual(self.reader.get(recovery.FEED_INDEX, timeout=0.1).failure_category, "timeout")
        self.assertLess(time.monotonic()-started, 0.5)

    def test_deadline_request_limit_foreign_upload_and_method(self):
        self.reader.deadline = time.monotonic()-1
        with self.assertRaisesRegex(executor.ExecutorError, "deadline_exceeded"):
            self.reader.get(recovery.FEED_INDEX)
        self.assertEqual(self.reader.request_count, 0)
        self.reader.deadline = time.monotonic()+5
        self.reader.max_requests = 1
        self.reader.get(recovery.FEED_INDEX)
        with self.assertRaisesRegex(executor.ExecutorError, "request_limit"):
            self.reader.get(recovery.FEED_INDEX)
        for method, url in (("DELETE", executor.PACKAGE_PUBLISH), ("PUT", recovery.FEED_INDEX), ("PUT", "https://other.invalid/")):
            with self.assertRaises(executor.ExecutorError):
                self.reader.request(method, url)
        self.assertEqual(self.received, [])

    def test_metadata_authorization_exact_endpoint_only_and_missing_token_blocks(self):
        api = executor.GitHubApi()
        api.transport = self.reader
        path = f"repos/{executor.REPOSITORY}/actions/secrets?per_page=100&page=1"
        with patch.dict(os.environ, {}, clear=True), self.assertRaisesRegex(executor.ExecutorError, "metadata_credential_missing"):
            api.get(path)
        self.assertEqual(self.reader.request_count, 0)
        for url in (recovery.FEED_INDEX, executor.PACKAGE_PUBLISH,
                    "https://api.github.com/repos/elsa-workflows/elsa-core/actions/artifacts/1",
                    "https://api.github.com/"+path+"&redirect=other",
                    "https://api.github.com/repos/other/repo/actions/secrets?per_page=100&page=1"):
            with self.assertRaisesRegex(executor.ExecutorError, "unsafe_metadata_authorization"):
                self.reader.request("GET", url, headers={"Authorization": "Bearer synthetic"})
        with self.assertRaisesRegex(executor.ExecutorError, "unsafe_publisher_authorization"):
            self.reader.request("GET", recovery.FEED_INDEX, headers={"X-NuGet-ApiKey": "synthetic"})
        self.assertEqual(self.reader.request_count, 0)

    def test_real_child_bound_and_timeout_reap(self):
        started = time.monotonic()
        with self.assertRaisesRegex(executor.ExecutorError, "response_limit"):
            executor.bounded_child([sys.executable, "-c", "import os\nwhile True: os.write(1,b'x'*65536)"], b"", deadline=time.monotonic()+2, limit=100)
        self.assertLess(time.monotonic()-started, 1)
        with self.assertRaisesRegex(executor.ExecutorError, "deadline_exceeded"):
            executor.bounded_child([sys.executable, "-c", "import time; time.sleep(2)"], b"", deadline=time.monotonic()+0.1, limit=100)


class InspectorIsolationTests(unittest.TestCase):
    def test_child_never_inherits_publisher_or_metadata_credentials(self):
        with tempfile.TemporaryDirectory() as directory:
            helper = Path(directory)/"helper.dll"
            helper.write_bytes(b"synthetic-helper")
            inspector = executor.Inspector(helper)
            def child(command, payload, **kwargs):
                self.assertEqual(command[-1], "--inspect-symbols")
                self.assertNotIn(executor.PUBLISH_SECRET, kwargs["env"])
                self.assertNotIn(executor.METADATA_SECRET, kwargs["env"])
                self.assertNotIn("GH_TOKEN", kwargs["env"])
                self.assertNotIn("GITHUB_TOKEN", kwargs["env"])
                self.assertNotIn("ACTIONS_READ_TOKEN", kwargs["env"])
                raise executor.ExecutorError("private-token-child-error")
            with patch.dict(os.environ, {executor.PUBLISH_SECRET: "private-key", executor.METADATA_SECRET: "metadata-private-key", "GH_TOKEN": "artifact-private-key",
                                         "GITHUB_TOKEN": "private", "ACTIONS_READ_TOKEN": "private"}), patch.object(executor, "bounded_child", side_effect=child):
                with self.assertRaisesRegex(executor.ExecutorError, "^symbol_inspection_failed$"):
                    inspector.inspect(b"assembly", b"symbols", "Synthetic.pdb")
            self.assertEqual(inspector.digest, executor.sha(b"synthetic-helper"))
            self.assertEqual(inspector.source_digest, executor.sha(Path(executor.__file__).with_name("VerifyPackageSymbolPair").joinpath("Program.cs").read_bytes()))
            helper.write_bytes(b"changed")
            with self.assertRaisesRegex(executor.ExecutorError, "inspector_changed"):
                inspector.inspect(b"assembly", b"symbols", "Synthetic.pdb")


class CliTests(unittest.TestCase):
    def test_original_artifact_tokens_remain_rejected_by_shared_validator(self):
        unused = Path("/nonexistent-immutable-input")
        for name in ("GH_TOKEN", "GITHUB_TOKEN", "ACTIONS_READ_TOKEN"):
            with patch.dict(os.environ, {name: "synthetic-artifact-token"}, clear=True):
                with self.assertRaisesRegex(RuntimeError, "Artifact-reading token must not enter verification/execution"):
                    executor.candidate_input.verify_candidate_inputs(unused, unused, unused, unused, unused)

    def test_existing_output_and_overlap_no_network_or_overwrite(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            output = root/"existing.json"
            output.write_text("keep")
            with patch.object(executor, "check_admission") as admit:
                self.assertEqual(executor.main(["admit", "--output", str(output)]), 1)
                admit.assert_not_called()
            self.assertEqual(output.read_text(), "keep")
            inputs = root/"inputs"
            inputs.mkdir()
            output = inputs/"receipt.json"
            with patch.object(executor.inputs, "prepare_recovery_inputs") as prepare:
                self.assertEqual(executor.main(["verify", "--inputs", str(inputs), "--verified-root", str(root/"verified"),
                    "--output", str(output), "--inspector", str(root/"helper.dll"), "--executor-source", "a"*40]), 1)
                prepare.assert_not_called()
            self.assertFalse(output.exists())

    def test_atomic_receipt_keeps_last_complete_snapshot_and_rejects_replaced_output(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory).resolve()/"receipt.json"
            with executor.output_file(output):
                pass
            owner = [output.stat().st_ino]
            executor.atomic_receipt(output, {"state": "acceptance_unknown"}, owner)
            self.assertEqual(json.loads(output.read_text()), {"state": "acceptance_unknown"})
            replacement = output.with_name("replacement.json")
            replacement.write_text("user content")
            os.replace(replacement, output)
            with self.assertRaisesRegex(executor.ExecutorError, "receipt_ownership_changed"):
                executor.atomic_receipt(output, {"state": "wrong"}, owner)
            self.assertEqual(output.read_text(), "user content")
            self.assertEqual(list(output.parent.glob(".elsa-executor-*")), [])

    def test_admit_default_retains_secure_blocked_receipt(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory).resolve()/"admission.json"
            self.assertEqual(executor.main(["admit", "--output", str(output)]), 1)
            receipt = json.loads(output.read_text())
            self.assertEqual(receipt["failure_category"], "operational_policy_unconfigured")
            self.assertFalse(receipt["publication_performed"])
            self.assertEqual(output.stat().st_mode & 0o777, 0o600)


if __name__ == "__main__":
    unittest.main()
