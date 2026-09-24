#!/usr/bin/env python3
"""Prepare, but never launch, an isolated Workbench Secrets runtime fixture."""
import argparse
import base64
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import secrets
import shlex
import shutil
import socket
import subprocess
import tempfile
import uuid


HERE = Path(__file__).resolve().parent
PATCH = HERE / 'consolidated-build' / 'workbench-canonical-secrets.patch'
PATCH_RELATIVE = Path('scripts/integration-program/consolidated-build/workbench-canonical-secrets.patch')
SOURCE_PATCH = HERE / 'consolidated-build' / 'source-integration.patch'
SUPPLEMENTAL_PATCHES = (
    HERE / 'consolidated-build' / 'dapper-atomic-updates.patch',
    HERE / 'consolidated-build' / 'mongo-atomic-updates.patch',
    HERE / 'consolidated-build' / 'studio-test-layout.patch'
)
OPTIONAL_FIXTURE_PATCHES = (
    HERE / 'consolidated-build' / 'studio-secrets-menu.patch',
    HERE / 'consolidated-build' / 'studio-bpmn-generator-layout.patch',
    HERE / 'consolidated-build' / 'workbench-two-tenant-multitenancy.patch'
)
SOURCE_PROJECT = Path('samples/extensions/workbench/Elsa.Server.Web')
REQUIRED_PROGRAM_MARKERS = (
    'var useSecrets = configuration.GetValue("Features:Secrets:Enabled", false);',
    '.UseSecrets(secrets =>',
    '.UseSecretsJavaScript();'
)
TWO_TENANT_MULTITENANCY_MARKER = 'var useMultitenancy = configuration.GetValue("Features:Multitenancy:Enabled", false);'
FORBIDDEN_PROGRAM_MARKERS = (
    'UseSecretsManagement(',
    'UpdateExpiredSecretsRecurringTask'
)
TENANT_ID = ''
ADMIN_ROLE_ID = 'workbench-synthetic-admin'
PBKDF2_ITERATIONS = 600_000
CANONICAL_WORKBENCH_BASE_PATCH_SHA256 = '5dd4667190c538279f3b89cea80b81681402775555a89d89248001c2e22aa446'
REQUIRED_SECRETS_ASSEMBLIES = (
    'Elsa.Secrets',
    'Elsa.Secrets.JavaScript',
    'Elsa.Secrets.Persistence.EFCore',
    'Elsa.Secrets.Persistence.EFCore.PostgreSql',
    'Elsa.Secrets.Persistence.EFCore.Sqlite',
    'Elsa.Secrets.Persistence.EFCore.SqlServer'
)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def is_within(path, parent):
    try:
        path.relative_to(parent)
        return True
    except ValueError:
        return False


def has_path_overlap(left, right):
    return is_within(left, right) or is_within(right, left)


def file_sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def git_text(root, *args):
    return subprocess.run(
        ['git', '-C', str(root), *args], check=True, capture_output=True, text=True).stdout.strip()


def read_regular_json(path):
    require(path.is_file() and not path.is_symlink(), f'Missing regular JSON receipt: {path.name}')
    return json.loads(path.read_text())


def get_compile_items(source):
    project = source / 'Elsa.Server.Web.csproj'
    command = ['dotnet', 'msbuild', str(project), '-getItem:Compile', '-nologo']
    result = subprocess.run(command, cwd=source, capture_output=True, text=True, check=False)
    require(result.returncode == 0, f'MSBuild Compile item evaluation failed: {result.stderr.strip()}')
    json_start = result.stdout.find('{')
    require(json_start >= 0, 'MSBuild did not return Compile item JSON')
    try:
        payload, _ = json.JSONDecoder().raw_decode(result.stdout[json_start:])
    except json.JSONDecodeError as error:
        raise ValueError('MSBuild returned invalid Compile item JSON') from error

    items = payload.get('Items', {}).get('Compile')
    require(isinstance(items, list), 'MSBuild Compile items are missing')
    return command, items


def get_patch_targets(patch):
    targets = []
    for line in patch.read_text().splitlines():
        if not line.startswith('diff --git '):
            continue
        paths = line.removeprefix('diff --git ').split(' ')
        require(len(paths) == 2 and paths[0].startswith('a/') and paths[1].startswith('b/'),
                f'Unsupported patch path header in {patch.name}')
        source_path = Path(paths[0][2:])
        target_path = Path(paths[1][2:])
        require(source_path == target_path, f'Renames are not supported in {patch.name}')
        require(not source_path.is_absolute() and '..' not in source_path.parts,
                f'Patch target escapes the mapped rehearsal in {patch.name}')
        targets.append(target_path.as_posix())

    require(bool(targets), f'Patch has no file targets: {patch.name}')
    require(len(targets) == len(set(targets)), f'Patch contains duplicate targets: {patch.name}')
    return targets


def hash_optional_file(path):
    if not path.exists():
        return None
    require(path.is_file() and not path.is_symlink(), f'Patch target is not a regular file: {path}')
    return file_sha256(path)


def is_patch_applied(root, patch, targets):
    with tempfile.TemporaryDirectory(prefix='fixture-patch-detect-') as directory:
        patch_root = Path(directory)
        for relative in targets:
            source_file = root / relative
            if source_file.exists():
                require(source_file.is_file() and not source_file.is_symlink(),
                        f'Patch target is not a regular file: {relative}')
                target = patch_root / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source_file, target)

        applied = []
        for relative in targets:
            result = subprocess.run(
                ['git', 'apply', '--reverse', '--check', f'--include={relative}', str(patch)],
                cwd=patch_root, check=False, capture_output=True)
            applied.append(result.returncode == 0)

    require(not any(applied) or all(applied),
            f'Only part of optional fixture patch is present: {patch.name}')
    return all(applied)


def display_patch_path(patch):
    try:
        return str(patch.relative_to(HERE))
    except ValueError:
        return patch.name


def git_blob_bytes(root, revision_path):
    result = subprocess.run(
        ['git', '-C', str(root), 'show', revision_path], check=False, capture_output=True)
    if result.returncode:
        return None
    return result.stdout


def verify_patch_chain(root, prepared_files, expected_pins, actual_files):
    prepared_hashes = {item['path']: item['sha256'] for item in prepared_files}
    workbench_targets = get_patch_targets(PATCH)
    supplemental = []
    supplemental_targets = set()
    for patch in SUPPLEMENTAL_PATCHES:
        require(patch.is_file() and not patch.is_symlink(), f'Missing regular supplemental patch: {patch.name}')
        targets = get_patch_targets(patch)
        require(not (supplemental_targets & set(targets)),
                f'Supplemental patch targets overlap; explicit ordering needs review: {patch.name}')
        supplemental_targets.update(targets)
        supplemental.append((patch, targets))

    applied_supplementals = bool(supplemental_targets & actual_files)
    if applied_supplementals:
        require(supplemental_targets.issubset(actual_files),
                f'Only part of the supplemental patch set is present: {sorted(supplemental_targets - actual_files)}')
    else:
        supplemental = []
        supplemental_targets = set()

    optional_supplementals = []
    optional_targets = set()
    for patch in OPTIONAL_FIXTURE_PATCHES:
        if not patch.exists() and not patch.is_symlink():
            continue
        require(patch.is_file() and not patch.is_symlink(),
                f'Missing regular optional fixture patch: {patch.name}')
        targets = get_patch_targets(patch)
        if is_patch_applied(root, patch, targets):
            require(not (optional_targets & set(targets))
                    and not (supplemental_targets & set(targets)),
                    f'Applied optional fixture patch targets overlap: {patch.name}')
            optional_supplementals.append((patch, targets))
            optional_targets.update(targets)

    all_targets = set(workbench_targets) | supplemental_targets | optional_targets
    expected_files = set(prepared_hashes) | all_targets
    require(actual_files == expected_files,
            f'Mapped rehearsal file set differs from the reviewed patch chain: '
            f'extra={sorted(actual_files - expected_files)}, missing={sorted(expected_files - actual_files)}')

    with tempfile.TemporaryDirectory(prefix='workbench-patch-ledger-') as directory:
        patch_root = Path(directory)
        for relative in all_targets:
            source_file = root / relative
            if source_file.exists():
                require(source_file.is_file() and not source_file.is_symlink(),
                        f'Patch target is not a regular file: {relative}')
                target = patch_root / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source_file, target)

        # The reviewed chain applies the Workbench opt-in first, then the supplemental
        # patches. Reverse it in the opposite order so overlays of a patched file are
        # removed before the base Workbench patch is reversed.
        ordered_patches = [*reversed(optional_supplementals), *reversed(supplemental), (PATCH, workbench_targets)]
        ledger_entries = []
        for patch, targets in ordered_patches:
            preimages = {path: hash_optional_file(patch_root / path) for path in targets}
            include_args = [f'--include={path}' for path in targets]
            try:
                subprocess.run(
                    ['git', 'apply', '--reverse', '--check', *include_args, str(patch)],
                    cwd=patch_root, check=True, capture_output=True)
                subprocess.run(
                    ['git', 'apply', '--reverse', *include_args, str(patch)],
                    cwd=patch_root, check=True, capture_output=True)
            except subprocess.CalledProcessError as error:
                raise ValueError(f'Mapped rehearsal does not match patch {patch.name}') from error

            postimages = {path: hash_optional_file(patch_root / path) for path in targets}
            ledger_entries.append({
                'patch': display_patch_path(patch),
                'sha256': file_sha256(patch),
                'targets': [
                    {'path': path, 'beforeReverseSha256': preimages[path], 'afterReverseSha256': postimages[path]}
                    for path in targets
                ]
            })

        baseline_hashes = dict(prepared_hashes)
        program_path = (SOURCE_PROJECT / 'Program.cs').as_posix()
        if program_path not in baseline_hashes:
            extension_program = git_blob_bytes(
                root,
                f"{expected_pins['extensions']}:src/workbench/Elsa.Server.Web/Program.cs")
            require(extension_program is not None,
                    'Pinned Extensions Workbench Program blob is not reachable in the mapped Git repository')
            baseline_hashes[program_path] = hashlib.sha256(extension_program).hexdigest()

        for relative in all_targets:
            expected_hash = baseline_hashes.get(relative)
            if expected_hash is None:
                head_blob = git_blob_bytes(root, f'HEAD:{relative}')
                if head_blob is not None:
                    expected_hash = hashlib.sha256(head_blob).hexdigest()
            actual_hash = hash_optional_file(patch_root / relative)
            require(actual_hash == expected_hash,
                    f'Reversing patch chain does not restore the expected base for {relative}')

        previous_patch = root / PATCH_RELATIVE
        require(get_patch_targets(previous_patch) == workbench_targets,
                'Previous Workbench patch targets differ from the current patch')
        require(file_sha256(previous_patch) != file_sha256(PATCH),
                'Previous and current Workbench patches must be distinct')
        transition_args = [
            (str(previous_patch),),
            ('--reverse', str(previous_patch)),
            (str(PATCH),),
            *((str(patch),) for patch, _ in supplemental),
            *((str(patch),) for patch, _ in optional_supplementals)
        ]
        for args in transition_args:
            try:
                subprocess.run(['git', 'apply', '--check', *args], cwd=patch_root,
                               check=True, capture_output=True)
                subprocess.run(['git', 'apply', *args], cwd=patch_root,
                               check=True, capture_output=True)
            except subprocess.CalledProcessError as error:
                raise ValueError('Pinned Workbench patch transition cannot be replayed') from error
        for relative in all_targets:
            require(hash_optional_file(patch_root / relative) == hash_optional_file(root / relative),
                    f'Patch transition does not restore mapped source for {relative}')

    for relative, expected_hash in prepared_hashes.items():
        if relative not in all_targets:
            require(file_sha256(root / relative) == expected_hash,
                    f'Mapped prepared source changed outside the reviewed patch chain: {relative}')

    return {
        'verified': True,
        'baseReceiptFileCount': len(prepared_files),
        'workbenchPatch': {
            'path': display_patch_path(PATCH),
            'sha256': file_sha256(PATCH),
            'targets': workbench_targets
        },
        'previousWorkbenchPatchSha256': file_sha256(root / PATCH_RELATIVE),
        'supplementalPatches': [
            {
                'path': display_patch_path(patch),
                'sha256': file_sha256(patch),
                'targets': targets
            }
            for patch, targets in supplemental
        ],
        'optionalFixturePatches': [
            {
                'path': display_patch_path(patch),
                'sha256': file_sha256(patch),
                'targets': targets
            }
            for patch, targets in optional_supplementals
        ],
        'reverseReplay': ledger_entries,
        'previousToCurrentTransitionVerified': True
    }


def inventory_workbench_sources(source):
    project = (source / 'Elsa.Server.Web.csproj').read_text()
    require('<EnableDefaultCompileItems>false' not in project, 'Workbench disables SDK default compile items')
    require('<Compile ' not in project, 'Workbench project has explicit Compile items requiring manual inventory')

    files = []
    workflow_declarations = []
    for path in sorted(source.rglob('*.cs')):
        if {'bin', 'obj'} & set(path.relative_to(source).parts):
            continue
        require(not path.is_symlink(), f'Workbench source file cannot be a symlink: {path.relative_to(source)}')
        relative = path.relative_to(source).as_posix()
        content = path.read_text()
        files.append({'path': relative, 'sha256': file_sha256(path)})
        if 'IWorkflow' in content or 'WorkflowBase' in content:
            workflow_declarations.append(relative)

    compile_command, compile_items = get_compile_items(source)
    compile_files = []
    compile_files_outside_project = []
    for item in compile_items:
        full_path = Path(item['FullPath']).resolve(strict=False)
        if {'bin', 'obj'} & set(full_path.parts):
            continue
        if not is_within(full_path, source.resolve()):
            compile_files_outside_project.append(str(full_path))
            continue
        compile_files.append(full_path.relative_to(source.resolve()).as_posix())

    require(not compile_files_outside_project,
            f'Workbench compile items include source outside the host project: {compile_files_outside_project}')
    scanned_files = sorted(item['path'] for item in files)
    require(sorted(compile_files) == scanned_files,
            f'MSBuild Compile items differ from the Workbench source inventory: '
            f'compile-only={sorted(set(compile_files) - set(scanned_files))}, '
            f'scan-only={sorted(set(scanned_files) - set(compile_files))}')

    program = (source / 'Program.cs').read_text()
    require('.AddActivitiesFrom<Program>()' in program and '.AddWorkflowsFrom<Program>()' in program,
            'Workbench assembly-discovery registrations changed; review startup side effects')
    require(not workflow_declarations,
            f'Workbench assembly contains workflow declarations requiring manual review: {workflow_declarations}')
    return {
        'compileMode': 'SDK default compile glob; no explicit Compile items',
        'compileItemsCommand': compile_command,
        'compiledSourceFileCount': len(compile_files),
        'compiledSourceFiles': sorted(compile_files),
        'sourceFileCount': len(files),
        'sourceFiles': files,
        'workflowDeclarationFiles': workflow_declarations,
        'discoveryScope': 'AddWorkflowsFrom<Program> scans exported IWorkflow types in the Workbench host assembly only.'
    }


def validate_source_root(rehearsal_root, expected_pins):
    root = Path(rehearsal_root).expanduser()
    require(not root.is_symlink(), 'Mapped rehearsal root cannot be a symlink')
    root = root.resolve(strict=True)
    require(root.is_dir(), 'Mapped rehearsal root must be a directory')
    require(Path(git_text(root, 'rev-parse', '--show-toplevel')).resolve() == root,
            'Pass the physical mapped rehearsal Git root')
    require(not git_text(root, 'remote'), 'Mapped rehearsal must not have Git remotes')

    import_receipt = read_regular_json(root / 'import-receipt.json')
    build_receipt = read_regular_json(root / 'consolidated-build-receipt.json')
    pins = {'core': expected_pins['core'], 'extensions': expected_pins['extensions'], 'studio': expected_pins['studio']}
    require(import_receipt.get('sourceCommits') == pins, 'Import receipt source pins do not match the requested pins')
    require(build_receipt.get('sourceCommits') == pins, 'Build receipt source pins differ from the import receipt')
    require(import_receipt.get('exactBlobAndModeMapping') is True and import_receipt.get('originalHistoriesReachable') is True,
            'Import receipt does not prove exact source history mapping')
    require(import_receipt.get('buildCompatibilityVerified') is False and import_receipt.get('publicationAuthorized') is False,
            'Import receipt has unsupported build or publication claims')
    require(build_receipt.get('buildCompatibilityVerified') is False and build_receipt.get('publicationAuthorized') is False,
            'Build receipt has unsupported compatibility or publication claims')
    rehearsal_commit = import_receipt.get('rehearsalCommit')
    require(bool(rehearsal_commit) and build_receipt.get('rehearsalCommit') == rehearsal_commit,
            'Import and build receipts identify different mapped source trees')
    require(git_text(root, 'rev-parse', 'HEAD') == rehearsal_commit, 'Mapped rehearsal HEAD differs from its source receipt')
    require(build_receipt.get('patchSha256') == file_sha256(SOURCE_PATCH),
            'Build receipt does not match the current source integration patch')
    require(PATCH.is_file() and not PATCH.is_symlink(), 'Missing regular canonical Workbench integration patch')
    previous_patch = root / PATCH_RELATIVE
    require(previous_patch.is_file() and not previous_patch.is_symlink(),
            'Mapped rehearsal is missing its previous canonical Workbench patch artifact')
    require(file_sha256(previous_patch) == CANONICAL_WORKBENCH_BASE_PATCH_SHA256,
            'Mapped rehearsal Workbench patch artifact is not the reviewed baseline')

    prepared_file_items = build_receipt.get('files', [])
    prepared_files = {item['path']: item['sha256'] for item in prepared_file_items}
    require(len(prepared_files) == len(prepared_file_items), 'Build receipt contains duplicate file entries')
    for relative in prepared_files:
        path = Path(relative)
        require(not path.is_absolute() and '..' not in path.parts,
                f'Build receipt contains an unsafe relative path: {relative}')

    changed = set(git_text(root, 'diff', '--name-only', 'HEAD').splitlines())
    untracked = set(git_text(root, 'ls-files', '--others', '--exclude-standard').splitlines())
    actual_files = changed | untracked
    actual_files.discard('import-receipt.json')
    actual_files.discard('consolidated-build-receipt.json')

    require((SOURCE_PROJECT / 'Elsa.Server.Web.csproj').as_posix() in prepared_files,
            'Build receipt omits the Workbench project file')
    for relative, expected_hash in prepared_files.items():
        file_path = root / relative
        require(file_path.is_file() and not file_path.is_symlink(), f'Mapped source file is missing or unsafe: {relative}')
    patch_chain = verify_patch_chain(root, prepared_file_items, pins, actual_files)

    source = (root / SOURCE_PROJECT).resolve(strict=True)
    require(is_within(source, root), 'Workbench project root is outside the mapped rehearsal')
    for name in ('Elsa.Server.Web.csproj', 'Program.cs', 'appsettings.json'):
        file_path = source / name
        require(file_path.is_file() and not file_path.is_symlink(), f'Missing regular Workbench file: {name}')

    program = (source / 'Program.cs').read_text()
    for marker in REQUIRED_PROGRAM_MARKERS:
        require(marker in program, f'Workbench source is missing reviewed Secrets marker: {marker}')
    for marker in FORBIDDEN_PROGRAM_MARKERS:
        require(marker not in program, f'Workbench source still contains obsolete Secrets registration: {marker}')

    source_inventory = inventory_workbench_sources(source)
    return root, source, program, import_receipt, build_receipt, source_inventory, patch_chain


def build_host(source, log_parent):
    project = source / 'Elsa.Server.Web.csproj'
    restore_command = [
        'dotnet', 'restore', str(project), '--force-evaluate', '--verbosity', 'minimal'
    ]
    restore_result = subprocess.run(restore_command, cwd=source, capture_output=True, text=True, check=False)
    restore_log = restore_result.stdout + restore_result.stderr

    command = [
        'dotnet', 'build', str(source / 'Elsa.Server.Web.csproj'),
        '--configuration', 'Debug', '--framework', 'net10.0', '--no-restore', '--no-incremental',
        '--verbosity', 'minimal'
    ]
    started_at = datetime.now(timezone.utc)
    if restore_result.returncode:
        failure_stage = 'restore'
        failure_code = restore_result.returncode
        build_log = restore_log
    else:
        result = subprocess.run(command, cwd=source, capture_output=True, text=True, check=False)
        build_log = restore_log + result.stdout + result.stderr
        failure_stage = 'build' if result.returncode else None
        failure_code = result.returncode

    if failure_stage:
        descriptor, log_name = tempfile.mkstemp(prefix='workbench-build-failure-', suffix='.log', dir=log_parent)
        with os.fdopen(descriptor, 'w') as log_file:
            log_file.write(build_log)
        raise ValueError(
            f'Clone-local Workbench {failure_stage} failed with exit code {failure_code}; log preserved at {log_name}')

    host_dll = source / 'bin' / 'Debug' / 'net10.0' / 'Elsa.Server.Web.dll'
    require(host_dll.is_file() and not host_dll.is_symlink(), 'Fresh build did not produce a regular Workbench host DLL')
    assets_path = source / 'obj' / 'project.assets.json'
    require(assets_path.is_file() and not assets_path.is_symlink(), 'Clone-local restore did not produce project assets')
    assets = json.loads(assets_path.read_text())
    restored_project_path = Path(assets.get('project', {}).get('restore', {}).get('projectPath', '')).resolve(strict=False)
    require(restored_project_path == project.resolve(strict=True),
            'Workbench project assets do not point at the isolated runtime clone')
    clone_root = source.parents[3]
    secret_assemblies = []
    for name in REQUIRED_SECRETS_ASSEMBLIES:
        project_output = clone_root / 'src' / 'modules' / name / 'bin' / 'Debug' / 'net10.0' / f'{name}.dll'
        host_output = host_dll.parent / f'{name}.dll'
        require(project_output.is_file() and not project_output.is_symlink(),
                f'Full graph build omitted a regular Secrets project assembly: {name}')
        require(host_output.is_file() and not host_output.is_symlink(),
                f'Full graph build omitted a regular host Secrets assembly: {name}')
        digest = file_sha256(project_output)
        require(file_sha256(host_output) == digest,
                f'Host Secrets assembly differs from the built project output: {name}')
        secret_assemblies.append({'name': name, 'sha256': digest,
                                  'projectOutput': str(project_output), 'hostOutput': str(host_output)})
    return {
        'restoreCommand': restore_command,
        'restoreExitCode': restore_result.returncode,
        'restoreLogSha256': hashlib.sha256(restore_log.encode()).hexdigest(),
        'command': command,
        'startedAtUtc': started_at.isoformat(),
        'finishedAtUtc': datetime.now(timezone.utc).isoformat(),
        'exitCode': result.returncode,
        'hostDll': str(host_dll.resolve(strict=True)),
        'hostDllSha256': file_sha256(host_dll),
        'hostDllSizeBytes': host_dll.stat().st_size,
        'secretsAssemblies': secret_assemblies,
        'log': build_log
    }


def validate_temp_parent(path, source):
    parent = Path(path).expanduser()
    require(not parent.is_symlink(), 'Fixture parent cannot be a symlink')
    parent = parent.resolve(strict=True)
    temp_root = Path(tempfile.gettempdir()).resolve(strict=True)
    require(parent.is_dir() and is_within(parent, temp_root), 'Fixture parent must be under the system temporary directory')
    require(not is_within(parent, source), 'Fixture parent cannot be inside the Workbench project root')
    return parent


def choose_loopback_port():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
        listener.bind(('127.0.0.1', 0))
        return listener.getsockname()[1]


def hash_password(password, salt):
    digest = hashlib.pbkdf2_hmac('sha256', password.encode('utf-8'), salt, PBKDF2_ITERATIONS, dklen=32)
    envelope = b'pbkdf2-sha256$' + str(PBKDF2_ITERATIONS).encode('ascii') + b'$' + base64.b64encode(digest)
    return base64.b64encode(envelope).decode('ascii')


def write_private(path, content):
    flags = os.O_WRONLY | os.O_CREAT | os.O_EXCL
    descriptor = os.open(path, flags, 0o600)
    with os.fdopen(descriptor, 'wb') as file:
        file.write(content)


def prepare_fixture(rehearsal_root, core_sha, extensions_sha, studio_sha, temp_parent=None, two_tenant=False):
    pins = {'core': core_sha, 'extensions': extensions_sha, 'studio': studio_sha}
    for name, commit in pins.items():
        require(len(commit) == 40 and all(character in '0123456789abcdef' for character in commit),
                f'{name} source pin must be a full lowercase commit SHA')

    root, source, _, import_receipt, build_receipt, source_inventory, patch_chain = validate_source_root(rehearsal_root, pins)
    program = (source / 'Program.cs').read_text()
    optional_paths = {Path(item['path']).name for item in patch_chain['optionalFixturePatches']}
    tenant_patch_name = 'workbench-two-tenant-multitenancy.patch'
    menu_patch_name = 'studio-secrets-menu.patch'
    studio_layout_patch_name = 'studio-bpmn-generator-layout.patch'
    if two_tenant:
        require(tenant_patch_name in optional_paths,
                'Two-tenant mode requires the reviewed Workbench multitenancy patch in the isolated mapped source')
        require(menu_patch_name in optional_paths,
                'Two-tenant mode requires the merged Studio Secrets menu patch in the mapped source')
        require(studio_layout_patch_name in optional_paths,
                'Two-tenant mode requires the Studio mapped-layout patch in the isolated source')
        require(TWO_TENANT_MULTITENANCY_MARKER in program,
                'Two-tenant mode requires configuration-gated Workbench multitenancy')

    parent = validate_temp_parent(temp_parent or tempfile.gettempdir(), source)
    build = build_host(source, parent)
    fixture_root = Path(tempfile.mkdtemp(prefix='elsa-workbench-secrets-', dir=parent)).resolve(strict=True)
    require(not has_path_overlap(fixture_root, source), 'Generated fixture root overlaps the Workbench project root')
    content_root = fixture_root / 'content-root'
    content_app_data = content_root / 'App_Data'
    working_app_data = fixture_root / 'App_Data'
    drop_ins = working_app_data / 'DropIns'
    locks = working_app_data / 'locks'
    home = fixture_root / 'home'
    temporary = fixture_root / 'tmp'
    for directory in (content_root, content_app_data, working_app_data, drop_ins, locks, home, temporary):
        directory.mkdir(mode=0o700, parents=True, exist_ok=True)

    database_path = (content_app_data / 'workbench.sqlite').resolve()
    database_url = f'http://127.0.0.1:{choose_loopback_port()}'
    workbench_port = database_url.rsplit(':', 1)[1]
    login_name = 'synthetic-admin'
    login_password = secrets.token_urlsafe(32)
    signing_key = base64.b64encode(secrets.token_bytes(64)).decode('ascii')
    encryption_key = list(secrets.token_bytes(32))
    users = []
    roles = []
    credential_lines = ['Local-only synthetic Workbench credentials. Delete this fixture after both hosts are stopped.']
    if two_tenant:
        tenant_ids = ('tenant-a', 'tenant-b')
        for tenant_id in tenant_ids:
            username = f'synthetic-{tenant_id}-admin'
            password = secrets.token_urlsafe(32)
            password_salt = secrets.token_bytes(32)
            role_id = f'workbench-{tenant_id}-admin'
            users.append({
                'Id': uuid.uuid4().hex,
                'Name': username,
                'HashedPassword': hash_password(password, password_salt),
                'HashedPasswordSalt': base64.b64encode(password_salt).decode('ascii'),
                'Roles': [role_id],
                'TenantId': tenant_id
            })
            roles.append({
                'Id': role_id,
                'Name': f'Synthetic {tenant_id} Administrator',
                'Permissions': ['*'],
                'TenantId': tenant_id
            })
            credential_lines.extend(['', f'Tenant: {tenant_id}', f'Username: {username}', f'Password: {password}'])
        tenants = [{
            'Id': tenant_id,
            'Name': f'Synthetic {tenant_id}',
            'Configuration': {
                'Http': {
                    'Prefix': '',
                    'Host': f'{"127.0.0.1" if tenant_id == "tenant-a" else "tenant-b.localhost"}:{workbench_port}'
                },
                'ConnectionStrings': {'Sqlite': f'Data Source={database_path};Cache=Shared;'}
            }
        } for tenant_id in tenant_ids]
        denied_password = secrets.token_urlsafe(32)
        denied_salt = secrets.token_bytes(32)
        users.append({
            'Id': uuid.uuid4().hex,
            'Name': 'synthetic-denied',
            'HashedPassword': hash_password(denied_password, denied_salt),
            'HashedPasswordSalt': base64.b64encode(denied_salt).decode('ascii'),
            'Roles': [],
            'TenantId': 'tenant-a'
        })
        credential_lines.extend(['', 'Tenant: tenant-a', 'Username: synthetic-denied',
                                 f'Password: {denied_password}', 'Permissions: none'])
    else:
        password_salt = secrets.token_bytes(32)
        users.append({
            'Id': uuid.uuid4().hex,
            'Name': login_name,
            'HashedPassword': hash_password(login_password, password_salt),
            'HashedPasswordSalt': base64.b64encode(password_salt).decode('ascii'),
            'Roles': [ADMIN_ROLE_ID],
            'TenantId': TENANT_ID
        })
        roles.append({
            'Id': ADMIN_ROLE_ID,
            'Name': 'Synthetic Workbench Administrator',
            'Permissions': ['*'],
            'TenantId': TENANT_ID
        })
        credential_lines.extend(['', f'Username: {login_name}', f'Password: {login_password}'])
    configuration = {
        'AllowedHosts': '127.0.0.1;localhost' + (';tenant-b.localhost' if two_tenant else ''),
        'ConnectionStrings': {
            'Sqlite': f'Data Source={database_path};Cache=Shared;'
        },
        'DatabaseProvider': 'Sqlite',
        'Identity': {
            'Tokens': {
                'SigningKey': signing_key,
                'AccessTokenLifetime': '00:05:00',
                'RefreshTokenLifetime': '00:15:00'
            },
            'Roles': roles,
            'Users': users,
            'Applications': []
        },
        'AppRole': 'Default',
        'Http': {
            'BaseUrl': database_url,
            'BasePath': '/workflows',
            'ApiRoutePrefix': 'elsa/api',
            'AvailableContentTypes': ['application/json']
        },
        'Runtime': {
            'DistributedLocking': {
                'Provider': 'File',
                'LockAcquisitionTimeout': '00:00:10'
            },
            'DistributedLockProvider': 'File',
            'WorkflowDispatcher': {
                'Channels': [{'Name': name} for name in ('Low', 'Medium', 'High')]
            }
        },
        'Secrets': {'EncryptionKey': encryption_key},
        'Webhooks': {'Sinks': []},
        'Smtp': {},
        'Mqtt': {'Host': '127.0.0.1', 'Port': 1}
    }
    if two_tenant:
        configuration['Features'] = {'Multitenancy': {'Enabled': True}}
        configuration['Multitenancy'] = {'Tenants': tenants}

    appsettings_bytes = (json.dumps(configuration, indent=2) + '\n').encode('utf-8')
    write_private(content_root / 'appsettings.json', appsettings_bytes)
    write_private(content_root / 'appsettings.Production.json', b'{}\n')
    marker = {
        'schemaVersion': 1,
        'fixtureId': uuid.uuid4().hex,
        'fixtureRoot': str(fixture_root),
        'sourceProjectRoot': str(source)
    }
    write_private(fixture_root / '.elsa-workbench-fixture.json', (json.dumps(marker, indent=2) + '\n').encode('utf-8'))
    write_private(
        fixture_root / 'synthetic-credentials.txt',
        ('\n'.join(credential_lines) + '\n').encode('utf-8'))
    write_private(fixture_root / 'host-build.log', build['log'].encode('utf-8'))

    patch_sha = file_sha256(PATCH)
    plan = {
        'fixtureRoot': str(fixture_root),
        'contentRoot': str(content_root),
        'processWorkingDirectory': str(fixture_root),
        'sourceProjectRoot': str(source),
        'sourcePins': pins,
        'rehearsalCommit': import_receipt['rehearsalCommit'],
        'sourceIntegrationPatchSha256': build_receipt['patchSha256'],
        'workbenchPatchSha256': patch_sha,
        'previousWorkbenchPatchSha256': patch_chain['previousWorkbenchPatchSha256'],
        'patchTransition': 'reverse the verified previous Workbench patch, then apply the current opt-in patch in this isolated source clone',
        'sourcePatchChain': patch_chain,
        'mappedProgramSha256': file_sha256(source / 'Program.cs'),
        'mappedProjectSha256': file_sha256(source / 'Elsa.Server.Web.csproj'),
        'hostAssemblySourceInventory': source_inventory,
        'freshBuild': {key: value for key, value in build.items() if key != 'log'},
        'databasePath': str(database_path),
        'privateAppsettingsSha256': hashlib.sha256(appsettings_bytes).hexdigest(),
        'loopbackUrl': database_url,
        'secretsEnabledOnlyByExplicitLaunchOverride': '--Features:Secrets:Enabled=true',
        'appsettingsContainsSecretsEnabled': False,
        'configuredWebhookSinkCount': 0,
        'dropInsDirectory': str(drop_ins),
        'lockDirectory': str(locks),
        'freshSqliteDatabase': True,
        'workflowRowsExpectedAtFirstStart': 0,
        'launchApproved': False,
        'twoTenantMode': two_tenant,
        'multitenancyEnabledOnlyInFixtureConfiguration': two_tenant,
        'tenantIsolationPolicy': ({
            'multitenancyEnabledByPrivateConfiguration': True,
            'selection': 'Tenant-specific request hosts select the tenant before login; matching ElsaIdentity TenantId claims resolve subsequent authenticated requests.',
            'tenants': ['tenant-a', 'tenant-b'],
            'databasePathShared': True,
            'rolePermissions': 'Each synthetic administrator role is scoped to its matching tenant.'
        } if two_tenant else None),
        'hostIdentityPolicy': ('Two configuration-backed synthetic ElsaIdentity administrators are scoped to tenant-a and tenant-b; both tenants share the fresh fixture database.' if two_tenant else 'Synthetic administrator and role use the empty default tenant ID. Workbench multitenancy remains disabled by its source constant.')
    }
    write_private(fixture_root / 'launch-plan.json', (json.dumps(plan, indent=2) + '\n').encode('utf-8'))

    host_dll = Path(build['hostDll'])
    launch_args = [
        'dotnet', str(host_dll),
        '--contentRoot', str(content_root),
        '--urls', database_url,
        '--Features:Secrets:Enabled=true'
    ]
    shell_env = [
        'env -i',
        f'PATH={shlex.quote(os.environ.get("PATH", "/usr/bin:/bin"))}',
        f'HOME={shlex.quote(str(home))}',
        'ASPNETCORE_ENVIRONMENT=Production',
        'DOTNET_CLI_TELEMETRY_OPTOUT=1',
        f'DOTNET_CLI_HOME={shlex.quote(str(home))}',
        f'TMPDIR={shlex.quote(str(temporary))}'
    ]
    if os.environ.get('DOTNET_ROOT'):
        shell_env.append(f'DOTNET_ROOT={shlex.quote(os.environ["DOTNET_ROOT"])}')
    shell_env.extend([
        f'ASPNETCORE_CONTENTROOT={shlex.quote(str(content_root))}',
        f'ASPNETCORE_URLS={shlex.quote(database_url)}'
    ])
    command = 'cd ' + shlex.quote(str(fixture_root)) + ' && ' + ' '.join(shell_env) + ' ' + ' '.join(map(shlex.quote, launch_args))
    write_private(fixture_root / 'launch-command.txt', (command + '\n').encode('utf-8'))

    print(json.dumps({
        'fixtureRoot': str(fixture_root),
        'sourcePins': pins,
        'rehearsalCommit': import_receipt['rehearsalCommit'],
        'workbenchPatchSha256': patch_sha,
        'hostDllSha256': build['hostDllSha256'],
        'loopbackUrl': database_url,
        'databasePath': str(database_path),
        'processWorkingDirectory': str(fixture_root),
        'contentRoot': str(content_root),
        'dropInsDirectory': str(drop_ins),
        'lockDirectory': str(locks),
        'configuredWebhookSinkCount': 0,
        'twoTenantMode': two_tenant,
        'launchCommandFile': str(fixture_root / 'launch-command.txt'),
        'launchApproved': False
    }, indent=2))
    return fixture_root


def cleanup_fixture(path, host_stopped):
    require(host_stopped, 'Pass --host-stopped only after confirming the Workbench process has exited')
    fixture_root = Path(path).expanduser()
    require(not fixture_root.is_symlink(), 'Fixture cleanup path cannot be a symlink')
    fixture_root = fixture_root.resolve(strict=True)
    temp_root = Path(tempfile.gettempdir()).resolve(strict=True)
    require(is_within(fixture_root, temp_root), 'Fixture cleanup is limited to the system temporary directory')
    marker_path = fixture_root / '.elsa-workbench-fixture.json'
    require(marker_path.is_file() and not marker_path.is_symlink(), 'Missing regular fixture ownership marker')
    marker = json.loads(marker_path.read_text())
    require(marker.get('schemaVersion') == 1 and marker.get('fixtureRoot') == str(fixture_root),
            'Fixture ownership marker does not match cleanup path')
    require(fixture_root.name.startswith('elsa-workbench-secrets-'), 'Fixture cleanup path has an unexpected name')
    shutil.rmtree(fixture_root)
    print(json.dumps({'removedFixtureRoot': str(fixture_root)}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--rehearsal-root', type=Path)
    parser.add_argument('--core-sha')
    parser.add_argument('--extensions-sha')
    parser.add_argument('--studio-sha')
    parser.add_argument('--temp-parent', type=Path)
    parser.add_argument('--two-tenant', action='store_true',
                        help='Prepare two synthetic tenant-scoped identities; requires the reviewed fixture patches in the mapped source.')
    parser.add_argument('--cleanup', type=Path)
    parser.add_argument('--host-stopped', action='store_true')
    args = parser.parse_args()

    if args.cleanup:
        require(args.rehearsal_root is None and not args.core_sha and not args.extensions_sha and not args.studio_sha,
                'Fixture cleanup cannot be combined with preparation arguments')
        cleanup_fixture(args.cleanup, args.host_stopped)
        return

    require(args.rehearsal_root is not None, 'Pass --rehearsal-root for fixture preparation')
    require(args.core_sha and args.extensions_sha and args.studio_sha, 'Pass all three pinned source SHAs')
    require(not args.host_stopped, '--host-stopped is valid only with --cleanup')
    prepare_fixture(args.rehearsal_root, args.core_sha, args.extensions_sha, args.studio_sha,
                    args.temp_parent, args.two_tenant)


if __name__ == '__main__':
    main()
