#!/usr/bin/env python3
"""Build and read back the two existing Studio npm archives. Never publishes."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import tarfile

ROOT = Path(__file__).resolve().parents[2]
WASM = '@elsa-workflows/elsa-studio-wasm'
REACT = WASM + '-react'
REPOSITORY = 'git+https://github.com/elsa-workflows/elsa-core.git'
HOST = Path('studio/src/hosts/Elsa.Studio.Host.CustomElements')
WORKSPACE = Path('studio/src/wrappers')
WRAPPER = Path('wrappers/react-wrapper')
PROOF = 'elsa-proof.json'
ASSETS = ('_framework', '_content', 'appsettings.json', 'Elsa.Studio.Host.CustomElements.styles.css')
OWNERSHIP = '.elsa-studio-wasm-assets.json'
EXPORTS = ('BackendProvider', 'WorkflowDefinitionEditor', 'WorkflowInstanceViewer', 'WorkflowDefinitionList')
MAX_BYTES = 512 * 1024 * 1024

# Reuse the local, unpublished tarball primitives, not registry/dist-tag verification.
_spec = importlib.util.spec_from_file_location('release_package_artifacts',
    ROOT / '.agents/skills/elsa-release/scripts/verify_packages.py')
_release = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_release)


class ProofError(Exception):
    """Only closed reason codes cross the retained diagnostic boundary."""


def require(condition: bool, code: str) -> None:
    if not condition:
        raise ProofError(code)


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def write_json(path: Path, value: dict) -> None:
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + '\n')


def identity(commit: str, tree: str, version: str, run: str, attempt: str) -> dict:
    require(bool(re.fullmatch('[0-9a-f]{40}', commit)) and bool(re.fullmatch('[0-9a-f]{40}', tree)), 'source-identity')
    require(bool(re.fullmatch('[1-9][0-9]*', run)) and bool(re.fullmatch('[1-9][0-9]*', attempt)), 'run-identity')
    require(version == f'0.0.0-proof.{run}.{attempt}', 'proof-version')
    return {'schema': 1, 'source_repository': 'elsa-workflows/elsa-core', 'source_commit': commit,
            'source_tree': tree, 'version': version, 'run_id': run, 'run_attempt': attempt,
            'framework': 'net10.0', 'published': False}


def regular(path: Path) -> None:
    require(path.is_file() and not any(part.is_symlink() for part in (path, *path.parents)), 'unsafe-file')


def files(root: Path) -> dict[str, dict]:
    result = {}
    for path in sorted(root.rglob('*')):
        require(not path.is_symlink(), 'unsafe-file')
        if path.is_file():
            regular(path)
            data = path.read_bytes()
            result[path.relative_to(root).as_posix()] = {'size': len(data), 'sha256': sha256(data)}
    return result


def read_archive(path: Path) -> tuple[dict, dict[str, bytes]]:
    regular(path)
    try:
        parsed = _release._parse_npm_artifact(path, MAX_BYTES)
        members = {}
        with tarfile.open(path, 'r:gz') as archive:
            for item in archive.getmembers():
                parts = PurePosixPath(item.name).parts
                require(item.name.startswith('package/') and '\\' not in item.name and
                        '..' not in parts and not PurePosixPath(item.name).is_absolute(), 'archive-path')
                require(item.isfile() and item.name not in members, 'archive-member')
                members[item.name] = archive.extractfile(item).read()
        return {key: parsed[key] for key in ('file', 'name', 'version', 'sha512_integrity')}, members
    except ProofError:
        raise
    except (OSError, ValueError, tarfile.TarError, _release.VerificationError):
        raise ProofError('archive-invalid') from None


def entrypoints(package: dict) -> set[str]:
    result = {package.get('main'), package.get('module')}
    def visit(value):
        if isinstance(value, str):
            result.add(value)
        elif isinstance(value, dict):
            for child in value.values():
                visit(child)
        else:
            raise ProofError('entrypoint-declaration')
    visit(package.get('exports'))
    require(all(isinstance(path, str) and path.startswith('./dist/') and
                '..' not in PurePosixPath(path).parts and '\\' not in path for path in result), 'entrypoint-path')
    require(package.get('exports', {}).get('.') == {'import': package.get('module'), 'require': package.get('main')},
            'entrypoint-declaration')
    return {path.removeprefix('./') for path in result}


def verify_archive(path: Path, name: str, proof: dict, *, expected: dict | None = None,
                   integrity: str | None = None, wasm_integrity: str | None = None) -> dict:
    require(name in (WASM, REACT), 'package-id')
    require(name != REACT or (isinstance(wasm_integrity, str) and
            re.fullmatch(r'sha512-[A-Za-z0-9+/]{86}==', wasm_integrity) is not None), 'paired-integrity-missing')
    archive, members = read_archive(path)
    require(archive['name'] == name and archive['version'] == proof['version'], 'archive-identity')
    require(integrity is None or archive['sha512_integrity'] == integrity, 'archive-integrity')
    try:
        package = json.loads(members['package/package.json'])
        embedded = json.loads(members['package/' + PROOF])
    except (KeyError, ValueError):
        raise ProofError('manifest-missing') from None
    wanted = proof | {'package': name}
    if name == REACT:
        wanted['wasm_sha512_integrity'] = wasm_integrity
    require(embedded == wanted, 'manifest-identity')
    require(package.get('repository') == {'type': 'git', 'url': REPOSITORY} and
            package.get('gitHead') == proof['source_commit'], 'package-source')
    inventory = {key.removeprefix('package/'): {'size': len(data), 'sha256': sha256(data)}
                 for key, data in sorted(members.items())}
    if expected is not None:
        require(inventory == expected, 'archive-inventory')
    if name == WASM:
        require(all('package/' + asset in members for asset in ASSETS[2:]) and
                'package/index.html' in members, 'wasm-host-assets')
        require(any(key.startswith('package/_content/') for key in members), 'wasm-content-assets')
        require('package/_framework/blazor.webassembly.js' in members and 'package/_framework/dotnet.js' in members,
                'wasm-runtime-assets')
        for prefix in ('dotnet.native.', 'Elsa.Studio.Host.CustomElements.'):
            require(any(key.startswith('package/_framework/' + prefix) and key.endswith('.wasm') and
                        data.startswith(b'\x00asm') for key, data in members.items()), 'wasm-managed-assets')
        # Every relative runtime/content reference in the actual host must exist.
        html = members['package/index.html'].decode('utf-8')
        references = re.findall(r'(?:src|href)=["\']([^"\']+)["\']', html)
        local = [value for value in references if not value.startswith(('/', '#', 'http:', 'https:'))]
        require(all('package/' + value in members for value in local), 'wasm-host-reference')
    else:
        require(package.get('dependencies', {}).get(WASM) == proof['version'], 'paired-dependency')
        require(all(members.get('package/' + target) for target in entrypoints(package)), 'entrypoint-missing')
        require(members.get('package/scripts/copy-elsa-studio-wasm.js') and
                package.get('scripts', {}).get('postinstall') == 'node scripts/copy-elsa-studio-wasm.js' and
                package.get('scripts', {}).get('copy:elsa-studio-wasm') == 'node scripts/copy-elsa-studio-wasm.js',
                'lifecycle-missing')
    return archive | {'manifest': embedded, 'inventory': inventory,
                      'package_metadata_sha256': sha256(members['package/package.json'])}


def stage_manifest(path: Path, name: str, proof: dict, wasm_integrity: str | None = None) -> None:
    package = json.loads(path.read_text())
    require(package['name'] == name, 'package-id')
    package.update(version=proof['version'], repository={'type': 'git', 'url': REPOSITORY}, gitHead=proof['source_commit'])
    package['files'] = ['**/*'] if name == WASM else ['dist', 'scripts/copy-elsa-studio-wasm.js', PROOF]
    if name == REACT:
        package['dependencies'][WASM] = proof['version']
    write_json(path, package)
    manifest = proof | {'package': name}
    if name == REACT:
        manifest['wasm_sha512_integrity'] = wasm_integrity
    write_json(path.parent / PROOF, manifest)


def stage_workspace(workspace: Path, wasm: Path, report: dict, proof: dict) -> None:
    # Change only the selected local package locator/version/integrity. Keep every
    # third-party lock entry and original dependency selection unchanged.
    manifest_path = workspace / WRAPPER / 'package.json'
    package = json.loads(manifest_path.read_text())
    locator = 'file:' + os.path.relpath(wasm, workspace / WRAPPER)
    package['dependencies'][WASM] = locator
    write_json(manifest_path, package)
    lock_path = workspace / 'package-lock.json'
    lock = json.loads(lock_path.read_text())
    lock['packages'][WRAPPER.as_posix()]['version'] = proof['version']
    lock['packages'][WRAPPER.as_posix()]['dependencies'][WASM] = locator
    lock['packages']['node_modules/' + WASM] = {'version': proof['version'],
        'resolved': 'file:' + os.path.relpath(wasm, workspace), 'integrity': report['sha512_integrity']}
    write_json(lock_path, lock)


def verify_local_lock(lock: dict, archives: dict[str, tuple[Path, dict]], consumer: Path) -> dict:
    observed = {}
    for locator, item in lock.get('packages', {}).items():
        if 'node_modules/@elsa-workflows/' not in locator:
            continue
        name = '@elsa-workflows/' + locator.rsplit('node_modules/@elsa-workflows/', 1)[1]
        require(name in archives and locator == 'node_modules/' + name and name not in observed, 'consumer-elsa-resolution')
        path, report = archives[name]
        resolved = item.get('resolved', '')
        require(resolved.startswith('file:') and (consumer / resolved[5:]).resolve() == path.resolve() and
                item.get('version') == report['version'] and item.get('integrity') == report['sha512_integrity'],
                'consumer-elsa-resolution')
        observed[name] = {'version': item['version'], 'sha512_integrity': item['integrity'], 'resolution': 'same-run-local-archive'}
    require(set(observed) == set(archives), 'consumer-elsa-resolution')
    return observed


def consumer_lock(producer: dict, package: dict, archives: dict[str, tuple[Path, dict]], consumer: Path) -> dict:
    # Seed npm's offline reachability selection with the already-installed lock,
    # rather than resolving ranged dependencies against registry metadata.
    # normalized_consumer_lock drops unused entries and preserves selected bytes.
    packages = {key: value for key, value in producer['packages'].items() if key.startswith('node_modules/')}
    require(all(not item.get('link') for key, item in packages.items() if key != 'node_modules/' + REACT), 'consumer-lock-link')
    for name, (path, report) in archives.items():
        _, members = read_archive(path)
        metadata = json.loads(members['package/package.json'])
        item = {'name': name, 'version': report['version'], 'resolved': package['dependencies'][name],
                'integrity': report['sha512_integrity']}
        if metadata.get('dependencies'):
            item['dependencies'] = metadata['dependencies']
        if metadata.get('scripts', {}).get('postinstall'):
            item['hasInstallScript'] = True
        packages['node_modules/' + name] = item
    packages[''] = {key: package[key] for key in ('name', 'dependencies', 'devDependencies')}
    lock = {'name': package['name'], 'lockfileVersion': 3, 'requires': True, 'packages': packages}
    verify_local_lock(lock, archives, consumer)
    require(all(packages[key] == item for key, item in producer['packages'].items()
                if key.startswith('node_modules/') and key not in ('node_modules/' + WASM, 'node_modules/' + REACT)),
            'consumer-lock-third-party')
    return lock


def normalized_consumer_lock(candidate: dict, normalized: dict) -> dict:
    # npm selects reachability offline. Keep the original producer records for
    # those entries, including flags, rather than accepting npm metadata edits.
    selected = normalized['packages']
    require(all(key in candidate['packages'] for key in selected), 'consumer-lock-new-package')
    fields = ('version', 'resolved', 'integrity', 'dependencies', 'optionalDependencies',
              'peerDependencies', 'peerDependenciesMeta', 'engines', 'cpu', 'os')
    for key, item in selected.items():
        require(all(item.get(field) == candidate['packages'][key].get(field) for field in fields),
                'consumer-lock-third-party')
    return candidate | {'packages': {key: item for key, item in candidate['packages'].items() if key in selected}}


class Runner:
    def __init__(self, private: Path, receipt: dict):
        self.private, self.receipt = private, receipt
        empty = private / 'empty.npmrc'
        empty.write_text('')
        global_config = private / 'global.npmrc'
        global_config.write_text('')
        home = private / 'home'
        home.mkdir()
        allowed = ('PATH', 'TMPDIR', 'DOTNET_ROOT', 'DOTNET_ROOT_X64')
        self.env = {key: os.environ[key] for key in allowed if key in os.environ} | {
            'HOME': str(home), 'NUGET_PACKAGES': os.environ.get('NUGET_PACKAGES', str(Path.home() / '.nuget/packages')),
            'DOTNET_CLI_TELEMETRY_OPTOUT': '1', 'DOTNET_NOLOGO': '1',
            'NPM_CONFIG_USERCONFIG': str(empty), 'NPM_CONFIG_GLOBALCONFIG': str(global_config),
            'NPM_CONFIG_CACHE': str(private / 'npm-cache'), 'NPM_CONFIG_AUDIT': 'false', 'NPM_CONFIG_FUND': 'false',
            'NPM_CONFIG_IGNORE_SCRIPTS': 'false'}

    def run(self, label: str, argv: list[str], cwd: Path, timeout: int = 1800) -> str:
        self.receipt['stage'] = label
        record = {'step': label, 'success': False}
        self.receipt['commands'].append(record)
        log = self.private / f'{len(self.receipt["commands"]):02}-{label}.log'
        try:
            with log.open('wb') as stream:
                process = subprocess.run(argv, cwd=cwd, env=self.env, stdout=stream, stderr=subprocess.STDOUT, timeout=timeout)
            record['exit_code'] = process.returncode
            require(process.returncode == 0, 'command-failed')
            record['success'] = True
            return log.read_text(errors='replace')
        except subprocess.TimeoutExpired:
            raise ProofError('command-timeout') from None


def pack(runner: Runner, folder: Path, artifacts: Path, name: str) -> Path:
    # Never trust npm's stdout filename or integrity as archive readback.
    before = set(artifacts.glob('*.tgz'))
    runner.run('pack-' + ('wasm' if name == WASM else 'react'),
               ['npm', 'pack', '--json', '--pack-destination', str(artifacts)], folder)
    added = set(artifacts.glob('*.tgz')) - before
    require(len(added) == 1, 'pack-output')
    return added.pop()


def asset_inventory(wasm: dict) -> dict:
    return {path: metadata for path, metadata in wasm['inventory'].items()
            if any(path == asset or path.startswith(asset + '/') for asset in ASSETS)}


def installed_assets(consumer: Path, wasm: dict) -> dict:
    roots = {'wasm': consumer / 'node_modules' / WASM,
             'consumer_public': consumer / 'public'}
    wanted = asset_inventory(wasm)
    for folder in roots.values():
        observed = files(folder)
        require(all(observed.get(path) == metadata for path, metadata in wanted.items()), 'installed-assets')
        if folder == roots['wasm']:
            require({path for path in observed if any(path == asset or path.startswith(asset + '/') for asset in ASSETS)} == set(wanted),
                    'installed-assets')
    ownership = roots['consumer_public'] / OWNERSHIP
    regular(ownership)
    try:
        ledger = json.loads(ownership.read_text())
    except (OSError, ValueError):
        raise ProofError('installed-ownership') from None
    require(ledger == {'schema': 1, 'package': WASM, 'files': [
        {'path': path, 'sha256': metadata['sha256']} for path, metadata in sorted(wanted.items())]}, 'installed-ownership')
    return {'verified_files_per_location': len(wanted), 'locations': list(roots), 'lifecycle_scripts_enabled': True,
            'ownership_manifest': {'file': OWNERSHIP, 'sha256': sha256(ownership.read_bytes()), 'owned_files': len(wanted)}}


def module_smoke_scripts(consumer: Path) -> None:
    expected = json.dumps(list(EXPORTS))
    (consumer / 'imports.mjs').write_text(f"import * as wrapper from '{REACT}';\n"
        f"for (const name of {expected}) if (typeof wrapper[name] !== 'function') throw new Error('export');\n")
    (consumer / 'require.cjs').write_text(f"const resolved = require.resolve('{REACT}');\n"
        f"const wrapper = require('{REACT}');\nif (!resolved.endsWith('.cjs')) throw new Error('resolution');\n"
        f"for (const name of {expected}) if (typeof wrapper[name] !== 'function') throw new Error('export');\n")


def verify_vite_assets(built: dict, wasm: dict) -> None:
    require(all(built.get(path) == metadata for path, metadata in asset_inventory(wasm).items()), 'consumer-vite-assets')


def consume(runner: Runner, private: Path, artifacts: dict[str, tuple[Path, dict]], workspace: Path, proof: dict) -> dict:
    consumer = private / 'consumer'
    consumer.mkdir()
    producer = json.loads((workspace / 'package-lock.json').read_text())
    producer_lock = producer['packages']
    dependencies = {name: 'file:' + os.path.relpath(path, consumer) for name, (path, _) in artifacts.items()}
    dependencies.update({name: producer_lock['node_modules/' + name]['version'] for name in ('react', 'react-dom')})
    package = {'name': 'elsa-paired-archive-consumer', 'private': True, 'type': 'module',
        'dependencies': dependencies, 'devDependencies': {'vite': producer_lock['node_modules/vite']['version']}}
    write_json(consumer / 'package.json', package)
    # Deny registry Elsa lookup in addition to the offline installation gate.
    (consumer / '.npmrc').write_text('@elsa-workflows:registry=http://127.0.0.1:9\nignore-scripts=false\n')
    lock = consumer_lock(producer, package, artifacts, consumer)
    write_json(consumer / 'package-lock.json', lock)
    runner.run('consumer-lock-offline', ['npm', 'install', '--package-lock-only', '--offline', '--ignore-scripts=false'], consumer)
    lock = normalized_consumer_lock(lock, json.loads((consumer / 'package-lock.json').read_text()))
    write_json(consumer / 'package-lock.json', lock)
    local = verify_local_lock(lock, artifacts, consumer)
    public = artifacts[WASM][0].parent
    retained_inputs = {}
    for filename in ('package.json', 'package-lock.json'):
        retained = 'consumer-' + filename
        shutil.copyfile(consumer / filename, public / retained)
        retained_inputs[filename] = {'retained_file': retained, 'sha256': sha256((consumer / filename).read_bytes())}
    runner.receipt['consumer_inputs'] = retained_inputs
    # The producer populated the cache with these same locked third-party tools.
    # A cold/absent cache is a failure, never a fallback to registry Elsa bytes.
    try:
        runner.run('consumer-install-offline', ['npm', 'ci', '--offline', '--ignore-scripts=false'], consumer)
    finally:
        unchanged = all(sha256((consumer / filename).read_bytes()) == record['sha256']
                        for filename, record in retained_inputs.items())
        runner.receipt['consumer_inputs_unchanged_after_install'] = unchanged
        require(unchanged, 'consumer-inputs-changed')
    verify_local_lock(json.loads((consumer / 'package-lock.json').read_text()), artifacts, consumer)
    wasm = artifacts[WASM][1]
    assets = installed_assets(consumer, wasm)
    stale = consumer / 'public/_framework/stale-from-previous-package.js'
    stale.write_bytes(b'previously owned asset')
    ownership_path = consumer / 'public' / OWNERSHIP
    previous_ownership = json.loads(ownership_path.read_text())
    previous_ownership['files'].append({'path': '_framework/' + stale.name, 'sha256': sha256(stale.read_bytes())})
    write_json(ownership_path, previous_ownership)
    unmanaged = {'_framework/consumer-owned.js': b'consumer runtime',
                 '_content/consumer/consumer-owned.js': b'consumer content'}
    for path, data in unmanaged.items():
        target = consumer / 'public' / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
    runner.run('consumer-lifecycle-refresh', ['npm', 'run', 'copy:elsa-studio-wasm', '--prefix',
               str(consumer / 'node_modules' / REACT)], consumer)
    require(not stale.exists(), 'lifecycle-stale-assets')
    assets = installed_assets(consumer, wasm)
    require(all((consumer / 'public' / path).read_bytes() == data for path, data in unmanaged.items()), 'lifecycle-unmanaged-assets')
    assets.update(stale_assets_removed=True, unmanaged_files_preserved=True)
    installed_versions = {}
    for name in ('react', 'react-dom', 'vite', 'uuid'):
        metadata_path = consumer / 'node_modules' / name / 'package.json'
        regular(metadata_path)
        installed_versions[name] = json.loads(metadata_path.read_text())['version']
        require(installed_versions[name] == producer_lock['node_modules/' + name]['version'], 'consumer-tool-version')
    require(installed_versions['react'].startswith('19.') and installed_versions['react-dom'].startswith('19.'), 'consumer-react-version')
    module_smoke_scripts(consumer)
    runner.run('consumer-esm', ['node', 'imports.mjs'], consumer)
    runner.run('consumer-commonjs', ['node', 'require.cjs'], consumer)
    _, wasm_members = read_archive(artifacts[WASM][0])
    html = wasm_members['package/index.html'].decode('utf-8')
    html = re.sub(r'((?:src|href)=["\'])(?=(?:_framework/|_content/|Elsa\.))', r'\1/', html)
    html = html.replace('</body>', '<div id="root"></div><script type="module" src="/src.js"></script></body>')
    (consumer / 'index.html').write_text(html)
    (consumer / 'src.js').write_text(f"import React from 'react'; import {{createRoot}} from 'react-dom/client';\n"
        f"import {{BackendProvider, WorkflowDefinitionEditor}} from '{REACT}';\n"
        "createRoot(document.getElementById('root')).render(React.createElement(BackendProvider, {}, React.createElement(WorkflowDefinitionEditor)));\n")
    (consumer / 'vite.config.mjs').write_text('export default {};\n')
    runner.run('consumer-vite', ['node', 'node_modules/vite/bin/vite.js', 'build'], consumer)
    built = files(consumer / 'dist')
    verify_vite_assets(built, wasm)
    require(all(built.get(path) == {'size': len(data), 'sha256': sha256(data)} for path, data in unmanaged.items()),
            'consumer-vite-unmanaged-assets')
    require(all(sha256((consumer / filename).read_bytes()) == record['sha256']
                for filename, record in retained_inputs.items()), 'consumer-inputs-changed')
    for name, (path, report) in artifacts.items():
        verify_archive(path, name, proof, integrity=report['sha512_integrity'],
                       wasm_integrity=wasm['sha512_integrity'] if name == REACT else None)
    return {'elsa_resolution': local, 'lock_sha256': sha256((consumer / 'package-lock.json').read_bytes()),
            'assets': assets, 'esm_import': True, 'commonjs_require': True, 'vite_build': True,
            'vite_output_files': len(built), 'offline_install': True, 'installed_versions': installed_versions,
            'third_party_resolution': 'npm-offline-reachable-unchanged-producer-entries',
            'producer_lock_sha256': sha256((workspace / 'package-lock.json').read_bytes())}


def git(root: Path, *args: str) -> str:
    try:
        return subprocess.check_output(['git', '--no-optional-locks', *args], cwd=root,
                                       text=True, stderr=subprocess.PIPE).strip()
    except (OSError, subprocess.CalledProcessError):
        raise ProofError('source-status-unavailable') from None


def prove(root: Path, output: Path, commit: str, version: str, run: str, attempt: str) -> dict:
    require(root.resolve() == root and not output.resolve().is_relative_to(root), 'output-location')
    require(not output.exists() and not any(part.is_symlink() for part in output.parents), 'output-exists')
    proof = identity(commit, git(root, 'rev-parse', 'HEAD^{tree}'), version, run, attempt)
    require(git(root, 'rev-parse', 'HEAD') == commit and not git(root, 'status', '--porcelain'), 'source-checkout')
    output.mkdir(parents=True)
    public = output / 'retained'
    public.mkdir()
    private = output / 'private'
    private.mkdir()
    receipt = proof | {'success': False, 'publisher_authority': False, 'commands': [], 'stage': 'admission'}
    runner = Runner(private, receipt)
    try:
        require(runner.run('node-version', ['node', '--version'], root).strip().split('.')[0] == 'v22', 'node-version')
        receipt['toolchain'] = {'node': runner.run('node-identity', ['node', '--version'], root).strip(),
                                'npm': runner.run('npm-identity', ['npm', '--version'], root).strip()}
        source = private / 'source'
        runner.run('source-init', ['git', 'init', '--quiet', str(source)], private)
        runner.run('source-fetch', ['git', 'fetch', '--no-tags', str(root), commit], source)
        runner.run('source-checkout', ['git', 'checkout', '--detach', '--quiet', commit], source)
        runner.run('source-origin', ['git', 'remote', 'add', 'origin', REPOSITORY.removeprefix('git+')], source)
        write_json(source / 'global.json', {'sdk': {'version': '10.0.300', 'rollForward': 'disable'}})
        receipt['toolchain']['dotnet'] = runner.run('sdk-identity', ['dotnet', '--version'], source).strip()
        require(receipt['toolchain']['dotnet'] == '10.0.300', 'sdk-version')
        runner.run('clientlibs', ['bash', 'scripts/integration-program/build_studio_clientlibs.sh', str(source)], source)
        publish = private / 'publish'
        runner.run('custom-elements-publish', ['dotnet', 'publish', str(HOST / 'Elsa.Studio.Host.CustomElements.csproj'),
            '-c', 'Release', '-f', 'net10.0', '-o', str(publish), '-p:Version=' + version], source, timeout=3600)
        wasm_stage = private / 'wasm'
        shutil.copytree(publish / 'wwwroot', wasm_stage)
        shutil.copyfile(source / HOST / 'npm/package.json', wasm_stage / 'package.json')
        stage_manifest(wasm_stage / 'package.json', WASM, proof)
        wasm_expected = files(wasm_stage)
        wasm_path = pack(runner, wasm_stage, public, WASM)
        wasm = verify_archive(wasm_path, WASM, proof, expected=wasm_expected)
        receipt['wasm'] = wasm
        workspace = source / WORKSPACE
        wrapper = workspace / WRAPPER
        stage_manifest(wrapper / 'package.json', REACT, proof, wasm['sha512_integrity'])
        stage_workspace(workspace, wasm_path, wasm, proof)
        runner.run('wrapper-install', ['npm', 'ci', '--ignore-scripts=false'], workspace)
        # Verify actual installed WASM bytes before allowing the wrapper build.
        installed = files(workspace / 'node_modules' / WASM)
        require(installed == wasm['inventory'], 'wrapper-wasm-input')
        receipt['wrapper_build_input'] = {'name': WASM, 'version': version, 'sha512_integrity': wasm['sha512_integrity'],
                                         'installed_inventory_matches': True}
        runner.run('wrapper-build', ['npm', 'run', 'build'], wrapper)
        stage_manifest(wrapper / 'package.json', REACT, proof, wasm['sha512_integrity'])
        expected = {'dist/' + key: value for key, value in files(wrapper / 'dist').items()}
        for key in ('package.json', PROOF, 'README.md', 'scripts/copy-elsa-studio-wasm.js'):
            regular(wrapper / key)
            data = (wrapper / key).read_bytes()
            expected[key] = {'size': len(data), 'sha256': sha256(data)}
        react_path = pack(runner, wrapper, public, REACT)
        react = verify_archive(react_path, REACT, proof, expected=expected, wasm_integrity=wasm['sha512_integrity'])
        receipt['react'] = react
        receipt['consumer'] = consume(runner, private, {WASM: (wasm_path, wasm), REACT: (react_path, react)}, workspace, proof)
        receipt.update(success=True, stage='complete')
    except ProofError as error:
        receipt['failure_code'] = str(error)
    except Exception:
        receipt['failure_code'] = 'unexpected-failure'
    finally:
        try:
            unchanged = git(root, 'rev-parse', 'HEAD') == commit and not git(root, 'status', '--porcelain')
            receipt['source_unchanged'] = unchanged
            if not unchanged:
                receipt.update(success=False, source_failure_code='source-checkout')
        except Exception:
            receipt.update(success=False, source_unchanged=False, source_failure_code='source-status-unavailable')
        write_json(public / 'receipt.json', receipt)
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--commit', required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--run-id', required=True)
    parser.add_argument('--run-attempt', required=True)
    args = parser.parse_args()
    try:
        report = prove(args.root.resolve(), args.output.resolve(), args.commit, args.version, args.run_id, args.run_attempt)
        print(json.dumps({'success': report['success'], 'stage': report['stage'],
                          'failure_code': report.get('failure_code')}))
        return 0 if report['success'] else 1
    except ProofError as error:
        print(json.dumps({'success': False, 'failure_code': str(error)}))
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
