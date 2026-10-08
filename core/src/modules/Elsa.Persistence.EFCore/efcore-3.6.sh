#!/usr/bin/env bash

# Define the modules to update
mods=("Management", "Runtime")

# Define the list of providers
providers=("MySql" "SqlServer" "Sqlite" "PostgreSql" "Oracle")

# Loop through each module
for module in "${mods[@]}"; do
    # Loop through each provider
    for provider in "${providers[@]}"; do
        providerPath="../Elsa.Persistence.EFCore.$provider"
        startupProject="$providerPath/Elsa.Persistence.EFCore.$provider.csproj"
        migrationsPath="Migrations/$module"
    
        echo "Updating migrations for $provider..."
        echo "Provider path: ${providerPath:?}"
        echo "Startup project: $startupProject"
        echo "Migrations path: $migrationsPath"
        ef-migration-runtime-schema --interface Elsa.Persistence.EFCore.IElsaDbContextSchema --efOptions "migrations add V3_6 -c ""$module""ElsaDbContext -p ""$providerPath""  -o ""$migrationsPath"" --startup-project ""$startupProject"""
    done
done
