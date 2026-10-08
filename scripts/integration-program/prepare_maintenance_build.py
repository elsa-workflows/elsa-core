#!/usr/bin/env python3
"""Core-controlled, original-source maintenance rehearsal. Never publishes."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile

from prove_consolidated_packages import archive_names, dependency_groups, metadata, only_abstract_methods, require, run, source_url

ROOT = Path(__file__).resolve().parents[2]
REGISTER = ROOT / 'docs/integration-program/maintenance-source-register.json'
PROOF_BUILD_PROPERTIES = {'EmbedUntrackedSources': 'true'}
TRX_COUNTERS = ('total', 'executed', 'passed', 'failed', 'error', 'timeout', 'aborted', 'inconclusive',
                'passedButRunAborted', 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending')


def load_register() -> dict:
    return json.loads(REGISTER.read_text())


def selection(register: dict, product: str, line: str, commit: str, version: str) -> dict:
    matches = [row for row in register['sources']
               if (row['product'], row['line'], row['commit']) == (product, line, commit)]
    require(len(matches) == 1 and re.fullmatch(r'[a-f0-9]{40}', commit), 'Unregistered product/line/source commit')
    # Proof-only identity: never reuse a released version or infer one from refs.
    require(isinstance(version, str) and re.fullmatch(re.escape(line) + r'\.(0|[1-9][0-9]*)-proof\.[1-9][0-9]*\.[1-9][0-9]*', version) is not None,
            'Expected same-line unpublished version MAJOR.MINOR.PATCH-proof.RUN.ATTEMPT')
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
    require(git(root, 'rev-parse', row['commit'] + '^{tree}') == row['tree'], 'Source tree mismatch')
    require(git(root, 'rev-parse', row['commit'] + '^') == row['parent'], 'Source parent mismatch')
    workflows = git(root, 'ls-tree', '-r', '--name-only', row['commit'], '--', '.github/workflows').splitlines()
    require(workflows == row['workflows'], 'Source workflow inventory changed')


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
}


def verification_reason(message: str) -> str:
    if message in VERIFICATION_REASONS:
        return VERIFICATION_REASONS[message]
    for key in ('Unexpected evaluated version', 'Unexpected Elsa dependency'):
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


def run_build_command(command: list[str], cwd: Path, log: Path, record: dict) -> None:
    try:
        run(command, cwd, timeout=7200, log=log, env=build_environment(), outcome=record.setdefault('process', {}))
        record['success'] = True
    finally:
        record['diagnostics'] = closed_diagnostics(log)


def recipes(row: dict, version: str, output: Path) -> list[tuple[str, list[str]]]:
    if row['product'] == 'extensions':
        return [('.', ['./build.sh', 'Compile+Test+Pack', '--version', version, '--analyseCode', 'true'])]
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


def evaluate_satellites(source: Path, project: str, framework: str, assembly: str, version: str) -> list[dict]:
    result = json.loads(run(['dotnet', 'msbuild', project, '-nologo', '-p:Configuration=Release',
        f'-p:Version={version}', f'-p:TargetFramework={framework}', '-target:SatelliteDllsProjectOutputGroup',
        '-getItem:SatelliteDllsProjectOutputGroupOutput'], source, env=build_environment()))
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
    solution = source / ('Elsa.Studio.sln' if row['product'] == 'studio' else 'Elsa.Extensions.sln')
    projects = re.findall(r'^Project\([^\n]+?= "[^"]+", "([^"]+\.csproj)"', solution.read_text(encoding='utf-8-sig'), re.M)
    require(bool(projects), 'No solution projects')
    properties = 'IsPackable,IsTestProject,AssemblyName,PackageId,PackageVersion,TargetFrameworks,TargetFramework,IncludeSymbols,IncludeBuildOutput'
    inventory, tests = [], []
    for project in projects:
        project = project.replace('\\', '/')
        require(not Path(project).is_absolute() and '..' not in Path(project).parts, 'Unsafe solution project')
        values = json.loads(run(['dotnet', 'msbuild', project, '-nologo', '-p:Configuration=Release',
            f'-p:Version={version}', f'-getProperty:{properties}'], source, env=build_environment()))['Properties']
        frameworks = (values['TargetFrameworks'] or values['TargetFramework']).split(';')
        if values['IsTestProject'].lower() == 'true':
            tests.append({'project': project, 'assembly_name': values['AssemblyName'], 'frameworks': frameworks})
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
                evaluate_satellites(source, project, framework, values['AssemblyName'], version)]
                if values['IncludeBuildOutput'].lower() == 'true' else []})
    require(bool(inventory) and len({p['id'].casefold() for p in inventory}) == len(inventory), 'Empty/duplicate package inventory')
    require(bool(tests), 'No evaluated test projects')
    write_json(output / 'test-inventory.json', tests)
    write_json(output / 'evaluated-inventory.json', inventory)
    return inventory


def verify_documents(details: dict, source: Path, row: dict) -> list[dict]:
    maps = details['source_link']['documents']
    prefix = f"https://raw.githubusercontent.com/{row['source_repository']}/{row['commit']}/"
    require(bool(maps) and all(value.startswith(prefix) for value in maps.values()), 'Unexpected SourceLink repository/commit')
    documents = []
    for index, document in enumerate(details['documents']):
        checksum = document['checksum']
        require(document['algorithm'] in ('sha1', 'sha256'), 'Unsupported document hash')
        url = source_url(document['path'], maps)
        if url is not None:
            require(url.startswith(prefix), 'Foreign SourceLink document')
            path = url[len(prefix):]
            require('..' not in Path(path).parts and not Path(path).is_absolute(), 'Unsafe SourceLink path')
            # A wildcard may also map generated files absent from the original tree.
            tracked = bool(git(source, 'ls-tree', row['commit'], '--', ':(literal)' + path))
        else:
            tracked = False
        if tracked:
            # Read immutable source bytes, never regenerated workspace files.
            data = subprocess.run(['git', 'show', f"{row['commit']}:{path}"], cwd=source,
                env=build_environment(), check=True, capture_output=True, timeout=30).stdout
            require(hashlib.new(document['algorithm'], data).hexdigest() == checksum, 'Tracked source checksum mismatch')
            evidence = {'path': path, 'source': 'original-git', 'url': url}
        else:
            require(document.get('embedded_checksum') == checksum, 'Unmapped source document is not verified embedded content')
            evidence = {'path': f'[embedded]/document-{index + 1}', 'source': 'embedded'}
        documents.append(evidence | {'algorithm': document['algorithm'], 'checksum': checksum})
    if not documents:
        require(only_abstract_methods(details) and type(details.get('nonmodule_types')) is int and
                details['nonmodule_types'] > 0 and details.get('reference_assembly') is False,
                'PDB source documents missing')
    return documents


def verify_artifacts(artifacts: Path, inventory: list[dict], row: dict, version: str, source: Path,
                     inspector: Path, output: Path, context: dict | None = None) -> list[dict]:
    expected = {p['id'].casefold(): p for p in inventory}
    produced = set(expected)
    found = set()
    receipts = []
    with tempfile.TemporaryDirectory(prefix='maintenance-symbols-') as temporary:
        for path in sorted(artifacts.glob('*.nupkg')):
            with zipfile.ZipFile(path) as package:
                nuspec = metadata(package)
                identifier = nuspec.findtext('id', '')
                require(identifier.casefold() in expected and identifier.casefold() not in found, 'Unknown/duplicate package')
                found.add(identifier.casefold())
                policy = expected[identifier.casefold()]
                if context is not None:
                    context.update(package=policy['id'])
                require(nuspec.findtext('version') == version, 'Packed version mismatch')
                repository = nuspec.find('repository')
                require(repository is not None and repository.get('commit') == row['commit'] and
                    repository.get('url', '').removesuffix('.git').rstrip('/') == 'https://github.com/' + row['source_repository'],
                    'Packed repository provenance mismatch')
                dependencies = dependency_groups(nuspec)
                for group in dependencies:
                    for dependency in group['dependencies']:
                        if dependency['id'].casefold().startswith('elsa'):
                            target = version if dependency['id'].casefold() in produced else row['dependency_version']
                            require(dependency['version'] in (target, f'[{target}]', f'[{target}, )', f'[{target},)'),
                                    f"Unexpected Elsa dependency: {identifier} -> {dependency}")
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
                require(not expected_assemblies or policy['symbols'] and symbols_path.is_file(), 'Symbol package missing')
                symbols = []
                if symbols_path.is_file():
                    with zipfile.ZipFile(symbols_path) as symbol_package:
                        symbol_metadata = metadata(symbol_package)
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
                            require(pdb_name in symbol_names, 'Framework PDB missing')
                            dll, pdb = Path(temporary) / Path(name).name, Path(temporary) / Path(pdb_name).name
                            dll.write_bytes(package.read(name)); pdb.write_bytes(symbol_package.read(pdb_name))
                            inspection = json.loads(run(['dotnet', str(inspector), str(dll), str(pdb), '--inspect-symbols'],
                                source, env=build_environment()))
                            details = inspection['details']
                            require(details['assembly_name'] == policy['assembly_name'], 'Packaged assembly identity mismatch')
                            source_evidence = {'documents': verify_documents(details, source, row)}
                            if not source_evidence['documents']:
                                source_evidence['source_applicability'] = {
                                    'classification': 'no-documents-no-executable-method-bodies', 'document_count': 0,
                                    **{key: details[key] for key in ('executable_method_bodies', 'nonabstract_methods_without_body',
                                       'native_or_external_methods', 'nonmodule_types', 'reference_assembly')}}
                            symbols.append({'assembly': name, 'assembly_sha256': digest(dll.read_bytes()),
                                'pdb': pdb_name, 'pdb_sha256': digest(pdb.read_bytes()), 'symbol': {key: inspection['symbol'][key] for key in ('key', 'pdb_name', 'guid', 'stamp',
                                    'checksum_algorithm', 'declared_checksum', 'normalized_checksum', 'pdb_sha256', 'pdb_size')}, 'assembly_version': details['assembly_version'],
                                'informational_version': details['informational_version'],
                                **source_evidence})
                receipts.append({'id': identifier, 'version': version, 'frameworks': frameworks,
                    'assembly_name': policy['assembly_name'], 'include_build_output': policy['include_build_output'],
                    'satellites': policy['satellites'],
                    'dependencies': dependencies, 'repository': dict(repository.attrib), 'symbols': symbols,
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
        if row['product'] != policy['product'] or row['commit'] not in policy['commits']:
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


def verify_tests(output: Path, row: dict, context: dict | None = None) -> dict:
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
    policies = placeholder_policies(source, row)
    require(set(policies) <= {(cell['project'], cell['framework']) for cell in expected.values()},
            'Inherited placeholder inventory mismatch')
    cells, results, failures = [], [], set()
    # Original NUKE adds TRX and its AnalyseCode=true recipe writes here.
    results_directory = output / 'test-results' if row['product'] == 'studio' else source / 'testresults'
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


def prepare(root: Path, row: dict, version: str, output: Path) -> dict:
    require(not output.exists(), 'Output must be new; retain prior evidence')
    require(not output.is_relative_to(root), 'Output must be outside the controller checkout')
    output.mkdir(parents=True)
    receipt = {'schema': 1, 'published': False, 'maintenance_refs_activated': False, 'success': False,
        'selection': row, 'version': version, 'proof_build_properties': PROOF_BUILD_PROPERTIES, 'controller_commit': git(root, 'rev-parse', 'HEAD'),
        'controller_tree': git(root, 'rev-parse', 'HEAD^{tree}'),
        'controller_sha256': digest(Path(__file__).read_bytes()), 'register_sha256': digest(REGISTER.read_bytes()),
        'run_id': os.environ.get('GITHUB_RUN_ID'), 'run_attempt': os.environ.get('GITHUB_RUN_ATTEMPT'), 'commands': []}
    source = output / 'source'
    try:
        receipt['stage'] = 'source-verification'
        verify_source(root, row)
        source.mkdir()
        git(source, 'init', '--quiet')
        git(source, 'fetch', '--no-tags', str(root), row['commit'])
        git(source, 'checkout', '--detach', '--quiet', row['commit'])
        git(source, 'remote', 'add', 'origin', 'https://github.com/' + row['source_repository'] + '.git')
        receipt['source_policy_files'] = {name: digest((source / name).read_bytes())
            for name in ('Directory.Build.props', 'Directory.Packages.props') if (source / name).is_file()}
        receipt['stage'] = 'toolchain'
        receipt['toolchain'] = {tool: [line.split()[0] for line in run(command, source, env=build_environment()).splitlines()] for tool, command in {
            'dotnet': ['dotnet', '--list-sdks'], 'node': ['node', '--version'], 'npm': ['npm', '--version']}.items()}
        (output / 'artifacts').mkdir()
        for index, (directory, command) in enumerate(recipes(row, version, output)):
            log = output / f'command-{index:02}.log'
            receipt['stage'] = 'build-command'
            receipt['focus'] = {'step': index + 1, 'directory': directory}
            record = {'cwd': directory, 'argv': [arg.replace(str(output), '$OUTPUT') for arg in command], 'success': False}
            receipt['commands'].append(record)
            run_build_command(command, source / directory, log, record)
        if row['product'] == 'extensions':
            for path in (source / 'packages').glob('*nupkg'):
                shutil.copyfile(path, output / 'artifacts' / path.name)
        receipt['stage'] = 'inventory'
        inventory = evaluate_inventory(source, row, version, output)
        receipt['stage'] = 'test-evidence'
        receipt.pop('focus', None)
        receipt['test_evidence'] = {}
        receipt['tests'] = verify_tests(output, row, receipt['test_evidence'])
        receipt['stage'] = 'symbol-inspector'
        helper = root / 'scripts/integration-program/VerifyPackageSymbolPair'
        inspector_out = output / 'symbol-verifier'
        run(['dotnet', 'build', str(helper / 'VerifyPackageSymbolPair.csproj'), '--configuration', 'Release',
             '--output', str(inspector_out)], root, log=output / 'symbol-verifier.log', timeout=600, env=build_environment())
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
        raise
    finally:
        receipt['logs'] = [{'name': p.name, 'sha256': digest(p.read_bytes())} for p in sorted(output.glob('*.log'))]
        write_json(output / 'receipt.json', receipt)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--product', choices=['studio', 'extensions'])
    parser.add_argument('--line', choices=['3.8', '3.9'])
    parser.add_argument('--commit')
    parser.add_argument('--version')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--prepare-containment', action='store_true')
    args = parser.parse_args()
    try:
        register = load_register()
        if args.prepare_containment:
            require(not any((args.product, args.line, args.commit, args.version)), 'Containment takes no build selection')
            prepare_containment(ROOT, args.output.resolve(), register)
        else:
            row = selection(register, args.product, args.line, args.commit, args.version)
            prepare(ROOT, row, args.version, args.output.resolve())
    except Exception:
        # Raw build outputs/tracebacks stay private on the runner. The retained
        # receipt identifies the closed stage and safe package/framework focus.
        print(json.dumps({'success': False, 'code': 'maintenance-proof-failed'}), file=sys.stderr)
        sys.exit(1)


if __name__ == '__main__':
    main()
