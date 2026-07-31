#!/usr/bin/env bash

# Define the modules to update.
# External authentication moved to Elsa.ExternalAuthentication.Persistence.EFCore; see efcore-initial.sh there.
# V3_8 for Identity makes User.HashedPassword and User.HashedPasswordSalt nullable for credential-less users.
mods=("Identity")

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
        ef-migration-runtime-schema --interface Elsa.Persistence.EFCore.IElsaDbContextSchema --efOptions "migrations add V3_8 -c ""$module""ElsaDbContext -p ""$providerPath""  -o ""$migrationsPath"" --startup-project ""$startupProject"" --framework net9.0"
    done
done
