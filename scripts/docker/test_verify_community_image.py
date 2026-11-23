import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from verify_community_image import BUILDKIT_TYPE, PUBLISHER, SLSA, SPDX, spdx_validator, verify_image


class PlatformEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.schema = spdx_validator(os.environ.get("SPDX_SCHEMA"))

    def setUp(self):
        # Digests stand in for blob handles here. ORAS readback verifies their bytes in the release path.
        self.index = {"manifests": []}
        self.documents = {"index": self.index}
        for arch in ("amd64", "arm64"):
            child = f"sha256:{arch}"
            layers = []
            self.index["manifests"].append({"platform": {"os": "linux", "architecture": arch}, "digest": child})
            self.index["manifests"].append({
                "digest": f"evidence-{arch}", "platform":{"os":"unknown","architecture":"unknown"}, "annotations": {
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
            runtime = {"amd64": "x64", "arm64": "arm64"}[arch]
            purls = ["pkg:deb/ubuntu/libc6", f"pkg:nuget/Microsoft.AspNetCore.App.Runtime.linux-{runtime}@10.0.15",
                     "pkg:nuget/Elsa.Server.Web"]
            packages = [{
                "SPDXID": f"SPDXRef-Package-{i}", "name": purl.split("/")[-1], "downloadLocation": "NOASSERTION",
                "externalRefs": [{"referenceCategory": "PACKAGE-MANAGER", "referenceType": "purl", "referenceLocator": purl}],
            } for i, purl in enumerate(purls)]
            sbom = {
                "SPDXID": "SPDXRef-DOCUMENT", "name": f"sample-{arch}", "spdxVersion": "SPDX-2.3",
                "dataLicense": "CC0-1.0", "documentNamespace": f"https://example.com/image/{arch}",
                "creationInfo": {"created": "2026-01-01T00:00:00Z", "creators": ["Tool: syft"]}, "packages": packages,
            }
            provenance = {
                "buildDefinition": {"buildType": BUILDKIT_TYPE, "externalParameters": {"request": {"frontend": "dockerfile.v0"}}},
                "runDetails": {"builder": {"id": PUBLISHER}},
            }
            for kind, predicate in ((SPDX, sbom), (SLSA, provenance)):
                key = f"{arch}-{kind}"
                layers.append({"digest": key, "mediaType": "application/vnd.in-toto+json",
                               "annotations": {"in-toto.io/predicate-type": kind}})
                self.documents[key] = {"_type": "https://in-toto.io/Statement/v1", "predicateType": kind,
                                       "subject": [{"digest": {"sha256": arch}}], "predicate": predicate}

    def verify(self):
        with tempfile.TemporaryDirectory() as out, patch(
            "verify_community_image.read_document", side_effect=lambda _, digest, **kwargs: self.documents[digest]
        ):
            verify_image("sample@index", Path(out), "source", self.schema)
            return sorted(path.name for path in Path(out).glob("*.spdx.json"))

    def test_both_platform_inventories(self):
        self.assertEqual(self.verify(), ["amd64.spdx.json", "arm64.spdx.json"])

    def test_swapped_subject_with_correct_descriptor(self):
        self.documents[f"amd64-{SPDX}"]["subject"] = [{"digest": {"sha256": "arm64"}}]
        with self.assertRaisesRegex(ValueError, "Wrong subject"):
            self.verify()

    def test_runtime_inventory_must_match_platform(self):
        self.documents[f"arm64-{SPDX}"]["predicate"]["packages"][1] = self.documents[f"amd64-{SPDX}"]["predicate"]["packages"][1]
        with self.assertRaisesRegex(ValueError, "runtime|Runtime"):
            self.verify()

    def test_attestation_cannot_advertise_a_runnable_platform(self):
        self.index["manifests"][1]["platform"] = {"os": "linux", "architecture": "amd64"}
        with self.assertRaisesRegex(ValueError, "non-runnable"):
            self.verify()

    def test_oci_artifact_subject(self):
        manifest = self.documents["evidence-amd64"]
        manifest["artifactType"] = "application/vnd.docker.attestation.manifest.v1+json"
        for subject in ({}, {"digest": "sha256:arm64"}):
            with self.subTest(subject=subject), self.assertRaisesRegex(ValueError, "OCI subject"):
                manifest["subject"] = subject
                self.verify()

    def test_target_variant_requires_baseline_arm64(self):
        self.index["manifests"][2]["platform"]["variant"] = "v9"
        with self.assertRaisesRegex(ValueError, "variant"):
            self.verify()

    def test_image_config_variant_requires_baseline_amd64(self):
        self.documents["config-amd64"]["variant"] = "v3"
        with self.assertRaisesRegex(ValueError, "variant"):
            self.verify()

    def test_spdx_document_requires_metadata(self):
        sbom = self.documents[f"amd64-{SPDX}"]["predicate"]
        for field in ("creationInfo", "dataLicense"):
            with self.subTest(field=field):
                value = sbom.pop(field)
                with self.assertRaisesRegex(ValueError, field):
                    self.verify()
                sbom[field] = value

    def test_malformed_external_parameters(self):
        self.documents[f"amd64-{SLSA}"]["predicate"]["buildDefinition"]["externalParameters"] = "placeholder"
        with self.assertRaisesRegex(ValueError, "parameters"):
            self.verify()

    def test_wrong_provenance_target(self):
        definition = self.documents[f"arm64-{SLSA}"]["predicate"]["buildDefinition"]
        definition["internalParameters"] = {"targetPlatform": "linux/amd64"}
        with self.assertRaisesRegex(ValueError, "target"):
            self.verify()

    def test_missing_builder_identity(self):
        self.documents[f"amd64-{SLSA}"]["predicate"]["runDetails"] = {"builder": {}}
        with self.assertRaisesRegex(ValueError, "build|provenance"):
            self.verify()

    def test_invalid_build_definition(self):
        self.documents[f"amd64-{SLSA}"]["predicate"]["buildDefinition"] = "placeholder"
        with self.assertRaisesRegex(ValueError, "build|provenance"):
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
