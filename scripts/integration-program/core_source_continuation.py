"""Closed Core maintenance candidates, independent of release-branch observations.

The tracked catalog is controller policy, never a receipt-supplied path. Candidate
rows become trusted only through independent controller review and root admission.
Pure validation accepts preloaded trusted contracts and performs no IO.
"""
from __future__ import annotations

from copy import deepcopy
import hashlib
import json
from pathlib import Path
import re

from prove_consolidated_packages import require

KIND = 'reviewed-core-maintenance-continuation'
CONTRACT = Path(__file__).with_name('core_source_continuation_contract.json')
SOURCE_KEYS = {'product', 'line', 'kind', 'commit', 'tree', 'parents', 'original_commit', 'original_tree',
               'observation', 'continuation_contract_sha256'}
REFS = {'3.8': 'refs/heads/release/3.8.4', '3.9': 'refs/heads/release/3.9.0'}


def load_contract() -> dict:
    return json.loads(CONTRACT.read_bytes())


def contract_hash(contract: dict) -> str:
    return hashlib.sha256(json.dumps(contract, sort_keys=True, separators=(',', ':')).encode()).hexdigest()


def candidate(line: str, contract: dict) -> dict:
    require(contract.get('schema') == 1 and contract.get('scope') == 'fixed-reviewed-core-maintenance-continuations' and
            set(contract.get('sources', {})) == set(REFS) and line in REFS, 'core_continuation_catalog')
    return contract['sources'][line]


def validate_tag_history(line: str, history: object) -> None:
    """Validate the closed original tag snapshot before any selected work."""
    require(type(history) is list and bool(history), 'selected_plan_core_tag_duplicate')
    refs = set()
    for tag in history:
        require(type(tag) is dict and set(tag) == {'ref', 'node_id', 'url', 'object'} and
                all(type(tag[key]) is str and not any(ord(c) < 32 for c in tag[key])
                    for key in ('ref', 'node_id', 'url')), 'selected_plan_core_tag')
        obj = tag['object']
        require(type(obj) is dict and set(obj) == {'sha', 'type', 'url'} and
                all(type(value) is str for value in obj.values()), 'selected_plan_core_tag')
        ref = tag['ref']
        require(not tag['node_id'].startswith(('/', '\\')) and ':' not in tag['node_id'] and
                not any(c in ref for c in ':?#'), 'selected_plan_core_tag')
        require(ref.startswith('refs/tags/' + line + '.') and obj['type'] in ('tag', 'commit') and
                re.fullmatch('[a-f0-9]{40}', obj['sha']) is not None and
                tag['url'] == 'https://api.github.com/repos/elsa-workflows/elsa-core/git/' + ref and
                obj['url'] == 'https://api.github.com/repos/elsa-workflows/elsa-core/git/' +
                ('tags/' if obj['type'] == 'tag' else 'commits/') + obj['sha'], 'selected_plan_core_tag')
        require(ref not in refs, 'selected_plan_core_tag_duplicate')
        refs.add(ref)


def bind(line: str, observation: dict, contract: dict) -> dict:
    row = candidate(line, contract)
    require(type(observation) is dict and
            (observation.get('ref'), observation.get('commit'), observation.get('tree')) ==
            (REFS[line], row['original_commit'], row['original_tree']), 'core_continuation_baseline_observation')
    return {'product': 'core', 'line': line, 'kind': KIND,
            **{key: deepcopy(row[key]) for key in ('commit', 'tree', 'parents', 'original_commit', 'original_tree')},
            'observation': deepcopy(observation), 'continuation_contract_sha256': contract_hash(contract)}


def policy(source: dict, original: dict, contract: dict) -> dict:
    """Derive the original recipe/test policy plus precisely the reviewed delta."""
    require(type(source) is dict and set(source) == SOURCE_KEYS and source['product'] == 'core' and
            source['kind'] == KIND, 'core_continuation_source_shape')
    row = candidate(source['line'], contract)
    require(source == bind(source['line'], source['observation'], contract) and
            (original['commit'], original['tree']) == (row['original_commit'], row['original_tree']),
            'core_continuation_source_identity')
    result = deepcopy(original)
    result.update(commit=row['commit'], tree=row['tree'])
    for change in row['delta']:
        path = change['path']
        require(change['before']['mode'] == change['after']['mode'] == '100644' and
                change['before']['type'] == change['after']['type'] == 'blob', 'core_continuation_delta_mode')
        if path in result['files']:
            require(result['files'][path] == change['before']['blob'], 'core_continuation_baseline_blob')
        result['files'][path] = change['after']['blob']
    return result


def verify_source(root: Path, source: dict, original: dict, contract: dict) -> None:
    """Verify the complete ancestry and whole-tree delta before native work."""
    import prepare_maintenance_build as maintenance
    policy(source, original, contract)
    row = candidate(source['line'], contract)

    def git(*args):
        return maintenance.git(root, *args)

    require(git('rev-parse', row['original_commit'] + '^{tree}') == row['original_tree'],
            'core_continuation_original_tree')
    previous = row['original_commit']
    actual = git('rev-list', '--reverse', previous + '..' + row['commit']).splitlines()
    require(actual == [item['commit'] for item in row['chain']] and bool(actual), 'core_continuation_chain')
    for item in row['chain']:
        require(item['parents'] == [previous] and git('show', '-s', '--format=%P', item['commit']).split() == item['parents'] and
                git('rev-parse', item['commit'] + '^{tree}') == item['tree'], 'core_continuation_parent_tree')
        previous = item['commit']
    require((row['chain'][-1]['commit'], row['chain'][-1]['tree'], row['chain'][-1]['parents']) ==
            (row['commit'], row['tree'], row['parents']), 'core_continuation_head')
    before, after = (maintenance.tree_entries(root, commit) for commit in (row['original_commit'], row['commit']))
    changed = {path for path in before.keys() | after.keys() if before.get(path) != after.get(path)}
    require(changed == {item['path'] for item in row['delta']} and len(changed) == len(row['delta']),
            'core_continuation_delta_inventory')
    for item in row['delta']:
        for key, commit, entries in (('before', row['original_commit'], before), ('after', row['commit'], after)):
            pin = item[key]
            require(entries.get(item['path']) == (pin['mode'], pin['type'], pin['blob']), 'core_continuation_delta_blob')
            data = maintenance.git_bytes(root, commit, item['path'])
            require(len(data) == pin['bytes'] and hashlib.sha256(data).hexdigest() == pin['sha256'],
                    'core_continuation_delta_bytes')


def validate_project_metadata(source: dict, selected: list[dict], contract: dict) -> None:
    """Changed project metadata must describe the actual candidate source bytes."""
    if source.get('kind') != KIND:
        return
    hashes = {item['path']: item['after']['sha256'] for item in candidate(source['line'], contract)['delta']
              if item['path'].endswith('.csproj')}
    seen = set()
    for item in selected:
        if item['project'] in hashes:
            require(item['metadata']['original_content_project_sha256'] == hashes[item['project']],
                    'core_continuation_project_metadata')
            seen.add(item['project'])
    require(seen == set(hashes), 'core_continuation_project_inventory')
