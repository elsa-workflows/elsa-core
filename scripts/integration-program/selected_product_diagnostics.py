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
