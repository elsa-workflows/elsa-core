# Secrets SQLite bridge contract

This is a **synthetic-only review fixture**, not a production database migrator. It combines a Python field projection, a historical package crypto check, and a current-Core SQLite conversion proof. The redacted report is synthetic database-conversion evidence only; it does not prove host tenant authorization, route/API compatibility, or production cutover readiness.

## Pinned artifacts and source

`artifacts.json` pins official NuGet package IDs, versions, package SHA-512 digests, and nuspec source commits. It also pins the Feedz-only package-manifest generator required to compile the pinned Core source; the script verifies its digest and serves it from a temporary local feed. `packages.lock.json` pins both historical package graphs. The corresponding source links and field-by-field mapping are in [`mapping.json`](mapping.json). Historical package pins match the artifact evidence recorded by [#8276](https://github.com/elsa-workflows/elsa-core/issues/8276).

The fixture's [`global.json`](global.json) selects .NET SDK `10.0.300` with roll-forward disabled. Every restore and runner invocation uses the fixture directory as its working directory, including the isolated Core checkout. This keeps SDK-supplied `Microsoft.NET.ILLink.Tasks` versions consistent with the committed lockfiles across operating systems and hosted runners. The workflow installs the same SDK, and the runner verifies and records the selected version before restoring in locked mode.

The old SQLite `Secrets` migration has 16 columns: per-version `Id`, shared logical `SecretId`, name/scope/value/description, version/latest/status, duration and absolute expiration, last access, tenant, timestamps, and owner. The Python projection checks candidate field placement and rejects unknown schema/history, unsupported statuses, tenant/owner semantics outside the historical target, duplicate versions, ambiguous latest markers, and normalized-name collisions.

## Historical encryption proof

The Extensions runner uses `DataProtectionEncryptor` with purpose `Elsa.Secrets.Encryption`, a temporary synthetic Data Protection key ring, and a fixture-only application name. A separate Core 3.8.4 process decrypts two synthetic versions, writes them through `EncryptedSecretStore` and `DefaultSecretValueProtector` with a separate synthetic Core key, then verifies hash equality and wrong/missing-key rejection. It prints hashes and booleans only.

## Current-Core SQLite proof

The old package runner materializes a disposable 3.8.1 SQLite source by executing migration SQL generated from the pinned published migration assembly. Its separate ordinary `MigrateAsync` diagnostic records the observed `PendingModelChangesWarning`; it does not suppress the warning or present generated SQL as proof that the historical package's normal migration path succeeds. It then seeds synthetic rows and ciphertext only.

The current runner creates a disposable local clone at pinned commit [`7b06b82d0ea89c12d49c3c28da8d770bfca13faf`](https://github.com/elsa-workflows/elsa-core/tree/7b06b82d0ea89c12d49c3c28da8d770bfca13faf), verifies its `src` tree and tracked build inputs (`Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json`, and both NuGet config spellings) against that commit, and rejects untracked source or build inputs. It copies only the current bridge runner sources into the same relative path in that isolated checkout, so later Core source changes on the tools branch do not silently change this proof's target. CI fetches full Git history so the pinned commit can be checked. This pins project inputs, not the SDK, operating system, environment variables, external build tools, or the network service; those remain trusted runner inputs, so the proof is not a hermetic build. The runner opens the source database read-only and applies the pinned commit's exact SQLite migration set to a fresh destination. It rejects any existing target file or symlink before database creation and uses exclusive creation so a concurrent target cannot be overwritten. The target includes nullable lifecycle ownership markers; the fixture leaves both unset and does not reinterpret legacy `Owner` as lifecycle ownership. In one transaction it maps four logical secrets and five versions into native tenant/default-tenant rows, encrypts values through Core's `EncryptedSecretStore`, and writes every source row and unsupported field to a namespaced compatibility sidecar. It reloads persisted Core entities through the EF repository before decrypting and comparing hashes; checks tenant visibility and a conflicting cross-tenant write; and injects aggregate-ID collision and post-save failures to verify rollback. Unknown source history/schema, unmapped tenants, unknown statuses, invalid latest markers, and normalized-name collisions fail closed. A source hash comparison and historical reopen check verify that the original source remains readable and unchanged.

The sidecar retains owners, old row IDs, old ciphertext, timestamps, status values, expiry duration, and last-access time. It does not restore old owner authorization or ID-addressed API behavior. The report flags the required owner-authorization and legacy-ID adapters and sets `cutoverAllowed` to false. Host-level authorization, API, route, and production migration remain unverified.

## Run

From the repository root:

```sh
python3 scripts/integration-program/run_secrets_sqlite_bridge_contract.py \
  --report-out /tmp/secrets-sqlite-bridge-result.json
```

Use `--update-lockfiles` only when intentionally regenerating the pinned package graphs. The integration-program workflow runs the Python suite in normal and optimized modes and runs the complete synthetic SQLite proof on pull requests. The report contains package hashes, migration IDs, plaintext hashes, and result flags; it prints no plaintext, key, or ciphertext.

## Recommendation

**No-go for direct or in-place upgrade and cutover.** This fixture demonstrates only a reversible synthetic conversion into a fresh current-Core SQLite database when the exact old keys and an explicit tenant map are supplied. Unknown source history/schema, invalid statuses/timestamps, duplicate versions, ambiguous latest markers, normalized-name or tenant collisions, unavailable keys, and transactional write failures must fail closed. No live database, real credentials, production DDL, compatibility API, or production converter is included. See [`futureBridgeDesign.md`](futureBridgeDesign.md) for the full compatibility gates.
