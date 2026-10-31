import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import prepare_paired_blazor_host as host


class SourceProofGuardTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.output = Path(temporary.name)
        imports = self.output / 'elsa-extensions' / host.IMPORTS
        imports.parent.mkdir(parents=True)
        imports.write_bytes(b'@using Known\n')
        baseline = b'@using Known\n' + host.UNUSED_IMPORT + b'\n'
        reader = patch.object(host.subprocess, 'check_output', return_value=baseline)
        reader.start()
        self.addCleanup(reader.stop)
        self.receipt = {
            'sourceCommits': dict(host.PINS),
            'result': {'providerTypeResolves': True, 'featureMatches': True, 'httpRoundtrip': True,
                       'descriptorCount': 1, 'descriptorName': 'Synthetic',
                       'descriptorType': 'SyntheticWorkflowContextProvider, ContractProbe'},
            'sourcePatch': {'path': host.IMPORTS, 'removedLine': host.UNUSED_IMPORT.decode(),
                            'beforeSha256': hashlib.sha256(baseline).hexdigest(),
                            'afterSha256': hashlib.sha256(imports.read_bytes()).hexdigest()},
        }
        (self.output / 'build.log').write_text('PAIR_PROOF=' + json.dumps(self.receipt['result']) + '\n')
        self.save_receipt()

    def save_receipt(self):
        (self.output / 'evidence.json').write_text(json.dumps(self.receipt))

    def source_git(self, source, command, *args):
        name = source.name[len('elsa-'):]
        if command == 'rev-parse':
            return host.PINS[name]
        return f'M {host.IMPORTS}' if name == 'extensions' else ''

    def test_accepts_verified_sources_but_rejects_untracked_drift(self):
        with patch.object(host, 'git', side_effect=self.source_git):
            host.validate_source_proof(self.output)
        with patch.object(host, 'git', side_effect=lambda source, command, *args: self.source_git(source, command, *args) if command == 'rev-parse' else '?? unexpected.cs'):
            with self.assertRaisesRegex(ValueError, 'source drift'):
                host.validate_source_proof(self.output)

    def test_rejects_wrong_pin_or_unresolved_provider(self):
        for key in ('pin', 'provider'):
            with self.subTest(key=key):
                self.receipt['sourceCommits'] = dict(host.PINS)
                self.receipt['result']['providerTypeResolves'] = key != 'provider'
                if key == 'pin':
                    self.receipt['sourceCommits']['core'] = 'wrong'
                self.save_receipt()
                with self.assertRaisesRegex(ValueError, 'paired source proof'):
                    host.validate_source_proof(self.output)

    def test_rejects_changed_import_even_when_git_status_is_identical(self):
        (self.output / 'elsa-extensions' / host.IMPORTS).write_bytes(b'@using Unexpected\n')
        with patch.object(host, 'git', side_effect=self.source_git):
            with self.assertRaisesRegex(ValueError, 'import patch drift'):
                host.validate_source_proof(self.output)

    def test_rejects_forged_patch_receipt_and_mismatched_probe_output(self):
        path = self.output / 'elsa-extensions' / host.IMPORTS
        path.write_bytes(b'@using Unexpected\n')
        self.receipt['sourcePatch']['afterSha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
        self.save_receipt()
        with patch.object(host, 'git', side_effect=self.source_git):
            with self.assertRaisesRegex(ValueError, 'import patch drift'):
                host.validate_source_proof(self.output)
        (self.output / 'build.log').write_text('PAIR_PROOF={}\n')
        with self.assertRaisesRegex(ValueError, 'retained probe output'):
            host.validate_source_proof(self.output)


class BuildEnvironmentTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.parent = Path(temporary.name)
        self.output = self.parent / 'proof'
        (self.output / 'ContractProbe').mkdir(parents=True)
        self.shared = self.output / 'ContractProbe' / 'HttpProbe.cs'
        self.shared.write_bytes((host.CONTRACT_FIXTURE / 'HttpProbe.cs').read_bytes())

    def test_rejects_root_and_parent_build_configuration(self):
        host.validate_build_environment(self.output)
        for directory in (self.output, self.parent):
            for name in host.AMBIENT_CONFIG:
                with self.subTest(directory=directory, name=name):
                    path = directory / name
                    path.write_text('unexpected')
                    with self.assertRaisesRegex(ValueError, 'Ambient build configuration'):
                        host.validate_build_environment(self.output)
                    path.unlink()

    def test_rejects_modified_shared_contract_source(self):
        self.shared.write_text('unexpected source')
        with self.assertRaisesRegex(ValueError, 'Shared contract source'):
            host.validate_build_environment(self.output)

    def test_failed_preparation_cleans_only_created_outputs_and_can_retry(self):
        preserved = self.output / 'evidence.json'
        preserved.write_text('retained receipt')
        with patch.object(host, 'validate_source_proof'), patch.object(host.subprocess, 'Popen', side_effect=OSError('synthetic launch failure')):
            for _ in range(2):
                with self.assertRaisesRegex(OSError, 'synthetic launch failure'):
                    host.prepare(self.output)
                self.assertFalse((self.output / 'UiProbe').exists())
                self.assertFalse((self.output / 'PairedDevelopment.sln').exists())
                self.assertEqual(preserved.read_text(), 'retained receipt')
                self.assertTrue(self.shared.is_file())
                self.assertTrue((self.output / 'blazor-build.log').is_file())


if __name__ == '__main__':
    unittest.main()
