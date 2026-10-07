import copy
from contextlib import contextmanager
from dataclasses import replace
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import paired_package_execution as execution
import paired_package_wasm_boot as boot
from test_paired_package_react_phase import phase_fixture
from test_paired_package_released_documents import fixture_identity, write_released_fixture
from test_paired_package_browser_matrix import attach_embedding, attach_native_interop, attach_react_phase, bpmn_proof, clipboard_proof, direct_backend_proof, reopen_row
from test_paired_package_wasm_boot import boot_receipt_fixture


class ExecutionSdkContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()

    def test_probe_uses_private_exact_policy_pin_instead_of_ambient_latest_sdk(self):
        def probe(command, **options):
            self.assertEqual(["dotnet", "--version"], command)
            self.assertEqual(self.root / "execution-sdk", options["cwd"])
            self.assertEqual(60, options["timeout"])
            pin = json.loads((options["cwd"] / "global.json").read_text())
            self.assertEqual({"sdk": {"version": "10.0.300", "rollForward": "disable"}}, pin)
            return "10.0.300\n"
        with patch.object(execution.subprocess, "check_output", side_effect=probe) as command:
            self.assertEqual("10.0.300", execution.select_execution_sdk(self.root))
        command.assert_called_once()

    def test_unreviewed_actual_sdk_is_rejected(self):
        with patch.object(execution.subprocess, "check_output", return_value="10.0.401\n"), \
                self.assertRaisesRegex(ValueError, "differs from reviewed"):
            execution.select_execution_sdk(self.root)

    def test_ambiguous_sdk_policy_fails_before_command_or_pin_creation(self):
        policy = self.root / "policy.json"
        policy.write_text(json.dumps({"converters": {
            "a": {"sdk_version": "10.0.300"}, "b": {"sdk_version": "10.0.401"}}}))
        with patch.object(execution.resources, "CONVERTER_POLICY", policy), \
                patch.object(execution.subprocess, "check_output") as command, \
                self.assertRaisesRegex(ValueError, "one reviewed"):
            execution.select_execution_sdk(self.root)
        command.assert_not_called()
        self.assertFalse((self.root / "execution-sdk").exists())


class ReleasedInputsContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.key = ("3.10.0", "net10.0", "server")
        self.identity = fixture_identity()
        for version in ("3.8.4", "3.9.0"):
            cell = (version, *self.key[1:])
            directory = self.root / "cells" / "-".join(cell)
            path = directory / "released-document.json"
            _, child = write_released_fixture(path, cell)
            evidence = dict(zip(("version", "framework", "host"), cell))
            evidence.update(result="passed", stage="complete", owned_process_cleanup=True,
                projects={"backend": {}}, resource_inventory={"assets": []}, runtime_readiness={"runtime": ".NET 10.0.8"},
                loaded_assemblies={"backend": []}, browser_resources={"requested": []})
            evidence["released_document"] = execution.documents.bind_released_document(path, cell, child, evidence, self.identity)
            (directory / "execution.json").write_text(json.dumps(evidence))
            (directory / "browser.json").write_text(json.dumps(child))

    def test_returns_only_matching_pair_with_exact_source_bindings(self):
        inputs = execution.released_document_inputs(self.key, self.root, self.identity)
        self.assertEqual(["3.8.4", "3.9.0"], [item["binding"]["document"]["source_cell"]["version"] for item in inputs])
        self.assertTrue(all(Path(item["private_path"]).is_file() for item in inputs))
        for key in (("3.10.0", "net9.0", "server"), ("3.10.0", "net10.0", "wasm"), ("3.9.0", "net10.0", "server")):
            with self.subTest(key=key), self.assertRaises((ValueError, RuntimeError, OSError)):
                execution.released_document_inputs(key, self.root, self.identity)

    def test_changed_fixture_child_source_or_missing_document_rejects(self):
        with self.assertRaises(ValueError):
            execution.released_document_inputs(self.key, self.root, {**self.identity, "fixture_source_commit": "c" * 40})
        directory = self.root / "cells/3.8.4-net10.0-server"
        for name, field, value in (("execution", "result", "failed"), ("execution", "projects", {"changed": []}),
                                   ("browser", "browser_version", "150.0")):
            path = directory / (name + ".json")
            raw = path.read_bytes()
            changed = json.loads(raw)
            changed[field] = value
            path.write_text(json.dumps(changed))
            with self.subTest(name=name, field=field), self.assertRaises(ValueError):
                execution.released_document_inputs(self.key, self.root, self.identity)
            path.write_bytes(raw)
        (directory / "released-document.json").unlink()
        with self.assertRaises((ValueError, RuntimeError, OSError)):
            execution.released_document_inputs(self.key, self.root, self.identity)


class ResourceInventoryContracts(unittest.TestCase):
    STYLESHEET = "/_content/Elsa.Studio.Workflows.Designer/designer.css"

    def setUp(self):
        _, expected, _ = boot_receipt_fixture()
        # These tests supply fake build paths; actual bootstrap authority has its own contracts.
        self.bootstrap = {"assets": [row for row in expected if row["owner"] == "platform"], "format": boot.POLICY["format"]}

    def test_client_inventory_uses_selected_client_manifest_and_keeps_both_authorities(self):
        for version in ("3.9.0", "3.10.0"):
            for host in ("wasm", "hosted-wasm", "custom-elements"):
                prefix = "compat" if host == "hosted-wasm" else ""
                layout = SimpleNamespace(request=execution.hosts.CellRequest(host, "net10.0", version, route_prefix=prefix),
                    packages_root=Path("/owned/packages"),
                    project_paths={name: Path("/owned") / name / (name + ".csproj") for name in execution.hosts.HOST_NAMES})
                static = {"assets": [{"path": "/script.js", "owner": "package", "required": True},
                                      {"path": ("/" + prefix if prefix else "") + self.STYLESHEET,
                                       "owner": "package", "required": True}],
                          "static_asset_manifest_sha256": "a" * 64}
                managed = {"assets": [{"path": "/managed.wasm"}], "static_asset_manifest_sha256": "b" * 64,
                           "package_runtime_count": 1}
                candidate = version == "3.10.0"
                static_owner = execution.resources if candidate else execution.baseline_resources
                static_name = "derive_candidate_resources" if candidate else "derive_baseline_resources"
                managed_name = "derive_candidate_wasm_resources" if candidate else "derive_baseline_wasm_resources"
                with self.subTest(version=version, host=host), patch.object(static_owner, static_name, return_value=static), \
                        patch.object(execution.wasm_resources, managed_name, return_value=managed) as derive, \
                        patch.object(boot, "derive_boot_resources", return_value=self.bootstrap) as derive_boot:
                    converter = {"task_sha256": "c" * 64}
                    result = execution._resource_inventory(layout, Path("/verified"), "d" * 64, converter=converter)
                    expected_static = copy.deepcopy(static["assets"])
                    next(asset for asset in expected_static if asset["path"].endswith(self.STYLESHEET))["required"] = False
                    expected_boot = self.bootstrap["assets"] if host == "wasm" else []
                    self.assertEqual(expected_static + managed["assets"] + expected_boot, result["assets"])
                    self.assertEqual("a" * 64, result["static_asset_manifest_sha256"])
                    self.assertEqual("b" * 64, result["managed_resources"]["static_asset_manifest_sha256"])
                    client = layout.project_paths["wasm" if host == "hosted-wasm" else host]
                    self.assertEqual(client, derive.call_args.args[1])
                    self.assertEqual(client.parent / "obj/Release/net10.0/staticwebassets.build.json", derive.call_args.args[2])
                    self.assertEqual(converter, derive.call_args.kwargs["converter"])
                    self.assertEqual("/compat" if prefix else "", derive.call_args.kwargs["route_prefix"])
                    self.assertEqual(host, result["host_network_policy"]["host"])
                    if host == "wasm":
                        derive_boot.assert_called_once_with(layout, client, derive.call_args.args[2], managed)
                        self.assertEqual({"format": boot.POLICY["format"]}, result["bootstrap_resources"])
                    else:
                        derive_boot.assert_not_called()
                        self.assertNotIn("bootstrap_resources", result)
                    self.assertEqual(("/" + prefix if prefix else "") + self.STYLESHEET,
                                     result["host_network_policy"]["stylesheet"]["path"])
                    self.assertFalse(result["host_network_policy"]["stylesheet"]["required"])
                    with self.assertRaises(ValueError):
                        execution._resource_inventory(layout, Path("/verified"), "d" * 64)
                    managed["assets"] = static["assets"]
                    with self.assertRaises(ValueError):
                        execution._resource_inventory(layout, Path("/verified"), "d" * 64, converter=converter)

    def test_host_policy_changes_only_standalone_stylesheet_request_requirement(self):
        policy = json.loads(execution.resources.CONVERTER_POLICY.with_name("coverage-policy.json").read_text())
        package_assets = policy["required_browser_assets"]
        stylesheet = self.STYLESHEET
        baseline_requests = {
            "/_content/Elsa.Studio.Workflows.Designer/designer.entry.js",
            stylesheet,
            "/_content/Elsa.Studio.DomInterop/dom.entry.js",
        }
        for version in ("3.9.0", "3.10.0"):
            for host in execution.browser.HOSTS:
                with self.subTest(version=version, host=host):
                    prefix = "compat" if host == "hosted-wasm" else ""
                    route_prefix = "/" + prefix if prefix else ""
                    layout = SimpleNamespace(
                        request=execution.hosts.CellRequest(host, "net10.0", version, route_prefix=prefix),
                        packages_root=Path("/owned/packages"),
                        project_paths={name: Path("/owned") / name / (name + ".csproj")
                                       for name in execution.hosts.HOST_NAMES})
                    initially_required = set(package_assets if version == "3.10.0" else baseline_requests)
                    extra_path = route_prefix + "/_content/Elsa.Studio.Shell/shell.js"
                    static = {
                        "assets": [
                            {"path": route_prefix + path, "owner": "package", "required": path in initially_required}
                            for path in package_assets
                        ] + [{"path": extra_path, "owner": "package", "required": False}],
                        "static_asset_manifest_sha256": "a" * 64,
                    }
                    managed_assets = [
                        {"path": "/_framework/managed-one.wasm", "owner": "package", "required": True},
                        {"path": "/_framework/managed-two.wasm", "owner": "package", "required": False},
                    ]
                    managed = {"assets": managed_assets, "static_asset_manifest_sha256": "b" * 64,
                               "package_runtime_count": 2}
                    candidate = version == "3.10.0"
                    static_owner = execution.resources if candidate else execution.baseline_resources
                    static_name = "derive_candidate_resources" if candidate else "derive_baseline_resources"
                    managed_name = "derive_candidate_wasm_resources" if candidate else "derive_baseline_wasm_resources"
                    original_flags = {asset["path"]: asset["required"] for asset in static["assets"]}
                    converter = {"task_sha256": "c" * 64}
                    with patch.object(static_owner, static_name, return_value=static), \
                            patch.object(boot, "derive_boot_resources", return_value=self.bootstrap):
                        if host == "server":
                            result = execution._resource_inventory(layout, Path("/verified"), "d" * 64)
                        else:
                            with patch.object(execution.wasm_resources, managed_name, return_value=managed):
                                result = execution._resource_inventory(layout, Path("/verified"), "d" * 64,
                                                                       converter=converter)

                    expected_flags = dict(original_flags)
                    stylesheet_path = route_prefix + stylesheet
                    expected_flags[stylesheet_path] = host == "server"
                    expected_flags.update({asset["path"]: asset["required"] for asset in managed_assets}
                                          if host != "server" else {})
                    if host == "wasm":
                        expected_flags.update({asset["path"]: asset["required"] for asset in self.bootstrap["assets"]})
                    self.assertEqual(expected_flags,
                                     {asset["path"]: asset["required"] for asset in result["assets"]})
                    if version == "3.10.0":
                        self.assertTrue(all(expected_flags[route_prefix + path]
                                            for path in package_assets if path != stylesheet))
                    else:
                        self.assertTrue(expected_flags[route_prefix + "/_content/Elsa.Studio.Workflows.Designer/designer.entry.js"])
                        self.assertTrue(expected_flags[route_prefix + "/_content/Elsa.Studio.DomInterop/dom.entry.js"])
                    network_policy = result["host_network_policy"]
                    self.assertEqual(host, network_policy["host"])
                    self.assertEqual(stylesheet_path, network_policy["stylesheet"]["path"])
                    self.assertEqual(host == "server", network_policy["stylesheet"]["required"])
                    self.assertEqual(["server"], network_policy["stylesheet"]["required_hosts"])
                    self.assertRegex(network_policy["coverage_policy_sha256"], r"^[0-9a-f]{64}$")

    def test_standalone_stylesheet_remains_mandatory_materialization(self):
        policy = json.loads(execution.resources.CONVERTER_POLICY.with_name("coverage-policy.json").read_text())
        required = policy["required_browser_assets"]
        self.assertEqual(6, len(required))
        self.assertIn(self.STYLESHEET, required)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            cache = root / "cache"
            build_manifest = root / "build.json"
            contents = {}
            rows = []
            package_ids = sorted({path.split("/")[2] for path in required})
            for path in required:
                package_id = path.split("/")[2]
                relative = path.removeprefix("/_content/" + package_id + "/")
                member = "staticwebassets/" + relative
                identity = cache / package_id.lower() / "3.10.0" / member
                identity.parent.mkdir(parents=True, exist_ok=True)
                content = relative.encode()
                identity.write_bytes(content)
                contents[(package_id, member)] = content
                rows.append({"SourceId": package_id, "SourceType": "Package", "BasePath": "_content/" + package_id,
                             "RelativePath": relative, "Identity": str(identity), "AssetRole": "Primary",
                             "FileLength": len(content)})
            build_manifest.write_text(json.dumps({"Assets": rows}))
            by_id = {package_id.casefold(): {"id": package_id} for package_id in package_ids}
            mandatory_calls = []

            def package_member(package, member, mandatory):
                mandatory_calls.append((package["id"], member, mandatory))
                return contents[(package["id"], member)]

            execution.resources._derive_package_resources(build_manifest, cache, "3.10.0", by_id, package_member,
                                                            route_prefix="")
            stylesheet_member = "staticwebassets/designer.css"
            self.assertIn(("Elsa.Studio.Workflows.Designer", stylesheet_member, True), mandatory_calls)
            without_stylesheet = [row for row in rows if row["RelativePath"] != "designer.css"]
            build_manifest.write_text(json.dumps({"Assets": without_stylesheet}))
            with self.assertRaisesRegex(ValueError, "Missing mandatory materialized"):
                execution.resources._derive_package_resources(build_manifest, cache, "3.10.0", by_id, package_member,
                                                                route_prefix="")


class ExecutionContracts(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name).resolve()
        self.key = ("3.10.0", "net10.0", "server")
        self.events = []
        self.record = {"version": self.key[0], "framework": self.key[1], "host": self.key[2],
                       "result": "passed", "resources": [],
                       "proof": {"baseline_reopens": [reopen_row(version) for version in execution.documents.TOOL_VERSIONS],
                                 "instance_id_sha256": "a" * 64, "value_sha256": "b" * 64,
                                 "bpmn_roundtrip": bpmn_proof(), "clipboard": clipboard_proof("a" * 64, "b" * 64)},
                       "browser_version": "149.0.7827.55",
                       "assertions": [{"name": name, "passed": True, "reason_category": None}
                                      for name in execution.browser.required_assertions(dict(zip(("version", "framework", "host"), self.key)))]}
        attach_native_interop(self.record)
        self.record["result"] = "incomplete"
        next(item for item in self.record["assertions"] if item["name"] == "reactflow_edit_save").update(
            passed=False, reason_category="not_implemented")
        _, _, self.assets, _ = phase_fixture()
        self.layout = SimpleNamespace(request=execution.hosts.CellRequest("server", "net10.0", "3.10.0"),
                                      project_paths={"backend": self.root / "backend.csproj", "server": self.root / "server.csproj"})

    def patch(self, owner, name, **kwargs):
        context = patch.object(owner, name, **kwargs)
        self.addCleanup(context.stop)
        return context.start()

    def readiness(self, request=None):
        request = request or self.layout.request
        features = ["Elsa.Identity", "Elsa.DefaultAuthentication", "Elsa.WorkflowManagement", "Elsa.WorkflowRuntime",
                    "Elsa.WorkflowsApi", "Elsa.EFCoreWorkflowDefinitionPersistence", "Elsa.EFCoreWorkflowInstancePersistence",
                    "Elsa.EFCoreWorkflowRuntimePersistence",
                    "Elsa.JavaScript"]
        if request.version != "3.8.4":
            features.extend(["Elsa.Bpmn", "Elsa.BpmnInterchange"])
        if "workflow-contexts" in request.backend_features:
            features.append("Elsa.WorkflowContexts")
        if "secrets" in request.backend_features:
            features.extend(["Elsa.Secrets", "Elsa.EFCoreSecretsPersistence"])
        return {"schema": 1, "framework": ".NETCoreApp,Version=v" + request.framework.removeprefix("net"),
                "runtime": ".NET " + request.framework.removeprefix("net") + ".8", "auth_mode": "ElsaIdentity",
                "permission_profile": request.permission_profile, "permission_grants": list(execution.hosts.permission_grants(request)),
                "workflow_contexts_enabled": "workflow-contexts" in request.backend_features,
                "secrets_enabled": "secrets" in request.backend_features, "features": features}

    def pipeline(self):
        def materialize(request, group, **kwargs):
            self.events.append(("materialize", group, kwargs["packages_root"]))
            self.layout.request = request
            return self.layout
        def validate(project):
            self.events.append(("validate", project))
            return {"project_assets_sha256": "a" * 64}
        @contextmanager
        def pair(layout, *, validate_project):
            self.events.append(("start",))
            for project in layout.project_paths.values():
                validate_project(project)
            try:
                yield SimpleNamespace(request=layout.request, password="PRIVATE-MUST-NOT-BE-RETAINED",
                    backend_url="http://127.0.0.1:4000/elsa/api", studio_url="http://127.0.0.1:4001/",
                    username="private-user", safe_ids={"definition_name": "private-name"}, process_ids=(42, 43))
            finally:
                self.events.append(("stop",))
        @contextmanager
        def phases(layout, **options):
            @contextmanager
            def phase(mode):
                self.events.append(("phase", mode))
                with execution.hosts.start_pair(layout, **options) as handle:
                    handle.request = replace(layout.request, designer_mode=mode)
                    yield handle
            yield SimpleNamespace(phase=phase)
        self.designer_owner = self.patch(execution.hosts, "start_designer_phases", side_effect=phases)
        self.patch(execution, "evidence_gaps", return_value=[])
        self.patch(execution, "released_document_inputs", return_value=[])
        self.patch(execution.packages, "_validated_manifest", return_value=("3.10.0", "b" * 40, {"elsa": {"id": "Elsa"}}, {}, []))
        self.patch(execution.packages, "render_nuget_config", return_value="<configuration />")
        self.patch(execution.hosts, "materialize", side_effect=materialize)
        self.patch(execution.hosts, "build", side_effect=lambda _, **_options: self.events.append(("build",)) or [])
        self.patch(execution, "_project_validator", return_value=validate)
        self.patch(execution, "_resource_inventory", side_effect=lambda *_: self.events.append(("resources",)) or {"assets": self.assets})
        self.patch(execution.hosts, "start_pair", side_effect=pair)
        self.patch(execution, "_json_request", side_effect=lambda *_args, **_kwargs:
                   self.events.append(("ready",)) or self.readiness())
        def run_browser(_handle, request, _resources, **_options):
            self.events.append(("browser",))
            record = copy.deepcopy(self.record)
            record.update(version=request.version, framework=request.framework, host=request.host)
            for row in record["proof"]["baseline_reopens"]:
                row["source_cell"].update(framework=request.framework, host=request.host)
            return record
        browser = self.patch(execution.browser, "run_browser", side_effect=run_browser)
        def run_react(_handle, request, _assets, original):
            self.events.append(("react_browser",))
            _, _, _, receipt = phase_fixture(request.host, request.framework)
            receipt["source_browser_sha256"] = execution.react_phase.browser_receipt_sha256(original)
            receipt["hashes"].update({name: original["proof"][parent]
                                     for name, parent in execution.react_phase.BEFORE_HASHES.items()})
            return receipt
        self.react_browser = self.patch(execution.react_phase, "run_react_phase", side_effect=run_react)
        self.patch(execution, "_observe_loaded", side_effect=lambda *_: self.events.append(("observe",)) or {})
        self.patch(execution, "_verify_loaded", side_effect=lambda *_: self.events.append(("loaded",)) or {})
        self.patch(execution.resources, "verify_browser_resources", side_effect=lambda *_args, **_kw: self.events.append(("resource_check",)) or {})
        return browser

    def execute(self, key=None):
        return execution.execute_cell(key or self.key, private=self.root / "private", retained=self.root / "retained",
                  verified_root=self.root / "candidate", manifest={}, manifest_hash="a" * 64, sdk="10.0.300",
                  fixture_identity=fixture_identity())

    def released_pipeline(self):
        self.pipeline()
        def download(_handle, request, _resources, *, released_document_output):
            self.download, self.child = write_released_fixture(released_document_output,
                (request.version, request.framework, request.host))
            return self.child
        self.patch(execution.browser, "run_browser", side_effect=download)
        self.patch(execution, "_verify_loaded", return_value={"backend": {"package_assemblies": []}})
        self.patch(execution.resources, "verify_browser_resources", return_value={"verified_assets": []})

    def test_released_export_retains_exact_download_after_all_source_checks(self):
        self.released_pipeline()
        for version in ("3.8.4", "3.9.0"):
            key = (version, "net10.0", "server")
            self.assertEqual("passed", self.execute(key)["result"])
            root = self.root / "retained/cells" / "-".join(key)
            path = root / "released-document.json"
            self.assertEqual(self.download, path.read_bytes())
            self.assertEqual(0o600, path.stat().st_mode & 0o777)
            self.assertEqual(self.child, json.loads((root / "browser.json").read_text()))
            evidence = json.loads((root / "execution.json").read_text())
            self.assertEqual(evidence["released_document"], execution.documents.bind_released_document(
                path, key, self.child, evidence, fixture_identity()))

    def test_failed_source_check_never_retains_export(self):
        self.released_pipeline()
        self.patch(execution.resources, "verify_browser_resources", side_effect=ValueError("rejected"))
        with self.assertRaises(ValueError):
            self.execute(("3.9.0", "net10.0", "server"))
        self.assertFalse(list((self.root / "retained").rglob("released-document.json")))
        self.assertTrue(list((self.root / "private").rglob("released-document.json")))

    def test_missing_fixture_identity_never_promotes_export(self):
        self.released_pipeline()
        with self.assertRaises(ValueError):
            execution.execute_cell(("3.9.0", "net10.0", "server"), private=self.root / "private",
                retained=self.root / "retained", verified_root=self.root / "candidate", manifest={},
                manifest_hash="a" * 64, sdk="10.0.300")
        self.assertFalse(list((self.root / "retained").rglob("released-document.json")))

    def test_missing_released_inputs_fail_before_candidate_build_or_runtime(self):
        run_browser = self.pipeline()
        self.patch(execution, "released_document_inputs", side_effect=ValueError("Missing source evidence"))
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual([], self.events)
        run_browser.assert_not_called()
        self.assertEqual("released_inputs", self.receipt()["stage"])
        self.assertEqual("failed", self.receipt()["result"])

    def receipt(self):
        return json.loads((self.root / "retained/cells/3.10.0-net10.0-server/execution.json").read_text())

    def test_full_selection_has_36_and_four_hosts_share_only_version_framework(self):
        selected = execution.selected_cells(None)
        self.assertEqual(execution.browser.MATRIX, set(selected))
        self.assertEqual(36, len(selected))
        self.assertEqual(4, len([k for k in selected if k[:2] == selected[0][:2]]))
        self.assertEqual([self.key], execution.selected_cells(",".join(self.key)))
        self.assertEqual([self.key], execution.selected_cells("/".join(self.key)))
        for invalid in ("", "3.10.0,net10.0,server,extra", "3.10.0,net10.0,unknown"):
            with self.subTest(invalid=invalid), self.assertRaises(ValueError):
                execution.selected_cells(invalid)

    def test_production_feature_policy_keeps_secrets_mandatory_for_all_candidate_cells(self):
        candidates, baselines = [], []
        for key in execution.selected_cells(None):
            request = execution.cell_request(key)
            self.assertEqual(key, (request.version, request.framework, request.host))
            self.assertEqual("full", request.permission_profile)
            if request.version == "3.10.0":
                candidates.append(key)
                self.assertEqual(("workflow-contexts", "secrets"), request.backend_features)
            else:
                baselines.append(key)
                self.assertEqual(("workflow-contexts",), request.backend_features)
        self.assertEqual(12, len(candidates))
        self.assertEqual(24, len(baselines))
        with self.assertRaisesRegex(ValueError, "Invalid package browser cell"):
            execution.cell_request(("unreviewed", "net10.0", "server"))

    def test_requested_features_are_retained_even_when_evidence_preflight_fails(self):
        self.patch(execution, "evidence_gaps", return_value=["unavailable_evidence"])
        for version in execution.browser.VERSIONS:
            with self.subTest(version=version), self.assertRaises(ValueError):
                self.execute((version, "net10.0", "server"))
            receipt = json.loads((self.root / "retained" / "cells" / f"{version}-net10.0-server" / "execution.json").read_text())
            expected = ["workflow-contexts", "secrets"] if version == "3.10.0" else ["workflow-contexts"]
            self.assertEqual(expected, receipt["requested_backend_features"])
            self.assertEqual("full", receipt["permission_profile"])
            self.assertEqual("candidate_representative_features" if version == "3.10.0" else
                             "released_shell_editor_export_contexts_only", receipt["feature_policy"])
            self.assertEqual("failed", receipt["result"])

    def test_valid_pipeline_verifies_before_start_and_retains_no_private_handle(self):
        self.pipeline()
        self.assertEqual("passed", self.execute()["result"])
        stages = [event[0] for event in self.events]
        self.assertLess(stages.index("validate"), stages.index("start"))
        self.assertLess(stages.index("start"), stages.index("ready"))
        self.assertLess(stages.index("ready"), stages.index("browser"))
        self.assertLess(stages.index("loaded"), stages.index("stop"))
        self.assertLess(stages.index("stop"), stages.index("resource_check"))
        self.assertTrue(self.receipt()["owned_process_cleanup"])
        self.assertEqual(sorted(self.readiness()["features"]), self.receipt()["runtime_readiness"]["features"])
        for path in (self.root / "retained").rglob("*.json"):
            self.assertNotIn("PRIVATE", path.read_text())

    def test_readiness_validates_profiles_features_and_frameworks_without_pinning_entire_catalog(self):
        for version in execution.browser.VERSIONS:
            for framework in execution.browser.FRAMEWORKS:
                for profile in execution.hosts.PERMISSION_PROFILES:
                    with self.subTest(version=version, framework=framework, profile=profile):
                        request = execution.hosts.CellRequest("server", framework, version,
                            backend_features=execution.cell_request((version, framework, "server")).backend_features,
                            permission_profile=profile)
                        value = self.readiness(request)
                        value["features"].append("Elsa.OtherInstalledFeature")
                        actual = execution.verify_runtime_readiness(value, request)
                        self.assertEqual(sorted(value["features"]), actual["features"])
                        self.assertEqual(value["permission_grants"], actual["permission_grants"])

    def test_versioned_bpmn_registration_must_match_available_backend_packages(self):
        bpmn = {"Elsa.Bpmn", "Elsa.BpmnInterchange"}
        for version in execution.browser.VERSIONS:
            request = execution.cell_request((version, "net10.0", "server"))
            original = self.readiness(request)
            for name in sorted(bpmn):
                changed = original | {"features": original["features"] + [name] if version == "3.8.4" else
                                      [feature for feature in original["features"] if feature != name]}
                with self.subTest(version=version, name=name), self.assertRaisesRegex(ValueError, "BPMN registration"):
                    execution.verify_runtime_readiness(changed, request)

    def test_observed_39_readiness_shape_accepts_specific_definition_and_instance_persistence_features(self):
        # Sanitized actual fixture 117a68 metadata; original observation hash:
        # 25ca09d6715e1f4ce6ad8c8c57926fea1542b433809cfb9cdb9e385265182d5b.
        observed = {"schema": 1, "framework": ".NETCoreApp,Version=v10.0", "runtime": ".NET 10.0.8",
                    "auth_mode": "ElsaIdentity", "permission_profile": "full", "permission_grants": ["*"],
                    "workflow_contexts_enabled": True, "secrets_enabled": False,
                    "features": ["Elsa." + name for name in (
                        "Mediator SystemClock Expressions DefaultFormatters Multitenancy CommitStrategies Workflows Flowchart "
                        "WorkflowRuntime DefaultWorkflowRuntime StringCompression MemoryCache WorkflowDefinitions WorkflowInstances "
                        "WorkflowManagement Elsa App Identity DefaultAuthentication WorkflowManagementPersistence "
                        "EFCoreWorkflowDefinitionPersistence EFCoreWorkflowInstancePersistence EFCoreWorkflowRuntimePersistence "
                        "KeyValue WorkflowsApi SasTokens JavaScript BpmnInterchange Bpmn WorkflowContexts WorkflowContextsJavaScript").split()]}
        request = execution.cell_request(("3.9.0", "net10.0", "server"))
        actual = execution.verify_runtime_readiness(observed, request)
        self.assertEqual(sorted(observed["features"]), actual["features"])
        self.assertFalse(actual["secrets_enabled"])
        self.assertNotIn("Elsa.Secrets", actual["features"])
        self.assertNotIn("Elsa.EFCoreWorkflowManagementPersistence", actual["features"])

    def test_runtime_mismatches_fail_before_browser_and_still_clean_up_without_retaining_raw_response(self):
        child = self.pipeline()
        original = self.readiness()
        mutations = [
            {"permission_profile": "denied"}, {"permission_grants": ["*"] + ["PRIVATE-GRANT"]},
            {"permission_grants": []}, {"workflow_contexts_enabled": False}, {"secrets_enabled": False},
            {"framework": ".NETCoreApp,Version=v9.0"}, {"runtime": "PRIVATE-RUNTIME"}, {"schema": True},
            {"auth_mode": "Unauthenticated"}, {"features": [name for name in original["features"] if name != "Elsa.Secrets"]},
            {"features": [name for name in original["features"] if name != "Elsa.EFCoreSecretsPersistence"]},
            {"features": [name for name in original["features"] if name != "Elsa.WorkflowsApi"]},
            {"features": [name for name in original["features"] if name != "Elsa.Bpmn"]},
            {"features": [name for name in original["features"] if name != "Elsa.BpmnInterchange"]},
            {"features": original["features"] + ["/PRIVATE/PATH"]}, {"encryption_key": "PRIVATE-KEY"}]
        for index, mutation in enumerate(mutations):
            with self.subTest(mutation=list(mutation)):
                self.patch(execution, "_json_request", return_value=original | mutation)
                retained = self.root / f"failure-{index}"
                with self.assertRaises(ValueError):
                    execution.execute_cell(self.key, private=self.root / "private", retained=retained,
                        verified_root=self.root / "candidate", manifest={}, manifest_hash="a" * 64, sdk="10.0.300")
                receipt = json.loads((retained / "cells/3.10.0-net10.0-server/execution.json").read_text())
                self.assertEqual("runtime_readiness", receipt["stage"])
                self.assertTrue(receipt["owned_process_cleanup"])
                self.assertNotIn("runtime_readiness", receipt)
                self.assertNotIn("PRIVATE", json.dumps(receipt))
        child.assert_not_called()

    def test_unrequested_optional_features_and_absent_registration_are_rejected(self):
        baseline = execution.cell_request(("3.9.0", "net10.0", "server"))
        original = self.readiness(baseline)
        for mutation in ({"secrets_enabled": True}, {"secrets_enabled": 0},
                         {"features": original["features"] + ["Elsa.Secrets"]},
                         {"features": original["features"] + ["Elsa.EFCoreSecretsPersistence"]},
                         {"features": [name for name in original["features"] if name != "Elsa.WorkflowContexts"]}):
            with self.subTest(mutation=list(mutation)), self.assertRaises(ValueError):
                execution.verify_runtime_readiness(original | mutation, baseline)

    def test_browser_exception_stops_pair_and_does_not_retain_raw_error(self):
        child = self.pipeline()
        child.side_effect = RuntimeError("PRIVATE-BROWSER-ERROR")
        with self.assertRaisesRegex(ValueError, "execution or evidence"):
            self.execute()
        self.assertEqual("stop", self.events[-1][0])
        self.assertTrue(self.receipt()["owned_process_cleanup"])
        self.assertFalse((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").exists())
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_incomplete_browser_assertions_remain_failed_with_safe_receipt(self):
        self.pipeline()
        self.record["result"] = "incomplete"
        next(item for item in self.record["assertions"] if item["name"] == "x6_edit_save_reload").update(passed=False, reason_category="not_implemented")
        with self.assertRaises(ValueError):
            self.execute()
        record = json.loads((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").read_text())
        self.assertEqual("incomplete", record["result"])
        self.assertFalse(next(item for item in record["assertions"] if item["name"] == "x6_edit_save_reload")["passed"])
        self.assertEqual("react_source_binding", self.receipt()["stage"])
        self.react_browser.assert_not_called()
        self.assertEqual("failed", self.receipt()["result"])

    def test_verified_python_assertions_complete_cell_without_mutating_child_receipt(self):
        self.pipeline()
        self.record["result"] = "incomplete"
        for assertion in self.record["assertions"]:
            if assertion["name"] in {"package_provenance", "browser_resources"}:
                assertion.update(passed=False, reason_category="not_implemented")
        original = copy.deepcopy(self.record)
        combined = self.execute()
        self.assertEqual("passed", combined["result"])
        execution.browser.check_cell(combined)
        retained = json.loads((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").read_text())
        self.assertEqual(original, retained)
        self.assertEqual(original, self.record)
        self.assertTrue(self.receipt()["owned_process_cleanup"])
        cell = self.root / "retained/cells/3.10.0-net10.0-server"
        self.assertEqual((json.dumps(original, indent=2, sort_keys=True) + "\n").encode(),
                         (cell / "browser.json").read_bytes())
        phase = json.loads((cell / "react-phase.json").read_text())
        self.assertEqual(execution.react_phase.browser_receipt_sha256(original), phase["source_browser_sha256"])
        self.assertEqual(original["resources"] + phase["resources"], combined["resources"])
        self.assertEqual(self.receipt()["react_phase"], combined["proof"]["reactflow"])
        self.assertEqual([("phase", "x6"), ("phase", "react-flow")],
                         [event for event in self.events if event[0] == "phase"])

    def test_react_failure_retains_original_but_never_promotes_combined_claim(self):
        self.pipeline()
        successful = self.react_browser.side_effect
        def failed(*args):
            phase = successful(*args)
            phase.update(result="failed", failure_category=execution.react_phase.FAILURE)
            phase["checks"]["identity_preserved"] = False
            return phase
        self.react_browser.side_effect = failed
        with self.assertRaises(ValueError):
            self.execute()
        cell = self.root / "retained/cells/3.10.0-net10.0-server"
        self.assertEqual(self.record, json.loads((cell / "browser.json").read_text()))
        self.assertEqual("failed", json.loads((cell / "react-phase.json").read_text())["result"])
        self.assertEqual("failed", self.receipt()["result"])

    def test_react_cleanup_uncertainty_cannot_be_overwritten_by_host_cleanup(self):
        self.pipeline()
        self.react_browser.side_effect = execution.browser.BrowserCleanupUnverified("PRIVATE", categories={"inventory"})
        with self.assertRaises(ValueError):
            self.execute()
        self.assertFalse(self.receipt()["owned_process_cleanup"])
        self.assertEqual(["inventory"], self.receipt()["cleanup_failure_categories"])
        self.assertEqual("react_browser_execution", self.receipt()["stage"])
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))
        self.assertEqual(self.record, json.loads((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").read_text()))

    def test_react_readiness_change_stops_before_second_browser(self):
        self.pipeline()
        self.patch(execution, "_observe_ready", side_effect=[self.readiness(), {**self.readiness(), "runtime": ".NET 10.0.9"}])
        with self.assertRaises(ValueError):
            self.execute()
        self.react_browser.assert_not_called()
        self.assertEqual("react_runtime", self.receipt()["stage"])
        self.assertNotIn("react_runtime_continuity", self.receipt())

    def test_changed_backend_identity_prevents_react_browser_and_retains_no_private_values(self):
        self.pipeline()
        original_owner = self.designer_owner.side_effect
        @contextmanager
        def changed_owner(*args, **options):
            with original_owner(*args, **options) as owner:
                @contextmanager
                def phase(mode):
                    with owner.phase(mode) as handle:
                        if mode == "react-flow":
                            handle.process_ids = (99, 100)
                            handle.password = "PRIVATE-CHANGED"
                        yield handle
                yield SimpleNamespace(phase=phase)
        self.designer_owner.side_effect = changed_owner
        with self.assertRaises(ValueError):
            self.execute()
        self.react_browser.assert_not_called()
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))
        self.assertNotIn("react_runtime_continuity", self.receipt())

    def test_invalid_react_private_fields_are_not_retained(self):
        self.pipeline()
        successful = self.react_browser.side_effect
        self.react_browser.side_effect = lambda *args: dict(successful(*args), password="PRIVATE")
        with self.assertRaises(ValueError):
            self.execute()
        self.assertNotIn("react_phase", self.receipt())
        self.assertFalse((self.root / "retained/cells/3.10.0-net10.0-server/react-phase.json").exists())
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_failed_child_cannot_be_promoted_even_with_all_true_assertions(self):
        self.pipeline()
        self.record["result"] = "failed"
        self.record["failure_category"] = "browser_execution_or_validation_failed"
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual("failed", self.receipt()["result"])

    def test_loaded_evidence_failure_still_stops_owned_pair(self):
        self.pipeline()
        self.patch(execution, "_verify_loaded", side_effect=RuntimeError("PRIVATE-PATH"))
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual("stop", self.events[-1][0])
        self.assertEqual("loaded_assemblies", self.receipt()["stage"])
        self.assertTrue(self.receipt()["owned_process_cleanup"])
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_unverified_browser_cleanup_is_not_overwritten_by_successful_host_cleanup(self):
        run_browser = self.pipeline()
        run_browser.side_effect = execution.browser.BrowserCleanupUnverified("PRIVATE-CLEANUP-ERROR")
        with self.assertRaises(ValueError):
            self.execute()
        receipt = self.receipt()
        self.assertEqual("stop", self.events[-1][0])
        self.assertFalse(receipt["owned_process_cleanup"])
        self.assertEqual("failed", receipt["result"])
        self.assertEqual("browser_execution", receipt["stage"])
        self.assertNotIn("PRIVATE", json.dumps(receipt))

    def test_cleanup_failure_never_claims_successful_owned_cleanup(self):
        self.pipeline()
        @contextmanager
        def broken_cleanup(_layout, **_kwargs):
            yield SimpleNamespace()
            raise RuntimeError("PRIVATE-CLEANUP-ERROR")
        self.patch(execution.hosts, "start_pair", side_effect=broken_cleanup)
        with self.assertRaises(ValueError):
            self.execute()
        self.assertNotIn("owned_process_cleanup", self.receipt())
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_host_cleanup_diagnostics_survive_the_outer_owner_boundary(self):
        self.pipeline()
        @contextmanager
        def broken_owner(*_args, **_kwargs):
            raise execution.browser.BrowserCleanupUnverified("PRIVATE", categories={"exit", "reap"})
            yield
        self.designer_owner.side_effect = broken_owner
        with self.assertRaises(ValueError):
            self.execute()
        self.assertFalse(self.receipt()["owned_process_cleanup"])
        self.assertEqual(["exit", "reap"], self.receipt()["cleanup_failure_categories"])
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_resource_failure_retains_safe_browser_without_granting_assertions(self):
        self.pipeline()
        next(item for item in self.record["assertions"] if item["name"] == "browser_resources").update(passed=False, reason_category="not_implemented")
        def reject_observed(_assets, observed, **_options):
            if observed:
                raise RuntimeError("PRIVATE-RESOURCE-PATH")
            return {}
        self.patch(execution.resources, "verify_browser_resources", side_effect=reject_observed)
        with self.assertRaises(ValueError):
            self.execute()
        record = json.loads((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").read_text())
        self.assertFalse(next(item for item in record["assertions"] if item["name"] == "browser_resources")["passed"])
        self.assertEqual("browser_resources", self.receipt()["stage"])

    def test_unavailable_evidence_fails_before_materialize_or_build(self):
        with patch.object(execution.hosts, "materialize") as materialize, \
                patch.object(execution.converter_selection, "prepare_decoder", None):
            with self.assertRaises(ValueError):
                self.execute(("3.10.0", "net10.0", "wasm"))
            materialize.assert_not_called()
        receipt = json.loads((self.root / "retained/cells/3.10.0-net10.0-wasm/execution.json").read_text())
        self.assertEqual(["wasm_converter_selection"], receipt["missing_evidence"])

    def test_converter_capture_failure_stops_before_host_start(self):
        self.pipeline()
        self.patch(execution.hosts, "isolated_environment", return_value={})
        self.patch(execution.converter_selection, "prepare_decoder", return_value=self.root / "Decoder.dll")
        build = self.patch(execution.hosts, "build", side_effect=ValueError("PRIVATE-CONVERTER-ERROR"))
        with self.assertRaises(ValueError):
            self.execute(("3.10.0", "net10.0", "wasm"))
        self.assertEqual(self.root / "Decoder.dll", build.call_args.kwargs["converter_decoder"])
        self.assertFalse(any(event[0] == "start" for event in self.events))
        receipt = (self.root / "retained/cells/3.10.0-net10.0-wasm/execution.json").read_text()
        self.assertEqual("build", json.loads(receipt)["stage"])
        self.assertNotIn("PRIVATE", receipt)

    def test_wasm_inventory_receives_only_the_actual_build_selection(self):
        self.pipeline()
        self.patch(execution.hosts, "isolated_environment", return_value={})
        self.patch(execution.converter_selection, "prepare_decoder", return_value=self.root / "Decoder.dll")
        selection = {"converter": {"task_sha256": "a" * 64}}
        self.patch(execution.hosts, "build", return_value=[{"converter_selection": selection}])
        self.patch(execution, "_command_receipts", return_value=[])
        # Stop at the boundary after observing the selected build tuple; a mock
        # cannot certify real WASM runtime/browser behavior.
        inventory = self.patch(execution, "_resource_inventory", side_effect=ValueError("stop"))
        with self.assertRaises(ValueError):
            self.execute(("3.10.0", "net10.0", "wasm"))
        self.assertEqual(selection["converter"], inventory.call_args.kwargs["converter"])
        receipt = json.loads((self.root / "retained/cells/3.10.0-net10.0-wasm/execution.json").read_text())
        self.assertEqual(selection, receipt["converter_selection"])
        self.assertFalse(any(event[0] == "start" for event in self.events))

    def test_failed_build_retains_only_last_trusted_operation(self):
        self.pipeline()
        def fail(_layout, *, report_operation, **_options):
            report_operation("backend", "restore")
            report_operation("converter", "binding_validation")
            raise ValueError("PRIVATE-PATH-TOKEN-LOG")
        self.patch(execution.hosts, "build", side_effect=fail)
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual({"component": "converter", "phase": "binding_validation"},
                         self.receipt()["last_build_operation"])
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))
        self.assertFalse(any(event[0] == "start" for event in self.events))

    def test_untrusted_build_operation_fields_never_enter_receipt(self):
        self.pipeline()
        def fail(_layout, *, report_operation, **_options):
            report_operation("PRIVATE-PATH", "PRIVATE-LOG")
        self.patch(execution.hosts, "build", side_effect=fail)
        with self.assertRaises(ValueError):
            self.execute()
        self.assertNotIn("last_build_operation", self.receipt())
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_group_cache_is_distinct_across_frameworks(self):
        self.pipeline()
        self.execute()
        self.execute(("3.10.0", "net9.0", "server"))
        materializations = [event for event in self.events if event[0] == "materialize"]
        self.assertNotEqual(materializations[0][1:], materializations[1][1:])
        self.assertTrue(all(cache == group / "packages" for _, group, cache in materializations))

    def test_invalid_child_private_fields_are_never_retained(self):
        self.pipeline()
        self.record["password"] = "PRIVATE-CHILD-DATA"
        with self.assertRaises(ValueError):
            self.execute()
        self.assertEqual("stop", self.events[-1][0])
        self.assertFalse((self.root / "retained/cells/3.10.0-net10.0-server/browser.json").exists())
        self.assertNotIn("PRIVATE", json.dumps(self.receipt()))

    def test_run_visits_all_36_but_pending_proof_and_development_selection_never_accept(self):
        def prepare(_inputs, destination, _retained, **_identity):
            destination.mkdir()
            (destination / "verified-artifacts.json").write_text("{}")
            return {"verified_artifacts_sha256": "a" * 64, "browser_execution": {}}
        def execute(key, **_kwargs):
            record = copy.deepcopy(self.record)
            record["result"] = "passed"
            record.update(zip(("version", "framework", "host"), key))
            if key[0] == "3.10.0":
                record["proof"]["baseline_reopens"] = [reopen_row(version, key[1], key[2])
                    for version in execution.documents.TOOL_VERSIONS]
            else:
                record["proof"] = {}
            if key[2] == "wasm":
                record["proof"]["direct_backend"] = direct_backend_proof()
                boot_proof, _, observed = boot_receipt_fixture(key[1])
                record["proof"].update(wasm_boot=boot_proof, interactive_validation_observed=True)
                record["resources"] = observed
            record["assertions"] = [{"name": name, "passed": True, "reason_category": None}
                                     for name in execution.browser.required_assertions(record)]
            if key[0] == "3.10.0":
                attach_native_interop(record)
            attach_embedding(record)
            if key[0] == "3.10.0":
                attach_react_phase(record)
                if key[2] == "custom-elements":
                    # Deliberate incomplete native proof must block overall acceptance.
                    record["proof"].pop("reactflow")
                    next(a for a in record["assertions"] if a["name"] == "reactflow_edit_save")["passed"] = False
                    record["result"] = "incomplete"
            return record
        self.patch(execution.browser, "prepare_candidate", side_effect=prepare)
        self.patch(execution.subprocess, "check_output", return_value="10.0.300\n")
        cells = self.patch(execution, "execute_cell", side_effect=execute)
        for suffix, selected in (("full", None), ("development", ",".join(self.key))):
            def run():
                return execution.run(self.root / "inputs", self.root / ("candidate-" + suffix), self.root / suffix,
                                     fixture_source="a" * 40, cell=selected)
            if selected is None:
                with self.assertRaisesRegex(ValueError, "Package browser matrix did not satisfy acceptance"):
                    run()
                ledger = json.loads((self.root / suffix / "retained-evidence/matrix.json").read_text())
                self.assertEqual(execution.browser.MATRIX, {call.args[0] for call in cells.call_args_list})
                pending = [cell for cell in ledger["cells"] if cell["result"] == "incomplete"]
                self.assertEqual(3, len(pending))
                self.assertFalse(any(cell["result"] == "not_run" for cell in ledger["cells"]))
                self.assertTrue(all(cell["version"] == "3.10.0" and cell["host"] == "custom-elements" for cell in pending))
                self.assertEqual({"reactflow_edit_save"}, {item["name"] for cell in ledger["cells"] for item in cell["assertions"] if not item["passed"]})
            else:
                ledger = run()
            # complete_matrix currently certifies acceptance, not merely visiting each cell.
            self.assertFalse(ledger["passed"])
            self.assertFalse(ledger["complete_matrix"])
            self.assertEqual(36, len(ledger["cells"]))
        self.assertEqual(37, cells.call_count)

    def test_setup_failure_retains_36_not_run_and_never_builds(self):
        self.patch(execution.browser, "prepare_candidate", side_effect=RuntimeError("PRIVATE-INPUT-ERROR"))
        build = self.patch(execution.hosts, "build")
        with self.assertRaises(ValueError):
            execution.run(self.root / "inputs", self.root / "candidate", self.root / "output", fixture_source="a" * 40)
        build.assert_not_called()
        receipt = (self.root / "output/retained-evidence/matrix.json").read_text()
        ledger = json.loads(receipt)
        self.assertEqual(36, sum(cell["result"] == "not_run" for cell in ledger["cells"]))
        self.assertFalse(ledger["passed"])
        self.assertNotIn("PRIVATE", receipt)

    def test_command_logs_are_hashes_without_commands_or_absolute_paths(self):
        group = self.root / "group"
        log = group / "logs/restore.log"
        log.parent.mkdir(parents=True)
        log.write_text("PRIVATE-LOG-CONTENT")
        receipt = execution._command_receipts([{"command": ["PRIVATE-ARGV"], "exit_code": 0, "log": str(log)}], group)
        self.assertNotIn("PRIVATE", json.dumps(receipt))
        self.assertNotIn(str(self.root), json.dumps(receipt))
        reuse = {"stage": "reuse_verified_build", "project": "server", "project_assets_sha256": "a" * 64}
        self.assertEqual([reuse], execution._command_receipts([reuse], group))
        for mutation in ({"project": str(self.root / "server.csproj")}, {"project_assets_sha256": "PRIVATE"}):
            with self.subTest(mutation=mutation), self.assertRaises(ValueError):
                execution._command_receipts([{**reuse, **mutation}], group)


if __name__ == "__main__":
    unittest.main()
