"""Owned execution and portable evidence for the package browser matrix.

Private host state, credentials and command logs stay outside retained-evidence.
Missing runtime/resource verifier seams fail before expensive host execution.
"""
from __future__ import annotations

import argparse
import copy
from contextlib import ExitStack
import hashlib
import os
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
import paired_package_converter_selection as converter_selection
import paired_package_provenance as provenance
import paired_package_provenance_diagnostics as provenance_diagnostics
import paired_package_released_documents as documents
import paired_package_react_phase as react_phase
import paired_package_secrets_endpoints as secrets_endpoints
import paired_package_wasm_resources as wasm_resources
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


def _metadata_token(handle) -> str:
    # This private API login supplies only metadata transport; the browser must
    # independently demonstrate the normal Studio sign-in journey.
    login = _json_request(handle.backend_url + "/identity/login",
                          data={"username": handle.username, "password": handle.password})
    require(login.get("isAuthenticated") is True and isinstance(login.get("accessToken"), str),
            "Runtime metadata authentication failed")
    return login["accessToken"]


def _observe_loaded(handle, layout) -> dict[str, list[dict]]:
    token = _metadata_token(handle)
    origin = handle.backend_url.removesuffix("/elsa/api")
    result = {"backend": _json_request(origin + "/_fixture/assemblies", token=token)}
    if layout.request.host == "server":
        result["server"] = _json_request(handle.studio_url.rstrip("/") + "/_fixture/assemblies")
    for rows in result.values():
        require(isinstance(rows, list) and 0 < len(rows) <= 512, "Invalid loaded assembly inventory")
        for row in rows:
            require(isinstance(row, dict) and set(row) == {
                "name", "fullName", "version", "informationalVersion", "location", "sha256"},
                "Invalid runtime metadata fields")
    return result


def _observe_secrets_ownership(handle, verified_assemblies: dict) -> dict:
    origin = handle.backend_url.removesuffix("/elsa/api")
    observation = _json_request(origin + "/_fixture/secrets-endpoints", token=_metadata_token(handle))
    secrets_endpoints.validate_secrets_endpoint_evidence(
        observation, verified_assemblies["backend"]["package_assemblies"])
    return observation


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
    if request.host != "server":
        if not callable(getattr(converter_selection, "prepare_decoder", None)):
            missing.append("wasm_converter_selection")
        if not callable(getattr(wasm_resources, "derive_candidate_wasm_resources", None)):
            missing.append("wasm_package_resources")
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


def _resource_inventory(layout, verified_root: Path, manifest_hash: str, *, converter: dict | None = None) -> dict:
    project = layout.project_paths[layout.request.host]
    build_manifest = project.parent / "obj" / "Release" / layout.request.framework / "staticwebassets.build.json"
    prefix = "/" + layout.request.route_prefix if layout.request.route_prefix else ""
    if layout.request.version == candidate.PRODUCER["version"]:
        static = resources.derive_candidate_resources(build_manifest, verified_root, layout.packages_root,
                    verified_manifest_sha256=manifest_hash, route_prefix=prefix)
    else:
        static = baseline_resources.derive_baseline_resources(build_manifest, layout.packages_root, layout.request.version,
                                                        route_prefix=prefix)
    if layout.request.host == "server":
        require(converter is None, "Server cannot claim a WASM converter")
        inventory = static
    else:
        require(isinstance(converter, dict) and converter, "Missing selected WASM converter evidence")
        client = layout.project_paths["wasm" if layout.request.host == "hosted-wasm" else layout.request.host]
        client_manifest = client.parent / "obj" / "Release" / layout.request.framework / "staticwebassets.build.json"
        if layout.request.version == candidate.PRODUCER["version"]:
            managed = wasm_resources.derive_candidate_wasm_resources(layout, client, client_manifest, verified_root,
                        verified_manifest_sha256=manifest_hash, converter=converter, route_prefix=prefix)
        else:
            managed = wasm_resources.derive_baseline_wasm_resources(layout, client, client_manifest,
                        converter=converter, route_prefix=prefix)
        assets = static["assets"] + managed["assets"]
        require(len({asset["path"] for asset in assets}) == len(assets), "Static and managed resource paths overlap")
        inventory = {**static, "assets": assets,
                     "managed_resources": {name: value for name, value in managed.items() if name != "assets"}}
        if layout.request.host == "wasm":
            import paired_package_wasm_boot as wasm_boot
            bootstrap = wasm_boot.derive_boot_resources(layout, client, client_manifest, managed)
            assets.extend(bootstrap["assets"])
            require(len({asset["path"] for asset in assets}) == len(assets), "Bootstrap and original resource paths overlap")
            inventory["bootstrap_resources"] = {name: value for name, value in bootstrap.items() if name != "assets"}

    # The six paths remain mandatory package materializations and retain their sealed
    # archive checks. This separate host policy only changes whether the standalone
    # stylesheet must be requested over HTTP; it does not relax any other resource.
    policy_path = resources.CONVERTER_POLICY.with_name("coverage-policy.json")
    policy_bytes = policy_path.read_bytes()
    policy = json.loads(policy_bytes)
    stylesheet = "/_content/Elsa.Studio.Workflows.Designer/designer.css"
    overrides = policy.get("host_network_request_overrides")
    require(isinstance(policy.get("required_browser_assets"), list)
            and stylesheet in policy["required_browser_assets"],
            "Designer stylesheet must remain a mandatory package materialization")
    require(isinstance(overrides, dict) and set(overrides) == {stylesheet},
            "Invalid host-specific browser request policy")
    host_overrides = overrides[stylesheet]
    require(isinstance(host_overrides, dict) and set(host_overrides) == set(browser.HOSTS)
            and all(type(value) is bool for value in host_overrides.values()),
            "Incomplete host-specific browser request policy")
    required_request = host_overrides[layout.request.host]
    stylesheet_path = prefix + stylesheet
    stylesheet_assets = [asset for asset in inventory["assets"] if asset.get("path") == stylesheet_path]
    require(len(stylesheet_assets) == 1 and stylesheet_assets[0].get("owner") == "package"
            and stylesheet_assets[0].get("required") is True,
            "Designer stylesheet is missing from the verified package inventory")
    stylesheet_assets[0]["required"] = required_request
    inventory["host_network_policy"] = {
        "coverage_policy_sha256": resources.sha256(policy_bytes),
        "host": layout.request.host,
        "stylesheet": {"path": stylesheet_path, "required": required_request,
                       "required_hosts": sorted(host for host, required in host_overrides.items() if required)},
    }
    return inventory


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


def released_document_inputs(key, retained: Path, fixture_identity: dict) -> list[dict]:
    """Resolve only the two completed baseline cells matching this candidate."""
    require(key in browser.MATRIX and key[0] == candidate.PRODUCER["version"], "Released inputs require a candidate cell")
    result = []
    for version in documents.TOOL_VERSIONS:
        source_cell = (version, key[1], key[2])
        cell = browser._external_path(retained) / "cells" / "-".join(source_cell)
        receipts = {}
        for name in ("execution", "browser"):
            path = provenance.regular_file(cell / (name + ".json"))
            require(0 < path.stat().st_size <= 8 * 1024 * 1024, "Unbounded released source receipt")
            receipts[name] = json.loads(path.read_text(encoding="utf-8"))
            require(isinstance(receipts[name], dict), "Invalid released source receipt")
        evidence = receipts["execution"]
        require(evidence.get("result") == "passed" and evidence.get("stage") == "complete", "Released source cell did not pass")
        path = cell / "released-document.json"
        binding = documents.bind_released_document(path, source_cell, receipts["browser"], evidence, fixture_identity)
        require(binding == evidence.get("released_document"), "Released source evidence changed")
        result.append({"private_path": str(path), "binding": binding})
    return result


def execute_cell(key, *, private: Path, retained: Path, verified_root: Path, manifest: dict,
                 manifest_hash: str, sdk: str, fixture_identity: dict | None = None) -> dict:
    version, framework, host = key
    request = cell_request(key)
    cell_root = retained / "cells" / f"{version}-{framework}-{host}"
    evidence = {"schema": 1, "version": version, "framework": framework, "host": host,
                "route_prefix": request.route_prefix,
                "execution_sdk": sdk, "result": "failed", "stage": "evidence_preflight",
                "requested_backend_features": list(request.backend_features),
                "permission_profile": request.permission_profile,
                "feature_policy": ("candidate_representative_features" if version == candidate.PRODUCER["version"]
                                   else "released_shell_editor_export_contexts_only")}
    original_browser = None
    react_receipt = None
    cleanup_categories = set()
    released_output = None
    try:
        evidence["missing_evidence"] = evidence_gaps(request)
        require(not evidence["missing_evidence"], "Required package browser evidence is unavailable")
        inputs = None
        if version == candidate.PRODUCER["version"]:
            evidence["stage"] = "released_inputs"
            inputs = released_document_inputs(key, retained, fixture_identity)
            evidence["released_document_inputs"] = [item["binding"] for item in inputs]
        _, _, by_id, exceptions, _ = packages._validated_manifest(manifest)
        config = (packages.render_nuget_config(verified_root / "artifacts", [row["id"] for row in by_id.values()], exceptions)
                  if version == candidate.PRODUCER["version"] else
                  '<configuration><packageSources><clear /><add key="nuget.org" value="' + packages.NUGET_ORG + '" /></packageSources></configuration>')
        group = private / "groups" / f"{version}-{framework}"
        evidence["stage"] = "materialize"
        layout = hosts.materialize(request, group, nuget_config=config, packages_root=group / "packages", sdk=sdk)
        evidence["stage"] = "build"
        build_options = {}
        if host != "server":
            evidence["stage"] = "converter_preparation"
            build_options["converter_decoder"] = converter_selection.prepare_decoder(
                private / "converter-decoder", sdk, hosts.isolated_environment(layout))
        evidence["stage"] = "build"
        def report_operation(component: str, phase: str) -> None:
            require(component in {"sdk", "backend", "converter", *hosts.HOST_NAMES} and phase in {
                "probe", "reuse_validation", "restore", "build", "decoder_validation", "build_command",
                "trace_inventory", "decode_command", "decode_parse", "binding_validation",
                "trace_revalidation", "selection_write", "binding_traces", "binding_events",
                "binding_owners", "binding_identities", "binding_contexts", "binding_task_edge",
                "binding_requestors", "binding_archive", "binding_paths", "binding_bytes", "binding_policy"},
                "Invalid build operation diagnostic")
            evidence["last_build_operation"] = {"component": component, "phase": phase}
        commands = hosts.build(layout, report_operation=report_operation, **build_options)
        evidence["commands"] = _command_receipts(commands, group)
        selections = [command["converter_selection"] for command in commands if "converter_selection" in command]
        require(len(selections) == (0 if host == "server" else 1), "Missing or ambiguous client converter selection")
        inventory_options = {}
        if selections:
            evidence["converter_selection"] = selections[0]
            inventory_options["converter"] = selections[0]["converter"]
        validate = provenance_diagnostics.wrap_project_validator(
            layout.project_paths, _project_validator(layout, verified_root, manifest), evidence)
        evidence["stage"] = "project_provenance"
        evidence["projects"] = {name: validate(project) for name, project in layout.project_paths.items()}
        evidence["stage"] = "resource_provenance"
        inventory = _resource_inventory(layout, verified_root, manifest_hash, **inventory_options)
        evidence["resource_inventory"] = inventory
        evidence["stage"] = "owned_runtime"
        if version in documents.TOOL_VERSIONS:
            document_root = private / "documents" / "-".join(key)
            document_root.mkdir(parents=True, mode=0o700, exist_ok=False)
            released_output = document_root / "released-document.json"
        runtime_failed = False
        browser_cleanup_verified = True
        dual_designer = version == candidate.PRODUCER["version"] and host in browser.REACT_PHASE_HOSTS
        with ExitStack() as runtime:
            owner = (runtime.enter_context(hosts.start_designer_phases(layout, validate_project=validate))
                     if dual_designer else None)
            primary = owner.phase("x6") if owner else hosts.start_pair(layout, validate_project=validate)
            with primary as handle:
                try:
                    evidence["stage"] = "runtime_readiness"
                    evidence["runtime_readiness"] = _observe_ready(handle, request)
                    evidence["stage"] = "browser_execution"
                    options = {"released_document_output": released_output} if released_output is not None else {}
                    if inputs is not None:
                        options["released_document_inputs"] = inputs
                    child = browser.run_browser(handle, request, inventory["assets"], **options)
                    # Validate before any returned child data enters portable evidence.
                    original_browser = browser.validate_browser_receipt(child, key)
                    evidence["stage"] = "loaded_assemblies"
                    evidence["loaded_assemblies"] = _verify_loaded(layout, _observe_loaded(handle, layout), verified_root, manifest)
                    if version == candidate.PRODUCER["version"] and any(
                            item["name"] == "secrets" and item["passed"] is True for item in original_browser["assertions"]):
                        evidence["stage"] = "secrets_endpoint_ownership"
                        evidence["secrets_endpoint_ownership"] = _observe_secrets_ownership(handle, evidence["loaded_assemblies"])
                    if owner:
                        evidence["stage"] = "react_source_binding"
                        # Fail before launching a second Studio if X6 did not
                        # establish the workflow and cleanup required by React.
                        react_phase.source_bindings(request, original_browser)
                except browser.BrowserCleanupUnverified as failure:
                    runtime_failed = True
                    browser_cleanup_verified = False
                    cleanup_categories.update(failure.categories)
                except Exception:
                    runtime_failed = True
            if owner and not runtime_failed:
                evidence["stage"] = "react_runtime"
                with owner.phase("react-flow") as react_handle:
                    try:
                        require(handle.process_ids and react_handle.process_ids
                                and handle.process_ids[0] == react_handle.process_ids[0]
                                and all(getattr(handle, name) == getattr(react_handle, name) for name in
                                        ("studio_url", "backend_url", "username", "password", "safe_ids")),
                                "Designer phase runtime continuity differs")
                        readiness = _observe_ready(react_handle, request)
                        require(readiness == evidence["runtime_readiness"], "Designer phase backend readiness changed")
                        evidence["react_runtime_continuity"] = True
                        evidence["stage"] = "react_browser_execution"
                        child = react_phase.run_react_phase(react_handle, request, inventory["assets"], original_browser)
                        react_receipt = react_phase.validate_react_phase_receipt(child, request, original_browser, inventory["assets"])
                        evidence["react_phase"] = react_phase.summarize_react_phase(react_receipt)
                        evidence["stage"] = "react_loaded_assemblies"
                        evidence["react_loaded_assemblies"] = _verify_loaded(
                            layout, _observe_loaded(react_handle, layout), verified_root, manifest)
                    except browser.BrowserCleanupUnverified as failure:
                        runtime_failed = True
                        browser_cleanup_verified = False
                        cleanup_categories.update(failure.categories)
                    except Exception:
                        runtime_failed = True
        # Host context cleanup does not establish browser descendant cleanup.
        # If either is uncertain, never retain a successful combined claim.
        evidence["owned_process_cleanup"] = browser_cleanup_verified
        require(not runtime_failed, "Owned runtime did not produce valid evidence")
        record = copy.deepcopy(original_browser)
        if react_receipt is not None:
            record["resources"].extend(copy.deepcopy(react_receipt["resources"]))
            record.setdefault("proof", {})["reactflow"] = evidence["react_phase"]
        evidence["stage"] = "browser_resources"
        prefix = "/" + request.route_prefix if request.route_prefix else ""
        evidence["browser_resources"] = resources.verify_browser_resources(inventory["assets"], record["resources"],
                                                                           require_all=False, route_prefix=prefix)
        if released_output is not None:
            evidence["stage"] = "released_document"
            evidence["released_document"] = documents.bind_released_document(
                released_output, key, original_browser, evidence, fixture_identity)
        evidence["stage"] = "browser_contract"
        # Only these independently demonstrated Python-owned assertions may be
        # completed here; unimplemented UI assertions remain unchanged.
        for assertion in record["assertions"]:
            if assertion["name"] in {"package_provenance", "browser_resources"} or (
                    assertion["name"] == "released_document" and released_output is not None):
                assertion.update(passed=True, reason_category=None)
            if assertion["name"] == "reactflow_edit_save" and react_receipt is not None:
                passed = react_receipt["result"] == "passed"
                assertion.update(passed=passed, reason_category=None if passed else "not_implemented")
        # A child computes its result before Python-owned provenance checks. Its
        # original receipt stays immutable; only a complete, nonfailed combined
        # result can pass the matrix after the owned cleanup above.
        record["result"] = ("passed" if original_browser["result"] != "failed"
                            and all(item["passed"] is True for item in record["assertions"]) else "failed")
        browser.check_cell(record)
        if released_output is not None:
            # Preserve the exact validated download, never a reserialized workflow.
            raw = provenance.regular_file(released_output).read_bytes()
            require(hashlib.sha256(raw).hexdigest() == evidence["released_document"]["document"]["document_sha256"],
                    "Released export changed before retention")
            cell_root.mkdir(parents=True, exist_ok=True)
            target = cell_root / "released-document.json"
            with os.fdopen(os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600), "wb") as stream:
                stream.write(raw)
        evidence.update(stage="complete", result="passed")
        return record
    except Exception as failure:
        if isinstance(failure, converter_selection.ConverterArchiveRejected):
            evidence["converter_archive_rejection"] = failure.evidence
        if isinstance(failure, browser.BrowserCleanupUnverified):
            cleanup_categories.update(failure.categories)
        evidence["failure_category"] = "execution_or_evidence_failed"
        raise ValueError("Package browser execution or evidence failed") from None
    finally:
        if cleanup_categories:
            evidence["owned_process_cleanup"] = False
            evidence["cleanup_failure_categories"] = sorted(cleanup_categories)
        _write(cell_root / "execution.json", evidence)
        if original_browser is not None:
            _write(cell_root / "browser.json", browser.validate_browser_receipt(original_browser, key))
        if react_receipt is not None:
            _write(cell_root / "react-phase.json", react_phase.validate_react_phase_receipt(
                react_receipt, request, original_browser, evidence["resource_inventory"]["assets"]))


def select_execution_sdk(private: Path) -> str:
    """Pin the reviewed converter SDK independently of the source checkout SDK."""
    policy = json.loads(resources.CONVERTER_POLICY.read_text(encoding="utf-8"))
    sdks = {entry["sdk_version"] for entry in policy["converters"].values()}
    require(len(sdks) == 1, "Browser execution requires one reviewed converter SDK")
    sdk = next(iter(sdks))
    require(isinstance(sdk, str) and re.fullmatch(r"10\.[0-9]+\.[0-9]+", sdk) is not None,
            "Unsupported reviewed execution SDK")
    root = private / "execution-sdk"
    root.mkdir(mode=0o700, exist_ok=False)
    _write(root / "global.json", {"sdk": {"version": sdk, "rollForward": "disable"}})
    actual = subprocess.check_output(["dotnet", "--version"], cwd=root, text=True,
                                     stderr=subprocess.PIPE, timeout=60).strip()
    require(actual == sdk, "Actual execution SDK differs from reviewed converter SDK")
    return sdk


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
        sdk = select_execution_sdk(private)
        return browser.run_matrix(lambda key: execute_cell(key, private=private, retained=retained,
                 verified_root=verified_root, manifest=manifest, manifest_hash=original["verified_artifacts_sha256"], sdk=sdk,
                 fixture_identity=original["browser_execution"]),
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
