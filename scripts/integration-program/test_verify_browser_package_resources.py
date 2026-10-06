import struct
import unittest

import verify_browser_package_resources as resources


def encoded(value):
    result = bytearray()
    while value >= 128:
        result.append((value & 127) | 128)
        value >>= 7
    return bytes(result + bytes([value]))


def section(kind, content):
    return bytes([kind]) + encoded(len(content)) + content


def binary_fixture():
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
    exports = b"\x03"
    for name, kind, index in (("webcilVersion", 3, 0), ("getWebcilSize", 0, 0), ("getWebcilPayload", 0, 1)):
        exports += encoded(len(name)) + name.encode() + bytes([kind, index])
    prefix = b"\0asm\x01\0\0\0" + section(6, b"\x01\x7f\x00\x41\x00\x0b") + section(7, exports)
    for padding in range(4):
        data = b"\x02\x01" + encoded(4 + padding) + struct.pack("<I", len(payload)) + bytes(padding) + b"\x01" + encoded(len(payload)) + payload
        wrapped = prefix + section(11, data)
        if (len(wrapped) - len(payload)) % 4 == 0:
            break
    converter = {"sdk_version": "10.0.401", "task_sha256": "a" * 64, "source_sha256": "b" * 64, "implementation_sha256": "c" * 64}
    policy = {converter["task_sha256"]: {"sdk_version": converter["sdk_version"], "source_sha256": converter["source_sha256"], "implementation_sha256": converter["implementation_sha256"], "wrapper_sha256": resources.sha256(prefix)}}
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

    def test_fetched_assets_are_distinct_from_materialized_and_fail_closed(self):
        asset = {"path": "/_content/Elsa.Studio.Workflows.Designer/designer.entry.js", "sha256": "a" * 64, "bytes": 123, "content_type": "text/javascript", "owner": "package"}
        fetched = {**asset, "status": 200}
        self.assertEqual(1, resources.verify_browser_resources([asset], [fetched])["requested"])
        self.assertEqual(1, resources.verify_browser_resources([asset], [], require_all=False)["materialized"])
        with self.assertRaises(ValueError):
            resources.verify_browser_resources([asset], [])
        prefixed = {**asset, "path": "/compat" + asset["path"]}
        self.assertEqual(1, resources.verify_browser_resources([prefixed], [{**prefixed, "status": 200}], route_prefix="/compat")["requested"])
        for path in (asset["path"] + "?token=private", "/_content/%2e%2e/private", "/_framework/../private"):
            with self.subTest(path=path), self.assertRaises(ValueError):
                resources.verify_browser_resources([{**asset, "path": path}], [], require_all=False)
        for key, value in (("sha256", "b" * 64), ("bytes", 124), ("status", 404), ("path", asset["path"] + "?token=private"), ("content_type", "text/html")):
            with self.subTest(key=key), self.assertRaises(ValueError):
                resources.verify_browser_resources([asset], [{**fetched, key: value}])


if __name__ == "__main__":
    unittest.main()
