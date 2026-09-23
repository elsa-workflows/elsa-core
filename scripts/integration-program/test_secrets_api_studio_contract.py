"""Regression tests for the pinned Secrets API and Studio source contract."""
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
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
        self.assertEqual({'studioApi', 'studioModels'}, set(self.fixture['studioTestSourceSnapshots']))

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

    def test_named_route_argument_is_included_in_route_collision_validation(self):
        routes = self.parse_fixture_endpoint('Get(route: "/secrets/shadow");')

        self.assertEqual(1, len(routes))
        self.assertEqual("/secrets/shadow", routes[0]['path'])

    def test_unrecognized_endpoint_route_form_fails_closed(self):
        with self.assertRaisesRegex(ValueError, 'no recognized route declaration'):
            self.parse_fixture_endpoint('Get(BuildRoute());')

    @staticmethod
    def parse_fixture_endpoint(route_declaration):
        with tempfile.TemporaryDirectory() as directory:
            repo = Path(directory)
            endpoint = repo / 'src' / 'Secrets' / 'Endpoint.cs'
            endpoint.parent.mkdir(parents=True)
            endpoint.write_text(
                'public class Endpoint : ElsaEndpointWithoutRequest<Response> { '
                f'public void Configure() {{ {route_declaration} '
                'ConfigurePermissions("secrets:read"); } }'
            )
            subprocess.run(['git', '-C', directory, 'init', '--quiet'], check=True)
            subprocess.run(['git', '-C', directory, 'add', '.'], check=True)
            subprocess.run([
                'git', '-C', directory,
                '-c', 'user.name=Contract Test',
                '-c', 'user.email=contract-test@example.invalid',
                '-c', 'commit.gpgsign=false',
                'commit', '--quiet', '-m', 'fixture'
            ], check=True)
            return CONTRACT.parse_api_routes(repo, 'HEAD', 'src', 'legacy')

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

    def test_http_fixture_uses_the_reviewed_workbench_secrets_feature_shape(self):
        patch_path = SCRIPT.parent / 'consolidated-build' / 'workbench-canonical-secrets.patch'
        patch = patch_path.read_text()
        added_source = '\n'.join(
            line[1:] for line in patch.splitlines()
            if line.startswith('+') and not line.startswith('+++'))
        for marker in (
            '.UseSecrets(secrets =>',
            'secrets.ConfigureOptions = options => configuration.GetSection("Secrets").Bind(options);',
            'secrets.UseEntityFrameworkCore(ef =>',
            '.UseSecretsJavaScript();'
        ):
            self.assertIn(marker, added_source)
        self.assertIn('ef.UseSqlite(sp => sp.GetSqliteConnectionString());', patch)
        self.assertIn('-    options.Schedule.ConfigureTask<UpdateExpiredSecretsRecurringTask>', patch)

        fixture_source = (SCRIPT.parents[2] / 'test/integration/Elsa.Secrets.Api.IntegrationTests/SecretsApiStudioHttpTests.cs').read_text()
        for marker in (
            '.UseSecrets(secrets =>',
            'secrets.UseEntityFrameworkCore(ef =>',
            'ef.UseSqlite($"Data Source={_databasePath};Default Timeout=30");',
            '.UseSecretsJavaScript()'
        ):
            self.assertIn(marker, fixture_source)


if __name__ == '__main__':
    unittest.main()
