"""Unit tests for the static Secrets contract comparison helpers."""
import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location(
    'secrets_contracts', Path(__file__).with_name('compare-secrets-contracts.py'))
contracts = importlib.util.module_from_spec(spec)
spec.loader.exec_module(contracts)


def run_git(repo, *args):
    return subprocess.run(
        ['git', '-C', str(repo), *args],
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()


def write_source(repo, path, source):
    file = repo / path
    file.parent.mkdir(parents=True, exist_ok=True)
    file.write_text(source)


def commit(repo, message):
    run_git(repo, 'add', '-A')
    run_git(repo, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.test',
            '-c', 'commit.gpgsign=false', 'commit', '-m', message)
    return run_git(repo, 'rev-parse', 'HEAD')


def create_comparison_repositories(root):
    core = root / 'core'
    extensions = root / 'extensions'
    core.mkdir()
    extensions.mkdir()
    for repo in (core, extensions):
        run_git(repo, 'init', '-q')

    write_source(
        core,
        'src/modules/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/Secrets/20260531141623_Initial.cs',
        '''public class Initial
{
    public void Up()
    {
        Id = table.Column<string>();
        Name = table.Column<string>();
    }
}''')
    write_source(
        core,
        'src/modules/Elsa.Secrets/Endpoints/Secrets/List.cs',
        'public class ListEndpoint { public void Map() { Get("/secrets"); Get("/secrets/{name}"); } }')
    core_published = commit(core, 'published core')
    write_source(
        core,
        'src/modules/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/Secrets/20260914120000_SecretDefaultTenantUniqueness.cs',
        'public class SecretDefaultTenantUniqueness { }')
    core_candidate = commit(core, 'candidate core')

    write_source(
        extensions,
        'src/modules/secrets/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/20240915164114_V3_3.cs',
        '''public class V3_3
{
    public void Up()
    {
        Id = table.Column<string>();
        EncryptedValue = table.Column<string>();
        Version = table.Column<int>();
    }
}''')
    write_source(
        extensions,
        'src/modules/secrets/Elsa.Secrets.Api/Endpoints/Secrets/Lookup.cs',
        'public class LookupEndpoint { public void Map() { Get("/secrets"); Get("/secrets/{id}"); } }')
    extensions_381 = commit(extensions, 'published extensions 3.8.1')

    projects = sorted(contracts.COLLISION_PROJECTS | contracts.LEGACY_SUPPORT_PROJECTS)
    for name in projects:
        write_source(extensions, f'src/modules/secrets/{name}/{name}.csproj', '<Project />')
    extensions_384 = commit(extensions, 'published extensions project inventory')
    return core, extensions, {
        'core_3_8_4': core_published,
        'core_candidate': core_candidate,
        'extensions_3_8_1': extensions_381,
        'extensions_3_8_4': extensions_384,
    }


class RouteComparisonTests(unittest.TestCase):
    def test_parameter_names_do_not_hide_route_collision(self):
        left = contracts.extract_routes('Get("/secrets/{id}"); Delete("/secrets/{id}");')
        right = contracts.extract_routes('Get("/secrets/{name}"); Delete("/secrets/{name}");')

        self.assertEqual(contracts.route_conflicts(left, right), [
            ('DELETE', '/secrets/{}'),
            ('GET', '/secrets/{}'),
        ])

    def test_client_and_endpoint_route_shapes_are_both_readable(self):
        client = contracts.extract_routes('[Get("/secrets")] [Post("/secrets/{name}/rotate")]')
        endpoint = contracts.extract_routes('Get("/secrets"); Post("/secrets/{name}/rotate");')

        self.assertEqual(client, endpoint)


class SchemaComparisonTests(unittest.TestCase):
    def test_columns_are_extracted_from_ef_migration_table_definition(self):
        migration = '''
            migrationBuilder.CreateTable(
                name: "Secrets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedValue = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                });
        '''

        self.assertEqual(contracts.extract_columns(migration), ['EncryptedValue', 'Id', 'Version'])


class ComparisonWorkflowTests(unittest.TestCase):
    def test_compare_reads_pinned_git_sources_and_assembles_report(self):
        with tempfile.TemporaryDirectory() as temp:
            core, extensions, pins = create_comparison_repositories(Path(temp))
            with patch.dict(contracts.PINS, pins):
                report = contracts.compare(core, extensions)

        self.assertEqual(report['sourceCommits'], pins)
        self.assertEqual(report['sqlite']['extensions3_8_1V3_3MigrationColumns'],
                         ['EncryptedValue', 'Id', 'Version'])
        self.assertEqual(report['sqlite']['core3_8_4InitialMigrationColumns'], ['Id', 'Name'])
        self.assertEqual(report['sqlite']['coreCandidateInitialMigrationColumns'], ['Id', 'Name'])
        self.assertEqual(report['sqlite']['core3_8_4Migrations'], ['20260531141623_Initial.cs'])
        self.assertEqual(report['sqlite']['coreCandidateMigrations'], [
            '20260531141623_Initial.cs', '20260914120000_SecretDefaultTenantUniqueness.cs'])
        self.assertEqual(report['apiRoutes']['publishedRouteConflicts'], [
            ('GET', '/secrets'), ('GET', '/secrets/{}')])
        self.assertEqual(report['extensionProjectsAt3_8_4SourceCommit']['collisionCopies'],
                         sorted(contracts.COLLISION_PROJECTS))
        self.assertEqual(report['extensionProjectsAt3_8_4SourceCommit']['legacySupportPackages'],
                         sorted(contracts.LEGACY_SUPPORT_PROJECTS))
        self.assertFalse(report['migrationExecutionVerified'])
        self.assertFalse(report['credentialConversionVerified'])

    def test_compare_rejects_an_unavailable_pin(self):
        with tempfile.TemporaryDirectory() as temp:
            core, extensions, pins = create_comparison_repositories(Path(temp))
            pins['core_candidate'] = 'missing-candidate-pin'
            with patch.dict(contracts.PINS, pins):
                with self.assertRaisesRegex(ValueError, "Unable to resolve source pin 'missing-candidate-pin'"):
                    contracts.compare(core, extensions)


if __name__ == '__main__':
    unittest.main()
