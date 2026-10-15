#!/usr/bin/env python3
"""Retain derived failed-control diagnostics, never original receipts or a seal.

Only the two fixed retained receipt locations are read. Original bytes stay
private and are bound by hash; #8693 owns this failure-only hosted adapter.
Remove with the selected-control action when replacement proof admission lands.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
from pathlib import Path

import product_artifact_execution as execution
import prove_product_release_artifacts as producer
import prove_product_release_consumers as consumer
import selected_control_seal as seal
import selected_control_transport as transport
from prove_consolidated_packages import require
from selected_product_diagnostics import selected_focus, prerequisite_failure, original_build_group_failure

SCOPE = 'selected-product-failure-diagnostics-only-not-control-proof'
SEAL_FAILURE_STATUS = 'seal-rejected'


def validate_identity(receipt: dict, role: str, plan: dict, plan_hash: str, controller: dict) -> None:
    """Join the receipt's bounded hosted identity to the admitted plan and current job."""
    mode = 'selected-product-artifact-control' if role == 'artifact' else 'selected-product-consumers'
    require(type(receipt.get('schema')) is int and receipt['schema'] == 1 and receipt.get('mode') == mode and
            type(receipt.get('success')) is bool and receipt.get('published') is False and
            receipt['source'] == plan['source'] and receipt['plan_sha256'] == plan_hash and
            receipt['planner_controller'] == plan['controller'] and receipt['artifact_controller'] == controller,
            'failure_diagnostic_identity')
    if role == 'artifact':
        require(all(receipt[key] is False for key in ('version_allocated', 'tag_created')) and
                (receipt['product'], receipt['line'], receipt['version']) ==
                (plan['product'], plan['line'], plan['requested_version']), 'failure_diagnostic_identity')
    else:
        require(receipt['controller'] == controller, 'failure_diagnostic_identity')
    require(receipt['execution'].get('kind') == 'github-actions-selected-control', 'failure_diagnostic_hosted')
    execution.validate_selected_execution(receipt['execution'], role, controller, plan, plan_hash)


def project(receipt: dict, digest: str, role: str, plan: dict, plan_hash: str, controller: dict) -> dict:
    """Reconstruct only reviewed fixed fields and validated optional diagnostic details."""
    validate_identity(receipt, role, plan, plan_hash, controller)
    stages = producer.PUBLIC_FAILURE_STAGES if role == 'artifact' else consumer.PUBLIC_FAILURE_STAGES
    require(receipt['success'] is False and type(receipt.get('stage')) is str and receipt['stage'] in stages and
            receipt.get('failure_code') == receipt['stage'] + '-failed', 'failure_diagnostic_receipt')
    result = {'schema': 1, 'scope': SCOPE, 'success': False, 'published': False,
        'original_receipt_sha256': digest, 'plan_sha256': plan_hash,
        'product': plan['product'], 'line': plan['line'],
        'source': {key: plan['source'][key] for key in ('commit', 'tree')}, 'controller': controller,
        'execution': {'id': receipt['execution']['id'], 'role': role, 'context': execution.hosted_context(controller)},
        'stage': receipt['stage'], 'failure_code': receipt['stage'] + '-failed'}
    if role == 'consumer' and receipt['stage'] in ('selected-restore-compile', plan['product'] + '-runtime-contract'):
        focus = selected_focus(receipt.get('focus'), plan['inventory']['selected'],
                               package_key='id', framework_required=True)
        if focus is not None:
            result['focus'] = focus
    prerequisite = prerequisite_failure(receipt.get('prerequisite_failure'), plan, role=role, stage=receipt['stage'])
    if prerequisite is not None:
        result['prerequisite_failure'] = prerequisite
    build_group = original_build_group_failure(receipt.get('original_build_group_failure'), plan,
                                               receipt.get('focus'), role=role, stage=receipt['stage'])
    if build_group is not None:
        result['original_build_group_failure'] = build_group
    detail = {'status': 'unavailable'}
    try:
        if role == 'artifact' and receipt['stage'] == 'original-product-recipe':
            detail = producer.validate_recipe_failure(receipt['product_recipe_failure'], plan['inventory']['selected'])
        elif role == 'consumer' and 'dependency_range_failure' in receipt:
            candidate = consumer.resolution.validate_range_failure(receipt['dependency_range_failure'])
            roots = [row for row in plan['inventory']['selected'] if row['id'] == candidate['root_id']]
            require(len(roots) == 1 and candidate['framework'] in roots[0]['frameworks'], 'diagnostic_invalid')
            detail = candidate
    except (KeyError, TypeError, ValueError, RecursionError):
        pass
    result['product_recipe_failure' if role == 'artifact' else 'dependency_range_failure'] = detail
    return result


def project_seal_failure(failure: dict, failure_hash: str, producer_receipt: dict, producer_hash: str,
                         consumer_receipt: dict, consumer_hash: str, plan: dict, plan_hash: str,
                         controller: dict) -> dict:
    """Project a seal rejection only after both successful stage receipts are rebound."""
    validate_identity(producer_receipt, 'artifact', plan, plan_hash, controller)
    validate_identity(consumer_receipt, 'consumer', plan, plan_hash, controller)
    require(producer_receipt['success'] is True and producer_receipt.get('stage') == 'complete' and
            producer_receipt.get('artifact_proof') is True and
            consumer_receipt['success'] is True and consumer_receipt.get('stage') == 'complete' and
            consumer_receipt.get('artifact_receipt_sha256') == producer_hash and
            consumer_receipt.get('producer_execution') == producer_receipt['execution'],
            'failure_diagnostic_seal_stages')
    context = execution.hosted_context(controller)
    require(type(failure_hash) is str and len(failure_hash) == 64 and
            all(char in '0123456789abcdef' for char in failure_hash), 'failure_diagnostic_seal_hash')
    transport.validate_execution_pair(plan, plan_hash, producer_receipt['execution'], consumer_receipt['execution'],
        controller, controller, now=datetime.now(timezone.utc))
    return {'schema': 1, 'scope': SCOPE, 'status': SEAL_FAILURE_STATUS,
        'producer_success': True, 'consumer_success': True, 'published': False,
        'failure_record_sha256': failure_hash, 'plan_sha256': plan_hash,
        'producer_receipt_sha256': producer_hash, 'consumer_receipt_sha256': consumer_hash,
        'product': plan['product'], 'line': plan['line'],
        'source': {key: plan['source'][key] for key in ('commit', 'tree')}, 'controller': controller,
        'executions': {
            'artifact': {'id': producer_receipt['execution']['id'], 'context': context},
            'consumer': {'id': consumer_receipt['execution']['id'], 'context': context}},
        'seal_rejection': failure}


def retain(runner_temp: Path) -> Path:
    """Stage only validated projections into a new physically separate directory."""
    runner_temp = seal.absolute(runner_temp)
    require(runner_temp.is_dir(), 'failure_diagnostic_location')
    producer_path = runner_temp / 'selected-producer/retained/receipt.json'
    consumer_path = runner_temp / 'selected-consumer/retained/receipt.json'
    plan_path = runner_temp / 'selected-plan/plan.json'
    expected_path = runner_temp / 'selected-expected.json'
    output = seal.output_location(runner_temp / 'selected-failure-diagnostics',
                                  [path.parent for path in (producer_path, consumer_path, plan_path)] + [expected_path])
    original, producer_hash = producer.read_diagnostic_receipt(producer_path, maximum=transport.MAX_RECEIPT_BYTES)
    plan, plan_hash = producer.read_diagnostic_receipt(plan_path, maximum=transport.MAX_PLAN_BYTES)
    require(original['plan_sha256'] == plan_hash, 'failure_diagnostic_plan')
    plan = producer.admit(transport.read_bound(plan_path, plan_hash, transport.MAX_PLAN_BYTES), plan_hash,
                          checked_at=original['execution']['started_at'])
    controller = producer.verify_controller(producer.ROOT, plan)
    validate_identity(original, 'artifact', plan, plan_hash, controller)
    files = {}
    if original.get('success') is False:
        files['producer.json'] = project(original, producer_hash, 'artifact', plan, plan_hash, controller)
        require(not consumer_path.exists() and not consumer_path.is_symlink(), 'failure_diagnostic_stage_order')
    else:
        require(original.get('success') is True and original.get('stage') == 'complete' and
                original.get('artifact_proof') is True, 'failure_diagnostic_producer')
        failed, consumer_hash = producer.read_diagnostic_receipt(consumer_path, maximum=transport.MAX_RECEIPT_BYTES)
        require(failed['artifact_receipt_sha256'] == producer_hash and
                failed['producer_execution'] == original['execution'], 'failure_diagnostic_producer')
        if failed['success'] is False:
            transport.validate_execution_pair(plan, plan_hash, original['execution'], failed['execution'],
                controller, controller, now=datetime.now(timezone.utc))
            files['consumer.json'] = project(failed, consumer_hash, 'consumer', plan, plan_hash, controller)
        else:
            require(failed['success'] is True and failed.get('stage') == 'complete',
                    'failure_diagnostic_consumer')
            failure, failure_hash = seal.read_failure_record(expected_path, plan_hash, producer_hash, consumer_hash)
            files['seal.json'] = project_seal_failure(failure, failure_hash, original, producer_hash,
                failed, consumer_hash, plan, plan_hash, controller)
    output.mkdir()
    for name, value in files.items():
        (output / name).write_bytes(seal.encoded(value))
    return output


def main() -> int:
    """Fail closed with a constant message; never emit an exception or original JSON."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--runner-temp', type=Path, required=True)
    args = parser.parse_args()
    try:
        retain(args.runner_temp)
    except Exception:
        print(json.dumps({'scope': SCOPE, 'status': 'unavailable'}, sort_keys=True))
        return 1
    print(json.dumps({'scope': SCOPE, 'status': 'retained'}, sort_keys=True))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
