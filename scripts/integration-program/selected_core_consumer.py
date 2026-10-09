"""Bounded original Core consumer source and native packaged-DLL policies."""
from __future__ import annotations

import json
from pathlib import Path
import re

import product_release_metadata as metadata
import selected_core_producer as original
from prove_consolidated_packages import require

ROOT = Path(__file__).resolve().parents[2]
CONTRACT = Path(__file__).with_name('selected_core_consumer_contract.json')
FIXTURE = ROOT / 'scripts/integration-program/selected-core-consumer/Program.cs'
FRAMEWORKS = ('net8.0', 'net9.0', 'net10.0')
REQUIRED_ASSEMBLIES = ('Elsa', 'Elsa.Workflows.Core', 'Elsa.Workflows.Runtime')


validate_plan = original.validate_plan


def source_contract(plan: dict) -> dict:
    original.policy(plan['source'])
    return json.loads(CONTRACT.read_text())['sources'][plan['line']]


def verify_source(root: Path, plan: dict) -> None:
    original.verify_source(root, plan['source'])
    for path, expected in source_contract(plan).items():
        require(metadata.sha256(original.maintenance.git_bytes(root, plan['source']['commit'], path)) == expected,
                'core_consumer_source_contract')
    fixtures = json.loads(CONTRACT.read_text())['fixtures']
    for path, expected in fixtures.items():
        actual = FIXTURE if path.endswith('/Program.cs') else ROOT / path
        require(actual.is_file() and not any(part.is_symlink() for part in (actual, *actual.parents)) and
                metadata.sha256(actual.read_bytes()) == expected, 'core_consumer_fixture_hash')


def runtime_contract() -> dict:
    return {'package': 'elsa', 'fixture': FIXTURE,
            'checks': {'variableNearestScope': True, 'outputLines': ['Sequence Value'],
                       'status': 'Finished', 'subStatus': 'Finished', 'incidents': 0},
            'required_packages': REQUIRED_ASSEMBLIES, 'assembly_release_version': None,
            'description': 'Core original nested variable-scope workflow executes with exact output and Finished/Finished',
            'limitation': 'Small in-memory engine run does not certify persistence, resume, distributed execution or all Core behavior.'}


def validate_runtime(selected: dict) -> None:
    require({name.casefold() for name in REQUIRED_ASSEMBLIES} <= set(selected), 'core_consumer_runtime_packages')
    frameworks = selected['elsa']['policy']['frameworks']
    require(len(frameworks) == len(FRAMEWORKS) and set(frameworks) == set(FRAMEWORKS),
            'core_consumer_runtime_frameworks')


def bind_assemblies(plan: dict, receipt: dict, selected: dict) -> None:
    """Join successful native producer evidence to the admitted archive inventory.

    The producer checked these native values against original per-TFM SDK
    GetAssemblyAttributes output. Never derive Core DLL versions from NuGet version.
    """
    validate_plan(plan)
    rows = receipt.get('package_verification', [])
    require(isinstance(rows, list) and len(rows) == len(selected) and
            {row['id'].casefold() for row in rows} == set(selected), 'core_consumer_native_inventory')
    for native in rows:
        item = selected[native['id'].casefold()]
        require(native['id'] == item['id'] and native['version'] == plan['requested_version'] and
                sorted(native['files'], key=lambda row: row['name']) ==
                sorted(item['artifact_files'], key=lambda row: row['name']), 'core_consumer_native_archive')
        frameworks = item['policy']['frameworks']
        emitted = {framework for framework in frameworks if
                   item['policy']['metadata']['original_output_policy'][framework]['IncludeBuildOutput'].lower() != 'false'}
        require(type(native['include_build_output']) is bool and native['include_build_output'] == bool(emitted) and
                len(native['frameworks']) == len(emitted) and set(native['frameworks']) == emitted,
                'core_consumer_native_frameworks')
        assembly_name = native['assembly_name']
        require(isinstance(assembly_name, str) and assembly_name and '/' not in assembly_name and
                '\\' not in assembly_name, 'core_consumer_native_assembly')
        expected = {f'lib/{framework}/{assembly_name}.dll' for framework in emitted}
        symbols = native['symbols']
        require(len(symbols) == len(expected) and {row['assembly'] for row in symbols} == expected,
                'core_consumer_native_assembly_inventory')
        inventory = {entry['path']: entry for entry in item['inventory']}
        satellites = native['satellites']
        satellite_paths = {entry['package_path'] for entry in satellites}
        require(len(satellites) == len(satellite_paths) and not expected & satellite_paths and
                {path for path in inventory if path.startswith('lib/') and path.endswith('.dll')} ==
                expected | satellite_paths and all(entry['package_path'] in inventory and
                entry['sha256'] == inventory[entry['package_path']]['sha256'] for entry in satellites),
                'core_consumer_native_dll_inventory')
        policies = {}
        for row in symbols:
            asset = row['assembly']
            version, information = row['assembly_version'], row['informational_version']
            require(asset in inventory and row['assembly_sha256'] == inventory[asset]['sha256'] and
                    isinstance(version, str) and re.fullmatch(r'\d+\.\d+\.\d+\.\d+', version) and
                    isinstance(information, str) and information.endswith('+' + plan['source']['commit']) and
                    len(information) > 41, 'core_consumer_native_assembly_identity')
            policies[asset] = {'sha256': row['assembly_sha256'], 'assembly_version': version,
                               'informational_version': information}
        item['assembly_policies'] = policies
