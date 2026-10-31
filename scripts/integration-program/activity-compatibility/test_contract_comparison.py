import copy
import importlib.util
import json
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('activity_contract_runner', Path(__file__).with_name('run.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class ContractComparisonTests(unittest.TestCase):
    def descriptor(self):
        return {'Assembly': 'Example', 'ClrType': 'Example.Activity', 'TypeName': 'Example.Task', 'Version': 1, 'Kind': 'Task', 'Inputs': [{'Name': 'Value', 'Type': 'Collection[Value, Version=1.0.0.0]', 'ContractType': 'Collection<Value, Example, Culture=neutral, PublicKeyToken=null>', 'IsSerializable': True}], 'Outputs': [], 'Ports': []}

    def test_assembly_build_version_alone_does_not_change_contract(self):
        first = self.descriptor()
        second = copy.deepcopy(first)
        second['Inputs'][0]['Type'] = 'Collection[Value, Version=2.0.0.0]'
        self.assertEqual(runner.normalized_descriptor(first), runner.normalized_descriptor(second))
        self.assertIn('Type', first['Inputs'][0])

    def test_strong_name_or_input_contract_change_remains_visible(self):
        first = self.descriptor()
        second = copy.deepcopy(first)
        second['Inputs'][0]['ContractType'] = second['Inputs'][0]['ContractType'].replace('PublicKeyToken=null', 'PublicKeyToken=abcdef1234567890')
        self.assertNotEqual(runner.normalized_descriptor(first), runner.normalized_descriptor(second))

    def test_activity_identity_and_ports_remain_visible(self):
        first = self.descriptor()
        for key, value in [('TypeName', 'Other.Task'), ('Version', 2), ('Ports', [{'Name': 'Next', 'Type': 'Embedded'}])]:
            with self.subTest(key=key):
                second = copy.deepcopy(first)
                second[key] = value
                self.assertNotEqual(runner.normalized_descriptor(first), runner.normalized_descriptor(second))

    def test_duplicate_clr_name_from_another_assembly_fails_closed(self):
        first = self.descriptor()
        second = copy.deepcopy(first)
        second['Assembly'] = 'Other'
        with self.assertRaisesRegex(ValueError, 'Duplicate CLR'):
            runner.descriptor_map({'descriptors': [first, second]})


    def test_generated_variants_can_share_clr_type(self):
        first = self.descriptor()
        second = dict(first, TypeName='Example.Generated')
        self.assertEqual(2, len(runner.descriptor_map({'descriptors': [first, second]})))

    def test_duplicate_activity_identity_is_rejected(self):
        first = self.descriptor()
        second = dict(first, ClrType='Example.Other')
        with self.assertRaisesRegex(ValueError, 'Duplicate activity identity'):
            runner.descriptor_map({'descriptors': [first, second]})

    def test_added_descriptor_in_released_assembly_is_rejected(self):
        before = {'original': self.descriptor()}
        after = dict(before, added=self.descriptor())
        allowed, unexpected = runner.classify_added_descriptors(before, after, [{'assembly': 'Example', 'releasedVersion': '3.8.4'}])
        self.assertEqual([], allowed)
        self.assertEqual(['added'], unexpected)

    def test_only_exact_reviewed_source_descriptors_are_allowed(self):
        row = self.descriptor()
        row['Assembly'] = 'Elsa.Ldap'
        allowed, unexpected = runner.classify_added_descriptors({}, {'ldap': row}, [{'assembly': 'Elsa.Ldap', 'releasedVersion': None, 'allowedSourceDescriptors': [row]}])
        self.assertEqual(['ldap'], allowed)
        self.assertEqual([], unexpected)

    def test_unreviewed_descriptor_in_source_only_assembly_is_rejected(self):
        row = self.descriptor()
        row['Assembly'] = 'Elsa.Ldap'
        reviewed = copy.deepcopy(row)
        for field, value in [('ClrType', 'Other.Clr'), ('TypeName', 'Other.Activity'), ('Version', 2), ('Kind', 'Trigger'), ('Inputs', []), ('Outputs', [{'Name': 'NewOutput'}]), ('Ports', [{'Name': 'NewPort'}])]:
            with self.subTest(field=field):
                changed = dict(row, **{field: value})
                allowed, unexpected = runner.classify_added_descriptors({}, {'added': changed}, [{'assembly': 'Elsa.Ldap', 'releasedVersion': None, 'allowedSourceDescriptors': [reviewed]}])
                self.assertEqual([], allowed)
                self.assertEqual(['added'], unexpected)

    def test_unlisted_assembly_addition_is_rejected(self):
        allowed, unexpected = runner.classify_added_descriptors({}, {'unknown': self.descriptor()}, [{'assembly': 'Elsa.Ldap', 'releasedVersion': None}])
        self.assertEqual([], allowed)
        self.assertEqual(['unknown'], unexpected)


class ReviewedReleasedAdditionTests(unittest.TestCase):
    TYPE_NAMES = ['Elsa.GitHub.Comments.DeleteComment', 'Elsa.GitHub.Comments.GetComment', 'Elsa.GitHub.Comments.UpdateComment', 'Elsa.GitHub.Gists.GetGist']

    def setUp(self):
        self.matrix = json.loads(Path(__file__).with_name('matrix.json').read_text())
        github = next(entry for entry in self.matrix if entry['assembly'] == 'Elsa.DevOps.GitHub')
        self.rows = [row for review in github['reviewedReleasedAssemblyAdditions'] for row in review['descriptors']]
        self.after = {f"{row['TypeName']}:{row['Version']}": copy.deepcopy(row) for row in self.rows}

    def classify(self, after, matrix=None):
        _, unexpected = runner.classify_added_descriptors({}, after, matrix or self.matrix)
        return runner.classify_reviewed_released_additions(unexpected, after, matrix or self.matrix)

    def test_matrix_reviews_exactly_the_four_github_version_two_contracts(self):
        reviewed = [entry for entry in self.matrix if entry.get('reviewedReleasedAssemblyAdditions')]
        self.assertEqual(['Elsa.DevOps.GitHub'], [entry['assembly'] for entry in reviewed])
        self.assertEqual('3.8.4', reviewed[0]['releasedVersion'])
        self.assertEqual([('elsa-core#8325', 1)], [(review['review'], review['revision']) for review in reviewed[0]['reviewedReleasedAssemblyAdditions']])
        self.assertEqual(sorted(f'{name}:2' for name in self.TYPE_NAMES), sorted(self.after))
        self.assertTrue(all(row['Assembly'] == 'Elsa.DevOps.GitHub' for row in self.rows))

    def test_exact_reviewed_contracts_are_approved_separately_from_source_only_additions(self):
        allowed, unexpected = runner.classify_added_descriptors({}, self.after, self.matrix)
        self.assertEqual([], allowed)
        self.assertEqual(sorted(self.after), unexpected)
        approved, remaining = self.classify(self.after)
        self.assertEqual(sorted(self.after), approved)
        self.assertEqual([], remaining)

    def test_any_change_to_a_reviewed_contract_still_fails(self):
        key = 'Elsa.GitHub.Comments.GetComment:2'
        row = self.after[key]
        changes = [
            ('ClrType', 'Elsa.DevOps.GitHub.Activities.Comments.GetComment'),
            ('Kind', 'Trigger'),
            ('Inputs', [dict(field, Name='Id') if field['Name'] == 'CommentId' else field for field in row['Inputs']]),
            ('Inputs', row['Inputs'][1:]),
            ('Inputs', [dict(field, IsSerializable=False) for field in row['Inputs']]),
            ('Outputs', []),
            ('Outputs', [dict(field, ContractType=field['ContractType'].replace('0be8860aee462442', 'null')) for field in row['Outputs']]),
            ('Ports', [{'Name': 'Done', 'Type': 'Flow'}]),
        ]
        for field, value in changes:
            with self.subTest(field=field, value=value):
                approved, remaining = self.classify(dict(self.after, **{key: dict(row, **{field: value})}))
                self.assertNotIn(key, approved)
                self.assertEqual([key], remaining)

    def test_another_addition_to_the_released_assembly_still_fails(self):
        extra = dict(self.after['Elsa.GitHub.Gists.GetGist:2'], TypeName='Elsa.GitHub.Gists.SearchGists', Version=2, ClrType='Elsa.DevOps.GitHub.Activities.Gists.SearchGistsV2')
        approved, remaining = self.classify(dict(self.after, **{'Elsa.GitHub.Gists.SearchGists:2': extra}))
        self.assertEqual(sorted(self.after), approved)
        self.assertEqual(['Elsa.GitHub.Gists.SearchGists:2'], remaining)

    def test_reviewed_contract_cannot_approve_another_assembly_or_an_unreleased_entry(self):
        key = 'Elsa.GitHub.Gists.GetGist:2'
        moved = dict(self.after[key], Assembly='Elsa.Slack')
        self.assertEqual(([], [key]), self.classify({key: moved}))
        unreleased = [dict(entry, releasedVersion=None) if entry['assembly'] == 'Elsa.DevOps.GitHub' else entry for entry in self.matrix]
        self.assertEqual(([], [key]), self.classify({key: self.after[key]}, unreleased))


if __name__ == '__main__':
    unittest.main()
