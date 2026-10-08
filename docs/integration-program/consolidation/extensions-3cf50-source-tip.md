# Extensions source tip 3cf50295 (Dapper V3_7 migration)

After r8 preserved Extensions `c2618de7`, Extensions `main` merged elsa-extensions#252, reaching
`3cf502955791cbd20375022ef4597f0e195faf8a`. With that change, the Dapper `V3_7` runtime migration skips an
`AggregateFaultCount` column that already exists. #8535 carried it into the import:

- `6c8f59c3` applies the delta to the mapped paths:
  - `V3_7.cs` and the new `DapperRuntimeMigrationTests.cs` are byte-identical to upstream.
  - The test project file, which the import had rewritten (no BOM, mapped `ProjectReference`), gains exactly upstream's
    added `FluentMigrator.Runner.SQLite` line.
- `e72f4b2d` joins the upstream tip as a second parent without changing the tree.
- `13b94663` is the GitHub merge into the import branch. It keeps both parents.

The [tenth receipt](source-tip-refresh-2026-09-28-r10.json) pins the commits and blobs, and records each file's transform.
`verify_import_source_tip_refresh_r10.py` requires:
- byte identity for the `identical` rows;
- for the project file, exactly upstream's added and removed lines;
- the history parents;
- unchanged publisher workflows (deferring the current `HEAD` check to r7);
- solution build rows for the Dapper persistence tests.

The mapped Slack package proof requires it. `Elsa.Persistence.Dapper.UnitTests` passed 38/38 at the delta. The
downgrade-only V3_7 rollback note Greptile raised is filed upstream as elsa-extensions#254. No package was published.
