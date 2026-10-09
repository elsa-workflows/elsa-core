"""Original historical Studio npm recipe and a clean local-archive consumer.

Keep original inline lifecycle and dist-only packaging. Their actual execution,
not a requirement for today's helper/ownership ledger, decides functionality.
"""
from __future__ import annotations

import json
import os
from pathlib import Path
import shutil

import prove_studio_npm_pair as npm
from product_artifact_execution import validate_local_execution

HOST = Path('src/hosts/Elsa.Studio.Host.CustomElements')
WORKSPACE = Path('src/wrappers')


def stage_version(path: Path, name: str, version: str, *, dependency: str | None = None) -> dict:
    package = json.loads(path.read_text())
    npm.require(package['name'] == name, 'historical-package-identity')
    package['version'] = version
    if dependency is not None:
        package['dependencies'][npm.WASM] = dependency
    npm.write_json(path, package)
    return package


def verify(path: Path, name: str, version: str, original: dict, wasm: dict | None = None) -> dict:
    archive, members = npm.read_archive(path)
    npm.require(archive['name'] == name and archive['version'] == version, 'historical-archive-identity')
    package = json.loads(members['package/package.json'])
    npm.require(package == original, 'historical-package-metadata')
    inventory = {key.removeprefix('package/'): {'size': len(value), 'sha256': npm.sha256(value)}
                 for key, value in sorted(members.items())}
    if name == npm.WASM:
        npm.require('package/_framework/blazor.webassembly.js' in members and 'package/_framework/dotnet.js' in members and
                    'package/index.html' in members and 'package/appsettings.json' in members and
                    any(key.startswith('package/_content/') for key in members), 'historical-host-assets')
        for prefix in ('dotnet.native.', 'Elsa.Studio.Host.CustomElements.'):
            npm.require(any(key.startswith('package/_framework/' + prefix) and key.endswith('.wasm') and
                            value.startswith(b'\x00asm') for key, value in members.items()), 'historical-managed-assets')
    else:
        npm.require(wasm is not None and package['dependencies'][npm.WASM] == version, 'historical-paired-dependency')
        npm.require(all(members.get('package/' + entry) for entry in npm.entrypoints(package)), 'historical-entrypoint')
        wanted = {path: record for path, record in wasm['inventory'].items()
                  if path.startswith(('_content/', '_framework/')) or path == 'appsettings.json'}
        npm.require(all(inventory.get('dist/' + path) == record for path, record in wanted.items()), 'historical-dist-assets')
    return archive | {'inventory': inventory, 'package_metadata_sha256': npm.sha256(members['package/package.json'])}


def consume(runner: npm.Runner, private: Path, archives: dict, workspace: Path) -> dict:
    consumer = private / 'consumer'
    consumer.mkdir()
    producer = json.loads((workspace / 'package-lock.json').read_text())
    locks = producer['packages']
    package = {'name': 'elsa-historical-local-archive-consumer', 'private': True, 'type': 'module',
               'dependencies': {name: 'file:' + os.path.relpath(path, consumer) for name, (path, _) in archives.items()} |
                 {name: locks['node_modules/' + name]['version'] for name in ('react', 'react-dom')},
               'devDependencies': {'vite': locks['node_modules/vite']['version']}}
    npm.write_json(consumer / 'package.json', package)
    (consumer / '.npmrc').write_text('@elsa-workflows:registry=http://127.0.0.1:9\nignore-scripts=false\n')
    lock = npm.consumer_lock(producer, package, archives, consumer)
    npm.write_json(consumer / 'package-lock.json', lock)
    runner.run('historical-consumer-lock', ['npm', 'install', '--package-lock-only', '--offline', '--ignore-scripts=false'], consumer)
    lock = npm.normalized_consumer_lock(lock, json.loads((consumer / 'package-lock.json').read_text()))
    npm.write_json(consumer / 'package-lock.json', lock)
    local = npm.verify_local_lock(lock, archives, consumer)
    inputs = {name: npm.sha256((consumer / name).read_bytes()) for name in ('package.json', 'package-lock.json')}
    # Fresh install cache and HOME. Third-party URLs/integrities stay in the
    # producer lock; selected Elsa packages have only exact local file locators.
    consumer_private = private / 'consumer-isolation'
    consumer_private.mkdir()
    clean_runner = npm.Runner(consumer_private, runner.receipt)
    try:
        clean_runner.run('historical-consumer-normal-install', ['npm', 'ci', '--ignore-scripts=false'], consumer)
    finally:
        npm.require(all(npm.sha256((consumer / name).read_bytes()) == digest for name, digest in inputs.items()),
                    'historical-consumer-inputs-changed')
    npm.verify_local_lock(json.loads((consumer / 'package-lock.json').read_text()), archives, consumer)
    for name, (_, report) in archives.items():
        npm.require(npm.files(consumer / 'node_modules' / name) == report['inventory'], 'historical-consumer-installed-bytes')
    npm.module_smoke_scripts(consumer)
    clean_runner.run('historical-consumer-esm', ['node', 'imports.mjs'], consumer)
    clean_runner.run('historical-consumer-commonjs', ['node', 'require.cjs'], consumer)
    (consumer / 'index.html').write_text('<div id="root"></div><script type="module" src="/src.js"></script>')
    (consumer / 'src.js').write_text(f"import {{ WorkflowDefinitionEditor }} from '{npm.REACT}'; console.log(WorkflowDefinitionEditor);\n")
    clean_runner.run('historical-consumer-vite', ['npm', 'exec', '--offline', '--', 'vite', 'build'], consumer)
    return {'local_archives': local, 'empty_install_cache': True, 'normal_lifecycle': True,
            'esm': True, 'commonjs': True, 'vite': True, 'input_sha256': inputs}


def prove(source: Path, private: Path, retained: Path, plan: dict, execution: dict, framework: str) -> dict:
    # The selected controller admits this local envelope before any product work.
    validate_local_execution(execution)
    npm.require(framework == 'net10.0', 'historical-host-framework')
    private.mkdir()
    retained.mkdir()
    receipt = {'source_commit': plan['source']['commit'], 'source_tree': plan['source']['tree'],
               'version': plan['requested_version'], 'execution': execution,
               'framework': framework, 'commands': [], 'success': False, 'published': False,
               'historical_workflow_executed': False, 'original_lifecycle_preserved': True}
    runner = npm.Runner(private, receipt)
    version = plan['requested_version']
    try:
        npm.require(runner.run('node-version', ['node', '--version'], source).strip().startswith('v22.'), 'node-version')
        publish = private / 'publish'
        runner.run('historical-host-publish', ['dotnet', 'publish', str(HOST), '-c', 'Release', '-f', framework,
                   '-o', str(publish), '/p:Version=' + version], source, timeout=3600)
        stage = private / 'wasm'
        shutil.copytree(publish / 'wwwroot', stage)
        shutil.copyfile(source / HOST / 'npm/package.json', stage / 'package.json')
        wasm_metadata = stage_version(stage / 'package.json', npm.WASM, version)
        wasm_path = npm.pack(runner, stage, retained, npm.WASM)
        wasm = verify(wasm_path, npm.WASM, version, wasm_metadata)
        receipt['wasm'] = wasm
        workspace, wrapper = source / WORKSPACE, source / WORKSPACE / npm.WRAPPER
        stage_version(wrapper / 'package.json', npm.REACT, version)
        npm.stage_workspace(workspace, wasm_path, wasm, {'version': version})
        runner.run('historical-wrapper-install', ['npm', 'ci', '--ignore-scripts=false'], workspace)
        npm.require(npm.files(workspace / 'node_modules' / npm.WASM) == wasm['inventory'], 'historical-wrapper-wasm-bytes')
        wrapper_metadata = stage_version(wrapper / 'package.json', npm.REACT, version, dependency=version)
        runner.run('historical-wrapper-copy', ['npm', 'run', 'copy:elsa-studio-wasm'], wrapper)
        runner.run('historical-wrapper-build', ['npm', 'run', 'build'], wrapper)
        react_path = npm.pack(runner, wrapper, retained, npm.REACT)
        react = verify(react_path, npm.REACT, version, wrapper_metadata, wasm)
        receipt['react'] = react
        receipt['consumer'] = consume(runner, private, {npm.WASM: (wasm_path, wasm), npm.REACT: (react_path, react)}, workspace)
        receipt.update(success=True, stage='complete')
        return receipt
    except Exception:
        receipt['failure_code'] = receipt.get('stage', 'historical-npm') + '-failed'
        raise
    finally:
        npm.write_json(retained / 'receipt.json', receipt)
