dotnet ef migrations add Initial -c ActivityDefinitionsElsaDbContext -o Migrations/ActivityDefinitions
dotnet ef migrations add Initial -c LabelsElsaDbContext -o Migrations/Labels
dotnet ef migrations add Initial -c ManagementElsaDbContext -o Migrations/Management
dotnet ef migrations add Initial -c RuntimeElsaDbContext -o Migrations/Runtime
dotnet ef migrations add Initial -c IdentityElsaDbContext -o Migrations/Identity