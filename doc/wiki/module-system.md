# Module System

The module system is the backbone of Elsa. It is a thin abstraction over `IServiceCollection` that lets packages register cohesive feature sets with dependency ordering.

## Core Types

| Type | File | Role |
| --- | --- | --- |
| `IModule` | [src/common/Elsa.Features/Services/IModule.cs](../../src/common/Elsa.Features/Services/IModule.cs) | Holds `IServiceCollection`, module properties, configured features, hosted service descriptors, and `Apply()`. |
| `Module` | [src/common/Elsa.Features/Implementations/Module.cs](../../src/common/Elsa.Features/Implementations/Module.cs) | Concrete feature graph builder and applier. |
| `IFeature` | [src/common/Elsa.Features/Services/IFeature.cs](../../src/common/Elsa.Features/Services/IFeature.cs) | Feature lifecycle contract. |
| `FeatureBase` | [src/common/Elsa.Features/Abstractions/FeatureBase.cs](../../src/common/Elsa.Features/Abstractions/FeatureBase.cs) | Base class for most code-first features. |
| `DependsOnAttribute` | [src/common/Elsa.Features/Attributes/DependsOn.cs](../../src/common/Elsa.Features/Attributes/DependsOn.cs) | Declares feature dependencies. |
| `DependencyOfAttribute` | [src/common/Elsa.Features/Attributes/DependencyOf.cs](../../src/common/Elsa.Features/Attributes/DependencyOf.cs) | Declares optional dependency relationships. |

## Lifecycle

Feature classes usually use three lifecycle methods:

1. `Configure()`: declare additional feature configuration, scan activities, or add endpoint assemblies.
2. `ConfigureHostedServices()`: register hosted services with optional priority.
3. `Apply()`: add concrete services, options, stores, handlers, endpoints, and providers to DI.

`Module.Apply()` topologically sorts configured features and dependencies, configures them once, filters features with missing optional dependencies, registers hosted services, applies services, and finally registers installed-feature metadata.

## Entry Points

The common public path is:

```csharp
services.AddElsa(elsa =>
{
    elsa
        .UseWorkflowManagement()
        .UseWorkflowRuntime()
        .UseWorkflowsApi();
});
```

Implementation links:

- [AddElsa and ConfigureElsa](../../src/modules/Elsa/Extensions/DependencyInjectionExtensions.cs)
- [CreateModule and Use<T>](../../src/common/Elsa.Features/Extensions/DependencyInjectionExtensions.cs)
- [ElsaFeature](../../src/modules/Elsa/Features/ElsaFeature.cs)
- [AppFeature](../../src/modules/Elsa/Features/AppFeature.cs)

`AppFeature` is a small wrapper that lets application-specific configuration run after the default `ElsaFeature` dependencies.

## Feature Dependencies

Feature dependencies are explicit attributes. Examples:

- [WorkflowsFeature](../../src/modules/Elsa.Workflows.Core/Features/WorkflowsFeature.cs) depends on system clock, expressions, mediator, default formatters, multitenancy, and commit strategies.
- [WorkflowManagementFeature](../../src/modules/Elsa.Workflows.Management/Features/WorkflowManagementFeature.cs) depends on string compression, mediator, memory cache, system clock, workflows, workflow definitions, and workflow instances.
- [WorkflowsApiFeature](../../src/modules/Elsa.Workflows.Api/Features/WorkflowsApiFeature.cs) depends on workflow instances, management, runtime, and SAS tokens.

This is why feature classes are the best way to learn a module. They encode its runtime assumptions.

## Module Properties

`IModule.Properties` is used as a shared bag during feature configuration. A concrete example is FastEndpoints assembly collection in [Elsa.Api.Common/Extensions/ModuleExtensions.cs](../../src/common/Elsa.Api.Common/Extensions/ModuleExtensions.cs). Features call `AddFastEndpointsAssembly`, and later `AddFastEndpointsFromModule` registers all collected assemblies with FastEndpoints.

## Shell Features

Many modules also have `ShellFeatures/*Feature.cs`. These implement CShells interfaces and allow modular server hosts to activate feature sets from configuration or packages. Shell features are parallel to code-first features:

- Code-first feature: [Elsa.Diagnostics.StructuredLogs/Features/StructuredLogsFeature.cs](../../src/modules/Elsa.Diagnostics.StructuredLogs/Features/StructuredLogsFeature.cs)
- Shell feature: [Elsa.Diagnostics.StructuredLogs/ShellFeatures/StructuredLogsFeature.cs](../../src/modules/Elsa.Diagnostics.StructuredLogs/ShellFeatures/StructuredLogsFeature.cs)

Use shell features when working on modular hosting, package discovery, or `Elsa.ModularServer.Web`. Use code-first features for normal host configuration and tests.

## Extension Method Pattern

Modules expose fluent extension methods in `Extensions/ModuleExtensions.cs` or related files. The method usually calls `module.Configure<TFeature>()` and returns `IModule`:

```csharp
public static IModule UseWorkflowsApi(this IModule module, Action<WorkflowsApiFeature>? configure = default)
{
    module.Configure(configure);
    return module;
}
```

When adding a new module, follow this shape:

- one `Features/*Feature.cs`
- one `ShellFeatures/*Feature.cs` if the module must work with CShells
- one `Extensions/ModuleExtensions.cs`
- tests that prove the feature registers its core contracts

## Common Pitfalls

- Do not register services in extension methods when the module already has a feature class. Put service registration in `Apply()`.
- Do not bypass dependencies with direct service provider access in unrelated modules. Add a contract and dependency if the relationship is real.
- Use `TryAdd*` for overridable defaults and normal `Add*` for deliberate multiple registrations such as handlers, validators, and descriptors.
- If a feature uses `Module.Configure<OtherFeature>()`, verify that the other feature is already a dependency or that optional behavior is intentional.
