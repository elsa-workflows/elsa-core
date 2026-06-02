using Elsa.ModularPersistence.Planning;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeStorageDefinitions;

public sealed class SaveRuntimeStorageDefinitionRequest
{
    public string Id { get; set; } = default!;

    public string SchemaName { get; set; } = default!;

    public string StorageUnitName { get; set; } = default!;

    public ICollection<RuntimeStorageFieldDefinition> Fields { get; set; } = [];

    public ICollection<RuntimeStorageIndexDefinition> Indexes { get; set; } = [];

    public ICollection<string> RequiredPermissions { get; set; } = [];
}

public sealed record RuntimeStorageDefinitionsResponse(IReadOnlyCollection<RuntimeStorageDefinition> Items);

public sealed class PublishRuntimeStorageDefinitionRequest
{
    public string? ProviderName { get; set; }
}

public sealed record RuntimeStorageDefinitionPublishResponse(
    RuntimeStorageDefinition Definition,
    bool Succeeded,
    IReadOnlyCollection<string> Errors);

public sealed record RuntimeSchemaAuditResponse(IReadOnlyCollection<RuntimeSchemaAuditEntry> Items);

public sealed class RuntimeStorageIndexPhysicalizationRequest
{
    public string IndexName { get; set; } = default!;
}

public sealed class RuntimeStoragePhysicalizationPlanRequest
{
    public string? ProviderName { get; set; }
}

public sealed record RuntimeStoragePhysicalizationPlanResponse(IReadOnlyCollection<StoragePhysicalizationPlan> Plans);
