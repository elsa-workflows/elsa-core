"""Unit tests for the static Secrets contract comparison helpers."""
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location(
    'secrets_contracts', Path(__file__).with_name('compare-secrets-contracts.py'))
contracts = importlib.util.module_from_spec(spec)
spec.loader.exec_module(contracts)


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


if __name__ == '__main__':
    unittest.main()
