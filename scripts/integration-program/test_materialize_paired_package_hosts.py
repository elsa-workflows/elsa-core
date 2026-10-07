import base64
import hashlib
import json
import os
from pathlib import Path
import tempfile
import subprocess
import sys
import unittest
from unittest.mock import Mock, patch
from xml.etree import ElementTree as ET

import materialize_paired_package_hosts as hosts


class HostMaterializationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)

    def materialize(self, host="server", version="3.10.0", framework="net10.0", **kwargs):
        request = hosts.CellRequest(host, framework, version, **kwargs)
        group = self.root / f"{version}-{framework}"
        return hosts.materialize(request, group, nuget_config="<configuration />",
                                 packages_root=group / "packages", sdk="10.0.300")

    def prepare_restored_assets(self, layout):
        for project in layout.project_paths.values():
            (project.parent / "obj").mkdir(exist_ok=True)
            (project.parent / "obj" / "project.assets.json").write_text('{"targets":{"net10.0":{}}}')

    def test_all_aligned_host_compositions_are_nonpackable_and_versioned(self):
        for version in hosts.VERSIONS:
            for framework in hosts.packages.FRAMEWORKS:
                for host in hosts.HOST_NAMES:
                    with self.subTest(version=version, framework=framework, host=host):
                        layout = self.materialize(host, version, framework)
                        for kind, project in layout.project_paths.items():
                            xml = ET.parse(project).getroot()
                            self.assertEqual("false", xml.findtext("PropertyGroup/IsPackable"))
                            self.assertEqual(framework, xml.findtext("PropertyGroup/TargetFramework"))
                            converter_pin = xml.find("Target[@Name='PinReviewedWebAssemblyConverterPack']")
                            if kind in ("wasm", "custom-elements"):
                                from paired_package_converter_selection import PACK_VERSION
                                self.assertIsNotNone(converter_pin)
                                self.assertEqual("ProcessFrameworkReferences", converter_pin.attrib["BeforeTargets"])
                                self.assertEqual({"Update": "Microsoft.NET.Sdk.WebAssembly.Pack",
                                    "WebAssemblySdkPackVersion": PACK_VERSION},
                                    converter_pin.find("ItemGroup/KnownWebAssemblySdkPack").attrib)
                            else:
                                self.assertIsNone(converter_pin)
                            constants = xml.findtext("PropertyGroup/DefineConstants")
                            if kind == "backend":
                                package_ids = {item.attrib["Include"] for item in xml.findall(".//PackageReference")}
                                self.assertEqual(version != "3.8.4", "Elsa.Bpmn.Interchange" in package_ids)
                                self.assertEqual("$(DefineConstants);FIXTURE_BPMN" if version != "3.8.4" else None, constants)
                                backend_code = (project.parent / "Program.cs").read_text()
                                self.assertIn("#if FIXTURE_BPMN\n    elsa.UseBpmnInterchange();\n#endif", backend_code)
                            else:
                                self.assertIsNone(constants)
                            refs = xml.findall(".//ProjectReference")
                            self.assertEqual(1 if kind == "hosted-wasm" else 0, len(refs))
                            if refs:
                                self.assertEqual(layout.project_paths["wasm"], (project.parent / refs[0].attrib["Include"]).resolve())
                            for package in xml.findall(".//PackageReference"):
                                if package.attrib["Include"].startswith("Elsa"):
                                    self.assertEqual(version, package.attrib["Version"])

    def test_baseline_glue_uses_released_workflows_api_and_usertasks_availability(self):
        for version in hosts.VERSIONS:
            for host in ("server", "wasm", "custom-elements"):
                with self.subTest(version=version, host=host):
                    layout = self.materialize(host, version)
                    project = layout.project_paths[host]
                    code = (project.parent / "Program.cs").read_text()
                    self.assertIn("AddWorkflowsModule();" if version == "3.8.4" else "AddWorkflowsModule(backendApiConfig);", code)
                    refs = {p.attrib["Include"] for p in ET.parse(project).findall(".//PackageReference")}
                    self.assertEqual(version != "3.8.4", "Elsa.Studio.UserTasks" in refs)
                    self.assertEqual(version != "3.8.4", "AddUserTasksModule" in code)

    def test_custom_feature_host_transform_is_source_bound_for_every_aligned_version(self):
        relative = "common/custom-elements/Components/ThemedComponentWrapper.razor"
        entries = {entry["file"]: entry for entry in json.loads((hosts.FIXTURE / "hosts/source-glue.json").read_text())["files"]}
        entry = entries[relative]
        original = "3bcfde9c9a3a3db6232c76ac492dbc5a563f7bdb7b23329a4e8c42fc9f523c64"
        self.assertEqual(original, entry["source_sha256"])
        self.assertEqual("ba5b348aa2414fdf7c19d9d8806e87b7c91a4205", entry["source_commit"])
        self.assertIn("feature initialization", entry["fixture_transform"])
        expected = (hosts.FIXTURE / "hosts" / relative).read_bytes()
        source = Path(__file__).resolve().parents[2] / entry["source_path"]
        self.assertEqual(source.read_bytes(), expected)
        self.assertNotEqual(original, entry["sha256"])
        for version in hosts.VERSIONS:
            with self.subTest(version=version):
                layout = self.materialize("custom-elements", version)
                root = layout.project_paths["custom-elements"].parent
                themed = root / "Components/ThemedComponentWrapper.razor"
                self.assertEqual(expected, themed.read_bytes())
                self.assertEqual(entry["sha256"], layout.input_hashes[str(themed.relative_to(layout.group_root))])
                provider = root / "Components/BackendProvider.razor"
                provider_entry = entries["common/custom-elements/Components/BackendProvider.razor"]
                self.assertEqual(provider_entry["sha256"], hashlib.sha256(provider.read_bytes()).hexdigest())
                self.assertNotIn("fixture_transform", provider_entry)

    def test_custom_feature_host_transform_rejects_unbound_fixture_bytes(self):
        fixture = self.root / "changed-fixture"
        relative = Path("common/custom-elements/Components/ThemedComponentWrapper.razor")
        themed = fixture / "hosts" / relative
        themed.parent.mkdir(parents=True)
        themed.write_bytes((hosts.FIXTURE / "hosts" / relative).read_bytes() + b"\n@ChildContent\n")
        (fixture / "hosts/source-glue.json").write_bytes((hosts.FIXTURE / "hosts/source-glue.json").read_bytes())
        with patch.object(hosts, "FIXTURE", fixture), self.assertRaisesRegex(RuntimeError, "Pinned host source glue changed"):
            hosts._host_source("custom-elements", "3.10.0")

    def test_group_reuse_has_fresh_runtime_and_rejects_identity_changes(self):
        first, second = self.materialize(), self.materialize()
        self.assertNotEqual(first.runtime_root, second.runtime_root)
        self.assertEqual(first.project_paths, second.project_paths)
        with self.assertRaisesRegex(RuntimeError, "identity differs"):
            hosts.materialize(hosts.CellRequest("server", "net8.0", "3.10.0"), first.group_root,
                              nuget_config="<configuration />", packages_root=first.packages_root, sdk="10.0.300")
        (first.project_paths["server"].parent / "Program.cs").write_text("changed")
        with self.assertRaisesRegex(RuntimeError, "glue/config changed"):
            self.materialize()

    def test_symlinked_and_unowned_roots_are_rejected(self):
        (self.root / "target").mkdir()
        (self.root / "link").symlink_to(self.root / "target", target_is_directory=True)
        with self.assertRaisesRegex(RuntimeError, "Symlinked"):
            hosts.materialize(hosts.CellRequest("server", "net10.0", "3.10.0"), self.root / "link",
                              nuget_config="<configuration />", packages_root=self.root / "cache", sdk="10.0.300")
        with self.assertRaisesRegex(RuntimeError, "unowned"):
            hosts.materialize(hosts.CellRequest("server", "net10.0", "3.10.0"), self.root / "target",
                              nuget_config="<configuration />", packages_root=self.root / "cache", sdk="10.0.300")

    def test_invalid_requests_fail_closed(self):
        for change in ({"version": "latest"}, {"framework": "net7.0"}, {"host": "fake-shell"},
                       {"backend_features": ("secrets", "secrets")}, {"backend_features": ("legacy-secrets",)},
                       {"route_prefix": "../escape"}, {"route_prefix": "prefix"}, {"permission_profile": "ambient"}):
            values = dict(host="server", framework="net10.0", version="3.10.0") | change
            with self.subTest(change=change), self.assertRaises(RuntimeError):
                hosts.CellRequest(**values)

    def test_designer_mode_defaults_to_x6_and_opt_in_is_candidate_only(self):
        for version in hosts.VERSIONS:
            self.assertEqual("x6", hosts.CellRequest("server", "net10.0", version).designer_mode)
        # Appending the mode keeps every earlier positional argument's meaning.
        request = hosts.CellRequest("hosted-wasm", "net10.0", "3.10.0", ("workflow-contexts",), "deny-secrets", "fixture", "react-flow")
        self.assertEqual(("react-flow", "fixture", "deny-secrets", ("workflow-contexts",)),
                         (request.designer_mode, request.route_prefix, request.permission_profile, request.backend_features))
        for version, mode in (("3.8.4", "react-flow"), ("3.9.0", "react-flow"),
                              ("3.10.0", "ReactFlow"), ("3.10.0", True), ("3.10.0", "")):
            with self.subTest(version=version, mode=mode), self.assertRaisesRegex(RuntimeError, "designer mode"):
                hosts.CellRequest("server", "net10.0", version, designer_mode=mode)

    def test_candidate_modes_share_exact_build_inputs_and_only_candidate_clients_bind_options(self):
        binding = 'Configure<Elsa.Studio.Workflows.Designer.Options.DesignerOptions>(configuration.GetSection("DesignerOptions"));'
        for host in hosts.HOST_NAMES:
            with self.subTest(host=host):
                x6 = self.materialize(host)
                react = self.materialize(host, designer_mode="react-flow")
                self.assertEqual(x6.project_paths, react.project_paths)
                self.assertEqual(x6.input_hashes, react.input_hashes)
                client = "wasm" if host == "hosted-wasm" else host
                code = (react.project_paths[client].parent / "Program.cs").read_text()
                self.assertEqual(client in ("wasm", "custom-elements"), binding in code)
                if client == "server":
                    self.assertIn('Configure<DesignerOptions>(configuration.GetSection("DesignerOptions"));', code)
                for version in ("3.8.4", "3.9.0"):
                    released = self.materialize(host, version)
                    code = (released.project_paths[client].parent / "Program.cs").read_text()
                    self.assertNotIn(binding, code)

    def test_owned_designer_configuration_is_exact_public_and_restored_for_every_host(self):
        ambient = {"DesignerOptions__UseReactFlow": "ambient", "designeroptions:UseReactFlow": "ambient",
                   "DESIGNEROPTIONS__UnreviewedOption": "ambient"}
        cases = [("3.10.0", mode) for mode in hosts.DESIGNER_MODES] + [(version, "x6") for version in ("3.8.4", "3.9.0")]
        for host in hosts.HOST_NAMES:
            for version, mode in cases:
                with self.subTest(host=host, version=version, mode=mode), patch.dict(os.environ, ambient):
                    layout = self.materialize(host, version, designer_mode=mode,
                                              route_prefix="fixture" if host == "hosted-wasm" else "")
                    self.prepare_restored_assets(layout)
                    launches, public = [], []
                    client = "wasm" if host == "hosted-wasm" else host
                    config = layout.project_paths[client].parent / "wwwroot/appsettings.json" if client != "server" else None

                    def launch(command, **kwargs):
                        launches.append(kwargs["env"].copy())
                        if config is not None:
                            public.append(json.loads(config.read_text()))
                        return Mock(pid=12345, poll=Mock(return_value=0), wait=Mock(return_value=0))

                    with patch.object(hosts.subprocess, "Popen", side_effect=launch), patch.object(hosts, "_wait_ready"), \
                            patch.object(hosts, "_free_port", side_effect=(10001, 10002)):
                        with hosts.start_pair(layout, validate_project=lambda project: {
                            "project_assets_sha256": hosts.sha256(project.parent / "obj/project.assets.json")}) as handle:
                            self.assertEqual(mode, handle.request.designer_mode)
                            self.assertNotIn("DesignerOptions__UseReactFlow", launches[0])
                            self.assertEqual(str(mode == "react-flow").lower() if version == "3.10.0" else None,
                                             launches[1].get("DesignerOptions__UseReactFlow"))
                            for environment in launches:
                                self.assertNotIn("designeroptions:UseReactFlow", environment)
                                self.assertNotIn("DESIGNEROPTIONS__UnreviewedOption", environment)
                            for settings in public:
                                self.assertEqual({"UseReactFlow": mode == "react-flow"} if version == "3.10.0" else None,
                                                 settings.get("DesignerOptions"))
                                self.assertEqual(handle.backend_url, settings["Backend"]["Url"])
                                self.assertNotIn(handle.password, json.dumps(settings))
                    if config is not None:
                        self.assertEqual(b'{}\n', config.read_bytes())
                        self.assertEqual(layout.input_hashes[str(config.relative_to(layout.group_root))], hosts.sha256(config))

    def test_targeted_denials_retain_versioned_editor_and_other_feature_grants(self):
        for version in hosts.VERSIONS:
            legacy = version == "3.8.4"
            editor = {"read:workflow-definitions", "write:workflow-definitions", "publish:workflow-definitions", "exec:workflow-definitions"} if legacy else {
                "workflows/definitions:view", "workflows/definitions:write", "workflows/definitions:publish", "workflows/definitions:execute"}
            for profile in hosts.PERMISSION_PROFILES:
                with self.subTest(version=version, profile=profile):
                    request = hosts.CellRequest("server", "net10.0", version, permission_profile=profile)
                    grants = hosts.permission_grants(request)
                    # Feature presence changes registration, not the caller's profile.
                    self.assertEqual(grants, hosts.permission_grants(hosts.CellRequest(
                        "server", "net10.0", version, backend_features=(), permission_profile=profile)))
                    self.assertEqual(len(grants), len(set(grants)))
                    if profile == "full":
                        self.assertEqual(("*",), grants)
                    elif profile == "denied":
                        self.assertEqual(("read:workflow-definitions", "read:workflow-instances", "read:activity-descriptors",
                                          "read:workflow-context-provider-descriptors"), grants)
                    else:
                        self.assertTrue(editor <= set(grants))
                        self.assertFalse(any("*" in grant for grant in grants))
                        self.assertEqual(profile != "deny-workflow-contexts", "read:workflow-context-provider-descriptors" in grants)
                        secret_grants = {grant for grant in grants if "secrets" in grant}
                        self.assertEqual(set() if profile == "deny-secrets" else
                                         ({"read:secrets", "write:secrets", "delete:secrets", "test:secrets", "use:secrets", "export:secrets", "import:secrets"}
                                          if legacy else {"secrets:view", "secrets:write", "secrets:delete", "secrets:test"}), secret_grants)

    def test_fresh_encryption_key_and_exact_grants_enter_only_owned_backend_environment(self):
        keys = set()
        ambient = {"fixture__secretsencryptionkey": "ambient-key", "Fixture:PermissionGrants": '["*"]',
                   "Fixture__PermissionProfile": "full"}
        for host in hosts.HOST_NAMES:
            with self.subTest(host=host), patch.dict(os.environ, ambient):
                layout = self.materialize(host, permission_profile="deny-secrets")
                self.prepare_restored_assets(layout)
                launches = []

                def launch(command, **kwargs):
                    launches.append((command, kwargs["env"].copy()))
                    return Mock(pid=12345, poll=Mock(return_value=0), wait=Mock(return_value=0))

                with patch.object(hosts.subprocess, "Popen", side_effect=launch), patch.object(hosts, "_wait_ready"):
                    with hosts.start_pair(layout, validate_project=lambda project: {
                        "project_assets_sha256": hosts.sha256(project.parent / "obj" / "project.assets.json")}) as handle:
                        backend = launches[0][1]
                        encoded_key = backend["Fixture__SecretsEncryptionKey"]
                        self.assertEqual(32, len(base64.b64decode(encoded_key, validate=True)))
                        self.assertNotIn(encoded_key, keys)
                        keys.add(encoded_key)
                        self.assertEqual(list(hosts.permission_grants(layout.request)), json.loads(backend["Fixture__PermissionGrants"]))
                        self.assertEqual("deny-secrets", backend["Fixture__PermissionProfile"])
                        self.assertFalse(any(key in launches[1][1] for key in ("Fixture__SecretsEncryptionKey", "Fixture__PermissionGrants")))
                        for command, env in launches:
                            self.assertFalse(any(key in env for key in ambient if key != "Fixture__PermissionProfile"))
                            self.assertNotIn(encoded_key, " ".join(command))
                        self.assertNotIn(encoded_key, repr(handle))
                        self.assertFalse(any(encoded_key.encode() in path.read_bytes()
                                             for path in layout.group_root.rglob("*") if path.is_file()))
        self.assertEqual(len(hosts.HOST_NAMES), len(keys))

    def test_no_listener_starts_without_asset_bound_provenance(self):
        layout = self.materialize()
        self.prepare_restored_assets(layout)
        with patch.object(hosts.subprocess, "Popen") as launch:
            with self.assertRaisesRegex(RuntimeError, "not bound"):
                with hosts.start_pair(layout, validate_project=lambda project: {"project_assets_sha256": "wrong"}):
                    self.fail("unverified host started")
            launch.assert_not_called()

    def test_startup_failure_stops_and_reaps_owned_process(self):
        layout = self.materialize()
        self.prepare_restored_assets(layout)
        real_launch = subprocess.Popen
        processes = []

        def launch(*args, **kwargs):
            process = real_launch([sys.executable, "-c", "import time; time.sleep(60)"],
                                  start_new_session=True, stdout=kwargs["stdout"], stderr=kwargs["stderr"])
            processes.append(process)
            return process

        def validate(project):
            return {"project_assets_sha256": hosts.sha256(project.parent / "obj" / "project.assets.json")}

        with patch.object(hosts.subprocess, "Popen", side_effect=launch), patch.object(hosts, "_wait_ready", side_effect=RuntimeError("startup failure")):
            with self.assertRaisesRegex(RuntimeError, "startup failure"):
                with hosts.start_pair(layout, validate_project=validate):
                    self.fail("failed start yielded a handle")
        self.assertEqual(1, len(processes))
        self.assertIsNotNone(processes[0].poll())

    def test_restored_library_source_fallback_fails_before_launch(self):
        layout = self.materialize()
        project = layout.project_paths["backend"]
        (project.parent / "obj").mkdir()
        (project.parent / "obj" / "project.assets.json").write_text(json.dumps({"targets": {"net10.0": {"Elsa/3.10.0": {"type": "project"}}}}))
        with patch.object(hosts.subprocess, "Popen") as launch:
            with self.assertRaisesRegex(RuntimeError, "source/project fallback"):
                with hosts.start_pair(layout, validate_project=lambda project: {}):
                    self.fail("source fallback executed")
            launch.assert_not_called()

    def test_local_host_settings_cannot_override_owned_backend_auth(self):
        layout = self.materialize()
        self.prepare_restored_assets(layout)
        (layout.project_paths["server"].parent / "appsettings.Local.json").write_text('{"Backend":{"Url":"https://unreviewed.example"}}')
        with patch.object(hosts.subprocess, "Popen") as launch:
            with self.assertRaisesRegex(RuntimeError, "Unreviewed host settings"):
                with hosts.start_pair(layout, validate_project=lambda project: {"project_assets_sha256": hosts.sha256(project.parent / "obj" / "project.assets.json")}):
                    self.fail("unreviewed settings launched")
            launch.assert_not_called()

    def test_isolation_inputs_cannot_change_on_reuse(self):
        layout = self.materialize()
        (layout.project_paths["backend"].parent / "global.json").write_text('{"sdk":{"version":"ambient"}}')
        with self.assertRaisesRegex(RuntimeError, "isolation changed"):
            self.materialize()

    def test_owned_listener_lifetime_is_finite_even_when_context_stays_open(self):
        layout = self.materialize()
        self.prepare_restored_assets(layout)
        real_launch, processes = subprocess.Popen, []

        def launch(*args, **kwargs):
            process = real_launch([sys.executable, "-c", "import time; time.sleep(60)"], start_new_session=True,
                                  stdout=kwargs["stdout"], stderr=kwargs["stderr"])
            processes.append(process)
            return process

        def validate(project):
            return {"project_assets_sha256": hosts.sha256(project.parent / "obj" / "project.assets.json")}

        with patch.object(hosts.subprocess, "Popen", side_effect=launch), patch.object(hosts, "_wait_ready"):
            with hosts.start_pair(layout, validate_project=validate, lifetime_seconds=0.05):
                for process in processes:
                    self.assertLess(process.wait(timeout=2), 0)

    def test_client_runtime_config_is_private_input_free_and_restored_after_failure(self):
        for host in ("wasm", "hosted-wasm", "custom-elements"):
            with self.subTest(host=host):
                layout = self.materialize(host, route_prefix="fixture" if host == "hosted-wasm" else "")
                self.prepare_restored_assets(layout)
                client = "wasm" if host == "hosted-wasm" else host
                config = layout.project_paths[client].parent / "wwwroot" / "appsettings.json"
                processes = []
                real_launch = subprocess.Popen

                def launch(*args, **kwargs):
                    public = json.loads(config.read_text())
                    self.assertEqual("ElsaIdentity", public["Authentication"]["Provider"])
                    self.assertEqual(kwargs["env"]["Backend__Url"], public["Backend"]["Url"])
                    self.assertNotIn(kwargs["env"]["Fixture__Password"], config.read_text())
                    process = real_launch([sys.executable, "-c", "import time; time.sleep(60)"], start_new_session=True,
                                          stdout=kwargs["stdout"], stderr=kwargs["stderr"])
                    processes.append(process)
                    return process

                def validate(project):
                    return {"project_assets_sha256": hosts.sha256(project.parent / "obj" / "project.assets.json")}

                with patch.object(hosts.subprocess, "Popen", side_effect=launch), patch.object(hosts, "_wait_ready"):
                    with self.assertRaisesRegex(RuntimeError, "browser failure"):
                        with hosts.start_pair(layout, validate_project=validate) as handle:
                            if host == "hosted-wasm":
                                self.assertTrue(handle.studio_url.endswith("/fixture/"))
                            raise RuntimeError("browser failure")
                self.assertEqual(b'{}\n', config.read_bytes())
                self.assertTrue(all(process.poll() is not None for process in processes))
                # Restoring the placeholder makes shared input/build hashes stable.
                self.assertEqual(layout.input_hashes[str(config.relative_to(layout.group_root))], hosts.sha256(config))

    def fake_build_runner(self, layout, called):
        def run(command, cwd, environment, log, timeout):
            called.append(command)
            log.parent.mkdir(parents=True, exist_ok=True)
            log.write_text(layout.sdk + "\n" if command == ["dotnet", "--version"] else "ok")
            if command == ["dotnet", "--version"]:
                return {"command": command, "exit_code": 0}
            (cwd / "obj").mkdir(exist_ok=True)
            if command[1] == "restore":
                assets = json.dumps({"configFilePaths": [str(cwd / "NuGet.Config")]})
                (cwd / "obj/project.assets.json").write_text(assets)
                if cwd.name == "hosted-wasm" and "--no-dependencies" not in command:
                    # Model the observed recursive restore changing the child's config identity.
                    (layout.project_paths["wasm"].parent / "obj/project.assets.json").write_text(assets)
            output = cwd / "bin" / "Release" / layout.request.framework
            output.mkdir(parents=True, exist_ok=True)
            (output / "fixture.dll").write_bytes(b"reviewed-output")
            if command[1] == "build" and cwd.name == "wasm" and layout.request.host == "hosted-wasm":
                bundle = cwd / "obj/Release" / layout.request.framework / "scopedcss/bundle/Elsa.Studio.Host.Wasm.styles.css"
                bundle.parent.mkdir(parents=True, exist_ok=True)
                bundle.write_bytes(b"reviewed-generated-bundle")
            return {"command": command, "exit_code": 0}
        return run

    def test_build_reuse_rejects_modified_asset_or_output_hash(self):
        layout = self.materialize()
        env, called = {}, []
        run = self.fake_build_runner(layout, called)

        with patch.object(hosts, "isolated_environment", return_value=env), patch.object(hosts.packages, "_run_command", side_effect=run):
            hosts.build(layout)
            called.clear()
            reused = hosts.build(layout)
            self.assertEqual([["dotnet", "--version"]], called)
            self.assertEqual(2, sum(record.get("stage") == "reuse_verified_build" for record in reused))
            backend = layout.project_paths["backend"].parent
            (backend / "obj" / "project.assets.json").write_text("changed")
            called.clear()
            hosts.build(layout)
            self.assertEqual(3, len(called))
            (backend / "bin" / "Release" / layout.request.framework / "fixture.dll").write_bytes(b"changed")
            called.clear()
            hosts.build(layout)
            self.assertEqual(3, len(called))

    def test_hosted_wrapper_preserves_completed_client_restore_and_output(self):
        import paired_package_converter_selection as converters
        for version in hosts.VERSIONS:
            with self.subTest(version=version):
                layout = self.materialize("hosted-wasm", version)
                called = []
                run = self.fake_build_runner(layout, called)
                def capture(_layout, project, command, env, log, _decoder, **_options):
                    return run(command, project.parent, env, log, 1200)
                with patch.object(hosts, "isolated_environment", return_value={}), \
                        patch.object(hosts.packages, "_run_command", side_effect=run), \
                        patch.object(converters, "capture_build", side_effect=capture):
                    hosts.build(layout, converter_decoder=self.root / "synthetic-decoder")
                client, wrapper = layout.project_paths["wasm"], layout.project_paths["hosted-wasm"]
                self.assertEqual([str(client.parent / "NuGet.Config")],
                    json.loads((client.parent / "obj/project.assets.json").read_text())["configFilePaths"])
                wrapper_commands = [command for command in called if len(command) > 2 and command[2] == wrapper.name]
                self.assertEqual(["restore", "build"], [command[1] for command in wrapper_commands])
                self.assertTrue(all("--no-dependencies" in command for command in wrapper_commands))
                client_build = next(command for command in called if len(command) > 2 and command[1:3] == ["build", client.name])
                self.assertLess(called.index(client_build), called.index(wrapper_commands[0]))
                self.assertEqual(set(layout.project_paths), set(hosts._build_identity(layout)))

    def test_hosted_generated_bundle_requires_build_pin_and_rebuilds_stale_or_changed_output(self):
        import paired_package_converter_selection as converters
        layout = self.materialize("hosted-wasm")
        called = []
        run = self.fake_build_runner(layout, called)
        def capture(_layout, project, command, env, log, _decoder, **_options):
            return run(command, project.parent, env, log, 1200)
        client = layout.project_paths["wasm"]
        stamp = client.parent / "build-reuse.json"
        bundle = client.parent / "obj/Release" / layout.request.framework / "scopedcss/bundle/Elsa.Studio.Host.Wasm.styles.css"
        with patch.object(hosts, "isolated_environment", return_value={}), \
                patch.object(hosts.packages, "_run_command", side_effect=run), \
                patch.object(converters, "capture_build", side_effect=capture), \
                patch.object(converters, "verify_reused_selection", return_value={}):
            hosts.build(layout, converter_decoder=self.root / "synthetic-decoder")
            recorded = json.loads(stamp.read_text())["generated_static_assets"]
            self.assertEqual({bundle.relative_to(client.parent).as_posix(): hosts.sha256(bundle)}, recorded)
            called.clear()
            hosts.build(layout, converter_decoder=self.root / "synthetic-decoder")
            self.assertEqual([["dotnet", "--version"]], called)
            for mutation in ("missing-pin", "changed-body", "missing-body"):
                with self.subTest(mutation=mutation):
                    if mutation == "missing-pin":
                        previous = json.loads(stamp.read_text())
                        del previous["generated_static_assets"]
                        stamp.write_text(json.dumps(previous))
                    elif mutation == "changed-body":
                        bundle.write_bytes(b"changed outside bin")
                    else:
                        bundle.unlink()
                    with self.assertRaises(RuntimeError):
                        hosts._build_identity(layout)
                    called.clear()
                    hosts.build(layout, converter_decoder=self.root / "synthetic-decoder")
                    self.assertEqual(["restore", "build"], [command[1] for command in called if len(command) > 2])
                    self.assertTrue(all(command[2] == client.name for command in called if len(command) > 2))
                    self.assertEqual(set(layout.project_paths), set(hosts._build_identity(layout)))

    def test_credentials_are_not_in_repr_or_materialized_inputs(self):
        layout = self.materialize()
        request = layout.request
        handle = hosts.RuntimeHandle(request, "http://127.0.0.1:1/", "http://127.0.0.1:2/elsa/api", "synthetic", "private-password")
        self.assertNotIn("private-password", repr(handle))
        self.assertNotIn("Password", (layout.group_root / "group.json").read_text())
        self.assertEqual(1, json.loads((layout.group_root / "group.json").read_text())["schema"])


if __name__ == "__main__":
    unittest.main()
