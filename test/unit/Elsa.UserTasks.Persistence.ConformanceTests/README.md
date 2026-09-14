# User Tasks persistence conformance suite

One suite, run unchanged against every implementation of the User Tasks persistence contracts. It exists
because the User Tasks build shipped four P1 defects that a single-threaded, happy-path, in-memory test
suite could not see — every one of them was found by injecting a failure against a real store.

## What it covers

| Contract | Suite | Implementations |
| --- | --- | --- |
| `IUserTaskRepository` | `UserTaskRepositoryConformanceTests` | InMemory, EF Core, VNext |
| `IUserTaskGuestSessionIssuer` | `UserTaskGuestSessionConformanceTests` | InMemory, EF Core |
| `IUserTaskInvitationOutbox` | `UserTaskInvitationOutboxConformanceTests` | InMemory, EF Core |
| The services above them | `UserTaskFaultInjectionConformanceTests` | InMemory, EF Core |

`UserTaskFaultInjectionConformanceTests` runs the real `DefaultUserTaskManager` and
`DefaultUserTaskInvitationService` against a real store and breaks the seams between them with the
decorators in `Faults/`. A cross-store operation must either commit fully or leave the caller able to
retry, and the retry must converge.

## Providers

Availability is resolved once per run by `ConformanceProviders`, and nothing is ever skipped quietly:

- **Always run**: in-memory, EF Core over SQLite, VNext over the SQLite document store.
- **Opt in with a connection string**: SQL Server, PostgreSQL, Oracle. Each test reports as *skipped, with
  the reason*, when the variable is unset — not as passed.
- **Not coverable**: MySQL. `Pomelo.EntityFrameworkCore.MySql` 9.0.0 caps
  `Microsoft.EntityFrameworkCore.Relational` at 9.0.x while this repository targets 10.0.9, so
  `Elsa.UserTasks.Persistence.EFCore.MySql` cannot be referenced from a test project at all (NU1107).

```bash
ELSA_USERTASKS_TEST_POSTGRES="Host=localhost;Database=elsa_conformance;Username=elsa;Password=elsa" dotnet test test/unit/Elsa.UserTasks.Persistence.ConformanceTests
```

The remaining variables are `ELSA_USERTASKS_TEST_SQLSERVER` and `ELSA_USERTASKS_TEST_ORACLE`.

Point them at a **disposable** database. The suite migrates the schema and isolates each test with its own
tenant, but it never drops the database — that is deliberate, so it can never delete something an operator
cared about.

`ConformanceCoverageTests` always runs. It fails if a provider that must run is unreachable, fails if a
variable is set but empty (configured on the CI job, gating nothing), and writes the full matrix to the
test output and to `user-task-conformance-coverage.md` in the output directory.

## Adding a provider

1. Add a `ConformanceProvider` entry to `ConformanceProviders.All`.
2. Add a fixture under `Providers/`.
3. Add a collection and one concrete class per contract in `ProviderConformanceSuites.cs`, each carrying
   `[ConformanceProvider(...)]`.

A conformance class without `[ConformanceProvider]` is skipped with a wiring error rather than counted as
coverage — the suite refuses to guess which provider a class exercised.
