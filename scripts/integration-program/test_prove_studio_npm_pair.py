"""Synthetic boundary contracts; real host/consumer proof is a separate gate."""
import copy
import base64
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
import unittest
from unittest.mock import patch

import prove_studio_npm_pair as pair

FIXTURE_INTEGRITY = 'sha512-' + base64.b64encode(bytes(64)).decode()

class PairContracts(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        self.proof = pair.identity('a' * 40, 'b' * 40, '0.0.0-proof.12.1', '12', '1')

    def payload(self, name=pair.WASM):
        package = {'name': name, 'version': self.proof['version'], 'repository': {'type': 'git', 'url': pair.REPOSITORY},
                   'gitHead': self.proof['source_commit']}
        manifest = self.proof | {'package': name}
        if name == pair.WASM:
            payload = {'index.html': b'<script src="_framework/blazor.webassembly.js"></script>',
                       'appsettings.json': b'{}', pair.ASSETS[3]: b'css',
                       '_content/shell/asset.js': b'asset', '_framework/blazor.webassembly.js': b'loader',
                       '_framework/dotnet.js': b'runtime', '_framework/dotnet.native.123.wasm': b'\x00asm-native',
                       '_framework/Elsa.Studio.Host.CustomElements.123.wasm': b'\x00asm-managed'}
        else:
            package.update(main='./dist/main.cjs', module='./dist/main.js',
                exports={'.': {'import': './dist/main.js', 'require': './dist/main.cjs'}},
                dependencies={pair.WASM: self.proof['version']}, scripts={'postinstall': 'node scripts/copy-elsa-studio-wasm.js',
                    'copy:elsa-studio-wasm': 'node scripts/copy-elsa-studio-wasm.js'})
            manifest['wasm_sha512_integrity'] = FIXTURE_INTEGRITY
            payload = {'dist/main.cjs': b'cjs', 'dist/main.js': b'esm', 'scripts/copy-elsa-studio-wasm.js': b'copy'}
        return {'package/' + key: value for key, value in payload.items()} | {
            'package/package.json': json.dumps(package).encode(), 'package/' + pair.PROOF: json.dumps(manifest).encode()}

    def archive(self, payload, name='fixture.tgz', extra=None):
        path = self.root / name
        with tarfile.open(path, 'w:gz') as archive:
            for key, data in payload.items():
                member = tarfile.TarInfo(key)
                member.size = len(data)
                archive.addfile(member, io.BytesIO(data))
            if extra:
                archive.addfile(extra)
        return path

    def verify(self, payload, name=pair.WASM, **kwargs):
        return pair.verify_archive(self.archive(payload), name, self.proof,
                                   wasm_integrity=FIXTURE_INTEGRITY if name == pair.REACT else None, **kwargs)

    def mutate_json(self, payload, key, mutate):
        result = copy.deepcopy(payload)
        value = json.loads(result[key])
        mutate(value)
        result[key] = json.dumps(value).encode()
        return result

    def test_valid_pair_and_original_integrity(self):
        for name in (pair.WASM, pair.REACT):
            with self.subTest(name=name):
                payload = self.payload(name)
                expected = {key.removeprefix('package/'): {'size': len(value), 'sha256': pair.sha256(value)}
                            for key, value in payload.items()}
                report = self.verify(payload, name, expected=expected)
                self.assertTrue(report['sha512_integrity'].startswith('sha512-'))
                self.assertEqual(expected, report['inventory'])

    def test_invalid_selection(self):
        valid = ['a' * 40, 'b' * 40, '0.0.0-proof.12.1', '12', '1']
        for index, wrong in ((0, 'main'), (0, 'A' * 40), (1, 'b' * 39), (2, '3.10.0'),
                             (2, '0.0.0-proof.13.1'), (3, '012'), (4, '0')):
            with self.subTest(index=index, wrong=wrong):
                args = valid.copy()
                args[index] = wrong
                with self.assertRaises(pair.ProofError):
                    pair.identity(*args)

    def test_identity_source_and_manifest_mutations_fail(self):
        original = self.payload()
        cases = [('package/package.json', lambda p: p.update(name=pair.REACT)),
                 ('package/package.json', lambda p: p.update(version='0.0.0-proof.13.1')),
                 ('package/package.json', lambda p: p.update(gitHead='c' * 40)),
                 ('package/package.json', lambda p: p.update(repository={'type': 'git', 'url': 'foreign'})),
                 ('package/' + pair.PROOF, lambda p: p.update(source_commit='c' * 40)),
                 ('package/' + pair.PROOF, lambda p: p.update(source_tree='c' * 40)),
                 ('package/' + pair.PROOF, lambda p: p.update(run_id='13')),
                 ('package/' + pair.PROOF, lambda p: p.update(private_path='/sensitive'))]
        for key, mutate in cases:
            with self.subTest(key=key, mutate=mutate):
                with self.assertRaises(pair.ProofError):
                    self.verify(self.mutate_json(original, key, mutate))
        for key in ('package/package.json', 'package/' + pair.PROOF):
            payload = original.copy()
            del payload[key]
            with self.assertRaises(pair.ProofError):
                self.verify(payload)

    def test_missing_corrupt_extra_assets_and_inventory_fail(self):
        original = self.payload()
        for key in list(original)[:8]:
            with self.subTest(key=key):
                missing = original.copy()
                del missing[key]
                with self.assertRaises(pair.ProofError):
                    self.verify(missing)
        payload = original | {'package/_framework/dotnet.native.123.wasm': b'not-wasm'}
        with self.assertRaisesRegex(pair.ProofError, 'wasm-managed-assets'):
            self.verify(payload)
        with self.assertRaisesRegex(pair.ProofError, 'archive-inventory'):
            self.verify(original | {'package/extra': b'extra'}, expected={})
        with self.assertRaisesRegex(pair.ProofError, 'archive-integrity'):
            self.verify(original, integrity='sha512-wrong')
        with self.assertRaises(pair.ProofError):
            pair.verify_archive(self.root / 'absent.tgz', pair.WASM, self.proof)

    def test_host_relative_reference_must_exist(self):
        payload = self.payload() | {'package/index.html': b'<link href="_content/missing.css">'}
        with self.assertRaisesRegex(pair.ProofError, 'wasm-host-reference'):
            self.verify(payload)

    def test_dependency_entrypoints_and_lifecycle_fail_closed(self):
        original = self.payload(pair.REACT)
        mutations = [lambda p: p.update(dependencies={}),
            lambda p: p['dependencies'].update({pair.WASM: '^' + self.proof['version']}),
            lambda p: p['dependencies'].update({pair.WASM: '0.0.0-proof.13.1'}),
            lambda p: p.update(main='./dist/missing.cjs'),
            lambda p: p.update(exports={'.': {'import': './dist/../../outside.js'}}),
            lambda p: p.update(scripts={})]
        for mutate in mutations:
            with self.subTest(mutate=mutate), self.assertRaises(pair.ProofError):
                self.verify(self.mutate_json(original, 'package/package.json', mutate), pair.REACT)
        for key in ('package/dist/main.cjs', 'package/dist/main.js', 'package/scripts/copy-elsa-studio-wasm.js'):
            payload = original.copy()
            del payload[key]
            with self.assertRaises(pair.ProofError):
                self.verify(payload, pair.REACT)
        with self.assertRaises(pair.ProofError):
            self.verify(self.mutate_json(original, 'package/' + pair.PROOF,
                        lambda p: p.update(wasm_sha512_integrity='other')), pair.REACT)

    def test_archive_paths_duplicates_and_links_fail(self):
        for bad in ('../escape', 'package/../escape', 'package\\escape', '/package/escape'):
            with self.subTest(bad=bad), self.assertRaises(pair.ProofError):
                self.verify(self.payload() | {bad: b'x'})
        for kind in ('duplicate', 'link'):
            member = tarfile.TarInfo('package/package.json' if kind == 'duplicate' else 'package/link')
            if kind == 'link':
                member.type = tarfile.SYMTYPE
                member.linkname = '../outside'
            path = self.archive(self.payload(), extra=member)
            with self.assertRaises(pair.ProofError):
                pair.verify_archive(path, pair.WASM, self.proof)

    def test_expected_package_and_pair_integrity_are_required(self):
        path = self.archive(self.payload(pair.REACT))
        for integrity in (None, '', 'sha512-fixture', 'sha256-' + 'a' * 64):
            with self.subTest(integrity=integrity), self.assertRaisesRegex(pair.ProofError, 'paired-integrity-missing'):
                pair.verify_archive(path, pair.REACT, self.proof, wasm_integrity=integrity)
        with self.assertRaisesRegex(pair.ProofError, 'package-id'):
            pair.verify_archive(path, '@elsa-workflows/unknown', self.proof)

    def test_local_lock_requires_both_exact_original_archives(self):
        archives, packages = {}, {}
        for name in (pair.WASM, pair.REACT):
            path = self.root / (name.rsplit('/', 1)[1] + '.tgz')
            report = {'version': self.proof['version'], 'sha512_integrity': 'sha512-fixture'}
            archives[name] = path, report
            packages['node_modules/' + name] = {'version': report['version'], 'integrity': report['sha512_integrity'],
                                                 'resolved': 'file:' + path.name}
        lock = {'packages': packages}
        self.assertEqual(set(archives), set(pair.verify_local_lock(lock, archives, self.root)))
        for field, value in (('resolved', 'https://registry.npmjs.org/elsa.tgz'), ('integrity', 'wrong'),
                             ('version', 'other'), ('resolved', 'file:wrong.tgz')):
            bad = copy.deepcopy(lock)
            bad['packages']['node_modules/' + pair.WASM][field] = value
            with self.subTest(field=field), self.assertRaises(pair.ProofError):
                pair.verify_local_lock(bad, archives, self.root)
        for locator in ('node_modules/extra/node_modules/' + pair.WASM, 'node_modules/@elsa-workflows/other'):
            bad = copy.deepcopy(lock)
            bad['packages'][locator] = packages['node_modules/' + pair.WASM]
            with self.assertRaises(pair.ProofError):
                pair.verify_local_lock(bad, archives, self.root)
        del packages['node_modules/' + pair.REACT]
        with self.assertRaises(pair.ProofError):
            pair.verify_local_lock(lock, archives, self.root)

    def test_workspace_preserves_all_third_party_lock_bytes(self):
        workspace = self.root / 'workspace'
        (workspace / pair.WRAPPER).mkdir(parents=True)
        package = json.loads(self.payload(pair.REACT)['package/package.json'])
        pair.write_json(workspace / pair.WRAPPER / 'package.json', package)
        third_party = {'version': '1.2.3', 'resolved': 'https://example/third.tgz', 'integrity': 'sha512-third'}
        pair.write_json(workspace / 'package-lock.json', {'packages': {
            'node_modules/third': third_party, pair.WRAPPER.as_posix(): copy.deepcopy(package),
            'node_modules/' + pair.WASM: {'resolved': 'foreign'}}})
        pair.stage_workspace(workspace, self.root / 'wasm.tgz', {'sha512_integrity': 'sha512-fixture'}, self.proof)
        lock = json.loads((workspace / 'package-lock.json').read_text())
        self.assertEqual(third_party, lock['packages']['node_modules/third'])
        self.assertTrue(lock['packages']['node_modules/' + pair.WASM]['resolved'].startswith('file:'))

    def test_real_offline_npm_ci_consumes_derived_lock_with_normal_lifecycle(self):
        # Tiny actual npm producer/consumer boundary; synthetic assets do not
        # substitute for the dedicated real CustomElements hosted proof.
        consumer = self.root / 'consumer'
        consumer.mkdir()
        wasm_path = self.archive(self.payload(), 'wasm.tgz')
        wasm = pair.verify_archive(wasm_path, pair.WASM, self.proof)
        react_payload = self.mutate_json(self.payload(pair.REACT), 'package/' + pair.PROOF,
            lambda p: p.update(wasm_sha512_integrity=wasm['sha512_integrity']))
        react_payload['package/scripts/copy-elsa-studio-wasm.js'] = (
            pair.ROOT / pair.WORKSPACE / pair.WRAPPER / 'scripts/copy-elsa-studio-wasm.js').read_bytes()
        react_payload = self.mutate_json(react_payload, 'package/package.json',
                                        lambda p: p.update(type='module'))
        react_path = self.archive(react_payload, 'react.tgz')
        react = pair.verify_archive(react_path, pair.REACT, self.proof, wasm_integrity=wasm['sha512_integrity'])
        third_path = self.archive({'package/package.json': b'{"name":"fixture-third","version":"1.2.3"}'}, 'third.tgz')
        third_integrity = pair.read_archive(third_path)[0]['sha512_integrity']
        third = {'version': '1.2.3', 'resolved': 'https://example.invalid/fixture-third/-/fixture-third-1.2.3.tgz',
                 'integrity': third_integrity}
        unused_path = self.archive({'package/package.json': b'{"name":"producer-only-sentinel","version":"1.0.0"}'}, 'unused.tgz')
        unused = {'version': '1.0.0', 'resolved': 'https://example.invalid/producer-only-sentinel/-/sentinel.tgz',
                  'integrity': pair.read_archive(unused_path)[0]['sha512_integrity'], 'dev': True}
        producer = {'packages': {'': {'workspaces': ['wrappers/*']},
            'node_modules/fixture-third': third, 'node_modules/producer-only-sentinel': unused,
            'node_modules/' + pair.REACT: {'link': True, 'resolved': str(pair.WRAPPER)},
            pair.WRAPPER.as_posix(): {'name': pair.REACT}}}
        archives = {pair.WASM: (wasm_path, wasm), pair.REACT: (react_path, react)}
        package = {'name': 'tiny-offline-consumer', 'private': True,
            'dependencies': {pair.WASM: 'file:../wasm.tgz', pair.REACT: 'file:../react.tgz', 'fixture-third': '1.2.3'},
            'devDependencies': {}}
        pair.write_json(consumer / 'package.json', package)
        lock = pair.consumer_lock(producer, package, archives, consumer)
        pair.write_json(consumer / 'package-lock.json', lock)
        self.assertEqual(third, lock['packages']['node_modules/fixture-third'])
        self.assertNotIn(pair.WRAPPER.as_posix(), lock['packages'])
        (consumer / '.npmrc').write_text('@elsa-workflows:registry=http://127.0.0.1:9\nregistry=http://127.0.0.1:9\n')
        private = self.root / 'private'
        private.mkdir()
        runner = pair.Runner(private, {'commands': []})
        runner.run('cache-third-fixture', ['npm', 'cache', 'add', str(third_path), '--offline'], consumer)
        runner.run('cache-unused-fixture', ['npm', 'cache', 'add', str(unused_path), '--offline'], consumer)
        runner.run('lock-only-fixture', ['npm', 'install', '--package-lock-only', '--offline', '--ignore-scripts=false'], consumer)
        lock = pair.normalized_consumer_lock(lock, json.loads((consumer / 'package-lock.json').read_text()))
        pair.write_json(consumer / 'package-lock.json', lock)
        self.assertNotIn('node_modules/producer-only-sentinel', lock['packages'])
        self.assertEqual(third, lock['packages']['node_modules/fixture-third'])
        runner.run('offline-fixture', ['npm', 'ci', '--offline', '--ignore-scripts=false'], consumer)
        self.assertTrue(pair.installed_assets(consumer, wasm)['lifecycle_scripts_enabled'])
        self.assertEqual('1.2.3', json.loads((consumer / 'node_modules/fixture-third/package.json').read_text())['version'])
        self.assertFalse((consumer / 'node_modules/producer-only-sentinel').exists())
        pair.verify_local_lock(json.loads((consumer / 'package-lock.json').read_text()), archives, consumer)

    def test_real_workspace_install_uses_staged_exact_local_wasm_archive(self):
        workspace = self.root / 'workspace'
        wrapper = workspace / pair.WRAPPER
        (wrapper / 'scripts').mkdir(parents=True)
        payload = self.payload()
        wasm_path = self.archive(payload, 'wasm.tgz')
        wasm = pair.verify_archive(wasm_path, pair.WASM, self.proof)
        package = json.loads(self.payload(pair.REACT)['package/package.json'])
        package['type'] = 'module'
        pair.write_json(wrapper / 'package.json', package)
        shutil.copyfile(pair.ROOT / pair.WORKSPACE / pair.WRAPPER / 'scripts/copy-elsa-studio-wasm.js',
                        wrapper / 'scripts/copy-elsa-studio-wasm.js')
        root_package = {'name': 'root', 'private': True, 'workspaces': ['wrappers/*']}
        pair.write_json(workspace / 'package.json', root_package)
        pair.write_json(workspace / 'package-lock.json', {'name': 'root', 'lockfileVersion': 3, 'requires': True,
            'packages': {'': root_package, pair.WRAPPER.as_posix(): copy.deepcopy(package),
                'node_modules/' + pair.REACT: {'resolved': pair.WRAPPER.as_posix(), 'link': True},
                'node_modules/' + pair.WASM: {'version': '0.0.0', 'resolved': 'https://invalid/old.tgz'}}})
        pair.stage_workspace(workspace, wasm_path, wasm, self.proof)
        (workspace / '.npmrc').write_text('@elsa-workflows:registry=http://127.0.0.1:9\nregistry=http://127.0.0.1:9\n')
        private = self.root / 'private'
        private.mkdir()
        runner = pair.Runner(private, {'commands': []})
        runner.run('workspace-fixture', ['npm', 'ci', '--offline', '--ignore-scripts=false'], workspace)
        self.assertEqual(wasm['inventory'], pair.files(workspace / 'node_modules' / pair.WASM))
        for asset in pair.ASSETS:
            self.assertTrue((wrapper / 'public' / asset).exists())

    def test_failure_receipt_retains_closed_reason_and_final_source_drift(self):
        controller = self.root / 'controller'
        controller.mkdir()
        for drift in (False, True):
            with self.subTest(drift=drift):
                output = self.root / str(drift)
                git_values = ['b' * 40, 'a' * 40, '', 'a' * 40, ' M source.py' if drift else '']
                with patch.object(pair, 'git', side_effect=git_values), \
                     patch.object(pair.Runner, 'run', side_effect=pair.ProofError('command-failed')):
                    report = pair.prove(controller, output, 'a' * 40, self.proof['version'], '12', '1')
                self.assertFalse(report['success'])
                self.assertFalse(report['published'])
                self.assertFalse(report['publisher_authority'])
                self.assertEqual(not drift, report['source_unchanged'])
                self.assertEqual('command-failed', report['failure_code'])
                self.assertEqual(report, json.loads((output / 'retained/receipt.json').read_text()))
                if drift:
                    self.assertEqual('source-checkout', report['source_failure_code'])

    def test_offline_normalization_cannot_introduce_or_change_dependency_identity(self):
        record = {'version': '1.2.3', 'resolved': 'https://example/third.tgz', 'integrity': FIXTURE_INTEGRITY,
                  'dependencies': {'child': '^1.0.0'}, 'dev': True}
        candidate = {'packages': {'': {'name': 'consumer'}, 'node_modules/third': record,
                                  'node_modules/unused': {'version': '1.0.0'}}}
        normalized = {'packages': {'': {'name': 'consumer'}, 'node_modules/third': record | {'dev': False}}}
        actual = pair.normalized_consumer_lock(candidate, normalized)
        self.assertEqual(record, actual['packages']['node_modules/third'])
        self.assertNotIn('node_modules/unused', actual['packages'])
        for field, value in (('version', '2.0.0'), ('resolved', 'https://other/third.tgz'),
                             ('integrity', 'changed'), ('dependencies', {'child': '^2.0.0'})):
            bad = copy.deepcopy(normalized)
            bad['packages']['node_modules/third'][field] = value
            with self.subTest(field=field), self.assertRaises(pair.ProofError):
                pair.normalized_consumer_lock(candidate, bad)
        bad = copy.deepcopy(normalized)
        bad['packages']['node_modules/new'] = {'version': '1.0.0'}
        with self.assertRaisesRegex(pair.ProofError, 'consumer-lock-new-package'):
            pair.normalized_consumer_lock(candidate, bad)

    def test_actual_module_smokes_reject_missing_or_noncallable_exports(self):
        package = self.root / 'node_modules' / pair.REACT
        package.mkdir(parents=True)
        pair.write_json(package / 'package.json', {'name': pair.REACT, 'type': 'module',
            'exports': {'.': {'import': './index.js', 'require': './index.cjs'}}})
        pair.module_smoke_scripts(self.root)
        for invalid in (False, True):
            names = pair.EXPORTS[:-1] if invalid else pair.EXPORTS
            (package / 'index.js').write_text('\n'.join(f'export function {name}() {{}}' for name in names))
            (package / 'index.cjs').write_text('\n'.join(f'exports.{name} = function() {{}};' for name in names))
            for script in ('imports.mjs', 'require.cjs'):
                result = subprocess.run(['node', script], cwd=self.root, capture_output=True)
                with self.subTest(invalid=invalid, script=script):
                    self.assertEqual(invalid, result.returncode != 0)
        (package / 'index.js').write_text('\n'.join(f'export const {name} = 42;' for name in pair.EXPORTS))
        self.assertNotEqual(0, subprocess.run(['node', 'imports.mjs'], cwd=self.root, capture_output=True).returncode)

    def test_vite_output_must_retain_every_actual_asset_byte(self):
        wasm = self.verify(self.payload())
        built = pair.asset_inventory(wasm)
        pair.verify_vite_assets(built, wasm)
        for path in built:
            missing = built.copy()
            del missing[path]
            with self.subTest(path=path), self.assertRaisesRegex(pair.ProofError, 'consumer-vite-assets'):
                pair.verify_vite_assets(missing, wasm)
        bad = built | {'appsettings.json': {'size': 1, 'sha256': 'wrong'}}
        with self.assertRaises(pair.ProofError):
            pair.verify_vite_assets(bad, wasm)

    def test_installed_assets_require_consumer_public_and_exact_bytes(self):
        payload = self.payload()
        report = self.verify(payload)
        for folder in (self.root / 'node_modules' / pair.WASM, self.root / 'public'):
            for path, data in payload.items():
                relative = path.removeprefix('package/')
                target = folder / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(data)
        self.assertTrue(pair.installed_assets(self.root, report)['lifecycle_scripts_enabled'])
        asset = self.root / 'public/appsettings.json'
        asset.write_text('wrong')
        with self.assertRaisesRegex(pair.ProofError, 'installed-assets'):
            pair.installed_assets(self.root, report)
        asset.unlink()
        with self.assertRaises(pair.ProofError):
            pair.installed_assets(self.root, report)

    def test_real_copy_helper_nested_install_and_workspace_preserve_bytes_remove_stale(self):
        script = pair.ROOT / pair.WORKSPACE / pair.WRAPPER / 'scripts/copy-elsa-studio-wasm.js'
        for installed in (True, False):
            with self.subTest(installed=installed):
                app = self.root / str(installed)
                wrapper = app / ('node_modules/' + pair.REACT if installed else 'wrappers/react-wrapper')
                wasm = app / 'node_modules' / pair.WASM
                (wrapper / 'scripts').mkdir(parents=True)
                wasm.mkdir(parents=True)
                shutil.copyfile(script, wrapper / 'scripts/copy-elsa-studio-wasm.js')
                pair.write_json(wrapper / 'package.json', {'type': 'module'})
                pair.write_json(wasm / 'package.json', {'name': pair.WASM})
                for path, data in self.payload().items():
                    target = wasm / path.removeprefix('package/')
                    target.parent.mkdir(parents=True, exist_ok=True)
                    target.write_bytes(data)
                destination = (app if installed else wrapper) / 'public'
                stale = destination / '_framework/stale.js'
                stale.parent.mkdir(parents=True)
                stale.write_text('stale')
                subprocess.run(['node', str(wrapper / 'scripts/copy-elsa-studio-wasm.js')], cwd=wrapper,
                               env=os.environ | {'INIT_CWD': str(app)}, check=True, capture_output=True)
                self.assertFalse(stale.exists())
                for asset in pair.ASSETS:
                    left = pair.files(wasm / asset) if (wasm / asset).is_dir() else (wasm / asset).read_bytes()
                    right = pair.files(destination / asset) if (destination / asset).is_dir() else (destination / asset).read_bytes()
                    self.assertEqual(left, right)

    def test_runner_strips_authority_and_retains_only_closed_process_result(self):
        private = self.root / 'private'
        private.mkdir()
        receipt = {'commands': []}
        with patch.dict(os.environ, {'GITHUB_TOKEN': 'secret', 'NODE_AUTH_TOKEN': 'secret', 'GITHUB_OUTPUT': '/secret',
                                     'NPM_CONFIG_IGNORE_SCRIPTS': 'true'}):
            runner = pair.Runner(private, receipt)
        self.assertNotIn('GITHUB_TOKEN', runner.env)
        self.assertNotIn('NODE_AUTH_TOKEN', runner.env)
        self.assertNotIn('GITHUB_OUTPUT', runner.env)
        self.assertEqual('false', runner.env['NPM_CONFIG_IGNORE_SCRIPTS'])
        with self.assertRaisesRegex(pair.ProofError, 'command-failed'):
            runner.run('fixture', ['node', '-e', "console.log('/private secret'); process.exit(4)"], self.root)
        self.assertEqual([{'step': 'fixture', 'success': False, 'exit_code': 4}], receipt['commands'])
        self.assertNotIn('/private secret', json.dumps(receipt))


if __name__ == '__main__':
    unittest.main()
