"""Narrow authority for the owned Hosted wrapper's referenced fixture client.

The SDK's ComputeReferenceStaticWebAssetItems changes Discovered/Computed to
Project and makes OriginalItemSpec absolute; it preserves the other metadata.
These are fixture receipts, never a substitute for package or WebCIL evidence.
https://github.com/dotnet/sdk/blob/main/src/StaticWebAssetsSdk/Tasks/ComputeReferenceStaticWebAssetItems.cs
"""
import base64
import hashlib
import json
from pathlib import Path
import re

import paired_package_provenance as provenance


CLIENT_SOURCE = "Elsa.Studio.Host.Wasm"
WRAPPER_SOURCE = "Elsa.Studio.Host.HostedWasm"
MATCH_FIELDS = ("RelativePath", "BasePath", "AssetKind", "AssetMode", "AssetRole",
                "AssetTraitName", "AssetTraitValue", "RelatedAsset", "Fingerprint",
                "Integrity", "FileLength", "CopyToOutputDirectory", "CopyToPublishDirectory")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def _path(value, root, *, file=False):
    require(isinstance(value, str) and value and "\0" not in value and "\\" not in value
            and ".." not in Path(value).parts, "Unsafe inherited fixture path")
    path = Path(value)
    path = path if path.is_absolute() else root / path
    return provenance.regular_file(path) if file else path.resolve()


class HostedClientAssets:
    def __init__(self, layout, wrapper_manifest, wrapper, version):
        import materialize_paired_package_hosts as hosts
        require(layout.request.host == "hosted-wasm" and layout.request.version == version,
                "Inherited fixture authority requires aligned Hosted layout")
        group = layout.group_root.resolve()
        self.client = group / "projects/wasm"
        wrapper_root = group / "projects/hosted-wasm"
        self.framework = layout.request.framework
        self.seen = set()
        self.project_hashes = {}
        self.discovered_hashes = {}
        for host, root, source in (("wasm", self.client, CLIENT_SOURCE),
                                   ("hosted-wasm", wrapper_root, WRAPPER_SOURCE)):
            project = root / (source + ".csproj")
            require(layout.project_paths.get(host) == project,
                    "Inherited fixture project differs from fixed source edge")
            actual = provenance.regular_file(project).read_bytes()
            name, expected = hosts._project(host, layout.request)
            require(name == project.name and actual == expected.encode(),
                    "Inherited fixture project differs from approved materialization")
            prefix = root.relative_to(group).as_posix() + "/"
            pins = {name: value for name, value in layout.input_hashes.items() if name.startswith(prefix)}
            sources = hosts._host_source(host, version)
            expected_names = (set(sources) - {"source-project.xml"}) | {
                name, "NuGet.Config", "Directory.Build.props", "Directory.Build.targets",
                "Directory.Packages.props", "global.json"}
            if host == "wasm":
                expected_names.add("wwwroot/appsettings.json")
                self.discovered_hashes = {root / name: digest(path.read_bytes()) for name, path in sources.items()
                                          if name.startswith("wwwroot/")}
                self.discovered_hashes[root / "wwwroot/appsettings.json"] = digest(b'{}\n')
            require(set(pins) == {prefix + name for name in expected_names},
                    "Inherited fixture input pin inventory differs")
            require(pins.get(project.relative_to(group).as_posix()) == digest(actual),
                    "Inherited fixture project lacks approved input pin")
            for name, value in pins.items():
                path = provenance.regular_file(group / name)
                require(path.is_relative_to(root) and digest(path.read_bytes()) == value,
                        "Inherited fixture materialized input changed")
            self.project_hashes[host] = digest(actual)
        expected_wrapper = wrapper_root / "obj/Release" / self.framework / "staticwebassets.build.json"
        wrapper_bytes = wrapper_manifest.read_bytes()
        require(wrapper_manifest == provenance.regular_file(expected_wrapper)
                and json.loads(wrapper_bytes) == wrapper
                and wrapper.get("Source") == WRAPPER_SOURCE
                and type(wrapper.get("Version")) is int and wrapper["Version"] == 1
                and wrapper.get("ManifestType") == "Build",
                "Inherited fixture wrapper manifest differs")
        client_manifest = provenance.regular_file(self.client / "obj/Release" / self.framework / "staticwebassets.build.json")
        client_bytes = client_manifest.read_bytes()
        client = json.loads(client_bytes)
        require(type(client.get("Version")) is int and client["Version"] == 1 and client.get("Source") == CLIENT_SOURCE
                and client.get("Mode") == "Root" and client.get("ManifestType") == "Build"
                and client.get("BasePath") == "/" and isinstance(client.get("Assets"), list),
                "Inherited fixture client manifest differs")
        stamp_path = provenance.regular_file(self.client / "build-reuse.json")
        stamp_bytes = stamp_path.read_bytes()
        stamp = json.loads(stamp_bytes)
        prefix = self.client.relative_to(group).as_posix() + "/"
        pins = {name: value for name, value in layout.input_hashes.items() if name.startswith(prefix)}
        restored = provenance.regular_file(self.client / "obj/project.assets.json")
        restored_sha256 = digest(restored.read_bytes())
        require(type(stamp.get("schema")) is int and stamp["schema"] == 1 and stamp.get("inputs") == pins
                and stamp.get("assets") == restored_sha256
                and isinstance(stamp.get("outputs"), dict) and stamp["outputs"],
                "Inherited fixture lacks completed client build")
        self.outputs = stamp["outputs"]
        generated = hosts._generated_static_assets(layout, "wasm", layout.project_paths["wasm"])
        require(stamp.get("generated_static_assets") == generated,
                "Inherited generated fixture asset differs from completed build")
        self.generated = generated
        self.pins = pins
        self.group = group
        self.rows = {}
        for row in client["Assets"]:
            if row.get("SourceId") == CLIENT_SOURCE and row.get("AssetRole") == "Primary":
                require(row.get("SourceType") in ("Discovered", "Computed"),
                        "Unexpected owned client source kind")
                identity = _path(row.get("Identity"), self.client, file=True)
                require(identity not in self.rows, "Duplicate owned client asset identity")
                self.rows[identity] = row
        self.binding = {
            "client_project_sha256": self.project_hashes["wasm"],
            "wrapper_project_sha256": self.project_hashes["hosted-wasm"],
            "client_static_manifest_sha256": digest(client_bytes),
            "wrapper_static_manifest_sha256": digest(wrapper_bytes),
            "client_build_stamp_sha256": digest(stamp_bytes),
        }
        self.manifest_pins = {client_manifest: self.binding["client_static_manifest_sha256"],
                              wrapper_manifest: self.binding["wrapper_static_manifest_sha256"],
                              stamp_path: self.binding["client_build_stamp_sha256"], restored: restored_sha256}

    def verify_unchanged(self):
        require(all(digest(provenance.regular_file(path).read_bytes()) == value
                    for path, value in self.manifest_pins.items()), "Inherited fixture manifests changed during derivation")

    def receipt(self, asset):
        require(asset.get("SourceId") == CLIENT_SOURCE and asset.get("SourceType") == "Project"
                and asset.get("AssetRole") == "Primary", "Unowned inherited fixture source")
        identity = _path(asset.get("Identity"), self.client, file=True)
        require(identity in self.rows and identity not in self.seen, "Missing or duplicate inherited client asset")
        own = self.rows[identity]
        require(all(key in own and key in asset and own[key] == asset[key] for key in MATCH_FIELDS)
                and type(asset["FileLength"]) is int,
                "Inherited client asset metadata differs")
        content_root = _path(own.get("ContentRoot"), self.client)
        require(content_root == _path(asset.get("ContentRoot"), self.client)
                and _path(own.get("OriginalItemSpec"), self.client, file=True) == _path(asset.get("OriginalItemSpec"), self.client, file=True),
                "Inherited client asset paths differ")
        relative = own["RelativePath"]
        require(isinstance(relative, str) and 0 < len(relative) <= 4096
                and re.fullmatch(r"[A-Za-z0-9_./#{}\[\]!?-]+", relative)
                and not relative.startswith("/") and ".." not in relative.split("/"),
                "Unsafe inherited client relative path")
        identity = provenance.regular_file(identity)
        require(identity.is_relative_to(content_root), "Inherited client identity escaped content root")
        body = identity.read_bytes()
        require(type(own["FileLength"]) is int and 0 < len(body) <= 32 * 1024 * 1024
                and len(body) == own["FileLength"]
                and own["Integrity"] == base64.b64encode(hashlib.sha256(body).digest()).decode(),
                "Inherited client asset bytes differ")
        if own["SourceType"] == "Discovered":
            require(content_root == self.client / "wwwroot" and identity.is_relative_to(content_root)
                    and self.discovered_hashes.get(identity) == digest(body)
                    and self.pins.get(identity.relative_to(self.group).as_posix()) == digest(body),
                    "Inherited client source lacks approved materialization pin")
        else:
            output = self.client / "bin/Release" / self.framework
            computed = self.client / "obj/Release" / self.framework
            bundle_root = computed / "scopedcss/bundle"
            require((content_root == output / "wwwroot" and identity.is_relative_to(content_root) and
                     self.outputs.get(identity.relative_to(output).as_posix()) == digest(body))
                    or (content_root == bundle_root and identity == bundle_root / (CLIENT_SOURCE + ".styles.css")
                        and self.generated.get(identity.relative_to(self.client).as_posix()) == digest(body)),
                    "Inherited computed asset escaped completed client build")
        self.seen.add(identity)
        return {"source_id": CLIENT_SOURCE, "source_type": "Project", "origin_source_type": own["SourceType"],
                "relative_path_sha256": digest(relative.encode()),
                "identity_sha256": digest(identity.relative_to(self.client).as_posix().encode()),
                "content_sha256": digest(body), "bytes": len(body), **self.binding}
