namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Benchmarks;

public static class WorkflowInstanceMetadataBenchmarkEstimator
{
    public const int DefaultMetadataBytes = 1024;

    public static IReadOnlyCollection<WorkflowInstanceMetadataBenchmarkEstimate> Estimate(IEnumerable<WorkflowInstanceMetadataBenchmarkScenario> scenarios) =>
        scenarios.Select(Estimate).ToArray();

    public static WorkflowInstanceMetadataBenchmarkEstimate Estimate(WorkflowInstanceMetadataBenchmarkScenario scenario)
    {
        if (scenario.InstanceCount < 0)
            throw new ArgumentOutOfRangeException(nameof(scenario.InstanceCount), "Instance count cannot be negative.");

        if (scenario.AverageMetadataBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(scenario.AverageMetadataBytes), "Average metadata bytes must be greater than zero.");

        if (scenario.AverageWorkflowStateBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(scenario.AverageWorkflowStateBytes), "Average workflow state bytes cannot be negative.");

        var metadataOnlyBytes = scenario.TotalMetadataBytes;
        var fullDocumentBytes = scenario.TotalFullDocumentBytes;
        var ratio = fullDocumentBytes == 0 ? 0 : decimal.Round(metadataOnlyBytes / (decimal)fullDocumentBytes, 4);

        return new WorkflowInstanceMetadataBenchmarkEstimate(
            scenario.Name,
            scenario.InstanceCount,
            scenario.AverageMetadataBytes,
            scenario.AverageWorkflowStateBytes,
            metadataOnlyBytes,
            fullDocumentBytes,
            ratio,
            "Declared metadata indexes only; no workflow state materialization.",
            "Metadata projection write only; workflow state blob remains on the existing persistence path.");
    }
}
