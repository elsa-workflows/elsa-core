"""Identify npm components from the exact bundles and embedded resources in a packed NuGet archive."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import tempfile


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def inspected_resources(package_path):
    inspector = Path(__file__).resolve().parents[1] / "release-evidence/InspectResources.cs"
    return json.loads(subprocess.check_output(["dotnet", "run", "--file", str(inspector), "--", str(package_path)],
                                              cwd=inspector.parent))


def react_libraries(package_path, row, candidate, react_root):
    if row["id"] == "Elsa.ReactStudio.Host":
        resource_prefix, bundle = "ReactStudio/", "dist"
    elif row["project"].startswith("studio/react/dotnet/modules/"):
        resource_prefix = "ReactStudio.Module/"
        bundle = (Path(row["project"]).relative_to("studio/react").parent / "wwwroot").as_posix()
    else:
        return {}
    records = {}
    for line in (candidate / "react-vite-assets.jsonl").read_text().splitlines():
        item = json.loads(line)
        if item["file"].startswith(bundle + "/"):
            records[resource_prefix + item["file"][len(bundle) + 1:]] = item
    require(records, f"No emitted Vite evidence for {row['id']}")
    assemblies = inspected_resources(package_path)
    result = {}
    for path, resources in assemblies.items():
        shipped = []
        for name, digest in resources.items():
            require(name.startswith(resource_prefix), f"Unexpected CLR resource prefix: {name}")
            record = records.get(name)
            if not name.endswith((".js", ".css")):
                output_file = react_root / bundle / name[len(resource_prefix):]
                require(output_file.is_file() and sha(output_file.read_bytes()) == digest, f"Packed resource differs from build output: {name}")
                shipped.append(dict(name=name, sha256=digest, components=[]))
                continue
            if record is None and name == "ReactStudio.Module/module.css":
                # The product writes this literal after Vite when the module has no styles.
                require(digest == sha(b"/* No module-specific styles. */\n"), f"Unrecognised module CSS: {path}")
                shipped.append(dict(name=name, sha256=digest, components=[]))
                continue
            require(record and digest == record["sha256"], f"CLR resource differs from emitted Vite asset: {path}/{name}")
            shipped.append(dict(name=name, sha256=digest, components=record["components"], imports=record["imports"]))
        require(set(records) <= set(resources), f"Packed assembly is missing emitted Vite assets: {path}")
        result[path] = dict(method="Vite output modules matched to named CLR resource digests", resources=shipped)
    return result


def embedded_libraries(package_path, row, client):
    if row["id"] != "Elsa.Expressions.JavaScript.Libraries":
        return {}
    packages = json.loads((client / "package-lock.json").read_bytes())["packages"]
    assemblies = inspected_resources(package_path)
    resources = []
    for path in sorted((client / "dist").glob("*.js")):
        entry = (client / "src" / path.name).read_text().strip()
        match = re.fullmatch(r"""module\.exports\s*=\s*require\(['"]([^/'"]+)(?:/[^'"]+)?['"]\);?""", entry)
        require(match, f"Unknown embedded entrypoint: {path}")
        name = match.group(1)
        version = packages["node_modules/" + name]["version"]
        resource = path.read_bytes()
        require(resource and version.encode() in resource, f"Embedded {path} does not identify {version}")
        components = [[name, version]]
        # webpack's Node-module decorator is emitted inside these minified UMD bundles.
        if b".nmd=" in resource or b"__webpack_require__.nmd" in resource:
            components.append(["webpack", packages["node_modules/webpack"]["version"]])
        resources.append((path.name, components, resource))
    require(resources, "Missing checked-in browser library resources")
    result = {}
    for item in row["assemblies"]:
        name = item["assembly"]
        observed = assemblies[name]
        shipped = []
        for filename, components, data in resources:
            matches = [entry for entry, digest in observed.items() if entry.endswith("." + filename) and digest == sha(data)]
            require(len(matches) == 1, f"Packed CLR resource differs: {name}/{filename}")
            shipped.append(dict(name=matches[0], sha256=sha(data), components=components))
        require(len(shipped) == len(observed), f"Unidentified JavaScript CLR resources: {name}")
        result[name] = dict(method="CLR resource bytes matched to package-lock and entrypoint", resources=shipped)
    return result


def modules_from_paths(paths, locks, client):
    modules = set()
    for source in paths:
        source = source.replace("\\", "/").split("?", 1)[0]
        if "node_modules/" not in source:
            if source.startswith("webpack://") and ("/webpack/runtime/" in source or source.endswith("/webpack/bootstrap")):
                modules.add(("webpack", locks[client]["node_modules/webpack"]["version"]))
            continue
        prefix, remainder = source.split("node_modules/", 1)
        # A dependency imported by the shared BPMN module resolves against its own installation.
        owner = next((directory for directory in locks if directory != client and directory.parent.name + "/ClientLib/" in prefix), client)
        parts = ("node_modules/" + remainder).split("/")
        last = len(parts) - 1 - parts[::-1].index("node_modules")
        name = "/".join(parts[last + 1:last + (3 if parts[last + 1].startswith("@") else 2)])
        key = "/".join(parts[:last + 1]) + "/" + name
        require(key in locks[owner], f"Bundled module {key} is missing from the lock for {owner}")
        modules.add((name, locks[owner][key]["version"]))
    return sorted(modules)


def esbuild_components(archive, asset, client, locks):
    # Ask the installed, locked esbuild for its per-output metafile. Reject any replay
    # whose output differs from the actual shipped bytes, including linked legal comments.
    name = Path(asset).name
    css = name.endswith(".css")
    entry = "src/" + name if css else "src/index.ts"
    with tempfile.TemporaryDirectory() as tmp:
        output = Path(tmp) / name
        metadata = Path(tmp) / "metafile.json"
        args = [str(client / "node_modules/.bin/esbuild"), str(client / entry), "--bundle", "--minify",
                "--format=esm", "--target=es2020", "--legal-comments=linked", f"--outfile={output}", f"--metafile={metadata}"]
        if css and name == "bpmn-js-designer.css":
            args += ["--asset-names=fonts/[name]", *[f"--loader:{ext}=file" for ext in (".woff", ".woff2", ".ttf", ".eot", ".svg")]]
        subprocess.run(args, cwd=client, check=True, capture_output=True)
        require(archive.read(asset) == output.read_bytes(), f"esbuild replay does not match shipped {asset}")
        outputs = json.loads(metadata.read_bytes())["outputs"]
        item = next(item for path, item in outputs.items() if (client / path).resolve() == output.resolve())
        modules = modules_from_paths((p for p, item in item["inputs"].items() if item["bytesInOutput"] > 0), locks, client)
        return dict(method="esbuild byte-identical replay", components=modules)


def browser_components(archive, assets, client, clients):
    locks = {directory: json.loads((directory / "package-lock.json").read_bytes())["packages"] for directory in clients}
    result = {}
    for asset in assets:
        name = asset["package_path"]
        if name + ".map" in archive.namelist():
            mapping = json.loads(archive.read(name + ".map"))
            require(mapping["version"] == 3 and mapping["file"] == Path(name).name and mapping["sources"],
                    f"Source map does not identify {name}")
            result[name] = dict(method="packed source map " + name + ".map",
                                components=modules_from_paths(mapping["sources"], locks, client))
        elif (client / "build.mjs").is_file():
            result[name] = esbuild_components(archive, name, client, locks)
        else:
            require(name.endswith(".css") and any(p.read_bytes() == archive.read(name) for p in (client / "css").glob("*.css")),
                    f"No exact build evidence for {name}")
            result[name] = dict(method="byte-identical project CSS", components=[])
    return result
