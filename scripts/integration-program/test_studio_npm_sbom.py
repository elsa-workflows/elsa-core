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
        receipt = self.proof | {'success': True, 'source_unchanged': True}
        fixture = fixtures.PairContracts()
        fixture.root = self.folder
        self.paths = []
        wasm_integrity = None
        for label, name in [('wasm', pair.WASM), ('react', pair.REACT)]:
            payload = fixture.payload(name, self.proof)
            if wasm_integrity:
                manifest = self.proof | {'package': name, 'wasm_sha512_integrity': wasm_integrity}
                payload['package/' + pair.PROOF] = json.dumps(manifest).encode()
            file = name.lstrip('@').replace('/', '-') + '-' + self.proof['version'] + '.tgz'
            path = fixture.archive(payload, file)
            self.paths.append(path)
            receipt[label] = pair.verify_archive(path, name, self.proof, wasm_integrity=wasm_integrity)
            wasm_integrity = receipt['wasm']['sha512_integrity']
        pair.write_json(self.folder / 'receipt.json', receipt)
        with patch.object(sbom, 'managed_assets', return_value={}), patch.object(sbom, 'bundled_js', side_effect=lambda *_: {}):
            sbom.generate(self.build, self.folder)

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
        file = Path(str(self.paths[1]) + '.cdx.json')
        bom = sbom.read(file)
        bom['metadata']['component']['purl'] = sbom.component('npm', pair.REACT, '3.10.2-preview.41')['purl']
        pair.write_json(file, bom)
        with self.assertRaisesRegex(pair.ProofError, 'published-sbom-binding'):
            self.check()
