#!/usr/bin/env zsh

# Define the modules to update
# mods=("Runtime")
mods=("Alterations" "Runtime" "Management" "Identity" "Labels")

# Define the list of providers
providers=("MySql" "SqlServer" "Sqlite" "PostgreSql")
# providers=("SqlServer")

# Connection strings for each provider
typeset -A connStrings
connStrings=(
    MySql "Server=localhost;Port=3306;Database=elsa;User=root;Password=password;"
    SqlServer ""
    Sqlite ""
    PostgreSql ""
)

# Loop through each module
for module in "${mods[@]}"; do
    # Loop through each provider
    for provider in "${providers[@]}"; do
        providerPath="./src/modules/Elsa.EntityFrameworkCore.$provider"
        migrationsPath="Migrations/$module"
    
        echo "Updating migrations for $provider..."
        echo "Provider path: ${providerPath:?}/${migrationsPath}"
        echo "Migrations path: $migrationsPath"
        echo "Connection string: ${connStrings[$provider]}"
    
        # 1. Delete the existing migrations folder
        rm -rf "${providerPath:?}/${migrationsPath}"
    
        # 2. Run the migrations command
        dotnet ef migrations add Initial -c "$module"ElsaDbContext -p "$providerPath"  -o "$migrationsPath" -- --connectionString "${connStrings[$provider]}"
    done
done
