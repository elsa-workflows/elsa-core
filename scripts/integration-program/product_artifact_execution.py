"""Selected control identities; hosted environment assertions are not provider proof."""
from __future__ import annotations

from datetime import datetime, timezone
import os
import re
from uuid import UUID, uuid4

from prove_consolidated_packages import require


def validate_local_execution(execution: dict) -> None:
    require(set(execution) == {'kind', 'id', 'started_at'} and execution['kind'] == 'local-control',
            'artifact_execution_kind')
    identity = UUID(execution['id'])
    require(identity.version == 4 and str(identity) == execution['id'], 'artifact_execution_identity')
    started = datetime.fromisoformat(execution['started_at'])
    require(started.tzinfo is not None and started.utcoffset().total_seconds() == 0, 'artifact_execution_time')


def local_execution(environment: dict | None = None) -> dict:
    environment = os.environ if environment is None else environment
    require(environment.get('GITHUB_ACTIONS') != 'true' and not any(
        environment.get(key) for key in ('GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT')), 'hosted_control_not_supported')
    execution = {'kind': 'local-control', 'id': str(uuid4()), 'started_at': datetime.now(timezone.utc).isoformat()}
    validate_local_execution(execution)
    return execution


REPOSITORY = 'elsa-workflows/elsa-core'
REPOSITORY_ID = '151148482'
WORKFLOW = '.github/workflows/selected-product-release-control.yml'
HOSTED_REF = 'refs/heads/codex/selected-product-hosted-controls-8693'

HOSTED_FIELDS = {'repository': 'GITHUB_REPOSITORY', 'repository_id': 'GITHUB_REPOSITORY_ID',
                 'event': 'GITHUB_EVENT_NAME', 'ref': 'GITHUB_REF', 'head_sha': 'GITHUB_SHA',
                 'workflow_ref': 'GITHUB_WORKFLOW_REF', 'workflow_sha': 'GITHUB_WORKFLOW_SHA',
                 'run_id': 'GITHUB_RUN_ID', 'run_attempt': 'GITHUB_RUN_ATTEMPT', 'job': 'GITHUB_JOB'}


def hosted_context(controller: dict) -> dict:
    """Runner assertions only; independent provider ZIP readback remains required."""
    environment = os.environ
    context = {key: environment.get(name) for key, name in HOSTED_FIELDS.items()}
    require(environment.get('GITHUB_ACTIONS') == 'true' and context['repository'] == REPOSITORY and
            context['repository_id'] == REPOSITORY_ID and context['event'] == 'push' and
            context['ref'] == HOSTED_REF and not environment.get('GITHUB_HEAD_REF') and
            not environment.get('GITHUB_BASE_REF'), 'artifact_hosted_context')
    require(set(controller) == {'commit', 'tree'} and all(isinstance(value, str) and
            re.fullmatch('[a-f0-9]{40}', value) for value in controller.values()), 'artifact_hosted_controller')
    require(context['head_sha'] == context['workflow_sha'] == controller['commit'] and
            context['workflow_ref'] == f'{REPOSITORY}/{WORKFLOW}@{HOSTED_REF}', 'artifact_hosted_workflow')
    require(all(isinstance(context[key], str) and re.fullmatch('[1-9][0-9]*', context[key])
            for key in ('run_id', 'run_attempt')) and isinstance(context['job'], str) and
            re.fullmatch('[A-Za-z_][A-Za-z0-9_-]*', context['job']), 'artifact_hosted_run')
    return context | {'workflow_path': WORKFLOW}


def validate_selected_execution(execution: dict, role: str, controller: dict, plan: dict, plan_hash: str) -> None:
    """Validate selected-stage identity; hosted claims bind the current trusted job."""
    if execution.get('kind') == 'local-control':
        validate_local_execution(execution)
        return
    require(set(execution) == {'kind', 'id', 'started_at', 'role', 'controller', 'plan_sha256',
                              'product', 'line', 'source', 'context', 'authority'} and
            execution['kind'] == 'github-actions-selected-control', 'artifact_execution_kind')
    validate_local_execution({key: execution[key] for key in ('id', 'started_at')} | {'kind': 'local-control'})
    require(datetime.fromisoformat(execution['started_at']) <= datetime.now(timezone.utc), 'artifact_hosted_time')
    require(role in ('artifact', 'consumer') and execution['role'] == role and execution['controller'] == controller,
            'artifact_hosted_role_controller')
    require(isinstance(plan_hash, str) and re.fullmatch('[a-f0-9]{64}', plan_hash) and
            execution['plan_sha256'] == plan_hash and execution['product'] == plan['product'] and
            execution['line'] == plan['line'] and execution['source'] == plan['source'], 'artifact_hosted_plan')
    require(execution['authority'] == 'runner-environment-only-provider-unverified' and
            execution['context'] == hosted_context(controller), 'artifact_hosted_binding')
    require(all(plan['controller'][key] == controller[key] for key in ('commit', 'tree')),
            'artifact_hosted_planner_controller')
    expected = {'repository': 'repository', 'event_name': 'event', 'ref': 'ref',
                'run_id': 'run_id', 'run_attempt': 'run_attempt'}
    require(plan['controller'].get('execution') == {key: execution['context'][value] for key, value in expected.items()},
            'artifact_hosted_planner_execution')


def selected_execution(role: str, controller: dict, plan: dict, plan_hash: str) -> dict:
    hosted_fields = ('GITHUB_ACTIONS', 'GITHUB_HEAD_REF', 'GITHUB_BASE_REF', *HOSTED_FIELDS.values())
    if not any(os.environ.get(key) for key in hosted_fields):
        return local_execution()
    execution = {'kind': 'github-actions-selected-control', 'id': str(uuid4()),
                 'started_at': datetime.now(timezone.utc).isoformat(), 'role': role, 'controller': controller,
                 'plan_sha256': plan_hash, 'product': plan['product'], 'line': plan['line'], 'source': plan['source'],
                 'context': hosted_context(controller), 'authority': 'runner-environment-only-provider-unverified'}
    validate_selected_execution(execution, role, controller, plan, plan_hash)
    return execution
