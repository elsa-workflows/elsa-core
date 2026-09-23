# [Task] Reproduce the published Secrets SQLite upgrade from 3.8.1 to 3.8.4

Parent story: `doc/integration-program/issue-drafts/8214-secrets-collision-compatibility.md` (F2.2)
Program: #8194

## Outcome

Establish what the public Extensions 3.8.1 SQLite migration path does when the same disposable database is opened by the public Core 3.8.4 package graph. This is a characterization task: reproduce and preserve the observed result before designing a schema bridge. It does not add or change a migration.

## Scope

- Use official NuGet artifacts `Elsa.Secrets.Persistence.EFCore.Sqlite` and `Elsa.Secrets.Persistence.EFCore`, both at 3.8.1 for the old phase and 3.8.4 for the Core phase. Keep each package graph in a separate process/project so NuGet cannot silently select only one version of the duplicate package IDs.
- Verify the 3.8.1 nuspec repository commit (`01bf9ad70d0399afadae9609fe820703d3de290d`) and the 3.8.4 nuspec repository commit (`33181ae3048f628f591a0155b5665a8e4d1bcea2`). Record official package URLs, downloaded `.nupkg` SHA-512 values and each phase's resolved dependency lockfile.
- Use a temporary SQLite database and synthetic rows only. Seed legacy fields with deterministic, non-sensitive values, including an `EncryptedValue` sentinel; this task does not establish that the sentinel is decryptable or convert it.
- Run the normal migration/startup path of each exact package graph against the same database file, closing and reopening it between phases. Record whether Core 3.8.4 succeeds or fails, the migration history and resulting table shape. If it fails, prove the old artifact can still reopen the fixture and read the seeded rows afterward.

## Acceptance criteria

- [ ] A repeatable automated fixture applies the official 3.8.1 SQLite migrations, seeds at least two rows representing multiple versions of one logical secret, and closes the old process cleanly.
- [ ] A separate runner using only the official Core 3.8.4 package graph attempts the normal migration path on that exact database; its success or failure is asserted and recorded without replacing the package source or hand-editing the database.
- [ ] The fixture records package URLs, hashes, nuspec source commits, target framework and resolved dependency lockfiles so another maintainer can reproduce the exact artifact pair.
- [ ] After the Core attempt, direct SQLite inspection confirms the database integrity, migration history, table columns and synthetic row/sentinel state. If the Core attempt fails, the 3.8.1 runner can still reopen and read the original fixture.
- [ ] Test output contains no real secret values. No cipher/key transformation or migration-code change is part of this task; package publication, release cutover and live databases are out of scope.
- [ ] The result states the next safe action: if Core cannot upgrade and read the fixture, open a separately reviewed implementation task for a provider-specific migration; if it can, add explicit semantic and cryptographic compatibility tests before claiming data preservation.

## Steps

1. Add an isolated integration fixture that restores the exact 3.8.1 and 3.8.4 package graphs into separate runners and saves their lockfiles.
2. Initialize a fresh temporary SQLite file with the 3.8.1 package's normal migration path, then seed a logical secret with multiple version rows, tenant/owner/status/expiry metadata, and deterministic encrypted-value sentinels.
3. Close the first runner and snapshot package/hash metadata, migration history, table definitions and seeded rows.
4. Launch the isolated Core 3.8.4 runner against the same file and invoke its normal migration path. Capture the exact outcome, then inspect the resulting database from a fresh connection.
5. Reopen with the 3.8.1 runner if Core failed; confirm the original synthetic rows remain queryable. Document any partial migration or data change as a failure requiring a separate design decision.
6. Run the focused fixture repeatedly from clean temporary directories and attach the compact provenance/result report to the task.

## Non-goals

Do not implement a V3_3-to-Core schema transform here, infer old-to-new ciphertext semantics, claim all providers or older releases are covered, or modify the active package ownership/cutover plan. PostgreSQL, SQL Server, rollback policy and other supported baseline versions remain future tasks.
