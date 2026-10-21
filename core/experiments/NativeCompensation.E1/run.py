#!/usr/bin/env python3
"""Reproducible native process-kill controller for #8930; Python never executes workflow transitions."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import queue
import shutil
import sqlite3
import subprocess
import sys
import threading
import time

from validation import MATRIX_CASES, validate_matrix

HERE = Path(__file__).resolve().parent
DLL = HERE / "bin/Debug/net10.0/NativeCompensation.E1.dll"
def dotnet_host():
    requested = os.environ.get("E1_DOTNET", "dotnet")
    resolved = shutil.which(requested)
    if not resolved:
        raise RuntimeError(f"Cannot resolve .NET host {requested!r}; set E1_DOTNET to the host used to build the fixture")
    return resolved

parser = argparse.ArgumentParser()
parser.add_argument("output", type=Path)
parser.add_argument("--case", choices=("all", *MATRIX_CASES), default="all")


class BackendFailure(RuntimeError):
    """A failed controller phase with its captured child diagnostics intact."""

    def __init__(self, message, result):
        super().__init__(message)
        self.result = result


class ParticipantFailure(RuntimeError):
    """A failed participant startup/cleanup with its captured diagnostics intact."""

    def __init__(self, message, result):
        super().__init__(message)
        self.result = result


def participant_command(directory, no_dedupe=False, fail_undo=False):
    command = [sys.executable, str(HERE / "participant.py"), str(directory / "participant.db")]
    if no_dedupe:
        command.append("--no-dedupe")
    if fail_undo:
        command.append("--fail-undo")
    return command


class ParticipantProcess:
    """Own participant startup, bounded port handshake, teardown and diagnostics."""

    def __init__(self, result, command, timeout_seconds=5):
        self.result = result
        self.command = command
        self.timeout_seconds = timeout_seconds
        self.process = None
        self.lines = queue.Queue()
        self.stdout_lines = []
        self.stderr_parts = []
        self.reader_errors = []
        self.stderr_errors = []
        self.reader = None
        self.stderr_reader = None
        self.stopped = False
        self.diagnostics = result.setdefault("participant", {
            "pid": None, "exit": None, "reaped": True, "killed": False,
            "cleanup_terminated": False, "port": None, "stdout": [], "stderr": "",
            "stdout_reader_completed": True, "stderr_reader_completed": True,
            "stdout_reader_error": None, "stderr_reader_error": None,
            "controller_error": None, "cleanup_error": None,
        })

    def __enter__(self):
        try:
            self.process = subprocess.Popen(self.command, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                            text=True, bufsize=1)
            self.diagnostics["pid"] = self.process.pid
            self.diagnostics["reaped"] = False
            self.result["participant_pid"] = self.process.pid
            self.reader = threading.Thread(target=self._read_stdout, daemon=True)
            self.stderr_reader = threading.Thread(target=self._read_stderr, daemon=True)
            self.reader.start()
            self.stderr_reader.start()
            try:
                kind, payload = self.lines.get(timeout=self.timeout_seconds)
            except queue.Empty as error:
                raise TimeoutError(f"Participant port handshake timed out after {self.timeout_seconds}s") from error
            if kind == "reader_error":
                raise RuntimeError(f"Participant stdout reader failed: {payload}") from payload
            if kind == "eof":
                raise RuntimeError("Participant closed stdout before reporting its port")
            try:
                message = json.loads(payload)
            except json.JSONDecodeError as error:
                raise RuntimeError(f"Participant startup output was not JSON: {error}") from error
            port = message.get("port") if isinstance(message, dict) else None
            if isinstance(port, bool) or not isinstance(port, int) or not 1 <= port <= 65535:
                raise RuntimeError(f"Participant startup reported an invalid port: {port!r}")
            self.diagnostics["port"] = port
            return f"http://127.0.0.1:{port}/"
        except BaseException as error:
            message = f"{type(error).__name__}: {error}"
            self.diagnostics["controller_error"] = message
            try:
                self._stop()
            except BaseException as cleanup_error:
                self.diagnostics["cleanup_error"] = f"{type(cleanup_error).__name__}: {cleanup_error}"
            if not isinstance(error, Exception):
                raise
            raise ParticipantFailure(message, self.result) from error

    def __exit__(self, error_type, error, traceback):
        self._stop()
        if error_type is None and self.diagnostics["cleanup_error"]:
            raise ParticipantFailure(self.diagnostics["cleanup_error"], self.result)
        return False

    def _read_stdout(self):
        try:
            for line in self.process.stdout:
                self.stdout_lines.append(line.rstrip("\r\n"))
                self.lines.put(("line", line))
        except Exception as error:
            self.reader_errors.append(f"{type(error).__name__}: {error}")
            self.lines.put(("reader_error", error))
        finally:
            self.lines.put(("eof", None))

    def _read_stderr(self):
        try:
            self.stderr_parts.append(self.process.stderr.read())
        except Exception as error:
            self.stderr_errors.append(f"{type(error).__name__}: {error}")

    def _stop(self):
        if self.stopped:
            return
        self.stopped = True
        cleanup_errors = []
        process = self.process
        if process is not None:
            if process.poll() is None:
                self.diagnostics["cleanup_terminated"] = True
                try:
                    process.terminate()
                except ProcessLookupError:
                    # The owned child exited after poll; wait below still reaps it.
                    pass
                except OSError as error:
                    cleanup_errors.append(f"Could not terminate participant child: {error}")
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self.diagnostics["killed"] = True
                try:
                    process.kill()
                except ProcessLookupError:
                    # The owned child exited before the escalation; wait below still reaps it.
                    pass
                except OSError as error:
                    cleanup_errors.append(f"Could not kill participant child: {error}")
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired as error:
                    cleanup_errors.append(f"Participant child was not reaped after kill: {error}")
            for reader in (self.reader, self.stderr_reader):
                if reader is not None and reader.ident is not None:
                    reader.join(timeout=5)
                    if reader.is_alive():
                        cleanup_errors.append(f"Participant {reader.name} did not finish after child reaping")
            for stream in (process.stdout, process.stderr):
                if stream is not None:
                    try:
                        stream.close()
                    except (OSError, ValueError):
                        # A reader or an exited child may already have closed this pipe.
                        pass
            for reader in (self.reader, self.stderr_reader):
                if reader is not None and reader.ident is not None and reader.is_alive():
                    reader.join(timeout=1)
            self.diagnostics.update({
                "exit": process.returncode,
                "reaped": process.poll() is not None,
                "stdout": self.stdout_lines,
                "stderr": "".join(self.stderr_parts),
                "stdout_reader_completed": self.reader is None or self.reader.ident is None or not self.reader.is_alive(),
                "stderr_reader_completed": self.stderr_reader is None or self.stderr_reader.ident is None or not self.stderr_reader.is_alive(),
                "stdout_reader_error": self.reader_errors[0] if self.reader_errors else None,
                "stderr_reader_error": self.stderr_errors[0] if self.stderr_errors else None,
            })
            self.result["participant_stderr"] = self.diagnostics["stderr"]
            if self.reader_errors:
                cleanup_errors.append(f"Participant stdout reader failed: {self.reader_errors[0]}")
            if self.stderr_errors:
                cleanup_errors.append(f"Participant stderr reader failed: {self.stderr_errors[0]}")
        self.diagnostics["cleanup_error"] = "; ".join(cleanup_errors) or None


def ledger(directory):
    with sqlite3.connect(directory / "participant.db") as db:
        db.row_factory = sqlite3.Row
        return {
            "operations": [dict(row) for row in db.execute("SELECT * FROM operations ORDER BY slot")],
            "calls": [dict(row) for row in db.execute("SELECT * FROM calls ORDER BY sequence")],
        }


def snapshot(directory):
    with sqlite3.connect(directory / "elsa.db") as db:
        tables = [row[0] for row in db.execute("SELECT name FROM sqlite_master WHERE type='table'")]
        table = next((name for name in tables if name.endswith("WorkflowInstances")), None)
        if table is None:
            return {"tables": tables, "state": None}
        db.row_factory = sqlite3.Row
        rows = [dict(row) for row in db.execute(f'SELECT * FROM "{table}"')]
        for row in rows:
            data = row.pop("Data", None)
            row["dataSha256"] = hashlib.sha256((data or "").encode()).hexdigest()
            row["snapshot"] = json.loads(data) if data else None
        return {"table": table, "rows": rows}


def backend(directory, url, kill_at=None, ambient=False, handoff="Compensated", mode="run",
            timeout_seconds=90, command_override=None):
    command = command_override or [dotnet_host(), str(DLL), mode, str(directory), url]
    if command_override is None:
        if mode == "run":
            command += ["--barriers"]
        if ambient:
            command += ["--ambient"]
    environment = dict(os.environ, E1_HANDOFF=handoff)
    try:
        process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                   text=True, bufsize=1, env=environment)
    except Exception as error:
        result = {"pid": None, "exit": None, "reaped": True, "killed": False, "cleanup_terminated": False,
                  "events": [], "stderr": "", "stdout_reader_completed": True,
                  "stderr_reader_completed": True, "stdout_reader_error": None, "stderr_reader_error": None,
                  "controller_error": f"{type(error).__name__}: {error}"}
        raise BackendFailure(result["controller_error"], result) from error
    lines = queue.Queue()
    reader_errors = []
    stderr_parts = []
    stderr_errors = []

    def read_lines():
        try:
            for line in process.stdout:
                lines.put(("line", line))
        except Exception as error:
            reader_errors.append(f"{type(error).__name__}: {error}")
            lines.put(("reader_error", error))
        finally:
            lines.put(("eof", None))

    def read_stderr():
        try:
            stderr_parts.append(process.stderr.read())
        except Exception as error:
            stderr_errors.append(f"{type(error).__name__}: {error}")

    reader = threading.Thread(target=read_lines, daemon=True)
    stderr_reader = threading.Thread(target=read_stderr, daemon=True)
    reader.start()
    stderr_reader.start()
    events = []
    deadline = time.monotonic() + timeout_seconds  # timeout is failure detection, never correctness synchronization
    killed = False
    controller_error = None
    cleanup_terminated = False
    try:
        while True:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise TimeoutError(f"Backend timeout after {timeout_seconds}s")
            try:
                kind, payload = lines.get(timeout=min(1, remaining))
            except queue.Empty:
                continue
            if kind == "eof":
                try:
                    process.wait(timeout=max(0, deadline - time.monotonic()))
                except subprocess.TimeoutExpired as error:
                    raise TimeoutError(f"Backend did not exit after closing stdout within {timeout_seconds}s") from error
                break
            if kind == "reader_error":
                raise RuntimeError(f"Backend stdout reader failed: {payload}") from payload
            event = json.loads(payload)
            events.append(event)
            if event["kind"] == "barrier":
                if event["value"]["name"] == kill_at:
                    if process.poll() is None:
                        process.kill()  # SIGKILL: no Dispose/finally/graceful drain
                    killed = True
                    break
                process.stdin.write("continue\n")
                process.stdin.flush()
    except Exception as error:
        controller_error = f"{type(error).__name__}: {error}"
    finally:
        if process.poll() is None:
            cleanup_terminated = True
            try:
                process.kill()  # Only the child created by this controller is ever terminated.
            except ProcessLookupError:
                # The owned child exited between the poll and kill calls.
                pass
        try:
            process.stdin.close()
        except (BrokenPipeError, OSError):
            # A terminated child's input pipe may already be closed.
            pass
        try:
            process.wait(timeout=15)
        except subprocess.TimeoutExpired:
            cleanup_terminated = True
            try:
                process.kill()
            except ProcessLookupError:
                # The owned child exited before the cleanup retry.
                pass
            process.wait()
        reader.join(timeout=5)
        stderr_reader.join(timeout=5)
        if reader.is_alive() and controller_error is None:
            controller_error = "RuntimeError: Backend stdout reader did not finish after child reaping"
        if stderr_reader.is_alive() and controller_error is None:
            controller_error = "RuntimeError: Backend stderr reader did not finish after child reaping"
        if reader_errors and controller_error is None:
            controller_error = f"Backend stdout reader failed: {reader_errors[0]}"
        if stderr_errors and controller_error is None:
            controller_error = f"Backend stderr reader failed: {stderr_errors[0]}"
        for stream in (process.stdout, process.stderr):
            try:
                stream.close()
            except (OSError, ValueError):
                # Preserve phase diagnostics when a teardown pipe is already closed.
                pass
        reader.join(timeout=1)
        stderr_reader.join(timeout=1)

    result = {
        "pid": process.pid,
        "exit": process.returncode,
        "reaped": process.poll() is not None,
        "killed": killed,
        "cleanup_terminated": cleanup_terminated,
        "events": events,
        "stderr": "".join(stderr_parts),
        "stdout_reader_completed": not reader.is_alive(),
        "stderr_reader_completed": not stderr_reader.is_alive(),
        "stdout_reader_error": reader_errors[0] if reader_errors else None,
        "stderr_reader_error": stderr_errors[0] if stderr_errors else None,
        "controller_error": controller_error,
    }
    if stderr_errors and result["controller_error"] is None:
        result["controller_error"] = f"Backend stderr reader failed: {stderr_errors[0]}"
    if kill_at and not killed and result["controller_error"] is None:
        result["controller_error"] = f"Backend did not reach requested barrier {kill_at!r}"
    if result["controller_error"]:
        raise BackendFailure(result["controller_error"], result)
    return result


def run_backend_phase(result, phase, *args, **kwargs):
    """Attach a failed backend phase before propagating its structured error."""
    try:
        phase_result = backend(*args, **kwargs)
    except BackendFailure as error:
        result[phase] = error.result
        raise
    except Exception as error:
        phase_result = {"pid": None, "exit": None, "reaped": True, "killed": False,
                        "cleanup_terminated": False, "events": [], "stderr": "",
                        "stdout_reader_completed": True, "stderr_reader_completed": True,
                        "stdout_reader_error": None, "stderr_reader_error": None,
                        "controller_error": f"{type(error).__name__}: {error}"}
        result[phase] = phase_result
        raise BackendFailure(phase_result["controller_error"], phase_result) from error
    result[phase] = phase_result
    return phase_result


def run_case(name, kill_at=None, ambient=False, no_dedupe=False, fail_undo=False, handoff="Compensated", restart_handoff=None):
    directory = args.output / name
    if directory.exists():
        raise RuntimeError(f"Refusing to replace evidence directory {directory}")
    directory.mkdir()
    result = {"case": name, "kill_at": kill_at, "participant_pid": None, "participant_stderr": "",
              "configuration": {"ambient": ambient, "no_dedupe": no_dedupe, "fail_undo": fail_undo,
                                "handoff": handoff, "restart_handoff": restart_handoff}}
    try:
        with ParticipantProcess(result, participant_command(directory, no_dedupe, fail_undo)) as url:
            run_backend_phase(result, "first", directory, url, kill_at, ambient, handoff)
            result["after_first"] = {"elsa": snapshot(directory), "participant": ledger(directory)}
            if kill_at:
                run_backend_phase(result, "restart", directory, url, handoff=restart_handoff or handoff)
            result["final"] = {"elsa": snapshot(directory), "participant": ledger(directory)}
            final_events = result.get("restart", result["first"])["events"]
            final = next((event["value"] for event in final_events if event["kind"] == "result"), None)
            result["observed_result"] = final
            result["classification"] = validate_matrix(result)
    except Exception as error:
        result["assertion_failure"] = str(error) or type(error).__name__
    finally:
        participant_error = result.get("participant", {}).get("controller_error")
        if participant_error:
            result.setdefault("assertion_failure", participant_error)
        (directory / "evidence.json").write_text(json.dumps(result, indent=2))
    print(json.dumps({"case": name, "observed": result.get("observed_result"), "assertion_failure": result.get("assertion_failure"), "evidence": str(directory / "evidence.json")}))
    return "assertion_failure" not in result


cases = MATRIX_CASES

def main(argv=None):
    global args
    args = parser.parse_args(argv)
    args.output.mkdir(parents=True, exist_ok=True)
    failed = False
    for case, parameters in cases.items():
        if args.case in ("all", case):
            failed = not run_case(case, **parameters) or failed
    return 1 if failed else 0

if __name__ == "__main__":
    sys.exit(main())
