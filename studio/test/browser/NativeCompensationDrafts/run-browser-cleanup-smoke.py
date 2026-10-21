#!/usr/bin/env python3
"""Browser-only smoke for the draft fixture's private process teardown."""
import importlib.util
import argparse
import os
import shutil
import subprocess
import tempfile
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
CHILD = HERE / 'browser-cleanup-smoke.mjs'
SUPERVISOR = HERE / 'run-proof-skeleton.py'

spec = importlib.util.spec_from_file_location('proof_supervisor', SUPERVISOR)
proof = importlib.util.module_from_spec(spec)
spec.loader.exec_module(proof)


def run_case(mode, node, browser_cache, scratch_parent):
    scratch = Path(tempfile.mkdtemp(prefix=f'cleanup-{mode}-', dir=scratch_parent))
    os.chmod(scratch, 0o700)
    capture = scratch / 'browser-process-private.json'
    ownership = scratch / 'browser-process-ownership.json'
    env = {
        'PATH': os.environ.get('PATH', ''),
        'HOME': str(scratch),
        'TMPDIR': str(scratch),
        'PLAYWRIGHT_BROWSERS_PATH': str(browser_cache),
    }
    process = subprocess.Popen(
        [str(node), str(CHILD), str(capture), mode],
        cwd=HERE, env=env, stdin=subprocess.DEVNULL,
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        start_new_session=True,
    )
    runner = proof.process_info(process.pid)
    if runner is None or runner['pgid'] != process.pid or runner['sid'] != process.pid:
        process.kill()
        process.wait()
        raise RuntimeError('runner identity unverified')
    runner_birth = runner['birth']
    deadline = time.monotonic() + (2.5 if mode == 'hold' else 30)
    last_mtime = None
    timed_out = False
    try:
        while True:
            if capture.is_file():
                current_mtime = capture.stat().st_mtime_ns
                if current_mtime != last_mtime:
                    proof.refresh_browser_ownership(capture, ownership, process.pid, runner_birth)
                    last_mtime = current_mtime
            if process.poll() is not None:
                if capture.is_file():
                    proof.refresh_browser_ownership(capture, ownership, process.pid, runner_birth)
                break
            if time.monotonic() >= deadline:
                timed_out = True
                if capture.is_file():
                    proof.refresh_browser_ownership(capture, ownership, process.pid, runner_birth)
                if mode != 'hold':
                    raise RuntimeError('normal browser smoke timed out')
                break
            time.sleep(0.05)

        if not capture.is_file():
            raise RuntimeError('browser CDP capture absent')
        if timed_out and not proof.terminate_verified_browser_processes(ownership, process.pid, runner_birth, process):
            raise RuntimeError('owned browser process teardown not confirmed')
        if not timed_out and process.poll() is None:
            raise RuntimeError('normal runner did not exit')
        if timed_out and process.poll() is None:
            raise RuntimeError('timed out runner remains alive')
        if not proof.terminate_verified_browser_processes(ownership, process.pid, runner_birth, process):
            raise RuntimeError('browser process groups remain or ownership is unverified')
        if not proof.process_groups_gone([('smoke', process)]):
            raise RuntimeError('runner process group remains')
        shutil.rmtree(scratch)
        return 'forced-timeout cleanup verified' if timed_out else 'normal cleanup verified'
    except Exception:
        # Keep the private scratch for diagnosis whenever teardown is uncertain.
        raise


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--node', type=Path, required=True)
    parser.add_argument('--browser-cache', type=Path, required=True)
    parser.add_argument('--scratch-parent', type=Path, required=True)
    args = parser.parse_args()
    if not all(path.is_absolute() for path in (args.node, args.browser_cache, args.scratch_parent)):
        raise SystemExit('absolute fixture paths required')
    node = args.node.resolve(strict=True)
    browser_cache = args.browser_cache.resolve(strict=True)
    scratch_parent = args.scratch_parent.resolve(strict=True)
    source_root = next((path for path in (HERE, *HERE.parents) if (path / '.git').exists()), HERE)
    if scratch_parent.is_relative_to(source_root) or not scratch_parent.is_dir():
        raise SystemExit('scratch must be outside source')
    if not node.is_file() or not os.access(node, os.X_OK) or not browser_cache.is_dir():
        raise SystemExit('private browser smoke prerequisites unavailable')
    for mode in ('normal', 'hold'):
        print(run_case(mode, node, browser_cache, scratch_parent))
