"""Safety tests for the isolated Workbench Secrets runtime fixture."""
import base64
import contextlib
import difflib
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import stat
import subprocess
import tempfile
import unittest
import unittest.mock as mock


SCRIPT = Path(__file__).with_name('prepare_workbench_secrets_runtime.py')
SPEC = importlib.util.spec_from_file_location('workbench_runtime_fixture', SCRIPT)
FIXTURE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FIXTURE)


class WorkbenchSecretsRuntimeFixtureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.base = Path(self.temp.name)
        self.rehearsal = self.base / 'mapped'
        self.source = self.rehearsal / FIXTURE.SOURCE_PROJECT
        self.source.mkdir(parents=True)
        self.base_program = (
            'const bool useMultitenancy = false;\nconst bool useSecrets = false;\n'
            'var builder = WebApplication.CreateBuilder(args);\n'
            'var configuration = builder.Configuration;\n'
            'UseSecretsManagement();\nUpdateExpiredSecretsRecurringTask();\n'
            'elsa.AddActivitiesFrom<Program>();\nelsa.AddWorkflowsFrom<Program>();\n'
            'if (useSignalR)\n    app.UseWorkflowsSignalRHubs();\n\n'
            'await app.RunAsync();\n')
        self.base_project = '<Project>\n  <Reference Include="legacy-secrets" />\n</Project>\n'
        (self.source / 'Elsa.Server.Web.csproj').write_text(self.base_project)
        (self.source / 'Program.cs').write_text(self.base_program)
        (self.source / 'appsettings.json').write_text(json.dumps({
            'ConnectionStrings': {'Sqlite': 'Data Source=App_Data/elsa.sqlite.db;Cache=Shared;'},
            'Webhooks': {'Sinks': [{'Url': 'https://outside.invalid/sentinel'}]}
        }))
        self.source_app_data = self.source / 'App_Data'
        self.source_app_data.mkdir()
        self.tracked_files = {
            self.source_app_data / 'elsa.sqlite.db': b'database-sentinel',
            self.source_app_data / 'elsa.sqlite.db-wal': b'wal-sentinel',
            self.source_app_data / 'elsa.sqlite.db-shm': b'shm-sentinel',
            self.source_app_data / 'locks' / 'workflow.lock': b'lock-sentinel'
        }
        for path, content in self.tracked_files.items():
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(content)
        self.temp_parent = self.base / 'isolated-temporary'
        self.temp_parent.mkdir()
        self.pins = ('a' * 40, 'b' * 40, 'c' * 40)
        self.initialize_rehearsal()

    def tearDown(self):
        self.temp.cleanup()

    def prepare(self, compile_items_provider=None, **overrides):
        supplemental_patches = overrides.pop('supplemental_patches', FIXTURE.SUPPLEMENTAL_PATCHES)
        optional_fixture_patches = overrides.pop('optional_fixture_patches', FIXTURE.OPTIONAL_FIXTURE_PATCHES)
        options = dict(
            rehearsal_root=self.rehearsal,
            core_sha=self.pins[0],
            extensions_sha=self.pins[1],
            studio_sha=self.pins[2],
            temp_parent=self.temp_parent
        )
        options.update(overrides)
        with contextlib.redirect_stdout(io.StringIO()), \
                mock.patch.object(FIXTURE, 'build_host', self.fake_build), \
                mock.patch.object(FIXTURE, 'SUPPLEMENTAL_PATCHES', supplemental_patches), \
                mock.patch.object(FIXTURE, 'OPTIONAL_FIXTURE_PATCHES', optional_fixture_patches), \
                mock.patch.object(
                    FIXTURE, 'CANONICAL_WORKBENCH_BASE_PATCH_SHA256',
                    hashlib.sha256(self.previous_workbench_patch.read_bytes()).hexdigest()), \
                mock.patch.object(FIXTURE, 'get_compile_items', compile_items_provider or self.fake_compile_items):
            return FIXTURE.prepare_fixture(**options)

    def fake_compile_items(self, source):
        files = sorted(
            path.resolve().relative_to(source.resolve()).as_posix()
            for path in source.rglob('*.cs')
            if not {'bin', 'obj'} & set(path.relative_to(source).parts))
        return ['dotnet', 'msbuild', 'synthetic.csproj', '-getItem:Compile', '-nologo'], [
            {'FullPath': str(source / path)} for path in files]

    def initialize_rehearsal(self):
        self.source_patch = self.base / 'source-integration.patch'
        self.source_patch.write_text('synthetic source integration patch\n')
        self.workbench_patch = self.base / 'workbench.patch'
        project = (FIXTURE.SOURCE_PROJECT / 'Elsa.Server.Web.csproj').as_posix()
        program = (FIXTURE.SOURCE_PROJECT / 'Program.cs').as_posix()
        self.mapped_studio_root = self.rehearsal / 'src/studio/modules/Elsa.Studio.Secrets/Menu'
        self.mapped_studio_root.mkdir(parents=True)
        self.menu_file = self.mapped_studio_root / 'SecretsMenu.cs'
        self.menu_before = 'namespace Elsa.Studio.Secrets;\npublic class SecretsMenu {}\n'
        self.menu_after = 'namespace Elsa.Studio.Secrets;\npublic class SecretsMenu { public const string TenantProof = "enabled"; }\n'
        self.menu_file.write_text(self.menu_before)
        self.layout_file = self.rehearsal / 'src/studio/modules/Elsa.Studio.Workflows.Designer/ClientLib/generate-bpmn-types.js'
        self.layout_file.parent.mkdir(parents=True)
        self.layout_before = 'const propsRoot = path.resolve(__dirname, "../../../../../");\n'
        self.layout_after = 'const propsRoot = locateStudioPropsRoot(__dirname);\n'
        self.layout_file.write_text(self.layout_before)

        canonical_program = self.base_program.replace('const bool useSecrets = false;\n', '')
        canonical_program = canonical_program.replace(
            'var configuration = builder.Configuration;\n',
            'var configuration = builder.Configuration;\n'
            'var useSecrets = configuration.GetValue("Features:Secrets:Enabled", false);\n')
        canonical_program = canonical_program.replace(
            'UseSecretsManagement();\nUpdateExpiredSecretsRecurringTask();\n',
            'elsa.UseSecrets(secrets => secrets.UseEntityFrameworkCore(ef => { }));\n'
            'elsa.UseSecretsJavaScript();\n')
        project_before = self.base_project
        project_after = project_before.replace('legacy-secrets', 'core-secrets')
        self.workbench_patch.write_text(
            self.make_patch(program, self.base_program, canonical_program)
            + self.make_patch(project, project_before, project_after))
        baseline_patch = self.rehearsal / FIXTURE.PATCH_RELATIVE
        baseline_patch.parent.mkdir(parents=True, exist_ok=True)
        self.previous_workbench_patch = baseline_patch
        previous_program = self.base_program.replace('const bool useSecrets = false;', 'const bool useSecrets = true;')
        previous_project = self.base_project.replace('legacy-secrets', 'previous-secrets')
        previous_program_diff = ''.join(difflib.unified_diff(
            self.base_program.splitlines(keepends=True), previous_program.splitlines(keepends=True),
            fromfile=f'a/{program}', tofile=f'b/{program}'))
        previous_project_diff = ''.join(difflib.unified_diff(
            self.base_project.splitlines(keepends=True), previous_project.splitlines(keepends=True),
            fromfile=f'a/{project}', tofile=f'b/{project}'))
        baseline_patch.write_text(
            f'diff --git a/{program} b/{program}\n{previous_program_diff}'
            f'diff --git a/{project} b/{project}\n{previous_project_diff}')
        git = lambda *args: subprocess.run(['git', '-C', str(self.rehearsal), *args], check=True, capture_output=True)
        git('init', '--quiet')
        git('add', '.')
        git('-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid', '-c', 'commit.gpgsign=false', 'commit', '--quiet', '-m', 'Mapped fixture')
        self.rehearsal_commit = git('rev-parse', 'HEAD').stdout.decode().strip()

        prepared_files = []
        menu_relative = 'src/studio/modules/Elsa.Studio.Secrets/Menu/SecretsMenu.cs'
        for relative, content in ((program, self.base_program), (project, self.base_project)):
            prepared_files.append({'path': relative, 'sha256': hashlib.sha256(content.encode()).hexdigest()})
        (self.rehearsal / 'import-receipt.json').write_text(json.dumps({
            'sourceCommits': dict(zip(('core', 'extensions', 'studio'), self.pins)),
            'rehearsalCommit': self.rehearsal_commit,
            'exactBlobAndModeMapping': True,
            'originalHistoriesReachable': True,
            'buildCompatibilityVerified': False,
            'publicationAuthorized': False
        }))
        self.prepared_files = prepared_files
        (self.rehearsal / 'consolidated-build-receipt.json').write_text(json.dumps({
            'sourceCommits': dict(zip(('core', 'extensions', 'studio'), self.pins)),
            'rehearsalCommit': self.rehearsal_commit,
            'patchSha256': hashlib.sha256(self.source_patch.read_bytes()).hexdigest(),
            'files': prepared_files,
            'buildCompatibilityVerified': False,
            'publicationAuthorized': False
        }))
        FIXTURE.SOURCE_PATCH = self.source_patch
        FIXTURE.PATCH = self.workbench_patch
        subprocess.run(['git', 'apply', str(self.workbench_patch)], cwd=self.rehearsal, check=True)

        self.menu_patch = self.base / 'studio-secrets-menu.patch'
        self.menu_patch.write_text(self.make_patch(menu_relative, self.menu_before, self.menu_after))
        self.layout_patch = self.base / 'studio-bpmn-generator-layout.patch'
        self.layout_patch.write_text(self.make_patch(
            'src/studio/modules/Elsa.Studio.Workflows.Designer/ClientLib/generate-bpmn-types.js',
            self.layout_before, self.layout_after))
        self.tenant_patch = self.base / 'workbench-two-tenant-multitenancy.patch'
        mapped_program = (self.source / 'Program.cs').read_text()
        tenant_program = mapped_program.replace('const bool useMultitenancy = false;\n', '')
        tenant_program = tenant_program.replace(
            'var useSecrets = configuration.GetValue("Features:Secrets:Enabled", false);\n',
            'var useSecrets = configuration.GetValue("Features:Secrets:Enabled", false);\n'
            'var useMultitenancy = configuration.GetValue("Features:Multitenancy:Enabled", false);\n')
        self.tenant_patch.write_text(self.make_patch(program, mapped_program, tenant_program))
        route_probe_before = mapped_program
        route_probe_after = route_probe_before.replace(
            'await app.RunAsync();\n',
            'if (configuration.GetValue("Features:Secrets:RouteProbe", false))\n'
            '{\n'
            '    app.MapGet("/__fixture/secrets/routes", () => Results.Json(new { routes = Array.Empty<object>(), secretAssemblies = Array.Empty<string>() }));\n'
            '}\n\n'
            'await app.RunAsync();\n')
        self.route_patch = self.base / 'workbench-secrets-route-probe.patch'
        self.route_patch.write_text(self.make_patch(program, route_probe_before, route_probe_after))

    @staticmethod
    def make_patch(relative, before, after):
        diff = ''.join(difflib.unified_diff(
            before.splitlines(keepends=True), after.splitlines(keepends=True),
            fromfile=f'a/{relative}', tofile=f'b/{relative}'))
        return f'diff --git a/{relative} b/{relative}\n{diff}'

    def apply_two_tenant_patches(self):
        subprocess.run(['git', 'apply', str(self.menu_patch)], cwd=self.rehearsal, check=True)
        subprocess.run(['git', 'apply', str(self.layout_patch)], cwd=self.rehearsal, check=True)
        subprocess.run(['git', 'apply', str(self.tenant_patch)], cwd=self.rehearsal, check=True)

    def apply_route_probe_patch(self):
        subprocess.run(['git', 'apply', str(self.route_patch)], cwd=self.rehearsal, check=True)

    def fake_build(self, source, log_parent):
        host_dll = source / 'bin' / 'Debug' / 'net10.0' / 'Elsa.Server.Web.dll'
        host_dll.parent.mkdir(parents=True)
        host_dll.write_bytes(b'fresh synthetic host assembly')
        return {
            'command': ['dotnet', 'build', str(source / 'Elsa.Server.Web.csproj')],
            'startedAtUtc': '2026-09-24T00:00:00+00:00',
            'finishedAtUtc': '2026-09-24T00:00:01+00:00',
            'exitCode': 0,
            'hostDll': str(host_dll),
            'hostDllSha256': hashlib.sha256(host_dll.read_bytes()).hexdigest(),
            'hostDllSizeBytes': host_dll.stat().st_size,
            'log': 'Build succeeded.'
        }

    def test_fixture_is_owned_loopback_only_and_does_not_copy_upstream_state(self):
        fixture_root = self.prepare()
        try:
            content_root = fixture_root / 'content-root'
            config = json.loads((content_root / 'appsettings.json').read_text())
            plan = json.loads((fixture_root / 'launch-plan.json').read_text())
            command = (fixture_root / 'launch-command.txt').read_text()
            working_app_data = fixture_root / 'App_Data'

            database_path = Path(config['ConnectionStrings']['Sqlite'].split(';')[0].split('=', 1)[1])
            self.assertTrue(FIXTURE.is_within(database_path, content_root))
            self.assertFalse(database_path.exists())
            self.assertEqual(str(fixture_root), plan['processWorkingDirectory'])
            self.assertEqual(str(content_root), plan['contentRoot'])
            self.assertEqual('/workflows', config['Http']['BasePath'])
            self.assertEqual(str(working_app_data / 'DropIns'), plan['dropInsDirectory'])
            self.assertEqual(str(working_app_data / 'locks'), plan['lockDirectory'])
            self.assertEqual([], list((working_app_data / 'DropIns').iterdir()))
            self.assertEqual([], list((working_app_data / 'locks').iterdir()))
            self.assertTrue(FIXTURE.is_within(Path(plan['databasePath']), content_root))
            self.assertNotEqual(Path(plan['dropInsDirectory']).parent, Path(plan['databasePath']).parent)
            self.assertNotIn('Features', config)
            self.assertEqual([], config['Webhooks']['Sinks'])
            self.assertNotIn('outside.invalid', (content_root / 'appsettings.json').read_text())
            self.assertEqual(0, plan['configuredWebhookSinkCount'])
            self.assertFalse(plan['appsettingsContainsSecretsEnabled'])
            self.assertEqual('--Features:Secrets:Enabled=true', plan['secretsEnabledOnlyByExplicitLaunchOverride'])
            self.assertEqual(self.pins[0], plan['sourcePins']['core'])
            self.assertEqual(64, len(plan['workbenchPatchSha256']))
            self.assertEqual(
                FIXTURE.file_sha256(self.rehearsal / FIXTURE.PATCH_RELATIVE),
                plan['previousWorkbenchPatchSha256'])
            self.assertIn('reverse the verified previous Workbench patch', plan['patchTransition'])
            self.assertEqual(self.rehearsal_commit, plan['rehearsalCommit'])
            self.assertEqual(hashlib.sha256(b'fresh synthetic host assembly').hexdigest(), plan['freshBuild']['hostDllSha256'])
            self.assertEqual(['Program.cs'], plan['hostAssemblySourceInventory']['compiledSourceFiles'])
            self.assertEqual(1, plan['hostAssemblySourceInventory']['compiledSourceFileCount'])
            self.assertIn('env -i', command)
            self.assertIn('ASPNETCORE_ENVIRONMENT=Production', command)
            self.assertIn('--contentRoot', command)
            self.assertIn('--Features:Secrets:Enabled=true', command)
            self.assertIn('http://127.0.0.1:', command)
            self.assertFalse(plan['launchApproved'])

            credentials_path = fixture_root / 'synthetic-credentials.txt'
            credentials = credentials_path.read_text()
            username = next(line.removeprefix('Username: ') for line in credentials.splitlines() if line.startswith('Username: '))
            password = next(line.removeprefix('Password: ') for line in credentials.splitlines() if line.startswith('Password: '))
            self.assertEqual('synthetic-admin', username)
            user = config['Identity']['Users'][0]
            self.assertEqual(username, user['Name'])
            self.assertIn(FIXTURE.ADMIN_ROLE_ID, user['Roles'])
            self.assertEqual(['*'], config['Identity']['Roles'][0]['Permissions'])
            self.assertEqual('', user['TenantId'])
            self.assertEqual('', config['Identity']['Roles'][0]['TenantId'])
            salt = base64.b64decode(user['HashedPasswordSalt'])
            envelope = base64.b64decode(user['HashedPassword']).decode('ascii')
            algorithm, iterations, digest = envelope.split('$')
            self.assertEqual('pbkdf2-sha256', algorithm)
            self.assertEqual(str(FIXTURE.PBKDF2_ITERATIONS), iterations)
            self.assertEqual(
                base64.b64encode(hashlib.pbkdf2_hmac('sha256', password.encode(), salt, int(iterations), dklen=32)).decode(),
                digest)
            self.assertEqual(32, len(config['Secrets']['EncryptionKey']))
            self.assertEqual(0o600, stat.S_IMODE(credentials_path.stat().st_mode))
            self.assertEqual(0o600, stat.S_IMODE((content_root / 'appsettings.json').stat().st_mode))
            self.assertEqual(0o600, stat.S_IMODE((fixture_root / 'host-build.log').stat().st_mode))
        finally:
            FIXTURE.cleanup_fixture(fixture_root, host_stopped=True)

        for path, content in self.tracked_files.items():
            self.assertEqual(content, path.read_bytes())

    def test_fixture_parent_inside_project_is_rejected_before_writing(self):
        with self.assertRaisesRegex(ValueError, 'cannot be inside the Workbench project root'):
            self.prepare(temp_parent=self.source)
        for path, content in self.tracked_files.items():
            self.assertEqual(content, path.read_bytes())

    def test_cleanup_requires_stopped_host_and_owned_fixture_marker(self):
        fixture_root = self.prepare()
        with self.assertRaisesRegex(ValueError, 'Pass --host-stopped'):
            FIXTURE.cleanup_fixture(fixture_root, host_stopped=False)
        self.assertTrue(fixture_root.exists())

        with contextlib.redirect_stdout(io.StringIO()):
            FIXTURE.cleanup_fixture(fixture_root, host_stopped=True)
        self.assertFalse(fixture_root.exists())

        with self.assertRaisesRegex(ValueError, 'Missing regular fixture ownership marker'):
            FIXTURE.cleanup_fixture(self.source, host_stopped=True)

    def test_fixture_rejects_unpatched_or_legacy_secrets_host(self):
        program = self.source / 'Program.cs'
        program.write_text(program.read_text().replace('var useSecrets = configuration.GetValue("Features:Secrets:Enabled", false);', 'const bool useSecrets = false;'))
        with self.assertRaisesRegex(ValueError, 'Mapped rehearsal does not match patch'):
            self.prepare()

    def test_route_probe_normalizes_the_configured_api_prefix_before_filtering(self):
        patch = FIXTURE.OPTIONAL_FIXTURE_PATCHES[-1].read_text()
        self.assertIn('configuredRoutePrefix', patch)
        self.assertIn('item.path[configuredRoutePrefix.Length..]', patch)
        self.assertIn('item.path.StartsWith("/actions/secrets/"', patch)

    def test_route_probe_mode_requires_the_reviewed_private_patch_before_build(self):
        build = self.fake_build
        with mock.patch.object(FIXTURE, 'build_host', wraps=build) as build_host:
            with self.assertRaisesRegex(ValueError, 'requires the reviewed fixture route-probe patch'):
                self.prepare(route_probe=True, optional_fixture_patches=())
        build_host.assert_not_called()

    def test_route_probe_plan_and_sanitized_receipt_cover_only_canonical_routes(self):
        self.apply_route_probe_patch()
        fixture_root = self.prepare(route_probe=True, optional_fixture_patches=(self.route_patch,))
        try:
            plan = json.loads((fixture_root / 'launch-plan.json').read_text())
            command = (fixture_root / 'launch-command.txt').read_text()
            self.assertTrue(plan['routeProbe']['enabled'])
            self.assertEqual(FIXTURE.ROUTE_PROBE_PATH, plan['routeProbe']['path'])
            self.assertEqual(FIXTURE.ROUTE_PROBE_PATCH_NAME, plan['routeProbe']['patch'])
            self.assertEqual(10, plan['routeProbe']['expectedRouteCount'])
            self.assertIn('--Features:Secrets:RouteProbe=true', command)
            self.assertEqual(
                self.route_patch.name,
                plan['sourcePatchChain']['optionalFixturePatches'][0]['path'])

            payload = {
                'routes': [
                    {
                        'path': path,
                        'methods': [method],
                        'endpointType': f'Elsa.Secrets.Endpoints.Secrets.Endpoint',
                        'displayName': f'Elsa.Secrets.Endpoints.Secrets.{path}',
                        'handlerType': 'Elsa.Secrets.EndpointHandler',
                        'metadataTypes': ['Microsoft.AspNetCore.Routing.HttpMethodMetadata']
                    }
                    for method, path in FIXTURE.EXPECTED_CANONICAL_SECRETS_ROUTES
                ],
                'secretAssemblies': ['Elsa.Secrets', 'Elsa.Secrets.JavaScript']
            }
            receipt = FIXTURE.validate_route_probe_payload(payload)
            self.assertEqual(10, receipt['routeCount'])
            self.assertTrue(receipt['canonicalOwnershipVerified'])
            self.assertTrue(receipt['legacyRoutesAbsent'])
            self.assertTrue(receipt['legacyAssembliesAbsent'])
            self.assertNotIn('displayName', json.dumps(receipt))
            self.assertNotIn('handlerType', json.dumps(receipt))
        finally:
            FIXTURE.cleanup_fixture(fixture_root, host_stopped=True)

    def test_route_probe_rejects_legacy_routes_or_assemblies(self):
        payload = {
            'routes': [
                {
                    'path': path,
                    'methods': [method],
                    'endpointType': 'Elsa.Secrets.Endpoints.Secrets.Endpoint',
                    'displayName': 'Elsa.Secrets.Endpoints.Secrets.Endpoint',
                    'handlerType': 'Elsa.Secrets.EndpointHandler',
                }
                for method, path in FIXTURE.EXPECTED_CANONICAL_SECRETS_ROUTES
            ],
            'secretAssemblies': ['Elsa.Secrets', 'Elsa.Secrets.Api']
        }
        payload['routes'].append({
            'path': '/secrets/{id}/input',
            'methods': ['GET'],
            'displayName': 'Elsa.Secrets.Api.Endpoints.Secrets.GetInputModel.Endpoint',
            'handlerType': 'Elsa.Secrets.Api.EndpointHandler',
        })
        with self.assertRaisesRegex(ValueError, 'forbidden legacy'):
            FIXTURE.validate_route_probe_payload(payload)

    def test_route_probe_observes_and_rejects_each_legacy_route_family(self):
        for path in (
            '/actions/secrets/generate-unique-name',
            '/bulk-actions/secrets/delete',
            '/queries/secrets/is-unique-name',
        ):
            payload = {
                'routes': [{
                    'path': path,
                    'methods': ['POST'],
                    'endpointType': 'Elsa.Secrets.Api.Endpoint',
                }],
                'secretAssemblies': ['Elsa.Secrets', 'Elsa.Secrets.Api'],
            }
            with self.assertRaisesRegex(ValueError, 'forbidden legacy'):
                FIXTURE.validate_route_probe_payload(payload)

    def test_route_probe_does_not_claim_ownership_from_arbitrary_metadata(self):
        payload = {
            'routes': [
                {
                    'path': path,
                    'methods': [method],
                    'endpointType': 'Some.Unrelated.Endpoint',
                    'displayName': 'Elsa.Secrets.Endpoints.Secrets.Endpoint',
                    'handlerType': 'Elsa.Secrets.EndpointHandler',
                }
                for method, path in FIXTURE.EXPECTED_CANONICAL_SECRETS_ROUTES
            ],
            'secretAssemblies': ['Elsa.Secrets'],
        }
        with self.assertRaisesRegex(ValueError, 'cannot prove Core endpoint ownership'):
            FIXTURE.validate_route_probe_payload(payload)

    def test_two_tenant_fixture_uses_scoped_synthetic_users_and_one_fresh_database(self):
        self.apply_two_tenant_patches()
        original_program = (self.source / 'Program.cs').read_bytes()
        fixture_root = self.prepare(two_tenant=True,
                                    optional_fixture_patches=(self.menu_patch, self.layout_patch, self.tenant_patch))
        try:
            content_root = fixture_root / 'content-root'
            config = json.loads((content_root / 'appsettings.json').read_text())
            plan = json.loads((fixture_root / 'launch-plan.json').read_text())
            users = config['Identity']['Users']
            roles = config['Identity']['Roles']
            tenants = config['Multitenancy']['Tenants']
            credentials = (fixture_root / 'synthetic-credentials.txt').read_text()

            self.assertEqual({'tenant-a', 'tenant-b'}, {user['TenantId'] for user in users})
            self.assertEqual(3, len(users))
            denied = next(user for user in users if user['Name'] == 'synthetic-denied')
            self.assertEqual('tenant-a', denied['TenantId'])
            self.assertEqual([], denied['Roles'])
            self.assertEqual({'tenant-a', 'tenant-b'}, {role['TenantId'] for role in roles})
            self.assertTrue(all(role['Permissions'] == ['*'] for role in roles))
            self.assertEqual({'tenant-a', 'tenant-b'}, {tenant['Id'] for tenant in tenants})
            self.assertEqual({'127.0.0.1', 'tenant-b.localhost'},
                             {tenant['Configuration']['Http']['Host'].split(':')[0] for tenant in tenants})
            self.assertEqual(1, len({tenant['Configuration']['ConnectionStrings']['Sqlite'] for tenant in tenants}))
            self.assertEqual(config['ConnectionStrings']['Sqlite'], tenants[0]['Configuration']['ConnectionStrings']['Sqlite'])
            self.assertTrue(FIXTURE.is_within(Path(plan['databasePath']), content_root))
            self.assertEqual(hashlib.sha256((content_root / 'appsettings.json').read_bytes()).hexdigest(),
                             plan['privateAppsettingsSha256'])
            self.assertTrue(config['Features']['Multitenancy']['Enabled'])
            self.assertNotIn('Secrets', config['Features'])
            self.assertIn('--Features:Secrets:Enabled=true', (fixture_root / 'launch-command.txt').read_text())
            self.assertIn('TenantId claims', plan['tenantIsolationPolicy']['selection'])
            self.assertTrue(plan['tenantIsolationPolicy']['databasePathShared'])
            self.assertTrue(plan['twoTenantMode'])
            self.assertTrue(plan['multitenancyEnabledOnlyInFixtureConfiguration'])
            self.assertEqual({'studio-secrets-menu.patch', 'studio-bpmn-generator-layout.patch',
                              'workbench-two-tenant-multitenancy.patch'},
                             {Path(item['path']).name for item in plan['sourcePatchChain']['optionalFixturePatches']})
            self.assertTrue(all(len(item['sha256']) == 64
                                for item in plan['sourcePatchChain']['optionalFixturePatches']))
            self.assertEqual(2, credentials.count('Username: synthetic-tenant-'))
            self.assertIn('Username: synthetic-denied', credentials)
            self.assertEqual(3, credentials.count('Password: '))
            self.assertEqual(original_program, (self.source / 'Program.cs').read_bytes())
        finally:
            FIXTURE.cleanup_fixture(fixture_root, host_stopped=True)

    def test_two_tenant_mode_requires_both_fixture_patches_before_build(self):
        build = self.fake_build
        with mock.patch.object(FIXTURE, 'build_host', wraps=build) as build_host:
            with self.assertRaisesRegex(ValueError, 'requires the reviewed Workbench multitenancy patch'):
                self.prepare(two_tenant=True,
                             optional_fixture_patches=(self.menu_patch, self.layout_patch, self.tenant_patch))
        build_host.assert_not_called()

    def test_supplemental_patch_overlay_is_reversed_before_workbench_patch(self):
        program = self.source / 'Program.cs'
        before = program.read_text()
        after = before.replace('elsa.UseSecretsJavaScript();', 'elsa.UseSecretsJavaScript();\n// supplemental fixture overlay')
        self.assertNotEqual(before, after)
        relative = program.relative_to(self.rehearsal).as_posix()
        diff = ''.join(difflib.unified_diff(
            before.splitlines(keepends=True), after.splitlines(keepends=True),
            fromfile=f'a/{relative}', tofile=f'b/{relative}'))
        patch = self.base / 'supplemental-overlay.patch'
        patch.write_text(f'diff --git a/{relative} b/{relative}\n{diff}')
        subprocess.run(['git', 'apply', str(patch)], cwd=self.rehearsal, check=True)

        fixture_root = self.prepare(supplemental_patches=(patch,))
        try:
            plan = json.loads((fixture_root / 'launch-plan.json').read_text())
            chain = plan['sourcePatchChain']
            self.assertEqual(1, len(chain['supplementalPatches']))
            self.assertEqual(2, len(chain['reverseReplay']))
            self.assertTrue(chain['previousToCurrentTransitionVerified'])
            self.assertNotEqual(chain['previousWorkbenchPatchSha256'], chain['workbenchPatch']['sha256'])
            self.assertEqual(patch.name, chain['supplementalPatches'][0]['path'])
            self.assertTrue(chain['reverseReplay'][0]['patch'].endswith('supplemental-overlay.patch'))
            self.assertTrue(chain['reverseReplay'][1]['patch'].endswith('workbench.patch'))
        finally:
            FIXTURE.cleanup_fixture(fixture_root, host_stopped=True)

    def test_unapplied_overlay_sharing_prepared_file_is_not_inferred(self):
        program = self.source / 'Program.cs'
        before = program.read_text()
        after = before + '// supplemental fixture overlay\n'
        relative = program.relative_to(self.rehearsal).as_posix()
        diff = ''.join(difflib.unified_diff(
            before.splitlines(keepends=True), after.splitlines(keepends=True),
            fromfile=f'a/{relative}', tofile=f'b/{relative}'))
        patch = self.base / 'unapplied-supplemental-overlay.patch'
        patch.write_text(f'diff --git a/{relative} b/{relative}\n{diff}')

        fixture_root = self.prepare(supplemental_patches=(patch,))
        try:
            plan = json.loads((fixture_root / 'launch-plan.json').read_text())
            self.assertEqual([], plan['sourcePatchChain']['supplementalPatches'])
            self.assertEqual(['workbench.patch'], [
                Path(item['patch']).name for item in plan['sourcePatchChain']['reverseReplay']
            ])
        finally:
            FIXTURE.cleanup_fixture(fixture_root, host_stopped=True)

    def test_rejects_mixed_supplemental_patch_set_before_build(self):
        program = self.source / 'Program.cs'
        relative = program.relative_to(self.rehearsal).as_posix()
        before = program.read_text()
        after = before + '// applied supplemental overlay\n'
        applied_patch = self.base / 'applied-supplemental-overlay.patch'
        applied_patch.write_text(self.make_patch(relative, before, after))
        subprocess.run(['git', 'apply', str(applied_patch)], cwd=self.rehearsal, check=True)

        build = self.fake_build
        with mock.patch.object(FIXTURE, 'build_host', wraps=build) as build_host:
            with self.assertRaisesRegex(ValueError, 'Only part of the supplemental patch set is present'):
                self.prepare(supplemental_patches=(applied_patch, self.menu_patch))
        build_host.assert_not_called()

    def test_rejects_receipt_pin_mismatch_before_build(self):
        path = self.rehearsal / 'import-receipt.json'
        receipt = json.loads(path.read_text())
        receipt['sourceCommits']['extensions'] = 'd' * 40
        path.write_text(json.dumps(receipt))
        with self.assertRaisesRegex(ValueError, 'Import receipt source pins'):
            self.prepare()

    def test_fixture_rejects_compile_items_outside_host_source_inventory(self):
        def unexpected_compile_items(source):
            command, items = self.fake_compile_items(source)
            items.append({'FullPath': str(source.parent / 'OtherHost.cs')})
            return command, items

        with self.assertRaisesRegex(ValueError, 'source outside the host project'):
            self.prepare(compile_items_provider=unexpected_compile_items)

    def test_compile_inventory_parses_msbuild_json_after_tool_output(self):
        output = 'build-slot: no wait\n' + json.dumps({
            'Items': {'Compile': [{'FullPath': str(self.source / 'Program.cs')}]}
        })
        result = subprocess.CompletedProcess([], 0, output, '')
        with mock.patch.object(FIXTURE.subprocess, 'run', return_value=result) as run:
            command, items = FIXTURE.get_compile_items(self.source)

        self.assertIn('-getItem:Compile', command)
        self.assertEqual(str(self.source / 'Program.cs'), items[0]['FullPath'])
        self.assertEqual(self.source, Path(run.call_args.kwargs['cwd']))

    def test_host_build_uses_clone_local_restore_and_rebuilds_project_references(self):
        project = self.source / 'Elsa.Server.Web.csproj'
        assets = self.source / 'obj' / 'project.assets.json'
        assets.parent.mkdir(parents=True)
        assets.write_text(json.dumps({'project': {'restore': {'projectPath': str(project)}}}))
        host_dll = self.source / 'bin' / 'Debug' / 'net10.0' / 'Elsa.Server.Web.dll'
        host_dll.parent.mkdir(parents=True)
        host_dll.write_bytes(b'host')
        for name in FIXTURE.REQUIRED_SECRETS_ASSEMBLIES:
            project_output = self.rehearsal / 'src' / 'modules' / name / 'bin' / 'Debug' / 'net10.0' / f'{name}.dll'
            project_output.parent.mkdir(parents=True)
            project_output.write_bytes(b'synthetic assembly bytes')
            (host_dll.parent / f'{name}.dll').write_bytes(b'synthetic assembly bytes')
        results = [
            subprocess.CompletedProcess([], 0, 'restore passed', ''),
            subprocess.CompletedProcess([], 0, 'build passed', '')
        ]
        with mock.patch.object(FIXTURE.subprocess, 'run', side_effect=results) as run:
            build = FIXTURE.build_host(self.source, self.temp_parent)

        self.assertEqual(2, run.call_count)
        restore_command = run.call_args_list[0].args[0]
        self.assertEqual('restore', restore_command[1])
        self.assertIn('--force-evaluate', restore_command)
        build_command = run.call_args_list[1].args[0]
        self.assertIn('--no-restore', build_command)
        self.assertIn('--no-incremental', build_command)
        self.assertNotIn('-p:BuildProjectReferences=false', build_command)
        self.assertEqual(0, build['exitCode'])
        self.assertEqual(str(host_dll.resolve()), build['hostDll'])
        self.assertEqual(set(FIXTURE.REQUIRED_SECRETS_ASSEMBLIES),
                         {item['name'] for item in build['secretsAssemblies']})

        (host_dll.parent / f'{FIXTURE.REQUIRED_SECRETS_ASSEMBLIES[0]}.dll').write_bytes(b'stale host copy')
        with mock.patch.object(FIXTURE.subprocess, 'run', side_effect=results):
            with self.assertRaisesRegex(ValueError, 'Host Secrets assembly differs'):
                FIXTURE.build_host(self.source, self.temp_parent)


if __name__ == '__main__':
    unittest.main()
