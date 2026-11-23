import hashlib
import io
import json
import os
import shutil
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parent))
import nuget_release_evidence as evidence


def archive(entries):
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, "w") as output:
        for name, data in entries.items():
            output.writestr(name, data)
    return stream.getvalue()


class ReleaseEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.version = "3.10.3"
        self.package = self.root / "Elsa.3.10.3.nupkg"
        self.original = archive({"Elsa.nuspec": b"package", "lib/Elsa.dll": b"assembly"})
        self.signed = archive({"Elsa.nuspec": b"package", "lib/Elsa.dll": b"assembly", ".signature.p7s": b"marker"})
        self.package.write_bytes(self.original)
        self.bom = b'{"bomFormat":"CycloneDX"}'
        self.row = {"id": "Elsa", "nupkg": self.package.name, "candidate_sha256": evidence.sha(self.original),
                    "frameworks": {"net10.0": {"path": "boms/Elsa/net10.0.cdx.json", "sha256": evidence.sha(self.bom)}}}
        self.index = dict(schema=1, version=self.version, producer_run=123, producer_attempt=1,
                          source_commit="f" * 40, tooling_commit="e" * 40, packages=[self.row])

    def readback(self, published, excluded=()):
        def get(url):
            return self.original if url.startswith(evidence.FEEDS["feedz"]) else published
        with patch.object(evidence, "download", side_effect=get), patch.object(evidence, "verify_nuget_signature") as verify:
            result = evidence.readback(self.row, version=self.version, excluded=excluded, stage=self.root, fingerprints=["f" * 64])
        return result, verify.call_count

    def test_signed_registry_copy_and_excluded_target(self):
        result, signatures = self.readback(self.signed)
        self.assertEqual(signatures, 1)
        self.assertNotEqual(result["feeds"]["feedz"]["sha256"], result["feeds"]["nuget"]["sha256"])
        result, signatures = self.readback(self.signed, {"elsa"})
        self.assertEqual(signatures, 0)
        self.assertEqual(set(result["feeds"]), {"feedz"})

    def test_reject_changed_payload_even_when_signature_check_passes(self):
        with self.assertRaisesRegex(ValueError, "payload differs"):
            self.readback(archive({"Elsa.nuspec": b"package", ".signature.p7s": b"marker"}))

    def test_nuget_signer_and_content_hash_must_both_match(self):
        import base64
        content_hash = base64.b64encode(hashlib.sha512(self.original).digest()).decode()
        output = f"Content hash: {content_hash}\nSignature type: Repository\nService index: https://api.nuget.org/v3/index.json"
        for invalid in (output.replace(content_hash, "wrong"), output.replace("Repository", "Author"),
                        output.replace("nuget.org", "example.com")):
            with self.subTest(invalid=invalid), patch.object(subprocess, "run", return_value=subprocess.CompletedProcess([], 0, stdout=invalid)):
                with self.assertRaises(ValueError):
                    evidence.verify_nuget_signature(self.package, ["f" * 64], self.original)

    def make_release(self):
        signed_index = self.root / evidence.INDEX
        bundle = self.root / evidence.BUNDLE
        bundle.write_bytes(archive({evidence.CANDIDATE: json.dumps(self.index),
                                    "boms/Elsa/net10.0.cdx.json": self.bom}))
        row = dict(self.row, feeds={"nuget": {"sha256": evidence.sha(self.signed)}})
        signed_index.write_text(json.dumps(dict(self.index, packages=[row], sbom_bundle={"sha256": evidence.sha(bundle.read_bytes())})))
        self.package.write_bytes(self.signed)
        return signed_index, bundle

    def test_release_asset_retry_and_conflicting_existing_bytes(self):
        import yaml
        workflow = Path(__file__).resolve().parents[1] / ".github/workflows/nuget-release-evidence.yml"
        script = yaml.safe_load(workflow.read_text())["jobs"]["attach"]["steps"][-1]["run"]
        local = self.root / "evidence"
        remote = self.root / "release"
        binary = self.root / "bin"
        for directory in (local, remote, binary):
            directory.mkdir()
        bundle = local / evidence.BUNDLE
        index = local / evidence.INDEX
        bundle.write_bytes(b"original bundle")
        index.write_text(json.dumps({"sbom_bundle": {"sha256": evidence.sha(bundle.read_bytes())}}))
        shutil.copyfile(bundle, remote / bundle.name)
        gh = binary / "gh"
        gh.write_text("""#!/usr/bin/env python3
import json, os, shutil, sys
from pathlib import Path
release = Path(os.environ["RELEASE"])
args = sys.argv[1:]
if args[0] == "api" and "/commits/" in args[1]:
    print(os.environ["SOURCE"])
elif args[0] == "api":
    print(json.dumps(dict(tag_name=os.environ["VERSION"], draft=False, prerelease=False,
                          assets=[{"name": f.name} for f in release.iterdir()])))
elif args[:2] == ["release", "download"]:
    sys.stdout.buffer.write((release / args[args.index("--pattern")+1]).read_bytes())
elif args[:2] == ["release", "upload"]:
    for name in args[args.index("--repo")+2:]:
        with (release / name).open("xb") as out:
            out.write(Path(name).read_bytes())
else:
    sys.exit(1)
""")
        curl = binary / "curl"
        curl.write_text('#!/bin/bash\nurl="${!#}"\ncat "$RELEASE/${url##*/}"\n')
        for executable in (gh, curl):
            executable.chmod(0o755)
        env = dict(os.environ, PATH=str(binary) + os.pathsep + os.environ["PATH"],
                   RUNNER_TEMP=str(self.root), RELEASE=str(remote), SOURCE="f" * 40, VERSION=self.version,
                   INDEX_DIGEST="sha256:" + evidence.sha(index.read_bytes()), GITHUB_REPOSITORY=evidence.REPO)
        for _ in range(2):
            result = subprocess.run(["bash", "-c", script], env=env, capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(index.read_bytes(), (remote / index.name).read_bytes())
        (remote / index.name).unlink()
        (remote / bundle.name).write_bytes(b"conflicting upload")
        self.assertNotEqual(subprocess.run(["bash", "-c", script], env=env, capture_output=True).returncode, 0)
        self.assertFalse((remote / index.name).exists())
        self.assertEqual((remote / bundle.name).read_bytes(), b"conflicting upload")

    def test_consumer_requires_correct_release_and_producer_signers(self):
        signed_index, bundle = self.make_release()
        with patch.object(evidence.subprocess, "run") as run:
            evidence.verify(self.version, "Elsa", "net10.0", "nuget", self.package, signed_index, bundle)
        commands = [c.args[0] for c in run.call_args_list]
        self.assertIn(evidence.SIGNER, commands[0])
        self.assertIn(evidence.PRODUCER, commands[1])
        self.assertIn(self.index["tooling_commit"], commands[1])
        self.assertTrue(all("--deny-self-hosted-runners" in c and "refs/heads/main" in c for c in commands))
        with patch.object(evidence.subprocess, "run", side_effect=subprocess.CalledProcessError(1, "wrong signer")):
            with self.assertRaises(subprocess.CalledProcessError):
                evidence.verify(self.version, "Elsa", "net10.0", "nuget", self.package, signed_index, bundle)

    def test_wrong_framework_package_or_incomplete_bundle_fails(self):
        signed_index, bundle = self.make_release()
        with patch.object(evidence, "verify_attestation"):
            with self.assertRaisesRegex(ValueError, "Framework"):
                evidence.verify(self.version, "Elsa", "net8.0", "nuget", self.package, signed_index, bundle)
            self.package.write_bytes(b"changed")
            with self.assertRaisesRegex(ValueError, "feed artifact"):
                evidence.verify(self.version, "Elsa", "net10.0", "nuget", self.package, signed_index, bundle)
            bundle.write_bytes(b"incomplete")
            with self.assertRaisesRegex(ValueError, "SBOM bundle"):
                evidence.verify(self.version, "Elsa", "net10.0", "nuget", self.package, signed_index, bundle)


if __name__ == "__main__":
    unittest.main()
