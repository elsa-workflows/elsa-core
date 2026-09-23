"""Regression tests for the pinned Secrets API and Studio source contract."""
import importlib.util
import json
from pathlib import Path
import unittest


SCRIPT = Path(__file__).with_name('verify_secrets_api_studio_contract.py')
FIXTURE = SCRIPT.with_name('secrets-api-studio-contract.json')
SPEC = importlib.util.spec_from_file_location('secrets_api_studio_contract', SCRIPT)
CONTRACT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CONTRACT)


class SecretsApiStudioContractTests(unittest.TestCase):
    def setUp(self):
        self.fixture = json.loads(FIXTURE.read_text())

    def test_pins_are_full_commits_and_source_paths_are_pinned(self):
        self.assertEqual(
            {'core', 'extensions', 'extensionsSchema', 'studio'},
            set(self.fixture['sourcePins']))
        for commit in self.fixture['sourcePins'].values():
            self.assertRegex(commit, r'^[0-9a-f]{40}$')
        self.assertIn('coreApi', self.fixture['sourcePaths'])
        self.assertIn('legacyApi', self.fixture['sourcePaths'])
        self.assertIn('studioApi', self.fixture['sourcePaths'])

    def test_studio_route_set_is_exactly_core_route_set(self):
        core_keys = sorted((row['verb'], CONTRACT.normalize_route(row['path'])) for row in self.fixture['coreRoutes'])
        studio_keys = sorted((row['verb'], CONTRACT.normalize_route(row['path'])) for row in self.fixture['studioRoutes'])

        self.assertEqual(core_keys, studio_keys)
        self.assertEqual(10, len(studio_keys))

    def test_route_template_normalization_detects_id_and_name_collision(self):
        core = [
            {'verb': 'GET', 'path': '/secrets/{name}'},
            {'verb': 'POST', 'path': '/secrets/{name}'}
        ]
        legacy = [
            {'verb': 'GET', 'path': '/secrets/{id}'},
            {'verb': 'POST', 'path': '/secrets/{id}'}
        ]

        self.assertEqual(
            [['GET', '/secrets/{}'], ['POST', '/secrets/{}']],
            CONTRACT.route_conflicts(core, legacy))

    def test_fixture_records_only_the_five_shared_templates(self):
        self.assertEqual([
            ['DELETE', '/secrets/{}'],
            ['GET', '/secrets'],
            ['GET', '/secrets/{}'],
            ['POST', '/secrets'],
            ['POST', '/secrets/{}']
        ], self.fixture['routeConflicts'])

    def test_nested_response_generic_is_parsed_without_losing_type_name(self):
        source = '''
        public class Endpoint(ISecretManager manager) :
            ElsaEndpoint<ListRequest, PagedListResponse<SecretModel>>
        {
        }
        '''

        self.assertEqual(
            ('ListRequest', 'PagedListResponse<SecretModel>'),
            CONTRACT.endpoint_dto(source))

    def test_legacy_plaintext_route_is_not_in_core_route_set(self):
        plaintext_route = tuple(self.fixture['legacyPlaintextRoute'])
        legacy = {(row['verb'], row['path']) for row in self.fixture['legacyRoutes']}
        core = {(row['verb'], row['path']) for row in self.fixture['coreRoutes']}

        self.assertIn(plaintext_route, legacy)
        self.assertNotIn(plaintext_route, core)

    def test_safe_studio_dto_omissions_are_explicit(self):
        differences = self.fixture['dtoProjectionDifferences']

        self.assertEqual(['Tags'], differences['SecretModel'])
        self.assertEqual(['Tags'], differences['CreateSecretRequest'])
        self.assertEqual(['StoreNames', 'TypeNames'], differences['ListSecretsRequest'])
        for name, fields in differences.items():
            if name not in {'SecretModel', 'CreateSecretRequest', 'ListSecretsRequest'}:
                self.assertEqual([], fields)

    def test_sample_compiler_collision_is_kept_separate_from_runtime_host_proof(self):
        observation = self.fixture['consolidatedSampleBuildObservation']

        self.assertEqual(8287, observation['followUpIssue'])
        self.assertEqual(['CS0121', 'CS1929', 'CS1929', 'CS1929'],
                         [item['code'] for item in observation['diagnostics']])
        self.assertEqual([622, 627, 633, 639],
                         [item['line'] for item in observation['diagnostics']])
        self.assertFalse(observation['runtimeSecretsSettingAtBuild'])
        self.assertTrue(observation['legacyExpiryTask']['outsideUseSecretsGuard'])
        self.assertFalse(observation['postCorrectionBuildClaimedHere'])
        self.assertFalse(observation['defaultHostRouteCompositionVerified'])


if __name__ == '__main__':
    unittest.main()
