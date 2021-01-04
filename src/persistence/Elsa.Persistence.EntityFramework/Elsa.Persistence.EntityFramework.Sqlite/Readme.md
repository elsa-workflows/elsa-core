### Entity Framework Core Commands

**Generate Migrations**

```
dotnet ef migrations add Initial
dotnet ef migrations add Update1
dotnet ef migrations add Update2
```

etc..

Optionally, specify a connection string:

```
dotnet ef migrations add Initial -- "{connection string}"
dotnet ef migrations add Update1 -- "{connection string}"
dotnet ef migrations add Update2 -- "{connection string}"
```

**Apply Migrations**

`dotnet ef database update -- "{connection string}"`