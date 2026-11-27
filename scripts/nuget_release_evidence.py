#!/usr/bin/env python3
"""Assemble consumer evidence from a pinned candidate and both public feeds, or verify a download."""
import argparse
import base64
from concurrent.futures import ThreadPoolExecutor
from functools import partial
import hashlib
import io
import json
import os
import re
import sys
from pathlib import Path
import subprocess
import tempfile
import time
import urllib.error
import urllib.request
import zipfile

import verify_consolidated_candidate as candidates

REPO = candidates.REPOSITORY
SIGNER = f"{REPO}/.github/workflows/packages.yml"
FEEDS = {
    "feedz": "https://f.feedz.io/elsa-workflows/elsa-3/nuget/v3/packages",
    "nuget": "https://api.nuget.org/v3-flatcontainer",
}
CERTIFICATES = "https://api.nuget.org/v3-index/repository-signatures/5.0.0/index.json"
PRODUCER = f"{REPO}/.github/workflows/prepare-consolidated-release-candidate.yml"
CANDIDATE = "nuget-candidate-index.json"
BUNDLE = "nuget-sboms.zip"
INDEX = "nuget-release-index.json"


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def download(url):
    for delay in (1, 4, 15, None):
        try:
            with urllib.request.urlopen(url, timeout=40) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            if delay is None or error.code not in {404, 429, 500, 502, 503, 504}:
                raise
        except (urllib.error.URLError, TimeoutError):
            if delay is None:
                raise
        time.sleep(delay)


def payload(archive):
    with zipfile.ZipFile(io.BytesIO(archive)) as package:
        entries = [i for i in package.infolist() if not i.is_dir()]
        require(len({i.filename.casefold() for i in entries}) == len(entries), "Duplicate package entries")
        return {i.filename: sha(package.read(i)) for i in entries}


def verify_nuget_signature(path, fingerprints, original):
    require(fingerprints and all(candidates.HEX64.fullmatch(value) for value in fingerprints),
            "Invalid nuget.org repository signing certificate list")
    args = ["dotnet", "nuget", "verify", "--all", "--verbosity", "normal", str(path)]
    for fingerprint in fingerprints:
        args.extend(["--certificate-fingerprint", fingerprint])
    result = subprocess.run(args, check=True, capture_output=True, text=True,
                            env=dict(os.environ, DOTNET_CLI_UI_LANGUAGE="en-US", VSLANG="1033", LANG="C"))
    lines = result.stdout.splitlines()
    content_hash = base64.b64encode(hashlib.sha512(original).digest()).decode()
    require(f"Content hash: {content_hash}" in lines, "NuGet signed content hash differs from original candidate")
    require("Signature type: Repository" in lines and "Service index: https://api.nuget.org/v3/index.json" in lines,
            "Expected the nuget.org repository primary signature")


def readback(row, *, version, excluded, stage, fingerprints):
    original = (stage / row["nupkg"]).read_bytes()
    require(sha(original) == row["candidate_sha256"], "Indexed candidate bytes differ from seal")
    identifier = row["id"].lower()
    relative = f"{identifier}/{version}/{identifier}.{version}.nupkg"
    feeds = {}
    for feed, base in FEEDS.items():
        if feed == "nuget" and identifier in excluded:
            continue
        url = f"{base}/{relative}"
        data = download(url)
        if feed == "feedz":
            require(data == original, f"Feedz bytes differ: {row['id']}")
        else:
            with tempfile.TemporaryDirectory() as directory:
                destination = Path(directory) / row["nupkg"]
                destination.write_bytes(data)
                verify_nuget_signature(destination, fingerprints, original)
            signed = payload(data)
            require(signed.pop(".signature.p7s", None) and signed == payload(original),
                    f"Repository-signed package payload differs: {row['id']}")
        feeds[feed] = {"url": url, "sha256": sha(data)}
    return dict(row, feeds=feeds)


def check_bom(content, row, version, framework, expected_assets=None):
    bom = json.loads(content)
    metadata = bom["metadata"]
    root = metadata["component"]
    require(bom.get("bomFormat") == "CycloneDX" and bom.get("specVersion") == "1.6" and
            (root["name"], root["version"]) == (row["id"], version) and
            {"alg": "SHA-256", "content": row["candidate_sha256"]} in root["hashes"], "Wrong SBOM package identity")
    require([p["value"] for p in metadata["properties"] if p["name"] == "elsa:target-framework"] == [framework],
            "Wrong SBOM target framework")
    if expected_assets is not None:
        files = {item["name"]: item for item in root.get("components", []) if item["type"] == "file"}
        for asset in expected_assets:
            file = files[asset["package_path"]]
            require({"alg": "SHA-256", "content": asset["sha256"]} in file["hashes"] and
                    any(p["name"] == "elsa:composition-evidence" for p in file.get("properties", [])),
                    "Missing shipped browser bundle evidence")


def check_restored_graph(bom, restore, framework, groups):
    target = restore["targets"][framework]
    resolved = {tuple(key.rsplit("/", 1)): record for key, record in target.items()}
    downloads = set()
    for item in restore["project"]["frameworks"][framework].get("downloadDependencies", []):
        match = re.fullmatch(r"\[([^,\]]+)(?:,\s*([^\]]+))?\]", item["version"])
        require(match and (not match[2] or match[1] == match[2]), "Ambiguous PackageDownload range")
        downloads.add((item["name"], match[1]))
    components = {(c["name"], c["version"]): c for c in bom.get("components", [])}
    require(set(components) == set(resolved) | downloads, "SBOM does not cover exact producer restore")
    by_id = {name.lower(): components[name, version]["bom-ref"] for name, version in resolved}
    edges = {edge["ref"]: set(edge.get("dependsOn", [])) for edge in bom.get("dependencies", [])}
    root = bom["metadata"]["component"]["bom-ref"]
    declarations = next(group["dependencies"] for group in groups if group["framework"] == framework)
    require(edges.get(root, set()) == {by_id[item["id"].lower()] for item in declarations}, "Missing declared dependency edges")
    for key, record in resolved.items():
        expected = {by_id[name.lower()] for name in record.get("dependencies", {})}
        require(edges.get(components[key]["bom-ref"], set()) == expected, "Missing restored dependency edges")


def check_packed_files(bom, packaged, resources, shipped):
    files = {f["name"]: f for f in bom["metadata"]["component"].get("components", []) if f["type"] == "file"}
    require(set(packaged) == set(files), "SBOM does not cover exact shipped files")
    for name, digest in packaged.items():
        require({"alg": "SHA-256", "content": digest} in files[name]["hashes"], "Wrong packed file hash")
    for path, composition in shipped.items():
        file = files[path]
        require(any(p == {"name": "elsa:composition-evidence", "value": composition["method"]} for p in file.get("properties", [])),
                "Missing bundle composition evidence")
        def compare_modules(component, expected):
            actual = {(c["name"], c["version"]) for c in component.get("components", []) if c["type"] == "library"}
            require(actual == {tuple(item) for item in expected}, "Missing or extra shipped npm components")
        compare_modules(file, composition.get("components", []))
        nested = {r["name"]: r for r in file.get("components", []) if r["type"] == "file"}
        require(set(nested) == {r["name"] for r in composition.get("resources", [])}, "Incorrect resource inventory")
        for item in composition.get("resources", []):
            compare_modules(nested[item["name"]], item.get("components", []))
    for path, expected in resources.items():
        actual = {entry["name"]: entry for entry in files[path].get("components", [])}
        require(set(actual) == set(expected), "Missing or extra embedded browser resources")
        for name, digest in expected.items():
            require({"alg": "SHA-256", "content": digest} in actual[name]["hashes"], "Wrong embedded browser resource hash")


def check_coverage(index, verified_rows, read, read_input, package_directory):
    verified = {r["id"]: r for r in verified_rows}
    rows = index["packages"]
    require(len(rows) == len(verified) and {r["id"] for r in rows} == set(verified), "Candidate SBOM inventory is incomplete")
    documents = {}
    shipped = json.loads(read_input("shipped-javascript.json")) if read_input else {}
    for row in rows:
        original = verified[row["id"]]
        require(candidates.PACKAGE_ID.fullmatch(row["id"]) and row["nupkg"] == f"{row['id']}.{index['version']}.nupkg"
                and row["candidate_sha256"] == original["nupkg_sha256"]
                and row["symbols"] == {"name": original["snupkg"], "sha256": original["snupkg_sha256"]},
                "Wrong package or symbol association")
        require(set(row["frameworks"]) == set(original["frameworks"]), "Missing framework SBOM")
        assets = original["browser_assets"]
        if assets:
            expected = {f"browser/{Path(original['project']).parent.name}.cdx.json"}
            if row["id"] in {"Elsa.Studio.Bpmn.BpmnJs", "Elsa.Studio.Bpmn.X6"}:
                expected.add("browser/Elsa.Studio.Bpmn.cdx.json")
            require({r["path"] for r in row["browser_build_inputs"]} == expected, "Missing browser build inventory")
        package = package_directory / row["nupkg"]
        packaged = payload(package.read_bytes())
        resources = {}
        if row["id"] in {"Elsa.Expressions.JavaScript.Libraries", "Elsa.ReactStudio.Host"} or original["project"].startswith("studio/react/dotnet/modules/"):
            # Read the actual CLR manifest a second time from the sealed archive.
            tools = str(Path(__file__).resolve().parent / "integration-program")
            if tools not in sys.path:
                sys.path.insert(0, tools)
            from shipped_javascript import inspected_resources
            resources = inspected_resources(package)
        snapshots = {item["framework"]: item for item in original["restore_assets"]}
        for framework, record in row["frameworks"].items():
            require(record["path"] == f"boms/{row['id']}/{framework}.cdx.json", "Swapped framework SBOM")
            content = read(record["path"])
            check_bom(content, row, index["version"], framework, assets)
            bom = json.loads(content)
            check_packed_files(bom, packaged, resources, shipped.get(row["id"], {}))
            snapshot = snapshots[framework]
            restore = read_input(snapshot["retained_path"])
            require(sha(restore) == snapshot["sha256"], "Wrong restore snapshot")
            check_restored_graph(bom, json.loads(restore), framework, original["dependency_groups"])
            require(sha(content) == record["sha256"], "Changed framework SBOM")
            documents[record["path"]] = content
        for record in row["browser_build_inputs"]:
            path = record["path"]
            require(path.startswith("browser/") and not Path(path).is_absolute() and ".." not in Path(path).parts
                    and "\\" not in path, "Invalid browser SBOM path")
            content = read(path)
            require(sha(content) == record["sha256"], f"Changed SBOM: {path}")
            documents[path] = content
    return documents


def check_produced(directory):
    evidence = directory / "release-evidence"
    index = json.loads((evidence / "candidate-index.json").read_bytes())
    manifest = json.loads((directory / "verified-artifacts.json").read_bytes())
    require((index["version"], index["source_commit"]) == (manifest["version"], manifest["source_commit"]),
            "Wrong produced index identity")
    check_coverage(index, manifest["packages"], lambda path: (evidence / path).read_bytes(),
                   lambda path: (directory / path).read_bytes(), directory / "artifacts")


def extract_index(pin, transport, stage, bundle):
    candidates.verify_transport(pin, transport, stage)
    run = json.loads((transport / "run.json").read_bytes())
    with zipfile.ZipFile(transport / "candidate.zip") as archive:
        prefix = "release-evidence/"
        require(candidates.CANDIDATE_INDEX in archive.namelist(),
                candidates.missing_candidate_index_message(pin))
        index = json.loads(archive.read(candidates.CANDIDATE_INDEX))
        require((index["schema"], index["version"], index["source_commit"], index["tooling_commit"],
                 index["producer_run"], index["producer_attempt"]) ==
                (1, pin.version, pin.source_sha, run["head_sha"], pin.run_id, run["run_attempt"]),
                "Wrong producer identity in SBOM index")
        verified = json.loads(archive.read("verified-artifacts.json"))["packages"]
        require(len(verified) == pin.feedz_count, "Candidate SBOM count differs from pin")
        documents = check_coverage(index, verified, lambda path: archive.read(prefix + path), archive.read, stage)
        documents[CANDIDATE] = archive.read(candidates.CANDIDATE_INDEX)
        # Stable ZIP headers make evidence-only retries byte-identical.
        with zipfile.ZipFile(bundle, "x") as zipped:
            for path, content in sorted(documents.items()):
                zipped.writestr(zipfile.ZipInfo(path), content, compress_type=zipfile.ZIP_DEFLATED)
    return index


def assemble(version, transport, output):
    pin = candidates.load_pin(version)
    require(not output.exists(), "Use a new output directory")
    with tempfile.TemporaryDirectory(dir=output.parent) as temporary:
        scratch = Path(temporary)
        evidence = scratch / "evidence"
        evidence.mkdir()
        bundle = evidence / BUNDLE
        stage = scratch / "packages"
        index = extract_index(pin, transport, stage, bundle)
        certificates = json.loads(download(CERTIFICATES))
        require(certificates["allRepositorySigned"] is True, "Unverified registry signing policy")
        fingerprints = [cert["fingerprints"]["2.16.840.1.101.3.4.2.1"] for cert in certificates["signingCertificates"]]
        reader = partial(readback, version=version, excluded={name.lower() for name in pin.exclude_ids},
                         stage=stage, fingerprints=fingerprints)
        with ThreadPoolExecutor(max_workers=4) as workers:
            index["packages"] = list(workers.map(reader, index["packages"]))
        require(sum("nuget" in row["feeds"] for row in index["packages"]) == pin.nuget_count,
                "Registry readback count differs from pin")
        index.update(candidate_artifact_id=pin.artifact_id, candidate_archive_sha256=pin.archive_sha256,
                     candidate_manifest_sha256=pin.manifest_sha256, sbom_bundle={"name": BUNDLE, "sha256": sha(bundle.read_bytes())})
        (evidence / INDEX).write_text(json.dumps(index, indent=2, sort_keys=True) + "\n")
        evidence.rename(output)


def verify_attestation(path, workflow, source=None):
    # Trust the named OSS workflow running from main on GitHub-hosted runners.
    args = ["gh", "attestation", "verify", str(path), "--repo", REPO, "--signer-workflow", workflow,
            "--source-ref", "refs/heads/main", "--deny-self-hosted-runners"]
    if source:
        args.extend(["--source-digest", source])
    subprocess.run(args, check=True, stdout=sys.stderr)


def select_sample(index):
    eligible = [row for row in index["packages"] if {"feedz", "nuget"} <= row["feeds"].keys() and row["frameworks"]]
    require(eligible, "Release has no package and framework distributed on both feeds")
    row = next((row for row in eligible if row["id"] == "Elsa"), eligible[0])
    require(candidates.PACKAGE_ID.fullmatch(row["id"]), "Invalid sample package identity")
    framework = "net10.0" if "net10.0" in row["frameworks"] else sorted(row["frameworks"])[0]
    return {"id": row["id"], "framework": framework}


def sample(version, index_file):
    verify_attestation(index_file, SIGNER)
    index = json.loads(index_file.read_bytes())
    require(index["schema"] == 1 and index["version"] == version, "Wrong release index")
    print(json.dumps(select_sample(index)))


def verify(version, identifier, framework, feed, package, index_file, bundle):
    verify_attestation(index_file, SIGNER)
    index = json.loads(index_file.read_bytes())
    require(index["schema"] == 1 and index["version"] == version, "Wrong release index")
    require(sha(bundle.read_bytes()) == index["sbom_bundle"]["sha256"], "Wrong SBOM bundle")
    matches = [p for p in index["packages"] if p["id"].casefold() == identifier.casefold()]
    require(len(matches) == 1, "Package ID is missing or ambiguous")
    row = matches[0]
    require(feed in row["feeds"] and sha(package.read_bytes()) == row["feeds"][feed]["sha256"], "Wrong feed artifact")
    require(framework in row["frameworks"], "Framework was not released")
    record = row["frameworks"][framework]
    with zipfile.ZipFile(bundle) as archive:
        content = archive.read(record["path"])
        with tempfile.TemporaryDirectory() as temporary:
            source_index = Path(temporary) / CANDIDATE
            source_index.write_bytes(archive.read(CANDIDATE))
            verify_attestation(source_index, PRODUCER, index["tooling_commit"])
            candidate = json.loads(source_index.read_bytes())
        require(all(candidate[k] == index[k] for k in ("source_commit", "tooling_commit", "producer_run",
                                                      "producer_attempt", "version")), "Candidate provenance mismatch")
        source_row = next(p for p in candidate["packages"] if p["id"] == row["id"])
        require({k: row[k] for k in source_row} == source_row, "Release mapping differs from candidate index")
    require(sha(content) == record["sha256"], "Wrong framework SBOM")
    check_bom(content, row, version, framework)
    print(f"Verified {row['id']} {version} from {feed} against {record['path']} and source {index['source_commit']}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    produced = commands.add_parser("check-produced")
    produced.add_argument("--directory", required=True, type=Path)
    build = commands.add_parser("assemble")
    build.add_argument("--version", required=True)
    build.add_argument("--transport", required=True, type=Path)
    build.add_argument("--output", required=True, type=Path)
    selection = commands.add_parser("sample")
    selection.add_argument("--version", required=True)
    selection.add_argument("--index-file", required=True, type=Path)
    consumer = commands.add_parser("verify")
    for name in ("version", "identifier", "framework"):
        consumer.add_argument("--" + name, required=True)
    consumer.add_argument("--feed", required=True, choices=FEEDS)
    for name in ("package", "index-file", "bundle"):
        consumer.add_argument("--" + name, required=True, type=Path)
    args = vars(parser.parse_args())
    command = args.pop("command")
    {"assemble": assemble, "verify": verify, "sample": sample, "check-produced": check_produced}[command](**args)


if __name__ == "__main__":
    main()
