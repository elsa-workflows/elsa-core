"""Stage bridge fixtures against the selected checkout's explicit Core layout."""
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import xml.etree.ElementTree as ET


LAYOUT_PATH = Path('scripts/integration-program/product-layout.json')
TEMPLATE_PREFIX = '../../../../core/src/'
LEGACY_PREFIX = '../../../../src/'


def core_source_root(checkout):
    manifest = checkout / LAYOUT_PATH
    if manifest.exists() or manifest.is_symlink():
        if manifest.is_symlink() or not manifest.is_file():
            raise ValueError('Bridge target layout manifest must be a regular file')
        layout = json.loads(manifest.read_text(encoding='utf-8'))
        if (not isinstance(layout, dict) or type(layout.get('schemaVersion')) is not int
                or layout['schemaVersion'] != 1 or not isinstance(layout.get('prefixMoves'), list)
                or any(not isinstance(row, list) or len(row) != 2
                       or any(not isinstance(value, str) or not value for value in row)
                       for row in layout['prefixMoves'])
                or len({row[0] for row in layout['prefixMoves']}) != len(layout['prefixMoves'])
                or [row for row in layout['prefixMoves'] if row[0] == 'src/']
                != [['src/', 'core/src/']]):
            raise ValueError('Bridge target layout does not define the reviewed Core source mapping')
        if (checkout / 'src').exists():
            raise ValueError('Bridge target contains both historical and current source roots')
        return 'core/src/'
    if (checkout / 'core/src').exists():
        raise ValueError('Relocated bridge target lacks its layout manifest')
    return 'src/'


def stage_bridge_project(template, tools_root, checkout):
    """Copy fixture bytes, translating only direct Core ProjectReference Include paths."""
    if template.is_symlink() or not template.is_file():
        raise ValueError('Bridge project template must be a regular file')
    relative = template.relative_to(tools_root)
    project = checkout / relative
    if (not project.resolve().is_relative_to(checkout.resolve())
            or any(parent.is_symlink() for parent in project.parents if parent != checkout and checkout in parent.parents)):
        raise ValueError('Bridge fixture staging path is unsafe')
    if (project.parent.resolve().is_relative_to(template.parent.resolve())
            or template.parent.resolve().is_relative_to(project.parent.resolve())):
        raise ValueError('Bridge fixture staging overlaps its template source')
    source_prefix = core_source_root(checkout)
    content = template.read_text(encoding='utf-8')
    references = [node.get('Include') for node in ET.fromstring(content).iter()
                  if node.tag.rsplit('}', 1)[-1] == 'ProjectReference']
    if not references or len(set(references)) != len(references):
        raise ValueError('Bridge fixture must have unique direct Core project references')
    for reference in references:
        if not isinstance(reference, str) or not reference.startswith((TEMPLATE_PREFIX, LEGACY_PREFIX)):
            raise ValueError('Unexpected bridge project-reference root')
        prefix = TEMPLATE_PREFIX if reference.startswith(TEMPLATE_PREFIX) else LEGACY_PREFIX
        suffix = reference[len(prefix):]
        path = PurePosixPath(suffix)
        if path.is_absolute() or '..' in path.parts or path.suffix != '.csproj' or '$' in suffix or '\\' in suffix:
            raise ValueError('Unsafe bridge project-reference path')
        selected = checkout / source_prefix / suffix
        if (not selected.is_file() or selected.is_symlink()
                or not selected.resolve().is_relative_to(checkout.resolve())
                or any(parent.is_symlink() for parent in selected.parents if parent != checkout and checkout in parent.parents)):
            raise ValueError('Bridge target project reference is missing or unsafe')
        original = f'Include="{reference}"'
        if content.count(original) != 1:
            raise ValueError('Bridge project reference does not have a unique literal Include')
        content = content.replace(original, f'Include="../../../../{source_prefix}{suffix}"')
    if project.parent.exists():
        shutil.rmtree(project.parent)
    shutil.copytree(template.parent, project.parent, ignore=shutil.ignore_patterns('bin', 'obj', '.vs'))
    project.write_text(content, encoding='utf-8')
    return project


def project_staging_evidence(template, tools_root, project, checkout):
    return {
        'templatePath': template.relative_to(tools_root).as_posix(),
        'templateSha256': hashlib.sha256(template.read_bytes()).hexdigest(),
        'stagedPath': project.relative_to(checkout).as_posix(),
        'stagedSha256': hashlib.sha256(project.read_bytes()).hexdigest(),
        'coreSourceRoot': core_source_root(checkout),
        'stagingHelperSha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    }
