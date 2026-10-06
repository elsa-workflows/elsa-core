"""Owned execution and portable evidence for the package browser matrix.

Private host state, credentials and command logs stay outside retained-evidence.
Missing runtime/resource verifier seams fail before expensive host execution.
"""
from __future__ import annotations

import argparse
import copy
import json
from pathlib import Path
import re
import subprocess
from urllib.parse import urlsplit
from urllib.request import Request, urlopen

import consolidated_candidate_input as candidate
import materialize_paired_package_hosts as hosts
import paired_package_baseline_provenance as baseline
import paired_package_baseline_resources as baseline_resources
import paired_package_provenance as provenance
import prove_consolidated_package_consumers as packages
import run_paired_package_browser_matrix as browser
import verify_browser_package_resources as resources


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def selected_cells(cell: str | None) -> list[tuple[str, str, str]]:
    if cell is None:
        # Share a build/cache only within one exact version/framework group.
        return [(version, framework, host) for version in browser.VERSIONS
                for framework in browser.FRAMEWORKS for host in browser.HOSTS]
    key = tuple(re.split(r"[,/]", cell))
    require(len(key) == 3 and key in browser.MATRIX, "Invalid development cell")
    return [key]


def cell_request(key: tuple[str, str, str]) -> hosts.CellRequest:
    require(key in browser.MATRIX, "Invalid package browser cell")
    version, framework, host = key
    # Released shell/editor/export smoke does not certify optional EF Secrets.
    # Its published lifetime defect remains recorded; candidate coverage is full.
    features = (("workflow-contexts", "secrets") if version == candidate.PRODUCER["version"]
                else ("workflow-contexts",))
    return hosts.CellRequest(host, framework, version, backend_features=features)


def _write(path: Path, value: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    require(not path.exists() and not path.is_symlink(), "Refusing to overwrite execution evidence")
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def _json_request(url: str, *, data: dict | None = None, token: str | None = None):
    parsed = urlsplit(url)
    require(parsed.scheme == "http" and parsed.hostname == "127.0.0.1" and parsed.port is not None
            and not parsed.username and not parsed.password and not parsed.query and not parsed.fragment,
            "Runtime evidence requires an owned loopback listener")
    headers = {"Content-Type": "application/json"}
    if token is not None:
        headers["Authorization"] = "Bearer " + token
    request = Request(url, data=None if data is None else json.dumps(data).encode(), headers=headers)
    with urlopen(request, timeout=15) as response:
        require(response.status == 200, "Runtime evidence request failed")
        body = response.read(4 * 1024 * 1024 + 1)
    require(len(body) <= 4 * 1024 * 1024, "Runtime evidence response exceeded bound")
    return json.loads(body)


def verify_runtime_readiness(value: dict, request: hosts.CellRequest) -> dict:
    require(isinstance(value, dict) and set(value) == {
        "schema", "framework", "runtime", "auth_mode", "permission_profile", "permission_grants",
        "workflow_contexts_enabled", "secrets_enabled", "features"}, "Invalid runtime readiness fields")
    require(type(value["schema"]) is int and value["schema"] == 1, "Invalid runtime readiness schema")
    framework = request.framework.removeprefix("net")
    require(value["framework"] == ".NETCoreApp,Version=v" + framework, "Runtime target framework differs")
    require(isinstance(value["runtime"], str)
            and re.fullmatch(r"\.NET " + re.escape(framework) + r"\.[0-9]{1,6}", value["runtime"]) is not None,
            "Invalid actual runtime identity")
    require(value["auth_mode"] == "ElsaIdentity" and value["permission_profile"] == request.permission_profile,
            "Runtime authentication/profile differs")
    require(type(value["permission_grants"]) is list
            and value["permission_grants"] == list(hosts.permission_grants(request)), "Runtime permission grants differ")
    features = value["features"]
    require(type(features) is list and 0 < len(features) <= 256
            and all(isinstance(name, str) and re.fullmatch(r"Elsa\.[A-Za-z0-9_.]{1,120}", name) for name in features)
            and len(features) == len(set(features)), "Invalid actual installed features")
    required = {"Elsa.Identity", "Elsa.DefaultAuthentication", "Elsa.WorkflowManagement", "Elsa.WorkflowRuntime",
                "Elsa.WorkflowsApi", "Elsa.EFCoreWorkflowDefinitionPersistence", "Elsa.EFCoreWorkflowInstancePersistence",
                "Elsa.EFCoreWorkflowRuntimePersistence"}
    require(required <= set(features), "Missing required backend feature")
    bpmn = {"Elsa.Bpmn", "Elsa.BpmnInterchange"}
    require(not bpmn.intersection(features) if request.version == "3.8.4" else bpmn <= set(features),
            "Runtime versioned BPMN registration differs")
    for name, flag, installed in (
        ("workflow-contexts", "workflow_contexts_enabled", {"Elsa.WorkflowContexts"}),
        ("secrets", "secrets_enabled", {"Elsa.Secrets", "Elsa.EFCoreSecretsPersistence"})):
        enabled = name in request.backend_features
        require(type(value[flag]) is bool and value[flag] is enabled, "Runtime feature configuration differs")
        require(installed <= set(features) if enabled else not installed.intersection(features),
                "Runtime optional feature registration differs")
    return {**value, "permission_grants": list(value["permission_grants"]), "features": sorted(features)}


def _observe_ready(handle, request: hosts.CellRequest) -> dict:
    origin = handle.backend_url.removesuffix("/elsa/api")
    return verify_runtime_readiness(_json_request(origin + "/_fixture/ready"), request)


def _observe_loaded(handle, layout) -> dict[str, list[dict]]:
    # This private API login supplies only metadata transport; the browser must
    # independently demonstrate the normal Studio sign-in journey.
    login = _json_request(handle.backend_url + "/identity/login",
                          data={"username": handle.username, "password": handle.password})
    require(login.get("isAuthenticated") is True and isinstance(login.get("accessToken"), str),
            "Runtime metadata authentication failed")
    origin = handle.backend_url.removesuffix("/elsa/api")
    result = {"backend": _json_request(origin + "/_fixture/assemblies", token=login["accessToken"])}
    if layout.request.host == "server":
        result["server"] = _json_request(handle.studio_url.rstrip("/") + "/_fixture/assemblies")
    for rows in result.values():
        require(isinstance(rows, list) and 0 < len(rows) <= 512, "Invalid loaded assembly inventory")
        for row in rows:
            require(isinstance(row, dict) and set(row) == {
                "name", "fullName", "version", "informationalVersion", "location", "sha256"},
                "Invalid runtime metadata fields")
    return result


def _verify_loaded(layout, observations: dict, verified_root: Path, manifest: dict) -> dict:
    result = {}
    version, source, by_id, _, _ = packages._validated_manifest(manifest)
    for host, rows in observations.items():
        project = layout.project_paths[host]
        fixture = [row for row in rows if row["name"] == project.stem]
        library_rows = [row for row in rows if row["name"] != project.stem]
        required = ("Elsa.Workflows.Core",) if host == "backend" else ("Elsa.Studio.Core",)
        if layout.request.version == candidate.PRODUCER["version"]:
            assets, _ = provenance.read_package_assets(project, layout.request.framework)
            records = packages.verify_loaded_assemblies(
                {"loadedAssemblies": library_rows}, assets, layout.request.framework, project.parent,
                layout.packages_root, verified_root / "artifacts", by_id, version, source,
                required_assemblies=required)
        else:
            records = baseline.verify_baseline_loaded_assemblies(
                library_rows, project, layout.packages_root, layout.request.framework,
                layout.request.version, required_assemblies=required)
        for record in records:
            location = provenance.regular_file(Path(record["location"]).absolute())
            require(location.is_relative_to(project.parent / "bin"), "Loaded assembly escaped owned output")
            record["location"] = location.relative_to(project.parent).as_posix()
        fixture_records = []
        if host == "server":
            require(len(fixture) == 1, "Missing or duplicate Server fixture assembly")
            expected = provenance.regular_file(project.parent / "bin" / "Release" / layout.request.framework / (project.stem + ".dll"))
            require(Path(fixture[0]["location"]).resolve() == expected
                    and fixture[0]["sha256"] == provenance.sha256(expected), "Server fixture assembly differs")
            fixture_records.append({"name": project.stem, "sha256": fixture[0]["sha256"],
                                    "location": expected.relative_to(project.parent).as_posix(), "owner": "fixture"})
        else:
            require(not fixture, "Unexpected fixture assembly in package metadata")
        result[host] = {"package_assemblies": records, "fixture_assemblies": fixture_records}
    require(set(result) == ({"backend", "server"} if layout.request.host == "server" else {"backend"}),
            "Missing owned runtime metadata")
    return result


def evidence_gaps(request) -> list[str]:
    missing = []
    if request.version != candidate.PRODUCER["version"]:
        if not callable(getattr(baseline, "verify_baseline_loaded_assemblies", None)):
            missing.append("baseline_loaded_assemblies")
        if not callable(getattr(baseline_resources, "derive_baseline_resources", None)):
            missing.append("baseline_browser_resources")
    # Backend metadata alone cannot attest browser-loaded WASM assemblies.
    if request.host != "server":
        missing.append("wasm_runtime_and_converter")
    return missing


def _project_validator(layout, verified_root: Path, manifest: dict):
    if layout.request.version == candidate.PRODUCER["version"]:
        return provenance.candidate_project_validator(layout, verified_root, manifest)
    projects = {path.resolve(): host for host, path in layout.project_paths.items()}
    def validate(project):
        project = provenance.regular_file(project.absolute())
        require(project in projects, "Unowned baseline project")
        fixture = layout.project_paths.get("wasm") if projects[project] == "hosted-wasm" else None
        return baseline.validate_baseline_project(project, layout.packages_root, layout.request.framework,
                                                  layout.request.version, fixture_project=fixture)
    return validate


def _resource_inventory(layout, verified_root: Path, manifest_hash: str) -> dict:
    project = layout.project_paths[layout.request.host]
    build_manifest = project.parent / "obj" / "Release" / layout.request.framework / "staticwebassets.build.json"
    prefix = "/" + layout.request.route_prefix if layout.request.route_prefix else ""
    if layout.request.version == candidate.PRODUCER["version"]:
        return resources.derive_candidate_resources(build_manifest, verified_root, layout.packages_root,
                    verified_manifest_sha256=manifest_hash, route_prefix=prefix)
    return baseline_resources.derive_baseline_resources(build_manifest, layout.packages_root, layout.request.version,
                                                        route_prefix=prefix)


def _command_receipts(commands: list[dict], group: Path) -> list[dict]:
    records = []
    for command in commands:
        if command.get("stage") == "reuse_verified_build":
            require(command.get("project") in {"backend", *hosts.HOST_NAMES}
                    and isinstance(command.get("project_assets_sha256"), str)
                    and re.fullmatch(r"[0-9a-f]{64}", command["project_assets_sha256"]),
                    "Invalid reused project evidence")
            records.append({key: command[key] for key in ("stage", "project", "project_assets_sha256")})
        else:
            log = provenance.regular_file(Path(command["log"]).absolute())
            require(log.is_relative_to(group / "logs") and command["exit_code"] == 0, "Invalid build command evidence")
            records.append({"stage": "completed_command", "exit_code": 0,
                            "log_name": log.name, "log_sha256": provenance.sha256(log)})
    return records


def execute_cell(key, *, private: Path, retained: Path, verified_root: Path, manifest: dict,
                 manifest_hash: str, sdk: str) -> dict:
    version, framework, host = key
    request = cell_request(key)
    cell_root = retained / "cells" / f"{version}-{framework}-{host}"
    evidence = {"schema": 1, "version": version, "framework": framework, "host": host,
                "execution_sdk": sdk, "result": "failed", "stage": "evidence_preflight",
                "requested_backend_features": list(request.backend_features),
                "permission_profile": request.permission_profile,
                "feature_policy": ("candidate_representative_features" if version == candidate.PRODUCER["version"]
                                   else "released_shell_editor_export_contexts_only")}
    original_browser = None
    try:
        evidence["missing_evidence"] = evidence_gaps(request)
        require(not evidence["missing_evidence"], "Required package browser evidence is unavailable")
        _, _, by_id, exceptions, _ = packages._validated_manifest(manifest)
        config = (packages.render_nuget_config(verified_root / "artifacts", [row["id"] for row in by_id.values()], exceptions)
                  if version == candidate.PRODUCER["version"] else
                  '<configuration><packageSources><clear /><add key="nuget.org" value="' + packages.NUGET_ORG + '" /></packageSources></configuration>')
        group = private / "groups" / f"{version}-{framework}"
        evidence["stage"] = "materialize"
        layout = hosts.materialize(request, group, nuget_config=config, packages_root=group / "packages", sdk=sdk)
        evidence["stage"] = "build"
        evidence["commands"] = _command_receipts(hosts.build(layout), group)
        validate = _project_validator(layout, verified_root, manifest)
        evidence["stage"] = "project_provenance"
        evidence["projects"] = {name: validate(project) for name, project in layout.project_paths.items()}
        evidence["stage"] = "resource_provenance"
        inventory = _resource_inventory(layout, verified_root, manifest_hash)
        evidence["resource_inventory"] = inventory
        evidence["stage"] = "owned_runtime"
        runtime_failed = False
        with hosts.start_pair(layout, validate_project=validate) as handle:
            try:
                evidence["stage"] = "runtime_readiness"
                evidence["runtime_readiness"] = _observe_ready(handle, request)
                evidence["stage"] = "browser_execution"
                child = browser.run_browser(handle, request, inventory["assets"])
                # Validate before any returned child data enters portable evidence.
                original_browser = browser.validate_browser_receipt(child, key)
                evidence["stage"] = "loaded_assemblies"
                evidence["loaded_assemblies"] = _verify_loaded(layout, _observe_loaded(handle, layout), verified_root, manifest)
            except Exception:
                runtime_failed = True
        # Reaching this line proves the context's cleanup completed. If cleanup
        # itself raises, no successful cleanup claim is retained.
        evidence["owned_process_cleanup"] = True
        require(not runtime_failed, "Owned runtime did not produce valid evidence")
        record = copy.deepcopy(original_browser)
        evidence["stage"] = "browser_resources"
        prefix = "/" + request.route_prefix if request.route_prefix else ""
        evidence["browser_resources"] = resources.verify_browser_resources(inventory["assets"], record["resources"],
                                                                           require_all=False, route_prefix=prefix)
        evidence["stage"] = "browser_contract"
        # Only these independently demonstrated Python-owned assertions may be
        # completed here; unimplemented UI assertions remain unchanged.
        for assertion in record["assertions"]:
            if assertion["name"] in {"package_provenance", "browser_resources"}:
                assertion.update(passed=True, reason_category=None)
        # A child computes its result before Python-owned provenance checks. Its
        # original receipt stays immutable; only a complete, nonfailed combined
        # result can pass the matrix after the owned cleanup above.
        record["result"] = ("passed" if original_browser["result"] != "failed"
                            and all(item["passed"] is True for item in record["assertions"]) else "failed")
        browser.check_cell(record)
        evidence.update(stage="complete", result="passed")
        return record
    except Exception:
        evidence["failure_category"] = "execution_or_evidence_failed"
        raise ValueError("Package browser execution or evidence failed") from None
    finally:
        _write(cell_root / "execution.json", evidence)
        if original_browser is not None:
            _write(cell_root / "browser.json", browser.validate_browser_receipt(original_browser, key))


def run(inputs: Path, candidate_artifacts: Path, output: Path, *, fixture_source: str,
        fixture_run: int | None = None, fixture_attempt: int | None = None, cell: str | None = None) -> dict:
    selected = selected_cells(cell)
    output = browser._external_path(output)
    require(not output.exists(), "Execution output must be fresh")
    inputs_path, artifacts_path = browser._external_path(inputs), browser._external_path(candidate_artifacts)
    require(not output.is_relative_to(inputs_path) and not inputs_path.is_relative_to(output)
            and not output.is_relative_to(artifacts_path) and not artifacts_path.is_relative_to(output),
            "Execution roots overlap")
    retained, private = output / "retained-evidence", output / "private"
    retained.mkdir(parents=True, mode=0o700)
    private.mkdir(mode=0o700)
    matrix_path = retained / "matrix.json"
    try:
        original = browser.prepare_candidate(inputs, candidate_artifacts, retained / "inputs",
                          fixture_source=fixture_source, fixture_run=fixture_run, fixture_attempt=fixture_attempt)
        verified_root = artifacts_path
        manifest = json.loads((verified_root / "verified-artifacts.json").read_text())
        sdk = subprocess.check_output(["dotnet", "--version"], cwd=browser.JOURNEY.parents[4], text=True).strip()
        require(re.fullmatch(r"10\.[0-9]+\.[0-9]+", sdk) is not None, "Unsupported execution SDK")
        return browser.run_matrix(lambda key: execute_cell(key, private=private, retained=retained,
                 verified_root=verified_root, manifest=manifest, manifest_hash=original["verified_artifacts_sha256"], sdk=sdk),
                 selected, matrix_path)
    except Exception:
        if not matrix_path.exists():
            ledger = browser.new_ledger()
            ledger.update(development_only=cell is not None, failure_category="execution_setup_failed")
            _write(matrix_path, ledger)
        raise ValueError("Package browser matrix did not satisfy acceptance") from None


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("inputs", "candidate-artifacts", "output"):
        parser.add_argument("--" + name, type=Path, required=True)
    parser.add_argument("--fixture-source", required=True)
    parser.add_argument("--fixture-run", type=int)
    parser.add_argument("--fixture-attempt", type=int)
    parser.add_argument("--cell", help="Development only: VERSION/FRAMEWORK/HOST (commas also accepted); never full acceptance")
    args = parser.parse_args(argv)
    try:
        ledger = run(args.inputs, args.candidate_artifacts, args.output, fixture_source=args.fixture_source,
                     fixture_run=args.fixture_run, fixture_attempt=args.fixture_attempt, cell=args.cell)
        return 0 if ledger["passed"] else 1
    except Exception:
        print("Package browser matrix incomplete or failed; inspect sanitized retained evidence")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
