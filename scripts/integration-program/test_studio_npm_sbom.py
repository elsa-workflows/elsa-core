"""Exercise release inventory binding independently of the real SDK build."""
import json
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

    def test_packed_monaco_banner_supplies_version(self):
        root = 'package/_content/BlazorMonaco/lib/monaco-editor/min/vs/'
        members = {root + 'nls.messages.it.js': b'/* Version: 0.54.0(' + b'a' * 40 + b') */',
                   root + 'editor.main.js': b'packed'}
        found = sbom.bundled_js(self.build, self.build, members, pair.WASM)
        editor = found['pkg:npm/monaco-editor@0.54.0']
        self.assertEqual({row['location'] for row in editor['evidence']['occurrences']},
                         {key.removeprefix('package/') for key in members})
