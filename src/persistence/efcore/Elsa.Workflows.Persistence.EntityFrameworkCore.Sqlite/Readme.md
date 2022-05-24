### Entity Framework Core Commands

**Generate Migrations**

```
dotnet ef migrations add Initial
dotnet ef migrations add Update1
dotnet ef migrations add Update2
```

etc..

**Apply Migrations Manually**

`dotnet ef database update -- "{connection string}"`