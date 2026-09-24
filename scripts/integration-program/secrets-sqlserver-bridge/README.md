# Synthetic SQL Server Secrets bridge

This bounded fixture reads synthetic Secrets rows written by the released Elsa Extensions 3.8.1 SQL Server provider and converts them into a fresh SQL Server database using pinned Elsa Core source. It is an isolated contract proof, not a production migration or customer cutover tool.

The fixture pins the released package versions and SHA-512 values, source commits, and SQL Server container digest in `artifacts.json`. The Core runner is built from the pinned source commit in a temporary checkout with the committed `current-core/packages.lock.json` restored in locked mode. The report records the complete `net10.0` dependency graph's lockfile hash and package count. A local package-manifest tooling package is included because that pinned Core source build requires it; its source commit and SHA-512 are recorded in the manifest and independently verified before restore.

The synthetic source has the released `20241011092820_V3_3` migration and five rows across default, tenant-a and tenant-b. It covers several versions, statuses, ownership metadata, timestamps and tenant-specific reuse of a normalized name. Its `ExpiresIn` column is SQL Server `time`, so fixtures use non-negative durations strictly below 24 hours, including the largest representable fixture tick (`23:59:59.9999999`). Durations of 24 hours or longer cannot be represented by this source column; the related compatibility concern remains tracked in [#6356](https://github.com/elsa-workflows/elsa-core/issues/6356).

For conversion, the runner validates source history/schema, expiry values, tenant mapping and the old Data Protection key/context before migrating the fresh destination. It unprotects with the released purpose and application name, then encrypts values using Core's `DefaultSecretValueProtector`. It writes a `ElsaSecretsLegacyV381` sidecar in the same transaction as Core secrets and maps each legacy row to a Core aggregate/version. The released package reopens and decrypts the original source afterward; source hashes must remain unchanged.

The fixture also exercises ID collision, rollback, unavailable/wrong keys and Data Protection context, invalid source history/schema/status/version/latest marker, plus pre-existing destination schema/history. The report includes package provenance, hashes, synthetic identifiers and boolean outcomes. The runner checks an allowlist of result fields and value shapes, requires the runner's redaction flags, and scans the serialized report for the synthetic plaintext, SQL password and connection strings before writing the CI artifact. Passwords, connection strings, keys, plaintext, ciphertext and provider exception text are excluded. Tenant verification compares the source to secrets reread from the target repository, checks tenant read visibility, and rejects a repository write with a novel cross-tenant name. HTTP authorization and arbitrary customer tenants remain outside this fixture's proof. `cutoverAllowed` is always false.

Run locally with the pinned .NET SDK and Docker available:

```sh
python scripts/integration-program/run_secrets_sqlserver_bridge.py \
  --report-out /tmp/secrets-sqlserver-bridge-report.json
```

All databases and key rings are disposable. The workflow runs this synthetic proof and retains its redacted report as a CI artifact. A local run against accepted Core commit `c37e9d7a2fa7e7c2af802b211e3d59db45fc2f6f`, including the merged SQL Server migration fix in #8358, completed; see `live-evidence.md`. It does not patch production migration code, publish packages, modify a production database, or grant cutover approval.
