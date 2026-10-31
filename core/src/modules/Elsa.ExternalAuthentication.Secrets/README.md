# Elsa External Authentication: Elsa Secrets

This optional package connects External Authentication Secret Bindings to Elsa Secrets.

```csharp
services.AddElsaSecretsExternalAuthentication();
```

Use resolver type `elsa-secrets` and set `reference` to the Elsa Secret name:

```json
{
  "resolverType": "elsa-secrets",
  "reference": "external-authentication/contoso/client-secret",
  "expectedType": "text",
  "expectedScope": "external-authentication"
}
```

The referenced secret must be active and have an active version. Optional expected type and scope constraints must match. Management APIs expose only whether the binding is configured and resolvable; values and generation fingerprints are never returned.

Every resolution produces a keyed, non-reversible generation fingerprint. Replacing or rotating a secret therefore changes the connection’s effective material revision and invalidates in-flight authentication transactions that started with the previous generation.
