import copy
import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from release_unit_manifest import (
    DEFAULT_UNIT_ID,
    MANIFEST_PATH,
    get_current_publisher,
    get_unit,
    load_manifest,
    validate_publisher_handoff,
)


class PublisherHandoffTests(unittest.TestCase):
    def setUp(self):
        self.document = load_manifest(MANIFEST_PATH)
        self.unit = get_unit(self.document, DEFAULT_UNIT_ID)
        self.current = get_current_publisher(self.unit)
        self.proposed = {
            "repository": "elsa-core",
            "workflow_path": ".github/workflows/packages.yml",
        }

    def test_current_manifest_is_a_nonpublishing_extensions_only_preflight(self):
        result = validate_publisher_handoff(self.unit)

        self.assertEqual("elsa-extensions", result["current_publisher"]["repository"])
        self.assertEqual("not-cut-over", result["handoff_status"])
        self.assertEqual(self.unit["mapped"]["source_commits"], result["provenance"]["mapped_source_commits"])
        self.assertEqual(
            self.unit["source"]["provenance"]["released_artifact_sha256"],
            result["provenance"]["released_artifact_sha256"],
        )
        self.assertFalse(result["provenance"]["local_proof_publishable"])
        self.assertFalse(result["publication_performed"])
        self.assertFalse(result["live_publisher_changed"])
        self.assertFalse(result["live_feed_history_verified"])
        self.assertEqual("elsa-extensions", get_current_publisher(self.unit)["repository"])

    def test_proposed_publisher_without_receipt_fails_closed(self):
        with self.assertRaisesRegex(ValueError, "both required"):
            validate_publisher_handoff(self.unit, self.proposed)

    def test_simulated_reviewed_handoff_validates_without_changing_current_owner(self):
        result = validate_publisher_handoff(self.unit, self.proposed, self.receipt())

        self.assertEqual("simulated-receipt-valid", result["handoff_status"])
        self.assertEqual("elsa-core", result["simulated_publisher"]["repository"])
        self.assertFalse(result["publication_performed"])
        self.assertFalse(result["live_publisher_changed"])
        self.assertEqual("elsa-extensions", get_current_publisher(self.unit)["repository"])

    def test_stale_source_pin_in_receipt_is_rejected(self):
        receipt = self.receipt()
        receipt["source_commits"]["elsa-core"] = "0" * 40

        with self.assertRaisesRegex(ValueError, "source_commits are stale"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_local_proof_version_cannot_be_used_as_release_version(self):
        receipt = self.receipt()
        receipt["release_version"] = self.unit["versioning"]["local_proof_version"]

        with self.assertRaisesRegex(ValueError, "proof version cannot be used"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_published_stable_version_cannot_be_reused(self):
        for version in ("3.8.4", "3.8.3"):
            with self.subTest(version=version):
                receipt = self.receipt()
                receipt["release_version"] = version
                with self.assertRaisesRegex(ValueError, "exceed the last known published stable"):
                    validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_build_metadata_cannot_disguise_a_published_version(self):
        receipt = self.receipt()
        receipt["release_version"] = "3.8.4+repack"
        with self.assertRaisesRegex(ValueError, "stable SemVer version"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_completed_handoff_can_record_core_without_rewriting_released_source_provenance(self):
        document = copy.deepcopy(self.document)
        unit = document["release_units"][0]
        unit["publisher"]["current_publishers"] = [dict(self.proposed)]
        with tempfile.TemporaryDirectory() as directory:
            manifest = Path(directory) / "release-units.json"
            manifest.write_text(json.dumps(document), encoding="utf-8")
            loaded = get_unit(load_manifest(manifest), DEFAULT_UNIT_ID)
        self.assertEqual("elsa-core", get_current_publisher(loaded)["repository"])
        self.assertEqual("elsa-extensions", loaded["source"]["repository"])

    def test_unapproved_receipt_is_rejected(self):
        receipt = self.receipt()
        receipt["review"]["status"] = "pending"

        with self.assertRaisesRegex(ValueError, "must be approved"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_receipt_without_old_publisher_disablement_is_rejected(self):
        receipt = self.receipt()
        receipt["old_publisher_disabled"]["state"] = "enabled"

        with self.assertRaisesRegex(ValueError, "state must be 'disabled'"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_receipt_that_enables_new_publisher_first_is_rejected(self):
        receipt = self.receipt()
        receipt["old_publisher_disabled"]["observed_at"] = "2026-09-25T12:00:00Z"

        with self.assertRaisesRegex(ValueError, "disabled before the new publisher"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_receipt_for_a_different_current_publisher_is_rejected(self):
        receipt = self.receipt()
        receipt["from_publisher"]["repository"] = "elsa-core"

        with self.assertRaisesRegex(ValueError, "differs from the current publisher"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_disablement_evidence_must_name_the_old_publisher_workflow(self):
        receipt = self.receipt()
        receipt["old_publisher_disabled"]["workflow_path"] = ".github/workflows/other.yml"

        with self.assertRaisesRegex(ValueError, "identify the current publisher workflow"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_receipt_must_remain_nonpublishing(self):
        receipt = self.receipt()
        receipt["publication_performed"] = True

        with self.assertRaisesRegex(ValueError, "must not perform publication"):
            validate_publisher_handoff(self.unit, self.proposed, receipt)

    def test_manifest_with_two_owners_is_rejected_before_handoff(self):
        invalid = copy.deepcopy(self.document)
        extra = copy.deepcopy(self.current)
        extra["repository"] = "elsa-core"
        invalid["release_units"][0]["publisher"]["current_publishers"].append(extra)

        with tempfile.TemporaryDirectory() as directory:
            manifest = Path(directory) / "release-units.json"
            manifest.write_text(json.dumps(invalid), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "exactly one current publisher"):
                load_manifest(manifest)

    def receipt(self):
        return {
            "schema_version": 1,
            "mode": "simulation",
            "package_id": self.unit["package_id"],
            "source_commits": dict(self.unit["mapped"]["source_commits"]),
            "release_version": "3.8.5",
            "from_publisher": dict(self.current),
            "to_publisher": dict(self.proposed),
            "review": {
                "status": "approved",
                "reference": "https://github.com/elsa-workflows/elsa-core/pull/9000",
                "commit_sha": "a" * 40,
            },
            "old_publisher_disabled": {
                "state": "disabled",
                "repository": self.current["repository"],
                "workflow_path": self.current["workflow_path"],
                "commit_sha": "b" * 40,
                "workflow_sha256": "c" * 64,
                "evidence_url": "https://github.com/elsa-workflows/elsa-extensions/blob/main/.github/workflows/packages.yml",
                "observed_at": "2026-09-25T10:00:00Z",
            },
            "new_publisher_enabled": {
                "state": "enabled",
                "repository": self.proposed["repository"],
                "workflow_path": self.proposed["workflow_path"],
                "commit_sha": "d" * 40,
                "workflow_sha256": "e" * 64,
                "evidence_url": "https://github.com/elsa-workflows/elsa-core/blob/main/.github/workflows/packages.yml",
                "observed_at": "2026-09-25T11:00:00Z",
            },
            "publication_performed": False,
        }


if __name__ == "__main__":
    unittest.main()
