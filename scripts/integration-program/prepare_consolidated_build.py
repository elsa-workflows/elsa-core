#!/usr/bin/env python3
"""Prepare the canonical solution in a clean, disposable history rehearsal.

This writes source/configuration and evaluates MSBuild packaging properties. It does
not compile, test, pack, publish, commit, add remotes or turn the synthetic rehearsal
commit into an import candidate.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import tempfile
import uuid

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('import_rehearsal', HERE / 'rehearse-import.py')
rehearsal = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rehearsal)
SOURCE_COMMITS = {
    'core': '8e893e02c4ac089d526b0a0d294a8546f021d072',
    **rehearsal.PINS,
}
# Keep both reviewed Extensions/Studio tips and exact Core commits available for
# existing receipts. The newer Core tip includes merged Secrets and integration
# changes, so its full mapped source must be evaluated again.
PREVIOUS_CURRENT_TIP_SOURCE_COMMITS = {
    'core': '1855a2ef2719d536a66181dec604e781bfdd42a9',
    **rehearsal.CURRENT_TIP_PINS,
}
CURRENT_TIP_SOURCE_COMMITS = {
    'core': 'a13ac7a412e037280d7a568fd9dd87b06ff8b724',
    **rehearsal.CURRENT_TIP_PINS,
}
POST_REGISTRY_SOURCE_COMMITS = {
    'core': 'c4b3ce150160e3c9062b57f7b158fd6b968e1631',
    **rehearsal.CURRENT_TIP_PINS,
}
CURRENT_TIP_SOURCE_PROFILES = (
    PREVIOUS_CURRENT_TIP_SOURCE_COMMITS,
    CURRENT_TIP_SOURCE_COMMITS,
    POST_REGISTRY_SOURCE_COMMITS,
)
# Keep prior reviewed profiles accepted alongside the current-tip rehearsal.
SUPPORTED_CORE_PROFILE_COMMITS = (
    '076f022cc174d497af26fc8e26414970e61a79b1',
    '95a658b96107ad4dbb280a13972479af74bc6a30',
)
PATCH = HERE / 'consolidated-build/source-integration.patch'
ADDED_TEST = 'test/extensions/modules/agents/Elsa.Studio.Agents.Tests/Elsa.Studio.Agents.Tests.csproj'
CONFIGURATIONS = ('Debug', 'Release')
REFERENCE_MODES = {
    'default': None,
    'source': 'true',
    'package': 'false',
}
PACK_PROPERTIES = (
    'TargetFramework', 'TargetFrameworks', 'PackageId', 'IsPackable',
    'GeneratePackageOnBuild', 'UseProjectReferences',
)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def supported_source_profiles():
    return (
        SOURCE_COMMITS,
        *({**SOURCE_COMMITS, 'core': core_commit} for core_commit in SUPPORTED_CORE_PROFILE_COMMITS),
        *CURRENT_TIP_SOURCE_PROFILES,
    )


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
        guid = '{' + str(uuid.uuid5(uuid.NAMESPACE_URL, 'elsa-canonical-build:' + path)).upper() + '}'
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


def parse_frameworks(properties):
    frameworks = properties.get('TargetFrameworks', '').split(';')
    frameworks = [framework.strip() for framework in frameworks if framework.strip()]
    target_framework = properties.get('TargetFramework', '').strip()
    if not frameworks and target_framework:
        frameworks = [target_framework]
    return frameworks or ['']


def msbuild_properties(project, configuration, target_framework, use_project_references):
    command = [
        'dotnet', 'msbuild', str(project),
        '-getProperty:' + ','.join(PACK_PROPERTIES),
        f'-p:Configuration={configuration}',
    ]
    if target_framework:
        command.append(f'-p:TargetFramework={target_framework}')
    if use_project_references is not None:
        command.append(f'-p:UseProjectReferences={use_project_references}')
    result = subprocess.run(command, capture_output=True, text=True, check=False,
                            cwd=project.parent.resolve())
    if result.returncode:
        raise ValueError(
            f"MSBuild property evaluation failed for {project} ({configuration}, "
            f"{target_framework or 'outer build'}, UseProjectReferences={use_project_references}): "
            f"{result.stderr.strip() or result.stdout.strip()}"
        )
    try:
        payload = json.loads(result.stdout)
        properties = payload['Properties']
    except (json.JSONDecodeError, KeyError, TypeError) as error:
        raise ValueError(f'MSBuild returned malformed property JSON for {project}: {result.stdout!r}') from error
    if not isinstance(properties, dict) or any(name not in properties for name in PACK_PROPERTIES):
        raise ValueError(f'MSBuild omitted one or more requested properties for {project}: {properties!r}')
    return properties


def sdk_version(root):
    result = subprocess.run(['dotnet', '--version'], capture_output=True, text=True,
                            check=True, cwd=Path(root).resolve())
    return result.stdout.strip()


def evaluate_packability(root, project_paths, evaluator=msbuild_properties, max_workers=8):
    """Evaluate pack identity and exclusion for every imported project/build dimension."""
    projects = []
    work = []
    for relative in sorted(set(project_paths)):
        path = root / relative
        require(path.is_file() and not path.is_symlink(), f'Missing regular project file: {relative}')
        project = dict(path=relative, evaluations=[])
        projects.append(project)
        for configuration in CONFIGURATIONS:
            for mode, use_project_references in REFERENCE_MODES.items():
                work.append((project, path, configuration, mode, use_project_references, ''))

    def evaluate(item):
        project, path, configuration, mode, use_project_references, target_framework = item
        properties = evaluator(path, configuration, target_framework, use_project_references)
        return item, properties

    def dimension_key(item):
        project, _, configuration, mode, use_project_references, target_framework = item
        return project['path'], configuration, mode, use_project_references, target_framework

    # Frameworks can be conditioned on configuration or reference mode, so query the
    # outer build for each combination before evaluating each inner target framework.
    discovered = {}
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {executor.submit(evaluate, item): item for item in work}
        for future in as_completed(futures):
            item, properties = future.result()
            discovered[dimension_key(item)] = parse_frameworks(properties)

    inner_items = []
    for item in work:
        project, path, configuration, mode, use_project_references, _ = item
        for target_framework in discovered[dimension_key(item)]:
            inner_items.append((project, path, configuration, mode, use_project_references, target_framework))

    violations = []
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {executor.submit(evaluate, item): item for item in inner_items}
        for future in as_completed(futures):
            item, properties = future.result()
            project, _, configuration, mode, _, target_framework = item
            project['evaluations'].append(dict(
                configuration=configuration,
                referenceMode=mode,
                targetFramework=target_framework or None,
                properties={name: properties[name] for name in PACK_PROPERTIES},
            ))
            if properties['IsPackable'].strip().lower() != 'false':
                violations.append(f"{project['path']} ({configuration}, {mode}, {target_framework or 'outer'}): IsPackable={properties['IsPackable']!r}")
            if properties['GeneratePackageOnBuild'].strip().lower() != 'false':
                violations.append(f"{project['path']} ({configuration}, {mode}, {target_framework or 'outer'}): GeneratePackageOnBuild={properties['GeneratePackageOnBuild']!r}")

    for project in projects:
        project['evaluations'].sort(key=lambda row: (
            row['configuration'], row['referenceMode'], row['targetFramework'] or ''
        ))
        package_ids = {row['properties']['PackageId'].strip() for row in project['evaluations']}
        if len(package_ids) != 1 or not next(iter(package_ids), ''):
            violations.append(f"{project['path']}: effective PackageId is missing or varies across the evaluation matrix: {sorted(package_ids)!r}")
    require(not violations, 'Imported project packability is not excluded:\n' + '\n'.join(violations[:30]))
    return dict(
        sdkVersion=sdk_version(root),
        projectCount=len(projects),
        evaluationCount=sum(len(project['evaluations']) for project in projects),
        configurations=list(CONFIGURATIONS),
        referenceModes=REFERENCE_MODES,
        allProjectsNonPackable=True,
        allProjectsDisablePackageOnBuild=True,
        projects=projects,
    )


def verify_workspace(root):
    require(root.is_dir() and not root.is_symlink(), 'Expected a real disposable repository directory')
    require(Path(rehearsal.git(root, 'rev-parse', '--show-toplevel').decode().strip()).resolve() == root,
            'Use the repository root')
    require(not rehearsal.git(root, 'remote').strip(), 'Disposable rehearsal must have no remotes')
    receipt_path = root / 'import-receipt.json'
    require(receipt_path.is_file() and not receipt_path.is_symlink(), 'Missing regular import receipt')
    receipt = json.loads(receipt_path.read_text())
    source_commits = receipt['sourceCommits']
    require(source_commits in supported_source_profiles(),
            'Unsupported source pins; re-review the patch for new source commits')
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


def prepare_in_place(root):
    require(not Path(root).is_symlink(), 'Expected a real disposable repository directory')
    root = Path(root).resolve()
    receipt, mapping = verify_workspace(root)
    mapped_projects = sorted(row['destination'] for row in mapping if row['destination'].endswith('.csproj'))
    require(ADDED_TEST not in mapped_projects, 'Added regression project already appears in the source mapping')
    projects = mapped_projects + [ADDED_TEST]
    require(len(projects) == len(set(projects)), 'Duplicate imported project paths')
    solution = solution_with_projects((root / 'Elsa.sln').read_text(), projects)
    remove_unused_blazored_references(root, receipt['sourceCommits'])
    patch = PATCH.read_bytes()
    # git apply checks every hunk before changing any file. It rejects unsafe paths
    # by default; the patch is a reviewed repository artifact, never caller input.
    rehearsal.git(root, 'apply', '--check', '--whitespace=error', '-', data=patch)
    rehearsal.git(root, 'apply', '--whitespace=error', '-', data=patch)
    (root / 'Elsa.sln').write_text(solution)
    packability = evaluate_packability(root, projects)
    packability_bytes = (json.dumps(packability, indent=2) + '\n').encode()
    (root / 'canonical-packability-report.json').write_bytes(packability_bytes)
    test_tree_projects = sorted(path for path in projects if path.startswith(('test/extensions/', 'test/studio/')))
    nuke_test_projects = sorted(path for path in test_tree_projects if PurePosixPath(path).stem.endswith('Tests'))
    non_test_hosts = sorted(set(test_tree_projects) - set(nuke_test_projects))
    require(non_test_hosts == ['test/extensions/workbench/Elsa.TestServer.Web/Elsa.TestServer.Web.csproj'],
            f'Unexpected project outside Nuke test naming convention: {non_test_hosts}')
    touched = set(rehearsal.git(root, 'diff', '--name-only', 'HEAD').decode().splitlines())
    touched.update(rehearsal.git(root, 'ls-files', '--others', '--exclude-standard').decode().splitlines())
    touched.discard('import-receipt.json')
    report = dict(sourceCommits=receipt['sourceCommits'], rehearsalCommit=receipt['rehearsalCommit'],
                  patchSha256=sha256(patch), canonicalSolution='Elsa.sln',
                  importedProjects=len(mapped_projects), addedTests=[ADDED_TEST],
                  packabilityReport=dict(path='canonical-packability-report.json', sha256=sha256(packability_bytes),
                                         projectCount=packability['projectCount'], evaluationCount=packability['evaluationCount'],
                                         configurations=packability['configurations'], referenceModes=packability['referenceModes']),
                  nukeTestDiscovery=dict(projects=len(test_tree_projects), selectedByNameSuffix=nuke_test_projects,
                                         nonTestHostProjects=non_test_hosts),
                  # Keep the established receipt path and status field for downstream
                  # readers; canonical proof is additive and remains explicitly false.
                  buildCompatibilityVerified=False, canonicalBuildAndTestsVerified=False,
                  publicationAuthorized=False,
                  files=[dict(path=path, sha256=sha256((root / path).read_bytes())) for path in sorted(touched)])
    (root / 'consolidated-build-receipt.json').write_text(json.dumps(report, indent=2) + '\n')
    return report


def remove_unused_blazored_references(root, source_commits):
    """Drop new references made obsolete by the reviewed integration patch.

    The patch replaces Agents' Blazored validator with the local submit validator
    and removes WorkflowContexts' unused Razor import. Secrets is deliberately
    retained under the inert duplicate-source tree, so it needs no active edit.
    """
    if source_commits not in CURRENT_TIP_SOURCE_PROFILES:
        return

    references = (
        'src/extensions/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj',
        'src/extensions/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj',
    )
    pattern = re.compile(
        rb'(?m)^[ \t]*\r?\n[ \t]*<ItemGroup>[ \t]*\r?\n'
        rb'[ \t]*<PackageReference Include="Blazored\.FluentValidation"[ \t]*/>[ \t]*\r?\n'
        rb'[ \t]*</ItemGroup>[ \t]*(?:\r?\n|$)'
    )
    for relative in references:
        path = root / relative
        content = path.read_bytes()
        updated, count = pattern.subn(b'', content)
        require(count == 1, f'Expected one obsolete Blazored.FluentValidation item group in {relative}; found {count}')
        path.write_bytes(updated)


def prepare(root):
    """Build the preparation in an isolated worktree, then apply it if inputs stayed pristine."""
    require(not Path(root).is_symlink(), 'Expected a real disposable repository directory')
    root = Path(root).resolve()
    receipt_path = root / 'import-receipt.json'
    original_receipt_bytes = receipt_path.read_bytes()
    receipt, _ = verify_workspace(root)
    head = receipt['rehearsalCommit']

    with tempfile.TemporaryDirectory(prefix='elsa-canonical-prep-') as temporary_directory:
        staged_root = Path(temporary_directory) / 'rehearsal'
        rehearsal.git(root, 'worktree', 'add', '--quiet', '--detach', str(staged_root), head)
        try:
            (staged_root / 'import-receipt.json').write_bytes(original_receipt_bytes)
            report = prepare_in_place(staged_root)

            staged_receipt = json.loads((staged_root / 'consolidated-build-receipt.json').read_text())
            expected_paths = {entry['path'] for entry in staged_receipt['files']}
            expected_paths.add('consolidated-build-receipt.json')
            changed = set(rehearsal.git(staged_root, 'diff', '--name-only', '-z', 'HEAD')
                          .decode('utf-8', errors='surrogateescape').split('\0')) - {''}
            untracked = set(rehearsal.git(staged_root, 'ls-files', '--others', '--exclude-standard', '-z')
                            .decode('utf-8', errors='surrogateescape').split('\0')) - {'', 'import-receipt.json'}
            require(expected_paths == changed | untracked,
                    'Staged preparation receipt does not describe every generated file')
            for relative in expected_paths:
                path = PurePosixPath(relative)
                require(not path.is_absolute() and '..' not in path.parts,
                        f'Unsafe staged output path: {relative!r}')
                output = staged_root.joinpath(*path.parts)
                current = staged_root
                for part in path.parts:
                    current = current / part
                    require(not current.is_symlink(), f'Refusing staged symlink output: {relative!r}')
                require(output.is_file(), f'Missing staged output file: {relative!r}')

            rehearsal.git(staged_root, 'add', '-N', '--', *sorted(expected_paths))
            prepared_diff = rehearsal.git(staged_root, 'diff', '--binary', '--no-ext-diff', 'HEAD')
            rehearsal.git(root, 'apply', '--check', '--whitespace=error', '-', data=prepared_diff)

            # Long property evaluation happens only in the private worktree. A user
            # change made to the source rehearsal during that work therefore blocks
            # copy-back without being overwritten.
            current_receipt, _ = verify_workspace(root)
            require((root / 'import-receipt.json').read_bytes() == original_receipt_bytes
                    and current_receipt == receipt,
                    'Source receipt changed during preparation; original workspace was left untouched')
            rehearsal.git(root, 'apply', '--whitespace=error', '-', data=prepared_diff)

            for entry in staged_receipt['files']:
                output = root.joinpath(*PurePosixPath(entry['path']).parts)
                require(output.is_file() and not output.is_symlink()
                        and sha256(output.read_bytes()) == entry['sha256'],
                        f'Applied output differs from staged receipt: {entry["path"]!r}')
            require((root / 'consolidated-build-receipt.json').read_bytes()
                    == (staged_root / 'consolidated-build-receipt.json').read_bytes(),
                    'Applied preparation receipt differs from staged receipt')
            print(json.dumps({k: v for k, v in report.items() if k != 'files'}, indent=2))
        finally:
            rehearsal.git(root, 'worktree', 'remove', '--force', str(staged_root))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--rehearsal', type=Path, required=True)
    args = parser.parse_args()
    prepare(args.rehearsal)


if __name__ == '__main__':
    main()
