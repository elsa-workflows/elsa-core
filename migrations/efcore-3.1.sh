#!/usr/bin/env bash

# Define the modules to update
mods=("Management" "Runtime")
# mods=("Alterations" "Runtime" "Management" "Identity" "Labels")

# Define the list of providers
providers=("MySql" "SqlServer" "Sqlite" "PostgreSql")
# providers=("SqlServer")

# Loop through each module
for module in "${mods[@]}"; do
    # Loop through each provider
    for provider in "${providers[@]}"; do
        providerPath="../src/modules/Elsa.EntityFrameworkCore.$provider"
        migrationsPath="Migrations/$module"
    
        echo "Updating migrations for $provider..."
        echo "Provider path: ${providerPath:?}/${migrationsPath}"
        echo "Migrations path: $migrationsPath"
        
        # 1. Remove all migrations except the initial and restore the snapshot to that state.
        migrations=$(dotnet ef migrations list --no-connect -c "$module"ElsaDbContext  -p "$providerPath" | grep -v ^Build | grep -v ^"Pending status" | grep -v Initial$)
                
        for i in ${migrations}; do
          dotnet ef migrations remove -f -c "$module"ElsaDbContext -p "$providerPath"
        done
            
        # 2. Run the migrations command
        dotnet ef migrations add V3_1 -c "$module"ElsaDbContext -p "$providerPath"  -o "$migrationsPath"
    done
done
