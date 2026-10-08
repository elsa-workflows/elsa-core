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

from prove_consolidated_packages import archive_names, dependency_groups, metadata, require, run, source_url

ROOT = Path(__file__).resolve().parents[2]
REGISTER = ROOT / 'docs/integration-program/maintenance-source-register.json'


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
    return {key: value for key, value in os.environ.items() if key in names}


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
            patch = git(root, 'diff', '--binary', row['commit'], commit)
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
        command = ['dotnet', target, 'Elsa.Studio.sln', '--configuration', 'Release', f'/p:Version={version}']
        if target == 'test':
            command.extend(['--no-build', '--logger', 'trx', '--results-directory', str(output / 'test-results')])
        if target == 'pack':
            command.append('/p:PackageOutputPath=' + str(output / 'artifacts'))
        commands.append(('.', command))
    return commands


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
            'symbols': values['IncludeSymbols'].lower() == 'true'})
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
    for document in details['documents']:
        checksum = document['checksum']
        require(document['algorithm'] in ('sha1', 'sha256'), 'Unsupported document hash')
        url = source_url(document['path'], maps)
        if url is not None:
            require(url.startswith(prefix), 'Foreign SourceLink document')
            path = url[len(prefix):]
            require('..' not in Path(path).parts and not Path(path).is_absolute(), 'Unsafe SourceLink path')
            # Read immutable source bytes, not potentially changed generated workspace files.
            data = subprocess.run(['git', 'show', f"{row['commit']}:{path}"], cwd=source,
                env=build_environment(), check=True, capture_output=True, timeout=30).stdout
            require(hashlib.new(document['algorithm'], data).hexdigest() == checksum, 'Tracked source checksum mismatch')
            evidence = {'path': path, 'source': 'original-git', 'url': url}
        else:
            require(document.get('embedded_checksum') == checksum, 'Unmapped source document is not verified embedded content')
            evidence = {'path': '[embedded]/' + Path(document['path']).name, 'source': 'embedded'}
        documents.append(evidence | {'algorithm': document['algorithm'], 'checksum': checksum})
    require(bool(documents), 'PDB source documents missing')
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
                require(assemblies == expected_assemblies, 'Packed assembly payload differs from evaluated build-output policy')
                symbols_path = path.with_suffix('.snupkg')
                require(not assemblies or policy['symbols'] and symbols_path.is_file(), 'Symbol package missing')
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
                        for name in assemblies:
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
                            symbols.append({'assembly': name, 'assembly_sha256': digest(dll.read_bytes()),
                                'pdb': pdb_name, 'pdb_sha256': digest(pdb.read_bytes()), 'symbol': {key: inspection['symbol'][key] for key in ('key', 'pdb_name', 'guid', 'stamp',
                                    'checksum_algorithm', 'declared_checksum', 'normalized_checksum', 'pdb_sha256', 'pdb_size')}, 'assembly_version': details['assembly_version'],
                                'informational_version': details['informational_version'],
                                'documents': verify_documents(details, source, row)})
                receipts.append({'id': identifier, 'version': version, 'frameworks': frameworks,
                    'assembly_name': policy['assembly_name'], 'include_build_output': policy['include_build_output'],
                    'dependencies': dependencies, 'repository': dict(repository.attrib), 'symbols': symbols,
                    'files': [{'name': p.name, 'sha256': digest(p.read_bytes()), 'size': p.stat().st_size}
                              for p in (path, symbols_path) if p.exists()]})
    require(found == produced, 'Missing evaluated packages')
    expected_files = {f['name'] for p in receipts for f in p['files']}
    require({p.name for p in artifacts.iterdir()} == expected_files, 'Unexpected artifact files')
    write_json(output / 'verified-artifacts.json', receipts)
    return receipts


def verify_tests(output: Path, row: dict) -> dict:
    tests = json.loads((output / 'test-inventory.json').read_text())
    source = output / 'source'
    expected = {str((source / Path(test['project']).parent / 'bin/Release' / framework /
                    (test['assembly_name'] + '.dll')).resolve()):
                {'project': test['project'], 'framework': framework}
                for test in tests for framework in test['frameworks']}
    require(bool(expected), 'No required test framework executions')
    cells = []
    if row['product'] == 'studio':
        results = []
        for path in sorted((output / 'test-results').glob('*.trx')):
            tree = ET.parse(path)
            counters = tree.find('.//{*}Counters')
            require(counters is not None and int(counters.get('total', '0')) > 0 and
                    int(counters.get('failed', '0')) == 0 and int(counters.get('passed', '0')) > 0, 'Missing/failed tests')
            assemblies = {str(Path(method.get('codeBase', '')).resolve()) for method in tree.findall('.//{*}TestMethod')}
            require(len(assemblies) == 1 and assemblies <= expected.keys(), 'Unknown test project/framework identity')
            assembly = next(iter(assemblies))
            cells.append(assembly)
            results.append(expected[assembly] | {'counters': counters.attrib, 'sha256': digest(path.read_bytes())})
        require(len(cells) == len(set(cells)) and set(cells) == expected.keys(), 'Missing/duplicate test project-framework results')
        return {'executions': results}
    log = (output / 'command-00.log').read_text()
    cells = [str(Path(path).resolve()) for path in re.findall(r'Test run for (.+\.dll) \(', log)]
    totals = re.findall(r'Passed!\s*-\s*Failed:\s*0,\s*Passed:\s*(\d+),\s*Skipped:\s*\d+,\s*Total:\s*(\d+)', log)
    require(len(cells) == len(set(cells)) and set(cells) == expected.keys() and len(totals) == len(cells) and
            all(int(passed) > 0 and int(total) >= int(passed) for passed, total in totals),
            'Missing/duplicate/nonpositive Extensions test execution evidence')
    return {'executions': [expected[cell] for cell in cells],
            'successful_test_summaries': [{'passed': int(p), 'total': int(t)} for p, t in totals]}


def prepare(root: Path, row: dict, version: str, output: Path) -> dict:
    require(not output.exists(), 'Output must be new; retain prior evidence')
    require(not output.is_relative_to(root), 'Output must be outside the controller checkout')
    output.mkdir(parents=True)
    receipt = {'schema': 1, 'published': False, 'maintenance_refs_activated': False, 'success': False,
        'selection': row, 'version': version, 'controller_commit': git(root, 'rev-parse', 'HEAD'),
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
            run(command, source / directory, timeout=7200, log=log, env=build_environment())
            record['success'] = True
        if row['product'] == 'extensions':
            for path in (source / 'packages').glob('*nupkg'):
                shutil.copyfile(path, output / 'artifacts' / path.name)
        receipt['stage'] = 'inventory'
        inventory = evaluate_inventory(source, row, version, output)
        receipt['stage'] = 'test-evidence'
        receipt.pop('focus', None)
        receipt['tests'] = verify_tests(output, row)
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
    except Exception:
        receipt['error'] = {'code': receipt['stage'] + '-failed'}
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
