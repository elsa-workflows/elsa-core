# Moving from the legacy Extensions Secrets module to Core Secrets

Elsa 3.10.0 ships one Secrets implementation: Core's `Elsa.Secrets`, its `Elsa.Secrets.Persistence.EFCore*`
providers, `Elsa.Secrets.JavaScript` and the Studio `Elsa.Studio.Secrets` module. The older Extensions Secrets
module is not carried forward. This guide is for hosts that use it, which means any of:

- `Elsa.Secrets.Api`, `Elsa.Secrets.Core`, `Elsa.Secrets.Management`, `Elsa.Secrets.Models` or
  `Elsa.Secrets.Scripting`;
- the 3.8.1 `Elsa.Secrets.Persistence.EFCore*` packages, which were built from the Extensions module and create a
  per-version `Secrets` table (`SecretId`, `EncryptedValue`, `IsLatest`, `Owner`, ...).

If your host already uses Core Secrets (3.8.2 or later persistence packages and the `/secrets/{name}` routes), this
guide does not apply; see [Secrets become tenant-scoped](secrets-tenancy.md) instead.

## What changes

- **The legacy packages are deprecated at 3.10.0.** Their 3.8.x versions stay on NuGet and keep working with a
  3.8/3.9 host. They get no 3.10 version, and NuGet marks them deprecated in favour of `Elsa.Secrets`.
- **Secret values are not converted.** No tool reads the legacy encrypted values and writes them into Core. The
  two modules use different storage shapes and encryption, and converting ciphertext would require taking custody
  of your Data Protection key ring. You re-enter or rotate each secret into Core instead (below).
- **The databases do not upgrade in place.** Both modules create a table named `Secrets`. Core's initial migration
  fails against a database that already has the legacy table, and leaves it untouched. Give Core Secrets its own
  database or schema.
- **The API addresses secrets by name.** Legacy routes took a per-version row ID (`/secrets/{id}`); Core routes take
  the secret name (`/secrets/{name}`). There is no ID adapter, no mapping for the legacy `Owner` field, and no
  equivalent of the legacy plaintext `GET /secrets/{id}/input`.
- **Scripts use `getSecret(name)`.** The legacy `secrets.get<PascalName>Async()` accessors are gone. Core exposes
  `getSecret("my-secret")`, which returns a promise of the value. A workflow expression returns that promise and Elsa
  awaits it: `return getSecret("my-secret");`. Top-level `await` does not parse in a workflow expression.

## Before you start

1. Keep the legacy host, its database and its Data Protection key ring as they are. They are your rollback.
2. Inventory the legacy secrets without reading their values: name, scope, description, status and expiry, and
   the workflows and clients that use each one. The legacy list endpoint returns these fields; ignore the
   `encryptedValue` it also returns, which is of no use to Core.
3. For each secret, find where its value comes from: the provider console, the vault or the team that issued it.
   Re-entry is a good moment to rotate: issue a new credential at the provider rather than copying the old one.

## Moving a host

1. Upgrade the host to 3.10.0 and replace the legacy registration with Core's:

   ```csharp
   elsa.UseSecrets(secrets => secrets.UseEntityFrameworkCore(ef => ef.UseSqlServer(connectionString)));
   elsa.UseSecretsJavaScript();
   ```

   Point `connectionString` at a database (or schema) without the legacy `Secrets` table. Remove every reference to
   the five legacy packages; do not register the legacy API beside Core, because both claim `/secrets`.
2. Grant permissions. Core uses `secrets:view` (list, detail, descriptors, picker), `secrets:write` (create,
   update, rotate, revoke), `secrets:delete` and `secrets:test`. Map legacy grants as follows:

   | Legacy token | What it allowed | Closest Core grant |
   | --- | --- | --- |
   | `read:secrets` | list | `secrets:view` |
   | `secrets:read` | detail, and the plaintext value | `secrets:view` (Core never returns the value) |
   | `write:secrets` | create | `secrets:write`, which is broader (see the warning below) |
   | `secrets:write` | update, name checks | `secrets:write`, which is broader (see the warning below) |
   | `secrets:delete` | delete, bulk delete | `secrets:delete` |
   | none | | `secrets:test`, for callers that test secrets |

   > **Every legacy write grant widens in Core.** Core has one write permission, `secrets:write`, covering create,
   > update, rotate and revoke. It has no create-only or update-only grant. A role that held only `write:secrets`
   > (create) or only `secrets:write` (update) gains the rest once it holds Core's `secrets:write`. The legacy
   > `secrets:write` spelling is the same as Core's, so a role holding it gains that authority as soon as the host
   > runs Core, before you map anything. Review every role holding either legacy token, and give Core's
   > `secrets:write` only to roles that may create, update, rotate and revoke.

3. Re-create each secret in Core, in Studio's Secrets screen or with `POST /secrets`, using the name the
   workflows expect and the value from step 3 above. Core stores the secret for the ambient tenant; create shared
   secrets from an authorized platform context (see [tenant scoping](secrets-tenancy.md)).
4. Update workflows. Replace an expression such as `return secrets.getMySecretAsync();` with
   `return getSecret("my-secret");`, or pick the secret in a Secret-typed input. Inside an `async` function in a
   larger script, `await getSecret("my-secret")` works too
   ([scripting compatibility](../integration-program/secrets-scripting-compatibility.md)). Publish and run each changed workflow against the new host.
5. Update API clients to the name-based routes. Clients that cannot stop using row IDs or the plaintext route
   stay on the legacy host until they can.
6. When every workflow and client runs against Core, retire the legacy host. Keep its database and key ring until
   your retention policy allows deleting them.

## Rollback

Nothing in this procedure changes the legacy host, database or key ring, so rolling back means routing traffic
back to the legacy host. Secrets created or rotated in Core in the meantime do not flow back; rotate them at the
provider again if the legacy copy is now stale.

## Host aliases are not a tenant boundary

In the reference host, the tenant comes from the authenticated user (`CurrentUserTenantResolver`), not from the
`Host` header. A token issued for tenant B is accepted on tenant A's host alias, and it still reads only tenant B's
secrets. Treat host aliases as routing. If you need one tenant per host, add your own check that rejects requests
whose host-resolved tenant differs from the principal's tenant.
