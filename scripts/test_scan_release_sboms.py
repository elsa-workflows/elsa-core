import json
import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path
from unittest.mock import patch

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

    def test_database_requires_fresh_valid_revision(self):
        self.assertEqual(monitor.database(status())["sha256"], "d" * 64)
        for data in [status(timedelta(days=6)), status(timedelta(days=-1)), dict(status(), valid=False),
                     dict(status(), **{"from": ""})]:
            with self.assertRaises(ValueError):
                monitor.database(data)

    def test_report_deduplicates_shared_package_components_and_preserves_context(self):
        def inputs(tag, commit, folder):
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
                with self.assertRaisesRegex(ValueError, "revision changed"):
                    monitor.scan(path, {})

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
