"""Generate public SBOMs from the pack verifier's exact artifacts and retained restore inputs."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import zipfile

from shipped_javascript import embedded_libraries, browser_components, react_libraries
from prove_consolidated_packages import archive_names, dependency_groups, framework_reference_groups, metadata, require, write_json


def digest(data):
    return hashlib.sha256(data).hexdigest()


def browser_bom(client, source, inputs, output):
    # npm ci used the reviewed lock copied by build_studio_clientlibs.sh. Verify the actual bytes.
    lock = client / "package-lock.json"
    lock_hash = digest(lock.read_bytes())
    matches = [name for name, sha in inputs.items() if name.endswith(".package-lock.json") and sha == lock_hash]
    require(matches, f"Missing reviewed browser lock for {client}")
    project = client / "package.json"
    require(inputs.get(project.relative_to(source).as_posix()) == digest(project.read_bytes()),
            f"Browser package manifest changed: {client}")
    with tempfile.TemporaryDirectory() as staging:
        shutil.copyfile(lock, Path(staging) / lock.name)
        shutil.copyfile(project, Path(staging) / project.name)
        bom = json.loads(subprocess.check_output(
            ["npm", "sbom", "--sbom-format=cyclonedx", "--package-lock-only", "--offline", "--ignore-scripts"],
            cwd=staging))
    bom["metadata"].setdefault("properties", []).append({
        "name": "elsa:scope", "value": "Browser build inputs (includes development tools and tree-shaken modules)"})
    name = f"browser/{client.parent.name}.cdx.json"
    write_json(output / name, bom)
    return {"path": name, "sha256": digest((output / name).read_bytes()), "lock_sha256": lock_hash}


def generate(candidate, source, tooling_commit, run_id, attempt):
    manifest = json.loads((candidate / "verified-artifacts.json").read_bytes())
    require(re.fullmatch(r"[0-9a-f]{40}", tooling_commit) and ((run_id > 0 and attempt > 0) or (run_id == attempt == 0)), "Missing producer identity")
    output = candidate / "release-evidence"
    output.mkdir()
    (output / "browser").mkdir()
    browser_inputs = json.loads((candidate / "studio-clientlibs.json").read_bytes())["inputs"]
    browsers = {}
    rows, packages = [], []
    shipped_components = {}
    for row in manifest["packages"]:
        package = candidate / "artifacts" / row["nupkg"]
        client = (source / row["project"]).parent / "ClientLib"
        with zipfile.ZipFile(package) as archive:
            names = sorted(archive_names(archive))
            payload = {name: digest(archive.read(name)) for name in names if not name.endswith("/")}
            nuspec = metadata(archive)
            declared = dependency_groups(nuspec)
            shared_frameworks = framework_reference_groups(nuspec)
            shipped = embedded_libraries(package, row, client)
            shipped.update(react_libraries(package, row, candidate, source / "studio/react"))
        require(declared == row["dependency_groups"], f"Packed dependency groups changed: {row['id']}")
        require(digest((candidate / "artifacts" / row["nupkg"]).read_bytes()) == row["nupkg_sha256"],
                f"Pack content changed: {row['id']}")
        evidence = {"id": row["id"], "nupkg": row["nupkg"], "candidate_sha256": row["nupkg_sha256"],
                    "symbols": None, "browser_build_inputs": []}
        if row["snupkg"]:
            symbols_hash = digest((candidate / "artifacts" / row["snupkg"]).read_bytes())
            require(symbols_hash == row["snupkg_sha256"], f"Symbols changed: {row['id']}")
            evidence["symbols"] = {"name": row["snupkg"], "sha256": symbols_hash}
        if row.get("browser_assets"):
            require((client / "package.json").is_file(), f"Missing browser package manifest: {client}")
        if row["id"] == "Elsa.Studio.Bpmn":
            # The framework ships this original ES module. Its ClientLib is consumed by the designer bundles.
            file = "staticwebassets/bpmn-canvas-host.js"
            require(payload.get(file) == digest((client.parent / "wwwroot/bpmn-canvas-host.js").read_bytes()),
                    "Packed BPMN host differs from project JavaScript")
            shipped[file] = dict(method="byte-identical project JavaScript", components=[])
        elif (client / "package.json").is_file() and any(name.startswith("staticwebassets/") for name in names):
            require(row["browser_assets"], f"No verified bundle coverage for {row['id']}")
            clients = [client]
            if row["id"] in {"Elsa.Studio.Bpmn.BpmnJs", "Elsa.Studio.Bpmn.X6"}:
                clients.append(client.parents[1] / "Elsa.Studio.Bpmn" / "ClientLib")
            with zipfile.ZipFile(package) as archive:
                shipped.update(browser_components(archive, row["browser_assets"], client, clients))
            for directory in clients:
                if directory not in browsers:
                    browsers[directory] = browser_bom(directory, source, browser_inputs, output)
                evidence["browser_build_inputs"].append(browsers[directory])
        if shipped:
            shipped_components[row["id"]] = shipped
        packages.append(evidence)
        rows.append({**{key: row[key] for key in ("id", "frameworks", "restore_assets", "nupkg_sha256")},
                     "dependency_groups": declared, "shared_frameworks": shared_frameworks, "payload": payload, "shipped": shipped})
    write_json(candidate / "shipped-javascript.json", shipped_components)
    with tempfile.TemporaryDirectory() as scratch:
        input_file = Path(scratch) / "input.json"
        write_json(input_file, {"version": manifest["version"], "packages": rows, "browser": list(browsers.values())})
        script = Path(__file__).resolve().parents[1] / "release-evidence/package-boms.mjs"
        subprocess.run(["npm", "ci", "--omit=dev", "--ignore-scripts", "--no-audit", "--no-fund"], cwd=script.parent, check=True)
        subprocess.run(["node", str(script), str(candidate), str(input_file)], check=True)
    frameworks = json.loads((output / "frameworks.json").read_bytes())
    (output / "frameworks.json").unlink()
    for package in packages:
        package["frameworks"] = frameworks[package["id"]]
    write_json(output / "candidate-index.json", {
        "schema": 1, "version": manifest["version"], "source_commit": manifest["source_commit"],
        "tooling_commit": tooling_commit, "producer_run": run_id, "producer_attempt": attempt, "packages": packages})
