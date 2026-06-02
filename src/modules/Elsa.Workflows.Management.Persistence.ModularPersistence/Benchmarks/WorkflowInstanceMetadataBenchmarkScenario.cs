namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Benchmarks;

public sealed record WorkflowInstanceMetadataBenchmarkScenario(
    string Name,
    int InstanceCount,
    int AverageWorkflowStateBytes,
    int AverageMetadataBytes = WorkflowInstanceMetadataBenchmarkEstimator.DefaultMetadataBytes)
{
    public long TotalMetadataBytes => (long)InstanceCount * AverageMetadataBytes;

    public long TotalWorkflowStateBytes => (long)InstanceCount * AverageWorkflowStateBytes;

    public long TotalFullDocumentBytes => TotalMetadataBytes + TotalWorkflowStateBytes;
}
