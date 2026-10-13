"""Exact owner-approved MySQL .NET8/9 boundary for historical Extensions.

The ordinary candidate verifier owns whole-tree and ancestry verification. This
module admits only the registered protected project/Build edits and binds their
support policy to selected plan metadata; it never excludes packages or consumers.
Owned by #8693/#8217; remove when replacement maintenance admission is accepted
and historical proof readback no longer consumes these source bindings.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from prove_consolidated_packages import require

CONTRACT = Path(__file__).with_name('extensions_mysql_continuation_contract.json')


def load_contract() -> dict:
    """Read the controller-owned exact continuation and support policy catalog."""
    return json.loads(CONTRACT.read_bytes())


CONTINUATIONS = load_contract()['sources']


def policy_hash(contract: dict) -> str:
    """Bind the public support decision independently from source/register hashes."""
    return hashlib.sha256(json.dumps(contract['support_policy'], sort_keys=True, separators=(',', ':')).encode()).hexdigest()


def verify_delta(row: dict, parent: dict, change: dict) -> None:
    """Admit only the catalog's exact project delta, source tree and sole parent."""
    chain = CONTINUATIONS.get(row['line'], {}).get('chain', [])
    expected = next((item for item in chain if item['commit'] == row['commit']), {})
    require(row['product'] == 'extensions' and row.get('source_kind') == 'core' and
            (row['commit'], row['tree'], parent['commit']) ==
            (expected.get('commit'), expected.get('tree'), expected.get('parent')) and
            row['parents'] == [parent['commit']] and row['delta'] == expected.get('delta') and
            change in expected['delta'], 'Core candidate protected control mismatch')


def binding(source: dict, contract: dict) -> dict:
    """Return the selected source's exact public support-policy binding."""
    if source['product'] != 'extensions':
        return {}
    expected = contract['sources'].get(source['line'], {})
    require((source['commit'], source['tree']) == (expected.get('commit'), expected.get('tree')),
            'source_mysql_support_identity')
    return {'support_policy_sha256': policy_hash(contract)}


def validate_inventory(source: dict, inventory: dict, contract: dict) -> None:
    """Reject MySQL net10 fallback, dropped packages, or changed .NET8/9 metadata."""
    if source['product'] != 'extensions':
        return
    require(source.get('support_policy_sha256') == binding(source, contract)['support_policy_sha256'],
            'source_mysql_support_policy')
    policy = contract['support_policy']
    projects = {row['path']: row for row in inventory['projects']}
    selected = {row['project']: row for row in inventory['selected']}
    for item in policy['projects']:
        path = item['project']
        require(path in projects and path in selected and selected[path]['id'] == item['id'] and
                projects[path]['package_id'] == item['id'] and
                projects[path]['target_frameworks'] == selected[path]['frameworks'] == policy['supported_frameworks'] and
                set(projects[path]['references_by_framework']) == set(policy['supported_frameworks']),
                'inventory_mysql_support_frameworks')
        metadata = selected[path]['metadata']
        require(set(metadata['original_output_policy']) == set(policy['supported_frameworks']) and
                sorted(row['framework'] for row in metadata['dependency_groups']) == policy['supported_frameworks'],
                'inventory_mysql_support_metadata')
    test = projects.get(policy['quartz_test_project'])
    require(test is not None and test['target_frameworks'] == policy['quartz_test_frameworks'],
            'inventory_mysql_support_tests')
    paths = {item['project'] for item in policy['projects']}
    ids = {item['id'].casefold() for item in policy['projects']} | {
        'elsa.persistence.efcore.mysql', 'pomelo.entityframeworkcore.mysql',
        'appany.quartz.entityframeworkcore.migrations.mysql'}
    for project in inventory['projects']:
        references = project.get('references_by_framework', {}).get('net10.0', {})
        require(not any(row['target_project'] in paths for row in references.get('project_references', [])) and
                not any(row['id'].casefold() in ids for row in references.get('package_references', [])),
                'inventory_mysql_net10_reference')
    for package in inventory['selected']:
        for group in package.get('metadata', {}).get('dependency_groups', []):
            require(group['framework'] != 'net10.0' or
                    not any(row['id'].casefold() in ids for row in group['dependencies']),
                    'inventory_mysql_net10_dependency')
