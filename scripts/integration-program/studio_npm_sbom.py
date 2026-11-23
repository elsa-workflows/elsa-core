"""Generate CycloneDX for the two packed classic Studio libraries and check published bytes."""
import argparse
import base64
from collections import defaultdict
import hashlib
import json
import os
import re
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
    ref = f'pkg:{ecosystem}/{encoded}' + ('@' + quote(version, safe='') if version else '')
    item = {'type': 'library', 'name': leaf, 'bom-ref': ref, 'purl': ref}
    if version:
        item['version'] = version
    if sep:
        item['group'] = group
    return item


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
    canonical_names = {key.lower(): key for key in target}
    for download in framework['downloadDependencies']:
        name = download['name']
        lower, upper = download['version'].strip('[]').split(', ')
        pair.require(lower == upper, 'runtime-pack-version')
        canonical_names[f'{name}/{lower}'.lower()] = f'{name}/{lower}'
        for folder in packs:
            runtime = folder / name.lower() / lower / 'runtimes/browser-wasm'
            for dll in runtime.rglob('*.dll'):
                assemblies[dll.stem].add(f'{name}/{lower}')
    own = pair.HOST.name
    assemblies[own].add(f'{own}/{release_version}')
    by_digest = defaultdict(set)
    published = read(obj / 'Release/net10.0/staticwebassets.publish.json')
    for asset in published['Assets']:
        package = None
        origin = Path(asset['OriginalItemSpec'])
        for folder in packs:
            if origin.is_relative_to(folder):
                name, version, *_ = origin.relative_to(folder).parts
                package = f'{name}/{version}'
                break
        webcil = next((Path(value) for value in (asset['Identity'], asset['OriginalItemSpec'])
                       if '/webcil/' in value and value.endswith('.wasm')), None)
        if not package and webcil is not None:
            matches = assemblies[webcil.stem]
            pair.require(len(matches) == 1, 'unresolved-packed-assembly-' + webcil.name)
            package = next(iter(matches))
        if package:
            # Preserve names from the lock whenever a NuGet cache path supplied lower-case spelling.
            canonical = canonical_names.get(package.lower(), package)
            name, version = canonical.split('/')
            is_project = canonical == f'{own}/{release_version}' or target.get(canonical, {}).get('type') == 'project'
            item = component('generic' if is_project else 'nuget', name, version)
            digest = base64.b64decode(asset['Integrity'], validate=True).hex()
            if asset['SourceType'] == 'Package' and any(origin.is_relative_to(folder) for folder in packs):
                # Packages can ship precomputed integrity from before packaging transforms.
                # Match against the resolved package file that actually entered this build.
                with origin.open('rb') as raw:
                    digest = hashlib.file_digest(raw, 'sha256').hexdigest()
            by_digest[digest].add((item['purl'], item['name'], version))
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
    monaco = [raw for key, raw in members.items()
              if '/_content/BlazorMonaco/lib/monaco-editor/min/vs/nls.messages.' in key and key.endswith('.js')]
    if monaco:
        versions = set()
        for raw in monaco:
            banner = re.search(rb'\* Version: (\d+\.\d+\.\d+)\([0-9a-f]{40}\)', raw[:1024])
            pair.require(banner is not None, 'unidentified-monaco-version')
            versions.add(banner.group(1).decode('ascii'))
        pair.require(len(versions) == 1, 'inconsistent-monaco-version')
        editor = component('npm', 'monaco-editor', versions.pop())
        editor['evidence'] = {'occurrences': [{'location': key.removeprefix('package/')} for key in sorted(members)
                                               if '/_content/BlazorMonaco/lib/monaco-editor/' in key]}
        result[editor['purl']] = editor
    if name == pair.REACT:
        package = json.loads(members['package/package.json'])
        for entry in pair.entrypoints(package):
            inputs = read(trace / (Path(entry).name + '.json'))
            result.update(npm_modules(inputs, source / pair.WORKSPACE / pair.WRAPPER, source))
    return result


def property_(name, value):
    return {'name': 'elsa:' + name, 'value': value}


def declared_dependencies(manifest, wasm_integrity):
    """Represent declared npm ranges independently of the versions embedded in browser bundles."""
    required = {}
    for field in ('dependencies', 'optionalDependencies', 'peerDependencies'):
        for name, requirement in manifest.get(field, {}).items():
            if name == pair.WASM:
                item = component('npm', name, requirement)
                item['hashes'] = [{'alg': 'SHA-512', 'content': base64.b64decode(wasm_integrity[7:]).hex()}]
            else:
                item = component('npm', name, None)
            if field == 'optionalDependencies' or manifest.get('peerDependenciesMeta', {}).get(name, {}).get('optional'):
                item['scope'] = 'optional'
            ref = item['bom-ref']
            props = required.setdefault(ref, item).setdefault('properties', [])
            props.append(property_('npm:' + field, requirement))
    return required


def publisher_context():
    """Read the original public release selection and native GitHub workflow build-type parameters."""
    github = {key: os.environ[variable] for key, variable in (
        ('event_name', 'GITHUB_EVENT_NAME'), ('repository_id', 'GITHUB_REPOSITORY_ID'),
        ('repository_owner_id', 'GITHUB_REPOSITORY_OWNER_ID'), ('runner_environment', 'RUNNER_ENVIRONMENT'))}
    pair.require(github['event_name'] == 'workflow_dispatch' and github['runner_environment'] == 'github-hosted'
                 and all(re.fullmatch(r'[1-9][0-9]*', github[key]) for key in ('repository_id', 'repository_owner_id')),
                 'release-build-context')
    selection = read(Path(os.environ['GITHUB_EVENT_PATH']))['inputs'].get('publish_npm')
    pair.require(selection in ('true', True), 'npm-release-not-selected')
    return github


def provenance(proof):
    # Use the build attempt embedded in both original tarballs even when signing is retried.
    repository = 'https://github.com/' + proof['source_repository']
    tag = 'refs/tags/' + proof['version']
    workflow = '.github/workflows/packages.yml'
    git_source = {'uri': 'git+' + repository + '@' + tag, 'digest': {'gitCommit': proof['source_commit']}}
    return {
        'buildDefinition': {
            'buildType': 'https://actions.github.io/buildtypes/workflow/v1',
            'externalParameters': {'workflow': {'ref': tag, 'repository': repository, 'path': workflow}},
            'internalParameters': {'github': publisher_context()},
            'resolvedDependencies': [git_source],
        },
        'runDetails': {
            'builder': {'id': repository + '/' + workflow + '@' + tag},
            # The GitHub workflow/v1 external parameter contract has only "workflow".
            # Record the verified npm selection separately as public SLSA byproduct data.
            'byproducts': [{'name': 'workflow_dispatch.inputs.publish_npm',
                            'mediaType': 'application/json', 'content': base64.b64encode(b'true').decode()}],
            'metadata': {'invocationId': repository + '/actions/runs/' + proof['run_id'] + '/attempts/' + proof['run_attempt']},
        },
    }


def generate(build, output, *, release_provenance=False):
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
        declared = declared_dependencies(packed, receipt['wasm']['sha512_integrity'])
        components.update(declared)
        for filename, entry in checked['inventory'].items():
            sha = entry['sha256']
            components['file:' + filename] = {'type': 'file', 'name': filename, 'bom-ref': 'file:' + filename,
                                              'hashes': [{'alg': 'SHA-256', 'content': sha}]}
            for ref, package_name, version in managed.get(sha, ()):
                entry = components.setdefault(ref, {'type': 'library', 'name': package_name, 'version': version,
                                                    'purl': ref, 'bom-ref': ref, 'evidence': {'occurrences': []}})
                entry['evidence']['occurrences'].append({'location': filename})
        bom = {'bomFormat': 'CycloneDX', 'specVersion': '1.6', 'version': 1,
               'serialNumber': 'urn:uuid:' + str(uuid.uuid5(uuid.NAMESPACE_URL, root['purl'] + root['hashes'][0]['content'])),
               'metadata': {'component': root}, 'components': [components[key] for key in sorted(components)],
               'dependencies': [{'ref': root['purl'], 'dependsOn': sorted(declared)}],
               'compositions': [{'aggregate': 'incomplete', 'assemblies': [root['purl']]}]}
        pair.write_json(output / (path.name + '.cdx.json'), bom)
    if release_provenance:
        pair.write_json(output / 'provenance.json', provenance(proof))


def checked_pair(folder, version, commit, run):
    artifacts = {}
    first_proof = None
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
        if first_proof is None:
            first_proof = proof
        pair.require(proof == first_proof and props['elsa:source_repository'] == proof['source_repository'], 'pair-build-identity')
        _, members = pair.read_archive(path)
        manifest = json.loads(members['package/package.json'])
        declared = declared_dependencies(manifest, artifacts[pair.WASM]['report']['sha512_integrity'] if artifacts else integrity)
        components = {item['bom-ref']: item for item in bom['components']}
        pair.require(len(components) == len(bom['components'])
                     and all(components.get(ref) == item for ref, item in declared.items())
                     and bom['dependencies'] == [{'ref': root['purl'], 'dependsOn': sorted(declared)}], 'sbom-npm-dependencies')
        expected_ranges = {f'elsa:npm:{field}:{dep}': spec for field in ('dependencies', 'optionalDependencies', 'peerDependencies')
                           for dep, spec in manifest.get(field, {}).items()}
        pair.require({k:v for k,v in props.items() if k.startswith(('elsa:npm:dependencies:', 'elsa:npm:optionalDependencies:',
                    'elsa:npm:peerDependencies:'))} == expected_ranges, 'sbom-declared-ranges')
        files = {item['name']: item['hashes'] for item in bom['components'] if item['type'] == 'file'}
        expected = {filename: [{'alg': 'SHA-256', 'content': item['sha256']}] for filename, item in report['inventory'].items()}
        pair.require(files == expected, 'sbom-file-inventory')
        artifacts[name] = {'bytes': data, 'report': report}
    pair.require(read(folder / 'provenance.json') == provenance(first_proof), 'producer-provenance')
    return {'identity': first_proof, 'artifacts': artifacts}


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
    gen.add_argument('--release-provenance', action='store_true')
    for command in ('verify-staged', 'verify-published'):
        verify = commands.add_parser(command)
        verify.add_argument('--folder', type=Path, required=True)
        for key in ('version', 'commit', 'run'):
            verify.add_argument('--' + key, required=True)
    args = parser.parse_args()
    if args.command == 'generate':
        generate(args.build.resolve(), args.output.resolve(), release_provenance=args.release_provenance)
    elif args.command == 'verify-staged':
        checked_pair(args.folder.resolve(), args.version, args.commit, args.run)
    else:
        verify_published(args.folder.resolve(), args.version, args.commit, args.run, 'feedz')
