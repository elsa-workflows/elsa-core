# Published Secrets SQLite upgrade fixture

This fixture characterizes the public `Elsa.Secrets.Persistence.EFCore` and `.Sqlite` package graphs at Extensions 3.8.1 and Core 3.8.4. It verifies the exact `.nupkg` SHA-512 and nuspec repository commit before restoring each graph into its own locked runner project and process. The two runner projects target .NET 10.0 and use the normal Elsa `UseElsaSqlite` / EF migration path.

Run it from the repository root:

```sh
python3 scripts/integration-program/run_secrets_sqlite_upgrade_fixture.py \
  --report-out scripts/integration-program/secrets-sqlite-upgrade/observed-result.json
```

The run creates a disposable SQLite file, migrates it with Extensions 3.8.1, inserts two deterministic synthetic versions of one logical secret, closes that process, and launches Core 3.8.4 against the same file. It prints the provenance and lockfile hashes, migration IDs, table columns, integrity result, synthetic-row hashes, and upgrade outcome as JSON. No real secret values are used.

The checked-in [`observed-result.json`](observed-result.json) records one successful reproduction with official NuGet artifacts. Extensions 3.8.1 applies `20240915164114_V3_3`. Core 3.8.4 sees `20260531141623_Initial` as pending and fails with `SQLite Error 1: 'table "Secrets" already exists'`. The fixture then verifies that SQLite integrity, table columns, migration history, all synthetic fields and ciphertext sentinels are unchanged, and that a separate Extensions 3.8.1 process can still read both versions. This is a reproduced package compatibility boundary; it does not establish decryptability, transform ciphertext, or change schema migration code.

Because Core cannot upgrade the fixture, the next safe step is a separately reviewed, provider-specific migration task. Do not infer an old-to-new ciphertext conversion or claim compatibility for other providers, releases, or live databases from this result.
