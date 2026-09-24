# Published Secrets SQL Server upgrade fixture

This fixture characterizes the public `Elsa.Secrets.Persistence.EFCore` and
`.SqlServer` packages at Extensions 3.8.1 and Core 3.8.4. The exact NuGet
package versions, complete archive SHA-512 hashes, and nuspec repository
commits are pinned in [`artifacts.json`](artifacts.json). Each released graph
is restored into a separate locked .NET 10 runner and executed in a separate
process against one disposable SQL Server database. The container image is
pinned by digest and uses Developer edition. No customer data or credentials
are part of this fixture.

NuGet does not guarantee source order during restore, so Package Source
Mapping binds the two direct Elsa package IDs to a verified local feed. The
runner checks the downloaded `.nupkg`, cached archive, NuGet metadata source,
lockfile content hash, and `project.assets.json` source path. The NuGet
content hash and complete signed archive SHA-512 cover different byte ranges;
both are recorded. The .NET 10 runtime is pinned to 10.0.8 to keep implicit
ILLink tooling dependencies stable across SDK hosts.

Run from the repository root with Docker and the .NET 10 SDK:

```sh
python3 scripts/integration-program/run_secrets_sqlserver_upgrade_fixture.py \
  --report-out scripts/integration-program/secrets-sqlserver-upgrade/observed-result.json
```

The released Extensions runner applies its migration, inserts two versions
of a synthetic secret, and snapshots columns, indexes, migration history,
field values, and ciphertext hashes. The released Core runner then attempts
its ordinary migration against that database. Afterward, the Extensions
runner snapshots the same state and a fresh Extensions process reads both
versions. The report excludes the random SQL Server password, connection
string, and synthetic ciphertext values.

The checked-in [`observed-result.json`](observed-result.json) records the
exact pinned baseline. Extensions applies `20241011092820_V3_3`. Core sees
`20260531141743_Initial` as pending, then SQL Server reports error 2714:
`There is already an object named 'Secrets' in the database.` The complete
captured schema, indexes, migration history, and synthetic rows remain equal
before and after that failed migration. A fresh Extensions 3.8.1 process
still reads both versions. The legacy `ExpiresIn` column is a SQL Server
`time` value. Microsoft's [documented `time` range](https://learn.microsoft.com/en-us/sql/t-sql/data-types/time-transact-sql)
ends before 24 hours; this is a storage-boundary inference, not a live
greater-than-24-hour write test. The separate duration-overflow and
non-atomic-update report remains [#6356](https://github.com/elsa-workflows/elsa-core/issues/6356).

This is characterization of one released package pair, not an upgrade bridge
or production cutover. It does not establish decryption/key custody,
conversion of customer records, other release baselines, an executable
downgrade, or safety of arbitrary customer databases. “Unchanged after
failure” refers to this synthetic database and the observed failed Core
migration only. No package is published or production database changed.
