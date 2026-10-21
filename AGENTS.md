# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project Overview

- Elsa Core is a .NET solution centered on `Elsa.sln`.
- Products live under `core/`, `extensions/`, `studio/`, `apps/`, and `templates/`. Apps contains official released-package application hosts and Python container-policy tests under `apps/scripts/tests/`; its contributor entrypoint is [Apps](apps/README.md).
- The root solution, solution filters, build automation, scripts and central policy are shared. Templates keeps its independent product solution; root `Elsa.sln` includes navigation items only for that product.
- Read the product documentation entrypoints: [Core](core/docs/README.md), [Extensions](extensions/docs/README.md), [Studio](studio/docs/README.md), [Apps](apps/docs/README.md), and [Templates](templates/docs/README.md).
- Build automation is implemented with NUKE in `build/Build.cs` and exposed through `build.sh`, `build.ps1`, and `build.cmd`.

## Working Rules

- Keep changes focused on the requested task. Do not mix unrelated cleanup, formatting, or dependency updates into functional changes.
- Preserve existing public APIs unless the task explicitly requires an API change.
- Update tests when behavior changes and add tests for new behavior where practical.
- Update documentation when changing externally visible behavior, configuration, APIs, or developer workflows.
- Do not delete generated-looking, artifact, or IDE files unless the task explicitly asks for cleanup.
- If the worktree contains unrelated user changes, leave them untouched.
- Before opening a PR, read `.github/reviewers.md`: Greptile is required (re-request with `@greptileai` after each push), and request at most one additional advisory reviewer from the list once the PR is open.

## Agent Operating Principles

- Do not assume, hide confusion, or flatten uncertainty; surface questions, constraints, and tradeoffs explicitly.
- Write the minimum code that solves the defined problem; do not add speculative abstractions, features, or cleanup.
- Touch only the files and behavior required for the task; clean up only issues introduced by your own changes.
- Define success criteria before implementation, then iterate until the criteria are verified or clearly state what could not be verified.

## Build And Test Commands

- Build the default target: `./build.sh`
- Run NUKE test target: `./build.sh Test`
- Build with dotnet directly: `dotnet build Elsa.sln`
- Focus a product build with `dotnet build Elsa.Core.slnf`, `dotnet build Elsa.Extensions.slnf`, `dotnet build Elsa.Studio.slnf`, or `dotnet build Elsa.Apps.slnf`; see product docs for selection boundaries.
- Build Templates separately: `dotnet build Elsa.Templates.slnf` or `dotnet build templates/Elsa.Templates.slnx`; test with `dotnet test templates/test/Elsa.Templates.Tests/Elsa.Templates.Tests.csproj`. Read [Templates guidance](templates/AGENTS.md) before changing that product.
- Run shared-solution tests directly: `dotnet test Elsa.sln`
- Run a specific test project: `dotnet test core/test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj`
- Run ElsaScript DSL integration tests: `./run-dsl-tests.sh`

Prefer targeted `dotnet test <project>` commands while iterating, then run a broader build or test command when the change affects shared infrastructure or cross-module behavior.

## Code Style

- C# uses `LangVersion` `latest`, nullable reference types, and implicit usings from `Directory.Build.props`.
- Product source libraries target `net8.0`, `net9.0`, and `net10.0` through their product `src/Directory.Build.props`; hosts and tests may select a specific framework.
- Follow `.editorconfig`: 4-space indentation for C#, file-scoped namespaces, braces required, `var` preferred, usings outside namespaces, and sorted system directives.
- Prefer clear domain names over abbreviations. Keep types small and responsibilities explicit.
- Avoid suppressing warnings unless compatibility or framework constraints make the warning unavoidable; explain the reason near the suppression.

## Repository Structure

- `core/src/apps/`, `core/src/clients/`, `core/src/common/`, `core/src/modules/`: Core hosts, clients, shared infrastructure and modules.
- `core/test/`: Core unit, integration, component and performance tests.
- `extensions/src/`, `extensions/test/`, `extensions/docs/`, `extensions/samples/`: Extensions-owned code, tests, documentation and samples.
- `studio/react/`: React Studio Preview frontend and host; read its contributor guidance.
- `studio/src/`, `studio/test/`, `studio/docs/`, `studio/samples/`: Studio-owned code, tests, documentation and samples. Read [Studio contributor guidance](studio/src/AGENTS.md) for Studio modules, hosts, frameworks and tests.
- `apps/src/`, `apps/scripts/`, `apps/docs/`: official net10 application/image hosts, container policy tests and source-import guidance. Read [Apps agent guidance](apps/AGENTS.md). Apps consumes released packages and does not add Core NuGet release units.
- `templates/src/`, `templates/test/`, `templates/docs/`: independent Elsa 3 .NET project-template pack, smoke tests and contributor guidance. Embedded starter project files are data; keep all Templates projects outside root `Elsa.sln` packing.
- `core/docs/specs/` and `studio/docs/specs/`: product feature plans and specifications.
- `docs/adr/`: shared architecture decisions; `docs/integration-program/`: retained consolidation and proof records.
- `build/`, `scripts/`, `docker/`, root solution/filter files and central `Directory.*` files: shared tooling and policy.
- `design/`: shared design assets; `core/docs/design/`: Core persistence design notes.

## Architecture Decision Records

- ADRs live in `docs/adr/`. New records are named `YYYY-MM-DD-slug.md`, and the `# ` heading carries the
  title alone with no numeric prefix. Do not continue the legacy `NNNN-` sequence: it collided on every
  concurrent branch, which is what the date prefix exists to stop.
- Records `0001`-`0027` keep their existing names and headings. Do not renumber or rename them.
- `docs/adr/toc.md` is generated. Never edit it by hand: run `scripts/adr/generate-toc.sh` and commit the
  result. `scripts/adr/generate-toc.sh --check` verifies it is current, and CI runs the same check.
- The rationale is recorded in
  [Identify new ADRs by date instead of a sequential number](docs/adr/2026-08-25-date-prefixed-adr-identifiers.md).

## Testing Guidance

- Place tests near the relevant existing test project rather than creating a new project by default.
- Use the existing shared testing helpers under `core/src/common/Elsa.Testing.Shared*` when they fit the scenario.
- For regressions, add a failing test that demonstrates the bug before or alongside the fix.
- For multi-targeting issues, consider whether the test must run against all target frameworks or only the affected one.

## Dependency And Package Guidance

- Central package versions are managed in the root and product `Directory.Packages.props` files; do not add ad hoc versions to individual project files unless the repository already uses that pattern for the package.
- Keep target framework changes centralized in the relevant product `src/Directory.Build.props` unless a project has a specific reason to differ.
- Be cautious with package upgrades because this repository targets multiple frameworks and modules.

## Review Checklist

Before handing off changes, verify the following when applicable:

- The project or solution builds.
- Relevant unit or integration tests pass.
- Public API or behavior changes are documented.
- New code follows nullable annotations and existing style.
- No unrelated files were changed.
- The change follows `.github/quality/PRINCIPLES.md`, which is the bar for public API, simplicity, domain language, tests, CI and docs. In particular, public API changes in 3.x deprecate rather than break (principle 6). `.github/quality/README.md` explains the audit and fix loop and its labels.
