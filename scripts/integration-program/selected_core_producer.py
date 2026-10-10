"""Original Core producer recipes and exact reviewed maintenance continuations."""
from __future__ import annotations

import json
import os
from pathlib import Path

import core_source_continuation as continuation
import prepare_maintenance_build as maintenance
from prove_consolidated_packages import require

CONTRACT = Path(__file__).with_name('selected_core_contract.json')
PROVIDER_GATES = ('ELSA_USERTASKS_TEST_SQLSERVER', 'ELSA_USERTASKS_TEST_POSTGRES', 'ELSA_USERTASKS_TEST_ORACLE')
SAMPLE_PROJECT = 'src/apps/Elsa.SamplePackage/Elsa.SamplePackage.csproj'


def original(row: dict) -> bool:
    """Sources preserving the original Core recipe and exported VERSION handling."""
    return row.get('product') == 'core' and row.get('kind') in ('observed-core-release-branch', continuation.KIND)


def source_binding(row: dict) -> dict:
    # prepare() adds a clone origin after the plan's closed source binding.
    if 'source_repository' not in row:
        return row
    require(row['source_repository'] == 'elsa-workflows/elsa-core', 'core_source_repository')
    return {key: value for key, value in row.items() if key != 'source_repository'}


def policy(row: dict) -> dict:
    row = source_binding(row)
    require(original(row), 'core_original_selection')
    value = json.loads(CONTRACT.read_text())['sources'].get(row.get('line'))
    if row.get('kind') == continuation.KIND:
        require(value is not None, 'core_original_source')
        return continuation.policy(row, value, continuation.load_contract())
    require(value is not None and (row.get('commit'), row.get('tree')) == (value['commit'], value['tree']),
            'core_original_source')
    return value


def verify_source(root: Path, row: dict) -> None:
    row = source_binding(row)
    value = policy(row)
    if row.get('kind') == continuation.KIND:
        original = json.loads(CONTRACT.read_bytes())['sources'][row['line']]
        continuation.verify_source(root, row, original, continuation.load_contract())
    require(maintenance.git(root, 'rev-parse', row['commit'] + '^{tree}') == row['tree'], 'core_original_tree')
    for path, blob in value['files'].items():
        require(maintenance.git(root, 'rev-parse', row['commit'] + ':' + path) == blob, 'core_original_blob')


def validate_plan(plan: dict) -> None:
    if plan['source'].get('kind') == continuation.KIND:
        require(set(plan['source']) == continuation.SOURCE_KEYS, 'core_continuation_source_shape')
    value = policy(plan['source'])
    require(plan['product'] == 'core' and plan['line'] == plan['source']['line'] and plan['npm'] is None,
            'core_original_plan')
    inventory = plan['inventory']
    continuation.validate_project_metadata(plan['source'], inventory['selected'], continuation.load_contract())
    require(inventory['release_recipe']['solution'] == 'Elsa.sln' and
            inventory['release_recipe']['workflow'] == '.github/workflows/packages.yml', 'core_original_recipe')
    tests = {row['path'] for row in inventory['projects'] if row['is_test_project']}
    require(tests == set(value['test_projects']), 'core_original_test_inventory')
    for row in inventory['selected']:
        require(row['symbols'] is True or (row['id'], row['project']) == ('Elsa.SamplePackage', SAMPLE_PROJECT),
                'core_original_symbols_policy')


def environment(row: dict, version: str, supplied: dict | None = None) -> dict:
    policy(row)
    supplied = os.environ if supplied is None else supplied
    result = maintenance.build_environment()
    # Explicit Release is passed to the recipe; do not impersonate server/GitHub execution.
    result.pop('CI', None)
    result.pop('GITHUB_ACTIONS', None)
    result['VERSION'] = version
    for name in PROVIDER_GATES:
        if name in supplied:
            result[name] = supplied[name]
    return result


def recipes(row: dict, version: str, output: Path) -> list[tuple[str, list[str]]]:
    value = policy(row)
    commands = [('.', ['./build.sh', 'Compile+Pack', '--version', version, '--analyseCode', 'true',
                       '--configuration', 'Release'])]
    for project in value['test_projects']:
        base = [project, '--configuration', 'Release', '--framework', 'net10.0']
        commands.append(('.', ['dotnet', 'build', *base]))
        test = ['dotnet', 'test', *base, '--no-build', '--logger',
                'trx;LogFileName=' + Path(project).stem + '.trx', '--results-directory', str(output / 'test-results'),
                '/p:CollectCoverage=true']
        if project.startswith('test/component/'):
            test += ['--verbosity', 'detailed', '--blame-hang', '--blame-hang-timeout', '2m',
                     '--blame-hang-dump-type', 'mini']
        commands.append(('.', test))
    return commands


def expected_skips(row: dict, supplied: dict) -> dict[str, str]:
    value = policy(row)
    result = {case['display_identity']: case['reason'] for case in value['declared_skips']}
    conditional = value['conditional']
    if conditional:
        for gate in conditional['gates']:
            if supplied.get(gate['env']):
                continue
            reason = f"{gate['description']} is not covered by this run: set {gate['env']} to a connection string to include it."
            for cls in conditional['classes']:
                if cls['provider'] != gate['provider']:
                    continue
                for method in conditional['methods']:
                    if method['base_class'] == cls['base_class']:
                        identity = 'Elsa.UserTasks.Persistence.ConformanceTests.' + cls['class'] + '.' + method['method']
                        require(identity not in result, 'core_skip_duplicate')
                        result[identity] = reason
    return result


def verify_outcomes(definitions: list, outcomes: list, counters: dict, row: dict, supplied: dict,
                    project: str) -> list[dict]:
    allowed = expected_skips(row, supplied)
    prefix = 'Elsa.UserTasks.Persistence.ConformanceTests.' if Path(project).stem == 'Elsa.UserTasks.Persistence.ConformanceTests' else None
    required = {key for key in allowed if (prefix and key.startswith(prefix)) or
                (Path(project).stem == 'Elsa.Workflows.ComponentTests' and key.startswith('Elsa.Workflows.ComponentTests.'))}
    identities = {item.get('id'): item.find('{*}TestMethod') for item in definitions}
    skipped, seen = [], set()
    for result in outcomes:
        method = identities[result.get('testId')]
        identity = method.get('className', '') + '.' + method.get('name', '')
        if result.get('outcome') == 'Passed':
            require(identity not in required, 'core_expected_skip_executed')
            continue
        require(result.get('outcome') == 'NotExecuted' and identity in required and identity not in seen,
                'core_skip_identity')
        reason = allowed[identity]
        messages = [item.text for item in result.findall('./{*}Output/{*}ErrorInfo/{*}Message') +
                    result.findall('./{*}Output/{*}StdOut')]
        require(reason in messages and result.get('testName') == identity, 'core_skip_reason_or_display')
        skipped.append({'identity': identity, 'reason': reason})
        seen.add(identity)
    require(seen == required and counters['passed'] > 0 and
            counters['total'] == counters['passed'] + len(skipped) == len(outcomes) and
            counters['executed'] == counters['passed'] and counters['notExecuted'] in (0, len(skipped)) and
            all(value == 0 for key, value in counters.items() if key not in ('total', 'executed', 'passed', 'notExecuted')),
            'core_skip_counts')
    return skipped


def verify_assembly(details: dict, package: dict, framework: str, row: dict) -> None:
    policy(row)
    expected = package['framework_properties'][framework]['assembly_policy']
    require(details['assembly_version'] == expected['AssemblyVersion'] and
            details['informational_version'] == expected['InformationalVersion'] and
            expected['InformationalVersion'].endswith('+' + row['commit']), 'core_original_assembly_policy')


def private_symbols(source: Path, package: dict, framework: str, payload: bytes) -> Path:
    require((package['id'], package['project'], package['symbols']) ==
            ('Elsa.SamplePackage', SAMPLE_PROJECT, False), 'core_original_symbols_policy')
    folder = source / Path(package['project']).parent / 'bin/Release' / framework
    dll, pdb = folder / (package['assembly_name'] + '.dll'), folder / (package['assembly_name'] + '.pdb')
    require(dll.is_file() and pdb.is_file() and not dll.is_symlink() and not pdb.is_symlink() and
            dll.read_bytes() == payload and folder.resolve().is_relative_to(source.resolve()) and
            not any(part.is_symlink() for part in (folder, *folder.parents) if part.is_relative_to(source)),
            'core_original_private_symbols')
    return pdb
