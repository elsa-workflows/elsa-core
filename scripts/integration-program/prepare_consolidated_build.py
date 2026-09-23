#!/usr/bin/env python3
"""Apply the reviewed build experiment to a clean, disposable history rehearsal.

This writes source/configuration only. It does not run .NET, pack, publish, commit,
add remotes or turn the synthetic rehearsal commit into an import candidate.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import re
import uuid

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('import_rehearsal', HERE / 'rehearse-import.py')
rehearsal = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rehearsal)
SOURCE_COMMITS = {
    'core': '8e893e02c4ac089d526b0a0d294a8546f021d072',
    **rehearsal.PINS,
}
# Reviewed Core integration adds the credential lifecycle/bindings and EF/BPMN CAS fixes.
CURRENT_CORE_COMMIT = '076f022cc174d497af26fc8e26414970e61a79b1'
PATCH = HERE / 'consolidated-build/source-integration.patch'
ADDED_TEST = 'test/extensions/modules/agents/Elsa.Studio.Agents.Tests/Elsa.Studio.Agents.Tests.csproj'


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def solution_with_projects(original, projects):
    """Keep Core's solution entries and add stable GUIDs for imported projects."""
    projects = sorted(set(projects))
    require(len(projects) > 0, 'No imported projects')
    for path in projects:
        require(not PurePosixPath(path).is_absolute() and '..' not in PurePosixPath(path).parts
                and '"' not in path and '\n' not in path and path.endswith('.csproj'), 'Invalid project path')
        require(path.replace('/', '\\') not in original and path not in original, 'Project already in solution')
    match = re.search(r'GlobalSection\(SolutionConfigurationPlatforms\) = preSolution\n(.*?)\tEndGlobalSection', original, re.S)
    require(match is not None, 'Missing solution configurations')
    configurations = [line.strip().split(' = ')[0] for line in match.group(1).splitlines() if line.strip()]
    project_lines, config_lines = [], []
    for path in projects:
        guid = '{' + str(uuid.uuid5(uuid.NAMESPACE_URL, 'elsa-consolidated-build:' + path)).upper() + '}'
        require(guid not in original, 'Solution GUID collision')
        project_lines.append(f'Project("{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}") = "{PurePosixPath(path).stem}", "{path.replace(chr(47), chr(92))}", "{guid}"\nEndProject\n')
        for configuration in configurations:
            build = configuration.split('|')[0] + '|Any CPU'
            for kind in ('ActiveCfg', 'Build.0'):
                config_lines.append(f'\t\t{guid}.{configuration}.{kind} = {build}\n')
    require(original.count('\nGlobal\n') == 1, 'Ambiguous solution global section')
    result = original.replace('\nGlobal\n', '\n' + ''.join(project_lines) + 'Global\n')
    marker = '\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\n'
    require(result.count(marker) == 1, 'Ambiguous project configurations')
    return result.replace(marker, marker + ''.join(config_lines))


def verify_workspace(root):
    require(root.is_dir() and not root.is_symlink(), 'Expected a real disposable repository directory')
    require(Path(rehearsal.git(root, 'rev-parse', '--show-toplevel').decode().strip()).resolve() == root,
            'Use the repository root')
    require(not rehearsal.git(root, 'remote').strip(), 'Disposable rehearsal must have no remotes')
    receipt_path = root / 'import-receipt.json'
    require(receipt_path.is_file() and not receipt_path.is_symlink(), 'Missing regular import receipt')
    receipt = json.loads(receipt_path.read_text())
    source_commits = receipt['sourceCommits']
    supported_pins = (SOURCE_COMMITS, {**SOURCE_COMMITS, 'core': CURRENT_CORE_COMMIT})
    require(source_commits in supported_pins, 'Unsupported source pins; re-review the patch for new source commits')
    head = rehearsal.git(root, 'rev-parse', 'HEAD').decode().strip()
    require(head == receipt['rehearsalCommit'], 'HEAD is not the recorded rehearsal')
    parents = rehearsal.git(root, 'show', '-s', '--format=%P', head).decode().split()
    require(parents == list(source_commits.values()), 'Rehearsal parent set changed')
    source_trees = {k: rehearsal.tree(root, ref) for k, ref in source_commits.items()}
    expected, mapping = rehearsal.relocation_plan(source_trees['core'], {k: v for k, v in source_trees.items() if k != 'core'})
    require(receipt['mapping'] == mapping, 'Receipt mapping differs from pinned source trees')
    require(rehearsal.tree(root, head) == expected, 'Rehearsal tree differs from pinned blob/mode mapping')
    require(not rehearsal.git(root, 'diff', '--name-only', 'HEAD').strip(), 'Workspace/index contains changes; use a fresh rehearsal')
    untracked = set(rehearsal.git(root, 'ls-files', '--others', '-z').decode().split('\0')) - {''}
    require(untracked == {'import-receipt.json'}, 'Unexpected untracked or ignored files; use a fresh rehearsal')
    for ref in source_commits.values():
        rehearsal.git(root, 'merge-base', '--is-ancestor', ref, head)
    return receipt, mapping


def prepare(root):
    require(not Path(root).is_symlink(), 'Expected a real disposable repository directory')
    root = Path(root).resolve()
    receipt, mapping = verify_workspace(root)
    projects = [row['destination'] for row in mapping if row['destination'].endswith('.csproj')]
    projects.append(ADDED_TEST)
    solution = solution_with_projects((root / 'Elsa.sln').read_text(), projects)
    patch = PATCH.read_bytes()
    # git apply checks every hunk before changing any file. It rejects unsafe paths
    # by default; the patch is a reviewed repository artifact, never caller input.
    rehearsal.git(root, 'apply', '--check', '--whitespace=error', '-', data=patch)
    rehearsal.git(root, 'apply', '--whitespace=error', '-', data=patch)
    (root / 'Consolidated.sln').write_text(solution)
    touched = set(rehearsal.git(root, 'diff', '--name-only', 'HEAD').decode().splitlines())
    touched.update(rehearsal.git(root, 'ls-files', '--others', '--exclude-standard').decode().splitlines())
    touched.discard('import-receipt.json')
    report = dict(sourceCommits=receipt['sourceCommits'], rehearsalCommit=receipt['rehearsalCommit'],
                  patchSha256=sha256(patch), importedProjects=len(projects) - 1, addedTests=[ADDED_TEST],
                  buildCompatibilityVerified=False, publicationAuthorized=False,
                  files=[dict(path=path, sha256=sha256((root / path).read_bytes())) for path in sorted(touched)])
    (root / 'consolidated-build-receipt.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k != 'files'}, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--rehearsal', type=Path, required=True)
    args = parser.parse_args()
    prepare(args.rehearsal)


if __name__ == '__main__':
    main()
