dotnet ef migrations add Initial -c ActivityDefinitionsDbContext -o Migrations/ActivityDefinitions
dotnet ef migrations add Initial -c LabelsDbContext -o Migrations/Labels
dotnet ef migrations add Initial -c ManagementDbContext -o Migrations/Management
dotnet ef migrations add Initial -c RuntimeDbContext -o Migrations/Runtime