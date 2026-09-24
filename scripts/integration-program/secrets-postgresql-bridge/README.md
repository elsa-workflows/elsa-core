# Synthetic PostgreSQL Secrets bridge

This fixture exercises a bounded conversion from the released Extensions 3.8.1
PostgreSQL schema into a fresh PostgreSQL database using the reviewed Core
source commit pinned in `artifacts.json`. It reuses the released package hashes
and source provenance recorded by [#8340](https://github.com/elsa-workflows/elsa-core/pull/8340),
and the field and encryption mapping verified for the SQLite bridge in
[#8282](https://github.com/elsa-workflows/elsa-core/issues/8282) and
[#8298](https://github.com/elsa-workflows/elsa-core/pull/8298).

The old package runner creates the source schema and deterministic synthetic
rows, encrypting their values with ASP.NET Core Data Protection and the
`Elsa.Secrets.Encryption` purpose. A separate Core source checkout applies its
PostgreSQL migrations to an empty destination. Core decrypts each old value,
rewrites it through its AES-GCM secret store, and persists native aggregate and
version rows with the exact old rows and unsupported metadata in a linked
sidecar, all in one transaction. The fixture checks tenant/default-tenant
behavior, same-name secrets across tenants, source hashes and a fresh old
package read after conversion. It also checks wrong or missing old and Core
keys, an incorrect Data Protection application context, migration/schema
collisions, and transaction rollback after an injected write failure.
Source schema/history, row validation, and old-key checks run before target
migrations. Their rejection reports verify that the original empty destination
is unchanged. The deliberate ID-collision and injected-rollback cases first
create target schema (and, for the collision, a seed row); those reports
separately verify `conversionWritesUnchanged` and mark
`originalDestinationUnchanged` false to make that setup explicit.

Run from the repository root:

```sh
python scripts/integration-program/run_secrets_postgresql_bridge.py \
  --report-out /tmp/secrets-postgresql-bridge-report.json
```

The command verifies the released `.nupkg` SHA-512 digests and nuspec source
commits, restores the old package graph in locked mode, builds the runner
against the pinned Core source, and starts disposable PostgreSQL databases in
the digest-pinned image. The Feedz-only build generator is served from its
locally retained, SHA-512-verified package artifact because its old feed URL is
no longer available. The report contains package and source hashes,
migration IDs, redacted row counts and hashes, and failure outcomes. It never
prints a connection string, plaintext, key, or ciphertext. The nonpublishing
integration-program CI job retains this report as an artifact.

This evidence applies to the one pinned synthetic baseline and mapping only.
It does not establish customer key-ring custody, security approval of a real
converter, arbitrary customer schema compatibility, production migration or
rollback, or restoration of legacy owner authorization and ID-addressed API
behavior. `cutoverAllowed` remains false, and parent issue #8275 stays open.
