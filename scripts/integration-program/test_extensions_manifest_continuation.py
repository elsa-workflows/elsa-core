"""Offline exact-source admission contracts; no NUKE or generator execution."""
from copy import deepcopy
from pathlib import Path
import unittest

import extensions_manifest_continuation as continuation
import prepare_maintenance_build as maintenance
import product_release_metadata as metadata
import selected_extensions_contract as extensions
import selected_maintenance_39 as maintenance39


class ExtensionsManifestContinuationTests(unittest.TestCase):
    def rows(self, line):
        """Return the registered Extensions continuation and parent rows for a release line."""
        expected = continuation.CONTINUATIONS[line]
        catalog = maintenance.load_candidates()
        rows = {row['commit']: row for row in catalog['candidates']}
        return rows[expected['commit']], rows[expected['parent']]

    def bytes(self, row):
        """Read the registered build-script bytes directly from the selected Git commit."""
        return maintenance.git_bytes(maintenance.ROOT, row['commit'], continuation.PATH)

    def test_two_single_file_continuations_bind_entire_delta_ancestry_and_eight_rehearsal_cells(self):
        """Verify two single file continuations bind entire delta ancestry and eight rehearsal
        cells.
        """
        catalog = maintenance.load_candidates()
        for line, expected in continuation.CONTINUATIONS.items():
            with self.subTest(line=line):
                row, parent = self.rows(line)
                before, after = self.bytes(parent), self.bytes(row)
                self.assertEqual(continuation.corrected_build(before), after)
                continuation.verify_delta(row, parent, row['delta'][0], before, after)
                admitted = next(r for r in maintenance.registered_core_candidates(maintenance.load_register())
                                if r['commit'] == row['commit'])
                maintenance.verify_source(maintenance.ROOT, admitted)
                self.assertEqual(metadata.DESCENDANTS[('extensions', line)], row['commit'])
                self.assertEqual(metadata.bind_source(maintenance.ROOT, 'extensions', line, row['commit'])['tree'], row['tree'])
                extensions.verify_source(maintenance.ROOT, row['commit'])
                with self.assertRaises(ValueError):
                    metadata.bind_source(maintenance.ROOT, 'extensions', line, parent['commit'])
        self.assertEqual(12, len(catalog['candidates']))
        self.assertEqual(8, len(catalog['rehearsal_sources']))
        self.assertEqual({r['commit'] for r in continuation.CONTINUATIONS.values()},
                         {r['commit'] for r in catalog['rehearsal_sources'] if r['product'] == 'extensions' and
                          r['commit'] in {x['commit'] for x in continuation.CONTINUATIONS.values()}})

    def test_exact_conditional_package_property_preserves_no_version_pack_and_assembly_source(self):
        """Verify exact conditional package property preserves no version pack and assembly
        source.
        """
        for line in continuation.CONTINUATIONS:
            row, parent = self.rows(line)
            before, after = self.bytes(parent), self.bytes(row)
            compile_part = after.split(b'public Configure<DotNetBuildSettings> CompileSettings')[1].split(b'public Configure<DotNetPackSettings>')[0]
            self.assertIn(b'!string.IsNullOrWhiteSpace(Version)', compile_part)
            self.assertIn(b'settings.SetProperty("PackageVersion", Version)', compile_part)
            self.assertNotIn(b'SetVersion(', compile_part)
            self.assertNotIn(b'AssemblyVersion', compile_part)
            self.assertEqual(before.split(b'public Configure<DotNetPackSettings>')[1],
                             after.split(b'public Configure<DotNetPackSettings>')[1])
            self.assertEqual(maintenance.git_bytes(maintenance.ROOT, parent['commit'], 'Directory.Build.props'),
                             maintenance.git_bytes(maintenance.ROOT, row['commit'], 'Directory.Build.props'))
        self.assertEqual(continuation.AFTER_BLOB, maintenance39.contract('extensions')['files'][continuation.PATH])

    def test_protected_control_exception_rejects_foreign_identity_modes_paths_and_other_edits(self):
        """Verify protected control exception rejects foreign identity modes paths and other
        edits.
        """
        row, parent = self.rows('3.8'); before, after = self.bytes(parent), self.bytes(row)
        mutations = [lambda r: r.update(product='studio'), lambda r: r.update(line='3.9'),
                     lambda r: r.update(commit=parent['commit']), lambda r: r.update(tree='f'*40),
                     lambda r: r.update(parents=[]), lambda r: r.update(source_kind='original'),
                     lambda r: r['delta'].append(deepcopy(r['delta'][0])),
                     lambda r: r['delta'][0].update(path='build/Other.cs'),
                     lambda r: r['delta'][0]['after'].update(mode='100755'),
                     lambda r: r['delta'][0]['after'].update(blob='f'*40)]
        for mutate in mutations:
            wrong = deepcopy(row); mutate(wrong)
            with self.subTest(row=wrong), self.assertRaises(ValueError):
                continuation.verify_delta(wrong, parent, wrong['delta'][0], before, after)
        for changed in (after.replace(b'"PackageVersion"', b'"Version"'),
                        after.replace(b'!string.IsNullOrWhiteSpace(Version)', b'true'),
                        after + b'\n// unrelated edit\n'):
            with self.assertRaises(ValueError):
                continuation.verify_delta(row, parent, row['delta'][0], before, changed)
        self.assertFalse(maintenance.maintenance_editable_path(continuation.PATH))

    def test_workflow_runs_contract_in_both_modes_and_filters_consumed_inputs(self):
        """Verify workflow runs contract in both modes and filters consumed inputs."""
        workflow = (maintenance.ROOT / '.github/workflows/product-release-plan.yml').read_text()
        for prefix in ('python3 -m unittest ', 'python3 -O -m unittest '):
            command = next(line for line in workflow.splitlines() if line.strip().startswith(prefix))
            self.assertIn('test_extensions_manifest_continuation', command)
        self.assertIn("'scripts/integration-program/**'", workflow)
        self.assertIn("'docs/integration-program/**'", workflow)


if __name__ == '__main__':
    unittest.main()
