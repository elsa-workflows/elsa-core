# Published Secrets PostgreSQL upgrade fixture

This fixture characterizes the public `Elsa.Secrets.Persistence.EFCore` and
`.PostgreSql` package graphs at Extensions 3.8.1 and Core 3.8.4. It verifies
each `.nupkg` SHA-512 and nuspec repository commit, then restores each package
graph into its own locked runner project and process. The PostgreSQL container
image is pinned by digest. The runners use the released `UseElsaPostgreSql` and
EF `Database.MigrateAsync` path.
Both runners pin the .NET 10 runtime framework to `10.0.8` so SDK machines
with a newer runtime patch do not silently change the lockfile's implicit
ILLink tooling dependency.

NuGet [does not honor configured source order during restore](https://learn.microsoft.com/en-us/nuget/consume-packages/package-restore#package-restore-behavior), so the
restore config uses [Package Source Mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping): the two direct Elsa package
IDs map only to the verified local feed, and `*` maps the other dependencies
to nuget.org. After restore, the fixture checks the cache's `.nupkg.metadata`
source, hashes the cached `.nupkg` against the pinned SHA-512, checks that
`project.assets.json` resolves each direct package through the isolated NuGet
cache, and matches the lockfile `contentHash` to NuGet's cached content hash.
These are two distinct hash checks: NuGet's package content hash excludes
package signatures ([NuGet package metadata](https://github.com/NuGet/Home/wiki/Nupkg-Metadata-File)), while the fixture's SHA-512 covers the complete
`.nupkg` archive. The report records the lockfile SHA-256, both package hashes,
the NuGet source, and the relative cache path.

Run from the repository root:

```sh
python3 scripts/integration-program/run_secrets_postgresql_upgrade_fixture.py \
  --report-out scripts/integration-program/secrets-postgresql-upgrade/observed-result.json
```

The run creates a disposable PostgreSQL database, migrates it with Extensions
3.8.1, inserts two deterministic synthetic versions of one logical secret,
closes that process, and runs Core 3.8.4 against the same database. The runner
records the schema columns and indexes, migration history, synthetic field
values and ciphertext hashes before and after the Core migration attempt. It
also starts a fresh Extensions 3.8.1 process to verify those rows remain
readable. No real secret values are used. Reports keep the database connection
string out and record hashes instead of the synthetic ciphertext values.

The published artifacts have these repository provenances:

| Package | Version | Repository commit | SHA-512 |
| --- | --- | --- | --- |
| `Elsa.Secrets.Persistence.EFCore` | 3.8.1 | `elsa-extensions@01bf9ad70d0399afadae9609fe820703d3de290d` | `00ffb499bb1164fb740c28bf669aaf26cf37e891dded5a5e88a33d41a48a6c9c8b1118554770fb12657c91e4307cc53cf72637c3e2c0dccd878610e6b11dd803` |
| `Elsa.Secrets.Persistence.EFCore.PostgreSql` | 3.8.1 | `elsa-extensions@01bf9ad70d0399afadae9609fe820703d3de290d` | `2f5422f6e275d4e74dba0bb8fb575a9e69910b731d7d271e4215733b8d2dded4069436795f2ae70bfccdad1f54846edf0e79b1673fb0400a19302632b1034285` |
| `Elsa.Secrets.Persistence.EFCore` | 3.8.4 | `elsa-core@33181ae3048f628f591a0155b5665a8e4d1bcea2` | `1c6855fcd9785db2f7c90f17bc75817c78db9a5bf630d5eb0ddfaa0c383b7ad1a3d64866e392dbabcfb762267a40e21fa564b112dfd9f4c9502c3c3bed92aeb8` |
| `Elsa.Secrets.Persistence.EFCore.PostgreSql` | 3.8.4 | `elsa-core@33181ae3048f628f591a0155b5665a8e4d1bcea2` | `c91718e729a11f2671a3c8c3c56e646dcfa0841218a9cb4076214163d75137f40efcc1064a10e3345a231d4541d040b92438e1986555b2703ac1dc3f505ca057` |

The Extensions 3.8.1 PostgreSQL migration
[`20241011082142_V3_3`](https://github.com/elsa-workflows/elsa-extensions/blob/01bf9ad70d0399afadae9609fe820703d3de290d/src/modules/secrets/Elsa.Secrets.Persistence.EFCore.PostgreSql/Migrations/20241011082142_V3_3.cs)
creates the row-per-version `Secrets` table in Elsa's `Elsa` schema. Core
3.8.4's first PostgreSQL Secrets migration
[`20260531141856_Initial`](https://github.com/elsa-workflows/elsa-core/blob/33181ae3048f628f591a0155b5665a8e4d1bcea2/src/modules/Elsa.Secrets.Persistence.EFCore.PostgreSql/Migrations/Secrets/20260531141856_Initial.cs)
also creates a table named `Secrets` in that schema, with a different
aggregate/JSON shape. The ordinary Core migration sees the Extensions history
entry as unknown and attempts this initial create.

The checked-in [`observed-result.json`](observed-result.json) records a run
against the hash-pinned artifacts and PostgreSQL image. For each direct
package, it ties the lockfile content hash to the cache metadata, proves that
the NuGet cache archive has the pinned full-artifact hash, and records that
NuGet restored it from the mapped local feed. Extensions applies
`20241011082142_V3_3`. Core 3.8.4 reports
`20260531141856_Initial` as pending and fails with
`Npgsql.PostgresException`, SQLSTATE `42P07` (`relation "Secrets" already
exists`). The captured schema, indexes, migration history, and every synthetic
field and ciphertext hash are unchanged after the failed migration; a new
Extensions process reads both rows.

Here, “rollback” describes the database transaction rollback from the failed
Core migration. The fixture does not invoke a downgrade migration. It
characterizes one exact published baseline; it does not prove upgrades from
other releases, arbitrary customer databases, decryption/key custody,
cryptographic conversion, or a production rollback procedure. It makes no
production migration changes and performs no package publication.
