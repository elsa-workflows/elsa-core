#!/usr/bin/env python3
"""Admit one reviewed maintenance plan and run its private Studio vertical control.

No publisher, allocation or historical workflow is invoked. Other products are
admitted structurally but deliberately have no producer adapter yet.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import re
import shutil
import zipfile

import plan_product_release as planner
import product_release_metadata as metadata
import prepare_maintenance_build as maintenance
from prove_consolidated_packages import archive_names, dependency_groups, framework_reference_groups, metadata as nuspec, require
import prove_historical_studio_npm_pair as historical

ROOT = Path(__file__).resolve().parents[2]
PLANNER_INPUTS = {
    'scripts/integration-program/plan_product_release.py',
    'scripts/integration-program/product_release_metadata.py',
    'scripts/integration-program/ProductReleaseSemantics/Program.cs',
    'scripts/integration-program/ProductReleaseSemantics/ProductReleaseSemantics.csproj',
}


def fresh_public(observation: dict, checked_at: str) -> None:
    require(observation.get('status') in ('observed', 'missing'), 'plan_observation_unavailable')
    observed, checked = (datetime.fromisoformat(value) for value in (observation['observed_at'], checked_at))
    require(observed.tzinfo is not None and checked.tzinfo is not None and
            0 <= (checked - observed).total_seconds() <= planner.MAX_AGE_SECONDS, 'plan_observation_stale')
    if observation['status'] == 'observed':
        require(re.fullmatch('[a-f0-9]{64}', observation['sha256']) is not None and
                type(observation['bytes']) is int and observation['bytes'] > 0, 'plan_observation_identity')


def admit(data: bytes, expected_sha256: str, *, checked_at: str | None = None) -> dict:
    """Cheap, side-effect-free admission; the reviewed hash is the trust boundary."""
    require(re.fullmatch('[a-f0-9]{64}', expected_sha256) is not None and
            metadata.sha256(data) == expected_sha256, 'plan_hash')
    plan = planner.read_json(data)
    try:
        require(plan['schema'] == 1 and plan['mode'] == 'read-only-product-release-plan' and
                plan['eligible'] is True and plan['reasons'] == [] and
                all(plan[key] is False for key in ('published', 'version_allocated', 'tag_created')), 'plan_ineligible')
        binding = plan['source']
        require(plan['product'] == binding['product'] and plan['line'] == binding['line'] and
                plan['product'] in ('core', 'studio', 'extensions') and plan['line'] in ('3.8', '3.9'), 'plan_selection')
        require(re.fullmatch(re.escape(plan['line']) + r'\.[0-9]+(?:-[0-9A-Za-z.-]+)?', plan['requested_version']) is not None,
                'plan_version')
        for identity in (binding, plan['controller']):
            require(all(re.fullmatch('[a-f0-9]{40}', identity[key]) is not None for key in ('commit', 'tree')),
                    'plan_source_identity')
        require(set(plan['controller']['input_sha256']) == PLANNER_INPUTS and all(
            re.fullmatch('[a-f0-9]{64}', value) is not None for value in plan['controller']['input_sha256'].values()),
            'plan_controller_inputs')
        planner.validate_inventory(plan['inventory'], binding, plan['requested_version'])
        for row in plan['inventory']['projects']:
            path = Path(row['path'])
            require(not path.is_absolute() and '..' not in path.parts and '\\' not in row['path'], 'plan_project_path')
        selected = plan['inventory']['selected']
        expected = [name for row in selected for name in ([f"{row['id']}.{plan['requested_version']}.nupkg"] +
                    ([f"{row['id']}.{plan['requested_version']}.snupkg"] if row['symbols'] else []))]
        npm = plan['npm']
        require((npm is not None) == (plan['product'] == 'studio'), 'plan_npm_identity')
        if npm:
            require(npm['atomic'] is True and npm['line'] == plan['line'] and
                    npm['source_commit'] == binding['commit'] and npm['source_tree'] == binding['tree'] and
                    [row['id'] for row in npm['packages']] == list(planner.NPM_IDS) and
                    all(row['version'] == plan['requested_version'] for row in npm['packages']), 'plan_npm_identity')
            expected += [row['expected_tarball'] for row in npm['packages']]
        require(plan['expected_artifacts'] == expected and len(set(expected)) == len(expected), 'plan_artifact_partition')
        history_ids = [row['id'].casefold() for row in selected] + ([value.casefold() for value in planner.NPM_IDS] if npm else [])
        require(sorted(row['id'].casefold() for row in plan['histories']) == sorted(history_ids), 'plan_history_inventory')
        checked_at = checked_at or datetime.now(timezone.utc).isoformat()
        fresh_public({'status': 'observed', 'observed_at': plan['observed_at'], 'sha256': expected_sha256,
                      'bytes': len(data)}, checked_at)
        observations = []
        for row in plan['histories'] + plan['prerequisites']:
            require(row['eligible'] is True and row['reason'] is None, 'plan_prerequisite_ineligible')
            checks = row.get('feeds', [row])
            require(checks, 'plan_feed_inventory')
            for check in checks:
                require(check.get('eligible') is True or check.get('reason') == 'prerequisite_missing', 'plan_feed_ineligible')
                observations.append(check['observation'])
        for service in plan['feed_service_observations'].values():
            require(service['eligible'] is True, 'plan_feed_ineligible')
            observations.append(service['observation'])
        require(observations, 'plan_observation_inventory')
        for observation in observations:
            fresh_public(observation, checked_at)
        selected_ids = {row['id'].casefold() for row in selected}
        require(not any(row['id'].casefold() in selected_ids for row in plan['prerequisites']) and
                sorted(plan['prerequisites_excluded_from_publication'], key=str.casefold) ==
                sorted({row['id'] for row in plan['prerequisites']}, key=str.casefold), 'plan_prerequisite_scope')
        require(all(row['eligible'] is True for row in plan['selected_dependency_intent']), 'plan_internal_dependencies')
        require(all(row['metadata']['status'] == 'observed' for row in selected), 'plan_metadata_unavailable')
        return plan
    except (KeyError, TypeError, ValueError) as error:
        if isinstance(error, ValueError) and re.fullmatch('(?:plan|inventory|ownership|source)_[a-z_]+', str(error)):
            raise
        raise ValueError('plan_malformed') from None


def verify_controller(root: Path, plan: dict) -> dict:
    require(not metadata.git(root, 'status', '--porcelain'), 'artifact_controller_dirty')
    controller = {'commit': metadata.git(root, 'rev-parse', 'HEAD'), 'tree': metadata.git(root, 'rev-parse', 'HEAD^{tree}')}
    planner_identity = plan['controller']
    require(metadata.git(root, 'rev-parse', planner_identity['commit'] + '^{tree}') == planner_identity['tree'],
            'planner_controller_identity')
    for path, expected in planner_identity['input_sha256'].items():
        require(metadata.sha256(maintenance.git_bytes(root, planner_identity['commit'], path)) == expected and
                metadata.sha256((root / path).read_bytes()) == expected, 'planner_controller_input_identity')
    binding = metadata.bind_source(root, plan['product'], plan['line'], plan['source']['commit'],
                                   plan['source'].get('observation'))
    require(binding == plan['source'], 'artifact_source_identity')
    return controller


def refresh_remote(plan: dict, source: Path, semantics: planner.Semantics) -> None:
    """GET-only preflight using the reviewed exact edges and original mapping."""
    observations = planner.Observations()
    ids = [row['id'] for row in plan['inventory']['selected']]
    edges = [{key: row[key] for key in ('consumer', 'project', 'framework', 'id', 'range', 'version')}
             for row in plan['prerequisites']]
    feeds = planner.FeedMetadata(source / 'NuGet.Config', ids + [row['id'] for row in edges], semantics, observations)
    require(feeds.policy == plan['consumer_feed_policy'], 'artifact_feed_policy_changed')
    feeds.prefetch(ids, edges)
    checked = planner.now()
    histories = [feeds.history(identifier, plan['requested_version'], plan['line'], semantics, checked) for identifier in ids]
    if plan['npm']:
        histories += [planner.check_history(identifier, plan['requested_version'], plan['line'],
            observations.get(planner.history_url(identifier, True)), semantics, checked, npm=True) for identifier in planner.NPM_IDS]
    prerequisites = [feeds.prerequisite(edge, semantics, checked) for edge in edges]
    require(all(row['eligible'] is True for row in histories + prerequisites), 'artifact_fresh_prerequisite_failed')
    # New history observations may grow; requested version must still be eligible.
    for old, current in zip(plan['prerequisites'], prerequisites):
        before = {row['observation']['sha256'] for row in old['feeds'] if row['eligible']}
        after = {row['observation']['sha256'] for row in current['feeds'] if row['eligible']}
        require(before == after, 'artifact_prerequisite_metadata_changed')


def retain_selected(plan: dict, artifacts: Path, destination: Path) -> dict:
    """Only selected bytes leave the private original-recipe directory."""
    destination.mkdir()
    selected = {row['id'].casefold(): row for row in plan['inventory']['selected']}
    expected = {name for name in plan['expected_artifacts'] if name.endswith(('.nupkg', '.snupkg'))}
    found, private_outputs, receipts = set(), [], []
    for path in sorted(artifacts.iterdir()):
        require(path.is_file() and not path.is_symlink() and path.suffix in ('.nupkg', '.snupkg'), 'artifact_file')
        with zipfile.ZipFile(path) as archive:
            names = archive_names(archive)
            package = nuspec(archive)
            identifier, version = package.findtext('id'), package.findtext('version')
            require(version == plan['requested_version'], 'artifact_version')
            record = {'file': path.name, 'id': identifier, 'version': version, 'sha256': metadata.sha256(path.read_bytes()),
                      'size': path.stat().st_size, 'inventory': [{'path': name, 'size': len(archive.read(name)),
                      'sha256': metadata.sha256(archive.read(name))} for name in sorted(names)]}
            if identifier.casefold() not in selected:
                excluded = {row['id'].casefold() for row in plan['inventory']['excluded'] if row['id']}
                require(identifier.casefold() in excluded, 'artifact_unknown_recipe_output')
                private_outputs.append(record)
                continue
            require(path.name in expected and path.name not in found, 'artifact_bijection')
            policy = selected[identifier.casefold()]
            require(dependency_groups(package) == policy['metadata']['dependency_groups'], 'artifact_dependency_intent')
            require(framework_reference_groups(package) == policy['metadata'].get('framework_reference_groups', []),
                    'artifact_framework_reference_intent')
            found.add(path.name)
            shutil.copyfile(path, destination / path.name)
            receipts.append(record)
    require(found == expected, 'artifact_bijection')
    return {'selected': receipts, 'private_recipe_only_outputs': private_outputs}


def execute(root: Path, data: bytes, digest: str, output: Path, run: str, attempt: str, *, setup_only: bool = False) -> dict:
    plan = admit(data, digest)
    require(re.fullmatch('[1-9][0-9]*', run) is not None and re.fullmatch('[1-9][0-9]*', attempt) is not None, 'artifact_run_identity')
    require(not output.exists() and not output.resolve().is_relative_to(root.resolve()) and
            not any(part.is_symlink() for part in (output, *output.parents)), 'artifact_output_location')
    controller = verify_controller(root, plan)
    # Explicit supported interface until its first source-faithful control settles.
    require(plan['product'] == 'studio' and plan['line'] == '3.8', 'artifact_control_not_implemented')
    output.mkdir(parents=True)
    private, retained = output / 'private', output / 'retained'
    private.mkdir()
    retained.mkdir()
    receipt = {'schema': 1, 'mode': 'selected-product-artifact-control', 'plan_sha256': digest,
               'planner_controller': plan['controller'], 'artifact_controller': controller, 'source': plan['source'],
               'product': plan['product'], 'line': plan['line'], 'version': plan['requested_version'],
               'run_id': run, 'run_attempt': attempt, 'success': False, 'published': False,
               'version_allocated': False, 'tag_created': False, 'stage': 'source-setup'}
    try:
        source = private / 'admitted-source'
        metadata.checkout_source(root, plan['source'], source)
        inventory = plan['inventory']
        for path, expected in ((inventory['release_recipe']['solution'], inventory['release_recipe']['sha256']),
                               (inventory['release_recipe']['workflow'], inventory['release_recipe']['workflow_sha256'])):
            require(metadata.sha256((source / path).read_bytes()) == expected, 'artifact_recipe_identity')
        require(planner.npm_intent(source, plan['source'], plan['requested_version']) == plan['npm'], 'artifact_npm_intent')
        receipt['stage'] = 'fresh-prerequisites'
        helper = private / 'semantics'; helper.mkdir()
        refresh_remote(plan, source, planner.build_helper(helper))
        if setup_only:
            receipt.update(stage='setup-complete', setup_complete=True, artifact_proof=False)
            return receipt
        receipt['stage'] = 'original-product-recipe'
        rows = [row for row in maintenance.registered_core_candidates(maintenance.load_register())
                if row['commit'] == plan['source']['commit'] and row['kind'] == 'maintenance']
        require(len(rows) == 1, 'artifact_registered_recipe')
        producer = maintenance.prepare(root, rows[0], plan['requested_version'], private / 'producer')
        require(producer['success'] is True, 'artifact_producer_failed')
        receipt['product_tests'] = producer['tests']
        receipt['packages'] = retain_selected(plan, private / 'producer/artifacts', retained / 'nuget')
        receipt['stage'] = 'historical-studio-npm'
        receipt['npm'] = historical.prove(private / 'producer/source', private / 'npm', retained / 'npm', plan, run, attempt)
        receipt.update(success=True, stage='complete', artifact_proof=True)
        return receipt
    except Exception:
        receipt['failure_code'] = receipt['stage'] + '-failed'
        raise
    finally:
        (retained / 'receipt.json').write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n')


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--run-id', required=True)
    parser.add_argument('--run-attempt', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--setup-only', action='store_true')
    args = parser.parse_args()
    try:
        execute(ROOT, args.plan.read_bytes(), args.plan_sha256, args.output, args.run_id, args.run_attempt, setup_only=args.setup_only)
        return 0
    except Exception:
        print('Selected artifact control failed; retained receipt records the stage, raw diagnostics remain private.')
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
