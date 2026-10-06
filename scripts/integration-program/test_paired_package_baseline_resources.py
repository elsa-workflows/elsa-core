import copy
import json
from pathlib import Path
import tempfile
import unittest
from zipfile import ZipFile

import paired_package_baseline_provenance as baseline
from paired_package_baseline_resources import BASELINE_REQUESTED_ASSETS, derive_baseline_resources
from verify_browser_package_resources import CONVERTER_POLICY, sha256


class BaselineResourceTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.cache = self.root / "cache"
        self.build = self.root / "build.json"
        self.policy = json.loads(baseline.POLICY_PATH.read_text())
        self.policy["packages"] = []
        self.rows = []
        required = json.loads(CONVERTER_POLICY.with_name("coverage-policy.json").read_text())["required_browser_assets"]
        for package_id in sorted({path.split("/")[2] for path in required}):
            record = {"id": package_id, "version": "3.8.4", "owner": "elsa-studio",
                      "repository": {"type": "git", "url": baseline.REPOSITORIES["elsa-studio"],
                                     "commit": baseline.RELEASE_COMMITS["elsa-studio"]["3.8.4"]}}
            root = self.cache / package_id.lower() / "3.8.4"
            root.mkdir(parents=True)
            archive = root / f"{package_id.lower()}.3.8.4.nupkg"
            repository = " ".join(f'{key}="{value}"' for key, value in record["repository"].items())
            with ZipFile(archive, "w") as package:
                package.writestr(package_id + ".nuspec", f'<package><metadata><id>{package_id}</id><version>3.8.4</version><repository {repository}/></metadata></package>')
                for path in required:
                    if path.split("/")[2] != package_id:
                        continue
                    relative = path.rsplit("/", 1)[1]
                    member = "staticwebassets/" + relative
                    content = relative.encode()
                    cached = root / member
                    cached.parent.mkdir(exist_ok=True)
                    cached.write_bytes(content)
                    package.writestr(member, content)
                    self.rows.append({"SourceId": package_id, "SourceType": "Package", "BasePath": "_content/" + package_id,
                                      "RelativePath": relative, "Identity": str(cached), "AssetRole": "Primary", "FileLength": len(content)})
            digest = baseline.package_consumer.base64_sha512(archive.read_bytes())
            archive.with_suffix(".nupkg.sha512").write_text(digest)
            (root / ".nupkg.metadata").write_text(json.dumps({"source": baseline.NUGET_ORG, "contentHash": digest}))
            record["archive_sha256"] = sha256(archive.read_bytes())
            self.policy["packages"].append(record)
        self.build.write_text(json.dumps({"Assets": self.rows}))

    def verify(self, **kwargs):
        return derive_baseline_resources(self.build, self.cache, "3.8.4", policy=self.policy, **kwargs)

    def test_binds_all_materialized_assets_and_default_smoke_requests(self):
        receipt = self.verify(route_prefix="/compat")
        self.assertEqual(len(receipt["assets"]), 6)
        self.assertEqual({row["path"] for row in receipt["assets"] if row["required"]},
                         {"/compat" + path for path in BASELINE_REQUESTED_ASSETS})
        self.assertEqual(len(receipt["packages"]), 2)
        self.assertTrue(all(row["repository"]["commit"] == baseline.RELEASE_COMMITS["elsa-studio"]["3.8.4"] for row in receipt["packages"]))

    def test_rejects_wrong_archive_policy_and_modified_cached_asset(self):
        original = copy.deepcopy(self.policy)
        self.policy["packages"][0]["archive_sha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "approved source digest"):
            self.verify()
        self.policy = original
        Path(self.rows[0]["Identity"]).write_bytes(b"altered")
        with self.assertRaisesRegex(ValueError, "sealed package member"):
            self.verify()

    def test_rejects_missing_materialized_assets_and_unknown_source_packages(self):
        for rows in (self.rows[:-1], [{**self.rows[0], "SourceId": "Elsa.Unknown"}, *self.rows[1:]]):
            with self.subTest(rows=rows):
                self.build.write_text(json.dumps({"Assets": rows}))
                with self.assertRaises(ValueError):
                    self.verify()


if __name__ == "__main__":
    unittest.main()
