#!/usr/bin/env python3
"""Admit one reviewed maintenance plan and run its private maintenance vertical control.

No publisher, allocation or historical workflow is invoked. All six supported product/line interfaces have bounded source adapters; actual proof remains separate.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import shutil
import stat
import zipfile

import plan_product_release as planner
import product_release_metadata as metadata
import prepare_maintenance_build as maintenance
import selected_extensions_contract as extensions
import selected_core_producer as core
import selected_maintenance_39 as maintenance39
from prove_consolidated_packages import archive_names, dependency_groups, framework_reference_groups, metadata as nuspec, require, run
import prove_historical_studio_npm_pair as historical
from product_artifact_execution import selected_execution

ROOT = Path(__file__).resolve().parents[2]
PLANNER_INPUTS = metadata.PLANNER_INPUTS

# Public diagnostics are a fixed vocabulary, never a projection of raw errors.
PUBLIC_FAILURE_CODES = frozenset((
    'plan_hash plan_ineligible plan_selection plan_version plan_source_identity plan_controller_inputs '
    'plan_project_path plan_npm_identity plan_artifact_partition plan_history_inventory plan_prerequisite_ineligible '
    'plan_feed_inventory plan_feed_ineligible plan_observation_inventory plan_observation_unavailable '
    'plan_observation_stale plan_observation_identity plan_prerequisite_scope plan_internal_dependencies '
    'plan_metadata_unavailable plan_malformed artifact_output_location artifact_controller_dirty '
    'planner_controller_identity planner_controller_input_identity artifact_source_identity '
    'artifact_execution_kind artifact_execution_identity artifact_execution_time artifact_hosted_context '
    'artifact_hosted_controller artifact_hosted_workflow artifact_hosted_run artifact_hosted_time '
    'artifact_hosted_role_controller artifact_hosted_plan artifact_hosted_binding artifact_hosted_planner_controller '
    'artifact_hosted_planner_execution artifact_control_not_implemented maintenance39_product maintenance39_source '
    'maintenance39_recipe maintenance39_test_inventory maintenance39_canonical_exclusion maintenance39_npm_scope '
    'maintenance39_blob maintenance39_runtime_fixture artifact_recipe_identity artifact_npm_intent '
    'artifact_extensions_recipe_configuration artifact_node_version artifact_npm_version artifact_sdk_unavailable '
    'artifact_sdk_selection artifact_host_recipe_identity artifact_host_recipe_framework artifact_host_unsupported_framework '
    'artifact_feed_policy_changed artifact_fresh_prerequisite_failed artifact_prerequisite_metadata_changed '
    'artifact_registered_recipe artifact_producer_failed'
).split())


PUBLIC_FAILURE_STAGES = frozenset(('source-setup', 'tool-preflight', 'fresh-prerequisites',
    'original-product-recipe', 'historical-studio-npm'))
RECIPE_FAILURE_STAGES = frozenset(('source-verification', 'toolchain', 'build-command', 'inventory',
    'test-evidence', 'symbol-inspector', 'sdk-metadata', 'package-verification'))
RECIPE_FAILURE_REASONS = frozenset(maintenance.VERIFICATION_REASONS.values()) | {'unknown-check-failure'}
DIAGNOSTIC_LIMIT = 4 * 1024 ** 2


def read_diagnostic_receipt(path: Path) -> tuple[dict, str]:
    """Read bounded regular diagnostic data without following a final symlink."""
    require(not any(part.is_symlink() for part in (path, *path.parents)), 'diagnostic_invalid')
    descriptor = os.open(path, os.O_RDONLY | os.O_NONBLOCK | getattr(os, 'O_NOFOLLOW', 0))
    with os.fdopen(descriptor, 'rb') as stream:
        info = os.fstat(stream.fileno())
        require(stat.S_ISREG(info.st_mode) and info.st_size <= DIAGNOSTIC_LIMIT, 'diagnostic_invalid')
        data = stream.read(DIAGNOSTIC_LIMIT + 1)
    require(len(data) <= DIAGNOSTIC_LIMIT, 'diagnostic_invalid')
    value = planner.read_json(data)
    require(type(value) is dict, 'diagnostic_invalid')
    return value, metadata.sha256(data)


def validate_recipe_failure(value: dict, selected: list[dict] | None = None) -> dict:
    """Reconstruct only a fixed vocabulary; receipt fields are never printed wholesale."""
    require(type(value) is dict, 'diagnostic_invalid')
    if value == {'status': 'unavailable'}:
        return {'status': 'unavailable'}
    require(value.get('status') == 'observed' and type(value.get('stage')) is str and
            value['stage'] in RECIPE_FAILURE_STAGES and type(value.get('reason')) is str and
            value['reason'] in RECIPE_FAILURE_REASONS and type(value.get('receipt_sha256')) is str and
            re.fullmatch('[a-f0-9]{64}', value['receipt_sha256']), 'diagnostic_invalid')
    result = {key: value[key] for key in ('status', 'stage', 'reason', 'receipt_sha256')}
    if value['stage'] == 'build-command':
        step, process, diagnostics = value['command_step'], value['process'], value['diagnostics']
        require(type(step) is int and 1 <= step <= 256 and type(process) is dict and
                set(process) == {'status', 'exit_code'} and type(process['status']) is str and
                process['status'] in ('exited', 'timed-out', 'interrupted', 'start-failed'), 'diagnostic_invalid')
        code = process['exit_code']
        require((process['status'] == 'start-failed' and code is None) or
                (process['status'] != 'start-failed' and type(code) is int and -2 ** 31 <= code < 2 ** 31 and
                 (process['status'] != 'exited' or code != 0)), 'diagnostic_invalid')
        require(type(diagnostics) is dict and set(diagnostics) ==
                {'codes', 'nuke_failed_targets', 'unretained_code_occurrences'}, 'diagnostic_invalid')
        codes, targets, overflow = (diagnostics[key] for key in
            ('codes', 'nuke_failed_targets', 'unretained_code_occurrences'))
        require(type(codes) is list and len(codes) <= 32 and type(targets) is list and len(targets) <= 4 and
                all(type(target) is str and target in ('Restore', 'Compile', 'Test', 'Pack') for target in targets) and
                len(set(targets)) == len(targets) and type(overflow) is int and 0 <= overflow < 2 ** 31,
                'diagnostic_invalid')
        projected_codes = []
        for item in codes:
            require(type(item) is dict and set(item) == {'severity', 'code', 'count'} and
                    type(item['severity']) is str and item['severity'] in ('error', 'warning') and
                    type(item['code']) is str and re.fullmatch('(CS|NU|MSB|NETSDK)[0-9]{4}', item['code']) and
                    type(item['count']) is int and 1 <= item['count'] < 2 ** 31, 'diagnostic_invalid')
            projected_codes.append({key: item[key] for key in ('severity', 'code', 'count')})
        require(len({(item['severity'], item['code']) for item in projected_codes}) == len(projected_codes),
                'diagnostic_invalid')
        result.update(command_step=step, process={key: process[key] for key in ('status', 'exit_code')},
            diagnostics={'codes': projected_codes, 'nuke_failed_targets': list(targets),
                         'unretained_code_occurrences': overflow})
    if value['stage'] == 'package-verification' and value['reason'] == 'source-producer-unverified':
        detail = value['source_producer_failure']
        require(type(detail) is dict and set(detail) == {'package', 'framework', 'check'} and
                type(detail['check']) is str and detail['check'] in maintenance.SOURCE_PRODUCER_CHECKS and
                type(detail['package']) is str and planner.ID.fullmatch(detail['package']) is not None and
                type(detail['framework']) is str and detail['framework'] in ('net8.0', 'net9.0', 'net10.0') and
                type(selected) is list, 'diagnostic_invalid')
        packages = [row for row in selected if row['id'] == detail['package']]
        require(len(packages) == 1 and detail['framework'] in packages[0]['frameworks'], 'diagnostic_invalid')
        result['source_producer_failure'] = {'package': packages[0]['id'],
            'framework': next(framework for framework in packages[0]['frameworks'] if framework == detail['framework']),
            'check': detail['check']}
    require(set(value) == set(result), 'diagnostic_invalid')
    return result


def recipe_failure_diagnostics(output: Path, plan: dict, controller: dict, execution: dict) -> dict:
    """Project the current failed Core producer only; diagnostics cannot replace its exception."""
    try:
        value, digest = read_diagnostic_receipt(output / 'private/producer/receipt.json')
        context = execution.get('context', {})
        require(type(value.get('schema')) is int and value['schema'] == 1 and value.get('success') is False and
                value.get('published') is False and value.get('maintenance_refs_activated') is False and
                value.get('selection') == plan['source'] | {'source_repository': maintenance.CORE_REPOSITORY} and
                value.get('version') == plan['requested_version'] and
                value.get('controller_commit') == controller['commit'] and
                value.get('controller_tree') == controller['tree'] and
                value.get('controller_sha256') == metadata.sha256(Path(maintenance.__file__).read_bytes()) and
                all(key in value and value[key] == context.get(key) for key in ('run_id', 'run_attempt')),
                'diagnostic_invalid')
        stage, error = value['stage'], value['error']
        require(type(stage) is str and stage in RECIPE_FAILURE_STAGES and type(error) is dict and
                error.get('code') == stage + '-failed', 'diagnostic_invalid')
        result = {'status': 'observed', 'stage': stage, 'reason': error['reason'], 'receipt_sha256': digest}
        if stage == 'build-command':
            step, commands = value['focus']['step'], value['commands']
            require(type(step) is int and 1 <= step <= 256 and type(commands) is list and len(commands) == step and
                    value['focus'] == {'step': step, 'directory': '.'} and
                    all(type(command) is dict and command.get('success') is True for command in commands[:-1]) and
                    type(commands[-1]) is dict and commands[-1].get('success') is False, 'diagnostic_invalid')
            result.update(command_step=step, process=commands[-1]['process'], diagnostics=commands[-1]['diagnostics'])
        if stage == 'package-verification' and error['reason'] == 'source-producer-unverified':
            result['source_producer_failure'] = value['focus'] | {'check': error['source_producer_check']}
        return validate_recipe_failure(result, plan['inventory']['selected'])
    except Exception:
        return {'status': 'unavailable'}


def public_recipe_failure(output: Path, output_existed: bool, stage: str, *,
                          plan_bytes: bytes | None = None, plan_sha256: str | None = None) -> dict:
    """Expose validated diagnostics only from a newly created original-recipe failure."""
    if output_existed or stage != 'original-product-recipe':
        return {}
    try:
        receipt, _ = read_diagnostic_receipt(output / 'retained/receipt.json')
        if type(receipt.get('schema')) is not int or receipt['schema'] != 1 or \
                receipt.get('mode') != 'selected-product-artifact-control' or receipt.get('success') is not False or \
                receipt.get('stage') != stage or receipt.get('failure_code') != stage + '-failed' or \
                receipt.get('product') != 'core' or receipt.get('line') not in ('3.8', '3.9') or \
                'product_recipe_failure' not in receipt:
            return {}
        selected = None
        if 'source_producer_failure' in receipt['product_recipe_failure']:
            require(plan_bytes is not None and metadata.sha256(plan_bytes) == plan_sha256 == receipt['plan_sha256'],
                    'diagnostic_invalid')
            plan = planner.read_json(plan_bytes)
            selected = plan['inventory']['selected']
        return {'product_recipe_failure': validate_recipe_failure(receipt['product_recipe_failure'], selected)}
    except Exception:
        return {'product_recipe_failure': {'status': 'unavailable'}}


def failure_receipt_status(output: Path, output_existed: bool, *, mode: str, stages: frozenset[str]) -> tuple[bool, str]:
    """Observe only a new safe receipt and project its fixed failed stage, never private fields."""
    created, stage = False, 'unclassified'
    if output_existed:
        return created, stage
    receipt = output / 'retained/receipt.json'
    try:
        created = receipt.is_file() and not any(part.is_symlink() for part in (receipt, *receipt.parents))
        if not created:
            return created, stage
        maximum = 64 * 1024 ** 2
        if receipt.stat().st_size > maximum or any(part.is_symlink() for part in (receipt, *receipt.parents)):
            return created, stage
        with receipt.open('rb') as stream:
            data = stream.read(maximum + 1)
        if len(data) > maximum:
            return created, stage
        result = json.loads(data)
        candidate = result.get('stage') if type(result) is dict else None
        if type(candidate) is str and candidate in stages and type(result.get('schema')) is int and \
                result['schema'] == 1 and result.get('mode') == mode and result.get('success') is False and \
                result.get('failure_code') == candidate + '-failed':
            stage = candidate
    except (OSError, ValueError, TypeError, RecursionError):
        pass
    return created, stage


def fresh_public(observation: dict, checked_at: str) -> None:
    """Require a recent public observation with valid identity metadata when present."""
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
        if plan['product'] == 'core':
            core.policy(binding)
            metadata.continuation.validate_tag_history(plan['line'], binding['observation']['tag_history'])
        observations = ([binding['observation'][key] for key in ('branch_observation', 'tag_observation')]
                        if plan['product'] == 'core' else [])
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
    """Verify clean controller trees, planner input hashes, and the admitted source binding."""
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


def refresh_remote(plan: dict, source: Path, semantics: planner.Semantics) -> dict:
    """GET-only preflight using the reviewed exact edges and original mapping."""
    observations = planner.Observations()
    ids = [row['id'] for row in plan['inventory']['selected']]
    edges = [{key: row[key] for key in ('consumer', 'project', 'framework', 'id', 'range', 'version')}
             for row in plan['prerequisites']]
    feeds = planner.FeedMetadata(source / 'NuGet.Config', ids + [row['id'] for row in edges], semantics, observations)
    require(feeds.policy == plan['consumer_feed_policy'], 'artifact_feed_policy_changed')
    feeds.prefetch(ids, edges)
    npm_histories = [(identifier, observations.get(planner.history_url(identifier, True)))
                     for identifier in planner.NPM_IDS] if plan['npm'] else []
    checked = planner.now()
    histories = [feeds.history(identifier, plan['requested_version'], plan['line'], semantics, checked) for identifier in ids]
    histories += [planner.check_history(identifier, plan['requested_version'], plan['line'],
        observation, semantics, checked, npm=True) for identifier, observation in npm_histories]
    prerequisites = [feeds.prerequisite(edge, semantics, checked) for edge in edges]
    require(all(row['eligible'] is True for row in histories + prerequisites), 'artifact_fresh_prerequisite_failed')
    # New history observations may grow; requested version must still be eligible.
    for old, current in zip(plan['prerequisites'], prerequisites):
        before = {row['observation']['sha256'] for row in old['feeds'] if row['eligible']}
        after = {row['observation']['sha256'] for row in current['feeds'] if row['eligible']}
        require(before == after, 'artifact_prerequisite_metadata_changed')
    return {'checked_at': checked, 'eligible': True, 'histories': histories, 'prerequisites': prerequisites}


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


def preflight(source: Path, plan: dict, private: Path) -> dict:
    """Read-only tool/evaluation commands; no restore, compile, test or pack."""
    if plan.get('product') == 'extensions':
        recipe = maintenance.recipes(plan['source'], plan['requested_version'], private)
        require(len(recipe) == 1 and recipe[0][0] == '.', 'artifact_extensions_recipe_configuration')
        command = recipe[0][1]
        positions = [index for index, value in enumerate(command) if value.split('=', 1)[0].casefold() == '--configuration']
        require(len(positions) == 1 and command[positions[0]:positions[0] + 2] == ['--configuration', 'Release'],
            'artifact_extensions_recipe_configuration')

    def inspect(label: str, command: list[str]) -> str:
        """Run a toolchain probe and retain its command and output in a private log."""
        log = private / ('preflight-' + label + '.log')
        try:
            output = run(command, source, timeout=60, env=maintenance.build_environment())
        except Exception as error:
            log.write_text(json.dumps(command) + '\n' + str(error) + '\n')
            raise
        log.write_text(json.dumps(command) + '\n' + output)
        return output.strip()

    tools = {'product_work_executed': False}
    if plan.get('product', 'studio') == 'studio':
        node = inspect('node', ['node', '--version'])
        require(re.fullmatch(r'v22\.[0-9]+\.[0-9]+', node) is not None, 'artifact_node_version')
        npm_version = inspect('npm', ['npm', '--version'])
        require(re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', npm_version) is not None and
                int(npm_version.split('.')[0]) >= 9, 'artifact_npm_version')
        tools.update(node=node, npm=npm_version)
    sdks = inspect('sdks', ['dotnet', '--list-sdks'])
    require(any(line.startswith(metadata.SDK + ' ') for line in sdks.splitlines()), 'artifact_sdk_unavailable')
    require(inspect('sdk-selection', ['dotnet', '--version']) == metadata.SDK, 'artifact_sdk_selection')
    tools['sdk'] = metadata.SDK
    if plan.get('product', 'studio') in ('extensions', 'core'):
        return tools
    workflow = source / plan['npm']['workflow']['path']
    require(metadata.sha256(workflow.read_bytes()) == plan['npm']['workflow']['sha256'], 'artifact_host_recipe_identity')
    frameworks = re.findall(r'^\s*run: dotnet publish \./src/hosts/Elsa\.Studio\.Host\.CustomElements .* -f (net[0-9.]+)\s*$',
                            workflow.read_text(), re.MULTILINE)
    require(frameworks == ['net10.0'], 'artifact_host_recipe_framework')
    host = historical.HOST / 'Elsa.Studio.Host.CustomElements.csproj'
    properties = json.loads(inspect('host-frameworks', ['dotnet', 'msbuild', str(host), '-nologo',
                                  '-getProperty:TargetFrameworks,TargetFramework']))['Properties']
    supported = (properties.get('TargetFrameworks') or properties.get('TargetFramework', '')).split(';')
    require(frameworks[0] in supported, 'artifact_host_unsupported_framework')
    return tools | {'host_framework': frameworks[0],
            'host_supported_frameworks': supported, 'original_workflow_sha256': metadata.sha256(workflow.read_bytes()),
            'host_project_sha256': metadata.sha256((source / host).read_bytes()),
            'product_work_executed': False}


def execute(root: Path, data: bytes, digest: str, output: Path, *, setup_only: bool = False) -> dict:
    """Admit a plan, run its selected artifact recipe, and persist the stage receipt."""
    plan = admit(data, digest)
    require(not output.exists() and not output.resolve().is_relative_to(root.resolve()) and
            not any(part.is_symlink() for part in (output, *output.parents)), 'artifact_output_location')
    controller = verify_controller(root, plan)
    execution = selected_execution('artifact', controller, plan, digest)
    # Adapter admission does not certify any actual product control.
    require(plan['product'] in ('studio', 'extensions') and plan['line'] in ('3.8', '3.9') or
            plan['product'] == 'core' and plan['line'] in ('3.8', '3.9'), 'artifact_control_not_implemented')
    if plan['product'] == 'core':
        core.validate_plan(plan)
        core.verify_source(root, plan['source'])
    elif plan['line'] == '3.9':
        maintenance39.verify_source(root, plan)
    output.mkdir(parents=True)
    (output / 'global.json').write_text(json.dumps({'sdk': {'version': metadata.SDK, 'rollForward': 'disable'}}))
    private, retained = output / 'private', output / 'retained'
    private.mkdir()
    retained.mkdir()
    receipt = {'schema': 1, 'mode': 'selected-product-artifact-control', 'plan_sha256': digest,
               'planner_controller': plan['controller'], 'artifact_controller': controller, 'source': plan['source'],
               'product': plan['product'], 'line': plan['line'], 'version': plan['requested_version'],
               'execution': execution, 'success': False, 'published': False,
               'version_allocated': False, 'tag_created': False, 'stage': 'source-setup'}
    producer_completed = False
    try:
        source = private / 'admitted-source'
        metadata.checkout_source(root, plan['source'], source)
        if plan['product'] == 'extensions':
            extensions.verify_source(root, plan['source']['commit'])
        inventory = plan['inventory']
        for path, expected in ((inventory['release_recipe']['solution'], inventory['release_recipe']['sha256']),
                               (inventory['release_recipe']['workflow'], inventory['release_recipe']['workflow_sha256'])):
            require(metadata.sha256((source / path).read_bytes()) == expected, 'artifact_recipe_identity')
        require(planner.npm_intent(source, plan['source'], plan['requested_version']) == plan['npm'], 'artifact_npm_intent')
        receipt['stage'] = 'tool-preflight'
        receipt['preflight'] = preflight(source, plan, private)
        receipt['stage'] = 'fresh-prerequisites'
        helper = private / 'semantics'
        helper.mkdir()
        refresh_remote(plan, source, planner.build_helper(helper))
        if setup_only:
            receipt.update(stage='setup-complete', setup_complete=True, artifact_proof=False)
            return receipt
        receipt['stage'] = 'original-product-recipe'
        if plan['product'] == 'core':
            producer = maintenance.prepare(root, plan['source'] | {'source_repository': maintenance.CORE_REPOSITORY},
                                           plan['requested_version'], private / 'producer', plan=plan)
        else:
            rows = [row for row in maintenance.registered_core_candidates(maintenance.load_register())
                    if row['commit'] == plan['source']['commit'] and row['kind'] == 'maintenance']
            require(len(rows) == 1, 'artifact_registered_recipe')
            producer = maintenance.prepare(root, rows[0], plan['requested_version'], private / 'producer')
        require(producer['success'] is True, 'artifact_producer_failed')
        producer_completed = True
        receipt['product_tests'] = producer['tests']
        if plan['product'] == 'core':
            receipt['package_verification'] = producer['packages']
        receipt['packages'] = retain_selected(plan, private / 'producer/artifacts', retained / 'nuget')
        if plan['product'] == 'extensions':
            selected = {row['id'].casefold() for row in plan['inventory']['selected']}
            receipt['manifest_verification'] = [{key: row[key] for key in ('id', 'package_manifest', 'sdk_assets')}
                for row in producer['packages'] if row['id'].casefold() in selected]
        if plan['product'] == 'studio':
            receipt['stage'] = 'historical-studio-npm'
            receipt['npm'] = historical.prove(private / 'producer/source', private / 'npm', retained / 'npm', plan, execution,
                                              receipt['preflight']['host_framework'], controller, digest)
        receipt.update(success=True, stage='complete', artifact_proof=True)
        return receipt
    except Exception:
        receipt['failure_code'] = receipt['stage'] + '-failed'
        if plan['product'] == 'core' and receipt['stage'] == 'original-product-recipe' and not producer_completed:
            receipt['product_recipe_failure'] = recipe_failure_diagnostics(output, plan, controller, execution)
        raise
    finally:
        (retained / 'receipt.json').write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n')


def main() -> int:
    """Run the artifact CLI and report success or bounded failure diagnostics."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--setup-only', action='store_true')
    args = parser.parse_args()
    output_existed = True
    plan_bytes = None
    try:
        output_existed = args.output.exists() or args.output.is_symlink()
        plan_bytes = args.plan.read_bytes()
        execute(ROOT, plan_bytes, args.plan_sha256, args.output, setup_only=args.setup_only)
        return 0
    except Exception as error:
        code = str(error) if type(error) is ValueError and str(error) in PUBLIC_FAILURE_CODES else 'artifact_control_failed'
        receipt_created, stage = failure_receipt_status(args.output, output_existed,
            mode='selected-product-artifact-control', stages=PUBLIC_FAILURE_STAGES)
        print(json.dumps({'success': False, 'failure_code': code, 'failure_stage': stage,
            'retained_receipt_created': receipt_created} | public_recipe_failure(args.output, output_existed, stage,
                plan_bytes=plan_bytes, plan_sha256=args.plan_sha256)))
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
