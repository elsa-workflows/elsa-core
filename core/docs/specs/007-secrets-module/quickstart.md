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

## Rotate the encrypted store key

For a hosted production deployment with the Elsa Secrets feature, supply a new random key from external
configuration before starting the service. For ModularServer the default shell setting is
`CShells__Shells__Default__Features__Secrets__EncryptionKey`, with a base64 value. Configure each shell that
uses Secrets, including shells that enable it through a feature dependency.
The sample key is rejected in Production.

For retained encrypted data, rehearse against a restored backup first. Stop all writers to the repository,
keep a verified backup and retain the old key in a separate secret store. Put the replacement in `EncryptionKey`
and the old key in `PreviousEncryptionKeys` temporarily. Previous keys decrypt existing envelopes only.
New writes always use the current key.

Resolve `SecretKeyRotation` in a private maintenance process using the same repository and tenant configuration
as the server. Activate each tenant scope that owns encrypted records, including the default or shared scope
where applicable. Run `ReencryptAsync()` to validate and count old-key envelopes without writing. Then run
`ReencryptAsync(dryRun: false)` with exclusive repository access. For a shared EF Core record, the trusted
maintenance process must also hold `SharedTenantWriteScope.Enter<Secret>()` around the write pass. Dispose
the scope before returning to normal tenant operations. Rotation preserves retained version metadata,
including status, expiry and managed ownership. If interrupted, keep the previous key active and rerun the same
operation. Writes are committed per secret, so an interrupted run may contain envelopes under both keys.

Repeat the dry run until it reports zero and verify representative reads with only the new key. Remove the previous
key and restart all writers only after those checks pass. To roll back, restore the intact backup and its matching key.
Keep the new key protected for any mixed-key backups. The operation rewrites ciphertext and cannot undo prior
disclosure of the underlying credentials.
