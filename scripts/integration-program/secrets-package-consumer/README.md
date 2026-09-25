# Secrets public-package consumer compatibility proof

This compile-only fixture uses the public NuGet packages `Elsa.Secrets.Persistence.EFCore` and `Elsa.Secrets.Persistence.EFCore.Sqlite`. The 3.8.1 artifacts are the Extensions implementation; the 3.8.4 artifacts are the Core implementation, as recorded in [the package lineage ledger](../../../doc/integration-program/secrets-collision-compatibility.md).

The fixture restores each package graph from NuGet.org using checked-in lock files and compiles three consumer cases on net8.0, net9.0 and net10.0:

- The old `SecretsDbContext` consumer compiles against the Extensions 3.8.1 baseline.
- The canonical `SecretsElsaDbContext` consumer compiles against Core 3.8.4.
- The same old consumer source fails against Core 3.8.4 with `CS0246` for `SecretsDbContext`.

Run `scripts/integration-program/secrets-package-consumer/run-compatibility-proof.sh` from the repository root. The script treats only that expected missing-type diagnostic as the compatibility result; restore failures, unexpected build failures and unexpected success fail the proof.

The runner first checks four diagnostic-classification cases, including a mixed expected and code-less MSBuild error; it rejects that mixed failure. This demonstrates a source-level API break for one EF Core persistence consumer surface and a successful public-package compile for the Core replacement across all three target frameworks. It does not establish an adapter or supported upgrade path, test the current Core source candidate, cover the other persistence providers or legacy APIs, exercise runtime behavior, or prove migration, rollback, tenant authorization, encryption or key custody. It uses no customer data or credentials and does not pack or publish packages.

Validated on 2026-09-25 with .NET SDK 10.0.300: both exact-locked package restores succeeded; baseline and Core consumer builds each passed for all three target frameworks with zero warnings and errors; the legacy consumer produced the expected `CS0246` for `SecretsDbContext` against Core 3.8.4 on all three frameworks. The runner exited successfully after checking those outcomes.
