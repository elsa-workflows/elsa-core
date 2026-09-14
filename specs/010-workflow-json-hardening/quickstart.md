# Quickstart: Workflow JSON Type Hardening

1. Register built-in workflow JSON aliases through workflow features.
2. Register module payload aliases through each module that serializes workflow payloads.
3. Verify `TypeJsonConverter` and `PolymorphicObjectConverter` use the shared serialization registry, not expression options.
4. Run targeted tests:

```sh
dotnet test --project test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj -- --treenode-filter "/*/*/SerializationTypeResolverTests/*"
dotnet test --project test/unit/Elsa.Workflows.Runtime.UnitTests/Elsa.Workflows.Runtime.UnitTests.csproj -- --treenode-filter "/*/*/WorkflowRuntimeFeatureTests/*"
```
