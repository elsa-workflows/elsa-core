# Legacy Secrets scripting compatibility

## Result

**Unsupported without an expression migration.** The pinned Extensions 3.8.4 Secrets Scripting feature adds a `secrets` object with per-secret methods named `get{PascalizedName}Async()`. Core's Secrets JavaScript feature exposes a global `getSecret(name)` function instead. For a secret named `apiKey`, `secrets.getApiKeyAsync()` does not execute unchanged under the Core feature, while `getSecret('apiKey')` does.

This result covers only this published scripting surface and a synthetic resolver. It does not claim compatibility for every Extensions release, persisted workflow, or host composition. No database, real credential, encryption key, or migration was used. No public runtime behavior was changed.

## Source pins

- Legacy package: `Elsa.Secrets.Scripting` 3.8.4, whose inspected nuspec identifies Extensions commit [`154ba15fb4da85b4bebecfbe43639579cbda1d0d`](https://github.com/elsa-workflows/elsa-extensions/commit/154ba15fb4da85b4bebecfbe43639579cbda1d0d). At that pin, [`SecretsScriptingFeature`](https://github.com/elsa-workflows/elsa-extensions/blob/154ba15fb4da85b4bebecfbe43639579cbda1d0d/src/modules/secrets/Elsa.Secrets.Scripting/Features/SecretsScriptingFeature.cs) registers the type provider; [`ConfigureEngineWithSecrets`](https://github.com/elsa-workflows/elsa-extensions/blob/154ba15fb4da85b4bebecfbe43639579cbda1d0d/src/modules/secrets/Elsa.Secrets.Scripting/JavaScript/ConfigureEngineWithSecrets.cs) creates `secrets` and installs one `get{PascalizedName}Async()` closure for each listed secret; [`SecretsTypeDefinitionProvider`](https://github.com/elsa-workflows/elsa-extensions/blob/154ba15fb4da85b4bebecfbe43639579cbda1d0d/src/modules/secrets/Elsa.Secrets.Scripting/JavaScript/SecretsTypeDefinitionProvider.cs) advertises the same methods.
- Core candidate: Elsa Core commit `e5fe7562935046dacd2be69295aa49dfee414c14`.
- Core implementation: `src/modules/Elsa.Secrets.JavaScript/Scripting/JavaScript/SecretsJavaScriptHandler.cs` registers the global `getSecret(name)` function and documents that signature in generated type definitions.
- Executable comparison: `test/integration/Elsa.JavaScript.IntegrationTests/SecretsJavaScriptTests.cs`, `PinnedLegacyNamedAccessorExpression_IsUnsupportedByCoreSecretsFeature`. The test evaluates the old expression and verifies it fails, then evaluates the Core expression through the same synthetic resolver.

The legacy accessor is generated from the secret name at runtime. The test uses the synthetic name `apiKey` to identify the old accessor spelling `getApiKeyAsync`; it does not seed or inspect a persisted legacy workflow. Therefore this test proves a scripting API mismatch, not the set of customer workflows affected or the migration required for any particular workflow.
