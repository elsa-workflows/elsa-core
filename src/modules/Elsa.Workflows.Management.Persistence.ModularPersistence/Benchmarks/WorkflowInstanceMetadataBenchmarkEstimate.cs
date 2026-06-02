namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Benchmarks;

public sealed record WorkflowInstanceMetadataBenchmarkEstimate(
    string Name,
    int InstanceCount,
    int AverageMetadataBytes,
    int AverageWorkflowStateBytes,
    long MetadataOnlyBytes,
    long FullDocumentBytes,
    decimal MetadataToFullDocumentRatio,
    string ReadPath,
    string WritePath);
