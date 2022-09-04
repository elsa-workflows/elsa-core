```shell
dotnet ef migrations add Initial -c ActivityDefinitionsDbContext -o Modules/ActivityDefinitions/Migrations
dotnet ef migrations add Initial -c LabelsDbContext -o Modules/Labels/Migrations
dotnet ef migrations add Initial -c ManagementDbContext -o Modules/Management/Migrations
dotnet ef migrations add Initial -c RuntimeDbContext -o Modules/Runtime/Migrations
```