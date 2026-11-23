import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from verify_community_image import SLSA, SPDX, verify_image


class PlatformEvidenceTests(unittest.TestCase):
    def setUp(self):
        # Digests stand in for blob handles here. ORAS readback verifies their bytes in the release path.
        self.index = {"manifests": []}
        self.documents = {"index": self.index}
        for arch in ("amd64", "arm64"):
            child = f"sha256:{arch}"
            layers = []
            self.index["manifests"].append({"platform": {"os": "linux", "architecture": arch}, "digest": child})
            self.index["manifests"].append({
                "digest": f"evidence-{arch}", "annotations": {
                    "vnd.docker.reference.type": "attestation-manifest",
                    "vnd.docker.reference.digest": child,
                },
            })
            self.documents[child] = {"config": {"digest": f"config-{arch}"}}
            self.documents[f"config-{arch}"] = {
                "os": "linux", "architecture": arch,
                "config": {"Labels": {"org.opencontainers.image.revision": "source"}},
            }
            self.documents[f"evidence-{arch}"] = {"layers": layers}
            packages = [{"externalRefs": [{"referenceType": "purl", "referenceLocator": purl}]} for purl in
                        ("pkg:deb/ubuntu/libc6", "pkg:nuget/Microsoft.AspNetCore.App.Runtime.linux-" + arch,
                         "pkg:nuget/Elsa.Server.Web")]
            for kind, predicate in ((SPDX, {"spdxVersion": "SPDX-2.3", "packages": packages}),
                                    (SLSA, {"buildDefinition": {"buildType": "BuildKit"}, "runDetails": {"builder": {}}})):
                key = f"{arch}-{kind}"
                layers.append({"digest": key, "mediaType": "application/vnd.in-toto+json",
                               "annotations": {"in-toto.io/predicate-type": kind}})
                self.documents[key] = {"_type": "https://in-toto.io/Statement/v1", "predicateType": kind,
                                       "subject": [{"digest": {"sha256": arch}}], "predicate": predicate}

    def verify(self):
        with tempfile.TemporaryDirectory() as out, patch(
            "verify_community_image.read_document", side_effect=lambda _, digest, **kwargs: self.documents[digest]
        ):
            verify_image("sample@index", Path(out), "source")
            return sorted(path.name for path in Path(out).glob("*.spdx.json"))

    def test_both_platform_inventories(self):
        self.assertEqual(self.verify(), ["amd64.spdx.json", "arm64.spdx.json"])

    def test_swapped_subject_with_correct_descriptor(self):
        self.documents[f"amd64-{SPDX}"]["subject"] = [{"digest": {"sha256": "arm64"}}]
        with self.assertRaisesRegex(ValueError, "Wrong subject"):
            self.verify()

    def test_incomplete_inventory(self):
        self.documents[f"amd64-{SPDX}"]["predicate"]["packages"].pop()
        with self.assertRaisesRegex(ValueError, "Missing expected pkg:nuget/Elsa"):
            self.verify()

    def test_missing_platform(self):
        self.index["manifests"].pop(0)
        with self.assertRaisesRegex(ValueError, "Expected distinct"):
            self.verify()

    def test_provenance_without_sbom(self):
        self.documents["evidence-amd64"]["layers"].pop(0)
        with self.assertRaisesRegex(ValueError, "Missing SPDX"):
            self.verify()
