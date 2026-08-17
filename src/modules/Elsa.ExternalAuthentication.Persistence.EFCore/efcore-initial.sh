#!/usr/bin/env bash

# Creates the initial migration for ExternalAuthenticationElsaDbContext across every provider.
# Run from this directory so the relative provider paths resolve.
#
# dotnet-ef is pinned to 9.0.11 with rollForward disabled, so the target framework must be pinned to net9.0.
# ef-migration-runtime-schema rewrites the generated migration to take IElsaDbContextSchema, which is what
# keeps the migration usable with a non-default schema name.

migrationName="${1:-Initial}"

# Define the list of providers
providers=("MySql" "SqlServer" "Sqlite" "PostgreSql" "Oracle")

for provider in "${providers[@]}"; do
    providerPath="../Elsa.ExternalAuthentication.Persistence.EFCore.$provider"
    startupProject="$providerPath/Elsa.ExternalAuthentication.Persistence.EFCore.$provider.csproj"
    migrationsPath="Migrations/ExternalAuthentication"

    echo "Creating $migrationName migration for $provider..."
    echo "Provider path: ${providerPath:?}"
    echo "Startup project: $startupProject"
    echo "Migrations path: $migrationsPath"
    ef-migration-runtime-schema --interface Elsa.Persistence.EFCore.IElsaDbContextSchema --efOptions "migrations add ""$migrationName"" -c ExternalAuthenticationElsaDbContext -p ""$providerPath"" -o ""$migrationsPath"" --startup-project ""$startupProject"" --framework net9.0"
done
