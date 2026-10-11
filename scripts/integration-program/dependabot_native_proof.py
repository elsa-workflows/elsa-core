"""Opt-in #8636 native diagnostic; delete after its inventory evidence is accepted.

Raw native output/logs stay in RUNNER_TEMP. Only the closed summary is uploaded.
No feed credentials, updater proposals, or hosted-equivalence claim are supported.
"""
from collections import Counter
import argparse
import hashlib
import math
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

TARGET = 'cc7ee0bd6a1ceb306e3dbadae1a4f3bf83e4efea'
TREE = 'e10dd15f97def82b832e95855d65d37760633be2'
UPSTREAM = 'd3bc6c74cbd106d79d24a728ab623a3eaa776087'
SUBMODULES = {'nuget/helpers/lib/NuGet.Client': '626dc64e95d6f5bc366cbbe191a15751ccbfb097',
              'nuget/helpers/lib/dotnet-core': '843c037496b2941d8bc42d0eceaabf8d15a9deff'}
SDK = '10.0.400'
RETAINED = 'extensions/src/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj'
ERROR_TYPES = {'unknown_error', 'dependency_file_not_parseable', 'dependency_file_not_found',
               'private_source_authentication_failure', 'private_source_bad_response',
               'private_source_timed_out', 'dependency_not_found', 'out_of_disk', 'bad_requirement'}

CONFIGURATION_BLOB = '83e586893f4e58a5f938db815d5478afeb5651b0'
MODES = ('enumerate', 'tooling', 'retained-probe', 'product-smoke')
SMOKE_PROJECTS = ('extensions/src/email/Elsa.Email/Elsa.Email.csproj',
                  'studio/src/framework/Elsa.Studio.Core/Elsa.Studio.Core.csproj')
SMOKE_TFMS = ('net8.0', 'net9.0', 'net10.0')
DEPENDENCY_TYPES = {'Unknown', 'PackagesConfig', 'PackageReference', 'PackageVersion',
                    'GlobalPackageReference', 'DotNetTool', 'MSBuildSdk'}
EVALUATION_RESULTS = {'Success', 'PropertyIgnored', 'CircularReference', 'PropertyNotFound'}
UNIT_IDS = ('core-production', 'core-tests', 'extensions', 'studio', 'tooling')
UNIT_SELECTORS = (['/core/src/**/*'], ['/core/test/**/Elsa.*'], ['/extensions/**/*'],
                  ['/studio/**/*'], ['/build', '/.config'])
UNIT_COUNTS = (114, 68, 100, 73, 1)
TOOL_MANIFEST = '.config/dotnet-tools.json'
VERSION = re.compile(r'[0-9][0-9A-Za-z.+\-\[\](),* ]{0,100}')


def units_from_configuration(data):
    # A fixed git blob admits exactly the reviewed YAML schema, not a general YAML parser.
    blob = hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()
    if blob != CONFIGURATION_BLOB:
        raise ValueError('configuration_pin_mismatch')
    blocks = data.decode().split('  - package-ecosystem: "nuget"\n')[1:]
    units = []
    for block in blocks:
        section = block.split('    directories:\n', 1)[1].split('    target-branch:', 1)[0]
        selectors = re.findall(r'^      - "([^"\n]+)"$', section, re.MULTILINE)
        units.append(selectors)
    if units != list(UNIT_SELECTORS):
        raise ValueError('configuration_schema_mismatch')
    return [{'Id': unit, 'Selectors': selectors} for unit, selectors in zip(UNIT_IDS, units)]


def unit_projects(expected):
    prefixes = ('core/src/', 'core/test/', 'extensions/', 'studio/', 'build/')
    result = {unit: sorted(path for path in expected if path.startswith(prefix))
              for unit, prefix in zip(UNIT_IDS, prefixes)}
    if tuple(map(len, result.values())) != UNIT_COUNTS or len(set().union(*map(set, result.values()))) != 356:
        raise ValueError('unit_inventory_changed')
    return result


def open_regular(path, root):
    """Walk from the physical filesystem root without following any component link."""
    root = Path(os.path.abspath(root))
    path = Path(os.path.abspath(path))
    relative = path.relative_to(root)
    parts = root.parts[1:]+relative.parts
    descriptor = os.open(os.sep, os.O_RDONLY | os.O_DIRECTORY)
    try:
        for index, part in enumerate(parts):
            flags = os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK
            if index < len(parts)-1:
                flags |= os.O_DIRECTORY
            child = os.open(part, flags, dir_fd=descriptor)
            os.close(descriptor)
            descriptor = child
        if not stat.S_ISREG(os.fstat(descriptor).st_mode):
            raise ValueError('unsafe_output')
        stream = os.fdopen(descriptor, 'rb')
        descriptor = None
        return stream
    finally:
        if descriptor is not None:
            os.close(descriptor)


def read_closed_json(path, root, limit=4*1024*1024):
    with open_regular(path, root) as stream:
        data = stream.read(limit+1)
    if len(data) > limit:
        raise ValueError('output_limit')
    return json.loads(data)


def reconcile_enumeration(state, expected, tracked):
    groups = unit_projects(expected)
    known_directories = {str(parent) for path in tracked for parent in PurePosixPath(path).parents if str(parent) != '.'}
    rows, starts, invalid = [], [], 0
    for index, unit in enumerate(UNIT_IDS):
        try:
            value = read_closed_json(state/f'enumeration-{index}.json', state)
            directories, entries = value['Directories'], value['EntryPoints']
            if value['Id'] != unit or not isinstance(directories, list) or not isinstance(entries, list):
                raise ValueError('invalid_enumeration')
            safe_directories = []
            for directory in directories:
                if (not isinstance(directory, str) or not directory.startswith('/')
                        or directory[1:] not in known_directories):
                    raise ValueError('invalid_enumeration')
                safe_directories.append(directory[1:])
            if len(safe_directories) != len(set(safe_directories)):
                raise ValueError('invalid_enumeration')
            if any(not isinstance(entry, str) or entry not in tracked for entry in entries):
                raise ValueError('invalid_enumeration')
            if any(str(PurePosixPath(entry).parent) not in safe_directories for entry in entries):
                raise ValueError('invalid_enumeration')
            direct = sorted(path for path in tracked if str(PurePosixPath(path).parent) in safe_directories
                            and PurePosixPath(path).suffix.lower() in ('.csproj', '.sln', '.slnx', '.proj', '.fsproj', '.vbproj'))
            if sorted(entries) != direct or (unit == 'tooling' and safe_directories != ['build', '.config']):
                raise ValueError('invalid_enumeration')
            projects = sorted(entry for entry in entries if entry.endswith('.csproj'))
            gaps = sorted(entry for entry in entries if not entry.endswith('.csproj'))
            good = projects == groups[unit] and not gaps and len(entries) == len(set(entries))
            rows.append({'unit': unit, 'directories': safe_directories, 'entry_projects': projects,
                         'other_entrypoint_gaps': gaps, 'status': 'matched' if good else 'mismatch'})
            starts.extend(projects)
        except (OSError, ValueError, KeyError, TypeError):
            invalid += 1
            rows.append({'unit': unit, 'status': 'missing_or_invalid'})
    return {'units': rows, 'complete': not invalid and all(row['status'] == 'matched' for row in rows)
            and len(starts) == len(set(starts)) == 356 and set(starts) == set(expected),
            'invalid_units': invalid}


def probe_workspaces(mode):
    if mode == 'product-smoke':
        return [str(PurePosixPath(path).parent) for path in SMOKE_PROJECTS]
    if mode == 'tooling':
        return ['build', '.config']
    if mode == 'retained-probe':
        return [str(PurePosixPath(RETAINED).parent)]
    if mode == 'enumerate':
        return []
    raise ValueError('unknown_mode')


def smoke_project(index):
    if type(index) is not int or index not in (0, 1):
        raise ValueError('unknown_smoke_project')
    return SMOKE_PROJECTS[index]


def known_snapshot_path(value, snapshot, tracked, base=''):
    if not isinstance(value, str):
        return None
    if value.startswith('/'):
        try:
            path = str(Path(os.path.abspath(value)).relative_to(snapshot))
        except ValueError:
            return None
    else:
        path = normalized(base, value.replace('\\', '/'))
    return path if path in tracked else None


def smoke_native(state, index, snapshot, tracked, package_ids):
    project = smoke_project(index)
    workspace = str(PurePosixPath(project).parent)
    result = {'path': project, 'status': 'missing', 'records': [], 'invalid_records': 0}
    try:
        value = read_closed_json(state/f'workspace-{index}.json', state)
        if (value.get('Path', '').lstrip('/') != workspace or not isinstance(value.get('Projects'), list)
                or len(value['Projects']) > len(tracked)):
            raise ValueError('invalid_workspace')
        result['workspace_success'] = value.get('IsSuccess') is True and value.get('Error') is None
        result['workspace_errors'] = safe_errors(value.get('Error'))
        for row in value['Projects']:
            path = normalized(workspace, row.get('FilePath'))
            if path not in tracked or not path.endswith('.csproj'):
                result['invalid_records'] += 1
                continue
            parent = str(PurePosixPath(path).parent)
            arrays = ('TargetFrameworks', 'Dependencies', 'ImportedFiles', 'ReferencedProjectPaths')
            if any(not isinstance(row.get(key), list) or len(row[key]) > 10000 for key in arrays):
                raise ValueError('invalid_record')
            tfms = row['TargetFrameworks']
            if any(tfm not in SMOKE_TFMS for tfm in tfms):
                raise ValueError('invalid_framework')
            dependencies, omitted_transitive, unsafe_inputs = [], 0, 0
            for dep in row['Dependencies']:
                if (not isinstance(dep, dict) or not isinstance(dep.get('Name'), str)
                        or dep.get('Type') not in DEPENDENCY_TYPES
                        or type(dep.get('IsTopLevel')) is not bool
                        or not isinstance(dep.get('Version'), str) or not VERSION.fullmatch(dep['Version'])
                        or not isinstance(dep.get('TargetFrameworks'), list)
                        or any(tfm not in SMOKE_TFMS for tfm in dep['TargetFrameworks'])):
                    unsafe_inputs += 1
                    continue
                if dep['Name'] not in package_ids:
                    if dep['IsTopLevel'] is False:
                        omitted_transitive += 1
                    else:
                        unsafe_inputs += 1
                    continue
                dependency = {key: dep[key] for key in ('Name', 'Version', 'Type', 'TargetFrameworks', 'IsTopLevel')}
                evaluation = dep.get('EvaluationResult')
                if evaluation is None:
                    dependency['EvaluationResult'] = {'present': False}
                elif (isinstance(evaluation, dict) and isinstance(evaluation.get('ResultType'), str)
                        and evaluation['ResultType'] in EVALUATION_RESULTS):
                    dependency['EvaluationResult'] = {'present': True, 'result_type': evaluation['ResultType']}
                else:
                    unsafe_inputs += 1
                dependencies.append(dependency)
            imports, references = [], []
            for key, destination in (('ImportedFiles', imports), ('ReferencedProjectPaths', references)):
                for raw in row[key]:
                    known = known_snapshot_path(raw, snapshot, tracked, parent)
                    if known is None:
                        unsafe_inputs += 1
                    else:
                        destination.append(known)
            result['records'].append({'path': path, 'success': row.get('IsSuccess') is True and row.get('Error') is None,
                'errors': safe_errors(row.get('Error')), 'target_frameworks': tfms, 'dependencies': dependencies,
                'imports': imports, 'references': references,
                'central_file': known_snapshot_path(row.get('PackageManagementSpecialFileRelativePath'), snapshot, tracked, parent),
                'omitted_transitive_dependencies': omitted_transitive, 'unsafe_input_fields': unsafe_inputs})
        matches = [row for row in result['records'] if row['path'] == project]
        result['status'] = ('observed' if result['workspace_success'] and len(matches) == 1 and matches[0]['success']
                            and set(matches[0]['target_frameworks']) == set(SMOKE_TFMS)
                            and matches[0]['central_file'] is not None
                            and matches[0]['central_file'].endswith('/Directory.Packages.props')
                            and all(record['success'] and not record['unsafe_input_fields'] for record in result['records'])
                            and not result['invalid_records'] else 'failed_or_empty')
    except (OSError, ValueError, KeyError, TypeError, AttributeError):
        result['status'] = 'missing_or_invalid'
    return result


def smoke_evaluation(state, index, tfm, snapshot, tracked, package_ids):
    project = smoke_project(index)
    result = {'path': project, 'tfm': tfm, 'status': 'missing_or_invalid'}
    try:
        raw = read_closed_json(state/f'evaluation-{index}-{tfm}.json', state)
        properties, items = raw['Properties'], raw['Items']
        if (tfm not in SMOKE_TFMS or properties['TargetFramework'] != tfm
                or set(properties['TargetFrameworks'].split(';')) != set(SMOKE_TFMS)
                or properties['ManagePackageVersionsCentrally'].lower() != 'true'):
            raise ValueError('evaluation_applicability')
        central = known_snapshot_path(properties['DirectoryPackagesPropsPath'], snapshot, tracked)
        if central is None or not central.endswith('/Directory.Packages.props'):
            raise ValueError('evaluation_central')
        parsed, implicit_references = {}, 0
        for key in ('PackageVersion', 'PackageReference', 'GlobalPackageReference', 'ProjectReference'):
            rows = items[key]
            if not isinstance(rows, list) or len(rows) > 10000:
                raise ValueError('evaluation_items')
            parsed[key] = []
            for row in rows:
                if not isinstance(row, dict):
                    raise ValueError('evaluation_item')
                if key == 'PackageReference' and row.get('IsImplicitlyDefined', '').lower() == 'true':
                    implicit_references += 1
                    continue
                if key == 'ProjectReference':
                    path = known_snapshot_path(row.get('FullPath'), snapshot, tracked)
                    if path is None or not path.endswith('.csproj'):
                        raise ValueError('evaluation_reference')
                    parsed[key].append({'path': path})
                    continue
                name = row.get('Identity')
                if not isinstance(name, str) or name not in package_ids:
                    raise ValueError('evaluation_package')
                value = {'id': name}
                for field in ('Version', 'VersionOverride'):
                    version = row.get(field, '')
                    if not isinstance(version, str) or version and not VERSION.fullmatch(version):
                        raise ValueError('evaluation_version')
                    value[field] = version or None
                defining = known_snapshot_path(row.get('DefiningProjectFullPath'), snapshot, tracked)
                if defining is None:
                    raise ValueError('evaluation_definition')
                value['defined_in'] = defining
                parsed[key].append(value)
        central_versions = {}
        for row in parsed['PackageVersion']:
            central_versions.setdefault(row['id'], []).append(row['Version'])
        direct = []
        for kind, row in ((kind, row) for kind in ('PackageReference', 'GlobalPackageReference') for row in parsed[kind]):
            versions = central_versions.get(row['id'], [])
            global_reference = kind == 'GlobalPackageReference'
            if (not global_reference and row['Version']
                    or not (row['VersionOverride'] or global_reference and row['Version'])
                    and (len(versions) != 1 or not versions[0])):
                raise ValueError('evaluation_ambiguous_version')
            direct.append({'id': row['id'], 'version': row['VersionOverride'] or (row['Version'] if global_reference else None) or versions[0],
                           'type': 'GlobalPackageReference' if global_reference else 'PackageReference'})
        if not direct:
            raise ValueError('evaluation_empty')
        result.update(status='evaluated', cpm=True, central_file=central, items=parsed, direct_versions=direct,
                      implicit_reference_count=implicit_references)
    except (OSError, ValueError, KeyError, TypeError, AttributeError):
        pass
    return result


def product_smoke_readback(state, snapshot, expected, tracked, versions):
    package_ids = set().union(*(set(rows) for rows in versions.values()))
    rows = []
    for index, project in enumerate(SMOKE_PROJECTS):
        native = smoke_native(state, index, snapshot, tracked, package_ids)
        evaluations = [smoke_evaluation(state, index, tfm, snapshot, tracked, package_ids) for tfm in SMOKE_TFMS]
        row = {'path': project, 'native': native, 'evaluations': evaluations, 'status': 'unresolved'}
        matches = [record for record in native['records'] if record['path'] == project]
        if native['status'] == 'observed' and all(value['status'] == 'evaluated' for value in evaluations):
            record = matches[0]
            gaps = []
            if any(item['unsafe_input_fields'] for item in native['records']):
                gaps.append({'kind': 'unsafe_input_or_closure'})
            if any(not item['success'] for item in native['records']):
                gaps.append({'kind': 'closure_record_failed'})
            for evaluation in evaluations:
                tfm = evaluation['tfm']
                if evaluation['central_file'] != record['central_file'] or expected[project] not in record['imports']:
                    gaps.append({'tfm': tfm, 'kind': 'central_identity'})
                references = [item['path'] for item in evaluation['items']['ProjectReference']]
                if Counter(references) != Counter(record['references']):
                    gaps.append({'tfm': tfm, 'kind': 'reference_edges'})
                for dependency in evaluation['direct_versions']:
                    found = [dep for dep in record['dependencies'] if dep['Name'] == dependency['id']
                             and dep['Version'] == dependency['version'] and tfm in dep['TargetFrameworks']
                             and dep['IsTopLevel'] and dep['Type'] in (
                                 ('GlobalPackageReference', 'PackageReference') if dependency['type'] == 'GlobalPackageReference'
                                 else ('PackageReference', 'PackageVersion'))]
                    if len(found) != 1:
                        gaps.append({'tfm': tfm, 'kind': 'native_version_attribution', 'id': dependency['id']})
            row.update(status='matched' if not gaps else 'unresolved', gaps=gaps)
        rows.append(row)
    return {'projects': rows, 'semantic_preflight_complete': all(row['status'] == 'matched' for row in rows),
            'selected_denominator': 356, 'product_denominator': 173, 'acceptance': False}


def smoke_can_continue(row):
    """Known aggregate attribution loss is evidence to retain, not an execution failure."""
    return (row['native']['status'] == 'observed'
            and all(value['status'] == 'evaluated' for value in row['evaluations'])
            and all(gap['kind'] == 'native_version_attribution' for gap in row.get('gaps', [])))


def probe_readback(state, mode, expected, versions, tracked, tool_version=None):
    calls, workspaces = [], []
    for index, workspace in enumerate(probe_workspaces(mode)):
        call = {'index': index, 'workspace': workspace, 'state': 'not_read'}
        try:
            value = read_closed_json(state/f'workspace-{index}.call.json', state, 4096)
            elapsed = value['ElapsedSeconds']
            if (value['Index'] != index or value['Workspace'].lstrip('/') != workspace
                    or value['State'] not in ('started', 'returned', 'threw') or type(elapsed) not in (int, float)
                    or not math.isfinite(elapsed) or not 0 <= elapsed <= 1800):
                raise ValueError('invalid_call')
            call.update(state=value['State'], elapsed_seconds=round(elapsed, 3))
        except (OSError, ValueError, KeyError, TypeError, AttributeError):
            pass
        calls.append(call)
        try:
            value = read_closed_json(state/f'workspace-{index}.json', state)
            if (not isinstance(value, dict) or not isinstance(value.get('Path'), str)
                    or value['Path'].lstrip('/') != workspace or not isinstance(value.get('Projects'), list)
                    or len(value['Projects']) > len(tracked)):
                raise ValueError('invalid_workspace')
            for row in value['Projects']:
                if not isinstance(row, dict):
                    raise ValueError('invalid_workspace')
                for key in ('Dependencies', 'ImportedFiles', 'TargetFrameworks'):
                    if not isinstance(row.get(key, []), list) or len(row.get(key, [])) > 10000:
                        raise ValueError('invalid_workspace')
            workspaces.append(value)
        except (OSError, ValueError, TypeError, AttributeError):
            workspaces.append({'Path': workspace, 'IsSuccess': False, 'Projects': []})
    result = reconcile(expected, workspaces, versions, tracked)
    # This deliberately retains the old literal-version reconciler's limited meaning.
    result['complete'] = False
    return {'calls': calls, 'reconciliation': result,
            'root_tool': safe_tool_result(workspaces, tool_version),
            'all_calls_returned': bool(calls) and all(call['state'] == 'returned' for call in calls)}


def safe_tool_result(workspaces, target_version=None):
    observed, per_workspace = [], []
    for workspace in workspaces:
        receipt = {'workspace': workspace['Path'].lstrip('/'), 'status': 'not_observed'}
        per_workspace.append(receipt)
        tool = workspace.get('DotNetToolsJson')
        if not isinstance(tool, dict) or normalized(workspace.get('Path', ''), tool.get('FilePath')) != TOOL_MANIFEST:
            continue
        dependencies = tool.get('Dependencies', [])
        if not isinstance(dependencies, list) or len(dependencies) > 1000:
            continue
        for dependency in dependencies:
            if (isinstance(dependency, dict) and dependency.get('Name') == 'dotnet-ef'
                    and isinstance(dependency.get('Version'), str) and VERSION.fullmatch(dependency['Version'])):
                observation = {'id': 'dotnet-ef', 'version': dependency['Version'],
                               'success': tool.get('IsSuccess') is True, 'errors': safe_errors(tool.get('Error')),
                               'matches_target_version': target_version is not None and dependency['Version'] == target_version}
                observed.append(observation)
                receipt.update(status='observed', path=TOOL_MANIFEST, **observation)
    return {'path': TOOL_MANIFEST, 'target_version': target_version, 'observations': observed,
            'workspaces': per_workspace, 'status': 'observed' if observed else 'not_observed'}


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
    return [kind if isinstance(kind, str) and kind in ERROR_TYPES else 'unclassified_native_error']


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
                if isinstance(name, str) and name in central_versions and (version is None or isinstance(version, str)
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
    paths = list(snapshot.rglob('*'))
    if any(p.is_symlink() for p in paths):
        raise ValueError('snapshot_symlink_escape')
    files = [p for p in paths if p.is_file()]
    for path in files:
        with open_regular(path, snapshot):
            pass
    tracked = {str(p.relative_to(snapshot)) for p in files}
    all_projects = {p for p in tracked if p.endswith('.csproj')}
    with open_regular(snapshot/'Elsa.sln', snapshot) as stream:
        solution = {p.replace('\\', '/') for p in re.findall(r'"([^"\r\n]+\.csproj)"', stream.read().decode())}
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
            with open_regular(central, snapshot) as stream:
                xml = ET.parse(stream).getroot()
            versions = list(xml.iter('PackageVersion'))
            if versions:
                break
            imports = list(xml.iter('Import'))
            if len(imports) != 1 or '$' in imports[0].get('Project', ''):
                raise ValueError('unhandled_central_import')
            central = Path(os.path.abspath(central.parent / imports[0].attrib['Project'].replace('\\', '/')))
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


def digest(path, root):
    with open_regular(path, root) as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def recheck_content(root, before, can_finalize):
    changed = []
    for path, sha in before.items():
        if not can_finalize():
            return 'not_checked_deadline', changed
        try:
            same = digest(root/path, root) == sha
        except (OSError, ValueError):
            same = False
        if not same:
            changed.append(path)
    return 'checked', sorted(changed)


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
    def __init__(self, snapshot, expected, workspace_count=2, log_root=None):
        self.log_root = log_root if log_root is not None else snapshot
        self.workspace_count = workspace_count
        self.offset, self.fragment, self.gaps = 0, b'', 0
        self.identity = None
        self.discard_fragment = False
        self.events, self.dropped = [], 0
        self.projects = {str(snapshot.resolve()/path): path for path in expected}
        self.last = None

    def parse(self, line):
        workspace = re.fullmatch(r'ELSA8636_WORKSPACE_([0-9]+)_(STARTED|RETURNED|THREW)', line)
        if workspace and int(workspace[1]) < self.workspace_count:
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
        try:
            with open_regular(path, self.log_root) as stream:
                metadata = os.fstat(stream.fileno())
                size, identity = metadata.st_size, (metadata.st_dev, metadata.st_ino)
                if not stat.S_ISREG(metadata.st_mode):
                    return self.last
                if size < self.offset or self.identity is not None and identity != self.identity:
                    self.offset, self.fragment = 0, b''
                    self.discard_fragment = False
                    self.gaps += 1
                self.identity = identity
                stream.seek(self.offset)
                data = stream.read(65536)
                self.offset += len(data)
            lines = (self.fragment+data).split(b'\n')
            fragment = lines.pop()
            if self.discard_fragment:
                if lines:
                    lines.pop(0)
                    self.discard_fragment = False
                else:
                    fragment = b''
            self.fragment = fragment
            if len(fragment) > 4096:
                self.gaps += 1
                self.fragment = b''
                self.discard_fragment = True
            for raw in lines:
                if len(raw) > 4096:
                    self.gaps += 1
                    continue
                event = self.parse(raw.decode('utf-8', errors='replace').rstrip('\r'))
                if event is None:
                    continue
                if 'workspace_index' not in event and self.last and 'workspace_index' in self.last:
                    event['workspace_index'] = self.last['workspace_index']
                self.last = dict(event, observed_seconds=round(elapsed, 3))
                self.events.append(self.last)
                if len(self.events) > 1024:
                    self.events.pop(0)
                    self.dropped += 1
        except (OSError, ValueError):
            self.gaps += 1
        return self.last


def child_environment(state):
    """Pass only execution necessities; never inherit credentials or runner secrets."""
    directories = {name: state/path for name, path in {
        'HOME': 'home', 'DOTNET_CLI_HOME': 'dotnet-home', 'NUGET_PACKAGES': 'packages',
        'NUGET_HTTP_CACHE_PATH': 'http-cache', 'TMPDIR': 'tmp'}.items()}
    for path in directories.values():
        path.mkdir(mode=0o700)
    return dict({name: str(path) for name, path in directories.items()},
        PATH=os.environ.get('PATH', os.defpath), LANG='C.UTF-8',
        TMP=str(directories['TMPDIR']), TEMP=str(directories['TMPDIR']),
        DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', DOTNET_MULTILEVEL_LOOKUP='0',
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', GIT_CONFIG_NOSYSTEM='1',
        GIT_CONFIG_GLOBAL=os.devnull, GIT_TERMINAL_PROMPT='0')


class DeadlineRunner:
    """Run closed stages in isolated process groups under one shared elapsed budget."""
    STAGES = {'proof_identity', 'target_identity', 'target_tree', 'archive', 'extract',
              'source_init', 'source_remote', 'source_fetch', 'source_checkout',
              'upstream_identity', 'upstream_tree', 'submodules', 'nuget_identity',
              'dotnet_identity', 'sdk_identity', 'source_files', 'official_build',
              'caller_build', 'native_discovery', 'native_smoke_0', 'native_smoke_1', 'product_evaluation', 'native_enumeration', 'inventory', 'snapshot_hashes', 'native_source_hashes', 'caller_hashes'}

    def __init__(self, state, summary, budget_seconds=1800, grace_seconds=5,
                 progress_seconds=15, reserve_seconds=60):
        self.state, self.summary = state, summary
        self.started = time.monotonic()
        self.deadline = self.started + budget_seconds - reserve_seconds
        self.finalization_deadline = self.started + budget_seconds - 15
        self.readback = None
        self.environment = child_environment(state)
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
            if name in ('native_discovery', 'native_smoke_0', 'native_smoke_1') and self.native_progress is not None:
                native_progress = self.native_progress.poll(self.state/(name+'.out'), row['elapsed_seconds'])
                if native_progress is not None:
                    self.summary['native_progress'] = native_progress
                    receipt['native_progress'] = native_progress
                self.summary['native_event_journal'] = {'events': self.native_progress.events, 'gaps': self.native_progress.gaps, 'dropped': self.native_progress.dropped}
            if name in ('native_discovery', 'native_smoke_0', 'native_smoke_1', 'product_evaluation') and self.readback is not None:
                self.readback()
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
                                                    stderr=log, start_new_session=True, env=self.environment)
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
            if name in capture:
                with open_regular(self.state/(name+'.out'), self.state) as stream:
                    return stream.read()
            return b''
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


def run_preflight(checkout, state, mode):
    if mode not in MODES:
        raise ValueError('unknown_mode')
    # Resolve only trusted launch roots once (e.g. macOS /var -> /private/var).
    checkout = checkout.resolve(strict=True)
    state = state.parent.resolve(strict=True)/state.name
    state.mkdir(parents=True, exist_ok=False)
    summary = {'target': TARGET, 'target_tree': TREE, 'upstream': UPSTREAM, 'submodules': SUBMODULES,
               'sdk_required': SDK, 'experiments': {'mode': 'default_diagnostic',
               'nuget_generate_simple_pr_body': False, 'nuget_find_root_directory': False},
               'mode': mode, 'configuration_blob': CONFIGURATION_BLOB,
               'feed_authentication': 'not_supplied_unknown', 'acceptance': False,
               'all_selected_native_evaluation_complete': False, 'managed_acceptance': False,
               'remaining_gaps': ['complete_per_tfm_central_evaluation', 'reference_closure_duplicates',
                                  'configured_feed_resolution', 'proposal_or_current_version', 'managed_helper_binary_equivalence'],
               'stages': []}
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
        summary['archive_sha256'] = digest(archive, state)
        snapshot = state / 'repository'
        snapshot.mkdir()
        run('extract', ['tar', '-xf', str(archive), '-C', str(snapshot)])
        expected, versions, tracked = runner.calculate('inventory', lambda: inventory(snapshot))
        with open_regular(snapshot/'.github/dependabot.yml', snapshot) as stream:
            units = units_from_configuration(stream.read())
        tool_version = read_closed_json(snapshot/TOOL_MANIFEST, snapshot)['tools']['dotnet-ef']['version']
        if not isinstance(tool_version, str) or not VERSION.fullmatch(tool_version):
            raise ValueError('tool_manifest_mismatch')
        summary['root_tool_target'] = {'path': TOOL_MANIFEST, 'id': 'dotnet-ef', 'version': tool_version}
        unit_projects(expected)
        (state/'units.json').write_text(json.dumps(units))
        summary['selected_dispositions'] = [{'path': path, 'expected_central': central, 'status': 'not_requested'} for path, central in sorted(expected.items())]
        summary['outside_projects'] = sorted(path for path in tracked if path.endswith('.csproj') and path not in expected)
        summary['extensions_studio_projects'] = sum(path.startswith(('extensions/', 'studio/')) for path in expected)
        before = runner.calculate('snapshot_hashes', lambda: {p: digest(snapshot / p, snapshot) for p in tracked})
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
        closure = runner.calculate('native_source_hashes', lambda: {p: digest(source/p, source) for p in source_files if p.startswith('nuget/helpers/lib/NuGetUpdater/')})
        summary['native_source_content_sha256'] = hashlib.sha256(json.dumps(closure, sort_keys=True).encode()).hexdigest()
        run('official_build', ['dotnet', 'build', 'NuGetUpdater.Cli/NuGetUpdater.Cli.csproj', '-c', 'Release', '--nologo'], native)
        caller = native / 'NativeDiscovery'
        caller.mkdir()
        helper = checkout / 'scripts/integration-program/dependabot-native-proof'
        shutil.copyfile(helper/'NativeDiscovery.csproj.template', caller/'NativeDiscovery.csproj')
        shutil.copyfile(helper/'Program.cs', caller/'Program.cs')
        summary['caller_sha256'] = {p.name: digest(p, checkout) for p in helper.iterdir() if p.is_symlink() or p.is_file()}
        run('caller_build', ['dotnet', 'build', str(caller/'NativeDiscovery.csproj'), '-c', 'Release', '-o', str(state/'caller'), '--nologo'], native)
        binaries = runner.calculate('caller_hashes', lambda: {p.name: digest(p, state) for p in (state/'caller').iterdir() if p.is_symlink() or p.is_file()})
        summary['caller_binary_content_sha256'] = hashlib.sha256(json.dumps(binaries, sort_keys=True).encode()).hexdigest()
        summary['reconstructed_binary_sha256'] = {name: binaries[name] for name in ('NativeDiscovery.dll', 'NuGetUpdater.Core.dll')}
        command = ['dotnet', str(state/'caller/NativeDiscovery.dll'), str(snapshot), str(state), str(state/'units.json')]
        run('native_enumeration', command+['enumerate'], native)
        summary['enumeration'] = reconcile_enumeration(state, expected, tracked)
        runner.save()
        if not summary['enumeration']['complete']:
            raise ValueError('enumeration_mismatch')
        if mode != 'enumerate':
            runner.native_progress = NativeProgress(snapshot, expected, len(probe_workspaces(mode)), log_root=state)
            def readback():
                summary['probe'] = probe_readback(state, mode, expected, versions, tracked, tool_version)
                summary['selected_dispositions'] = summary['probe']['reconciliation']['projects']
                if mode == 'product-smoke':
                    summary['product_smoke'] = product_smoke_readback(state, snapshot, expected, tracked, versions)
            runner.readback = readback
            if mode == 'product-smoke':
                for index in (0, 1):
                    project = smoke_project(index)
                    unit = summary['enumeration']['units'][2+index]
                    if project not in unit['entry_projects'] or str(PurePosixPath(project).parent) not in unit['directories']:
                        raise ValueError('product_smoke_incomplete')
                    run(f'native_smoke_{index}', command+[mode, str(index)], native)
                    if summary['product_smoke']['projects'][index]['native']['status'] != 'observed':
                        raise ValueError('product_smoke_incomplete')
                    for tfm in SMOKE_TFMS:
                        run('product_evaluation', ['dotnet', 'msbuild', str(snapshot/project), '-nologo',
                            '-property:TargetFramework='+tfm,
                            '-getProperty:TargetFramework,TargetFrameworks,ManagePackageVersionsCentrally,DirectoryPackagesPropsPath',
                            '-getItem:PackageVersion,PackageReference,GlobalPackageReference,ProjectReference'], native,
                            output=state/f'evaluation-{index}-{tfm}.json')
                        if summary['product_smoke']['projects'][index]['evaluations'][SMOKE_TFMS.index(tfm)]['status'] != 'evaluated':
                            raise ValueError('product_smoke_incomplete')
                    if not smoke_can_continue(summary['product_smoke']['projects'][index]):
                        raise ValueError('product_smoke_incomplete')
                    checks = {}
                    for label, root, hashes in (('target', snapshot, before), ('upstream', source, closure)):
                        status, changed = runner.calculate('snapshot_hashes', lambda: recheck_content(
                            root, hashes, lambda: time.monotonic() < runner.deadline))
                        checks[label] = {'status': status, 'changed_files': changed}
                    summary.setdefault('smoke_content_rechecks', []).append({'project_index': index, **checks})
                    runner.save()
                    if any(check['status'] != 'checked' or check['changed_files'] for check in checks.values()):
                        raise ValueError('product_smoke_incomplete')
            else:
                run('native_discovery', command+[mode], native)
    except Exception as error:
        # Details are deliberately excluded; stage exit codes and raw ephemeral logs diagnose failures.
        summary['preflight_error'] = str(error) if isinstance(error, ValueError) and str(error) in {
            'static_inventory_changed', 'central_file_missing', 'unhandled_central_import',
            'central_import_escapes_snapshot', 'central_import_cycle', 'target_tree_mismatch',
            'overall_deadline', 'stage_failed', 'submodule_pin_mismatch', 'sdk_pin_mismatch', 'snapshot_symlink_escape', 'upstream_pin_mismatch', 'configuration_pin_mismatch',
            'configuration_schema_mismatch', 'unit_inventory_changed', 'enumeration_mismatch', 'tool_manifest_mismatch', 'product_smoke_incomplete'} else 'preflight_exception'
    finally:
        # Read already-written files inside the existing reserve, after owned work has stopped.
        runner.stop()
        def can_finalize():
            return time.monotonic() < runner.finalization_deadline
        if 'expected' in locals() and can_finalize():
            if 'enumeration' not in summary:
                summary['enumeration'] = reconcile_enumeration(state, expected, tracked)
            if mode != 'enumerate':
                summary['probe'] = probe_readback(state, mode, expected, versions, tracked, locals().get('tool_version'))
                summary['selected_dispositions'] = summary['probe']['reconciliation']['projects']
                if mode == 'product-smoke':
                    summary['product_smoke'] = product_smoke_readback(state, snapshot, expected, tracked, versions)
            if 'before' in locals():
                status, changed = recheck_content(snapshot, before, can_finalize)
                summary['target_content_recheck'] = status
                summary['changed_target_files'] = changed
        else:
            summary['native_readback'] = 'not_checked_finalization_deadline'
        if 'closure' in locals():
            status, changed = recheck_content(source, closure, can_finalize)
            summary['upstream_content_recheck'] = status
            summary['changed_upstream_files'] = changed
        summary['attempt_complete'] = (not summary.get('preflight_error')
            and summary.get('enumeration', {}).get('complete') is True
            and summary.get('target_content_recheck') == 'checked' and summary.get('changed_target_files') == []
            and summary.get('upstream_content_recheck') == 'checked' and summary.get('changed_upstream_files') == []
            and (mode == 'enumerate' or summary.get('probe', {}).get('all_calls_returned') is True))
        # Completion here is only this bounded stage's execution/receipt completion.
        summary['diagnostic_complete'] = False
        runner.save()
        signal.setitimer(signal.ITIMER_REAL, 0)
        signal.signal(signal.SIGALRM, previous_handler)
    return 0 if summary['attempt_complete'] else 1


if __name__ == '__main__':
    try:
        class ClosedArgumentParser(argparse.ArgumentParser):
            def error(self, message):
                raise ValueError('unknown_mode')
        parser = ClosedArgumentParser()
        parser.add_argument('--mode', choices=MODES, required=True)
        arguments = parser.parse_args()
        code = run_preflight(Path.cwd(), Path(os.environ['RUNNER_TEMP'])/'dependabot-native-proof-8636', arguments.mode)
    except Exception:
        # A hard watchdog during finalization still leaves the last atomic safe receipt.
        code = 1
    raise SystemExit(code)
