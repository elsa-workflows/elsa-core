# Build, Run, And Operate

This page collects day-to-day build, run, and operational notes for Elsa Core contributors.

## Build Commands

Restore first when working from a clean checkout or after dependency changes. The `--ignore-failed-sources` option keeps external feed hiccups from blocking packages that are available from other configured sources:

```bash
./build.sh Restore --ignore-failed-sources
```

Default NUKE build target after restore:

```bash
./build.sh
```

NUKE test target after restore:

```bash
./build.sh Test
```

Direct solution build with the same restore/no-restore pattern:

```bash
dotnet restore Elsa.sln --ignore-failed-sources
dotnet build Elsa.sln --no-restore
```

Direct solution tests:

```bash
dotnet restore Elsa.sln --ignore-failed-sources
dotnet test Elsa.sln --no-restore
```

Targeted test project:

```bash
dotnet restore test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj --ignore-failed-sources
dotnet test test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj --no-restore
```

ElsaScript DSL tests:

```bash
./run-dsl-tests.sh
```

## Build System

The NUKE build lives in [build/Build.cs](../../build/Build.cs). It defines clean, restore, compile, test, and package behavior through NUKE components. Test projects are discovered as solution projects whose names end with `Tests`.

Source projects multi-target `net8.0`, `net9.0`, and `net10.0` through [src/Directory.Build.props](../../src/Directory.Build.props). Central package versions are in [Directory.Packages.props](../../Directory.Packages.props), including conditional version blocks for .NET 8/9 and .NET 10.

## Run The Reference Server

The main sample host is [src/apps/Elsa.Server.Web](../../src/apps/Elsa.Server.Web). It wires most major modules in [Program.cs](../../src/apps/Elsa.Server.Web/Program.cs).

Restore the project and run it with:

```bash
dotnet restore src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj --ignore-failed-sources
dotnet run --project src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj --no-restore
```

Notable toggles in `Program.cs`:

- `useReadOnlyMode`
- `useSignalR`
- `useStructuredLogs`
- `useMultitenancy`
- `disableVariableWrappers`

The sample configures identity, default authentication, workflow management/runtime with SQLite, workflow API, fluent storage, ElsaScript blob storage, scheduling, C#, JavaScript, Python, Liquid, HTTP, and optional tenants/structured logs.

## Docker Quick Try

The root [README](../../README.md) documents the public Docker quick start:

```bash
docker pull elsaworkflows/elsa-server-and-studio-v3:latest
docker run -t -i -e ASPNETCORE_ENVIRONMENT='Development' -e HTTP_PORTS=8080 -e HTTP__BASEURL=http://localhost:13000 -p 13000:8080 elsaworkflows/elsa-server-and-studio-v3:latest
```

Default development login:

```text
Username: admin
Password: password
```

Do not use default credentials in production.

## ASP.NET Middleware Order

The reference server pipeline is a useful ordering guide:

1. developer exception page in development
2. CORS
3. health checks
4. routing
5. authentication
6. authorization
7. tenants
8. workflow API
9. JSON serialization error handler
10. workflow HTTP endpoint middleware
11. controllers
12. Swagger UI in development
13. SignalR workflow hubs if enabled
14. structured logs hub if enabled

See [Program.cs](../../src/apps/Elsa.Server.Web/Program.cs).

## Operational Endpoints

With default route prefix `elsa/api`, runtime admin endpoints include:

- `GET /elsa/api/admin/workflow-runtime/status`
- `POST /elsa/api/admin/workflow-runtime/pause`
- `POST /elsa/api/admin/workflow-runtime/resume`
- `POST /elsa/api/admin/workflow-runtime/force-drain`

Structured log diagnostics endpoints include:

- `GET|POST /elsa/api/diagnostics/structured-logs/recent`
- `GET /elsa/api/diagnostics/structured-logs/sources`
- `GET /elsa/api/diagnostics/structured-logs/storage`

Health checks are mapped to `/` in the reference server.

## Runtime Knobs

Common runtime-related options in the reference host:

- `RuntimeOptions.InactivityThreshold`
- `BookmarkQueuePurgeOptions.Ttl`
- `CachingOptions.CacheDuration`
- `IncidentOptions.DefaultIncidentStrategy`
- recurring task schedules for trigger queue, bookmark queue purge, and interrupted workflow restart

Structured logs options include recent log capacity, query size, source heartbeat timeout, redaction settings, and storage provider options.

## Local Development Notes

- Prefer targeted builds/tests while iterating.
- Use `rg` to find feature registration and endpoint routes.
- Keep package version changes centralized in [Directory.Packages.props](../../Directory.Packages.props).
- Avoid provider-specific assumptions in core modules.
- When changing middleware, verify both code-first host setup and shell-feature setup if applicable.

## Release And Package Notes

Package behavior is controlled by the NUKE build and project metadata. Because source projects multi-target three frameworks, package upgrades should be checked against all target frameworks and provider packages. Persistence changes usually need extra scrutiny because each provider package may need migrations or compatibility updates.
