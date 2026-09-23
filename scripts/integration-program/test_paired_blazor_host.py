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
        self.receipt = {
            'sourceCommits': dict(host.PINS),
            'result': {'providerTypeResolves': True},
            'sourcePatch': {'afterSha256': hashlib.sha256(imports.read_bytes()).hexdigest()},
        }
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


if __name__ == '__main__':
    unittest.main()
