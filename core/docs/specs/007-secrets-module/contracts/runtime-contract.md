# Runtime Contract: Secrets Module

## Secret Resolver

Runtime consumers resolve by immutable technical name. Implementations return latest active versions only and never expose cleartext through metadata APIs.

```csharp
public interface ISecretResolver
{
    ValueTask<ResolvedSecret> ResolveAsync(SecretReference reference, CancellationToken cancellationToken = default);
}
```

Resolution failures use safe error codes such as `NotFound`, `Inactive`, `Expired`, `Revoked`, `TypeMismatch`, `ScopeMismatch`, `StoreUnavailable`, or `Unauthorized`.

## Secret Manager

Management operations create logical secrets, rotate values, revoke/delete secrets, and return metadata-only models.

```csharp
public interface ISecretManager
{
    ValueTask<SecretMetadata> CreateAsync(CreateSecretRequest request, CancellationToken cancellationToken = default);
    ValueTask<SecretMetadata> RotateAsync(string technicalName, RotateSecretRequest request, CancellationToken cancellationToken = default);
    ValueTask<SecretMetadata?> FindAsync(string technicalName, CancellationToken cancellationToken = default);
    ValueTask<Page<SecretMetadata>> ListAsync(SecretQuery query, CancellationToken cancellationToken = default);
    ValueTask RevokeAsync(string technicalName, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string technicalName, CancellationToken cancellationToken = default);
}
```

Rules:

- `technicalName` is immutable after creation.
- `RotateAsync` retires the current latest active version and creates a new latest active version.
- No method returns current cleartext values.

## Secret Store

Stores own payload persistence and declare their capabilities.

```csharp
public interface ISecretStore
{
    SecretStoreDescriptor Descriptor { get; }
    ValueTask<SecretPayload> WriteAsync(SecretWriteContext context, CancellationToken cancellationToken = default);
    ValueTask<SecretPayload?> ReadAsync(SecretReadContext context, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(SecretDeleteContext context, CancellationToken cancellationToken = default);
    ValueTask<SecretTestResult> TestAsync(SecretTestContext context, CancellationToken cancellationToken = default);
}
```

Capability rules:

- Configuration store supports read/test where possible, not write/delete/rotate.
- Elsa encrypted store supports read/write/delete/rotate/encrypted export.
- Store-private payloads are not exposed through general metadata APIs.

## Secret Type Provider

Types provide validation, generation, payload shaping, and Studio editor metadata.

```csharp
public interface ISecretTypeProvider
{
    SecretTypeDescriptor Descriptor { get; }
    ValueTask ValidateAsync(SecretTypeValidationContext context, CancellationToken cancellationToken = default);
    ValueTask<SecretPayloadInput> GenerateAsync(SecretGenerationContext context, CancellationToken cancellationToken = default);
}
```

V1 built-in types are `Text`, `RsaKey`, and `X509CertificateReference`.

## Compatibility Adapter

Existing `ISecretProvider.GetSecretAsync(name)` consumers continue through an adapter.

```csharp
public interface ISecretProvider
{
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}
```

Adapter behavior:

- Map `name` to `SecretReference.TechnicalName`.
- Resolve latest active version.
- Return `null` only for legacy compatibility where the current provider did so; new APIs should return structured failures.
