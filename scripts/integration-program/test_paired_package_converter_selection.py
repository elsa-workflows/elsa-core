import base64
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
from zipfile import ZipFile

import paired_package_converter_selection as selected
import materialize_paired_package_hosts as hosts


class ConverterSelectionContracts(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        self.cache = self.root / "packages"
        self.pack = self.cache / selected.PACK_ID / selected.PACK_VERSION
        self.pack.mkdir(parents=True)
        self.archive = self.pack / f"{selected.PACK_ID}.{selected.PACK_VERSION}.nupkg"
        with ZipFile(self.archive, "w") as zipped:
            for name, body in ((selected.TASK, b"reviewed-task"), (selected.IMPLEMENTATION, b"reviewed-implementation")):
                member = "tools/net10.0/" + name + ".dll"
                target = self.pack / member
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(body)
                zipped.writestr(member, body)
        self.policy = self.root / "policy.json"
        self.policy.write_text(json.dumps({"converters": {
            selected.sha256(self.pack / "tools/net10.0" / (selected.TASK + ".dll")): {
                "sdk_version": "10.0.300", "source_sha256": "a" * 64,
                "implementation_sha256": selected.sha256(self.pack / "tools/net10.0" / (selected.IMPLEMENTATION + ".dll")),
                "wrapper_sha256": "b" * 64}}}))
        self.addCleanup(patch.stopall)
        patch.object(selected, "ARCHIVE_SHA256", selected.sha256(self.archive)).start()
        patch.object(selected.resources, "CONVERTER_POLICY", self.policy).start()
        self.inventory = [{"file": "runtime-123.nettrace", "sha256": "1" * 64, "bytes": 10}]
        task = self.event(selected.TASK)
        implementation = self.event(selected.IMPLEMENTATION)
        edge = copy.deepcopy(implementation)
        edge.update(requestor=task["result"], requestor_context=task["context"], requested_path=None)
        self.decoded = {"schema": 1, "traces": [{"file": self.inventory[0]["file"], "sha256": "1" * 64,
                                                 "events_lost": 0, "parser_completed": True}],
                        "records": [task, implementation, edge]}

    def event(self, name):
        identity = name + ", Version=10.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"
        path = str(self.pack / "tools/net10.0" / (name + ".dll"))
        return {"trace": "runtime-123.nettrace", "trace_sha256": "1" * 64, "pid": 123,
                "provider": "Microsoft-Windows-DotNETRuntime", "event_id": 291, "event_name": "AssemblyLoader/Stop",
                "success": True, "assembly": identity, "result": identity, "path": path, "requested_path": path,
                "requestor": selected.CORELIB, "context": "MSBuild plugin " + str(self.pack), "requestor_context": "Default"}

    def verify(self, value=None, inventory=None):
        return selected.verify_decoded(value or self.decoded, inventory or self.inventory, self.cache, "10.0.300")

    def test_actual_task_edge_and_identical_corelib_path_load_form_one_pair(self):
        receipt = self.verify()
        self.assertEqual(3, receipt["successful_load_records"])
        self.assertEqual(1, receipt["unique_implementation_identity"])
        self.assertEqual("reported_target_bind_context", receipt["context_scope"])
        self.assertNotIn(str(self.root), json.dumps(receipt))
        probe = copy.deepcopy(self.decoded["records"][2])
        probe.update(success=False, result=None, path=None)
        self.decoded["records"].append(probe)
        self.assertEqual(receipt, self.verify())

    def test_binding_diagnostic_distinguishes_requestor_and_policy_rejection(self):
        for mutated, sdk, expected in ((True, "10.0.300", "binding_requestors"),
                                      (False, "10.0.401", "binding_policy")):
            decoded = copy.deepcopy(self.decoded)
            if mutated:
                decoded["records"][0]["requestor"] = "PRIVATE-UNKNOWN-ASSEMBLY"
            operations = []
            with self.subTest(expected=expected), self.assertRaises(RuntimeError):
                selected.verify_decoded(decoded, self.inventory, self.cache, sdk,
                    lambda component, phase: operations.append((component, phase)))
            self.assertEqual(("converter", expected), operations[-1])
            self.assertNotIn("PRIVATE", json.dumps(operations))

    def test_incomplete_changed_lost_or_duplicate_trace_inventory_fails(self):
        for field, value in (("parser_completed", False), ("events_lost", 1), ("events_lost", False),
                             ("sha256", "2" * 64), ("file", "other.nettrace")):
            decoded = copy.deepcopy(self.decoded)
            decoded["traces"][0][field] = value
            with self.subTest(field=field, value=value), self.assertRaises(RuntimeError):
                self.verify(decoded)
        with self.assertRaises(RuntimeError):
            self.verify(inventory=self.inventory * 2)

    def test_malformed_owner_payload_and_assembly_binding_fails(self):
        for field, value in (("pid", 124), ("pid", True), ("success", "true"), ("provider", "other"),
                             ("event_id", 0), ("event_name", "other"), ("assembly", "other"),
                             ("context", "other"), ("requestor", "other"), ("requestor_context", "other")):
            decoded = copy.deepcopy(self.decoded)
            decoded["records"][2][field] = value
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                self.verify(decoded)
        for record_index in (1, 2):
            decoded = copy.deepcopy(self.decoded)
            decoded["records"].pop(record_index)
            if record_index == 1:
                # The direct task bind alone is sufficient; CoreLib duplication isn't mandatory.
                self.assertEqual(2, self.verify(decoded)["successful_load_records"])
            else:
                with self.assertRaisesRegex(RuntimeError, "task to converter"):
                    self.verify(decoded)

    def test_cross_trace_or_multiple_process_records_never_pair(self):
        for split in (True, False):
            decoded = copy.deepcopy(self.decoded)
            inventory = self.inventory + [{"file": "runtime-456.nettrace", "sha256": "2" * 64, "bytes": 10}]
            decoded["traces"].append({"file": "runtime-456.nettrace", "sha256": "2" * 64,
                                      "events_lost": 0, "parser_completed": True})
            extra = copy.deepcopy(decoded["records"] if not split else decoded["records"][1:])
            for row in extra:
                row.update(trace="runtime-456.nettrace", trace_sha256="2" * 64, pid=456)
            decoded["records"] = decoded["records"][:1] + extra if split else decoded["records"] + extra
            with self.subTest(split=split), self.assertRaises(RuntimeError):
                self.verify(decoded, inventory)

    def test_conflicting_successful_path_cache_archive_and_policy_fail(self):
        changed = copy.deepcopy(self.decoded)
        changed["records"][1]["path"] += ".other"
        with self.assertRaises(RuntimeError):
            self.verify(changed)
        target = self.pack / "tools/net10.0" / (selected.IMPLEMENTATION + ".dll")
        original = target.read_bytes()
        target.write_bytes(b"tampered")
        with self.assertRaisesRegex(RuntimeError, "original archive"):
            self.verify()
        target.write_bytes(original)
        target.unlink()
        outside = self.root / "outside.dll"
        outside.write_bytes(original)
        target.symlink_to(outside)
        with self.assertRaisesRegex(RuntimeError, "Symlink"):
            self.verify()
        target.unlink()
        target.write_bytes(original)
        with patch.object(selected, "ARCHIVE_SHA256", "0" * 64), self.assertRaises(RuntimeError):
            self.verify()
        with self.assertRaises(RuntimeError):
            selected.verify_decoded(self.decoded, self.inventory, self.cache, "10.0.401")
        self.policy.write_text('{"converters":{}}')
        with self.assertRaisesRegex(RuntimeError, "not approved"):
            self.verify()

    def test_trace_inventory_requires_dead_owner_and_regular_exact_files(self):
        traces = self.root / "traces"
        traces.mkdir()
        trace = traces / f"runtime-{os.getpid()}.nettrace"
        trace.write_bytes(b"not-a-valid-trace")
        with self.assertRaisesRegex(RuntimeError, "still running"):
            selected._trace_inventory(traces)
        trace.unlink()
        (traces / "unexpected").write_bytes(b"trace")
        with self.assertRaisesRegex(RuntimeError, "Unexpected"):
            selected._trace_inventory(traces)

    def decoder_fixture(self):
        name, version = "Public.Tool", "1.0.0"
        package = self.root / "packages/public.tool" / version
        package.mkdir(parents=True)
        output = self.root / "bin"
        output.mkdir()
        (self.root / "obj").mkdir()
        members = {"compile": "ref/net10.0/Public.Tool.dll", "runtime": "lib/net10.0/Public.Tool.dll",
                   "build": "build/Public.Tool.props"}
        bodies = {"compile": b"public-reference-tool", "runtime": b"public-managed-tool", "build": b"<Project />"}
        archive = package / "public.tool.1.0.0.nupkg"
        with ZipFile(archive, "w") as zipped:
            for kind, member in members.items():
                target = package / member
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(bodies[kind])
                zipped.writestr(member, bodies[kind])
        (output / "Public.Tool.dll").write_bytes(bodies["runtime"])
        lock_content = "NuGet-content-hash-is-not-the-full-signed-archive-hash"
        (package / ".nupkg.metadata").write_text(json.dumps({"contentHash": lock_content}))
        (self.root / "packages.lock.json").write_text(json.dumps({"dependencies": {"net10.0": {
            name: {"resolved": version, "contentHash": lock_content}}}}))
        assets = self.root / "obj/project.assets.json"
        assets.write_text(json.dumps({"packageFolders": {str(self.root.resolve() / "packages"): {}},
            "libraries": {name + "/" + version: {"type": "package", "path": "public.tool/1.0.0"}},
            "targets": {"net10.0": {name + "/" + version: {
            "type": "package", **{kind: {member: {}} for kind, member in members.items()}}}}}))
        (self.root / "dependency-archives.json").write_text(json.dumps({"packages": {name: {
            "version": version, "sha256": selected.sha256(archive), "bytes": archive.stat().st_size,
            "sha512": base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode(),
            "selected_assets": {kind: [member] for kind, member in members.items()}}}}))
        deps = output / "Decoder.deps.json"
        deps.write_text(json.dumps({"runtimeTarget": {"name": ".NETCoreApp,Version=v10.0"}, "targets": {
            ".NETCoreApp,Version=v10.0": {"Decoder/1.0.0": {"runtime": {"Decoder.dll": {}}},
                                       name + "/" + version: {"runtime": {members["runtime"]: {}}}}}}))
        return SimpleNamespace(package=package, output=output, archive=archive, members=members,
                               assets=assets, deps=deps, lock_content=lock_content)

    def test_decoder_dependencies_bind_lock_signed_archive_and_runtime_output(self):
        fixture = self.decoder_fixture()
        selected._decoder_dependencies(self.root, None)
        selected._decoder_dependencies(self.root, fixture.output)
        runtime = fixture.output / "Public.Tool.dll"
        original = runtime.read_bytes()
        runtime.write_bytes(b"different-runtime")
        with self.assertRaisesRegex(RuntimeError, "runtime dependency"):
            selected._decoder_dependencies(self.root, fixture.output)
        runtime.write_bytes(original)
        metadata = fixture.package / ".nupkg.metadata"
        metadata.write_text('{"contentHash":"different"}')
        with self.assertRaisesRegex(RuntimeError, "content hash"):
            selected._decoder_dependencies(self.root, fixture.output)
        metadata.write_text(json.dumps({"contentHash": fixture.lock_content}))
        fixture.archive.write_bytes(fixture.archive.read_bytes() + b"changed-signed-archive")
        with self.assertRaisesRegex(RuntimeError, "signed archive"):
            selected._decoder_dependencies(self.root, fixture.output)

    def test_decoder_checks_distinct_compiler_and_build_inputs_before_build(self):
        fixture = self.decoder_fixture()
        for kind in ("compile", "build"):
            target = fixture.package / fixture.members[kind]
            original = target.read_bytes()
            target.write_bytes(b"changed-input")
            with self.subTest(kind=kind), self.assertRaisesRegex(RuntimeError, "selected dependency"):
                selected._decoder_dependencies(self.root, None)
            target.write_bytes(original)

    def test_decoder_rejects_removed_or_added_selected_assets(self):
        fixture = self.decoder_fixture()
        original = json.loads(fixture.assets.read_text())
        for kind in ("compile", "runtime", "build"):
            changed = copy.deepcopy(original)
            changed["targets"]["net10.0"]["Public.Tool/1.0.0"][kind] = {}
            fixture.assets.write_text(json.dumps(changed))
            with self.subTest(kind=kind), self.assertRaisesRegex(RuntimeError, "selected dependency inventory"):
                selected._decoder_dependencies(self.root, fixture.output)
        changed = copy.deepcopy(original)
        changed["targets"]["net10.0"]["Public.Tool/1.0.0"]["native"] = {}
        fixture.assets.write_text(json.dumps(changed))
        with self.assertRaisesRegex(RuntimeError, "unreviewed asset kinds"):
            selected._decoder_dependencies(self.root, fixture.output)

    def test_decoder_rejects_redirected_package_resolution_before_build(self):
        fixture = self.decoder_fixture()
        original = json.loads(fixture.assets.read_text())
        mutations = [lambda a: a.update(packageFolders={"/unowned/cache": {}}),
                     lambda a: a["packageFolders"].update({"/fallback/cache": {}}),
                     lambda a: a["libraries"]["Public.Tool/1.0.0"].update(path="../outside"),
                     lambda a: a["libraries"]["Public.Tool/1.0.0"].update(type="project"),
                     lambda a: a["libraries"].pop("Public.Tool/1.0.0"),
                     lambda a: a["libraries"].update({"Extra/1.0.0": {"type": "package", "path": "extra/1.0.0"}})]
        for index, mutate in enumerate(mutations):
            changed = copy.deepcopy(original)
            mutate(changed)
            fixture.assets.write_text(json.dumps(changed))
            with self.subTest(mutation=index), self.assertRaisesRegex(RuntimeError, "package resolution"):
                selected._decoder_dependencies(self.root, None)

    def test_decoder_runtime_closure_must_match_fixed_selected_members(self):
        fixture = self.decoder_fixture()
        original = json.loads(fixture.deps.read_text())
        for mutation in ("remove-package", "remove-member", "extra-package"):
            changed = copy.deepcopy(original)
            target = changed["targets"][".NETCoreApp,Version=v10.0"]
            if mutation == "remove-package":
                target.pop("Public.Tool/1.0.0")
            elif mutation == "remove-member":
                target["Public.Tool/1.0.0"]["runtime"] = {}
            else:
                target["Unexpected/1.0.0"] = {"runtime": {"Unexpected.dll": {}}}
            fixture.deps.write_text(json.dumps(changed))
            with self.subTest(mutation=mutation), self.assertRaisesRegex(RuntimeError, "runtime closure"):
                selected._decoder_dependencies(self.root, fixture.output)

    def test_decoder_prepares_owned_commands_and_binds_reused_restore_metadata(self):
        fixture = self.root / "source"
        fixture.mkdir()
        (fixture / "Decoder.csproj").write_text("<Project />")
        events = []
        def command(argv, root, environment, log, timeout):
            phase = argv[1]
            events.append(phase)
            log.write_text("private command output")
            if phase == "restore":
                (root / "obj").mkdir()
                (root / "obj/project.assets.json").write_text("{}")
            else:
                output = root / "bin/Release/net10.0"
                output.mkdir(parents=True)
                (output / "Decoder.dll").write_bytes(b"synthetic decoder")
        root = self.root / "decoder"
        with patch.object(selected, "FIXTURE", fixture), patch.object(selected, "_owned_command", side_effect=command), \
             patch.object(selected, "_decoder_dependencies", side_effect=lambda _root, output: events.append("cache" if output is None else "output")):
            decoder = selected.prepare_decoder(root, "10.0.300", {})
            self.assertEqual(["restore", "cache", "build", "output"], events)
            self.assertEqual(decoder, selected.prepare_decoder(root, "10.0.300", {}))
            self.assertEqual("output", events[-1])
            (root / "obj/project.assets.json").write_text('{"changed":true}')
            with self.assertRaisesRegex(RuntimeError, "source or built output changed"):
                selected.prepare_decoder(root, "10.0.300", {})

    def test_group_query_timeout_cannot_certify_quiescence(self):
        with patch.object(selected.subprocess, "run", side_effect=subprocess.TimeoutExpired("ps", 2)) as query:
            with self.assertRaises(subprocess.TimeoutExpired):
                selected._wait_group_empty(123)
            self.assertGreater(query.call_args.kwargs["timeout"], 0)
            self.assertLessEqual(query.call_args.kwargs["timeout"], 2)

    def test_command_timeout_kills_owned_wrapper_children(self):
        pid_file = self.root / "child-pid"
        with self.assertRaises(subprocess.TimeoutExpired):
            selected._owned_command(["/bin/sh", "-c", 'sleep 60 & echo $! > "$1"; wait', "owned-test", str(pid_file)],
                                    self.root, os.environ.copy(), self.root / "command-private.log", 1)
        self.assertTrue(pid_file.exists())
        child = int(pid_file.read_text())
        result = subprocess.run(["ps", "-p", str(child), "-o", "stat="], capture_output=True, text=True)
        self.assertTrue(not result.stdout.strip() or "Z" in result.stdout)
        self.assertEqual(0o600, (self.root / "command-private.log").stat().st_mode & 0o777)

    def capture_fixture(self):
        project = self.root / "Wasm.csproj"
        project.write_text("<Project />")
        layout = SimpleNamespace(project_paths={"wasm": project}, group_root=self.root,
                                 packages_root=self.cache, sdk="10.0.300", request=SimpleNamespace(framework="net10.0"))
        env = {"DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER": "1", "MSBUILDDISABLENODEREUSE": "1"}
        command = ["dotnet", "build", project.name, "--no-restore", "-p:UseSharedCompilation=false"]
        return layout, project, command, env

    def test_capture_rejects_stale_intermediates_before_build(self):
        layout, project, command, env = self.capture_fixture()
        (self.root / "obj/Release/net10.0/webcil").mkdir(parents=True)
        with patch.object(selected, "prepare_decoder", return_value=project) as prepare, patch.object(selected, "_owned_command") as run:
            with self.assertRaisesRegex(RuntimeError, "fresh reviewed build"):
                selected.capture_build(layout, project, command, env, self.root / "build.log", project)
            run.assert_not_called()
            prepare.assert_not_called()
            self.assertEqual([], list(self.root.glob("converter-capture-*")))

    def test_capture_rechecks_cold_conversion_after_decoder_preparation(self):
        layout, project, command, env = self.capture_fixture()
        decoder = self.root / "decoder/bin/Release/net10.0/Decoder.dll"
        def prepare(*_):
            (self.root / "obj/Release/net10.0/webcil").mkdir(parents=True)
            return decoder
        with patch.object(selected, "prepare_decoder", side_effect=prepare), patch.object(selected, "_owned_command") as run:
            with self.assertRaisesRegex(RuntimeError, "fresh reviewed build"):
                selected.capture_build(layout, project, command, env, self.root / "build.log", decoder)
            run.assert_not_called()

    def test_capture_reports_trace_failure_after_completed_owned_build(self):
        layout, project, command, env = self.capture_fixture()
        decoder = self.root / "decoder/bin/Release/net10.0/Decoder.dll"
        operations = []
        with patch.object(selected, "prepare_decoder", return_value=decoder), \
                patch.object(selected, "_owned_command", return_value={}) as build, \
                patch.object(selected, "_trace_inventory", side_effect=ValueError("PRIVATE-TRACE")), \
                patch.object(selected, "_decode") as decode:
            with self.assertRaisesRegex(ValueError, "PRIVATE-TRACE"):
                selected.capture_build(layout, project, command, env, self.root / "build.log", decoder,
                    report_operation=lambda component, phase: operations.append((component, phase)))
        build.assert_called_once()
        decode.assert_not_called()
        self.assertEqual([("converter", phase) for phase in
                          ("decoder_validation", "build_command", "trace_inventory")], operations)

    def test_nonserver_materializer_requires_selection_before_sdk_command(self):
        request = hosts.CellRequest("wasm", "net10.0", "3.10.0")
        layout = hosts.materialize(request, self.root / "group", nuget_config="<configuration />",
                                   packages_root=self.root / "group/packages", sdk="10.0.300")
        with patch.object(hosts.packages, "_run_command") as run:
            with self.assertRaisesRegex(RuntimeError, "explicitly prepared"):
                hosts.build(layout)
            run.assert_not_called()

    def test_decoder_preparation_rejects_repository_or_fixture_cache_roots(self):
        for root, environment in ((Path(selected.__file__).parent / "unowned-tool", {}),
                                  (self.cache / "tool", {"NUGET_PACKAGES": str(self.cache)})):
            with self.subTest(root=root), patch.object(selected, "_owned_command") as run:
                with self.assertRaisesRegex(RuntimeError, "repository|fixture package cache"):
                    selected.prepare_decoder(root, "10.0.300", environment)
                run.assert_not_called()

    def test_reuse_rejects_escape_or_changed_selection_before_decode(self):
        project = self.root / "Wasm.csproj"
        project.write_text("<Project />")
        marker = self.root / "converter-selection-private.json"
        layout = SimpleNamespace(group_root=self.root, packages_root=self.cache, sdk="10.0.300")
        with patch.object(selected, "_decode") as decode:
            for capture in ("../other", "/private/tmp/other", "converter-capture-unreviewed"):
                marker.write_text(json.dumps({"schema": 1, "capture": capture, "selection_sha256": "0" * 64}))
                with self.assertRaisesRegex(RuntimeError, "Invalid reused"):
                    selected.verify_reused_selection(layout, project, project, {})
            capture = self.root / "converter-capture-0123456789abcdef"
            capture.mkdir()
            (capture / "selection.json").write_text("{}")
            marker.write_text(json.dumps({"schema": 1, "capture": capture.name, "selection_sha256": "0" * 64}))
            with self.assertRaisesRegex(RuntimeError, "selection changed"):
                selected.verify_reused_selection(layout, project, project, {})
            decode.assert_not_called()

    def test_client_build_and_reuse_both_require_converter_binding(self):
        request = hosts.CellRequest("wasm", "net10.0", "3.10.0")
        layout = hosts.materialize(request, self.root / "group", nuget_config="<configuration />",
                                   packages_root=self.root / "group/packages", sdk="10.0.300")
        decoder = self.root / "Decoder.dll"
        decoder.write_bytes(b"test-decoder")
        selection = self.verify()
        def run(command, cwd, env, log, timeout=1200):
            log.parent.mkdir(parents=True, exist_ok=True)
            log.write_text("10.0.300\n" if command == ["dotnet", "--version"] else "ok")
            if command != ["dotnet", "--version"]:
                (cwd / "obj").mkdir(exist_ok=True)
                (cwd / "obj/project.assets.json").write_text("{}")
                output = cwd / "bin/Release/net10.0"
                output.mkdir(parents=True, exist_ok=True)
                (output / "fixture.dll").write_bytes(b"test-output")
            return {"command": command, "exit_code": 0, "log": str(log)}
        def capture(actual_layout, project, command, env, log, actual_decoder, **_options):
            self.assertIs(layout, actual_layout)
            self.assertEqual(decoder, actual_decoder)
            self.assertEqual(layout.project_paths["wasm"], project)
            return run(command, project.parent, env, log) | {"converter_selection": selection}
        with patch.object(hosts.packages, "_run_command", side_effect=run), \
                patch.object(selected, "capture_build", side_effect=capture) as captured, \
                patch.object(selected, "verify_reused_selection", return_value=selection) as reused:
            first = hosts.build(layout, converter_decoder=decoder)
            self.assertEqual(1, captured.call_count)
            self.assertEqual([selection], [row["converter_selection"] for row in first if "converter_selection" in row])
            second = hosts.build(layout, converter_decoder=decoder)
            self.assertEqual(1, captured.call_count)
            reused.assert_called_once()
            self.assertEqual([selection], [row["converter_selection"] for row in second if "converter_selection" in row])
            reused.side_effect = RuntimeError("trace binding failed")
            with self.assertRaisesRegex(RuntimeError, "trace binding failed"):
                hosts.build(layout, converter_decoder=decoder)


if __name__ == "__main__":
    unittest.main()
