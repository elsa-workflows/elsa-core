"""Generate CycloneDX for the two packed classic Studio libraries and check published bytes."""
import argparse
import base64
from collections import defaultdict
import hashlib
import json
from pathlib import Path
from urllib.parse import quote
import uuid
import time

import prove_studio_npm_pair as pair
import publish_studio_npm_pair as publish


def read(path):
    return json.loads(path.read_bytes())


def component(ecosystem, name, version):
    group, sep, leaf = name.rpartition('/')
    encoded = '/'.join(quote(part, safe='') for part in name.split('/'))
    ref = f'pkg:{ecosystem}/{encoded}@{quote(version, safe="")}'
    return {'type': 'library', 'name': leaf, 'version': version, 'bom-ref': ref,
            'purl': ref} | ({'group': group} if sep else {})


def managed_assets(source, release_version):
    """Resolve only assets whose publish-manifest hash is present in the packed file inventory."""
    obj = source / pair.HOST / 'obj'
    assets = read(obj / 'project.assets.json')
    target = assets['targets']['net10.0/browser-wasm']
    assemblies = defaultdict(set)
    for package, details in target.items():
        for kind in ('runtime', 'resource'):
            for item in details.get(kind, {}):
                if item.endswith('.dll'):
                    assemblies[Path(item).stem].add(package)
    framework = assets['project']['frameworks']['net10.0']
    packs = list(map(Path, assets['packageFolders']))
    for download in framework['downloadDependencies']:
        name = download['name']
        lower, upper = download['version'].strip('[]').split(', ')
        pair.require(lower == upper, 'runtime-pack-version')
        for folder in packs:
            runtime = folder / name.lower() / lower / 'runtimes/browser-wasm'
            for dll in runtime.rglob('*.dll'):
                assemblies[dll.stem].add(f'{name}/{lower}')
    own = pair.HOST.name
    assemblies[own].add(f'{own}/{release_version}')
    by_digest = defaultdict(set)
    published = read(obj / 'Release/net10.0/staticwebassets.publish.json')
    canonical_names = {key.lower(): key for key in target}
    for asset in published['Assets']:
        package = None
        origin = Path(asset['OriginalItemSpec'])
        for folder in packs:
            if origin.is_relative_to(folder):
                name, version, *_ = origin.relative_to(folder).parts
                package = f'{name}/{version}'
                break
        if not package and 'webcil/publish/' in asset['Identity'] and asset['Identity'].endswith('.wasm'):
            matches = assemblies[Path(asset['Identity']).stem]
            pair.require(len(matches) == 1, 'unresolved-packed-assembly-' + Path(asset['Identity']).name)
            package = next(iter(matches))
        if package:
            # Preserve names from the lock whenever a NuGet cache path supplied lower-case spelling.
            canonical = canonical_names.get(package.lower(), package)
            name, version = canonical.split('/')
            is_project = canonical == f'{own}/{release_version}' or target.get(canonical, {}).get('type') == 'project'
            item = component('generic' if is_project else 'nuget', name.lower() if not is_project else name, version)
            by_digest[base64.b64decode(asset['Integrity'], validate=True).hex()].add(
                (item['purl'], item['name'], version))
    return by_digest


def npm_modules(inputs, base, source):
    """Attribute emitted modules to the installed npm package that actually supplied the file."""
    found = {}
    for item in inputs:
        item = item.replace('\x00', '').split('?', 1)[0]
        if 'node_modules/' not in item:
            continue
        path = (base / item).resolve()
        pair.require(path.is_relative_to(source) and '/node_modules/' in path.as_posix(), 'bundle-input-path')
        package, _, relative = path.as_posix().rpartition('/node_modules/')
        parts = relative.split('/')
        count = 2 if parts[0].startswith('@') else 1
        manifest = read(Path(package, 'node_modules', *parts[:count], 'package.json'))
        c = component('npm', manifest['name'], manifest['version'])
        found[c['purl']] = c
    return found


def bundled_js(source, trace, members, name):
    projects = {p.parent.name: p for p in (source / 'studio/src').glob('*/*/ClientLib')}
    result = {}
    for key, raw in members.items():
        parts = key.split('/')
        if '_content' not in parts:
            continue
        project = parts[parts.index('_content') + 1]
        if project not in projects:
            continue
        client = projects[project]
        if key.endswith('.entry.js.map'):
            sources = json.loads(raw)['sources']
            inputs = [s.split('/./', 1)[1] for s in sources if '/./' in s and 'node_modules/' in s]
        elif key.endswith(('/bpmn-js-designer.js', '/x6-designer.js')):
            inputs = read(trace / (project + '.json'))
        else:
            continue
        result.update(npm_modules(inputs, client, source))
    if name == pair.REACT:
        package = json.loads(members['package/package.json'])
        for entry in pair.entrypoints(package):
            inputs = read(trace / (Path(entry).name + '.json'))
            result.update(npm_modules(inputs, source / pair.WORKSPACE / pair.WRAPPER, source))
    return result


def property_(name, value):
    return {'name': 'elsa:' + name, 'value': value}


def generate(build, output):
    retained = build / 'retained'
    receipt = read(retained / 'receipt.json')
    pair.require(receipt['success'] and receipt['source_unchanged'], 'build-incomplete')
    proof = pair.release_candidate_identity(receipt['source_commit'], receipt['source_tree'], receipt['version'],
                                            receipt['run_id'], receipt['run_attempt'])
    source = build / 'private/source'
    managed = managed_assets(source, proof['version'])
    output.mkdir(parents=True, exist_ok=True)
    for label, name in [('wasm', pair.WASM), ('react', pair.REACT)]:
        saved = receipt[label]
        path = retained / saved['file']
        checked = pair.verify_archive(path, name, proof, expected=saved['inventory'], integrity=saved['sha512_integrity'],
                                      wasm_integrity=receipt['wasm']['sha512_integrity'])
        _, members = pair.read_archive(path)
        packed = json.loads(members['package/package.json'])
        components = bundled_js(source, build / 'private/bundle-inputs', members, name)
        root = component('npm', name, proof['version'])
        with path.open('rb') as archive:
            archive_sha256 = hashlib.file_digest(archive, 'sha256').hexdigest()
        root['hashes'] = [{'alg': 'SHA-256', 'content': archive_sha256},
                          {'alg': 'SHA-512', 'content': base64.b64decode(checked['sha512_integrity'][7:]).hex()}]
        root['properties'] = [property_(key, proof[key]) for key in
                              ('source_repository', 'source_commit', 'source_tree', 'run_id', 'run_attempt')]
        root['properties'].append(property_('npm:dist.integrity', checked['sha512_integrity']))
        for field in ('dependencies', 'optionalDependencies', 'peerDependencies'):
            for dep, requirement in sorted(packed.get(field, {}).items()):
                root['properties'].append(property_('npm:' + field + ':' + dep, requirement))
        if name == pair.REACT:
            wasm = component('npm', pair.WASM, proof['version'])
            wasm['hashes'] = [{'alg': 'SHA-512', 'content': base64.b64decode(receipt['wasm']['sha512_integrity'][7:]).hex()}]
            components[wasm['purl']] = wasm
        for filename, entry in checked['inventory'].items():
            sha = entry['sha256']
            components['file:' + filename] = {'type': 'file', 'name': filename, 'bom-ref': 'file:' + filename,
                                              'hashes': [{'alg': 'SHA-256', 'content': sha}]}
            for ref, package_name, version in managed.get(sha, ()):
                components[ref] = {'type': 'library', 'name': package_name, 'version': version, 'purl': ref, 'bom-ref': ref}
        bom = {'bomFormat': 'CycloneDX', 'specVersion': '1.6', 'version': 1,
               'serialNumber': 'urn:uuid:' + str(uuid.uuid5(uuid.NAMESPACE_URL, root['purl'] + root['hashes'][0]['content'])),
               'metadata': {'component': root}, 'components': [components[key] for key in sorted(components)],
               'compositions': [{'aggregate': 'incomplete', 'assemblies': [root['purl']]}]}
        pair.write_json(output / (path.name + '.cdx.json'), bom)


def checked_pair(folder, version, commit, run):
    artifacts = {}
    for name in (pair.WASM, pair.REACT):
        path = folder / (name.removeprefix('@').replace('/', '-') + '-' + version + '.tgz')
        data = path.read_bytes()
        bom = read(folder / (path.name + '.cdx.json'))
        root = bom['metadata']['component']
        props = {p['name']: p['value'] for p in root['properties']}
        integrity = 'sha512-' + base64.b64encode(hashlib.sha512(data).digest()).decode()
        pair.require(root['purl'] == component('npm', name, version)['purl']
                     and root['hashes'] == [{'alg': 'SHA-256', 'content': hashlib.sha256(data).hexdigest()},
                                            {'alg': 'SHA-512', 'content': hashlib.sha512(data).hexdigest()}]
                     and props['elsa:npm:dist.integrity'] == integrity and props['elsa:source_commit'] == commit
                     and props['elsa:run_id'] == run, 'published-sbom-binding')
        proof = pair.release_candidate_identity(commit, props['elsa:source_tree'], version, run, props['elsa:run_attempt'])
        report = pair.verify_archive(path, name, proof, integrity=integrity,
                                     wasm_integrity=artifacts.get(pair.WASM, {}).get('report', {}).get('sha512_integrity'))
        files = {item['name']: item['hashes'] for item in bom['components'] if item['type'] == 'file'}
        expected = {filename: [{'alg': 'SHA-256', 'content': item['sha256']}] for filename, item in report['inventory'].items()}
        pair.require(files == expected, 'sbom-file-inventory')
        artifacts[name] = {'bytes': data, 'report': report}
    return {'identity': {'version': version}, 'artifacts': artifacts}


def verify_published(folder, version, commit, run, registry):
    candidate = checked_pair(folder, version, commit, run)
    for name in candidate['artifacts']:
        for attempt in range(3):
            observed = publish.observe(candidate, registry, name, publish.NpmReadTransport())
            if observed['state'] in ('matching', 'conflicting') or attempt == 2:
                break
            time.sleep(5)
        pair.require(observed['state'] == 'matching', 'registry-readback-' + observed['state'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    gen = commands.add_parser('generate')
    gen.add_argument('--build', type=Path, required=True)
    gen.add_argument('--output', type=Path, required=True)
    verify = commands.add_parser('verify-published')
    verify.add_argument('--folder', type=Path, required=True)
    for key in ('version', 'commit', 'run'):
        verify.add_argument('--' + key, required=True)
    args = parser.parse_args()
    if args.command == 'generate':
        generate(args.build.resolve(), args.output.resolve())
    else:
        verify_published(args.folder.resolve(), args.version, args.commit, args.run, 'feedz')
