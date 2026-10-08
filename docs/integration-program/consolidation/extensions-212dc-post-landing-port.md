# Post-landing port of Extensions 3cf50295..212dc558

#8409 landed the import on `main` (merge `8b34ab1e`) with Extensions at `3cf50295`. By then Extensions `main` had
merged 20 more commits, reaching `212dc558c8ba3de4be50733c8475733391787893`:
- Dapper PostgreSQL support (elsa-extensions#255, #259): quoted identifiers, identifier and boolean-literal dialect
  hooks, a terminated upsert that includes the primary key, FluentMigrator 7.2 provider-name matching, a
  `DateTimeOffset` type handler and `Id`-cased instance search, with migration, dialect and snapshot tests;
- the legacy Secrets bulk-delete fix (elsa-extensions#253, #257);
- `ElsaVersion` and `ElsaStudioVersion` preview bumps (#262).

This port carries that delta and joins `212dc558` as a second parent, so the upstream commits stay in `main`'s history.

## How each file was mapped

- **Byte-identical to upstream:** every mapped source and test file, except the two below. That includes the three
  Dapper migrations, `MigrationDatabases.cs`, `PostgreSqlDbConnectionProvider`, `DateTimeOffsetHandler`,
  `DapperWorkflowInstanceStore` and `DefaultSecretManager`, plus the new tests.
- **Converged on upstream:** `PostgreSqlDialect.cs` and `ParameterizedQueryBuilderExtensions.cs`. The import carried the
  reviewed #8297 patch for these files, which fixed a primary key missing from the PostgreSQL upsert (SQLSTATE 23502) and
  boolean-versus-integer version filters (42883). Upstream now fixes both independently, with quoting and a
  boolean-literal hook. The three-way merge conflicted only where both sides fixed the same defect, so both files now
  match upstream byte for byte.
- **Reviewed transform kept:** `SqlDialectBase.cs` and `ISqlDialect.cs` are upstream plus the trailing newline that r5
  records.
- **Project files:** upstream's added lines are applied to the mapped form. That covers `InternalsVisibleTo` in the
  migrations project, and the FluentMigrator runner, Npgsql and Testcontainers references in the Dapper tests. The new
  `Elsa.Secrets.Management.UnitTests` references the mapped legacy project, and is added to `Elsa.sln` and
  `Elsa.Extensions.slnf`.
- **Central versions:** the three new FluentMigrator runners go into the scoped `src/extensions/Directory.Packages.props`,
  which `test/extensions` imports.
- **Not carried:** upstream's `ElsaVersion` and `ElsaStudioVersion` preview bumps. The monorepo builds Extensions by
  project reference and keeps its reviewed package-mode pins.

## Receipts after landing

Source-tip receipts r1–r10 and the retained-asset ledger used to compare their pinned bytes with the live `HEAD`. On
the import branch that kept every mapped file under a receipt. On `main`, the same check would make any ordinary edit to
roughly a hundred mapped files fail until someone wrote another receipt. This port hit exactly that.

From now on, the receipts check the bytes as they landed, at import head `b6baab6b` (`LANDED_IMPORT` in
`verify_import_source_tip_refresh_r2.py`):
- Ancestry checks still require the reviewed commits to be reachable from `HEAD`.
- The ledger accepts an active file that matches either the working tree or the landed commit.
- The current-publisher safety check still runs on `HEAD` through the seventh receipt's
  `validate_packages_workflow_gates.py` call.

Later changes to imported files, including ports like this one, are ordinary reviewed changes on `main`.

## Validation

- `Elsa.Persistence.Dapper.UnitTests`: 64 passed, including the live PostgreSQL Testcontainers migration and workflow test.
- `Elsa.Dapper.UnitTests`: 12 passed.
- `Elsa.Secrets.Management.UnitTests`: 1 passed.
- Verifiers r1–r10 pass, and the ledger stays at 163 of 163.
- CI runs the Dapper provider tests against PostgreSQL and SQL Server.
