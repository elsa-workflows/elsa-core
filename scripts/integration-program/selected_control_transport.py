"""Bounded, pure selected-control transport primitives.

This module does not admit product receipts, seal a control, extract files, or
certify a product. Its byte checks are a prerequisite for the separately closed
six-cell payload validator. No fixed consolidated/Slack identities are reused.
"""
from __future__ import annotations

import base64
from datetime import datetime, timezone
import gzip
import hashlib
import io
import json
import math
from pathlib import Path
import re
import stat
import tarfile
from uuid import UUID
import zipfile
import zlib

from product_artifact_execution import HOSTED_REF, REPOSITORY, REPOSITORY_ID, WORKFLOW
from prove_consolidated_packages import require

MAX_ARCHIVE_BYTES = 2 * 1024 ** 3
MAX_EXPANDED_BYTES = 8 * 1024 ** 3
MAX_MEMBERS = 10_000
MAX_PACKAGE_BYTES = 512 * 1024 ** 2
MAX_RECEIPT_BYTES = 64 * 1024 ** 2
MAX_PLAN_BYTES = 32 * 1024 ** 2
MANIFEST = 'preupload-manifest.json'
TRANSPORT_SCOPE = 'transport-only-not-selected-control-acceptance'


def closed(value: object, keys: set[str], label: str) -> dict:
    require(type(value) is dict and set(value) == keys, label + '_schema')
    return value


def digest(value: object, length: int = 64) -> str:
    require(type(value) is str and re.fullmatch('[a-f0-9]{' + str(length) + '}', value) is not None,
            'selected_transport_digest')
    return value


def integer(value: object, maximum: int, *, positive: bool = False) -> int:
    require(type(value) is int and (value > 0 if positive else value >= 0) and value <= maximum,
            'selected_transport_size')
    return value


def safe_name(value: object) -> str:
    require(type(value) is str, 'selected_transport_path')
    try:
        length = len(value.encode('utf-8'))
    except UnicodeError:
        raise ValueError('selected_transport_path') from None
    require(value and length <= 4096 and
            not any(ord(char) < 32 or ord(char) == 127 for char in value) and
            not any(char in value for char in ('\\', ':', '\x00')) and
            all(part not in ('', '.', '..') for part in value.split('/')),
            'selected_transport_path')
    return value


def unique_names(names: list[str]) -> None:
    require(len(names) <= MAX_MEMBERS and len({safe_name(name).casefold() for name in names}) == len(names),
            'selected_transport_duplicate')
    # Reject a file used as another member's directory, including case variants.
    folded = {name.casefold() for name in names}
    require(all('/'.join(name.casefold().split('/')[:index]) not in folded
                for name in names for index in range(1, len(name.split('/')))),
            'selected_transport_path_collision')


def strict_json(data: bytes, maximum: int = MAX_RECEIPT_BYTES) -> dict:
    require(type(data) is bytes and len(data) <= maximum, 'selected_transport_json_size')
    def object_pairs(pairs):
        result = {}
        for key, value in pairs:
            require(key not in result, 'selected_transport_json_duplicate')
            result[key] = value
        return result
    def reject_constant(_):
        raise ValueError('selected_transport_json_constant')
    def finite_float(value):
        number = float(value)
        require(math.isfinite(number), 'selected_transport_json_nonfinite')
        return number
    try:
        result = json.loads(data.decode('utf-8'), object_pairs_hook=object_pairs,
                            parse_constant=reject_constant, parse_float=finite_float)
    except (UnicodeError, json.JSONDecodeError, RecursionError):
        raise ValueError('selected_transport_json') from None
    require(type(result) is dict, 'selected_transport_json_object')
    return result


def utc(value: object, *, now: datetime) -> datetime:
    require(isinstance(now, datetime) and now.tzinfo is not None and
            now.utcoffset().total_seconds() == 0, 'selected_transport_clock')
    require(type(value) is str and value.endswith(('Z', '+00:00')), 'selected_transport_time')
    try:
        result = datetime.fromisoformat(value.replace('Z', '+00:00'))
    except ValueError:
        raise ValueError('selected_transport_time') from None
    require(result.tzinfo is not None and result.utcoffset().total_seconds() == 0 and
            result <= now, 'selected_transport_time')
    return result


def safe_file(path: Path) -> Path:
    require(path.is_absolute() and '..' not in path.parts and
            not any(part.is_symlink() for part in (path, *path.parents)) and path.is_file(),
            'selected_transport_file')
    return path


def read_bound(path: Path, expected_hash: str, maximum: int) -> bytes:
    safe_file(path)
    digest(expected_hash)
    integer(path.stat().st_size, maximum)
    with path.open('rb') as stream:
        data = stream.read(maximum + 1)
    require(len(data) <= maximum and hashlib.sha256(data).hexdigest() == expected_hash,
            'selected_transport_bytes')
    return data


def _bounded_read(stream, expected: int, maximum: int) -> bytes:
    integer(expected, maximum)
    data = stream.read(expected + 1)
    require(len(data) == expected, 'selected_transport_member_size')
    return data


def zip_members(data: bytes, *, leaf: bool = False) -> dict[str, bytes]:
    """Check every ZIP member before returning bytes; never extract or execute."""
    require(type(data) is bytes and len(data) <= (MAX_PACKAGE_BYTES if leaf else MAX_ARCHIVE_BYTES),
            'selected_transport_archive_size')
    maximum = MAX_PACKAGE_BYTES if leaf else MAX_EXPANDED_BYTES
    try:
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            infos = archive.infolist()
            require(len(infos) <= MAX_MEMBERS, 'selected_transport_member_count')
            # upload-artifact v4 emits file members. Explicit directory entries
            # are unnecessary for the selected canonical payload and fail closed.
            require(all(item.orig_filename == item.filename for item in infos), 'selected_transport_path')
            unique_names([item.filename for item in infos])
            total = 0
            for item in infos:
                mode = item.external_attr >> 16
                require(not item.is_dir() and stat.S_IFMT(mode) in (0, stat.S_IFREG) and
                        not item.external_attr & 0x10 and not item.flag_bits & 1 and item.compress_type in (zipfile.ZIP_STORED, zipfile.ZIP_DEFLATED),
                        'selected_transport_member_type')
                total += integer(item.file_size, maximum)
                require(total <= maximum, 'selected_transport_expanded_size')
                if not leaf:
                    integer(item.file_size, file_limit(item.filename))
            result = {}
            for item in infos:
                with archive.open(item) as stream:
                    result[item.filename] = _bounded_read(stream, item.file_size,
                        MAX_PACKAGE_BYTES if leaf else file_limit(item.filename))
            return result
    except (zipfile.BadZipFile, OSError, RuntimeError, EOFError, zlib.error):
        raise ValueError('selected_transport_zip') from None


def tar_members(data: bytes) -> dict[str, bytes]:
    """Accept only bounded regular npm package files, without extraction."""
    require(type(data) is bytes and len(data) <= MAX_PACKAGE_BYTES, 'selected_transport_archive_size')
    result, total, count, folded = {}, 0, 0, set()
    try:
        with gzip.GzipFile(fileobj=io.BytesIO(data)) as compressed:
            expanded = compressed.read(MAX_PACKAGE_BYTES + 1)
        require(len(expanded) <= MAX_PACKAGE_BYTES and len(expanded) % tarfile.BLOCKSIZE == 0,
                'selected_transport_expanded_size')
        with tarfile.open(fileobj=io.BytesIO(expanded), mode='r:') as archive:
            for item in archive:
                count += 1
                require(count <= MAX_MEMBERS and item.isfile() and
                        item.name.startswith('package/'), 'selected_transport_tar_member')
                name = safe_name(item.name[len('package/'):])
                require(name.casefold() not in folded, 'selected_transport_duplicate')
                folded.add(name.casefold())
                total += integer(item.size, MAX_PACKAGE_BYTES)
                require(total <= MAX_PACKAGE_BYTES, 'selected_transport_expanded_size')
                with archive.extractfile(item) as stream:
                    result[name] = _bounded_read(stream, item.size, MAX_PACKAGE_BYTES)
            require(not any(memoryview(expanded)[archive.offset:]), 'selected_transport_tar_trailing_members')
        unique_names(list(result))
        require(result and 'package.json' in result, 'selected_transport_npm_metadata')
        strict_json(result['package.json'])
        return result
    except (tarfile.TarError, OSError, EOFError, zlib.error):
        raise ValueError('selected_transport_tar') from None


def file_limit(name: str) -> int:
    safe_name(name)
    if name == 'plan.json':
        return MAX_PLAN_BYTES
    if name == MANIFEST or name in ('producer/receipt.json', 'consumer/receipt.json', 'producer/npm/receipt.json'):
        return MAX_RECEIPT_BYTES
    if name.startswith('producer/nuget/') and name.count('/') == 2 and name.endswith(('.nupkg', '.snupkg')):
        return MAX_PACKAGE_BYTES
    if name.startswith('producer/npm/') and name.count('/') == 2 and name.endswith('.tgz'):
        return MAX_PACKAGE_BYTES
    raise ValueError('selected_transport_unlisted_path')


def inventory(files: dict[str, bytes]) -> list[dict]:
    unique_names(list(files))
    return [{'path': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()}
            for name, data in sorted(files.items())]


def archive_inventory(data: bytes, *, npm: bool = False) -> list[dict]:
    return inventory(tar_members(data) if npm else zip_members(data, leaf=True))


def sha512_integrity(data: bytes) -> str:
    return 'sha512-' + base64.b64encode(hashlib.sha512(data).digest()).decode('ascii')


def validate_context(context: dict, controller: dict) -> None:
    """Structural immutable job claims; no original GITHUB_JOB/environment read."""
    closed(controller, {'commit', 'tree'}, 'selected_transport_controller')
    for value in controller.values():
        digest(value, 40)
    closed(context, {'repository', 'repository_id', 'event', 'ref', 'head_sha', 'workflow_ref',
                     'workflow_sha', 'run_id', 'run_attempt', 'job', 'workflow_path'}, 'selected_transport_context')
    require(context['repository'] == REPOSITORY and context['repository_id'] == REPOSITORY_ID and
            context['event'] == 'push' and context['ref'] == HOSTED_REF and context['workflow_path'] == WORKFLOW and
            context['head_sha'] == context['workflow_sha'] == controller['commit'] and
            context['workflow_ref'] == f'{REPOSITORY}/{WORKFLOW}@{HOSTED_REF}', 'selected_transport_context_binding')
    require(all(type(context[key]) is str and re.fullmatch('[1-9][0-9]*', context[key])
                for key in ('run_id', 'run_attempt')) and type(context['job']) is str and
            re.fullmatch('[A-Za-z_][A-Za-z0-9_-]*', context['job']), 'selected_transport_context_run')


def validate_execution(execution: dict, role: str, controller: dict, plan: dict, plan_hash: str,
                       *, now: datetime) -> None:
    closed(execution, {'kind', 'id', 'started_at', 'role', 'controller', 'plan_sha256', 'product', 'line',
                       'source', 'context', 'authority'}, 'selected_transport_execution')
    try:
        identity = UUID(execution['id'])
    except (ValueError, TypeError, AttributeError):
        raise ValueError('selected_transport_execution_uuid') from None
    require(identity.version == 4 and str(identity) == execution['id'], 'selected_transport_execution_uuid')
    utc(execution['started_at'], now=now)
    require(execution['kind'] == 'github-actions-selected-control' and role in ('artifact', 'consumer') and
            execution['role'] == role and execution['controller'] == controller and
            execution['plan_sha256'] == digest(plan_hash) and execution['product'] == plan['product'] and
            execution['line'] == plan['line'] and execution['source'] == plan['source'] and
            execution['authority'] == 'runner-environment-only-provider-unverified', 'selected_transport_execution_binding')
    validate_context(execution['context'], controller)
    require(all(plan['controller'][key] == controller[key] for key in ('commit', 'tree')) and
            plan['controller']['execution'] == {key: execution['context'][value] for key, value in {
                'repository': 'repository', 'event_name': 'event', 'ref': 'ref', 'run_id': 'run_id',
                'run_attempt': 'run_attempt'}.items()}, 'selected_transport_planner_context')



def validate_execution_pair(plan: dict, plan_hash: str, artifact: dict, consumer: dict,
                            artifact_controller: dict, consumer_controller: dict, *, now: datetime) -> None:
    """Join same-job hosted stage claims without reading the readback job's env."""
    validate_execution(artifact, 'artifact', artifact_controller, plan, plan_hash, now=now)
    validate_execution(consumer, 'consumer', consumer_controller, plan, plan_hash, now=now)
    require(artifact['id'] != consumer['id'] and artifact['context'] == consumer['context'] and
            utc(artifact['started_at'], now=now) <= utc(consumer['started_at'], now=now),
            'selected_transport_stage_identity')


def validate_provider(provider: dict, expected: dict, *, now: datetime) -> None:
    """Closed projection supplied by the future fixed actions:read retrieval job.

    This only binds the projection; it cannot establish that an arbitrary caller
    actually obtained it from GitHub. Current availability requires later recheck.
    """
    closed(expected, {'context', 'controller', 'product', 'line', 'manifest_sha256'}, 'selected_transport_expected')
    validate_context(expected['context'], expected['controller'])
    require(expected['product'] in ('core', 'studio', 'extensions') and expected['line'] in ('3.8', '3.9'),
            'selected_transport_cell')
    digest(expected['manifest_sha256'])
    closed(provider, {'schema', 'repository', 'repository_id', 'run_id', 'run_attempt', 'head_sha', 'head_branch',
                      'event', 'workflow_path', 'control_job', 'control_conclusion', 'run_status', 'run_conclusion',
                      'artifact_id', 'artifact_name', 'archive_sha256', 'archive_size', 'manifest_sha256',
                      'created_at', 'expires_at', 'expired', 'retrieved_at'}, 'selected_transport_provider')
    context = expected['context']
    require(type(provider['schema']) is int and provider['schema'] == 1 and
            all(provider[key] == context[key] for key in ('repository', 'repository_id', 'run_id', 'run_attempt',
                'head_sha', 'event', 'workflow_path')) and provider['head_branch'] == HOSTED_REF.removeprefix('refs/heads/') and
            provider['control_job'] == context['job'] and provider['control_conclusion'] == 'success',
            'selected_transport_provider_binding')
    require((provider['run_status'], provider['run_conclusion']) in
            (('in_progress', None), ('completed', 'success')), 'selected_transport_provider_run')
    integer(provider['artifact_id'], 2 ** 63 - 1, positive=True)
    name = (f"selected-control-{expected['product']}-{expected['line']}-{context['head_sha']}-"
            f"{context['run_id']}-{context['run_attempt']}")
    require(provider['artifact_name'] == name and provider['manifest_sha256'] == expected['manifest_sha256'] and
            provider['expired'] is False, 'selected_transport_provider_artifact')
    digest(provider['archive_sha256'])
    integer(provider['archive_size'], MAX_ARCHIVE_BYTES, positive=True)
    created, retrieved = (utc(provider[key], now=now) for key in ('created_at', 'retrieved_at'))
    # Expiry is deliberately allowed to be in the future, unlike observation times.
    try:
        expiry = datetime.fromisoformat(provider['expires_at'].replace('Z', '+00:00'))
    except (ValueError, TypeError, AttributeError):
        raise ValueError('selected_transport_provider_expiry') from None
    require(expiry.tzinfo is not None and expiry.utcoffset().total_seconds() == 0 and
            created <= retrieved <= now < expiry, 'selected_transport_provider_expiry')


def readback_transport(data: bytes, provider: dict, expected: dict, *, now: datetime | None = None) -> dict[str, bytes]:
    """Verify original outer bytes and complete manifest closure, never extract.

    Receipt/native/product validation is deliberately NOT implied by returning
    these bytes. There is no public seal/control-readback CLI until that separate
    six-cell validator is implemented and reviewed.
    """
    now = now or datetime.now(timezone.utc)
    validate_provider(provider, expected, now=now)
    require(len(data) == provider['archive_size'] and hashlib.sha256(data).hexdigest() == provider['archive_sha256'],
            'selected_transport_outer_bytes')
    files = zip_members(data)
    require(MANIFEST in files and hashlib.sha256(files[MANIFEST]).hexdigest() == expected['manifest_sha256'],
            'selected_transport_manifest_bytes')
    manifest = strict_json(files[MANIFEST])
    closed(manifest, {'schema', 'scope', 'context', 'controller', 'product', 'line', 'files'}, 'selected_transport_manifest')
    require(type(manifest['schema']) is int and manifest['schema'] == 1 and manifest['scope'] == TRANSPORT_SCOPE and
            all(manifest[key] == expected[key] for key in ('context', 'controller', 'product', 'line')),
            'selected_transport_manifest_binding')
    require(type(manifest['files']) is list and len(manifest['files']) <= MAX_MEMBERS, 'selected_transport_manifest_files')
    entries = {}
    for row in manifest['files']:
        closed(row, {'path', 'size', 'sha256'}, 'selected_transport_manifest_entry')
        name = safe_name(row['path'])
        require(name != MANIFEST and name not in entries, 'selected_transport_manifest_duplicate')
        integer(row['size'], file_limit(name))
        digest(row['sha256'])
        entries[name] = row
    unique_names(list(entries))
    require(set(files) == set(entries) | {MANIFEST}, 'selected_transport_manifest_closure')
    require(inventory({key: value for key, value in files.items() if key != MANIFEST}) ==
            sorted(entries.values(), key=lambda row: row['path']), 'selected_transport_manifest_content')
    return files
