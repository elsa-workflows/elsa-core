"""Cheap synthetic receipt tests; these do not execute the native updater."""
import json
import unittest

from dependabot_native_proof import reconcile


class NativeProofReceiptTests(unittest.TestCase):
    def test_missing_and_failed_records_remain_visible_without_error_values(self):
        expected = {'core/A.csproj': 'Directory.Packages.props',
                    'extensions/src/Retained/Retained.csproj': 'extensions/src/Directory.Packages.props'}
        workspaces = [{'Path': '', 'IsSuccess': False,
                       'Error': {'error-type': 'private_source_authentication_failure',
                                 'error-details': {'message': 'SECRET https://private/token'}},
                       'Projects': [{'FilePath': 'core/A.csproj', 'IsSuccess': True,
                                     'ImportedFiles': ['../Directory.Packages.props'],
                                     'TargetFrameworks': ['net10.0'],
                                     'Dependencies': [{'Name': 'Known', 'Version': '1.2.3'},
                                                      {'Name': 'SECRET', 'Version': 'https://private'}]}]}]
        result = reconcile(expected, workspaces, {'Directory.Packages.props': {'Known': {'1.2.3'}}}, set(expected) | {'Directory.Packages.props'})
        self.assertEqual('omitted', result['projects'][1]['status'])
        self.assertEqual('workspace_failed', result['projects'][0]['status'])
        self.assertFalse(result['complete'])
        encoded = json.dumps(result)
        self.assertNotIn('SECRET', encoded)
        self.assertNotIn('https://', encoded)
        self.assertEqual(['private_source_authentication_failure'], result['workspaces'][0]['errors'])

    def test_success_requires_central_import_tfms_and_versions_and_retains_empty(self):
        expected = {'A.csproj': 'Directory.Packages.props', 'B.csproj': 'Directory.Packages.props'}
        native = {'Path': '', 'IsSuccess': True, 'Error': None,
                  'Projects': [{'FilePath': 'A.csproj', 'IsSuccess': True, 'Error': None,
                                'ImportedFiles': ['Directory.Packages.props'], 'TargetFrameworks': ['net8.0'],
                                'Dependencies': [{'Name': 'Known', 'Version': '1.2.3'}]},
                               {'FilePath': 'B.csproj', 'IsSuccess': True, 'Error': None,
                                'ImportedFiles': [], 'TargetFrameworks': [], 'Dependencies': []}]}
        result = reconcile(expected, [native], {'Directory.Packages.props': {'Known': {'1.2.3'}}}, set(expected) | {'Directory.Packages.props'})
        self.assertEqual(['observed', 'incomplete_metadata'], [p['status'] for p in result['projects']])
        self.assertFalse(result['complete'])
        self.assertEqual(0, result['projects'][1]['dependency_count'])

    def test_nested_and_retained_project_relative_central_paths(self):
        cases = [('', 'core/src/modules/Foo/Foo.csproj',
                  'core/src/modules/Foo/Foo.csproj', 'Directory.Packages.props',
                  '../../../../Directory.Packages.props'),
                 ('extensions/src/Elsa.Testing.Extensions', 'Elsa.Testing.Extensions.csproj',
                  'extensions/src/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj',
                  'extensions/src/Directory.Packages.props', '../Directory.Packages.props')]
        for workspace, file_path, project, central, relative_central in cases:
            for special_only in (False, True):
                with self.subTest(workspace=workspace, special_only=special_only):
                    native = {'Path': workspace, 'IsSuccess': True, 'Projects': [{
                        'FilePath': file_path, 'IsSuccess': True,
                        'ImportedFiles': [] if special_only else [relative_central, '../../../../../Secrets.props'],
                        'PackageManagementSpecialFileRelativePath': relative_central,
                        'TargetFrameworks': ['net10.0'],
                        'Dependencies': [{'Name': 'Known', 'Version': '1.2.3'}]}]}
                    result = reconcile({project: central}, [native],
                                       {central: {'Known': {'1.2.3'}}}, {project, central, 'Secrets.props'})
                    row = result['projects'][0]
                    self.assertEqual('observed', row['status'])
                    self.assertTrue(result['complete'])
                    self.assertEqual(central, row['central_file'])
                    self.assertEqual([] if special_only else [central], row['imported_files'])

    def test_version_from_another_product_does_not_satisfy_central_join(self):
        native = {'Path': '', 'IsSuccess': True, 'Projects': [{
            'FilePath': 'A.csproj', 'IsSuccess': True, 'ImportedFiles': ['Directory.Packages.props'],
            'TargetFrameworks': ['net10.0'], 'Dependencies': [{'Name': 'Known', 'Version': '2.0.0'}]}]}
        versions = {'Directory.Packages.props': {'Known': {'1.2.3'}},
                    'studio/src/Directory.Packages.props': {'Known': {'2.0.0'}}}
        result = reconcile({'A.csproj': 'Directory.Packages.props'}, [native], versions,
                           {'A.csproj', 'Directory.Packages.props'})
        self.assertEqual('version_requires_review', result['projects'][0]['status'])
        self.assertFalse(result['complete'])

    def test_duplicate_or_escaping_record_cannot_certify_coverage(self):
        native = {'Path': '', 'IsSuccess': True, 'Projects': [
            {'FilePath': '../A.csproj'}, {'FilePath': 'A.csproj'}, {'FilePath': 'A.csproj'}]}
        result = reconcile({'A.csproj': 'Directory.Packages.props'}, [native], {}, {'A.csproj'})
        self.assertFalse(result['complete'])
        self.assertEqual('duplicate', result['projects'][0]['status'])
        self.assertEqual(1, result['invalid_record_count'])


if __name__ == '__main__':
    unittest.main()
