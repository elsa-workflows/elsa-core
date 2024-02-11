#!/usr/bin/env zsh

# Define the previous migration name
previousMigrationName="Initial"

# Define the version of the migration
version="3_1"

# Define the modules to update
mods=("Alterations" "Runtime" "Management" "Identity" "Labels")
# mods=("Runtime" "Management" "Identity" "Labels")

# Define the list of providers
# providers=("MySql" "SqlServer" "Sqlite" "PostgreSql")
providers=("SqlServer" "Sqlite" "PostgreSql")

# Connection strings for each provider
typeset -A connStrings
connStrings=(
    MySql "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;"
    SqlServer ""
    Sqlite "Data Source=App_Data/elsa.sqlite.db;Cache=Shared;"
    PostgreSql ""
)

# Loop through each module
for module in "${mods[@]}"; do
    # Loop through each provider
    for provider in "${providers[@]}"; do
        providerPath="../src/modules/Elsa.EntityFrameworkCore.$provider"
        migrationsPath="Migrations/$module"
    
        echo "Updating migrations for $provider..."
        echo "Provider path: ${providerPath:?}/${migrationsPath}"
        echo "Migrations path: $migrationsPath"
        echo "Connection string: ${connStrings[$provider]}"
        
#        # 1. Remove the existing migrations
#        LAST_MIGRATION=$(dotnet ef migrations list -c "$module"ElsaDbContext -p "$providerPath" -- --connectionString "${connStrings[$provider]}" | tail -n 1)
#        MIGRATION_NAME=$(echo $LAST_MIGRATION | cut -d'_' -f 2- | cut -d' ' -f 1 | tr -d '[:space:]')
#        
#        echo "Last migration: $LAST_MIGRATION"
#        echo "Migration name: $MIGRATION_NAME"
#        
#        if [ "${MIGRATION_NAME}" != "${previousMigrationName}" ]; then
#          echo "Attemtping to remove migration V${version}..."
#          dotnet ef migrations remove -c "$module"ElsaDbContext -p "$providerPath" -- --connectionString "${connStrings[$provider]}"
#        fi
        
        # 2. Run the migrations command
        dotnet ef migrations add V$version -c "$module"ElsaDbContext -p "$providerPath"  -o "$migrationsPath" -- --connectionString "${connStrings[$provider]}"
    done
done
