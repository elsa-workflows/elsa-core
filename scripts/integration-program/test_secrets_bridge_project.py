"""Exercise bridge staging in real old/current filesystems without executing an SDK."""
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

from secrets_bridge_project import (LAYOUT_PATH, TEMPLATE_PREFIX, project_staging_evidence,
                                   stage_bridge_project)


ROOT = Path(__file__).resolve().parents[2]
PROJECTS = (
    'secrets-sqlite-bridge-contract/current-core/CurrentCoreBridgeRunner.csproj',
    'secrets-postgresql-bridge/current-core/PostgreSqlBridgeRunner.csproj',
    'secrets-sqlserver-bridge/current-core/SqlServerBridgeRunner.csproj',
)


class SecretsBridgeProjectTests(unittest.TestCase):
    @staticmethod
    def configure_layout(checkout):
        manifest = checkout / LAYOUT_PATH
        manifest.parent.mkdir(parents=True, exist_ok=True)
        manifest.write_text(json.dumps({'schemaVersion': 1, 'prefixMoves': [['src/', 'core/src/']]}))
        return manifest

    def prepare_template(self, directory, include=TEMPLATE_PREFIX + 'modules/Secrets/Secrets.csproj'):
        tools = directory / 'tools'
        template = tools / 'scripts/integration-program/secrets-sqlite-bridge-contract/current-core/Runner.csproj'
        template.parent.mkdir(parents=True)
        template.write_text(f'<Project><ItemGroup><ProjectReference Include="{include}" /></ItemGroup></Project>')
        (template.parent / 'Program.cs').write_text('fixture source')
        checkout = directory / 'target'
        checkout.mkdir()
        target = checkout / 'src/modules/Secrets/Secrets.csproj'
        target.parent.mkdir(parents=True)
        target.write_text('<Project />')
        return tools, template, checkout, target

    def test_all_three_actual_templates_stage_both_layouts_with_exact_nonreference_bytes(self):
        for relative in PROJECTS:
            template = ROOT / 'scripts/integration-program' / relative
            original = template.read_bytes()
            refs = [node.get('Include') for node in ET.fromstring(original).iter('ProjectReference')]
            self.assertEqual(8, len(refs))
            for current in (False, True):
                with self.subTest(project=relative, current=current), tempfile.TemporaryDirectory() as directory:
                    checkout = Path(directory)
                    prefix = 'core/src/' if current else 'src/'
                    if current:
                        self.configure_layout(checkout)
                    for reference in refs:
                        self.assertTrue(reference.startswith(TEMPLATE_PREFIX))
                        target = checkout / prefix / reference[len(TEMPLATE_PREFIX):]
                        target.parent.mkdir(parents=True, exist_ok=True)
                        target.write_text('<Project />')
                    staged = stage_bridge_project(template, ROOT, checkout)
                    expected = original if current else original.replace(b'Include="../../../../core/src/', b'Include="../../../../src/')
                    self.assertEqual(expected, staged.read_bytes())
                    self.assertEqual(original, template.read_bytes())
                    evidence = project_staging_evidence(template, ROOT, staged, checkout)
                    self.assertEqual(prefix, evidence['coreSourceRoot'])
                    self.assertEqual(hashlib.sha256(original).hexdigest(), evidence['templateSha256'])
                    self.assertEqual(hashlib.sha256(expected).hexdigest(), evidence['stagedSha256'])

    def test_staging_cannot_replace_its_source_template(self):
        with tempfile.TemporaryDirectory() as directory:
            tools, template, _, _ = self.prepare_template(Path(directory))
            before = template.read_bytes()
            with self.assertRaisesRegex(ValueError, 'overlaps its template'):
                stage_bridge_project(template, tools, tools)
            self.assertEqual(before, template.read_bytes())

    def test_current_manifest_never_falls_back_to_existing_old_targets(self):
        with tempfile.TemporaryDirectory() as directory:
            tools, template, checkout, _ = self.prepare_template(Path(directory))
            self.configure_layout(checkout)
            with self.assertRaisesRegex(ValueError, 'both historical and current'):
                stage_bridge_project(template, tools, checkout)
            (checkout / 'src').rename(checkout / 'old-source')
            with self.assertRaisesRegex(ValueError, 'missing or unsafe'):
                stage_bridge_project(template, tools, checkout)

    def test_malformed_or_missing_current_manifest_fails_closed(self):
        for value in ('not-json', '{}', '{"schemaVersion":true,"prefixMoves":[["src/","core/src/"]]}',
                      '{"schemaVersion":1,"prefixMoves":[["src/","core/src/"],"bad"]}',
                      '{"schemaVersion":1,"prefixMoves":[["src/","wrong/"]]}'):
            with self.subTest(value=value), tempfile.TemporaryDirectory() as directory:
                tools, template, checkout, _ = self.prepare_template(Path(directory))
                self.configure_layout(checkout).write_text(value)
                with self.assertRaises(ValueError):
                    stage_bridge_project(template, tools, checkout)
        with tempfile.TemporaryDirectory() as directory:
            tools, template, checkout, _ = self.prepare_template(Path(directory))
            (checkout / 'core/src').mkdir(parents=True)
            with self.assertRaisesRegex(ValueError, 'lacks its layout'):
                stage_bridge_project(template, tools, checkout)

    def test_unexpected_outside_and_symlink_references_are_rejected(self):
        for include in ('../../../../elsewhere/Secret.csproj',
                        TEMPLATE_PREFIX + '../../../../outside.csproj',
                        '/outside.csproj', TEMPLATE_PREFIX + '$(Other)/Secret.csproj'):
            with self.subTest(include=include), tempfile.TemporaryDirectory() as directory:
                tools, template, checkout, _ = self.prepare_template(Path(directory), include)
                with self.assertRaises(ValueError):
                    stage_bridge_project(template, tools, checkout)
        with tempfile.TemporaryDirectory() as directory:
            tools, template, checkout, target = self.prepare_template(Path(directory))
            outside = Path(directory) / 'outside.csproj'
            outside.write_text('<Project />')
            target.unlink()
            target.symlink_to(outside)
            with self.assertRaisesRegex(ValueError, 'missing or unsafe'):
                stage_bridge_project(template, tools, checkout)
            target.unlink()
            target.parent.rename(target.parent.with_name('real'))
            target.parent.symlink_to(target.parent.with_name('real'), target_is_directory=True)
            with self.assertRaisesRegex(ValueError, 'missing or unsafe'):
                stage_bridge_project(template, tools, checkout)


if __name__ == '__main__':
    unittest.main()
