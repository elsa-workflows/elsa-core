"""Synthetic process/build contracts: no dotnet, listeners or browser are started."""
from dataclasses import replace
import json
from pathlib import Path
import signal
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import materialize_paired_package_hosts as hosts
import paired_package_converter_selection as converters


class FakeProcess:
    def __init__(self, pid, events):
        self.pid, self.events, self.returncode = pid, events, None
        self.group_alive = True

    def poll(self):
        return self.returncode

    def wait(self, timeout):
        if self.returncode is None:
            raise subprocess.TimeoutExpired("synthetic", timeout)
        self.events.append(("reaped", self.pid))
        return self.returncode


class FakeTimer:
    def __init__(self, seconds, callback):
        self.seconds, self.callback, self.started, self.cancelled = seconds, callback, False, False

    def start(self):
        self.started = True

    def cancel(self):
        self.cancelled = True


class DesignerPhasesTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        self.events, self.calls, self.processes, self.timers = [], [], [], []
        self.layout_count = 0
        self.reused_pids = set()
        for name, replacement in (("subprocess.Popen", self.launch), ("os.killpg", self.kill),
                                  ("browser_processes._browser_process_info", self.process_info),
                                  ("browser_processes._cleanup_browser_process", self.cleanup_process),
                                  ("threading.Timer", self.timer), ("_wait_ready", self.ready),
                                  ("_free_port", iter(range(41000, 41100)).__next__)):
            patcher = patch("materialize_paired_package_hosts." + name, replacement)
            patcher.start()
            self.addCleanup(patcher.stop)
        patcher = patch.object(converters, "_wait_group_empty", side_effect=lambda pid: not next(process for process in self.processes if process.pid == pid).group_alive)
        patcher.start()
        self.addCleanup(patcher.stop)

    def launch(self, command, **kwargs):
        process = FakeProcess(20000 + len(self.processes), self.events)
        self.events.append(("launch", process.pid))
        self.processes.append(process)
        self.calls.append((list(command), kwargs | {"env": dict(kwargs["env"])}))
        return process

    def kill(self, pid, signum):
        self.events.append(("signal", pid, signum))
        process = next(process for process in self.processes if process.pid == pid)
        if not process.group_alive:
            raise ProcessLookupError
        if signum == 0:
            return
        process.returncode = -signum
        # Model a launcher exiting on TERM while its child still owns a listener.
        if signum == signal.SIGKILL:
            process.group_alive = False

    def timer(self, seconds, callback):
        timer = FakeTimer(seconds, callback)
        self.timers.append(timer)
        return timer

    def process_info(self, pid):
        process = next(process for process in self.processes if process.pid == pid)
        return (1, "unrelated" if pid in self.reused_pids else pid, False, False)

    def cleanup_process(self, process, started):
        self.assertEqual(process.pid, started)
        if process.pid in self.reused_pids or process.poll() is not None:
            raise RuntimeError("synthetic lost root ownership")
        self.kill(process.pid, signal.SIGTERM)
        process.wait(timeout=10)
        self.kill(process.pid, signal.SIGKILL)

    def ready(self, process, url, timeout):
        self.assertIsNone(process.poll())

    def layout(self, host="server"):
        self.layout_count += 1
        group = self.root / str(self.layout_count)
        runtime = group / "runtime"
        runtime.mkdir(parents=True)
        paths, inputs = {}, {}
        names = ("backend", "wasm", "hosted-wasm") if host == "hosted-wasm" else ("backend", host)
        for name in names:
            project = group / name / (name + ".csproj")
            project.parent.mkdir()
            reference = '<ItemGroup><ProjectReference Include="../wasm/wasm.csproj" /></ItemGroup>' if name == "hosted-wasm" else ""
            project.write_text("<Project>" + reference + "</Project>")
            paths[name] = project
            inputs[str(project.relative_to(group))] = hosts.sha256(project)
            if name in ("wasm", "custom-elements"):
                config = project.parent / "wwwroot" / "appsettings.json"
                config.parent.mkdir()
                config.write_bytes(b'{}\n')
                inputs[str(config.relative_to(group))] = hosts.sha256(config)
            obj = project.parent / "obj"
            obj.mkdir()
            target = {"wasm/1.0.0": {"type": "project"}} if name == "hosted-wasm" else {}
            assets = {"targets": {"net10.0": target}, "libraries": {"wasm/1.0.0": {"msbuildProject": "../wasm/wasm.csproj"}}}
            (obj / "project.assets.json").write_text(json.dumps(assets))
            output = project.parent / "bin" / "Release" / "net10.0"
            output.mkdir(parents=True)
            (output / (project.stem + ".dll")).write_bytes(b"synthetic-built-output")
        layout = hosts.CellLayout(hosts.CellRequest(host, "net10.0", "3.10.0"), group, paths, runtime, group / "packages", "10.0.300", inputs)
        for project in paths.values():
            output = project.parent / "bin" / "Release" / "net10.0"
            stamp = {"schema": 1, "inputs": {key: digest for key, digest in inputs.items() if key.startswith(project.parent.name + "/")},
                     "assets": hosts.sha256(project.parent / "obj" / "project.assets.json"),
                     "outputs": {path.name: hosts.sha256(path) for path in output.iterdir()}}
            (project.parent / "build-reuse.json").write_text(json.dumps(stamp))
        return layout

    def validate(self, project):
        return {"project_assets_sha256": hosts.sha256(project.parent / "obj" / "project.assets.json")}

    def assert_clean(self):
        self.assertTrue(all(process.poll() is not None and not process.group_alive for process in self.processes))
        self.assertTrue(all(call[1]["stdout"].closed for call in self.calls))
        self.assertTrue(all(timer.cancelled for timer in self.timers))

    def test_supported_hosts_share_backend_state_and_reap_studio_between_phases(self):
        for host in hosts.HOST_NAMES:
            with self.subTest(host=host):
                layout = self.layout(host)
                offset = len(self.calls)
                with hosts.start_designer_phases(layout, validate_project=self.validate) as owner:
                    self.assertTrue(self.timers[-1].started)
                    with owner.phase("x6") as first:
                        self.assertNotIn(first.password, repr(first))
                        first_studio = first.process_ids[1]
                        if host != "server":
                            client = "wasm" if host == "hosted-wasm" else host
                            config = layout.project_paths[client].parent / "wwwroot" / "appsettings.json"
                            self.assertFalse(json.loads(config.read_text())["DesignerOptions"]["UseReactFlow"])
                    self.assertIsNone(owner.backend.poll())
                    self.assertFalse(next(process for process in self.processes if process.pid == first_studio).group_alive)
                    with owner.phase("react-flow") as second:
                        self.assertEqual(first.backend_url, second.backend_url)
                        self.assertEqual(first.studio_url, second.studio_url)
                        self.assertEqual(first.password, second.password)
                        self.assertEqual(first.safe_ids, second.safe_ids)
                        self.assertEqual(first.process_ids[0], second.process_ids[0])
                        self.assertNotEqual(first_studio, second.process_ids[1])
                        self.assertLess(self.events.index(("reaped", first_studio)), self.events.index(("launch", second.process_ids[1])))
                        self.assertEqual("react-flow", second.request.designer_mode)
                        if host != "server":
                            self.assertTrue(json.loads(config.read_text())["DesignerOptions"]["UseReactFlow"])
                backend, x6, react = self.calls[offset:]
                self.assertEqual(x6[0], react[0])
                self.assertEqual("false", x6[1]["env"]["DesignerOptions__UseReactFlow"])
                self.assertEqual("true", react[1]["env"]["DesignerOptions__UseReactFlow"])
                self.assertNotIn("Fixture__SecretsEncryptionKey", x6[1]["env"])
                self.assertNotIn("DesignerOptions__UseReactFlow", backend[1]["env"])
                for name in ("Fixture__RuntimeRoot", "Fixture__Password", "Identity__Tokens__SigningKey", "Backend__Url"):
                    self.assertEqual(x6[1]["env"][name], react[1]["env"][name])
                self.assertEqual(3, len(self.calls[offset:]))
                self.assertEqual(3, len(list(layout.runtime_root.glob("*-private.log"))))
                self.assertTrue(all(path.stat().st_mode & 0o777 == 0o600 for path in layout.runtime_root.iterdir()))
                if host != "server":
                    self.assertEqual(b'{}\n', config.read_bytes())
        self.assert_clean()

    def test_rejects_unsupported_cells_and_invalid_bounds_before_launch(self):
        layout = self.layout()
        variants = [replace(layout, request=replace(layout.request, version="3.9.0")),
                    replace(layout, request=replace(layout.request, designer_mode="react-flow"))]
        for invalid in variants:
            with self.assertRaises(RuntimeError), hosts.start_designer_phases(invalid, validate_project=self.validate):
                pass
        for options in ({"lifetime_seconds": 0}, {"lifetime_seconds": 601}, {"timeout_seconds": 301}):
            with self.assertRaises(RuntimeError), hosts.start_designer_phases(layout, validate_project=self.validate, **options):
                pass
        self.assertEqual([], self.calls)

    def test_phase_order_repetition_and_overlap_close_all_processes(self):
        for bad in ("first-react", "repeated-x6", "nested"):
            with self.subTest(bad=bad), self.assertRaises(RuntimeError), hosts.start_designer_phases(self.layout(), validate_project=self.validate) as owner:
                if bad == "first-react":
                    with owner.phase("react-flow"):
                        pass
                else:
                    with owner.phase("x6"):
                        if bad == "nested":
                            with owner.phase("react-flow"):
                                pass
                    with owner.phase("x6"):
                        pass
            self.assert_clean()

    def test_changed_input_assets_output_stamp_or_symlink_blocks_second_phase(self):
        for kind in ("input", "assets", "output", "stamp", "output-and-stamp", "symlink"):
            with self.subTest(kind=kind):
                layout = self.layout()
                project = layout.project_paths["server"]
                with hosts.start_designer_phases(layout, validate_project=self.validate) as owner:
                    with owner.phase("x6"):
                        pass
                    target = {"input": project, "assets": project.parent / "obj/project.assets.json", "stamp": project.parent / "build-reuse.json"}.get(kind, project.parent / "bin/Release/net10.0/server.dll")
                    if kind == "symlink":
                        twin = target.with_suffix(".original")
                        target.rename(twin)
                        target.symlink_to(twin)
                    else:
                        target.write_bytes(target.read_bytes() + b" ")
                        if kind == "output-and-stamp":
                            stamp = project.parent / "build-reuse.json"
                            data = json.loads(stamp.read_text())
                            data["outputs"][target.name] = hosts.sha256(target)
                            stamp.write_text(json.dumps(data))
                    with self.assertRaises(RuntimeError), owner.phase("react-flow"):
                        pass
                self.assert_clean()

    def test_second_phase_startup_and_body_failure_cleanup_backend(self):
        for failure in ("startup", "body"):
            with self.subTest(failure=failure), self.assertRaises(RuntimeError), hosts.start_designer_phases(self.layout(), validate_project=self.validate) as owner:
                with owner.phase("x6"):
                    pass
                if failure == "startup":
                    with patch.object(hosts, "_wait_ready", side_effect=RuntimeError("synthetic readiness failure")):
                        with owner.phase("react-flow"):
                            pass
                else:
                    with owner.phase("react-flow"):
                        raise RuntimeError("synthetic journey failure")
            self.assert_clean()

    def test_watchdog_during_phase_and_between_phases_prevents_new_listeners(self):
        for stage in ("inside", "between"):
            with self.subTest(stage=stage), self.assertRaises(RuntimeError), hosts.start_designer_phases(self.layout(), validate_project=self.validate) as owner:
                with owner.phase("x6"):
                    if stage == "inside":
                        self.timers[-1].callback()
                before = len(self.calls)
                self.timers[-1].callback()
                with owner.phase("react-flow"):
                    pass
            self.assert_clean()
            if stage == "between":
                self.assertEqual(before, len(self.calls))

    def test_delayed_watchdog_and_backend_exit_reject_next_phase(self):
        for failure in ("deadline", "backend"):
            with self.subTest(failure=failure), self.assertRaises(RuntimeError), hosts.start_designer_phases(self.layout(), validate_project=self.validate) as owner:
                with owner.phase("x6"):
                    pass
                before = len(self.calls)
                if failure == "backend":
                    owner.backend.returncode = 1
                    with owner.phase("react-flow"):
                        pass
                else:
                    # No timer callback: the launch boundary independently checks elapsed time.
                    with patch.object(hosts.time, "monotonic", return_value=owner.deadline + 1):
                        with owner.phase("react-flow"):
                            pass
            self.assertEqual(before, len(self.calls))
            if failure == "deadline":
                self.assert_clean()
            else:
                self.assertTrue(owner.backend.group_alive)  # Never guess ownership after root exit.

    def test_backend_startup_failure_and_second_spawn_failure_reap_owned_processes(self):
        with self.assertRaises(RuntimeError), patch.object(hosts, "_wait_ready", side_effect=RuntimeError("synthetic startup failure")):
            with hosts.start_designer_phases(self.layout(), validate_project=self.validate):
                pass
        self.assert_clean()
        with self.assertRaises(RuntimeError), hosts.start_designer_phases(self.layout(), validate_project=self.validate) as owner:
            with owner.phase("x6"):
                pass
            with patch.object(hosts.subprocess, "Popen", side_effect=RuntimeError("synthetic spawn failure")):
                with owner.phase("react-flow"):
                    pass
        self.assert_clean()

    def test_wasm_failure_resets_public_config_without_credentials(self):
        layout = self.layout("wasm")
        config = layout.project_paths["wasm"].parent / "wwwroot/appsettings.json"
        with self.assertRaises(RuntimeError), hosts.start_designer_phases(layout, validate_project=self.validate) as owner:
            with owner.phase("x6"):
                pass
            with patch.object(hosts, "_wait_ready", side_effect=RuntimeError("synthetic failure")):
                with owner.phase("react-flow"):
                    pass
        self.assertEqual(b'{}\n', config.read_bytes())
        self.assert_clean()

    def test_runtime_root_escape_or_alias_rejected_before_listener(self):
        layout = self.layout()
        outside = self.root / "outside"
        outside.mkdir()
        alias = layout.group_root / "alias"
        alias.symlink_to(layout.runtime_root, target_is_directory=True)
        for root in (outside, alias):
            with self.assertRaises(RuntimeError), hosts.start_designer_phases(replace(layout, runtime_root=root), validate_project=self.validate):
                pass
        self.assertEqual([], self.calls)

    def test_cleaned_phase_pgid_reuse_survives_watchdog_and_final_cleanup(self):
        with hosts.start_designer_phases(self.layout(), validate_project=self.validate) as owner:
            with owner.phase("x6") as first:
                old_pid = first.process_ids[1]
            self.reused_pids.add(old_pid)
            before = len(self.events)
            with owner.phase("react-flow"):
                pass
            self.timers[-1].callback()
        self.assertFalse(any(event[0] == "signal" and event[1] == old_pid for event in self.events[before:]))
        self.assert_clean()

    def test_changed_process_birth_is_unverified_without_signalling_reused_identity(self):
        with self.assertRaises(RuntimeError), hosts.start_designer_phases(self.layout(), validate_project=self.validate) as owner:
            with owner.phase("x6") as handle:
                studio_pid = handle.process_ids[1]
                self.reused_pids.add(studio_pid)
                before = len(self.events)
        self.assertFalse(any(event[0] == "signal" and event[1] == studio_pid for event in self.events[before:]))
        self.assertFalse(owner.backend.group_alive)

    def test_unverified_group_quiescence_fails_closed(self):
        with patch.object(converters, "_wait_group_empty", return_value=False):
            with self.assertRaises(RuntimeError), hosts.start_designer_phases(self.layout(), validate_project=self.validate) as owner:
                with owner.phase("x6"):
                    pass
        self.assertTrue(owner.closed)

    def test_stop_all_attempts_backend_when_studio_cleanup_fails(self):
        backend, studio = FakeProcess(10001, self.events), FakeProcess(10002, self.events)
        self.processes.extend((backend, studio))
        owned, attempts = [backend, studio], []
        def stop(process):
            attempts.append(process.pid)
            if process is studio:
                raise RuntimeError("synthetic unverified reap")
            self.cleanup_process(process, process.pid)
        with self.assertRaises(RuntimeError):
            hosts._stop_all(owned, stop=stop)
        self.assertEqual([studio.pid, backend.pid], attempts)
        self.assertFalse(backend.group_alive)
        self.assertEqual([studio], owned)

    def test_missing_stamp_or_wrong_provenance_fails_before_listener(self):
        layout = self.layout()
        (layout.project_paths["server"].parent / "build-reuse.json").unlink()
        with self.assertRaises((RuntimeError, FileNotFoundError)), hosts.start_designer_phases(layout, validate_project=self.validate):
            pass
        with self.assertRaises(RuntimeError), hosts.start_designer_phases(layout, validate_project=lambda project: {"project_assets_sha256": "wrong"}):
            pass
        self.assertEqual([], self.calls)


if __name__ == "__main__":
    unittest.main()
