#!/bin/bash

# Define the list of providers
providers=("MySql" "SqlServer" "Sqlite" "PostgreSql")

# Connection strings for each provider (these are just placeholder examples, adjust accordingly)
declare -A connStrings
connStrings["MySql"]="Server=localhost;Port=3306;Database=elsa;User=root;Password=password;"
connStrings["SqlServer"]=""
connStrings["Sqlite"]=""
connStrings["PostgreSql"]=""

# Loop through each provider
for provider in "${providers[@]}"; do
    modulePath="./src/modules/Elsa.EntityFrameworkCore.$provider"
    migrationsPath="$modulePath/Migrations/Runtime"

    cd $modulePath

    # 1. Delete the existing migrations folder
    rm -rf $migrationsPath

    # 2. Run the migrations command
    dotnet ef migrations add Initial -c RuntimeElsaDbContext -o $migrationsPath -- "${connStrings[$provider]}"
done
