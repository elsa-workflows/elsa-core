#!/usr/bin/env python3
"""Core-controlled, exact-source maintenance rehearsal. Never publishes."""
from __future__ import annotations

import argparse
from contextlib import nullcontext
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tempfile
import traceback
import xml.etree.ElementTree as ET
import zipfile

from prove_consolidated_packages import (archive_names, capture_sdk_assets, capture_compiler_evidence, dependency_groups, framework_reference_groups,
    generated_family, metadata, only_abstract_methods, read_staged_nuspecs, require, restored_archive, restored_assets,
    run, source_url, verify_package_manifest, verify_sdk_assets, verify_external_document, verify_external_entry,
    verify_generator_identity, verify_tracked_document)

ROOT = Path(__file__).resolve().parents[2]
REGISTER = ROOT / 'docs/integration-program/maintenance-source-register.json'
CANDIDATES = ROOT / 'docs/integration-program/maintenance-core-candidates.json'
CONTAINMENT = ROOT / 'docs/integration-program/maintenance-containment.json'
CORE_REPOSITORY = 'elsa-workflows/elsa-core'
REHEARSAL_BRANCHES = {'refs/heads/codex/maintenance-builds-8677': 'original',
                      'refs/heads/codex/elsa-integration-maintenance-candidates-8683': 'core'}
PROOF_BUILD_PROPERTIES = {'EmbedUntrackedSources': 'true'}
TRX_COUNTERS = ('total', 'executed', 'passed', 'failed', 'error', 'timeout', 'aborted', 'inconclusive',
                'passedButRunAborted', 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending')


def load_register() -> dict:
    return json.loads(REGISTER.read_text())


def load_candidates() -> dict:
    return json.loads(CANDIDATES.read_text())


def validate_candidate_delta(delta: object) -> None:
    require(isinstance(delta, list) and bool(delta), 'Core candidate source delta mismatch')
    paths = []
    for change in delta:
        require(isinstance(change, dict) and set(change) == {'path', 'before', 'after'},
                'Core candidate source delta mismatch')
        path = change['path']
        require(isinstance(path, str) and bool(path) and not PurePosixPath(path).is_absolute() and
                '..' not in PurePosixPath(path).parts and PurePosixPath(path).as_posix() == path and
                not any(ord(char) < 32 or ord(char) == 127 or char in '\\?#%' for char in path),
                'Core candidate source delta mismatch')
        paths.append(path)
        require(change['before'] != change['after'], 'Core candidate source delta mismatch')
        for entry in (change['before'], change['after']):
            require(entry is None or isinstance(entry, dict) and set(entry) == {'mode', 'type', 'blob'} and
                    entry['mode'] in ('100644', '100755') and entry['type'] == 'blob' and
                    isinstance(entry['blob'], str) and re.fullmatch(r'[a-f0-9]{40}', entry['blob']) is not None,
                    'Core candidate source delta mismatch')
    require(paths == sorted(set(paths)), 'Core candidate source delta mismatch')


def registered_core_candidates(register: dict) -> list[dict]:
    candidates = load_candidates()
    require(set(candidates) == {'schema', 'candidates', 'rehearsal_sources', 'published', 'maintenance_refs_activated'} and
            candidates['schema'] == 2 and candidates['published'] is False and
            candidates['maintenance_refs_activated'] is False and isinstance(candidates['candidates'], list),
            'Core candidate register identity mismatch')
    rows = []
    common = {'product', 'line', 'source_kind', 'kind', 'source_repository', 'commit', 'tree', 'parents',
              'original_commit', 'original_parents', 'contained_commit', 'contained_tree'}
    for candidate in candidates['candidates']:
        require(isinstance(candidate, dict) and candidate.get('kind') in ('metadata-bridge', 'maintenance') and
                set(candidate) == common | ({'delta'} if candidate['kind'] == 'maintenance' else set()) and
                all(isinstance(candidate[key], str) and re.fullmatch(r'[a-f0-9]{40}', candidate[key])
                    for key in ('commit', 'tree', 'original_commit', 'contained_commit', 'contained_tree')) and
                all(isinstance(candidate[key], list) and candidate[key] and
                    all(isinstance(value, str) and re.fullmatch(r'[a-f0-9]{40}', value) for value in candidate[key])
                    for key in ('parents', 'original_parents')) and len(candidate['parents']) == 1,
                'Core candidate register identity mismatch')
        originals = [row for row in register['sources'] if
            (row['product'], row['line'], row['commit']) ==
            (candidate['product'], candidate['line'], candidate['original_commit'])]
        require(len(originals) == 1 and candidate['source_kind'] == 'core' and
                candidate['source_repository'] == CORE_REPOSITORY, 'Core candidate register identity mismatch')
        if candidate['kind'] == 'maintenance':
            validate_candidate_delta(candidate['delta'])
        original = originals[0]
        rows.append({**original, **candidate, 'workflows': [],
                     'original_source_repository': original['source_repository'],
                     'original_source_ref': original['source_ref']})
        rows[-1].pop('source_ref')
        rows[-1].pop('parent')
    by_commit = {row['commit']: row for row in rows}
    require(len(by_commit) == len(rows), 'Core candidate register identity mismatch')
    binding = ('product', 'line', 'source_repository', 'original_commit', 'original_parents',
               'contained_commit', 'contained_tree')
    for row in rows:
        seen = set()
        while row['kind'] == 'maintenance':
            require(row['commit'] not in seen, 'Core candidate parent chain mismatch')
            seen.add(row['commit'])
            parent = by_commit.get(row['parents'][0])
            require(parent is not None and all(parent[key] == row[key] for key in binding),
                    'Core candidate parent chain mismatch')
            row = parent
    return rows


def selection(register: dict, product: str, line: str, commit: str, version: str,
              source_kind: str = 'original') -> dict:
    require(source_kind in ('original', 'core'), 'Unknown source kind')
    rows = registered_core_candidates(register) if source_kind == 'core' else register['sources']
    matches = [row for row in rows
               if (row['product'], row['line'], row['commit']) == (product, line, commit)]
    require(len(matches) == 1 and re.fullmatch(r'[a-f0-9]{40}', commit), 'Unregistered product/line/source commit')
    # Proof-only identity: never reuse a released version or infer one from refs.
    require(isinstance(version, str) and re.fullmatch(re.escape(line) + r'\.(0|[1-9][0-9]*)-proof\.[1-9][0-9]*\.[1-9][0-9]*', version) is not None,
            'Expected same-line unpublished version MAJOR.MINOR.PATCH-proof.RUN.ATTEMPT')
    return matches[0]


def workflow_selections(register: dict, environment: dict[str, str]) -> list[dict]:
    """Select exact nonpublishing rehearsals; manual selections remain main-only."""
    event, ref = environment['EVENT'], environment['REF']
    if event == 'workflow_dispatch':
        require(ref == 'refs/heads/main', 'Manual maintenance proof requires main')
        row = selection(register, environment['PRODUCT'], environment['LINE'], environment['SOURCE_COMMIT'],
                        environment['PROOF_VERSION'], environment['SOURCE_KIND'])
        return [dict(row, version=environment['PROOF_VERSION'])]
    require(event == 'push' and ref in REHEARSAL_BRANCHES, 'Unregistered maintenance rehearsal event/ref')
    source_kind = REHEARSAL_BRANCHES[ref]
    if source_kind == 'core':
        admitted = registered_core_candidates(register)
        candidates = load_candidates()['rehearsal_sources']
        require(isinstance(candidates, list) and all(isinstance(row, dict) and
                set(row) == {'product', 'line', 'commit'} and isinstance(row['commit'], str) and
                re.fullmatch(r'[a-f0-9]{40}', row['commit']) is not None for row in candidates) and
                len({row['commit'] for row in candidates}) == len(candidates), 'Incomplete maintenance rehearsal cells')
        selected = []
        for candidate in candidates:
            matches = [row for row in admitted if all(row[key] == candidate[key] for key in candidate)]
            require(len(matches) == 1, 'Incomplete maintenance rehearsal cells')
            selected.append(matches[0])
        candidates = selected
    else:
        candidates = register['sources']
    expected_cells = {(row['product'], row['line']) for row in register['sources']}
    if source_kind == 'core':
        # Deliberate eight-source B+D checkpoint; never infer tips or build the registry.
        expected = {(product, line, kind) for product, line in expected_cells
                    for kind in ('metadata-bridge', 'maintenance')}
        require(len(candidates) == len(expected) and
                {(row['product'], row['line'], row['kind']) for row in candidates} == expected,
                'Incomplete maintenance rehearsal cells')
    else:
        require(len(candidates) == len(expected_cells) and
                {(row['product'], row['line']) for row in candidates} == expected_cells,
                'Incomplete maintenance rehearsal cells')
    rows = []
    for candidate in candidates:
        original_commit = candidate['original_commit'] if source_kind == 'core' else candidate['commit']
        originals = [row for row in register['sources'] if (row['product'], row['line'], row['commit']) ==
                     (candidate['product'], candidate['line'], original_commit)]
        require(len(originals) == 1, 'Core candidate register identity mismatch')
        version = f"{originals[0]['dependency_version']}-proof.{environment['GITHUB_RUN_ID']}.{environment['GITHUB_RUN_ATTEMPT']}"
        row = selection(register, candidate['product'], candidate['line'], candidate['commit'], version, source_kind)
        rows.append(dict(row, version=version))
    return rows


def build_selection(register: dict, selected: dict, environment: dict[str, str]) -> dict:
    """Re-admit a cached matrix row using the actual build attempt's version."""
    kind = selected.get('source_kind', 'original')
    if environment['EVENT'] == 'workflow_dispatch':
        require(environment['REF'] == 'refs/heads/main', 'Manual maintenance proof requires main')
        row = selection(register, selected['product'], selected['line'], selected['commit'], selected['version'], kind)
        return dict(row, version=selected['version'])
    matches = [row for row in workflow_selections(register, environment)
               if (row['product'], row['line'], row['commit'], row.get('source_kind', 'original')) ==
               (selected['product'], selected['line'], selected['commit'], kind)]
    require(len(matches) == 1, 'Unregistered maintenance rehearsal selection')
    return matches[0]


def build_environment() -> dict[str, str]:
    # Old build/npm lifecycle code receives no repository or registry authority.
    names = {'PATH', 'HOME', 'TMPDIR', 'DOTNET_ROOT', 'DOTNET_ROOT_X64', 'RUNNER_TEMP',
             'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_NOLOGO', 'NUGET_PACKAGES', 'CI', 'GITHUB_ACTIONS'}
    return {key: value for key, value in os.environ.items() if key in names} | PROOF_BUILD_PROPERTIES


def git(root: Path, *args: str, env: dict | None = None) -> str:
    return run(['git', *args], root, env=env or build_environment()).strip()


def verify_source(root: Path, row: dict) -> None:
    git(root, 'merge-base', '--is-ancestor', row['commit'], 'HEAD')
    if row.get('source_kind') == 'core':
        verify_core_candidate(root, row)
        return
    require(git(root, 'rev-parse', row['commit'] + '^{tree}') == row['tree'], 'Source tree mismatch')
    require(git(root, 'rev-parse', row['commit'] + '^') == row['parent'], 'Source parent mismatch')
    workflows = git(root, 'ls-tree', '-r', '--name-only', row['commit'], '--', '.github/workflows').splitlines()
    require(workflows == row['workflows'], 'Source workflow inventory changed')


def git_bytes(root: Path, commit: str, path: str) -> bytes:
    return subprocess.run(['git', 'show', f'{commit}:{path}'], cwd=root,
        env=build_environment(), check=True, capture_output=True, timeout=30).stdout


def tree_entries(root: Path, commit: str) -> dict:
    entries = git(root, 'ls-tree', '-rz', commit).split('\0')
    return {path: tuple(identity.split()) for entry in entries if entry
            for identity, path in [entry.split('\t', 1)]}


def verify_core_candidate(root: Path, row: dict) -> None:
    """Validate local objects; controller ancestry is separately required by verify_source."""
    register = load_register()
    admitted = selection(register, row['product'], row['line'], row['commit'],
                         row['dependency_version'] + '-proof.1.1', 'core')
    require(row == admitted, 'Core candidate register identity mismatch')
    by_commit = {item['commit']: item for item in registered_core_candidates(register)}
    while row['kind'] == 'maintenance':
        require(git(root, 'show', '-s', '--format=%P', row['commit']).split() == row['parents'] and
                git(root, 'rev-parse', row['commit'] + '^{tree}') == row['tree'], 'Core candidate graph mismatch')
        parent = by_commit[row['parents'][0]]
        verify_maintenance_delta(root, row, parent, register)
        row = parent
    original = next(item for item in register['sources'] if item['commit'] == row['original_commit'])
    require(git(root, 'rev-parse', original['commit'] + '^{tree}') == original['tree'] and
            git(root, 'show', '-s', '--format=%P', original['commit']).split() == row['original_parents'],
            'Core candidate original graph mismatch')
    contained = [item for item in json.loads(CONTAINMENT.read_text())['sources'] if
                 (item['product'], item['line'], item['parent']) ==
                 (row['product'], row['line'], original['commit'])]
    require(len(contained) == 1, 'Core candidate containment mismatch')
    contained = contained[0]
    require((row['contained_commit'], row['contained_tree']) == (contained['commit'], contained['tree']) and
            contained['original_tree'] == original['tree'] and
            git(root, 'rev-parse', contained['commit'] + '^{tree}') == contained['tree'] and
            git(root, 'show', '-s', '--format=%P', contained['commit']).split() == [original['commit']],
            'Core candidate containment mismatch')
    expected = tree_entries(root, original['commit'])
    require(sorted(move['from'] for move in contained['moves']) == original['workflows'],
            'Core candidate containment mismatch')
    for move in contained['moves']:
        target = '.github/maintenance-inert-workflows/' + move['from'].rsplit('/', 1)[-1] + '.source'
        require(move['to'] == target and target not in expected and
                expected.pop(move['from']) == (move['mode'], 'blob', move['blob']),
                'Core candidate containment mismatch')
        expected[target] = (move['mode'], 'blob', move['blob'])
    require(tree_entries(root, contained['commit']) == expected, 'Core candidate containment mismatch')
    require(row['parents'] == [contained['commit']] and
            git(root, 'show', '-s', '--format=%P', row['commit']).split() == row['parents'] and
            git(root, 'rev-parse', row['commit'] + '^{tree}') == row['tree'], 'Core candidate graph mismatch')
    actual = tree_entries(root, row['commit'])
    properties = 'Directory.Build.props'
    old = git_bytes(root, contained['commit'], properties)
    token = f"<RepositoryUrl>https://github.com/{original['source_repository']}</RepositoryUrl>".encode()
    require(old.count(token) == 1, 'Core candidate metadata mismatch')
    new = old.replace(token, f'<RepositoryUrl>https://github.com/{CORE_REPOSITORY}</RepositoryUrl>'.encode())
    require(git_bytes(root, row['commit'], properties) == new and
            actual[properties][:2] == expected[properties][:2], 'Core candidate metadata mismatch')
    expected[properties] = actual[properties]
    require(actual == expected and not any(path.startswith('.github/workflows/') for path in actual),
            'Core candidate source delta mismatch')


def candidate_tree_delta(before: dict, after: dict) -> list[dict]:
    def identity(entry):
        return dict(zip(('mode', 'type', 'blob'), entry)) if entry is not None else None
    return [{'path': path, 'before': identity(before.get(path)), 'after': identity(after.get(path))}
            for path in sorted(set(before) | set(after)) if before.get(path) != after.get(path)]


def maintenance_editable_path(path: str) -> bool:
    # Ordinary source/test/fixture/docs formats stay usable. Build, inventory,
    # dependency, toolchain and authority controls require a coordinated slice.
    parts = PurePosixPath(path).parts
    name = parts[-1].casefold()
    if parts[0] not in ('src', 'test', 'tests', 'docs') and path != 'README.md':
        return False
    if any(part.startswith('.') or part.casefold() in
           ('build', 'scripts', 'obj', 'bin', 'node_modules', 'packages') for part in parts):
        return False
    controls = {'global.json', 'nuget.config', 'package.json', 'package-lock.json', 'npm-shrinkwrap.json',
                'yarn.lock', 'pnpm-lock.yaml', 'bun.lock', 'bun.lockb', 'deno.json', 'deno.jsonc',
                'packages.lock.json', 'packages.config', 'build.cs', 'makefile', 'cmakelists.txt'}
    return not (name in controls or name.startswith(('directory.', 'dockerfile', 'tsconfig', 'jsconfig')) or
                (PurePosixPath(name).suffix in ('.json', '.js', '.ts', '.mjs', '.cjs', '.yaml', '.yml') and
                 re.search(r'(^|\.)(config|settings)\.', name)) or
                PurePosixPath(name).suffix in ('.csproj', '.fsproj', '.vbproj', '.proj', '.sln', '.slnf', '.slnx',
                    '.props', '.targets', '.nuspec', '.config', '.lock', '.lockb', '.runsettings', '.ruleset',
                    '.rsp', '.sh', '.ps1', '.cmd', '.bat', '.user'))


def verify_maintenance_delta(root: Path, row: dict, parent: dict, register: dict) -> None:
    before, after = tree_entries(root, parent['commit']), tree_entries(root, row['commit'])
    require(candidate_tree_delta(before, after) == row['delta'], 'Core candidate source delta mismatch')
    placeholders = {item['source_file'] for item in register['inherited_skipped_placeholders']
                    if item['product'] == row['product'] and row['original_commit'] in item['commits']}
    for change in row['delta']:
        path = change['path']
        if path == 'Directory.Build.props':
            token = f"<PackageProjectUrl>https://github.com/{row['original_source_repository']}</PackageProjectUrl>".encode()
            old = git_bytes(root, parent['commit'], path)
            require(old.count(token) == 1 and change['before'] is not None and change['after'] is not None and
                    before[path][:2] == after[path][:2] and
                    git_bytes(root, row['commit'], path) == old.replace(token,
                        f'<PackageProjectUrl>https://github.com/{CORE_REPOSITORY}</PackageProjectUrl>'.encode()),
                    'Core candidate metadata mismatch')
        elif path == 'build/Build.cs':
            from extensions_manifest_continuation import CONTINUATIONS, verify_delta
            require(row['product'] == 'extensions' and row['commit'] in
                    {item['commit'] for item in CONTINUATIONS.values()}, 'Core candidate protected control mismatch')
            verify_delta(row, parent, change, git_bytes(root, parent['commit'], path),
                         git_bytes(root, row['commit'], path))
        elif path == 'src/wrappers/wrappers/react-wrapper/package.json':
            from historical_studio_npm_continuation import verify_delta
            verify_delta(row, parent, change, git_bytes(root, parent['commit'], path),
                         git_bytes(root, row['commit'], path))
        else:
            require(maintenance_editable_path(path) and path not in placeholders,
                    'Core candidate protected control mismatch')


def prepare_containment(root: Path, output: Path, register: dict) -> dict:
    """Create deterministic workflow-only commits as local objects, never refs."""
    output.mkdir(parents=True, exist_ok=False)
    result = {'schema': 1, 'remote_refs_created': False, 'sources': []}
    for row in register['sources']:
        verify_source(root, row)
        with tempfile.TemporaryDirectory(prefix='maintenance-index-') as temporary:
            env = build_environment() | {'GIT_INDEX_FILE': str(Path(temporary) / 'index'),
                'GIT_AUTHOR_NAME': 'Elsa maintenance preparation', 'GIT_AUTHOR_EMAIL': 'maintenance@example.invalid',
                'GIT_COMMITTER_NAME': 'Elsa maintenance preparation', 'GIT_COMMITTER_EMAIL': 'maintenance@example.invalid',
                'GIT_AUTHOR_DATE': '2026-10-08T00:00:00Z', 'GIT_COMMITTER_DATE': '2026-10-08T00:00:00Z'}
            git(root, 'read-tree', row['commit'], env=env)
            moved = []
            for path in row['workflows']:
                require(path.endswith(('.yml', '.yaml')), 'Unexpected workflow file; review containment manually')
                mode, _, blob = git(root, 'ls-tree', row['commit'], '--', path).split('\t')[0].split()
                target = '.github/maintenance-inert-workflows/' + path.rsplit('/', 1)[-1] + '.source'
                git(root, 'update-index', '--force-remove', '--', path, env=env)
                git(root, 'update-index', '--add', '--cacheinfo', f'{mode},{blob},{target}', env=env)
                moved.append({'from': path, 'to': target, 'blob': blob, 'mode': mode})
            tree = git(root, 'write-tree', env=env)
            commit = git(root, 'commit-tree', tree, '-p', row['commit'], '-m',
                         f"Keep {row['product']} {row['line']} historical workflows inert; no activation", env=env)
            patch = git(root, 'diff', '--binary', '-M', '--src-prefix=a/', '--dst-prefix=b/',
                        '--no-color', '--no-ext-diff', '--no-textconv', '--no-relative', '-O/dev/null', row['commit'], commit)
            name = f"{row['product']}-{row['line']}.patch"
            (output / name).write_text(patch + '\n')
            result['sources'].append({'product': row['product'], 'line': row['line'], 'parent': row['commit'],
                'original_tree': row['tree'], 'commit': commit, 'tree': tree, 'moves': moved,
                'patch': name, 'patch_sha256': digest((output / name).read_bytes())})
    write_json(output / 'containment.json', result)
    return result


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def write_json(path: Path, data: object) -> None:
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + '\n')


VERIFICATION_REASONS = {
    'Unexpected SourceLink repository/commit': 'sourcelink-identity-mismatch',
    'Foreign SourceLink document': 'sourcelink-document-identity-mismatch',
    'Unsafe SourceLink path': 'sourcelink-path-invalid',
    'Unsupported document hash': 'source-hash-algorithm-unsupported',
    'Tracked source checksum mismatch': 'source-checksum-mismatch',
    'Unmapped source document is not verified embedded content': 'source-document-unverified',
    'PDB source documents missing': 'pdb-documents-missing',
    'Unknown/duplicate package': 'package-inventory-identity-mismatch',
    'Packed version mismatch': 'package-version-mismatch',
    'Packed repository provenance mismatch': 'package-repository-mismatch',
    'Packed assembly payload differs from evaluated build-output policy': 'assembly-payload-mismatch',
    'Emitted satellite bytes missing': 'satellite-output-missing',
    'Packaged satellite bytes differ from emitted output': 'satellite-bytes-mismatch',
    'Symbol package missing': 'symbol-package-missing',
    'Symbol identity mismatch': 'symbol-package-identity-mismatch',
    'Symbol metadata disagrees with package': 'symbol-metadata-mismatch',
    'Framework PDB missing': 'framework-pdb-missing',
    'Packaged assembly identity mismatch': 'assembly-identity-mismatch',
    'Missing evaluated packages': 'package-inventory-incomplete',
    'Unexpected artifact files': 'artifact-inventory-mismatch',
    'Duplicate ZIP entries': 'archive-duplicate-entries',
    'Artifact must contain exactly one nuspec': 'archive-nuspec-count-invalid',
    'Nuspec metadata is missing': 'archive-nuspec-metadata-missing',
    'No required test framework executions': 'test-inventory-empty',
    'Inherited placeholder source binding mismatch': 'placeholder-source-binding-invalid',
    'Inherited placeholder source declaration mismatch': 'placeholder-source-declaration-invalid',
    'Inherited placeholder inventory mismatch': 'placeholder-inventory-invalid',
    'Test evidence rejected': 'test-evidence-invalid',
    'Unexpected evaluated version': 'evaluated-version-mismatch',
    'Unexpected Elsa dependency': 'elsa-dependency-mismatch',
    'Missing/failed tests': 'test-counts-invalid',
    'Unknown test project/framework identity': 'test-identity-unknown',
    'Missing/duplicate test project-framework results': 'test-cells-incomplete-or-duplicate',
    'SDK dependency metadata missing': 'sdk-dependency-metadata-missing',
    'SDK dependency groups disagree with package': 'sdk-dependency-groups-mismatch',
    'SDK framework reference metadata missing': 'sdk-framework-reference-metadata-missing',
    'SDK framework reference groups disagree with package': 'sdk-framework-reference-groups-mismatch',
    'SDK framework reference framework unsupported': 'sdk-framework-reference-framework-unsupported',
    'Source producer evidence rejected': 'source-producer-unverified',
    'Duplicate PDB document': 'source-document-duplicate',
    'Core remote source unavailable': 'source-remote-unavailable',
    'Core remote source mismatch': 'source-remote-mismatch',
    'Core candidate register identity mismatch': 'candidate-identity-invalid',
    'Core candidate original graph mismatch': 'candidate-original-graph-invalid',
    'Core candidate containment mismatch': 'candidate-containment-invalid',
    'Core candidate graph mismatch': 'candidate-graph-invalid',
    'Core candidate parent chain mismatch': 'candidate-parent-chain-invalid',
    'Core candidate protected control mismatch': 'candidate-protected-control-invalid',
    'Studio inline lifecycle delta mismatch': 'studio-inline-lifecycle-delta-invalid',
    'Studio inline lifecycle source mismatch': 'studio-inline-lifecycle-source-invalid',
    'Core candidate metadata mismatch': 'candidate-metadata-invalid',
    'Core candidate source delta mismatch': 'candidate-source-delta-invalid',
    'Core assembly commit mismatch': 'assembly-commit-mismatch',
    'Generated package manifest identity/version mismatch': 'package-manifest-identity-version-mismatch',
}


def verification_reason(message: str) -> str:
    if message in VERIFICATION_REASONS:
        return VERIFICATION_REASONS[message]
    for key in ('Unexpected evaluated version', 'Unexpected Elsa dependency',
                'Generated package manifest identity/version mismatch'):
        if message.startswith(key + ':'):
            return VERIFICATION_REASONS[key]
    return 'unknown-check-failure'


def closed_diagnostics(log: Path) -> dict:
    diagnostics = {'codes': [], 'nuke_failed_targets': [], 'unretained_code_occurrences': 0}
    if not log.is_file():
        return diagnostics
    counts, targets = {}, set()
    with log.open(errors='replace') as stream:
        stream.readline()  # The shared runner's first line is the private argv JSON, not process output.
        for raw in stream:
            line = re.sub(r'\x1b\[[0-?]*[ -/]*[@-~]', '', raw).replace('\u00a0', ' ')
            for severity, code in re.findall(r'\b(error|warning)\s+(CS[0-9]{4}|NU[0-9]{4}|MSB[0-9]{4}|NETSDK[0-9]{4})\b', line, re.I):
                key = severity.lower(), code.upper()
                if key in counts or len(counts) < 32:
                    counts[key] = counts.get(key, 0) + 1
                else:
                    diagnostics['unretained_code_occurrences'] += 1
            targets.update(re.findall(r'\bTarget (Restore|Compile|Test|Pack) has thrown an exception\b', line))
    diagnostics['codes'] = [{'severity': severity, 'code': code, 'count': count}
                            for (severity, code), count in sorted(counts.items())]
    diagnostics['nuke_failed_targets'] = sorted(targets)
    return diagnostics


def run_build_command(command: list[str], cwd: Path, log: Path, record: dict, *, environment: dict | None = None) -> None:
    try:
        run(command, cwd, timeout=7200, log=log, env=environment or build_environment(), outcome=record.setdefault('process', {}))
        record['success'] = True
    finally:
        record['diagnostics'] = closed_diagnostics(log)


def original_core(row: dict) -> bool:
    from selected_core_producer import original
    return original(row)


def recipe_environment(row: dict, version: str, source: Path) -> dict:
    config = source / 'NuGet.Config'
    require(config.is_file() and not config.is_symlink(), 'Original restore config must be a regular file')
    if original_core(row):
        from selected_core_producer import environment
        result = environment(row, version)
    else:
        result = build_environment()
    # Use the admitted source's config exclusively, including NUKE's child
    # restore and later SDK evaluations. Never inherit this authority from HOME.
    return result | {'RestoreConfigFile': str(config.resolve())}


def version_arguments(row: dict, version: str) -> list[str]:
    return [] if original_core(row) else [f'-p:Version={version}']


def recipes(row: dict, version: str, output: Path) -> list[tuple[str, list[str]]]:
    if original_core(row):
        from selected_core_producer import recipes as core_recipes
        return core_recipes(row, version, output)
    require(row['product'] in ('studio', 'extensions'), 'Unsupported producer product')
    if row['product'] == 'extensions':
        return [('.', ['./build.sh', 'Compile+Test+Pack', '--configuration', 'Release',
                       '--version', version, '--analyseCode', 'true'])]
    designer = 'src/modules/Elsa.Studio.Workflows.Designer/ClientLib'
    dom = 'src/framework/Elsa.Studio.DomInterop/ClientLib'
    commands = []
    if row['line'] == '3.9':
        commands.append(('.', ['dotnet', 'restore', 'src/modules/Elsa.Studio.Workflows.Designer/Elsa.Studio.Workflows.Designer.csproj']))
    commands.append((designer, ['npm', 'install', '--force']))
    if row['line'] == '3.9':
        commands.extend([(designer, ['npm', 'run', 'check:generated']), (designer, ['npm', 'test'])])
    commands.extend([(designer, ['npm', 'run', 'build']), (dom, ['npm', 'install', '--force']),
        (dom, ['npm', 'run', 'build'])])
    for target in ('build', 'test', 'pack'):
        command = ['dotnet', target, 'Elsa.Studio.sln', '--configuration', 'Release', f'/p:Version={version}',
                   *[f'/p:{name}={value}' for name, value in PROOF_BUILD_PROPERTIES.items()]]
        if target == 'test':
            command.extend(['--no-build', '--logger', 'trx', '--results-directory', str(output / 'test-results')])
        if target == 'pack':
            command.append('/p:PackageOutputPath=' + str(output / 'artifacts'))
        commands.append(('.', command))
    return commands


def evaluate_satellites(source: Path, project: str, framework: str, assembly: str, version: str, row: dict | None = None) -> list[dict]:
    row = row or {}
    result = json.loads(run(['dotnet', 'msbuild', project, '-nologo', '-p:Configuration=Release',
        *version_arguments(row, version), f'-p:TargetFramework={framework}', '-target:SatelliteDllsProjectOutputGroup',
        '-getItem:SatelliteDllsProjectOutputGroupOutput'], source, env=recipe_environment(row, version, source)))
    satellites = []
    for item in result['Items']['SatelliteDllsProjectOutputGroupOutput']:
        culture, target = item['Culture'], item['TargetPath'].replace('\\', '/')
        require(re.fullmatch(r'[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*', culture) is not None and
                target == f'{culture}/{assembly}.resources.dll', 'Unexpected evaluated satellite identity')
        emitted = Path(item['FinalOutputPath'].replace('\\', '/'))
        emitted = (source / emitted).resolve()
        require(emitted.is_relative_to(source.resolve()) and emitted.is_file() and not emitted.is_symlink(),
                'Evaluated satellite output is missing or outside source checkout')
        satellites.append({'framework': framework, 'culture': culture, 'target_path': target,
            'final_output_path': emitted.relative_to(source.resolve()).as_posix(),
            'package_path': f'lib/{framework}/{target}', 'sha256': digest(emitted.read_bytes())})
    require(len({item['package_path'] for item in satellites}) == len(satellites), 'Duplicate evaluated satellite')
    return satellites


def evaluate_inventory(source: Path, row: dict, version: str, output: Path) -> list[dict]:
    require(row['product'] in ('core', 'studio', 'extensions'), 'Unsupported producer product')
    solution = source / {'core': 'Elsa.sln', 'studio': 'Elsa.Studio.sln', 'extensions': 'Elsa.Extensions.sln'}[row['product']]
    projects = re.findall(r'^Project\([^\n]+?= "[^"]+", "([^"]+\.csproj)"', solution.read_text(encoding='utf-8-sig'), re.M)
    require(bool(projects), 'No solution projects')
    recipe_projects = {project.replace('\\', '/') for project in projects}
    if original_core(row):
        from selected_core_producer import policy as core_policy
        projects += sorted(set(core_policy(row)['test_projects']) - recipe_projects)
    properties = 'IsPackable,IsTestProject,AssemblyName,PackageId,PackageVersion,TargetFrameworks,TargetFramework,IncludeSymbols,IncludeBuildOutput'
    inventory, tests = [], []
    for project in projects:
        project = project.replace('\\', '/')
        require(not Path(project).is_absolute() and '..' not in Path(project).parts, 'Unsafe solution project')
        values = json.loads(run(['dotnet', 'msbuild', project, '-nologo', '-p:Configuration=Release',
            *version_arguments(row, version), f'-getProperty:{properties}'], source, env=recipe_environment(row, version, source)))['Properties']
        frameworks = (values['TargetFrameworks'] or values['TargetFramework']).split(';')
        if values['IsTestProject'].lower() == 'true':
            tests.append({'project': project, 'assembly_name': values['AssemblyName'], 'frameworks': frameworks})
        if project not in recipe_projects:
            require(original_core(row) and values['IsTestProject'].lower() == 'true', 'Core original extra test project')
            continue
        if values['IsPackable'].lower() != 'true':
            continue
        require(values['PackageVersion'] == version, f'Unexpected evaluated version: {project}')
        require(values['IncludeBuildOutput'].lower() in ('true', 'false'), 'Build-output policy was not evaluated')
        require(bool(values['AssemblyName']) and '/' not in values['AssemblyName'] and '\\' not in values['AssemblyName'],
                'Invalid evaluated assembly identity')
        inventory.append({'id': values['PackageId'], 'project': project,
            'assembly_name': values['AssemblyName'], 'include_build_output': values['IncludeBuildOutput'].lower() == 'true',
            'frameworks': frameworks,
            'symbols': values['IncludeSymbols'].lower() == 'true',
            'satellites': [satellite for framework in frameworks for satellite in
                evaluate_satellites(source, project, framework, values['AssemblyName'], version, row)]
                if values['IncludeBuildOutput'].lower() == 'true' else []})
    require(bool(inventory) and len({p['id'].casefold() for p in inventory}) == len(inventory), 'Empty/duplicate package inventory')
    require(bool(tests), 'No evaluated test projects')
    write_json(output / 'test-inventory.json', tests)
    return inventory


def physical_families(row: dict, policy: dict) -> tuple:
    if row['product'] != 'extensions':
        return ()
    project = policy['project']
    if (policy['id'], project) == ('Elsa.Caching.Distributed.ProtoActor',
            'src/modules/caching/Elsa.Caching.Distributed.ProtoActor/Elsa.Caching.Distributed.ProtoActor.csproj'):
        return ((r'Proto/LocalCacheMessages\.cs', 'grpc'),) + (
            ((r'protopotato/LocalCache-[0-9A-F]{32}\.cs', 'protograin'),) if row['line'] == '3.8' else ())
    if (policy['id'], project) == ('Elsa.Workflows.Runtime.ProtoActor',
            'src/modules/runtimes/Elsa.Workflows.Runtime.ProtoActor/Elsa.Workflows.Runtime.ProtoActor.csproj'):
        return ((r'Proto/(Shared|WorkflowInstanceMessages)\.cs', 'grpc'),
                (r'protopotato/WorkflowInstance-[0-9A-F]{32}\.cs', 'protograin'))
    return ()


def maintenance_family(row: dict, policy: dict, framework: str, path: str) -> str | None:
    family = generated_family(policy, framework, path)
    prefix = f"{Path(policy['project']).parent.as_posix()}/obj/Release/{framework}/"
    if family is None and path.startswith(prefix):
        family = next((kind for pattern, kind in physical_families(row, policy)
                       if re.fullmatch(pattern, path[len(prefix):])), None)
    return family


def public_producer(evidence: dict) -> dict:
    # Explicit projection: compiler paths, restored assets and task paths are private.
    keys = ('kind', 'sdk_version', 'compiler_sha256', 'package_id', 'package_version',
            'archive_sha256', 'restore_sha512', 'nuget_content_hash', 'signed', 'archive_entry',
            'content_sha256', 'targeting_pack', 'checked_archive_contents', 'source_emitting_targets_sha256')
    result = {key: evidence[key] for key in keys if key in evidence}
    if 'frameworks' in evidence:
        result['frameworks'] = [{key: pack[key] for key in ('Identity', 'TargetingPackName', 'TargetingPackVersion')}
                                for pack in evidence['frameworks']]
    return result


def metadata_command(project: str, version: str, row: dict | None = None) -> list[str]:
    return ['dotnet', 'msbuild', project, '-nologo', '-p:Configuration=Release', *version_arguments(row or {}, version),
            *[f'-p:{key}={value}' for key, value in PROOF_BUILD_PROPERTIES.items()]]


def stage_maintenance_metadata(source: Path, row: dict, inventory: list[dict], version: str,
                               inspector: Path, *, diagnostics: Path | None = None) -> None:
    source = source.resolve()
    cache = {'archive_inspector': inspector, 'source_commit': row['commit']}
    context = {'package': None, 'project': None, 'framework': None, 'operation': 'temporary-stage'}
    command_index = 0
    if diagnostics is not None:
        diagnostics.mkdir()

    def execute(command: list[str]) -> str:
        nonlocal command_index
        command_index += 1
        record = None if diagnostics is None else diagnostics / f'command-{command_index:04d}.txt'
        if record is not None:
            record.write_text(json.dumps({'context': context, 'argv': command}, indent=2) + '\nstdout:\n')
        stdout = run(command, source, env=recipe_environment(row, version, source))
        if record is not None:
            with record.open('a') as stream:
                stream.write(stdout)
        return stdout

    def retain_nuspecs(destination: Path) -> None:
        if diagnostics is None:
            return
        for path in sorted(destination.glob('*.nuspec')):
            require(not destination.is_symlink() and not path.is_symlink() and path.is_file() and
                    path.resolve().is_relative_to(Path(temporary)),
                    'Invalid staged nuspec diagnostic input')
            target = diagnostics / destination.name
            target.mkdir(exist_ok=True)
            (target / (path.name + '.txt')).write_bytes(path.read_bytes())

    try:
        # This directory is inside the unretained source checkout. Never upload raw SDK metadata.
        with tempfile.TemporaryDirectory(prefix='maintenance-metadata-', dir=source) as temporary:
            for index, policy in enumerate(inventory):
                context.update(package=policy['id'], project=policy['project'], framework=None, operation='package')
                destination = Path(temporary) / str(index)
                destination.mkdir()
                command = metadata_command(policy['project'], version, row)
                context['operation'] = 'restore-input'
                assets_name = execute(command + ['-getProperty:ProjectAssetsFile']).strip()
                stage_assets = (source / assets_name).resolve()
                require(stage_assets.is_relative_to(source.resolve()) and stage_assets.is_file() and not stage_assets.is_symlink(),
                        'Invalid restored metadata input')
                stage_assets_hash = digest(stage_assets.read_bytes())
                context['operation'] = 'nuspec'
                try:
                    execute(command + ['-target:_GetRestoreProjectStyle;GenerateNuspec', '-p:NoBuild=true',
                        '-p:ContinuePackingAfterGeneratingNuspec=false', f'-p:NuspecOutputPath={destination}',
                        f"-p:PackageOutputPath={destination / 'forbidden-packages'}"])
                except Exception:
                    try:
                        retain_nuspecs(destination)
                    except (OSError, ValueError):
                        pass  # Diagnostic failure must not replace the original command failure.
                    raise
                else:
                    retain_nuspecs(destination)
                policy.update(nupkg=f"{policy['id']}.{version}.nupkg",
                              snupkg=f"{policy['id']}.{version}.snupkg" if policy['symbols'] else None,
                              source_commit=row['commit'], restore_assets=[], framework_properties={})
                read_staged_nuspecs(destination, policy)
                for framework in policy['frameworks']:
                    context.update(framework=framework, operation='compiler-metadata')
                    properties = ('MSBuildToolsPath,NETCoreSdkVersion,NetCoreRoot,RuntimeIdentifier,ProjectAssetsFile,'
                                  'GenerateElsaPackageManifest,ElsaPackageManifestIncludeInPackage,ElsaPackageManifestPackagePath' +
                                  (',AssemblyVersion,InformationalVersion' if original_core(row) else ''))
                    targets = 'ResolveReferences' + (';GetAssemblyAttributes' if original_core(row) else '')
                    if ET.parse(source / policy['project']).getroot().get('Sdk') == 'Microsoft.NET.Sdk.Razor':
                        targets += ';_PrepareRazorSourceGenerators'
                    resolved = json.loads(execute(command + [f'-p:TargetFramework={framework}', '-p:BuildProjectReferences=false',
                        f'-target:{targets}', f'-getProperty:{properties}', '-getItem:Analyzer,ResolvedFrameworkReference,Compile']))
                    assets = (source / resolved['Properties']['ProjectAssetsFile']).resolve()
                    require(assets.is_relative_to(source.resolve()) and assets.is_file() and not assets.is_symlink(),
                            'Invalid restored metadata input')
                    require(assets == stage_assets and digest(assets.read_bytes()) == stage_assets_hash,
                            'Restored metadata input changed')
                    policy['restore_assets'].append({'framework': framework,
                        'path': assets.relative_to(source).as_posix(), 'sha256': digest(assets.read_bytes())})
                    evidence = capture_compiler_evidence(source, policy, framework, resolved, cache,
                                                        physical_families=physical_families(row, policy))
                    policy['framework_properties'][framework] = {'compiler_evidence': evidence,
                        'manifest_required': resolved['Properties']['GenerateElsaPackageManifest'].lower() == 'true' and
                            resolved['Properties']['ElsaPackageManifestIncludeInPackage'].lower() == 'true',
                        'manifest_path': resolved['Properties']['ElsaPackageManifestPackagePath']}
                    if original_core(row):
                        policy['framework_properties'][framework]['assembly_policy'] = {key: resolved['Properties'][key]
                            for key in ('AssemblyVersion', 'InformationalVersion')}
                context.update(framework=None, operation='manifest-contract')
                if row['product'] == 'extensions':
                    from selected_extensions_contract import bind_manifest_contract
                    bind_manifest_contract(source, row, policy)
                context['operation'] = 'sdk-assets'
                if row['product'] == 'extensions' or original_core(row):
                    policy['expected_sdk_assets'] = capture_sdk_assets(source, policy,
                        (destination / (policy['nupkg'].removesuffix('.nupkg') + '.nuspec')).read_bytes())

            context['operation'] = 'temporary-cleanup'
    except Exception:
        if diagnostics is not None:
            try:
                (diagnostics / 'failure.txt').write_text(json.dumps(context, indent=2) + '\n' + traceback.format_exc())
            except OSError:
                pass  # Preserve the original validation or temporary-directory cleanup exception.
        raise


def public_inventory(inventory: list[dict]) -> list[dict]:
    keys = ('id', 'project', 'assembly_name', 'include_build_output', 'frameworks', 'symbols', 'satellites',
            'source_commit', 'expected_dependency_groups', 'expected_symbol_dependency_groups',
            'expected_framework_reference_groups', 'expected_symbol_framework_reference_groups',
            'sdk_nuspec_sha256', 'sdk_symbol_nuspec_sha256')
    return [{**{key: policy[key] for key in keys if key in policy},
             'restore_inputs': [{'framework': item['framework'], 'sha256': item['sha256']}
                                for item in policy.get('restore_assets', [])],
             'producers': {framework: {'sdk_version': evidence['sdk_version'],
                 'compiler_sha256': evidence['compiler_sha256'],
                 'tools': {family: public_producer(tool) for family, tool in evidence['tools'].items()}}
                 for framework, properties in policy.get('framework_properties', {}).items()
                 for evidence in [properties['compiler_evidence']]}}
            for policy in inventory]


def verify_restored_manifest_hint(source: Path, policy: dict, framework: str,
                                  document: dict, cache: dict, *, version: str) -> tuple[dict, dict]:
    """Bind the caller's reviewed hint version to its actual restored compiler input."""
    identifier = 'Elsa.Platform.PackageManifest.Generator'
    assets = restored_assets(source, policy, framework)
    relative = identifier.lower() + '/' + version
    library = assets.get('libraries', {}).get(identifier + '/' + version, {})
    require(library.get('path', relative) == relative, 'Unexpected external package cache layout')
    candidates = []
    for folder in assets.get('packageFolders', {}):
        root = Path(folder)
        require(root.is_absolute() and folder in (root.as_posix(), root.as_posix() + '/') and '..' not in root.parts,
                'Non-canonical external cache root')
        candidate = root / relative / f'{identifier.lower()}.{version}.nupkg'
        require(not any(path.is_symlink() for path in (candidate, *candidate.parents)),
                'Symlinked external archive path')
        if candidate.exists():
            require(candidate.is_file(), 'External archive is not a regular file')
            candidates.append(candidate)
    require(len(candidates) == 1, 'Missing or ambiguous restored external archive')
    archive, identity = restored_archive(assets, identifier, version, cache=cache)
    require(archive == candidates[0], 'External archive selection changed')
    external = verify_external_document(source, policy, framework, document, cache)
    if external is None:
        actual = Path(document['path'])
        require(actual.is_absolute() and actual.as_posix() == document['path'] and '..' not in actual.parts and
                actual.is_relative_to(archive.parent), 'External document is outside its restored package')
        entry = actual.relative_to(archive.parent).as_posix()
        external = verify_external_entry(source, policy, framework, document, version, entry, cache)
    require(identity['archive_sha256'] == external['archive_sha256'], 'External archive identity changed')
    extracted = archive.parent / external['archive_entry']
    require(not any(path.is_symlink() for path in (extracted, *extracted.parents)) and extracted.is_file() and
            hashlib.new(document['algorithm'], extracted.read_bytes()).hexdigest() == document['checksum'],
            'External compiler input bytes changed')
    inputs = policy['framework_properties'][framework]['compiler_evidence']['compile_inputs']
    actual = extracted.resolve()
    locator = actual.relative_to(source.resolve()).as_posix() if actual.is_relative_to(source.resolve()) else actual.as_posix()
    require(any(item['path'] == locator for item in inputs), 'External source is not a compiler input')
    return external, identity


SOURCE_PRODUCER_ERRORS = {
    'Missing resolved SDK/framework evidence': 'producer-context-missing',
    'Pinned SDK compiler content changed': 'sdk-compiler-changed',
    'Missing actual resolved generator evidence:': 'generator-evidence-missing',
    'Resolved generator content changed:': 'generator-content-changed',
    'Resolved generator archive/identity changed:': 'generator-archive-changed',
    'Generator does not belong to pinned SDK': 'generator-sdk-mismatch',
    'SDK source-emitting targets changed': 'generator-targets-changed',
    'Generator does not belong to resolved framework': 'generator-framework-mismatch',
    'Missing restored dependency evidence:': 'restore-evidence-missing',
    'Restored dependency evidence changed': 'restore-evidence-changed',
}
SOURCE_PRODUCER_CHECKS = frozenset(SOURCE_PRODUCER_ERRORS.values()) | {
    'source-context-invalid', 'embedded-checksum-mismatch', 'unmapped-family-unsupported',
    'restored-hint-unverified', 'generated-family-unknown', 'producer-file-unavailable',
    'producer-context-invalid', 'unknown-check-failure',
}


class SourceProducerVerificationError(ValueError):
    def __init__(self, check: str):
        super().__init__('Source producer evidence rejected')
        self.check = check


def verify_non_git_document(document: dict, path: str | None, source: Path, row: dict,
                            policy: dict | None, framework: str | None, cache: dict) -> dict:
    check = 'source-context-invalid'
    try:
        require(policy is not None and policy.get('source_commit') == row['commit'] and
                framework in policy['frameworks'] and framework in policy['framework_properties'], 'Missing source context')
        check = 'embedded-checksum-mismatch'
        require(document.get('embedded_checksum') == document['checksum'], 'Missing matching embedded bytes')
        if path is None:
            # Only this reviewed original package-content family can be unmapped.
            check = 'unmapped-family-unsupported'
            version = '0.0.1-preview.50' if row['product'] == 'extensions' else None
            if original_core(row):
                from selected_core_producer import policy as core_policy
                core_policy(row)  # Admit the exact original source or reviewed continuation, not merely its kind.
                version = '0.0.1-preview.53'
            require(version is not None and
                    f'/elsa.platform.packagemanifest.generator/{version}/' in document['path'], 'Unknown external family')
            check = 'restored-hint-unverified'
            external, archive_identity = verify_restored_manifest_hint(source, policy, framework, document, cache, version=version)
            return {'family': 'manifest-hints', 'producer': {key: external[key] for key in
                    ('external_package', 'archive_entry', 'archive_sha256', 'feed')} | {
                    'restore_sha512': archive_identity['restore_sha512']}}
        check = 'generated-family-unknown'
        family = maintenance_family(row, policy, framework, path)
        require(family is not None, 'Unknown generated family')
        check = 'unknown-check-failure'
        producer = verify_generator_identity(source, policy, framework, family, cache)
        return {'family': family, 'producer': public_producer(producer)}
    except (ValueError, KeyError, OSError, TypeError) as error:
        if check == 'unknown-check-failure':
            if isinstance(error, OSError):
                check = 'producer-file-unavailable'
            elif isinstance(error, (KeyError, TypeError)):
                check = 'producer-context-invalid'
            else:
                check = next((code for message, code in SOURCE_PRODUCER_ERRORS.items()
                              if str(error).startswith(message)), check)
        raise SourceProducerVerificationError(check) from None


def verify_documents(details: dict, source: Path, row: dict, policy: dict | None = None,
                     framework: str | None = None, cache: dict | None = None) -> list[dict]:
    cache = cache if cache is not None else {}
    maps = details['source_link']['documents']
    prefix = f"https://raw.githubusercontent.com/{row['source_repository']}/{row['commit']}/"
    core = row.get('source_kind') == 'core'
    require(not core or row['source_repository'] == CORE_REPOSITORY, 'Unexpected SourceLink repository/commit')
    require(bool(maps) and all(isinstance(value, str) and value.startswith(prefix) for value in maps.values()), 'Unexpected SourceLink repository/commit')
    documents, seen = [], set()
    for index, document in enumerate(details['documents']):
        require(document['path'] not in seen, 'Duplicate PDB document')
        seen.add(document['path'])
        checksum = document['checksum']
        require(document['algorithm'] in ('sha1', 'sha256'), 'Unsupported document hash')
        url = source_url(document['path'], maps)
        path = None
        if url is not None:
            require(url.startswith(prefix), 'Foreign SourceLink document')
            path = url[len(prefix):]
            require(bool(path) and '..' not in Path(path).parts and not Path(path).is_absolute() and
                    '\\' not in path and not any(char in path for char in ('?', '#', '%')), 'Unsafe SourceLink path')
            # A wildcard may also map generated files absent from the original tree.
            entry = git(source, 'ls-tree', row['commit'], '--', ':(literal)' + path)
            tracked = bool(entry)
        else:
            tracked = False
        if tracked:
            entry = entry.split('\t')[0].split()
            require(entry[:2] in (['100644', 'blob'], ['100755', 'blob']), 'Unsafe SourceLink path')
            try:
                record = verify_tracked_document(source, row['commit'], path, document, url, core, cache,
                                                 reject_redirects=core)
                require(record is not None, 'Tracked source checksum mismatch')
            except ValueError as error:
                if str(error).startswith('Remote source'):
                    raise ValueError('Core remote source mismatch') from None
                raise ValueError('Tracked source checksum mismatch') from None
            except OSError:
                raise ValueError('Core remote source unavailable') from None
            evidence = {'path': path, 'source': 'core-git' if core else 'original-git', 'url': url}
            if core:
                evidence['remote_fetched'] = True
        else:
            origin = verify_non_git_document(document, path, source, row, policy, framework, cache)
            evidence = {'path': f'[embedded]/document-{index + 1}', 'source': 'embedded', **origin}
        documents.append(evidence | {'algorithm': document['algorithm'], 'checksum': checksum})
    if not documents:
        require(only_abstract_methods(details) and type(details.get('nonmodule_types')) is int and
                details['nonmodule_types'] > 0 and details.get('reference_assembly') is False,
                'PDB source documents missing')
    return documents


def verify_sdk_dependencies(nuspec: ET.Element, policy: dict, *, symbols: bool = False) -> None:
    suffix = 'symbol_' if symbols else ''
    groups = policy.get(f'expected_{suffix}dependency_groups')
    checksum = policy.get(f'sdk_{suffix}nuspec_sha256')
    require(isinstance(groups, list) and isinstance(checksum, str) and
            re.fullmatch(r'[0-9a-f]{64}', checksum) is not None, 'SDK dependency metadata missing')
    require(dependency_groups(nuspec) == groups, 'SDK dependency groups disagree with package')
    references = policy.get(f'expected_{suffix}framework_reference_groups')
    require(isinstance(references, list), 'SDK framework reference metadata missing')
    require(framework_reference_groups(nuspec) == references, 'SDK framework reference groups disagree with package')
    require(all(group['framework'] in policy['frameworks'] for group in references),
            'SDK framework reference framework unsupported')


def verify_artifacts(artifacts: Path, inventory: list[dict], row: dict, version: str, source: Path,
                     inspector: Path, output: Path, context: dict | None = None) -> list[dict]:
    expected = {p['id'].casefold(): p for p in inventory}
    produced = set(expected)
    found = set()
    receipts = []
    producer_cache = {'archive_inspector': inspector, 'source_commit': row['commit']}
    with tempfile.TemporaryDirectory(prefix='maintenance-symbols-') as temporary:
        for path in sorted(artifacts.glob('*.nupkg')):
            with zipfile.ZipFile(path) as package:
                nuspec = metadata(package)
                identifier = nuspec.findtext('id', '')
                require(identifier.casefold() in expected and identifier.casefold() not in found, 'Unknown/duplicate package')
                found.add(identifier.casefold())
                policy = expected[identifier.casefold()]
                if context is not None:
                    context.pop('framework', None)
                    context.update(package=policy['id'])
                require(nuspec.findtext('version') == version, 'Packed version mismatch')
                repository = nuspec.find('repository')
                require(repository is not None and repository.get('commit') == row['commit'] and
                    repository.get('url', '').removesuffix('.git').rstrip('/') == 'https://github.com/' + row['source_repository'],
                    'Packed repository provenance mismatch')
                dependencies = dependency_groups(nuspec)
                verify_sdk_dependencies(nuspec, policy)
                for group in dependencies:
                    for dependency in group['dependencies']:
                        if dependency['id'].casefold().startswith('elsa') and not original_core(row):
                            target = version if dependency['id'].casefold() in produced else row['dependency_version']
                            require(dependency['version'] in (target, f'[{target}]', f'[{target}, )', f'[{target},)'),
                                    f"Unexpected Elsa dependency: {identifier} -> {dependency}")
                manifest = None
                if row['product'] == 'extensions' or original_core(row):
                    verify_sdk_assets(package, policy, required=True)
                    manifest = verify_package_manifest(package, policy, version, require_sdk_metadata=True)
                names = archive_names(package)
                assemblies = sorted(n for n in names if n.startswith('lib/') and n.endswith('.dll'))
                frameworks = sorted({name.split('/')[1] for name in assemblies})
                expected_assemblies = sorted(f"lib/{framework}/{policy['assembly_name']}.dll"
                    for framework in policy['frameworks']) if policy['include_build_output'] else []
                satellite_paths = [item['package_path'] for item in policy['satellites']]
                require(assemblies == sorted(expected_assemblies + satellite_paths),
                        'Packed assembly payload differs from evaluated build-output policy')
                for satellite in policy['satellites']:
                    emitted = (source / satellite['final_output_path']).resolve()
                    require(emitted.is_relative_to(source.resolve()) and emitted.is_file(), 'Emitted satellite bytes missing')
                    emitted_bytes = emitted.read_bytes()
                    require(digest(emitted_bytes) == satellite['sha256'] and
                            package.read(satellite['package_path']) == emitted_bytes, 'Packaged satellite bytes differ from emitted output')
                symbols_path = path.with_suffix('.snupkg')
                require(not expected_assemblies or policy['symbols'] and symbols_path.is_file() or
                        original_core(row) and (policy['id'], policy['project'], policy['symbols']) ==
                        ('Elsa.SamplePackage', 'src/apps/Elsa.SamplePackage/Elsa.SamplePackage.csproj', False),
                        'Symbol package missing')
                if original_core(row):
                    require(symbols_path.is_file() == policy['symbols'], 'Core symbol output policy mismatch')
                symbols = []
                if symbols_path.is_file() or original_core(row) and expected_assemblies:
                    with (zipfile.ZipFile(symbols_path) if symbols_path.is_file() else nullcontext(None)) as symbol_package:
                        symbol_names = []
                        if symbol_package is not None:
                            symbol_metadata = metadata(symbol_package)
                            verify_sdk_dependencies(symbol_metadata, policy, symbols=True)
                            require(symbol_metadata.findtext('id') == identifier and symbol_metadata.findtext('version') == version,
                                    'Symbol identity mismatch')
                            symbol_repository = symbol_metadata.find('repository')
                            require(symbol_repository is not None and symbol_repository.attrib == repository.attrib and
                                    dependency_groups(symbol_metadata) == dependencies, 'Symbol metadata disagrees with package')
                            symbol_names = archive_names(symbol_package)
                        for name in expected_assemblies:
                            if context is not None:
                                context.update(framework=name.split('/')[1])
                            pdb_name = name[:-4] + '.pdb'
                            require(symbol_package is None or pdb_name in symbol_names, 'Framework PDB missing')
                            dll, pdb = Path(temporary) / Path(name).name, Path(temporary) / Path(pdb_name).name
                            dll.write_bytes(package.read(name))
                            if symbol_package is None:
                                from selected_core_producer import private_symbols
                                pdb.write_bytes(private_symbols(source, policy, name.split('/')[1], package.read(name)).read_bytes())
                            else:
                                pdb.write_bytes(symbol_package.read(pdb_name))
                            inspection = json.loads(run(['dotnet', str(inspector), str(dll), str(pdb), '--inspect-symbols'],
                                source, env=build_environment()))
                            details = inspection['details']
                            require(details['assembly_name'] == policy['assembly_name'], 'Packaged assembly identity mismatch')
                            if original_core(row):
                                from selected_core_producer import verify_assembly
                                verify_assembly(details, policy, name.split('/')[1], row)
                            # Original Extensions NUKE applies --version only when packing, not compiling.
                            informational_prefix = {'studio': version, 'extensions': '1.0.0'}.get(row['product'])
                            require(row.get('source_kind') != 'core' or informational_prefix is not None and
                                    details.get('informational_version') == informational_prefix + '+' + row['commit'],
                                    'Core assembly commit mismatch')
                            source_evidence = {'documents': verify_documents(details, source, row, policy,
                                name.split('/')[1], producer_cache)}
                            if not source_evidence['documents']:
                                source_evidence['source_applicability'] = {
                                    'classification': 'no-documents-no-executable-method-bodies', 'document_count': 0,
                                    **{key: details[key] for key in ('executable_method_bodies', 'nonabstract_methods_without_body',
                                       'native_or_external_methods', 'nonmodule_types', 'reference_assembly')}}
                            symbols.append({'assembly': name, 'assembly_sha256': digest(dll.read_bytes()),
                                'pdb': pdb_name, 'pdb_sha256': digest(pdb.read_bytes()), 'symbol': {key: inspection['symbol'][key] for key in ('key', 'pdb_name', 'guid', 'stamp',
                                    'checksum_algorithm', 'declared_checksum', 'normalized_checksum', 'pdb_sha256', 'pdb_size')}, 'assembly_version': details['assembly_version'],
                                'informational_version': details['informational_version'],
                                **source_evidence, **({'symbol_package': False} if symbol_package is None else {})})
                receipts.append({'id': identifier, 'version': version, 'frameworks': frameworks,
                    'assembly_name': policy['assembly_name'], 'include_build_output': policy['include_build_output'],
                    'satellites': policy['satellites'],
                    'dependencies': dependencies, 'repository': dict(repository.attrib), 'symbols': symbols,
                    'package_manifest': manifest, 'sdk_assets': [{key: asset[key] for key in ('path', 'sha256')}
                        for asset in policy.get('expected_sdk_assets', [])],
                    'files': [{'name': p.name, 'sha256': digest(p.read_bytes()), 'size': p.stat().st_size}
                              for p in (path, symbols_path) if p.exists()]})
    require(found == produced, 'Missing evaluated packages')
    expected_files = {f['name'] for p in receipts for f in p['files']}
    require({p.name for p in artifacts.iterdir()} == expected_files, 'Unexpected artifact files')
    write_json(output / 'verified-artifacts.json', receipts)
    return receipts


def placeholder_policies(source: Path, row: dict) -> dict:
    policies = {}
    for policy in load_register()['inherited_skipped_placeholders']:
        if row['product'] != policy['product'] or row.get('original_commit', row['commit']) not in policy['commits']:
            continue
        locator = row['commit'] + ':' + policy['source_file']
        require(git(source, 'rev-parse', locator) == policy['source_blob'], 'Inherited placeholder source binding mismatch')
        text = git(source, 'show', locator)
        namespace, name = policy['class'].rsplit('.', 1)
        # These two reviewed immutable blobs contain one unconditional skipped
        # Fact each. Check the explicit declarations, without parsing C#.
        require(f'[Fact(Skip = "{policy["skip_reason"]}")]' in text and
                f' {policy["method"]}(' in text and f'namespace {namespace};' in text and
                any(line.split()[:3] == ['public', 'class', name] for line in text.splitlines()),
                'Inherited placeholder source declaration mismatch')
        key = policy['project'], policy['framework']
        require(key not in policies, 'Inherited placeholder inventory mismatch')
        policies[key] = {name: value for name, value in policy.items() if name not in ('commits', 'product')} | {
            'source_commit': row['commit']}
    return policies


def verify_tests(output: Path, row: dict, context: dict | None = None, *, environment: dict | None = None) -> dict:
    require(not original_core(row) or environment is not None, 'Core test environment missing')
    tests = json.loads((output / 'test-inventory.json').read_text())
    source = output / 'source'
    expected = {str((source / Path(test['project']).parent / 'bin/Release' / framework /
                    (test['assembly_name'] + '.dll')).resolve()):
                {'project': test['project'], 'framework': framework}
                for test in tests for framework in test['frameworks']}
    require(bool(expected) and len(expected) == sum(len(test['frameworks']) for test in tests),
            'No required test framework executions')
    evidence = context if context is not None else {}
    evidence.update(expected_cells=list(expected.values()), admitted_observed_cells=[], unknown_path_count=0,
                    duplicate_cell_count=0, positive_summary_count=0, summary_count=0,
                    cells=[], failure_reasons=[], unknown_test_identity_count=0, inherited_skipped_placeholders=[])
    policies = {} if original_core(row) else placeholder_policies(source, row)
    require(set(policies) <= {(cell['project'], cell['framework']) for cell in expected.values()},
            'Inherited placeholder inventory mismatch')
    cells, results, failures = [], [], set()
    # Original NUKE adds TRX and its AnalyseCode=true recipe writes here.
    results_directory = output / 'test-results' if row['product'] == 'studio' or original_core(row) else source / 'testresults'
    for path in sorted(results_directory.glob('*.trx')):
        evidence['summary_count'] += 1
        cell, assemblies = None, set()
        try:
            tree = ET.parse(path)
            methods = tree.findall('./{*}TestDefinitions/{*}UnitTest/{*}TestMethod')
            assemblies = {str(Path(method.get('codeBase', '')).resolve()) for method in methods}
            cells.extend(assemblies)
            if len(assemblies) != 1 or not assemblies <= expected.keys():
                failures.add('test-identity-unknown')
                evidence['unknown_test_identity_count'] += 1
                continue
            cell = expected[next(iter(assemblies))]
            counter_elements = tree.findall('.//{*}Counters')
            require(len(counter_elements) == 1, 'Missing/failed tests')
            counters_element = counter_elements[0]
            counters = {name: int(counters_element.get(name, '-1')) for name in TRX_COUNTERS}
            require(all(value >= 0 for value in counters.values()), 'Missing/failed tests')
        except (ET.ParseError, ValueError):
            failures.add('test-counts-invalid')
            # Retain the admitted cell even when its counts are malformed.
            if cell is not None:
                evidence['cells'].append(cell | {'status': 'invalid-counts'})
            continue
        diagnostic = cell | {'counters': counters, 'status': 'rejected'}
        evidence['cells'].append(diagnostic)
        if set(counters_element.attrib) != set(TRX_COUNTERS):
            diagnostic['unknown_counter_count'] = len(set(counters_element.attrib) - set(TRX_COUNTERS))
            failures.add('test-counter-schema-invalid')
            continue
        definitions = tree.findall('./{*}TestDefinitions/{*}UnitTest')
        outcomes = tree.findall('./{*}Results/{*}UnitTestResult')
        entries = tree.findall('./{*}TestEntries/{*}TestEntry')
        definition_ids = [definition.get('id') for definition in definitions]
        result_ids = [result.get('testId') for result in outcomes]
        result_pairs = [(result.get('testId'), result.get('executionId')) for result in outcomes]
        result_pair_set = set(result_pairs)
        entry_pairs = [(entry.get('testId'), entry.get('executionId')) for entry in entries]
        execution_ids = [pair[1] for pair in result_pairs]
        anchors = [definition.findall('{*}Execution') for definition in definitions]
        nested_result_count = len(tree.findall('.//{*}UnitTestResult')) - len(outcomes)
        unsupported_structure_count = sum(len(tree.findall('./{*}' + container + '/*')) - len(items)
            for container, items in (('TestDefinitions', definitions), ('Results', outcomes), ('TestEntries', entries))) + \
            sum(len(result.findall('{*}InnerResults')) for result in outcomes)
        # Dynamic theories may share a definition, but every flat execution has
        # its own identity and exactly one matching TestEntry. The definition's
        # sole Execution anchors one result in that same definition group.
        linked = bool(definition_ids) and all(definition_ids) and len(set(definition_ids)) == len(definition_ids) and \
            all(len(definition.findall('{*}TestMethod')) == 1 for definition in definitions) and \
            all(len(tree.findall('./{*}' + container)) == 1 for container in ('TestDefinitions', 'Results', 'TestEntries')) and \
            nested_result_count == unsupported_structure_count == 0 and set(result_ids) == set(definition_ids) and \
            all(execution_ids) and len(set(execution_ids)) == len(execution_ids) and \
            len(set(entry_pairs)) == len(entry_pairs) and set(entry_pairs) == result_pair_set and \
            all(len(anchor) == 1 and (identifier, anchor[0].get('id')) in result_pair_set
                for identifier, anchor in zip(definition_ids, anchors))
        policy = policies.get((cell['project'], cell['framework']))
        receipt = cell | {'counters': counters, 'sha256': digest(path.read_bytes())}
        summaries = tree.findall('.//{*}ResultSummary')
        completed = len(summaries) == 1 and summaries[0].get('outcome') == 'Completed'
        diagnostic['structure'] = {'definition_count': len(definitions), 'result_count': len(outcomes),
            'entry_count': len(entries), 'unique_execution_count': len(set(execution_ids)),
            'repeated_definition_result_count': len(result_ids) - len(set(result_ids)),
            'nested_result_count': nested_result_count, 'unsupported_structure_count': unsupported_structure_count,
            'linkage_valid': linked, 'summary_completed': completed}
        if policy is not None:
            execution = definitions[0].find('{*}Execution') if len(definitions) == 1 else None
            identity_matches = linked and len(definitions) == len(methods) == len(outcomes) == len(entries) == 1 and \
                methods[0].get('className') == policy['class'] and methods[0].get('name') == policy['method'] and \
                entries[0].get('testId') == definitions[0].get('id') and execution is not None and \
                bool(execution.get('id')) and execution.get('id') == outcomes[0].get('executionId') == entries[0].get('executionId')
            evidence['unknown_test_identity_count'] += int(not identity_matches)
            not_executed = sum(result.get('outcome') == 'NotExecuted' for result in outcomes)
            diagnostic['not_executed_result_count'] = not_executed
            receipt['not_executed_result_count'] = not_executed
            valid = identity_matches and not_executed == 1 and completed and \
                counters == dict.fromkeys(TRX_COUNTERS, 0) | {'total': 1}
            if valid:
                diagnostic['status'] = 'inherited-skipped-placeholder'
                evidence['inherited_skipped_placeholders'].append(receipt | policy)
            else:
                failures.add('placeholder-result-invalid')
        elif original_core(row):
            from selected_core_producer import verify_outcomes
            try:
                require(linked and completed, 'core_test_linkage_or_summary')
                skipped = verify_outcomes(definitions, outcomes, counters, row, environment or {}, cell['project'])
                diagnostic['status'] = 'passed-with-source-bound-skips' if skipped else 'passed'
                receipt['expected_skips'] = skipped
                evidence['positive_summary_count'] += 1
                results.append(receipt)
            except ValueError:
                failures.add('core-test-counts-outcomes-or-skips-invalid')
        else:
            valid = linked and completed and counters['total'] == counters['executed'] == counters['passed'] == len(outcomes) > 0 and \
                all(counters[name] == 0 for name in TRX_COUNTERS if name not in ('total', 'executed', 'passed')) and \
                all(result.get('outcome') == 'Passed' for result in outcomes)
            if valid:
                diagnostic['status'] = 'passed'
                evidence['positive_summary_count'] += 1
                results.append(receipt)
            else:
                if not linked:
                    failures.add('test-execution-linkage-invalid')
                if not completed:
                    failures.add('test-summary-invalid')
                failures.add('test-counts-or-outcomes-invalid')
    admitted = [cell for cell in cells if cell in expected]
    evidence.update(admitted_observed_cells=[expected[cell] | {'occurrences': admitted.count(cell)}
        for cell in dict.fromkeys(admitted)], unknown_path_count=len(cells) - len(admitted),
        duplicate_cell_count=len(cells) - len(set(cells)))
    if len(cells) != len(set(cells)) or set(cells) != expected.keys():
        failures.add('test-cells-incomplete-or-duplicate')
    evidence['failure_reasons'] = sorted(failures)
    require(not failures, 'Test evidence rejected')
    return {'executions': results, 'inherited_skipped_placeholders': evidence['inherited_skipped_placeholders']}


def inspect_toolchain(source: Path, row: dict) -> dict:
    tools = {'dotnet': ['dotnet', '--list-sdks']}
    if row['product'] == 'studio':
        tools.update(node=['node', '--version'], npm=['npm', '--version'])
    return {tool: [line.split()[0] for line in run(command, source, env=build_environment()).splitlines()]
            for tool, command in tools.items()}


def prepare(root: Path, row: dict, version: str, output: Path, *, plan: dict | None = None) -> dict:
    require(not output.exists(), 'Output must be new; retain prior evidence')
    require(not output.is_relative_to(root), 'Output must be outside the controller checkout')
    output.mkdir(parents=True)
    receipt = {'schema': 1, 'published': False, 'maintenance_refs_activated': False, 'success': False,
        'selection': row, 'version': version, 'proof_build_properties': PROOF_BUILD_PROPERTIES, 'controller_commit': git(root, 'rev-parse', 'HEAD'),
        'controller_tree': git(root, 'rev-parse', 'HEAD^{tree}'),
        'controller_sha256': digest(Path(__file__).read_bytes()), 'register_sha256': digest(REGISTER.read_bytes()),
        'run_id': os.environ.get('GITHUB_RUN_ID'), 'run_attempt': os.environ.get('GITHUB_RUN_ATTEMPT'), 'commands': []}
    if row.get('source_kind') == 'core':
        receipt['candidates_sha256'] = digest(CANDIDATES.read_bytes())
        receipt['containment_sha256'] = digest(CONTAINMENT.read_bytes())
    source = output / 'source'
    try:
        receipt['stage'] = 'source-verification'
        if original_core(row):
            from selected_core_producer import verify_source as verify_original_source, validate_plan
            require(plan is not None and plan['source'] == {key: value for key, value in row.items()
                    if key != 'source_repository'}, 'core_original_plan_binding')
            validate_plan(plan)
            verify_original_source(root, row)
        else:
            require(row['product'] in ('studio', 'extensions'), 'Unsupported producer product')
            verify_source(root, row)
        source.mkdir()
        git(source, 'init', '--quiet')
        git(source, 'fetch', '--no-tags', str(root), row['commit'])
        git(source, 'checkout', '--detach', '--quiet', row['commit'])
        git(source, 'remote', 'add', 'origin', 'https://github.com/' + row['source_repository'] + '.git')
        config = source / 'NuGet.Config'
        require(config.is_file() and not config.is_symlink() and
                config.read_bytes() == git_bytes(root, row['commit'], 'NuGet.Config'),
                'Original restore config differs from source')
        if plan is not None:
            require(digest(config.read_bytes()) == plan['consumer_feed_policy']['config_sha256'],
                    'Original restore config differs from plan')
        receipt['source_policy_files'] = {name: digest((source / name).read_bytes())
            for name in ('Directory.Build.props', 'Directory.Packages.props') if (source / name).is_file()}
        receipt['stage'] = 'toolchain'
        receipt['toolchain'] = inspect_toolchain(source, row)
        environment = recipe_environment(row, version, source)
        (output / 'artifacts').mkdir()
        for index, (directory, command) in enumerate(recipes(row, version, output)):
            log = output / f'command-{index:02}.log'
            receipt['stage'] = 'build-command'
            receipt['focus'] = {'step': index + 1, 'directory': directory}
            record = {'cwd': directory, 'argv': [arg.replace(str(output), '$OUTPUT') for arg in command], 'success': False}
            receipt['commands'].append(record)
            run_build_command(command, source / directory, log, record, environment=environment)
        if row['product'] == 'extensions' or original_core(row):
            for path in (source / 'packages').glob('*nupkg'):
                shutil.copyfile(path, output / 'artifacts' / path.name)
        receipt['stage'] = 'inventory'
        inventory = evaluate_inventory(source, row, version, output)
        if original_core(row):
            from selected_core_producer import policy as core_policy
            tests = json.loads((output / 'test-inventory.json').read_text())
            require({item['project'] for item in tests} == set(core_policy(row)['test_projects']) and
                    all(item['frameworks'] == ['net10.0'] for item in tests), 'core_original_test_inventory')
            selected = {item['id']: item for item in plan['inventory']['selected']}
            require({item['id'] for item in inventory} == set(selected), 'core_original_package_inventory')
            require(all(item['project'] == selected[item['id']]['project'] and
                        item['frameworks'] == selected[item['id']]['frameworks'] and
                        item['symbols'] == selected[item['id']]['symbols'] for item in inventory),
                    'core_original_package_policy')
        write_json(output / 'evaluated-inventory.json', public_inventory(inventory))
        receipt['stage'] = 'test-evidence'
        receipt.pop('focus', None)
        receipt['test_evidence'] = {}
        receipt['tests'] = (verify_tests(output, row, receipt['test_evidence'], environment=environment)
                            if original_core(row) else verify_tests(output, row, receipt['test_evidence']))
        receipt['stage'] = 'symbol-inspector'
        helper = root / 'scripts/integration-program/VerifyPackageSymbolPair'
        inspector_out = output / 'symbol-verifier'
        run(['dotnet', 'build', str(helper / 'VerifyPackageSymbolPair.csproj'), '--configuration', 'Release',
             '--output', str(inspector_out)], root, log=output / 'symbol-verifier.log', timeout=600, env=build_environment())
        receipt['stage'] = 'sdk-metadata'
        stage_maintenance_metadata(source, row, inventory, version, inspector_out / 'VerifyPackageSymbolPair.dll',
                                   diagnostics=output / 'sdk-metadata-diagnostics')
        write_json(output / 'evaluated-inventory.json', public_inventory(inventory))
        receipt['stage'] = 'package-verification'
        receipt['focus'] = {}
        receipt['packages'] = verify_artifacts(output / 'artifacts', inventory, row, version, source,
            inspector_out / 'VerifyPackageSymbolPair.dll', output, receipt['focus'])
        receipt['stage'] = 'complete'
        receipt.pop('focus', None)
        receipt['success'] = True
        return receipt
    except Exception as error:
        receipt['error'] = {'code': receipt['stage'] + '-failed',
                            'reason': verification_reason(str(error))}
        if receipt['stage'] == 'package-verification' and isinstance(error, SourceProducerVerificationError):
            receipt['error']['source_producer_check'] = error.check
        raise
    finally:
        receipt['logs'] = [{'name': p.name, 'sha256': digest(p.read_bytes())} for p in sorted(output.glob('*.log'))]
        write_json(output / 'receipt.json', receipt)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--product', choices=['studio', 'extensions'])
    parser.add_argument('--line', choices=['3.8', '3.9'])
    parser.add_argument('--commit')
    parser.add_argument('--source-kind', choices=['original', 'core'], default='original')
    parser.add_argument('--version')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--prepare-containment', action='store_true')
    parser.add_argument('--workflow-build', action='store_true')
    args = parser.parse_args()
    try:
        register = load_register()
        if args.prepare_containment:
            require(not any((args.product, args.line, args.commit, args.version, args.workflow_build)), 'Containment takes no build selection')
            prepare_containment(ROOT, args.output.resolve(), register)
        else:
            if args.workflow_build:
                selected = build_selection(register, {'product': args.product, 'line': args.line, 'commit': args.commit,
                    'source_kind': args.source_kind, 'version': args.version}, dict(os.environ))
                version = selected.pop('version')
                row = selected
            else:
                version = args.version
                row = selection(register, args.product, args.line, args.commit, version, args.source_kind)
            prepare(ROOT, row, version, args.output.resolve())
    except Exception:
        # Raw build outputs/tracebacks stay private on the runner. The retained
        # receipt identifies the closed stage and safe package/framework focus.
        print(json.dumps({'success': False, 'code': 'maintenance-proof-failed'}), file=sys.stderr)
        sys.exit(1)


if __name__ == '__main__':
    main()
