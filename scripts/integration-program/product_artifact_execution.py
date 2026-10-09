"""Local identity for the selected artifact control; no hosted authority."""
from __future__ import annotations

from datetime import datetime, timezone
import os
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
