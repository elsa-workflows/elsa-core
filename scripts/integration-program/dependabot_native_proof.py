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
import subprocess
import sys
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


def normalized(workspace, value):
    """Reject paths outside the snapshot without exposing arbitrary input strings."""
    if not isinstance(value, str) or value.startswith('/') or '\\' in value:
        return None
    parts = []
    for part in PurePosixPath(workspace.lstrip('/'), value).parts:
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
            imports = sorted({p for value in row.get('ImportedFiles', [])
                              if (p := normalized(workspace, value)) in tracked})
            special = normalized(workspace, row.get('PackageManagementSpecialFileRelativePath'))
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


def run_preflight(checkout, state):
    state.mkdir(parents=True, exist_ok=False)
    summary = {'target': TARGET, 'target_tree': TREE, 'upstream': UPSTREAM, 'submodules': SUBMODULES,
               'sdk_required': SDK, 'experiments': {'mode': 'default_diagnostic',
               'nuget_generate_simple_pr_body': False, 'nuget_find_root_directory': False},
               'feed_authentication': 'not_supplied_unknown', 'acceptance': False, 'stages': []}
    def run(name, args, cwd=state):
        with (state / (name + '.log')).open('wb') as log:
            result = subprocess.run(args, cwd=cwd, stdout=log, stderr=subprocess.STDOUT, check=False)
        summary['stages'].append({'name': name, 'exit_code': result.returncode})
        if result.returncode:
            raise ValueError('stage_failed')
    try:
        summary['proof_head'] = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=checkout, text=True).strip()
        actual_target = subprocess.check_output(['git', 'rev-parse', TARGET], cwd=checkout, text=True).strip()
        if actual_target != TARGET:
            raise ValueError('target_tree_mismatch')
        summary['target_actual'] = actual_target
        tree = subprocess.check_output(['git', 'rev-parse', TARGET + '^{tree}'], cwd=checkout, text=True).strip()
        if tree != TREE:
            raise ValueError('target_tree_mismatch')
        archive = state / 'target.tar'
        with archive.open('wb') as output:
            subprocess.run(['git', 'archive', '--format=tar', TARGET], cwd=checkout, stdout=output, check=True)
        summary['archive_sha256'] = digest(archive)
        snapshot = state / 'repository'
        snapshot.mkdir()
        run('extract', ['tar', '-xf', str(archive), '-C', str(snapshot)])
        expected, versions, tracked = inventory(snapshot)
        before = {p: digest(snapshot / p) for p in tracked}
        summary.update(tracked_projects=379, solution_projects=355, selected_projects=len(expected),
                       snapshot_content_sha256=hashlib.sha256(json.dumps(before, sort_keys=True).encode()).hexdigest())
        source = state / 'upstream'
        run('source_init', ['git', 'init', str(source)])
        run('source_remote', ['git', 'remote', 'add', 'origin', 'https://github.com/dependabot/dependabot-core.git'], source)
        run('source_fetch', ['git', 'fetch', '--depth=1', 'origin', UPSTREAM], source)
        run('source_checkout', ['git', 'checkout', '--detach', UPSTREAM], source)
        actual_upstream = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=source, text=True).strip()
        if actual_upstream != UPSTREAM:
            raise ValueError('upstream_pin_mismatch')
        summary['upstream_actual'] = actual_upstream
        summary['upstream_tree'] = subprocess.check_output(['git', 'rev-parse', 'HEAD^{tree}'], cwd=source, text=True).strip()
        run('submodules', ['git', 'submodule', 'update', '--init', '--depth=1'], source)
        summary['submodules_actual'] = {}
        for path, pin in SUBMODULES.items():
            actual = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=source/path, text=True).strip()
            summary['submodules_actual'][path] = actual
            if actual != pin:
                raise ValueError('submodule_pin_mismatch')
        native = source / 'nuget/helpers/lib/NuGetUpdater'
        actual_sdk = subprocess.check_output(['dotnet', '--version'], cwd=native, text=True, stderr=subprocess.STDOUT).strip()
        if actual_sdk != SDK:
            raise ValueError('sdk_pin_mismatch')
        summary['sdk_actual'] = actual_sdk
        source_files = subprocess.check_output(['git', 'ls-files', '-z'], cwd=source, text=True).split('\0')
        closure = {p: digest(source/p) for p in source_files if p.startswith('nuget/helpers/lib/NuGetUpdater/') and (source/p).is_file()}
        summary['native_source_content_sha256'] = hashlib.sha256(json.dumps(closure, sort_keys=True).encode()).hexdigest()
        run('official_build', ['dotnet', 'build', 'NuGetUpdater.Cli/NuGetUpdater.Cli.csproj', '-c', 'Release', '--nologo'], native)
        caller = native / 'NativeDiscovery'
        caller.mkdir()
        helper = checkout / 'scripts/integration-program/dependabot-native-proof'
        shutil.copyfile(helper/'NativeDiscovery.csproj.template', caller/'NativeDiscovery.csproj')
        shutil.copyfile(helper/'Program.cs', caller/'Program.cs')
        summary['caller_sha256'] = {p.name: digest(p) for p in helper.iterdir() if p.is_file()}
        run('caller_build', ['dotnet', 'build', str(caller/'NativeDiscovery.csproj'), '-c', 'Release', '-o', str(state/'caller'), '--nologo'], native)
        binaries = {p.name: digest(p) for p in (state/'caller').iterdir() if p.is_file()}
        summary['caller_binary_content_sha256'] = hashlib.sha256(json.dumps(binaries, sort_keys=True).encode()).hexdigest()
        run('native_discovery', ['dotnet', str(state/'caller/NativeDiscovery.dll'), str(snapshot), str(state)], native)
    except Exception as error:
        # Details are deliberately excluded; stage exit codes and raw ephemeral logs diagnose failures.
        summary['preflight_error'] = str(error) if isinstance(error, ValueError) and str(error) in {
            'static_inventory_changed', 'central_file_missing', 'unhandled_central_import',
            'central_import_escapes_snapshot', 'central_import_cycle', 'target_tree_mismatch',
            'stage_failed', 'submodule_pin_mismatch', 'sdk_pin_mismatch', 'snapshot_symlink_escape', 'upstream_pin_mismatch'} else 'preflight_exception'
    finally:
        try:
            calls = json.loads((state/'calls.json').read_text())
            summary['native_calls_completed'] = calls if isinstance(calls, list) and len(calls) == 2 and all(type(v) is bool for v in calls) else None
        except (OSError, ValueError):
            summary['native_calls_completed'] = None
        workspaces = []
        for index in range(2):
            path = state / f'workspace-{index}.json'
            try:
                workspaces.append(json.loads(path.read_text()))
            except (OSError, ValueError):
                workspaces.append({'Path': ['', 'extensions/src/Elsa.Testing.Extensions'][index],
                                   'IsSuccess': False, 'Error': {'error-type': 'output_missing'}, 'Projects': []})
        if 'expected' in locals():
            summary['reconciliation'] = reconcile(expected, workspaces, versions, tracked)
            changed = [p for p, sha in before.items() if not (snapshot/p).is_file() or digest(snapshot/p) != sha]
            summary['changed_target_files'] = sorted(changed)
        if 'closure' in locals():
            summary['changed_upstream_files'] = [p for p, sha in closure.items() if not (source/p).is_file() or digest(source/p) != sha]
        summary['diagnostic_complete'] = (not summary.get('preflight_error')
            and summary.get('reconciliation', {}).get('complete', False)
            and not summary.get('changed_target_files') and not summary.get('changed_upstream_files'))
        (state/'summary.json').write_text(json.dumps(summary, indent=2)+'\n')
    return 0 if summary['diagnostic_complete'] else 1


if __name__ == '__main__':
    raise SystemExit(run_preflight(Path.cwd(), Path(os.environ['RUNNER_TEMP'])/'dependabot-native-proof-8636'))
