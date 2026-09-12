# Coding Conventions

## Naming Rules

| Item | Rule | Example | Evidence |
|---|---|---|---|
| Files | PascalCase for C# types | `ExternalAuthenticationBroker.cs` | `src/modules/Elsa.ExternalAuthentication/Services/` |
| Methods | PascalCase; async methods end in `Async` | `CompleteCallbackAsync` | `ExternalAuthenticationBroker.cs` |
| Interfaces | `I` prefix | `IExternalIdentityProvisioner` | `ExternalAuthenticationContracts.cs` |
| Locals/parameters | camelCase | `signedInAt` | `EFCoreExternalIdentityProvisioner.cs` |

## Formatting and Linting

- Formatter/rules: Roslyn `.editorconfig` settings.
- Relevant rules: 4-space indentation, system usings first, braces required, file-scoped namespaces, `var` preferred.
- Run commands: `dotnet format Elsa.sln` when formatting is explicitly required; normal validation uses `dotnet build` and `dotnet test`.

## Import and Module Conventions

- Usings are outside namespaces and system directives sort first.
- Project namespaces follow the feature folder layout.
- Public contracts are kept in contract/model namespaces rather than persistence projects.

## Error and Logging Conventions

- Services throw internal exceptions; broker endpoints translate failures to bounded public categories.
- Logging uses `Microsoft.Extensions.Logging` and structured message templates.
- External authentication response DTOs omit subject hashes and sensitive tokens; redaction is covered by dedicated tests.

## Testing Conventions

- Tests live under `test/{unit,integration,component,performance}` and use xUnit.
- Prefer real in-memory implementations; NSubstitute is used at external or expensive boundaries.
- Integration test coverage threshold defaults to 10% per project.

## Evidence

- `.editorconfig`
- `Directory.Build.props`
- `test/Directory.Build.props`
- `test/integration/Elsa.ExternalAuthentication.IntegrationTests/`

