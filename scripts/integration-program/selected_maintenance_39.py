"""The two exact registered Studio/Extensions 3.9 source interfaces."""
from __future__ import annotations

import json
from pathlib import Path

import product_release_metadata as metadata
from prove_consolidated_packages import require

ROOT = Path(__file__).resolve().parents[2]
CONTRACT = Path(__file__).with_name('selected_maintenance_39_contract.json')


def contract(product: str) -> dict:
    value = json.loads(CONTRACT.read_text())['sources'].get(product)
    require(value is not None, 'maintenance39_product')
    return value


def validate_plan(plan: dict) -> dict:
    value = contract(plan['product'])
    source = plan['source']
    require(plan['line'] == source['line'] == '3.9' and source['product'] == plan['product'] and
            source['kind'] == 'admitted-maintenance-descendant' and
            (source['commit'], source['tree']) == (value['commit'], value['tree']), 'maintenance39_source')
    recipe = plan['inventory']['release_recipe']
    require((recipe['solution'], recipe['workflow']) == (value['solution'], value['workflow']), 'maintenance39_recipe')
    tests = {row['path']: row['target_frameworks'] for row in plan['inventory']['projects'] if row['is_test_project']}
    require(tests == value['test_projects'], 'maintenance39_test_inventory')
    selected = {row['id'] for row in plan['inventory']['selected']}
    for exclusion in value['excluded_canonical']:
        require(exclusion in plan['inventory']['excluded'] and exclusion['id'] not in selected,
                'maintenance39_canonical_exclusion')
    require((plan['npm'] is not None) == (plan['product'] == 'studio'), 'maintenance39_npm_scope')
    return value


def verify_git(root: Path, product: str, commit: str) -> dict:
    value = contract(product)
    require(commit == value['commit'] and metadata.git(root, 'rev-parse', commit + '^{tree}') == value['tree'],
            'maintenance39_source')
    for path, blob in value['files'].items():
        require(metadata.git(root, 'rev-parse', commit + ':' + path) == blob, 'maintenance39_blob')
    fixture = ROOT / value['fixture']['path']
    require(fixture.is_file() and not any(path.is_symlink() for path in (fixture, *fixture.parents)) and
            metadata.sha256(fixture.read_bytes()) == value['fixture']['sha256'], 'maintenance39_runtime_fixture')
    return value


def verify_source(root: Path, plan: dict) -> None:
    validate_plan(plan)
    verify_git(root, plan['product'], plan['source']['commit'])
