#!/usr/bin/env bash

# Define the modules to update
mods=("Alterations" "Runtime" "Management" "Identity")

# Define the list of providers
providers=("MySql" "SqlServer" "Sqlite" "PostgreSql" "Oracle")

# Loop through each module
for module in "${mods[@]}"; do
    # Loop through each provider
    for provider in "${providers[@]}"; do
        providerPath="../src/modules/Elsa.EntityFrameworkCore.$provider"
        sqlPath="$providerPath/Migrations/$module/v3.1.sql"
    
        echo "Generating SQL for $provider..."
        echo "SQL path: $sqlPath"
        dotnet ef migrations script -p "$providerPath" -c "$module"ElsaDbContext -o "$sqlPath"
        
        # Make the file writable:
        chmod 777 "$sqlPath"
    done
done
