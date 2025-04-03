#!/usr/bin/env bash

# Define the modules to update
mods=("Runtime")

# Define the list of providers
providers=("MySql" "SqlServer" "Sqlite" "PostgreSql" "Oracle")

# Loop through each module
for module in "${mods[@]}"; do
    # Loop through each provider
    for provider in "${providers[@]}"; do
        providerPath="../../src/modules/Elsa.EntityFrameworkCore.$provider"
        migrationsPath="Migrations/$module"
    
        echo "Updating migrations for $provider..."
        echo "Provider path: ${providerPath:?}/${migrationsPath}"
        echo "Migrations path: $migrationsPath"
        ef-migration-runtime-schema --interface Elsa.EntityFrameworkCore.IElsaDbContextSchema --efOptions "migrations add V3_5 -c ""$module""ElsaDbContext -p ""$providerPath""  -o ""$migrationsPath"""
    done
done
