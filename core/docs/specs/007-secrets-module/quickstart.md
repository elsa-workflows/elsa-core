# Quickstart: Secrets Module

## Configure the module

```csharp
services.AddElsa(elsa =>
{
    elsa.UseSecrets(secrets =>
    {
        secrets.ConfigureOptions = options => options.ConfigurationSectionName = "Elsa:Secrets";
    });
});
```

The v1 Core/server feature provides:

- Elsa-managed encrypted store.
- Configuration-backed read-only store.
- Metadata-only management APIs.
- Runtime latest-active resolution by immutable technical name.
- Studio management and picker UX in the paired `elsa-studio` module.

## Reference a secret from workflow or module settings

Workflow definitions and module settings store a secret reference, not a value:

```json
{
  "technicalName": "orders-db-connection",
  "requiredType": "Text",
  "requiredScope": "ConnectionString"
}
```

At runtime, callers resolve the latest active version:

```csharp
var secret = await secretResolver.ResolveAsync(new SecretReference
{
    TechnicalName = "orders-db-connection",
    RequiredType = "Text",
    RequiredScope = "ConnectionString"
}, cancellationToken);
```

## Configure read-only secrets

```json
{
  "Elsa": {
    "Secrets": {
      "orders-db-connection": "Host=localhost;Database=orders;Username=elsa;Password=..."
    }
  }
}
```

Configuration-backed secrets can be selected and resolved, but not rotated or deleted through Elsa.

## Security rules

- Technical names are immutable.
- Current cleartext values cannot be revealed after creation.
- General API responses are metadata-only.
- Tests and audit events never contain raw values.
- Import conflicts require explicit create-new, update/rotate, or skip behavior.

## Studio

Register the paired Studio module with the same backend configuration:

```csharp
services.AddSecretsModule(backendApiConfig);
```

The Studio module adds `/security/secrets` for management plus a reusable `SecretPicker` component for sensitive editors.

## Validation

Run targeted checks after implementation:

```bash
dotnet build src/modules/Elsa.Secrets/Elsa.Secrets.csproj
dotnet test test/unit/Elsa.Secrets.UnitTests/Elsa.Secrets.UnitTests.csproj
dotnet build /Users/sipke/.codex/worktrees/ae40/elsa-studio/src/modules/Elsa.Studio.Secrets/Elsa.Studio.Secrets.csproj
```
