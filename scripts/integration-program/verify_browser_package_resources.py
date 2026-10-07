"""Strict package PE → default WebCIL sections → browser response byte checks.

Format: https://github.com/dotnet/runtime/blob/v10.0.3/docs/design/mono/webcil.md
Debug fixups: src/tasks/Microsoft.NET.WebAssembly.Webcil/WebcilConverter.cs.
Selected converter evidence is supplied by the separately reviewed build verifier.
"""
from __future__ import annotations

from contextlib import ExitStack
import hashlib
import json
from pathlib import Path
import struct
import re
from urllib.parse import urlsplit
from zipfile import ZipFile

CONVERTER_POLICY = Path(__file__).resolve().parents[2] / "test/studio/browser/PackageCompatibility/converters.json"

RESOURCE_CONTENT_TYPES = json.loads(CONVERTER_POLICY.with_name("coverage-policy.json").read_text())["resource_content_types"]

def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def uint(data: bytes, offset: int, size: int = 4) -> int:
    require(0 <= offset <= len(data) - size, "Truncated binary field")
    return int.from_bytes(data[offset:offset + size], "little")


def leb(data: bytes, offset: int) -> tuple[int, int]:
    value = 0
    for index in range(5):
        require(offset < len(data), "Truncated WASM length")
        byte = data[offset]
        offset += 1
        require(index < 4 or byte < 16, "Overflow WASM length")
        value |= (byte & 127) << (index * 7)
        if byte < 128:
            require(index == 0 or byte != 0, "Noncanonical WASM length")
            return value, offset
    raise ValueError("Invalid WASM length")


def webcil_payload(wasm: bytes) -> bytes:
    require(wasm[:8] == b"\0asm\x01\0\0\0", "Unsupported WASM wrapper")
    offset, sections = 8, {}
    order = (1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 10, 11)
    last = -1
    while offset < len(wasm):
        kind = wasm[offset]
        length, start = leb(wasm, offset + 1)
        end = start + length
        require(end <= len(wasm), "Truncated WASM section")
        if kind:
            require(kind in order and kind not in sections and order.index(kind) > last, "Duplicate or unordered WASM section")
            last = order.index(kind)
            sections[kind] = (start, wasm[start:end])
        offset = end
    require(6 in sections and 7 in sections and 11 in sections, "Missing WebCIL wrapper section")
    # The reviewed 10.0.8 source adds only the immutable, zero-initialized webcilSize global.
    # Keep each exact global layout paired with its corresponding export index inventory.
    # https://github.com/dotnet/dotnet/blob/94ea82652cdd4e0f8046b5bd5becbd11461482ca/src/runtime/src/tasks/Microsoft.NET.WebAssembly.Webcil/WebcilWasmWrapper.cs
    legacy_globals = b"\x01\x7f\x00\x41\x00\x0b"
    wrapper_exports = {
        legacy_globals: {"webcilVersion": (3, 0), "getWebcilSize": (0, 0), "getWebcilPayload": (0, 1)},
        b"\x02" + legacy_globals[1:] * 2: {"webcilVersion": (3, 0), "webcilSize": (3, 1), "getWebcilSize": (0, 0), "getWebcilPayload": (0, 1)},
    }
    expected_exports = wrapper_exports.get(bytes(sections[6][1]))
    require(expected_exports is not None, "Unsupported WebCIL wrapper version")
    exports = sections[7][1]
    count, pos = leb(exports, 0)
    names = {}
    for _ in range(count):
        length, pos = leb(exports, pos)
        require(pos + length + 1 < len(exports), "Truncated wrapper export")
        name = exports[pos:pos + length].decode("ascii")
        pos += length
        kind = exports[pos]
        index, pos = leb(exports, pos + 1)
        require(name not in names, "Duplicate wrapper export")
        names[name] = (kind, index)
    require(pos == len(exports) and names == expected_exports, "Unexpected WebCIL wrapper exports")
    start, data = sections[11]
    count, pos = leb(data, 0)
    require(count == 2, "WebCIL must have exactly two data segments")
    segments = []
    positions = []
    for _ in range(count):
        flags, pos = leb(data, pos)
        require(flags == 1, "WebCIL data segment must be passive")
        length, pos = leb(data, pos)
        require(pos + length <= len(data), "Truncated WebCIL data segment")
        positions.append(start + pos)
        segments.append(data[pos:pos + length])
        pos += length
    require(pos == len(data) and 4 <= len(segments[0]) <= 7 and not any(segments[0][4:]), "Invalid WebCIL size/padding")
    require(uint(segments[0], 0) == len(segments[1]) and positions[1] % 4 == 0, "WebCIL payload size/alignment mismatch")
    return segments[1]


def pe_sections(pe: bytes) -> tuple[list[tuple[int, int, int, int]], tuple[int, int], tuple[int, int]]:
    require(pe[:2] == b"MZ", "Original package asset is not PE")
    header = uint(pe, 60)
    require(pe[header:header + 4] == b"PE\0\0", "Invalid PE signature")
    count, optional_size = uint(pe, header + 6, 2), uint(pe, header + 20, 2)
    optional = header + 24
    magic = uint(pe, optional, 2)
    require(magic in (0x10b, 0x20b) and count > 0, "Unsupported PE header")
    directory = optional + (96 if magic == 0x10b else 112)
    require(directory + 15 * 8 <= optional + optional_size, "Missing PE data directories")
    debug = (uint(pe, directory + 6 * 8), uint(pe, directory + 6 * 8 + 4))
    cli = (uint(pe, directory + 14 * 8), uint(pe, directory + 14 * 8 + 4))
    sections = []
    table = optional + optional_size
    for index in range(count):
        pos = table + index * 40
        section = tuple(uint(pe, pos + field) for field in (8, 12, 16, 20))
        require(section[3] + section[2] <= len(pe), "Truncated PE section")
        sections.append(section)
    require(len({s[1] for s in sections}) == count, "Duplicate PE section RVA")
    for index, section in enumerate(sections):
        for earlier in sections[:index]:
            require(section[3] >= earlier[3] + earlier[2] or earlier[3] >= section[3] + section[2], "Overlapping PE raw sections")
            require(section[1] >= earlier[1] + earlier[0] or earlier[1] >= section[1] + section[0], "Overlapping PE virtual sections")
    return sections, cli, debug


def verify_webcil(pe: bytes, wasm: bytes, converter: dict, *, _test_policy: dict | None = None) -> dict:
    # Production approvals come only from tracked policy, never runtime handles.
    reviewed_converters = json.loads(CONVERTER_POLICY.read_text())["converters"] if _test_policy is None else _test_policy
    binding = reviewed_converters.get(converter.get("task_sha256"), {})
    require(converter.get("task_sha256") in reviewed_converters and
            binding.get("source_sha256") == converter.get("source_sha256") and
            binding.get("implementation_sha256") == converter.get("implementation_sha256") and
            converter.get("sdk_version") == binding.get("sdk_version"), "Unreviewed selected WebCIL converter")
    offset, outside_data = 8, bytearray(wasm[:8])
    while offset < len(wasm):
        kind = wasm[offset]
        length, start = leb(wasm, offset + 1)
        end = start + length
        require(end <= len(wasm), "Truncated WASM section")
        if kind != 11:
            outside_data.extend(wasm[offset:end])
        offset = end
    require(sha256(outside_data) == binding.get("wrapper_sha256"), "Unreviewed or changed WebCIL wrapper implementation")
    payload = webcil_payload(wasm)
    sections, cli, debug = pe_sections(pe)
    require(payload[:4] == b"WbIL" and uint(payload, 4, 2) == uint(payload, 6, 2) == uint(payload, 10, 2) == 0, "Unsupported WebCIL header")
    require(uint(payload, 8, 2) == len(sections), "WebCIL section count mismatch")
    require(tuple(uint(payload, o) for o in (12, 16)) == cli and tuple(uint(payload, o) for o in (20, 24)) == debug, "WebCIL CLI/debug directory mismatch")
    position = 28 + 16 * len(sections)
    first = position
    adjustment = min(s[3] for s in sections) - first
    expected_sections = [bytearray(pe[s[3]:s[3] + s[2]]) for s in sections]
    generated_spans = []
    for section in sections:
        generated_spans.append((position, position + section[2]))
        position += section[2]
    position = first
    fixups = []
    if debug[0] and debug[1]:
        require(debug[1] % 28 == 0, "Invalid PE debug directory length")
        matches = [(i, debug[0] - s[1]) for i, s in enumerate(sections) if s[1] <= debug[0] < s[1] + s[0]]
        require(len(matches) == 1, "Unmapped PE debug directory")
        index, offset = matches[0]
        content = expected_sections[index]
        require(offset + debug[1] <= len(content), "PE debug directory escaped section")
        for entry in range(offset, offset + debug[1], 28):
            kind, size, pointer = (uint(content, entry + f) for f in (12, 16, 24))
            predicted = pointer - adjustment if kind != 16 and size and pointer else pointer
            require(predicted >= 0, "Invalid predicted debug pointer")
            if predicted != pointer:
                require(any(s[3] <= pointer and pointer + size <= s[3] + s[2] for s in sections), "PE debug pointer escaped sections")
                require(any(start <= predicted and predicted + size <= end for start, end in generated_spans), "Predicted debug pointer escaped WebCIL sections")
            struct.pack_into("<I", content, entry, 0)
            struct.pack_into("<I", content, entry + 24, predicted)
            fixups.append({"section": index, "offset": entry, "characteristics": 0, "pointer_to_raw_data": predicted})
    receipts = []
    for index, (section, expected) in enumerate(zip(sections, expected_sections)):
        actual = tuple(uint(payload, 28 + 16 * index + f) for f in (0, 4, 8, 12))
        require(actual == (*section[:3], position), "Missing, extra or altered WebCIL section")
        data = payload[position:position + section[2]]
        require(data == expected, "Unpredicted WebCIL section content change")
        receipts.append({"index": index, "original_sha256": sha256(pe[section[3]:section[3] + section[2]]), "generated_sha256": sha256(data)})
        position += section[2]
    require(position == len(payload), "Unexpected WebCIL trailing bytes")
    return {"original_pe_sha256": sha256(pe), "generated_wasm_sha256": sha256(wasm), "sections": receipts, "debug_fixups": fixups, "converter": {key: converter[key] for key in ("sdk_version", "task_sha256", "source_sha256", "implementation_sha256")}}


def _derive_package_resources(build_manifest: Path, cache: Path, version: str, by_id: dict,
                              package_member, *, route_prefix: str, hosted_layout=None) -> dict:
    """Shared build/cache/member checks; wrappers own the immutable archive authority."""
    import paired_package_provenance as provenance
    build_manifest = provenance.regular_file(build_manifest.absolute())
    build = json.loads(build_manifest.read_text())
    require(isinstance(build.get("Assets"), list), "Missing actual static asset inventory")
    inherited = None
    if hosted_layout is not None:
        from paired_package_hosted_assets import HostedClientAssets
        inherited = HostedClientAssets(hosted_layout, build_manifest, build, version)
    policy = json.loads(CONVERTER_POLICY.with_name("coverage-policy.json").read_text())
    require(not route_prefix or re.fullmatch(r"/[A-Za-z0-9_-]+(?:/[A-Za-z0-9_-]+)*", route_prefix), "Unsafe route prefix")
    mandatory = {route_prefix + path for path in policy["required_browser_assets"]}
    assets, paths, nonpackage = [], set(), []
    for asset in build["Assets"]:
        package_id = asset.get("SourceId", "")
        if not package_id.startswith("Elsa") or asset.get("AssetRole") != "Primary":
            continue
        if package_id == build.get("Source") and asset.get("SourceType") in ("Discovered", "Computed"):
            nonpackage.append({"source_id": package_id, "source_type": asset["SourceType"],
                               "relative_path_sha256": sha256(asset["RelativePath"].encode())})
            continue
        if inherited is not None and package_id == "Elsa.Studio.Host.Wasm" and asset.get("SourceType") == "Project":
            nonpackage.append(inherited.receipt(asset))
            continue
        require(asset.get("SourceType") == "Package" and package_id.casefold() in by_id, "Unowned Elsa source browser asset")
        package = by_id[package_id.casefold()]
        require(package_id == package["id"] and asset.get("BasePath") == "_content/" + package_id, "Browser source/base package identity differs")
        relative = asset.get("RelativePath", "")
        require(isinstance(relative, str) and re.fullmatch(r"[A-Za-z0-9_./-]+", relative) and ".." not in relative.split("/"), "Unsafe browser asset path")
        path = route_prefix + "/_content/" + package_id + "/" + relative
        require(path not in paths, "Duplicate materialized Elsa browser asset")
        paths.add(path)
        identity = provenance.regular_file(Path(asset["Identity"]).absolute())
        package_root = (cache / package_id.lower() / version).resolve()
        require(identity.is_relative_to(package_root), "Browser asset escaped isolated package cache")
        member = identity.relative_to(package_root).as_posix()
        require(member == "staticwebassets/" + relative, "Browser build member differs from original package layout")
        original = package_member(package, member, path in mandatory)
        body = identity.read_bytes()
        require(body == original and len(body) == asset.get("FileLength") and 0 < len(body) <= 32 * 1024 * 1024, "Materialized browser asset differs from sealed package member")
        extension = Path(relative).suffix
        require(extension in RESOURCE_CONTENT_TYPES, "Unsupported original Elsa browser resource type")
        assets.append({"path": path, "sha256": sha256(body), "bytes": len(body),
                       "content_type": RESOURCE_CONTENT_TYPES[extension],
                       "owner": "package", "required": path in mandatory})
    require(mandatory <= paths, "Missing mandatory materialized Designer/DomInterop assets")
    if inherited is not None:
        inherited.verify_unchanged()
    return {"assets": sorted(assets, key=lambda item: item["path"]),
            "nonpackage_build_assets": nonpackage,
            "static_asset_manifest_sha256": sha256(build_manifest.read_bytes())}


def derive_candidate_resources(build_manifest: Path, verified_root: Path, cache: Path, *,
                               verified_manifest_sha256: str, route_prefix: str = "", hosted_layout=None) -> dict:
    """Bind actual build assets to the sealed original packages, never a caller approval map."""
    import consolidated_candidate_input as candidate
    import paired_package_provenance as provenance
    import prove_consolidated_package_consumers as packages
    build_manifest = provenance.regular_file(build_manifest.absolute())
    manifest_path = provenance.regular_file(verified_root / "verified-artifacts.json")
    require(sha256(manifest_path.read_bytes()) == verified_manifest_sha256, "Verified manifest changed after original input verification")
    manifest = json.loads(manifest_path.read_text())
    version, source, by_id, _, _ = packages._validated_manifest(manifest)
    require(version == candidate.PRODUCER["version"] and source == candidate.SOURCE, "Original candidate resource producer/version differs")
    with ExitStack() as stack:
        archives = {}
        def package_member(package, member, mandatory):
            package_id = package["id"]
            if package_id not in archives:
                archive = provenance.regular_file(verified_root / "artifacts" / package["nupkg"])
                require(sha256(archive.read_bytes()) == package["nupkg_sha256"], "Original browser package archive changed")
                archives[package_id] = stack.enter_context(ZipFile(archive))
            original = archives[package_id].read(member)
            if mandatory:
                pins = [item for item in package["browser_assets"] if item["package_path"] == member]
                require(len(pins) == 1 and pins[0]["sha256"] == sha256(original), "Required browser asset lacks original sealed member pin")
            return original
        receipt = _derive_package_resources(build_manifest, cache, version, by_id, package_member,
                                            route_prefix=route_prefix, hosted_layout=hosted_layout)
    return {**receipt, "verified_artifacts_sha256": verified_manifest_sha256,
            "candidate_producer": dict(candidate.PRODUCER)}


def verify_browser_resources(expected: list[dict], fetched: list[dict], *, require_all: bool = True, route_prefix: str = "") -> dict:
    require(not route_prefix or re.fullmatch(r"/[A-Za-z0-9_-]+(?:/[A-Za-z0-9_-]+)*", route_prefix), "Unsafe route prefix")
    def safe_path(path):
        parsed = urlsplit(path)
        require(not parsed.scheme and not parsed.netloc and not parsed.query and not parsed.fragment and
                path.startswith((route_prefix + "/_content/", route_prefix + "/_framework/")) and
                ".." not in path.split("/") and "%" not in path and "\\" not in path, "Unsafe browser resource path")
    for asset in expected:
        safe_path(asset["path"])
        require(asset.get("owner") in ("package", "fixture", "platform") and
                type(asset.get("bytes")) is int and type(asset.get("required", True)) is bool and 0 < asset["bytes"] <= 32 * 1024 * 1024 and
                isinstance(asset.get("sha256"), str) and re.fullmatch("[0-9a-f]{64}", asset["sha256"]) and
                asset.get("content_type") in set(RESOURCE_CONTENT_TYPES.values()) | {"application/javascript"}, "Invalid materialized resource manifest")
    materialized = {record["path"]: record for record in expected}
    require(len(materialized) == len(expected) and bool(materialized), "Missing or duplicate materialized browser assets")
    requested = set()
    for response in fetched:
        path = response["path"]
        safe_path(path)
        require(path in materialized, "Browser requested unpaired package resource")
        asset = materialized[path]
        require(response["status"] == 200 and response["sha256"] == asset["sha256"] and
                response["bytes"] == asset["bytes"] and response["content_type"] == asset["content_type"] and
                response.get("owner") == asset["owner"] and response.get("requested") is True, "Browser response differs from verified materialized resource")
        require(asset["owner"] in ("package", "fixture", "platform"), "Unknown browser resource owner")
        requested.add(path)
    required = {path for path, asset in materialized.items() if asset.get("required", True)}
    require(required <= requested, "Missing mandatory browser-requested assets")
    require(not require_all or requested == set(materialized), "Missing browser-requested assets")
    return {"materialized": len(materialized), "requested": len(requested), "not_requested": sorted(set(materialized) - requested)}
