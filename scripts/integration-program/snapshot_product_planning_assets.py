#!/usr/bin/env python3
"""Preserve original selected planning assets privately, without rewriting paths."""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

import prove_product_release_artifacts as artifacts
from product_release_metadata import sha256
from prove_consolidated_packages import require


def safe_path(path: Path) -> Path:
    """Return an absolute snapshot path after rejecting traversal and symlink components."""
    require('..' not in path.parts, 'snapshot_path')
    absolute = Path(os.path.abspath(path))
    require(not any(part.is_symlink() for part in (absolute, *absolute.parents)), 'snapshot_path')
    return absolute


def snapshot(plan_path: Path, plan_hash: str, output: Path) -> dict:
    """Preserve exact original restore assets privately after validating all plan joins."""
    plan_path, output = safe_path(plan_path), safe_path(output)
    require(plan_path.is_file(), 'snapshot_plan_path')
    plan = artifacts.admit(plan_path.read_bytes(), plan_hash)
    root = plan_path.parent
    require(not output.exists() and not output.is_relative_to(root) and not root.is_relative_to(output) and
            not output.is_relative_to(artifacts.ROOT), 'snapshot_output_location')
    selected = plan['inventory']['selected']
    require(selected and len({row['project'] for row in selected}) == len(selected) and
            len({row['id'].casefold() for row in selected}) == len(selected), 'snapshot_selected_partition')
    by_hash = {}
    # Reject unsafe components before globbing; pathlib does not recurse symlink directories.
    for path in root.rglob('*'):
        require(not path.is_symlink(), 'snapshot_path')
        if path.name == 'project.assets.json':
            path = safe_path(path)
            require(path.is_file(), 'snapshot_asset_path')
            raw = path.read_bytes()
            by_hash.setdefault(sha256(raw), []).append((path, raw))
    rows, payloads = [], {}
    for policy in selected:
        project = Path(policy['project'])
        require(not project.is_absolute() and '..' not in project.parts and '\\' not in str(project),
                'snapshot_project_path')
        expected = policy['metadata']['restore_assets_sha256']
        matches = by_hash.get(expected, [])
        require(len(matches) == 1, 'snapshot_asset_missing_or_ambiguous')
        path, raw = matches[0]
        assets = artifacts.planner.read_json(raw)
        original_project = safe_path(Path(assets['project']['restore']['projectPath']))
        expected_project = safe_path(root / 'source.private' / project)
        require(Path(assets['project']['restore']['projectPath']).is_absolute() and
                original_project == expected_project and expected_project.is_file() and
                path.is_relative_to(root / 'source.private'), 'snapshot_source_project')
        frameworks = policy['frameworks']
        require(frameworks and len(set(frameworks)) == len(frameworks) and
                set(frameworks) == set(assets['project']['frameworks']) and
                set(frameworks) <= set(assets['targets']), 'snapshot_frameworks')
        versions = {framework: sorted([{'id': name.rsplit('/', 1)[0], 'version': name.rsplit('/', 1)[1],
                    'type': item['type']} for name, item in target.items()],
                    key=lambda row: (row['id'].casefold(), row['version']))
                    for framework, target in assets['targets'].items()}
        rows.append({'id': policy['id'], 'project': policy['project'], 'frameworks': frameworks,
                     'sha256': expected, 'bytes': len(raw), 'file': expected + '.json', 'resolved_targets': versions})
        payloads[expected] = raw
    receipt = {'schema': 1, 'mode': 'private-original-planning-assets-snapshot', 'plan_sha256': plan_hash,
               'planner_controller': plan['controller'], 'source': plan['source'], 'product': plan['product'],
               'line': plan['line'], 'version': plan['requested_version'], 'selected': rows,
               'product_build_executed': False, 'publication': False,
               'privacy': 'Raw project.assets files contain local paths and stay private; this is consumer input, not consumer execution proof.'}
    # Complete all identity joins before the first output write. Raw bytes remain unchanged.
    output.mkdir(parents=True)
    for digest, raw in payloads.items():
        with (output / (digest + '.json')).open('xb') as writer:
            writer.write(raw)
        require(sha256((output / (digest + '.json')).read_bytes()) == digest, 'snapshot_written_hash')
    (output / 'receipt.private.json').write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n')
    return receipt


def main() -> int:
    """Run private snapshot creation and report counts or a generic failure status."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    try:
        receipt = snapshot(args.plan, args.plan_sha256, args.output)
        print(json.dumps({'selected_projects_snapshotted': len(receipt['selected']),
                          'unique_original_asset_files': len({row['sha256'] for row in receipt['selected']}),
                          'plan_sha256': args.plan_sha256, 'product_build_executed': False}))
        return 0
    except (ValueError, KeyError, TypeError, OSError):
        print('Private planning snapshot failed; no raw assets or paths are public evidence.')
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
