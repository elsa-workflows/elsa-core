import copy
import importlib.util
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


    def test_added_descriptor_in_released_assembly_is_rejected(self):
        before = {'original': self.descriptor()}
        after = dict(before, added=self.descriptor())
        allowed, unexpected = runner.classify_added_descriptors(before, after, [{'assembly': 'Example', 'releasedVersion': '3.8.4'}])
        self.assertEqual([], allowed)
        self.assertEqual(['added'], unexpected)

    def test_only_explicit_source_only_assemblies_allow_added_descriptors(self):
        row = self.descriptor()
        row['Assembly'] = 'Elsa.Ldap'
        allowed, unexpected = runner.classify_added_descriptors({}, {'ldap': row}, [{'assembly': 'Elsa.Ldap', 'releasedVersion': None}])
        self.assertEqual(['ldap'], allowed)
        self.assertEqual([], unexpected)

    def test_unlisted_assembly_addition_is_rejected(self):
        allowed, unexpected = runner.classify_added_descriptors({}, {'unknown': self.descriptor()}, [{'assembly': 'Elsa.Ldap', 'releasedVersion': None}])
        self.assertEqual([], allowed)
        self.assertEqual(['unknown'], unexpected)


if __name__ == '__main__':
    unittest.main()
