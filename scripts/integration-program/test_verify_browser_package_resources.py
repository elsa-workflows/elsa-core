import copy
import json
from pathlib import Path
import struct
import tempfile
from zipfile import ZipFile

import consolidated_candidate_input as candidate
import prove_consolidated_package_consumers as packages
import unittest

import verify_browser_package_resources as resources

# Exact prefix/suffix from the official 10.0.8 wrapper source pinned in the verifier.
SDK10_PREFIX = bytes.fromhex(
    "0061736d01000000010a0260017f0060027f7f000212010677656263696c066d656d6f7279020001"
    "0303020001060b027f0041000b7f0041000b0741040d77656263696c56657273696f6e03000a7765"
    "6263696c53697a6503010d67657457656263696c53697a6500001067657457656263696c5061796c6f"
    "616400010c01020a1b020c00200041004104fc0800000b0c00200041002001fc0801000b")
SDK10_SUFFIX = bytes.fromhex("001b046e616d650214020001000764657374507472010200016401016e")


def encoded(value):
    result = bytearray()
    while value >= 128:
        result.append((value & 127) | 128)
        value >>= 7
    return bytes(result + bytes([value]))


def section(kind, content):
    return bytes([kind]) + encoded(len(content)) + content


def binary_fixture(*, size_global=False, official_wrapper=False):
    pe = bytearray(608)
    pe[:2] = b"MZ"
    struct.pack_into("<I", pe, 60, 128)
    pe[128:132] = b"PE\0\0"
    struct.pack_into("<H", pe, 134, 1)
    struct.pack_into("<H", pe, 148, 224)
    struct.pack_into("<H", pe, 152, 0x10b)
    struct.pack_into("<II", pe, 248 + 48, 0x1010, 28)
    struct.pack_into("<II", pe, 248 + 112, 0x1000, 16)
    struct.pack_into("<IIII", pe, 376 + 8, 96, 0x1000, 96, 512)
    pe[512:] = bytes(range(96))
    struct.pack_into("<IIHHIIII", pe, 528, 123, 42, 1, 2, 2, 8, 0x1046, 582)
    expected = bytearray(pe[512:])
    struct.pack_into("<I", expected, 16, 0)
    struct.pack_into("<I", expected, 40, 114)
    payload = b"WbIL" + struct.pack("<HHHHIIII", 0, 0, 1, 0, 0x1000, 16, 0x1010, 28)
    payload += struct.pack("<IIII", 96, 0x1000, 96, 44) + expected
    exported = [("webcilVersion", 3, 0), ("getWebcilSize", 0, 0), ("getWebcilPayload", 0, 1)]
    globals = b"\x01\x7f\x00\x41\x00\x0b"
    if size_global:
        globals = b"\x02" + globals[1:] * 2
        exported.insert(1, ("webcilSize", 3, 1))
    exports = encoded(len(exported))
    for name, kind, index in exported:
        exports += encoded(len(name)) + name.encode() + bytes([kind, index])
    prefix = b"\0asm\x01\0\0\0" + section(6, globals) + section(7, exports)
    suffix = b""
    if official_wrapper:
        prefix, suffix = SDK10_PREFIX, SDK10_SUFFIX
    for padding in range(4):
        data = b"\x02\x01" + encoded(4 + padding) + struct.pack("<I", len(payload)) + bytes(padding) + b"\x01" + encoded(len(payload)) + payload
        wrapped = prefix + section(11, data) + suffix
        if (len(wrapped) - len(payload) - len(suffix)) % 4 == 0:
            break
    converter = {"sdk_version": "10.0.401", "task_sha256": "a" * 64, "source_sha256": "b" * 64, "implementation_sha256": "c" * 64}
    policy = {converter["task_sha256"]: {"sdk_version": converter["sdk_version"], "source_sha256": converter["source_sha256"], "implementation_sha256": converter["implementation_sha256"], "wrapper_sha256": resources.sha256(prefix + suffix)}}
    return bytes(pe), wrapped, converter, policy


class ResourceContracts(unittest.TestCase):
    def test_exact_sections_and_predicted_debug_fields_only(self):
        pe, wasm, converter, policy = binary_fixture()
        receipt = resources.verify_webcil(pe, wasm, converter, _test_policy=policy)
        self.assertEqual(1, len(receipt["sections"]))
        self.assertEqual(114, receipt["debug_fixups"][0]["pointer_to_raw_data"])
        payload_start = len(wasm) - len(resources.webcil_payload(wasm))
        for offset in (8, 28, 32, 36, 40, 44, 60, 84, 139):
            with self.subTest(offset=offset):
                changed = bytearray(wasm)
                changed[payload_start + offset] ^= 1
                with self.assertRaises(ValueError):
                    resources.verify_webcil(pe, changed, converter, _test_policy=policy)
        with self.assertRaisesRegex(ValueError, "Unreviewed"):
            resources.verify_webcil(pe, wasm, converter)
        with self.assertRaises(ValueError):
            resources.verify_webcil(pe, wasm, {**converter, "implementation_sha256": "d" * 64}, _test_policy=policy)
        with self.assertRaises(ValueError):
            resources.verify_webcil(pe, wasm, {**converter, "sdk_version": "8.0.100"}, _test_policy=policy)
        with self.assertRaises(ValueError):
            resources.webcil_payload(wasm + section(11, b"\x00"))

    def test_size_global_variant_keeps_exact_globals_exports_and_payload_guards(self):
        pe, wasm, converter, policy = binary_fixture(size_global=True)
        receipt = resources.verify_webcil(pe, wasm, converter, _test_policy=policy)
        self.assertEqual(1, len(receipt["sections"]))
        self.assertEqual(114, receipt["debug_fixups"][0]["pointer_to_raw_data"])
        with self.assertRaisesRegex(ValueError, "Unreviewed"):
            resources.verify_webcil(pe, wasm, converter)
        # The new global remains immutable i32 zero, and webcilSize must export global index1.
        global_start = wasm.index(b"\x02\x7f\x00\x41\x00\x0b")
        export_index = wasm.index(b"webcilSize") + len(b"webcilSize") + 1
        payload_start = len(wasm) - len(resources.webcil_payload(wasm))
        for offset in (global_start, global_start + 6, global_start + 7, global_start + 9, export_index,
                       payload_start + 28, payload_start + 44, payload_start + 139):
            changed = bytearray(wasm)
            changed[offset] ^= 1
            with self.subTest(offset=offset), self.assertRaises(ValueError):
                # Parser negatives cannot rely only on the independent wrapper hash check.
                resources.webcil_payload(changed) if offset < payload_start else resources.verify_webcil(pe, changed, converter, _test_policy=policy)
        legacy_globals = b"\x01\x7f\x00\x41\x00\x0b"
        new_globals = b"\x02" + legacy_globals[1:] * 2
        with self.assertRaisesRegex(ValueError, "exports"):
            resources.webcil_payload(wasm.replace(section(6, new_globals), section(6, legacy_globals), 1))
        _, legacy_wasm, _, _ = binary_fixture()
        with self.assertRaisesRegex(ValueError, "exports"):
            resources.webcil_payload(legacy_wasm.replace(section(6, legacy_globals), section(6, new_globals), 1))
        for malformed in (wasm + section(11, b"\x00"), wasm + section(6, new_globals)):
            with self.subTest(malformed=malformed[-8:]), self.assertRaises(ValueError):
                resources.webcil_payload(malformed)

    def test_official_wrapper_prefix_suffix_and_source_hash(self):
        self.assertEqual("186b1bbdf5c3b1c0dbee30c4359348f1ad3e347264c33cf1d3718f312dcaee54",
                         resources.sha256(SDK10_PREFIX + SDK10_SUFFIX))
        pe, wasm, converter, policy = binary_fixture(official_wrapper=True)
        self.assertEqual(1, len(resources.verify_webcil(pe, wasm, converter, _test_policy=policy)["sections"]))
        with self.assertRaisesRegex(ValueError, "Unreviewed"):
            resources.verify_webcil(pe, wasm, converter)
        with self.assertRaisesRegex(ValueError, "wrapper"):
            resources.verify_webcil(pe, wasm[:-1] + bytes([wasm[-1] ^ 1]), converter, _test_policy=policy)

    def test_fetched_assets_are_distinct_from_materialized_and_fail_closed(self):
        asset = {"path": "/_content/Elsa.Studio.Workflows.Designer/designer.entry.js", "sha256": "a" * 64, "bytes": 123, "content_type": "text/javascript", "owner": "package"}
        fetched = {**asset, "status": 200, "requested": True}
        self.assertEqual(1, resources.verify_browser_resources([asset], [fetched])["requested"])
        self.assertEqual(1, resources.verify_browser_resources([{**asset, "required": False}], [], require_all=False)["materialized"])
        with self.assertRaises(ValueError):
            resources.verify_browser_resources([asset], [])
        prefixed = {**asset, "path": "/compat" + asset["path"]}
        self.assertEqual(1, resources.verify_browser_resources([prefixed], [{**prefixed, "status": 200, "requested": True}], route_prefix="/compat")["requested"])
        for path in (asset["path"] + "?token=private", "/_content/%2e%2e/private", "/_framework/../private"):
            with self.subTest(path=path), self.assertRaises(ValueError):
                resources.verify_browser_resources([{**asset, "path": path}], [], require_all=False)
        for key, value in (("owner", "fixture"), ("requested", False), ("requested", None), ("sha256", "b" * 64), ("bytes", 124), ("status", 404), ("path", asset["path"] + "?token=private"), ("content_type", "text/html")):
            with self.subTest(key=key), self.assertRaises(ValueError):
                resources.verify_browser_resources([asset], [{**fetched, key: value}])

    def test_actual_build_resource_identity_cache_and_sealed_archive_are_required(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            cache, artifacts = root / "cache", root / "artifacts"
            artifacts.mkdir()
            required = json.loads(resources.CONVERTER_POLICY.with_name("coverage-policy.json").read_text())["required_browser_assets"]
            ids = set(packages.REQUIRED_PACKAGES) | {path.split("/")[2] for path in required}
            manifest = {"version": "3.10.0", "source_commit": candidate.SOURCE, "packages": [], "external_package_exceptions": {name: "reviewed" for name in packages.AUDITED_EXTERNAL_ELSA_IDS}}
            rows = []
            for package_id in sorted(ids):
                archive = artifacts / (package_id + ".3.10.0.nupkg")
                pins = []
                with ZipFile(archive, "w") as zipped:
                    for path in required:
                        if path.split("/")[2] != package_id:
                            continue
                        relative = path.rsplit("/", 1)[1]
                        member, content = "staticwebassets/" + relative, relative.encode()
                        cached = cache / package_id.lower() / "3.10.0" / member
                        cached.parent.mkdir(parents=True, exist_ok=True)
                        cached.write_bytes(content)
                        zipped.writestr(member, content)
                        pins.append({"package_path": member, "sha256": resources.sha256(content)})
                        rows.append({"SourceId": package_id, "SourceType": "Package", "BasePath": "_content/" + package_id, "RelativePath": relative, "Identity": str(cached), "AssetRole": "Primary", "FileLength": len(content)})
                manifest["packages"].append({"id": package_id, "frameworks": list(packages.FRAMEWORKS), "nupkg": archive.name, "nupkg_sha256": resources.sha256(archive.read_bytes()), "browser_assets": pins})
            manifest_path = root / "verified-artifacts.json"
            manifest_path.write_text(json.dumps(manifest))
            build = root / "build.json"
            def check(data):
                build.write_text(json.dumps({"Assets": data}))
                return resources.derive_candidate_resources(build, root, cache, verified_manifest_sha256=resources.sha256(manifest_path.read_bytes()))
            receipt = check(rows)
            with self.assertRaisesRegex(ValueError, "manifest changed"):
                resources.derive_candidate_resources(build, root, cache, verified_manifest_sha256="0" * 64)
            self.assertEqual(6, len(receipt["assets"]))
            self.assertTrue(all(row["required"] for row in receipt["assets"]))
            for mutate in (lambda data: data.pop(), lambda data: data.append(copy.deepcopy(data[0])),
                           lambda data: data[0].update(SourceId="Elsa.Unknown"), lambda data: data[0].update(SourceType="Project"),
                           lambda data: data[0].update(BasePath="_content/Elsa.Other"), lambda data: data[0].update(FileLength=0)):
                changed = copy.deepcopy(rows)
                mutate(changed)
                with self.subTest(mutate=mutate), self.assertRaises(ValueError):
                    check(changed)
            original = manifest_path.read_text()
            for key, value in (("version", "3.9.0"), ("source_commit", "b" * 40)):
                altered = json.loads(original)
                altered[key] = value
                manifest_path.write_text(json.dumps(altered))
                with self.subTest(key=key), self.assertRaises(ValueError):
                    check(rows)
            manifest_path.write_text(original)
            Path(rows[0]["Identity"]).write_bytes(b"changed cache body")
            with self.assertRaisesRegex(ValueError, "sealed package member"):
                check(rows)
            target = next(package for package in manifest["packages"] if package["id"] == rows[0]["SourceId"])
            with (artifacts / target["nupkg"]).open("ab") as archive:
                archive.write(b"changed original archive")
            with self.assertRaisesRegex(ValueError, "archive changed"):
                check(rows)


if __name__ == "__main__":
    unittest.main()
