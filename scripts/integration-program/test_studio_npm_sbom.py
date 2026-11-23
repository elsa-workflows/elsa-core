"""Exercise release inventory binding independently of the real SDK build."""
import base64
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import prove_studio_npm_pair as pair
import studio_npm_sbom as sbom
import test_prove_studio_npm_pair as fixtures


class PackedLibraryEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.build = Path(self.temporary.name).resolve()
        self.folder = self.build / 'retained'
        self.folder.mkdir()
        event = self.build / 'event.json'
        pair.write_json(event, {'inputs': {'publish_npm': 'true'}})
        self.enterContext(patch.dict(os.environ, {'GITHUB_EVENT_PATH': str(event), 'GITHUB_EVENT_NAME': 'workflow_dispatch',
                          'GITHUB_REPOSITORY_ID': '123', 'GITHUB_REPOSITORY_OWNER_ID': '456', 'RUNNER_ENVIRONMENT': 'github-hosted'}))
        self.proof = pair.release_candidate_identity('a' * 40, 'b' * 40, '3.10.2-preview.42', '12', '2')
        self.paths = self.stage(self.build, self.folder, self.proof)

    def stage(self, build, folder, proof):
        receipt = proof | {'success': True, 'source_unchanged': True}
        fixture = fixtures.PairContracts()
        fixture.root = folder
        paths = []
        wasm_integrity = None
        for label, name in [('wasm', pair.WASM), ('react', pair.REACT)]:
            payload = fixture.payload(name, proof)
            if wasm_integrity:
                manifest = proof | {'package': name, 'wasm_sha512_integrity': wasm_integrity}
                payload['package/' + pair.PROOF] = json.dumps(manifest).encode()
            file = name.lstrip('@').replace('/', '-') + '-' + proof['version'] + '.tgz'
            path = fixture.archive(payload, file)
            paths.append(path)
            receipt[label] = pair.verify_archive(path, name, proof, wasm_integrity=wasm_integrity)
            wasm_integrity = receipt['wasm']['sha512_integrity']
        pair.write_json(folder / 'receipt.json', receipt)
        with patch.object(sbom, 'managed_assets', return_value={}), patch.object(sbom, 'bundled_js', side_effect=lambda *_: {}):
            sbom.generate(build, folder, release_provenance=True)
        return paths

    def check(self):
        return sbom.checked_pair(self.folder, self.proof['version'], self.proof['source_commit'], self.proof['run_id'])

    def test_exact_pair_and_files(self):
        self.assertEqual(set(self.check()['artifacts']), {pair.WASM, pair.REACT})

    def test_changed_tarball_fails(self):
        with self.paths[0].open('ab') as archive:
            archive.write(b'changed')
        with self.assertRaisesRegex(pair.ProofError, 'published-sbom-binding'):
            self.check()

    def test_missing_inventory_entry_fails(self):
        file = Path(str(self.paths[1]) + '.cdx.json')
        bom = sbom.read(file)
        bom['components'] = [item for item in bom['components'] if item['name'] != 'package.json']
        pair.write_json(file, bom)
        with self.assertRaisesRegex(pair.ProofError, 'sbom-file-inventory'):
            self.check()

    def test_mixed_package_versions_fail(self):
        other = self.build / 'other'
        folder = other / 'retained'
        folder.mkdir(parents=True)
        proof = self.proof | {'version': '3.10.2-preview.41'}
        _, wrapper = self.stage(other, folder, proof)
        self.paths[1].write_bytes(wrapper.read_bytes())
        Path(str(self.paths[1]) + '.cdx.json').write_bytes(Path(str(wrapper) + '.cdx.json').read_bytes())
        with self.assertRaisesRegex(pair.ProofError, 'published-sbom-binding'):
            self.check()

    def test_false_paired_wasm_hash_and_missing_dependency_edge_fail(self):
        file = Path(str(self.paths[1]) + '.cdx.json')
        original = file.read_bytes()
        for change in ('hash', 'edge'):
            with self.subTest(change=change):
                bom = json.loads(original)
                if change == 'hash':
                    wasm = next(c for c in bom['components'] if c['name'] == 'elsa-studio-wasm')
                    wasm['hashes'][0]['content'] = '0' * 128
                else:
                    bom['dependencies'][0]['dependsOn'] = []
                pair.write_json(file, bom)
                with self.assertRaisesRegex(pair.ProofError, 'sbom-npm-dependencies'):
                    self.check()

    def test_producer_attempt_is_taken_from_original_tarballs(self):
        file = self.folder / 'provenance.json'
        data = sbom.read(file)
        self.assertTrue(data['runDetails']['metadata']['invocationId'].endswith('/12/attempts/2'))
        data['runDetails']['metadata']['invocationId'] += '0'
        pair.write_json(file, data)
        with self.assertRaisesRegex(pair.ProofError, 'producer-provenance'):
            self.check()

    def test_provenance_binds_release_selection_and_native_build_type(self):
        predicate = sbom.read(self.folder / 'provenance.json')
        self.assertEqual({'event_name': 'workflow_dispatch', 'repository_id': '123',
                          'repository_owner_id': '456', 'runner_environment': 'github-hosted'},
                         predicate['buildDefinition']['internalParameters']['github'])
        with patch.dict(os.environ, {'GITHUB_REPOSITORY_ID': '321'}), self.assertRaisesRegex(pair.ProofError, 'producer-provenance'):
            self.check()
        pair.write_json(Path(os.environ['GITHUB_EVENT_PATH']), {'inputs': {'publish_npm': 'false'}})
        with self.assertRaisesRegex(pair.ProofError, 'npm-release-not-selected'):
            self.check()

    def test_static_asset_uses_resolved_package_bytes_after_packaging_transform(self):
        source = self.build / 'source'
        obj = source / pair.HOST / 'obj'
        published = obj / 'Release/net10.0'
        published.mkdir(parents=True)
        cache = self.build / 'packages'
        static_file = cache / 'radzen.blazor/9.0.5/staticwebassets/Radzen.Blazor.min.js'
        static_file.parent.mkdir(parents=True)
        static_file.write_bytes(b'shipped javascript')
        pair.write_json(obj / 'project.assets.json', {
            'targets': {'net10.0/browser-wasm': {'Radzen.Blazor/9.0.5': {'type': 'package'}}},
            'project': {'frameworks': {'net10.0': {'downloadDependencies': []}}}, 'packageFolders': {str(cache): {}}})
        pair.write_json(published / 'staticwebassets.publish.json', {'Assets': [{
            'OriginalItemSpec': str(static_file), 'Identity': str(static_file), 'SourceType': 'Package', 'AssetRole': 'Primary',
            'Integrity': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA='}, {
            'OriginalItemSpec': str(static_file), 'Identity': str(obj / 'compressed.br'), 'SourceType': 'Package',
            'AssetRole': 'Alternative', 'Integrity': 'ERERERERERERERERERERERERERERERERERERERERERE='}]})
        assets = sbom.managed_assets(source, self.proof['version'])
        self.assertEqual({('pkg:nuget/Radzen.Blazor@9.0.5', 'Radzen.Blazor', '9.0.5')},
                         assets[pair.sha256(static_file.read_bytes())])
        self.assertNotIn('0' * 64, assets)
        self.assertEqual(assets[pair.sha256(static_file.read_bytes())], assets['1' * 64])

    def test_runtime_boot_data_and_compressed_alternatives_keep_their_runtime_identity(self):
        source = self.build / 'source'
        obj = source / pair.HOST / 'obj'
        published = obj / 'Release/net10.0'
        published.mkdir(parents=True)
        cache = self.build / 'packages'
        name = 'Microsoft.NETCore.App.Runtime.Mono.browser-wasm'
        template = cache / name.lower() / '10.0.8/runtimes/browser-wasm/native/dotnet.js'
        template.parent.mkdir(parents=True)
        template.write_bytes(b'/*! bundlerFriendlyImports */load(/*! dotnetBootConfig */{});')
        generated = published / 'publish.dotnet.js'
        generated.write_bytes(b'load(/*json-start*/{"mainAssemblyName":"test"}/*json-end*/);')
        sha = pair.sha256(generated.read_bytes())
        pair.write_json(obj / 'project.assets.json', {'targets': {'net10.0/browser-wasm': {}}, 'packageFolders': {str(cache): {}},
                        'project': {'frameworks': {'net10.0': {'downloadDependencies': [{'name': name, 'version': '[10.0.8, 10.0.8]'}]}}}})
        def record(target, role, integrity, related):
            return {'Identity': str(target), 'OriginalItemSpec': str(target), 'AssetRole': role,
                    'SourceType': 'Computed', 'RelatedAsset': related, 'Integrity': integrity}
        pair.write_json(published / 'staticwebassets.publish.json', {'Assets': [
            record(published / 'compressed.br', 'Alternative', 'ERERERERERERERERERERERERERERERERERERERERERE=', str(generated)),
            record(generated, 'Primary', base64.b64encode(bytes.fromhex(sha)).decode(), '')]})
        assets = sbom.managed_assets(source, self.proof['version'])
        self.assertEqual({('pkg:nuget/' + name + '@10.0.8', name, '10.0.8')}, assets[sha])
        self.assertEqual(assets[sha], assets['1' * 64])
        generated.write_bytes(b'different loader')
        self.assertFalse(sbom.published_runtime_loader(template, generated))

    def test_vendor_notice_evidence_requires_the_reviewed_path_and_bytes(self):
        catalog = sbom.read(Path(sbom.__file__).with_name('studio-vendor-notices.cdx.json'))
        review = catalog['components'][0]
        review['hashes'] = [{'alg': 'SHA-256', 'content': pair.sha256(b'reviewed')}]
        for package, prefix in ((pair.WASM, ''), (pair.REACT, 'dist/')):
            path = prefix + review['name']
            found = sbom.vendor_components(catalog, {'package/' + path: b'reviewed'}, package)
            self.assertEqual('pkg:npm/oidc-client@1.11.5', found['vendored:auth/oidc-client']['purl'])
            fragment = found['vendored:auth/cryptojs-core-fix']
            self.assertEqual('pkg:npm/crypto-js@3.1.2', fragment['pedigree']['ancestors'][0]['purl'])
            self.assertEqual([{'location': path}], fragment['evidence']['occurrences'])
            self.assertNotIn('version', found['vendored:auth/jsbn'])
            self.assertEqual({}, sbom.vendor_components(catalog, {'package/' + path: b'new vendor build'}, package))
            self.assertEqual({}, sbom.vendor_components(catalog, {'package/elsewhere.js': b'reviewed'}, package))

    def test_partial_dependency_graph_preserves_unknown_relationships(self):
        bom = sbom.read(Path(str(self.paths[0]) + '.cdx.json'))
        self.assertEqual([], bom['dependencies'])
        root = bom['metadata']['component']['bom-ref']
        self.assertEqual([{'aggregate': 'incomplete', 'assemblies': [root], 'dependencies': [root]}], bom['compositions'])

    def test_packed_monaco_banner_supplies_version(self):
        root = 'package/_content/BlazorMonaco/lib/monaco-editor/min/vs/'
        members = {root + 'nls.messages.it.js': b'/* Version: 0.54.0(' + b'a' * 40 + b') */',
                   root + 'editor.main.js': b'packed'}
        found = sbom.bundled_js(self.build, self.build, members, pair.WASM)
        editor = found['pkg:npm/monaco-editor@0.54.0']
        self.assertEqual({row['location'] for row in editor['evidence']['occurrences']},
                         {key.removeprefix('package/') for key in members})
