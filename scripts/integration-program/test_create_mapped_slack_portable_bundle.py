import base64
import hashlib
import json
import os
from pathlib import Path
import tarfile
import tempfile
import unittest
import zipfile

from create_mapped_slack_portable_bundle import create_bundle, required_paths
from run_mapped_slack_package_proof import PACKAGE_ID, PACKAGE_VERSION, TFMS
from verify_mapped_slack_consumer_provenance import verify_retained_evidence


class PortableSlackBundleTests(unittest.TestCase):
    def test_downloaded_bundle_rechecks_all_four_consumers(self):
        with tempfile.TemporaryDirectory() as temporary:
            parent = Path(temporary)
            root = parent / "recorded-runner-proof"
            feed = root / "local-feed"
            feed.mkdir(parents=True)
            package = feed / f"{PACKAGE_ID}.{PACKAGE_VERSION}.nupkg"
            with zipfile.ZipFile(package, "w") as archive:
                for framework in TFMS:
                    archive.writestr(f"lib/{framework}/{PACKAGE_ID}.dll", framework.encode())
            package_bytes = package.read_bytes()
            package_hash = hashlib.sha256(package_bytes).hexdigest()
            package_sha512 = base64.b64encode(hashlib.sha512(package_bytes).digest()).decode()
            identity = f"{PACKAGE_ID}/{PACKAGE_VERSION}"
            cache_relative = Path(PACKAGE_ID.casefold()) / PACKAGE_VERSION.casefold()
            recorded_runner_root = Path('/home/runner/work/_temp/elsa-slack-proof-fixture')
            for consumer, framework in [*((name, name) for name in TFMS),
                                        ("offline-activity-smoke", "net10.0")]:
                directory = root / "consumers" / consumer if consumer != "offline-activity-smoke" else root / consumer
                cache = (root / "package-caches" / "consumers" / consumer if consumer != "offline-activity-smoke"
                         else root / "package-caches" / consumer)
                recorded_cache = (recorded_runner_root / "package-caches" / "consumers" / consumer
                                  if consumer != "offline-activity-smoke"
                                  else recorded_runner_root / "package-caches" / consumer)
                cached_package = cache / cache_relative
                cached_package.mkdir(parents=True)
                (cached_package / ".nupkg.metadata").write_text(json.dumps({
                    "source": str(recorded_runner_root / "local-feed"), "contentHash": package_sha512,
                }))
                (cached_package / f"{PACKAGE_ID.casefold()}.{PACKAGE_VERSION.casefold()}.nupkg.sha512").write_text(package_sha512)
                (cached_package / f"{PACKAGE_ID.casefold()}.{PACKAGE_VERSION.casefold()}.nupkg").write_bytes(package_bytes)
                assets = directory / "obj" / "project.assets.json"
                assets.parent.mkdir(parents=True)
                assembly_asset = f"lib/{framework}/{PACKAGE_ID}.dll"
                assets.write_text(json.dumps({
                    "targets": {framework: {identity: {"type": "package", "compile": {assembly_asset: {}},
                                                      "runtime": {assembly_asset: {}}}}},
                    "libraries": {identity: {"type": "package", "path": cache_relative.as_posix(),
                                              "sha512": package_sha512}},
                    "packageFolders": {str(recorded_cache) + os.sep: {}},
                }))
                output_assembly = directory / "bin" / "Debug" / framework / f"{PACKAGE_ID}.dll"
                output_assembly.parent.mkdir(parents=True)
                output_assembly.write_bytes(framework.encode())
            (root / "evidence.json").write_text(json.dumps({
                "result": "passed", "publication_authorized": False, "proof_root": str(recorded_runner_root),
                "package": {"package_id": PACKAGE_ID, "package_version": PACKAGE_VERSION,
                            "nupkg_sha256": package_hash},
            }))

            bundle = parent / "portable.tar.gz"
            create_bundle(root / "evidence.json", bundle)
            relocated = parent / "downloaded"
            relocated.mkdir()
            with tarfile.open(bundle, "r:gz") as archive:
                archive.extractall(relocated, filter="data")
            result = verify_retained_evidence(relocated / "evidence.json")
            self.assertEqual("passed", result["result"])
            self.assertEqual(4, result["consumer_count"])

    def test_bundle_retains_only_offline_verifier_inputs(self):
        with tempfile.TemporaryDirectory() as temporary:
            parent = Path(temporary)
            root = parent / "proof"
            root.mkdir()
            for relative in required_paths():
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(b"synthetic verifier input")
            (root / "evidence.json").write_text(json.dumps({
                "result": "passed", "publication_authorized": False,
            }))
            (root / "private-not-for-upload.txt").write_text("excluded")
            output = parent / "portable.tar.gz"
            receipt = create_bundle(root / "evidence.json", output)
            self.assertEqual(len(required_paths()), receipt["fileCount"])
            with tarfile.open(output, "r:gz") as archive:
                self.assertEqual({path.as_posix() for path in required_paths()},
                                 {member.name for member in archive.getmembers()})

    def test_missing_or_symlinked_input_fails_closed(self):
        with tempfile.TemporaryDirectory() as temporary:
            parent = Path(temporary)
            root = parent / "proof"
            root.mkdir()
            for relative in required_paths():
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(b"synthetic verifier input")
            (root / "evidence.json").write_text(json.dumps({
                "result": "passed", "publication_authorized": False,
            }))
            target = root / required_paths()[-1]
            target.unlink()
            with self.assertRaisesRegex(ValueError, "Missing or unsafe"):
                create_bundle(root / "evidence.json", parent / "missing.tar.gz")
            target.symlink_to(root / "evidence.json")
            with self.assertRaisesRegex(ValueError, "Missing or unsafe"):
                create_bundle(root / "evidence.json", parent / "symlink.tar.gz")


if __name__ == "__main__":
    unittest.main()
