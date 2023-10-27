#!/usr/bin/env zsh

# Define the modules to update
mods=("Runtime")
# mods=("Alterations" "Runtime" "Management" "Identity" "Labels")

# Define the list of providers
providers=("MySql" "SqlServer" "Sqlite" "PostgreSql")
# providers=("SqlServer")

# Connection strings for each provider
typeset -A connStrings
connStrings=(
    MySql "Host=localhost;Database=elsa_efcore;Username=mysql;Password=password"
    SqlServer "Host=localhost;Database=elsa_efcore;Username=sqlserver;Password=password"
    Sqlite "Data Source=elsa.sqlite.db"
    PostgreSql "Host=localhost;Database=elsa_efcore;Username=postgres;Password=Test1234!;Pooling=true;"
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
