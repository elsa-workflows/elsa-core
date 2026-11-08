"""Distinct 3.10.1 pin stays unpublished and does not retarget 3.10.0 publication."""
from __future__ import annotations

import hashlib
import json
import sys
import unittest
from pathlib import Path

import consolidated_3101_candidate_input as pin_3101
import consolidated_candidate_input as pin_3100
import consolidated_package_executor as executor

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from stage_consolidated_candidate_nupkgs import PINNED_TAG
from validate_packages_publication_selection import selection_errors

ROOT = Path(__file__).resolve().parents[2]
PACKET_3100 = ROOT / "docs/integration-program/feedz-publish-operational-packet.md"
PACKET_3101 = ROOT / "docs/integration-program/feedz-publish-3.10.1-operational-packet.md"
# Exact bytes of the unsigned 3.10.1 packet in this revision. Sipke signs this
# digest; the file itself must not contain it (that would change the digest).
UNSIGNED_PACKET_3101_SHA256 = "6fe9e0b88a6d7503c61ead7a44208364dce1389e2156ccf034299e1b6d2c4bb0"
STAGED_3100_PACKET_SHA256 = "416725fcd31207336a3fd9bd0d4eb18d2ff618ddcda9fb137dcc5b1d87f49d17"
WITHDRAWN_WRONG_PACKET_SHA256 = "1373d68b2981946fff5d12c039fb43df47344e1d02a443f7947f9351a3497461"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class Distinct3101PinTests(unittest.TestCase):
    def test_3101_envelope_bytes_match_the_pin_constants(self):
        self.assertEqual(sha256(pin_3101.ORIGINAL_ENVELOPE), pin_3101.ENVELOPE_SHA256)
        envelope = json.loads(pin_3101.ORIGINAL_ENVELOPE.read_text(encoding="utf-8"))
        self.assertEqual("3.10.1", envelope["version"])
        self.assertFalse(envelope["published"])
        self.assertEqual(pin_3101.SOURCE, envelope["source_commit"])
        self.assertEqual(pin_3101.RUN, envelope["run_id"])
        self.assertEqual(1, envelope["run_attempt"])
        self.assertEqual(pin_3101.ARTIFACT, envelope["artifact_id"])
        self.assertEqual(96234443, envelope["archive_size"])
        self.assertEqual("2026-12-08T08:00:48Z", envelope["expires_at"])
        self.assertEqual("2026-11-08T08:01:07Z", envelope["retrieved_at"])
        self.assertEqual(pin_3101.ARCHIVE_SHA256, envelope["archive_sha256"])
        self.assertEqual(pin_3101.MANIFEST_SHA256, envelope["preupload_manifest_sha256"])

    def test_3101_identity_is_distinct_from_staged_3100(self):
        self.assertNotEqual(pin_3100.SOURCE, pin_3101.SOURCE)
        self.assertNotEqual(pin_3100.ARTIFACT, pin_3101.ARTIFACT)
        self.assertNotEqual(pin_3100.RUN, pin_3101.RUN)
        self.assertNotEqual(pin_3100.ARCHIVE_SHA256, pin_3101.ARCHIVE_SHA256)
        self.assertNotEqual(pin_3100.MANIFEST_SHA256, pin_3101.MANIFEST_SHA256)
        self.assertNotEqual(pin_3100.ENVELOPE_SHA256, pin_3101.ENVELOPE_SHA256)
        self.assertNotEqual(pin_3100.ORIGINAL_ENVELOPE.resolve(), pin_3101.ORIGINAL_ENVELOPE.resolve())
        self.assertEqual("3.10.0", pin_3100.PRODUCER["version"])
        self.assertEqual("3.10.1", pin_3101.PRODUCER["version"])
        self.assertEqual(231, pin_3100.PIN_PACKAGE_COUNT)
        self.assertEqual(231, pin_3101.PIN_PACKAGE_COUNT)
        self.assertEqual(230, pin_3101.PROMOTE_PACKAGE_COUNT)
        self.assertEqual("Elsa.Platform.Integration", pin_3101.PROMOTE_EXCLUSION_ID)
        self.assertNotEqual("Elsa.Testing.Shared.Integration", pin_3101.PROMOTE_EXCLUSION_ID)
        self.assertNotEqual("Elsa.Studio.PlatformIntegration", pin_3101.PROMOTE_EXCLUSION_ID)

    def test_230_promote_set_excludes_only_platform_integration(self):
        staged = {row["id"] for row in json.loads(
            pin_3100.PIN_INVENTORY.read_text(encoding="utf-8"))["packages"]}
        self.assertEqual(231, len(staged))
        for package_id in pin_3101.STAGE_INTEGRATION_IDS:
            self.assertIn(package_id, staged)
        self.assertIn("Elsa.Testing.Shared.Integration", staged)
        self.assertIn("Elsa.Platform.Integration", staged)
        promote = staged - {pin_3101.PROMOTE_EXCLUSION_ID}
        self.assertEqual(230, len(promote))
        self.assertNotIn("Elsa.Platform.Integration", promote)
        self.assertIn("Elsa.Testing.Shared.Integration", promote)
        self.assertIn("Elsa.Studio.PlatformIntegration", promote)
        self.assertEqual({pin_3101.PROMOTE_EXCLUSION_ID}, staged - promote)

    def test_3100_pin_files_and_executor_policy_are_unchanged(self):
        self.assertEqual("5cff67228e550b353b3483eb3b37dfdb73a41c74", pin_3100.SOURCE)
        self.assertEqual(12664519771, pin_3100.ARTIFACT)
        self.assertEqual(40347931480, pin_3100.RUN)
        self.assertEqual(sha256(pin_3100.ORIGINAL_ENVELOPE), pin_3100.ENVELOPE_SHA256)
        self.assertEqual(STAGED_3100_PACKET_SHA256, sha256(PACKET_3100))
        self.assertEqual(STAGED_3100_PACKET_SHA256, executor.REVIEWED_OPERATIONAL_POLICY["operational_packet_sha256"])
        self.assertIs(executor.candidate_input, pin_3100)
        self.assertEqual("refs/tags/3.10.0", PINNED_TAG)

    def test_unsigned_3101_packet_binds_the_new_identity_and_is_not_the_executor_pin(self):
        text = PACKET_3101.read_text(encoding="utf-8")
        digest = sha256(PACKET_3101)
        self.assertEqual(UNSIGNED_PACKET_3101_SHA256, digest)
        self.assertNotEqual(STAGED_3100_PACKET_SHA256, digest)
        self.assertNotEqual(WITHDRAWN_WRONG_PACKET_SHA256, digest)
        self.assertNotEqual(digest, executor.REVIEWED_OPERATIONAL_POLICY["operational_packet_sha256"])
        self.assertNotIn(digest, text)
        self.assertIn("pending", text.lower())
        self.assertIn("no signature", text.lower())
        self.assertIn(pin_3101.SOURCE, text)
        self.assertIn(str(pin_3101.ARTIFACT), text)
        self.assertIn(pin_3101.ARCHIVE_SHA256, text)
        self.assertIn(pin_3101.MANIFEST_SHA256, text)
        self.assertIn(pin_3101.ENVELOPE_SHA256, text)
        self.assertIn("42458011137", text)
        self.assertIn("231", text)
        self.assertIn("230", text)
        self.assertIn("Elsa.Platform.Integration", text)
        self.assertIn("Elsa.Testing.Shared.Integration", text)
        self.assertIn("exclude only `Elsa.Platform.Integration`", text)
        self.assertIn("stay in the 230", text)
        self.assertIn("retains Feedz and nuget.org", text)
        self.assertIn("938393", text)
        self.assertIn("25739609238", text)
        self.assertIn("refs/heads/main", text)
        self.assertIn("MAX_SECONDS = 1800", text)
        self.assertIn("#9192", text)
        self.assertIn("#9198", text)
        self.assertNotIn("BEGIN PGP", text)
        self.assertNotIn("sig-ok", text.lower())
        self.assertNotIn("signed-off-by: sfmskywalker", text.lower())
        self.assertNotIn("skip Feedz", text)
        self.assertNotIn("nuget.org only", text)
        self.assertNotIn("exclude `Elsa.Testing.Shared.Integration`", text)

    def test_publication_selection_rejects_old_and_unwired_3101_promotion(self):
        self.assertIn("must not be promoted", " ".join(selection_errors("refs/tags/3.10.0", nuget=True)))
        self.assertIn("blocked until a distinct", " ".join(selection_errors("refs/tags/3.10.1", nuget=True)))
        self.assertIn(
            "owned by publish-consolidated-original-candidate",
            " ".join(selection_errors("refs/tags/3.10.1", feedz=True)),
        )
        self.assertEqual([], selection_errors("refs/tags/3.8.5", nuget=True))


if __name__ == "__main__":
    unittest.main()
