"""Failure-only selected focus for #8693; remove with the selected-control failure adapter."""
from __future__ import annotations


def selected_focus(value: object, selected: object, *, package_key: str,
                   framework_required: bool = False) -> dict | None:
    """Reconstruct exact admitted package context without exposing its project path."""
    if type(value) is not dict or type(selected) is not list:
        return None
    fields = set(value)
    if (fields not in ({package_key}, {package_key, 'framework'}) or
            framework_required and 'framework' not in value or type(value.get(package_key)) is not str):
        return None
    matches = [row for row in selected if type(row) is dict and row.get('id') == value[package_key]]
    if len(matches) != 1:
        return None
    row = matches[0]
    if type(row.get('project')) is not str or not row['project'] or type(row.get('frameworks')) is not list:
        return None
    result = {package_key: row['id']}
    if 'framework' in value:
        if type(value['framework']) is not str or value['framework'] not in row['frameworks']:
            return None
        result['framework'] = next(framework for framework in row['frameworks'] if framework == value['framework'])
    return result


FEED_REASONS = frozenset(('feed_missing', 'feed_unavailable', 'feed_stale', 'feed_malformed',
                         'feed_endpoint_identity', 'feed_mapping_unavailable'))
HISTORY_REASONS = FEED_REASONS | frozenset(('history_missing', 'history_unavailable', 'history_stale',
    'history_malformed', 'history_partial', 'history_ambiguous', 'version_reused', 'version_not_monotonic'))
PREREQUISITE_REASONS = FEED_REASONS | frozenset(('prerequisite_missing', 'prerequisite_unavailable',
    'prerequisite_stale', 'prerequisite_malformed', 'prerequisite_wrong_version',
    'prerequisite_incompatible_framework', 'prerequisite_feed_ambiguity',
    'prerequisite_missing_from_applicable_feeds'))
BUILD_GROUP_REASONS = frozenset(('original-package-missing', 'current-group-mismatch',
    'original-group-mismatch', 'current-build-transitive', 'original-build-transitive'))


def exact_strings(value: object, fields: set[str]) -> bool:
    return type(value) is dict and set(value) == fields and all(type(item) is str for item in value.values())


def prerequisite_context(value: object, *, role: str, stage: str) -> dict | None:
    """Keep closed actual private context even when no public plan selector admits its identity."""
    if (role, stage) not in (('artifact', 'fresh-prerequisites'), ('consumer', 'current-consumer-prerequisites')):
        return None
    if type(value) is not dict or value.get('guard') != 'artifact_fresh_prerequisite_failed':
        return None
    fields = {'guard', 'kind', 'id', 'version', 'reason'}
    if value.get('kind') == 'history':
        return dict(value) if exact_strings(value, fields) and value['reason'] in HISTORY_REASONS else None
    fields |= {'consumer', 'project', 'framework'}
    return (dict(value) if value.get('kind') == 'prerequisite' and exact_strings(value, fields) and
            value['reason'] in PREREQUISITE_REASONS else None)


def prerequisite_failure(value: object, plan: dict, *, role: str, stage: str) -> dict | None:
    """Project only actual refresh failures in the admitted history or external edge scope."""
    value = prerequisite_context(value, role=role, stage=stage)
    if value is None:
        return None
    if value['kind'] == 'history':
        if (value['version'] != plan['requested_version'] or
                selected_focus({'id': value['id']}, plan['inventory']['selected'], package_key='id') is None):
            return None
        return value
    keys = ('consumer', 'project', 'framework', 'id', 'version')
    if not any(all(row.get(key) == value[key] for key in keys) for row in plan['prerequisites']):
        return None
    return {key: value[key] for key in ('guard', 'kind', 'consumer', 'framework', 'id', 'version', 'reason')}


def original_build_group_context(value: object, *, role: str, stage: str, product: str) -> dict | None:
    if role != 'consumer' or stage not in ('selected-restore-compile', product + '-runtime-contract'):
        return None
    fields = {'guard', 'id', 'version', 'framework', 'kind', 'reason'}
    return (dict(value) if exact_strings(value, fields) and value['guard'] == 'consumer_original_build_group' and
            value['reason'] in BUILD_GROUP_REASONS and value['kind'] in ('build', 'buildMultiTargeting') else None)


def original_build_group_failure(value: object, plan: dict, focus: object, *, role: str, stage: str) -> dict | None:
    """Join an actual dependency key to the exact selected root and admitted dependency scope."""
    value = original_build_group_context(value, role=role, stage=stage, product=plan['product'])
    if value is None:
        return None
    selected = plan['inventory']['selected']
    root = selected_focus(focus, selected, package_key='id', framework_required=True)
    if root is None or root['framework'] != value['framework']:
        return None
    dependency = selected_focus({'id': value['id'], 'framework': value['framework']}, selected,
                                package_key='id', framework_required=True)
    if dependency is not None and value['version'] == plan['requested_version']:
        return {key: value[key] for key in ('guard', 'id', 'version', 'framework', 'kind', 'reason')}
    project = next(row['project'] for row in selected if row['id'] == root['id'])
    if any(row.get('consumer') == root['id'] and row.get('project') == project and
           all(row.get(key) == value[key] for key in ('framework', 'id', 'version')) for row in plan['prerequisites']):
        return {key: value[key] for key in ('guard', 'id', 'version', 'framework', 'kind', 'reason')}
    return None
