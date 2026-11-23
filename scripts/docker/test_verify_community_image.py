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
                "config": {"Cmd": ["dotnet", "Elsa.Server.Web.dll"], "Labels": {"org.opencontainers.image.revision": "source"}},
            }
            self.documents[f"evidence-{arch}"] = {"layers": layers}
            runtime = {"amd64": "x64", "arm64": "arm64"}[arch]
            entries = [
                ("libc6", "2.39", f"pkg:deb/ubuntu/libc6@2.39?arch={arch}"),
                (f"Microsoft.AspNetCore.App.Runtime.linux-{runtime}", "10.0.15",
                 f"pkg:nuget/Microsoft.AspNetCore.App.Runtime.linux-{runtime}@10.0.15"),
                ("Elsa.Server.Web", "1.0.0", "pkg:nuget/Elsa.Server.Web@1.0.0"),
            ]
            packages = [{
                "SPDXID": f"SPDXRef-Package-{i}", "name": name, "versionInfo": version, "downloadLocation": "NOASSERTION",
                "externalRefs": [{"referenceCategory": "PACKAGE-MANAGER", "referenceType": "purl", "referenceLocator": purl}],
            } for i, (name, version, purl) in enumerate(entries)]
            sbom = {
                "SPDXID": "SPDXRef-DOCUMENT", "name": f"sample-{arch}", "spdxVersion": "SPDX-2.3",
                "dataLicense": "CC0-1.0", "documentNamespace": f"https://example.com/image/{arch}",
                "creationInfo": {"created": "2026-01-01T00:00:00Z", "creators": ["Tool: syft"]}, "packages": packages,
            }
            provenance = {
                "buildDefinition": {"buildType": BUILDKIT_TYPE, "externalParameters": {
                    "request": {"frontend": "dockerfile.v0"}, "configSource": {"path": "ElsaServer.Dockerfile"}},
                    "internalParameters": {"buildConfig": {"llbDefinition": [{"id": "step0"}]}},
                    "resolvedDependencies": [{"uri": f"pkg:docker/mcr.microsoft.com/dotnet/aspnet@10.0?platform=linux%2F{arch}", "digest": {"sha256": "a"*64}}]},
                "runDetails": {"builder": {"id": PUBLISHER}, "metadata": {"invocationId": "run-1"}},
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
            verify_image("docker.io/elsaworkflows/elsa-server-v3@index", Path(out), "source", self.schema)
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

    def test_unrelated_elsa_package_or_invalid_version_is_insufficient(self):
        package = self.documents[f"amd64-{SPDX}"]["predicate"]["packages"][2]
        for name, version, purl in (("Elsa.Other", "1.0.0", "pkg:nuget/Elsa.Other@1.0.0"),
                                    ("Elsa.Server.Web", "bad version", "pkg:nuget/Elsa.Server.Web@bad%20version"),
                                    ("Elsa.Server.Web", "1.0.0", "pkg:nuget/Elsa.Server.Web@2.0.0")):
            with self.subTest(purl=purl), self.assertRaisesRegex(ValueError, "Elsa.Server.Web"):
                package.update(name=name, versionInfo=version)
                package["externalRefs"][0]["referenceLocator"] = purl
                self.verify()

    def test_empty_debian_locator_does_not_identify_an_os_package(self):
        self.documents[f"amd64-{SPDX}"]["predicate"]["packages"][0]["externalRefs"][0]["referenceLocator"] = "pkg:deb/"
        with self.assertRaisesRegex(ValueError, "libc6"):
            self.verify()

    def test_host_command_belongs_to_image(self):
        self.documents["config-amd64"]["config"]["Cmd"] = ["dotnet", "Elsa.Studio.Host.Server.dll"]
        with self.assertRaisesRegex(ValueError, "entry assembly"):
            self.verify()

    def test_empty_request_wrong_dockerfile_and_missing_build_inputs(self):
        definition = self.documents[f"amd64-{SLSA}"]["predicate"]["buildDefinition"]
        external = definition["externalParameters"]
        for field, value, message in (("externalParameters", {}, "request"), ("resolvedDependencies", [], "inputs"),
                                      ("externalParameters", dict(external, configSource={"path": "ElsaStudio.Dockerfile"}), "config source")):
            with self.subTest(field=field, value=value):
                previous = definition[field]
                definition[field] = value
                with self.assertRaisesRegex(ValueError, message):
                    self.verify()
                definition[field] = previous

    def test_swapped_base_inputs_rejected_without_target_field(self):
        arm = self.documents[f"arm64-{SLSA}"]["predicate"]["buildDefinition"]
        amd = self.documents[f"amd64-{SLSA}"]["predicate"]["buildDefinition"]
        arm["resolvedDependencies"] = amd["resolvedDependencies"]
        with self.assertRaisesRegex(ValueError, "base|platform"):
            self.verify()

    def test_missing_invocation_fails(self):
        self.documents[f"amd64-{SLSA}"]["predicate"]["runDetails"]["metadata"] = {}
        with self.assertRaisesRegex(ValueError, "invocation"):
            self.verify()

    def test_missing_build_config_fails(self):
        self.documents[f"amd64-{SLSA}"]["predicate"]["buildDefinition"]["internalParameters"] = {}
        with self.assertRaisesRegex(ValueError, "configuration"):
            self.verify()

    def test_wrong_provenance_target(self):
        definition = self.documents[f"arm64-{SLSA}"]["predicate"]["buildDefinition"]
        definition["internalParameters"] = {"targetPlatform": "linux/amd64"}
        with self.assertRaisesRegex(ValueError, "target"):
            self.verify()

    def test_missing_builder_identity(self):
        self.documents[f"amd64-{SLSA}"]["predicate"]["runDetails"]["builder"] = {}
        with self.assertRaisesRegex(ValueError, "build|provenance"):
            self.verify()

    def test_invalid_build_definition(self):
        self.documents[f"amd64-{SLSA}"]["predicate"]["buildDefinition"] = "placeholder"
        with self.assertRaisesRegex(ValueError, "build|provenance"):
            self.verify()

    def test_incomplete_inventory(self):
        self.documents[f"amd64-{SPDX}"]["predicate"]["packages"].pop()
        with self.assertRaisesRegex(ValueError, "Elsa.Server.Web"):
            self.verify()

    def test_missing_platform(self):
        self.index["manifests"].pop(0)
        with self.assertRaisesRegex(ValueError, "Expected distinct"):
            self.verify()

    def test_provenance_without_sbom(self):
        self.documents["evidence-amd64"]["layers"].pop(0)
        with self.assertRaisesRegex(ValueError, "Missing SPDX"):
            self.verify()
