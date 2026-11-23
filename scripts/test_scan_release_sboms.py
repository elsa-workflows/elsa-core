import io
import json
import sys
import zipfile
import subprocess
import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path
from unittest.mock import Mock, call, patch

import scan_release_sboms as monitor


def release(tag, published="2026-11-24T00:00:00Z", assets=()):
    return {"tag_name": tag, "draft": False, "prerelease": False, "published_at": published,
            "assets": [{"name": name} for name in assets]}


def status(age=timedelta(hours=2)):
    return {"schemaVersion": "v6.1", "valid": True,
            "from": "https://grype.anchore.io/db?checksum=sha256%3A" + "d" * 64,
            "built": (datetime.now(timezone.utc) - age).isoformat()}


class ReleaseMonitorTests(unittest.TestCase):
    def test_latest_stable_in_each_supported_minor(self):
        rows = [release(v) for v in ("2.17.0", "3.9.10", "3.10.0", "3.9.2", "3.9.11-preview.1")]
        groups = monitor.lines(rows + [dict(release("3.10.1"), draft=True)])
        self.assertEqual([[r["tag_name"] for r in line] for line in groups], [["3.9.10", "3.9.2"], ["3.10.0"]])

    def test_historical_backfill_newer_failure_and_older_covered(self):
        releases = [release("3.10.4"), release("3.10.3", assets=[monitor.INDEX, monitor.BUNDLE]),
                    release("3.9.0", "2025-01-01T00:00:00Z")]
        def attempt(release, family, *args):
            tag = release["tag_name"]
            return {"version": tag, "family": family, "state": "error" if tag == "3.10.4" else "clean"}
        with (patch.object(monitor, "run", return_value=json.dumps([releases])),
              patch.object(monitor, "registry_backfills", side_effect=lambda name: {"3.9.0"} if name == "elsa-server-v3" else set()),
              patch.object(monitor, "attempt", side_effect=attempt)):
            result = monitor.monitor()
        self.assertEqual([(r["version"], r["state"]) for r in result if r["family"] == "nuget"],
                         [("3.9.0", "predates-evidence"), ("3.10.4", "error"), ("3.10.3", "clean")])
        self.assertEqual([r["state"] for r in result if r["version"] == "3.9.0" and r["family"] == "elsa-server-v3"], ["clean"])
        self.assertFalse(any(r["family"] == "elsa-studio-v3" and r["version"].startswith("3.10.") for r in result))

    def test_registry_discovery_error_does_not_stop_newer_releases(self):
        releases = [release("3.10.0"), release("3.11.0")]
        def discover(family):
            if family == "elsa-server-v3":
                raise OSError("Registry unavailable")
            return set()
        def attempt(release, family, *args):
            return {"version": release["tag_name"], "family": family, "state": "clean"}
        with (patch.object(monitor, "run", return_value=json.dumps([releases])),
              patch.object(monitor, "registry_backfills", side_effect=discover),
              patch.object(monitor, "attempt", side_effect=attempt)):
            result = monitor.monitor()
        self.assertEqual(sum(r["state"] == "error" for r in result), 1)
        self.assertEqual(sum(r["state"] == "clean" for r in result), 7)

    def test_no_inventories_and_unsigned_release_fail_closed(self):
        with patch.object(monitor, "scan") as scan:
            record = monitor.attempt(release("3.11.0"), "nuget", None, {})
            self.assertEqual(record["state"], "error")
            release_with_assets = release("3.11.0", assets=[monitor.INDEX, monitor.BUNDLE])
            with (patch.object(monitor, "release_commit", return_value="c" * 40),
                  patch.object(monitor, "packages", side_effect=ValueError("Signer mismatch"))):
                record = monitor.attempt(release_with_assets, "nuget", None, {})
            self.assertEqual(record["error"], "Signer mismatch")
            self.assertNotIn("findings", record)
            scan.assert_not_called()

    def test_verified_package_inputs_survive_an_independent_feed_failure(self):
        tag = "3.11.0"
        commit = "a" * 40
        import nuget_release_evidence as producer
        tarball = b"published archive"
        bom = json.dumps({"bomFormat": "CycloneDX", "specVersion": "1.6", "metadata": {
            "component": {"name": "Elsa", "version": tag, "hashes": [{"alg": "SHA-256", "content": monitor.digest(tarball)}]},
            "properties": [{"name": "elsa:target-framework", "value": "net10.0"}]}}).encode()
        row = {"id": "Elsa", "candidate_sha256": monitor.digest(tarball),
               "frameworks": {"net10.0": {"path": "boms/Elsa/net10.0.cdx.json", "sha256": monitor.digest(bom)}},
               "browser_build_inputs": []}
        candidate = {"schema": 1, "version": tag, "source_commit": commit, "tooling_commit": "c" * 40,
                     "producer_run": 3, "producer_attempt": 1, "packages": [row]}
        feeds = {feed: {"url": f"{base}/elsa/{tag}/elsa.{tag}.nupkg", "sha256": monitor.digest(tarball)}
                 for feed, base in producer.FEEDS.items()}
        with (tempfile.TemporaryDirectory() as tmp,
              patch.object(producer, "verify_attestation", wraps=producer.verify_attestation) as attest,
              patch.object(producer, "check_bom", wraps=producer.check_bom) as check_bom,
              patch.object(subprocess, "run") as gh):
            folder = Path(tmp)
            bundle = folder / monitor.BUNDLE
            with zipfile.ZipFile(bundle, "w") as archive:
                archive.writestr(producer.CANDIDATE, json.dumps(candidate))
                archive.writestr("boms/Elsa/net10.0.cdx.json", bom)
            index = dict(candidate, packages=[dict(row, feeds=feeds)],
                         sbom_bundle={"name": monitor.BUNDLE, "sha256": monitor.digest(bundle.read_bytes())})
            index_file = folder / monitor.INDEX
            index_file.write_text(json.dumps(index))
            with (patch.object(monitor, "run"), patch.object(monitor, "read_feed", side_effect=[tarball, b"unexpected bytes"]) as read):
                evidence = {}
                subjects = monitor.packages(tag, commit, folder, evidence, {})
                self.assertEqual(evidence["release_sha256"], monitor.digest(index_file.read_bytes()))
                self.assertEqual(evidence["verification_errors"], {"elsa/nuget": "Wrong feed artifact"})
                self.assertEqual([s["feed"] for s in subjects[0][1]["artifacts"]], ["feedz"])
                self.assertEqual(subjects[0][0].read_bytes(), bom)
                self.assertEqual(attest.call_args_list, [
                    call(index_file, producer.SIGNER), call(folder / producer.CANDIDATE, producer.PRODUCER, "c" * 40)])
                self.assertIn("--deny-self-hosted-runners", gh.call_args.args[0])
                self.assertIn("refs/heads/main", gh.call_args.args[0])
                check_bom.assert_called_once_with(bom, index["packages"][0], tag, "net10.0")
                gh.side_effect = ValueError("Wrong signer")
                read.reset_mock()
                with self.assertRaisesRegex(ValueError, "Wrong signer"):
                    monitor.packages(tag, commit, folder, {}, {})
                read.assert_not_called()
                gh.side_effect = None
                bundle.write_bytes(b"wrong bundle")
                with self.assertRaisesRegex(ValueError, "Wrong SBOM bundle"):
                    monitor.packages(tag, commit, folder, {}, {})

    def test_image_subject_and_producer_are_bound_before_inventory_verification(self):
        name = "elsa-server-v3"
        repository = "docker.io/elsaworkflows/" + name
        tag = "3.11.0"
        commit = "a" * 40
        checksum = "b" * 64
        index = "sha256:" + checksum
        publisher = Mock()
        publisher.verify_image.side_effect = ValueError("Missing platform evidence")
        with tempfile.TemporaryDirectory() as tmp, patch.dict(sys.modules, {"verify_community_image": publisher}):
            for subject_name in ("docker.io/incorrect/name", repository):
                proof = [{"verificationResult": {"statement": {"subject": [
                    {"name": subject_name, "digest": {"sha256": checksum}}]}}}]
                with patch.object(monitor, "run", side_effect=[index, json.dumps(proof)]) as run:
                    evidence = {}
                    with self.assertRaisesRegex(ValueError, "Attestation subject|Missing platform evidence"):
                        monitor.image(tag, commit, Path(tmp), name, evidence)
                    self.assertEqual(run.call_args_list[0], call("oras", "resolve", f"{repository}:{tag}"))
                    args = run.call_args_list[1].args
                    self.assertIn(monitor.IMAGE_WORKFLOW, args)
                    self.assertIn(f"refs/tags/{tag}", args)
                    self.assertIn(commit, args)
                    if subject_name == repository:
                        publisher.verify_image.assert_called_once_with(f"{repository}@{index}", Path(tmp), commit,
                                                                      publisher.spdx_validator.return_value)
                        self.assertEqual(evidence["release_sha256"], checksum)
                    else:
                        publisher.verify_image.assert_not_called()
                        self.assertNotIn("release_sha256", evidence)

    def test_image_consumer_reads_both_real_publisher_outputs(self):
        import verify_community_image as publisher
        from test_verify_community_image import PlatformEvidenceTests
        from jsonschema import Draft201909Validator

        # Reuse the producer's own platform fixture; only OS/registry and signing I/O is substituted.
        fixture = PlatformEvidenceTests()
        fixture.setUp()
        name = "elsa-server-v3"
        repository = "docker.io/elsaworkflows/" + name
        index = "sha256:" + "b" * 64
        fixture.documents[index] = fixture.index
        for arch in ("amd64", "arm64"):
            fixture.documents[f"config-{arch}"]["config"]["Labels"]["org.opencontainers.image.version"] = "3.11.0"
        proof = [{"verificationResult": {"statement": {"subject": [{"name": repository, "digest": {"sha256": index[7:]}}]}}}]
        with (tempfile.TemporaryDirectory() as tmp, patch.object(monitor, "run", side_effect=[index, json.dumps(proof)]),
              patch.object(publisher, "spdx_validator", return_value=Draft201909Validator(
                  {"type": "object", "required": ["spdxVersion", "packages"]})),
              patch.object(publisher, "read_document", side_effect=lambda repo, key, **kwargs: fixture.documents[key])):
            evidence = {}
            subjects = monitor.image("3.11.0", "source", Path(tmp), name, evidence)
            self.assertEqual(evidence["release_sha256"], index[7:])
            self.assertEqual([r["context"] for _, r in subjects], ["linux/amd64", "linux/arm64"])
            for path, r in subjects:
                self.assertEqual(monitor.digest(path.read_bytes()), r["sbom_sha256"])

    def test_feed_timeouts_are_bounded_and_do_not_stop_other_feeds(self):
        failures = {}
        with patch.object(monitor, "urlopen", side_effect=TimeoutError("too slow")) as fetch:
            for _ in range(3):
                with self.assertRaises(TimeoutError):
                    monitor.read_feed("https://feed.invalid", "nuget", failures)
            with self.assertRaisesRegex(ValueError, "deferred"):
                monitor.read_feed("https://feed.invalid", "nuget", failures)
            with self.assertRaises(TimeoutError):
                monitor.read_feed("https://other.invalid", "feedz", failures)
            self.assertEqual(fetch.call_count, 4)
            fetch.assert_called_with("https://other.invalid", timeout=15)

    def test_nontransient_failure_resets_the_feed_cutoff(self):
        def response(status):
            return monitor.HTTPError("https://feed.invalid", status, "fixture", {}, None)
        failures = {}
        with patch.object(monitor, "urlopen", side_effect=[response(503), response(404), response(503), response(503), io.BytesIO(b"ok")]):
            for value in (1, 0, 1, 2):
                with self.assertRaises(monitor.HTTPError):
                    monitor.read_feed("https://feed.invalid", "nuget", failures)
                self.assertEqual(failures["nuget"], value)
            self.assertEqual(monitor.read_feed("https://feed.invalid", "nuget", failures), b"ok")

    def test_failed_tag_listing_keeps_earlier_pages_and_probes_unlisted_historical_versions(self):
        def tags(name):
            if name == "elsa-server-v3":
                yield "3.9.0"
                raise OSError("page two unavailable")
        historical = [release(tag, "2026-01-01T00:00:00Z") for tag in ("3.9.0", "3.10.0")]
        def attempt(release, family, *args):
            return {"version": release["tag_name"], "family": family, "state": "clean"}
        with (patch.object(monitor, "run", return_value=json.dumps([historical])),
              patch.object(monitor, "registry_backfills", side_effect=tags),
              patch.object(monitor, "historical_image_candidate", return_value=True) as probe,
              patch.object(monitor, "attempt", side_effect=attempt)):
            results = monitor.monitor()
        self.assertEqual([r["version"] for r in results if r["state"] == "clean"], ["3.9.0", "3.10.0"])
        self.assertEqual([r["family"] for r in results if r["state"] == "error"], ["elsa-server-v3"])
        probe.assert_called_once_with("elsa-server-v3", "3.10.0")

    def test_distribution_deadline_keeps_partial_evidence_and_continues_with_siblings(self):
        release_row = release("3.11.0")
        def attempt(release, family, stage, cache, record, progress):
            if family == "nuget":
                record.update(release_sha256="a"*64, database={"built": "2026-11-23"},
                              findings=[{"subject_sha256": "b"*64, "component": "fixture", "advisory": "GHSA-test"}])
                progress()
                subprocess.run([sys.executable, "-c", "import time; time.sleep(5)"], check=True)
            return dict(record, state="clean")
        with (tempfile.TemporaryDirectory() as tmp,
              patch.object(monitor, "DISTRIBUTION_TIMEOUT_SECONDS", 0.1),
              patch.object(monitor, "run", return_value=json.dumps([[release_row]])),
              patch.object(monitor, "registry_backfills", return_value=set()), patch.object(monitor, "attempt", side_effect=attempt)):
            path = Path(tmp) / "report.json"
            result = monitor.monitor(path)
            self.assertEqual(json.loads(path.read_bytes())["results"], result)
        self.assertEqual([r["state"] for r in result], ["error", "clean", "clean", "clean"])
        self.assertEqual(result[0]["timeout_seconds"], 0.1)
        self.assertEqual(result[0]["release_sha256"], "a"*64)
        self.assertEqual(result[0]["findings"][0]["advisory"], "GHSA-test")

    def test_external_command_has_its_own_shorter_timeout(self):
        with patch.object(monitor, "COMMAND_TIMEOUT_SECONDS", 0.1), self.assertRaises(subprocess.TimeoutExpired) as timed:
            monitor.run(sys.executable, "-c", "import time; time.sleep(5)")
        self.assertIn("timed out after 0.1 seconds", monitor.error_message(timed.exception))

    def test_interrupted_scan_keeps_previous_outcomes_and_active_subject(self):
        previous = [{"version": "3.10.0", "family": "nuget", "state": "clean"}]
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "report.json"
            monitor.checkpoint(path, previous, {"version": "3.11.0", "family": "nuget"})
            results = json.loads(path.read_bytes())["results"]
            self.assertEqual(results[0], previous[0])
            self.assertEqual(results[-1]["state"], "error")
            self.assertEqual(results[-1]["version"], "3.11.0")

    def test_corrupt_inventory_falls_back_and_receipt_counts_observed_matches(self):
        records = [release("3.11.1"), release("3.11.0")]
        def attempt(release, family, *args):
            row = {"version": release["tag_name"], "family": family, "state": "clean", "findings": []}
            if row["version"] == "3.11.1":
                row.update(state="error", inventory_errors={"a"*64: "Invalid inventory"}, inventories=["fixture"],
                           findings=[{"advisory": "GHSA-x", "subject_sha256": "b"*64, "component": "pkg:nuget/test@1"}])
            return row
        with (patch.object(monitor, "run", return_value=json.dumps([records])),
              patch.object(monitor, "registry_backfills", return_value=set()), patch.object(monitor, "attempt", side_effect=attempt)):
            rows = [r for r in monitor.monitor() if r["family"] == "nuget"]
        self.assertEqual([r["state"] for r in rows], ["error", "clean"])
        with patch.object(monitor, "run", return_value="[[]]") as run:
            monitor.receipts(rows, "https://example.invalid")
            self.assertIn("coverage incomplete; 1 observed distinct match", run.call_args.args[-1])

    def test_database_requires_fresh_valid_revision(self):
        self.assertEqual(monitor.database(status())["sha256"], "d" * 64)
        for data in [status(timedelta(days=6)), status(timedelta(days=-1)), dict(status(), valid=False),
                     dict(status(), **{"from": ""})]:
            with self.assertRaises(ValueError):
                monitor.database(data)

    def test_failed_db_status_retains_missing_and_stale_diagnostics(self):
        for reason in ("database does not exist", "database is older than allowed"):
            result = subprocess.CompletedProcess(["grype", "db", "status"], 1,
                                                 stdout=json.dumps({"valid": False, "error": reason}))
            update = subprocess.CompletedProcess(["grype", "db", "update"], 1)
            cache = {}
            with patch.object(monitor.subprocess, "run", side_effect=[update, result]), self.assertRaisesRegex(ValueError, reason):
                monitor.refresh_database(cache)
            self.assertEqual(cache["database_status"]["error"], reason)
            self.assertNotIn("database", cache)

    def test_report_deduplicates_shared_package_components_and_preserves_context(self):
        def inputs(tag, commit, folder, evidence, feed_failures):
            path = folder / "bom"
            path.write_text("{}")
            return [(path, {"release_sha256": "f" * 64, "sbom_sha256": checksum,
                            "artifacts": [{"sha256": "e" * 64}], "context": context})
                    for checksum, context in [("a" * 64, "net8.0"), ("b" * 64, "net9.0")]]
        matches = [{"component": "pkg:nuget/Example@1.0.0", "advisory": "GHSA-example",
                    "severity": "High", "fix": {"versions": ["1.1.0"]}}]
        with (patch.object(monitor, "release_commit", return_value="c" * 40),
              patch.object(monitor, "packages", side_effect=inputs), patch.object(monitor, "scan", return_value=matches)):
            row = monitor.attempt(release("3.11.0", assets=[monitor.INDEX, monitor.BUNDLE]), "nuget", None,
                                  {"database": monitor.database(status())})
        self.assertEqual(row["state"], "findings")
        self.assertEqual(row["findings"][0]["inventories"], ["a" * 64, "b" * 64])
        self.assertEqual(len(row["findings"]), 1)

    def test_partial_scan_retains_verified_inputs_error_and_earlier_finding(self):
        subjects = [(Path(k), {"release_sha256": "f" * 64, "sbom_sha256": k * 64,
                               "artifacts": [{"sha256": "e" * 64}], "context": k}) for k in "ab"]
        matched = [{"component": "pkg:nuget/Example@1.0.0", "advisory": "GHSA-example"}]
        release_with_assets = release("3.11.0", assets=[monitor.INDEX, monitor.BUNDLE])
        with (patch.object(monitor, "release_commit", return_value="c" * 40),
              patch.object(monitor, "packages", return_value=subjects),
              patch.object(monitor, "scan", side_effect=[matched, ValueError("Cannot parse")])):
            row = monitor.attempt(release_with_assets, "nuget", None, {"database": monitor.database(status())})
            failed_db = monitor.attempt(release_with_assets, "nuget", None, {"database_error": "Database unavailable"})
        self.assertEqual(row["state"], "error")
        self.assertEqual(len(row["inventories"]), 2)
        self.assertEqual(row["findings"][0]["advisory"], "GHSA-example")
        self.assertEqual(row["inventory_errors"], {"b" * 64: "Cannot parse"})
        self.assertEqual(failed_db["state"], "error")
        self.assertEqual(failed_db["inventories"], row["inventories"])
        self.assertNotIn("findings", failed_db)

    def test_fresh_database_rescan_marks_unchanged_findings_in_its_receipt(self):
        finding = {"subject_sha256": "b" * 64, "component": "pkg:nuget/Example@1.0.0", "advisory": "GHSA-example"}
        row = {"version": "3.11.0", "family": "nuget", "state": "findings", "release_sha256": "a" * 64,
               "database": monitor.database(status()), "findings": [finding]}
        with patch.object(monitor, "run", return_value="[[]]") as run:
            monitor.receipts([row], "https://example.invalid")
            previous = run.call_args.args[-1]
        previous_receipt = [{"user": {"login": "github-actions[bot]"}, "body": previous}]
        next_scan = dict(row, database=dict(row["database"], sha256="c" * 64))
        with patch.object(monitor, "run", return_value=json.dumps([previous_receipt])) as run:
            monitor.receipts([next_scan], "https://example.invalid")
            self.assertIn("previously recorded finding set", run.call_args.args[-1])
            self.assertNotIn(finding["advisory"], run.call_args.args[-1])

    def test_grype_input_and_database_must_match(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "inventory"
            path.write_text("{}")
            with self.assertRaisesRegex(ValueError, "Unsupported"):
                monitor.scan(path, {})
            path.write_text(json.dumps({"bomFormat": "CycloneDX", "specVersion": "1.6"}))
            state = status()
            result = {"matches": [], "descriptor": {"version": monitor.GRYPE, "db": {"status": state}}}
            with patch.object(monitor, "run", return_value=json.dumps(result)):
                self.assertEqual(monitor.scan(path, monitor.database(state)), [])
                path.write_text(json.dumps({"bomFormat": "CycloneDX", "specVersion": "1.5"}))
                self.assertEqual(monitor.scan(path, monitor.database(state)), [])
                with self.assertRaisesRegex(ValueError, "revision changed"):
                    monitor.scan(path, {})

    def test_missing_advisory_metadata_preserves_the_match(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "inventory"
            path.write_text(json.dumps({"spdxVersion": "SPDX-2.3"}))
            state = status()
            artifact = {"type": "nuget", "name": "Example", "version": "1"}
            report = {"descriptor": {"version": monitor.GRYPE, "db": {"status": state}},
                      "matches": [{"artifact": artifact, "vulnerability": {"id": "GHSA-example"}}]}
            with patch.object(monitor, "run", return_value=json.dumps(report)):
                result = monitor.scan(path, monitor.database(state))
            self.assertEqual(result, [{"advisory": "GHSA-example", "component": "nuget:Example@1",
                                       "severity": "Unknown", "fix": None}])

    def test_deadline_finalizes_active_verified_release_and_partial_findings(self):
        release_with_assets = release("3.11.0", assets=[monitor.INDEX, monitor.BUNDLE])
        subjects = [(Path(k), {"release_sha256": "f"*64, "sbom_sha256": k*64,
                               "artifacts": [{"sha256": "e"*64}], "context": k}) for k in "ab"]
        def refresh(cache):
            cache["database"] = monitor.database(status())
        matched = [{"component": "pkg:nuget/Example@1", "advisory": "GHSA-example"}]
        with (tempfile.TemporaryDirectory() as tmp, patch.object(monitor, "run", return_value=json.dumps([[release_with_assets]])),
              patch.object(monitor, "registry_backfills", return_value=set()), patch.object(monitor, "release_commit", return_value="c"*40),
              patch.object(monitor, "packages", return_value=subjects), patch.object(monitor, "refresh_database", side_effect=refresh),
              patch.object(monitor, "scan", side_effect=[matched, monitor.MonitoringInterrupted()])):
            path = Path(tmp) / "report.json"
            rows = monitor.monitor(path)
            self.assertEqual(rows, json.loads(path.read_bytes())["results"])
        row = rows[-1]
        self.assertEqual(row["state"], "error")
        self.assertEqual(row["release_sha256"], "f"*64)
        self.assertEqual(len(row["inventories"]), 2)
        self.assertEqual(row["database"]["sha256"], "d"*64)
        self.assertEqual(row["findings"][0]["advisory"], "GHSA-example")

    def test_error_receipt_ignores_temporary_paths_and_elapsed_database_age(self):
        keys = []
        for directory in ("first", "second"):
            exc = subprocess.CalledProcessError(1, ["grype", f"sbom:/tmp/{directory}/inventory", "-o", "json"])
            keys.append(monitor.receipt_key({"state": "error", "inventory_errors": {"a" * 64: monitor.error_message(exc)}}))
        self.assertEqual(*keys)
        row = {"state": "error", "database_status": {"valid": False, "built": "2026-01-01T00:00:00Z"}}
        self.assertEqual(monitor.receipt_key(dict(row, error="built 8 days ago")),
                         monitor.receipt_key(dict(row, error="built 9 days ago")))

    def test_receipts_only_deduplicate_authenticated_bot_receipts(self):
        row = {"version": "3.11.0", "family": "nuget", "state": "clean", "release_sha256": "a" * 64,
               "database": monitor.database(status()), "findings": []}
        marker = f"<!-- elsa-rescan:{monitor.receipt_key(row)} -->"
        for login, calls in [("github-actions[bot]", 1), ("other", 2)]:
            with patch.object(monitor, "run", return_value=json.dumps([[{"user": {"login": login}, "body": marker}]])) as run:
                monitor.receipts([row, row], "https://example.invalid")
                self.assertEqual(len(run.call_args_list), calls)


if __name__ == "__main__":
    unittest.main()
