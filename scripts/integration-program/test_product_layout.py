"""Keep physical path resolution separate from frozen source identities."""
import json
from pathlib import Path
import tempfile
import unittest

from product_layout import LAYOUT, current_path, map_path, relocation_baseline, verified_relocation_identity

ROOT = Path(__file__).resolve().parents[2]


class ProductLayoutTests(unittest.TestCase):
    def test_mapping_is_idempotent_for_shared_and_current_product_paths(self):
        for path in ('Elsa.sln', 'Directory.Build.props', '.github/workflows/pr.yml',
                     'core/src/modules/A.cs', 'extensions/test/A.cs', 'studio/docs/README.md',
                     'docs/adr/toc.md'):
            self.assertEqual(path, map_path(path))

    def test_specific_products_win_over_general_core_prefix(self):
        for before, after in [('src/extensions/A', 'extensions/src/A'),
                              ('src/studio/A', 'studio/src/A'),
                              ('src/modules/A', 'core/src/modules/A'),
                              ('test/studio/A', 'studio/test/A'),
                              ('doc/adr/A', 'docs/adr/A')]:
            self.assertEqual(after, map_path(before))
        self.assertEqual('core/samples/elsa-script/test-flowchart.txt', map_path('test-flowchart.txt'))

    def test_historical_checkout_is_unchanged_and_current_resolution_does_not_mutate_receipt(self):
        receipt = {'destination': 'src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj'}
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.assertEqual(root / receipt['destination'], current_path(root, receipt['destination']))
            config = root / 'scripts/integration-program/product-layout.json'
            config.parent.mkdir(parents=True)
            config.write_text('{}')
            self.assertEqual(root / 'extensions/src/communication/Elsa.Slack/Elsa.Slack.csproj',
                             current_path(root, receipt['destination']))
        self.assertEqual('src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj', receipt['destination'])

    def test_verified_tooling_transform_rejects_extra_edits_mode_drift_and_unlisted_paths(self):
        before, mode = relocation_baseline(ROOT, '.github/dependabot.yml')
        after = before.replace(b'src/extensions', b'extensions/src').replace(b'src/studio', b'studio/src')
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'dependabot.yml'
            path.write_bytes(after)
            path.chmod(0o755 if mode == '100755' else 0o644)
            self.assertIsNotNone(verified_relocation_identity(ROOT, '.github/dependabot.yml', path))
            self.assertIsNone(verified_relocation_identity(ROOT, 'unknown.yml', path))
            path.write_bytes(after + b'# unrelated edit\n')
            self.assertIsNone(verified_relocation_identity(ROOT, '.github/dependabot.yml', path))
            path.write_bytes(after)
            path.chmod(0o644 if mode == '100755' else 0o755)
            self.assertIsNone(verified_relocation_identity(ROOT, '.github/dependabot.yml', path))

    def test_strict_admission_catalog_has_only_existing_current_projects_and_case_sources(self):
        manifest = json.loads((ROOT / 'scripts/integration-program/admission-proof-cases.json').read_text())
        for row in [*manifest['testProjects'], *manifest['buildProjects']]:
            path = row['project'] if isinstance(row, dict) else row
            self.assertTrue((ROOT / path).is_file(), path)
            self.assertEqual(path, map_path(path))
        for row in manifest['cases']:
            self.assertTrue((ROOT / row['project']).is_file(), row['project'])
            self.assertEqual(row['project'], map_path(row['project']))

    def test_frozen_release_source_paths_are_preserved(self):
        release = json.loads((ROOT / 'docs/integration-program/release-units.json').read_text())
        for unit in release['release_units']:
            self.assertTrue(unit['source']['project_path'].startswith('src/'))
        contract = json.loads((ROOT / 'scripts/integration-program/secrets-api-studio-contract.json').read_text())
        self.assertTrue(all(path.startswith('src/') for path in contract['sourcePaths'].values()))
        self.assertTrue(all((ROOT / path).exists() for path in contract['importCandidateSourcePaths'].values()))


if __name__ == '__main__':
    unittest.main()
