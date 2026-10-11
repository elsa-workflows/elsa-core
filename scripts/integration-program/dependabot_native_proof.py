"""Opt-in #8636 native diagnostic; delete after its inventory evidence is accepted.

Raw native output/logs stay in RUNNER_TEMP. Only the closed summary is uploaded.
No feed credentials, updater proposals, or hosted-equivalence claim are supported.
"""
from collections import Counter
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import signal
import stat
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

TARGET = 'c0f470853bfa8fbf9c3266df75da48cb859f9799'
TREE = '51aa6e0938cd24732d8a285f701dc6b614b63a69'
UPSTREAM = '9c06d60057ba9e7e79210e6618f932a28cf6a158'
SUBMODULES = {'nuget/helpers/lib/NuGet.Client': '626dc64e95d6f5bc366cbbe191a15751ccbfb097',
              'nuget/helpers/lib/dotnet-core': '843c037496b2941d8bc42d0eceaabf8d15a9deff'}
SDK = '10.0.400'
RETAINED = 'extensions/src/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj'
ERROR_TYPES = {'unknown_error', 'dependency_file_not_parseable', 'dependency_file_not_found',
               'private_source_authentication_failure', 'private_source_bad_response',
               'private_source_timed_out', 'dependency_not_found', 'out_of_disk', 'bad_requirement'}


def normalized(base, value):
    """Reject paths outside the snapshot without exposing arbitrary input strings."""
    if not isinstance(value, str) or value.startswith('/') or '\\' in value:
        return None
    parts = []
    for part in PurePosixPath(base.lstrip('/'), value).parts:
        if part == '..':
            if not parts:
                return None
            parts.pop()
        elif part != '.':
            parts.append(part)
    return '/'.join(parts)


def safe_errors(error):
    if error is None:
        return []
    kind = error.get('error-type') if isinstance(error, dict) else None
    return [kind if kind in ERROR_TYPES else 'unclassified_native_error']


def reconcile(expected, workspaces, package_versions, tracked):
    """Join every selected project, keeping filtered/empty/failed native rows unverified."""
    records = {}
    workspace_rows = []
    invalid = 0
    for native in workspaces:
        if (not isinstance(native, dict) or not isinstance(native.get('Projects'), list)
                or not isinstance(native.get('Path', ''), str)):
            native = {'Path': '', 'IsSuccess': False, 'Error': {}, 'Projects': []}
            invalid += 1
        workspace = native.get('Path', '')
        failed = native.get('IsSuccess') is not True or native.get('Error') is not None
        workspace_rows.append({'index': len(workspace_rows), 'success': not failed,
                               'errors': safe_errors(native.get('Error')),
                               'record_count': len(native.get('Projects', []))})
        for row in native.get('Projects', []):
            if not isinstance(row, dict):
                invalid += 1
                continue
            path = normalized(workspace, row.get('FilePath'))
            if path not in tracked:
                invalid += 1
                continue
            records.setdefault(path, []).append((row, workspace, failed))
    projects = []
    for path, central in sorted(expected.items()):
        matches = records.get(path, [])
        result = {'path': path, 'expected_central': central, 'status': 'omitted'}
        if matches:
            row, workspace, workspace_failed = matches[0]
            # Native imports and central-file paths are relative to the project directory.
            project_directory = str(PurePosixPath(path).parent)
            imports = sorted({p for value in row.get('ImportedFiles', [])
                              if (p := normalized(project_directory, value)) in tracked})
            special = normalized(project_directory, row.get('PackageManagementSpecialFileRelativePath'))
            tfms = sorted({v for v in row.get('TargetFrameworks', [])
                           if isinstance(v, str) and re.fullmatch(r'net(?:standard|coreapp)?[0-9][a-zA-Z0-9.\-]*', v)})
            dependencies = []
            central_versions = package_versions.get(central, {})
            for dep in row.get('Dependencies', []):
                if not isinstance(dep, dict):
                    invalid += 1
                    continue
                name, version = dep.get('Name'), dep.get('Version')
                if name in central_versions and (version is None or isinstance(version, str)
                        and re.fullmatch(r'[0-9][0-9A-Za-z.+\-\[\](),* ]{0,100}', version)):
                    dependencies.append({'name': name, 'version': version,
                                         'matches_literal_central_version': version in central_versions[name]})
            status = 'observed'
            if len(matches) > 1:
                status = 'duplicate'
            elif row.get('IsSuccess') is not True or row.get('Error') is not None:
                status = 'project_failed'
            elif workspace_failed:
                status = 'workspace_failed'
            elif (central not in imports and central != special) or not tfms or not dependencies:
                status = 'incomplete_metadata'
            if status == 'observed' and any(not d['matches_literal_central_version'] for d in dependencies):
                status = 'version_requires_review'
            result.update(status=status, errors=safe_errors(row.get('Error')), target_frameworks=tfms,
                          imported_files=imports, central_file=special if special in tracked else None,
                          dependency_count=len(row.get('Dependencies', [])), dependencies=dependencies,
                          unreported_dependency_count=len(row.get('Dependencies', [])) - len(dependencies))
        projects.append(result)
    return {'projects': projects, 'workspaces': workspace_rows,
            'status_counts': dict(Counter(p['status'] for p in projects)),
            'invalid_record_count': invalid, 'unexpected_tracked_projects': sorted(set(records) - set(expected)),
            'complete': bool(projects) and all(p['status'] == 'observed' for p in projects)
                        and not invalid and all(w['success'] for w in workspace_rows)}


def inventory(snapshot):
    snapshot = snapshot.resolve()
    files = [p for p in snapshot.rglob('*') if p.is_file()]
    if any(not p.resolve().is_relative_to(snapshot) for p in files):
        raise ValueError('snapshot_symlink_escape')
    tracked = {str(p.relative_to(snapshot)) for p in files}
    all_projects = {p for p in tracked if p.endswith('.csproj')}
    solution = {p.replace('\\', '/') for p in re.findall(r'"([^"\r\n]+\.csproj)"',
                                                        (snapshot / 'Elsa.sln').read_text())}
    if len(all_projects) != 379 or len(solution) != 355 or not solution <= all_projects or RETAINED not in all_projects:
        raise ValueError('static_inventory_changed')
    expected, package_versions = {}, {}
    for path in sorted(solution | {RETAINED}):
        parent = (snapshot / path).parent
        while not (parent / 'Directory.Packages.props').exists():
            if parent == snapshot:
                raise ValueError('central_file_missing')
            parent = parent.parent
        central = parent / 'Directory.Packages.props'
        visited = set()
        while central not in visited:
            visited.add(central)
            xml = ET.parse(central).getroot()
            versions = list(xml.iter('PackageVersion'))
            if versions:
                break
            imports = list(xml.iter('Import'))
            if len(imports) != 1 or '$' in imports[0].get('Project', ''):
                raise ValueError('unhandled_central_import')
            central = (central.parent / imports[0].attrib['Project'].replace('\\', '/')).resolve()
            if not central.is_relative_to(snapshot):
                raise ValueError('central_import_escapes_snapshot')
        if not versions:
            raise ValueError('central_import_cycle')
        expected[path] = str(central.relative_to(snapshot))
        central_versions = package_versions.setdefault(expected[path], {})
        for item in versions:
            name = item.get('Include') or item.get('Update')
            value = item.get('Version') or item.findtext('Version')
            if name and value:
                central_versions.setdefault(name, set()).add(value)
    return expected, package_versions, tracked


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def diagnostic_complete(summary):
    """Content checks must finish explicitly even if native work finished before deadline."""
    return (not summary.get('preflight_error') and not summary.get('content_recheck')
            and summary.get('reconciliation', {}).get('complete') is True
            and summary.get('target_content_recheck') == 'checked'
            and summary.get('upstream_content_recheck') == 'checked'
            and summary.get('changed_target_files') == []
            and summary.get('changed_upstream_files') == [])


class NativeProgress:
    """Project only pinned logger markers; never copy native log text into a receipt."""
    def __init__(self, snapshot, expected):
        self.projects = {str(snapshot.resolve()/path): path for path in expected}
        self.last = None

    def parse(self, line):
        workspace = re.fullmatch(r'ELSA8636_WORKSPACE_([01])_(STARTED|RETURNED|THREW)', line)
        if workspace:
            return {'workspace_index': int(workspace[1]), 'phase': workspace[2].lower()}
        message = re.fullmatch(r'[0-9]{4}/[0-9]{2}/[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2} INFO (.*)', line)
        if not message:
            return None
        for absolute, relative in self.projects.items():
            if message[1] == 'Performing single restore for project '+absolute:
                return {'phase': 'single_restore_selected', 'project': relative}
            prefix = 'Performing individual restores for project '+absolute+' using target frameworks '
            if message[1].startswith(prefix) and re.fullmatch(
                    r'net(?:standard|coreapp)?[0-9][A-Za-z0-9.\-]*(?:, net(?:standard|coreapp)?[0-9][A-Za-z0-9.\-]*)*',
                    message[1][len(prefix):]):
                return {'phase': 'individual_restores_selected', 'project': relative}
        return None

    def poll(self, path, elapsed):
        # Bounded tail reads keep diagnostics cheap even when raw subprocess output grows.
        try:
            descriptor = os.open(path, os.O_RDONLY | os.O_NONBLOCK | os.O_NOFOLLOW)
            with os.fdopen(descriptor, 'rb') as stream:
                if not stat.S_ISREG(os.fstat(stream.fileno()).st_mode):
                    return self.last
                stream.seek(0, 2)
                start = max(0, stream.tell()-65536)
                stream.seek(start)
                data = stream.read(65536)
            lines = data.split(b'\n')
            if start:
                lines = lines[1:]  # the first fragment may start in an arbitrary raw message
            previous = {k: v for k, v in (self.last or {}).items() if k != 'observed_seconds'}
            latest = previous
            for raw in lines[:-1]:  # only complete lines are eligible
                event = self.parse(raw.decode('utf-8', errors='replace').rstrip('\r'))
                if event is not None:
                    if 'workspace_index' not in event and 'workspace_index' in latest:
                        event['workspace_index'] = latest['workspace_index']
                    latest = event
            if latest and latest != previous:
                self.last = dict(latest, observed_seconds=round(elapsed, 3))
        except OSError:
            pass
        return self.last


class DeadlineRunner:
    """Run closed stages in isolated process groups under one shared elapsed budget."""
    STAGES = {'proof_identity', 'target_identity', 'target_tree', 'archive', 'extract',
              'source_init', 'source_remote', 'source_fetch', 'source_checkout',
              'upstream_identity', 'upstream_tree', 'submodules', 'nuget_identity',
              'dotnet_identity', 'sdk_identity', 'source_files', 'official_build',
              'caller_build', 'native_discovery', 'inventory', 'snapshot_hashes', 'native_source_hashes', 'caller_hashes'}

    def __init__(self, state, summary, budget_seconds=1800, grace_seconds=5,
                 progress_seconds=15, reserve_seconds=60):
        self.state, self.summary = state, summary
        self.started = time.monotonic()
        self.deadline = self.started + budget_seconds - reserve_seconds
        self.grace, self.progress = grace_seconds, progress_seconds
        self.process = None
        self.native_progress = None
        summary['runner_budget_seconds'] = budget_seconds
        self.save()

    def save(self):
        temporary = self.state/'summary.tmp'
        temporary.write_text(json.dumps(self.summary, indent=2)+'\n')
        temporary.replace(self.state/'summary.json')

    def check(self):
        if time.monotonic() >= self.deadline:
            raise ValueError('overall_deadline')

    def stop(self):
        if self.process is None:
            return
        # Kill the group even if its original leader has exited and left descendants.
        for sig in (signal.SIGTERM, signal.SIGKILL):
            try:
                os.killpg(self.process.pid, sig)
            except ProcessLookupError:
                pass
            if sig == signal.SIGTERM:
                try:
                    self.process.wait(timeout=self.grace)
                except subprocess.TimeoutExpired:
                    pass
        self.process.wait(timeout=self.grace)

    def calculate(self, name, operation):
        if name not in self.STAGES:
            raise ValueError('unknown_stage')
        row = {'name': name, 'state': 'running', 'elapsed_seconds': 0}
        self.summary['stages'].append(row)
        started = time.monotonic()
        self.save()
        print(json.dumps({'stage': name, 'state': 'running', 'elapsed_seconds': 0}), flush=True)
        try:
            self.check()
            result = operation()
            self.check()
            row['state'] = 'passed'
            return result
        except BaseException:
            row['state'] = 'interrupted'
            raise
        finally:
            row['elapsed_seconds'] = round(time.monotonic()-started, 3)
            self.save()
            print(json.dumps({'stage': name, 'state': row['state'], 'elapsed_seconds': row['elapsed_seconds']}), flush=True)

    def run(self, name, args, cwd=None, output=None):
        if name not in self.STAGES:
            raise ValueError('unknown_stage')
        started = time.monotonic()
        row = {'name': name, 'state': 'running', 'elapsed_seconds': 0}
        self.summary['stages'].append(row)
        def progress():
            row['elapsed_seconds'] = round(time.monotonic()-started, 3)
            receipt = {'stage': name, 'state': row['state'], 'elapsed_seconds': row['elapsed_seconds']}
            if name == 'native_discovery' and self.native_progress is not None:
                native_progress = self.native_progress.poll(self.state/'native_discovery.out', row['elapsed_seconds'])
                if native_progress is not None:
                    self.summary['native_progress'] = native_progress
                    receipt['native_progress'] = native_progress
            self.save()
            print(json.dumps(receipt), flush=True)
        try:
            if started >= self.deadline:
                row['state'] = 'not_started_deadline'
                raise ValueError('overall_deadline')
            progress()
            with (self.state/(name+'.log')).open('wb') as log:
                with (output or self.state/(name+'.out')).open('wb') as stdout:
                    self.process = subprocess.Popen(args, cwd=cwd or self.state, stdout=stdout,
                                                    stderr=log, start_new_session=True)
                    while True:
                        remaining = self.deadline-time.monotonic()
                        if remaining <= 0:
                            row['state'] = 'timed_out'
                            raise ValueError('overall_deadline')
                        try:
                            code = self.process.wait(timeout=min(self.progress, remaining))
                            break
                        except subprocess.TimeoutExpired:
                            progress()
            row.update(state='passed' if code == 0 else 'failed', exit_code=code)
            if code:
                raise ValueError('stage_failed')
            capture = {'proof_identity', 'target_identity', 'target_tree', 'upstream_identity',
                       'upstream_tree', 'nuget_identity', 'dotnet_identity', 'sdk_identity', 'source_files'}
            return (self.state/(name+'.out')).read_bytes() if name in capture else b''
        except BaseException:
            if row['state'] == 'running':
                row['state'] = 'interrupted'
            self.stop()
            if self.process is not None:
                row['exit_code'] = self.process.returncode
            raise
        finally:
            if self.process is not None:
                row['process_group_cleanup'] = 'term_then_kill'
            self.stop()
            progress()
            self.process = None


def run_preflight(checkout, state):
    state.mkdir(parents=True, exist_ok=False)
    summary = {'target': TARGET, 'target_tree': TREE, 'upstream': UPSTREAM, 'submodules': SUBMODULES,
               'sdk_required': SDK, 'experiments': {'mode': 'default_diagnostic',
               'nuget_generate_simple_pr_body': False, 'nuget_find_root_directory': False},
               'feed_authentication': 'not_supplied_unknown', 'acceptance': False, 'stages': []}
    runner = DeadlineRunner(state, summary)
    run = runner.run
    def hard_deadline(signum, frame):
        summary.update(preflight_error='overall_deadline', diagnostic_complete=False)
        runner.stop()
        runner.save()
        raise ValueError('overall_deadline')
    previous_handler = signal.signal(signal.SIGALRM, hard_deadline)
    signal.setitimer(signal.ITIMER_REAL, 1790)
    try:
        summary['proof_head'] = run('proof_identity', ['git', 'rev-parse', 'HEAD'], checkout).decode().strip()
        actual_target = run('target_identity', ['git', 'rev-parse', TARGET], checkout).decode().strip()
        if actual_target != TARGET:
            raise ValueError('target_tree_mismatch')
        summary['target_actual'] = actual_target
        tree = run('target_tree', ['git', 'rev-parse', TARGET + '^{tree}'], checkout).decode().strip()
        if tree != TREE:
            raise ValueError('target_tree_mismatch')
        archive = state / 'target.tar'
        run('archive', ['git', 'archive', '--format=tar', TARGET], checkout, output=archive)
        summary['archive_sha256'] = digest(archive)
        snapshot = state / 'repository'
        snapshot.mkdir()
        run('extract', ['tar', '-xf', str(archive), '-C', str(snapshot)])
        expected, versions, tracked = runner.calculate('inventory', lambda: inventory(snapshot))
        before = runner.calculate('snapshot_hashes', lambda: {p: digest(snapshot / p) for p in tracked})
        summary.update(tracked_projects=379, solution_projects=355, selected_projects=len(expected),
                       snapshot_content_sha256=hashlib.sha256(json.dumps(before, sort_keys=True).encode()).hexdigest())
        source = state / 'upstream'
        run('source_init', ['git', 'init', str(source)])
        run('source_remote', ['git', 'remote', 'add', 'origin', 'https://github.com/dependabot/dependabot-core.git'], source)
        run('source_fetch', ['git', 'fetch', '--depth=1', 'origin', UPSTREAM], source)
        run('source_checkout', ['git', 'checkout', '--detach', UPSTREAM], source)
        actual_upstream = run('upstream_identity', ['git', 'rev-parse', 'HEAD'], source).decode().strip()
        if actual_upstream != UPSTREAM:
            raise ValueError('upstream_pin_mismatch')
        summary['upstream_actual'] = actual_upstream
        summary['upstream_tree'] = run('upstream_tree', ['git', 'rev-parse', 'HEAD^{tree}'], source).decode().strip()
        run('submodules', ['git', 'submodule', 'update', '--init', '--depth=1'], source)
        summary['submodules_actual'] = {}
        for path, pin in SUBMODULES.items():
            actual = run('nuget_identity' if 'NuGet.Client' in path else 'dotnet_identity', ['git', 'rev-parse', 'HEAD'], source/path).decode().strip()
            summary['submodules_actual'][path] = actual
            if actual != pin:
                raise ValueError('submodule_pin_mismatch')
        native = source / 'nuget/helpers/lib/NuGetUpdater'
        actual_sdk = run('sdk_identity', ['dotnet', '--version'], native).decode().strip()
        if actual_sdk != SDK:
            raise ValueError('sdk_pin_mismatch')
        summary['sdk_actual'] = actual_sdk
        source_files = run('source_files', ['git', 'ls-files', '-z'], source).decode().split('\0')
        closure = runner.calculate('native_source_hashes', lambda: {p: digest(source/p) for p in source_files if p.startswith('nuget/helpers/lib/NuGetUpdater/') and (source/p).is_file()})
        summary['native_source_content_sha256'] = hashlib.sha256(json.dumps(closure, sort_keys=True).encode()).hexdigest()
        run('official_build', ['dotnet', 'build', 'NuGetUpdater.Cli/NuGetUpdater.Cli.csproj', '-c', 'Release', '--nologo'], native)
        caller = native / 'NativeDiscovery'
        caller.mkdir()
        helper = checkout / 'scripts/integration-program/dependabot-native-proof'
        shutil.copyfile(helper/'NativeDiscovery.csproj.template', caller/'NativeDiscovery.csproj')
        shutil.copyfile(helper/'Program.cs', caller/'Program.cs')
        summary['caller_sha256'] = {p.name: digest(p) for p in helper.iterdir() if p.is_file()}
        run('caller_build', ['dotnet', 'build', str(caller/'NativeDiscovery.csproj'), '-c', 'Release', '-o', str(state/'caller'), '--nologo'], native)
        binaries = runner.calculate('caller_hashes', lambda: {p.name: digest(p) for p in (state/'caller').iterdir() if p.is_file()})
        summary['caller_binary_content_sha256'] = hashlib.sha256(json.dumps(binaries, sort_keys=True).encode()).hexdigest()
        runner.native_progress = NativeProgress(snapshot, expected)
        run('native_discovery', ['dotnet', str(state/'caller/NativeDiscovery.dll'), str(snapshot), str(state)], native)
    except Exception as error:
        # Details are deliberately excluded; stage exit codes and raw ephemeral logs diagnose failures.
        summary['preflight_error'] = str(error) if isinstance(error, ValueError) and str(error) in {
            'static_inventory_changed', 'central_file_missing', 'unhandled_central_import',
            'central_import_escapes_snapshot', 'central_import_cycle', 'target_tree_mismatch',
            'overall_deadline', 'stage_failed', 'submodule_pin_mismatch', 'sdk_pin_mismatch', 'snapshot_symlink_escape', 'upstream_pin_mismatch'} else 'preflight_exception'
    finally:
        deadline_readback = time.monotonic() >= runner.deadline
        if deadline_readback:
            summary['native_readback'] = 'not_checked_deadline'
        try:
            if deadline_readback:
                raise ValueError('readback_deadline')
            calls = json.loads((state/'calls.json').read_text())
            summary['native_calls_completed'] = calls if isinstance(calls, list) and len(calls) == 2 and all(type(v) is bool for v in calls) else None
        except (OSError, ValueError):
            summary['native_calls_completed'] = None
        workspaces = []
        for index in range(2):
            path = state / f'workspace-{index}.json'
            try:
                if deadline_readback:
                    raise ValueError('readback_deadline')
                workspaces.append(json.loads(path.read_text()))
            except (OSError, ValueError):
                workspaces.append({'Path': ['', 'extensions/src/Elsa.Testing.Extensions'][index],
                                   'IsSuccess': False, 'Error': None if deadline_readback else {'error-type': 'output_missing'}, 'Projects': []})
        if 'expected' in locals():
            summary['reconciliation'] = reconcile(expected, workspaces, versions, tracked)
            if deadline_readback:
                for project in summary['reconciliation']['projects']:
                    project['status'] = 'not_read_deadline'
                summary['reconciliation']['status_counts'] = {'not_read_deadline': len(expected)}
            if 'before' in locals() and time.monotonic() < runner.deadline:
                changed = [p for p, sha in before.items() if not (snapshot/p).is_file() or digest(snapshot/p) != sha]
                summary['changed_target_files'] = sorted(changed)
                summary['target_content_recheck'] = 'checked'
            else:
                summary['content_recheck'] = 'not_checked_deadline'
        if 'closure' in locals() and time.monotonic() < runner.deadline:
            summary['changed_upstream_files'] = [p for p, sha in closure.items() if not (source/p).is_file() or digest(source/p) != sha]
            summary['upstream_content_recheck'] = 'checked'
        summary['diagnostic_complete'] = diagnostic_complete(summary)
        runner.save()
        signal.setitimer(signal.ITIMER_REAL, 0)
        signal.signal(signal.SIGALRM, previous_handler)
    return 0 if summary['diagnostic_complete'] else 1


if __name__ == '__main__':
    try:
        code = run_preflight(Path.cwd(), Path(os.environ['RUNNER_TEMP'])/'dependabot-native-proof-8636')
    except Exception:
        # A hard watchdog during finalization still leaves the last atomic safe receipt.
        code = 1
    raise SystemExit(code)
