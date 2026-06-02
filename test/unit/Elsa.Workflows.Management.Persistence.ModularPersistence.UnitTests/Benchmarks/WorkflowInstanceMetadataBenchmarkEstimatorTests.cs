using Elsa.Workflows.Management.Persistence.ModularPersistence.Benchmarks;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.UnitTests.Benchmarks;

public class WorkflowInstanceMetadataBenchmarkEstimatorTests
{
    [Fact]
    public void EstimateKeepsMetadataCostStableWhenWorkflowStateGrows()
    {
        var small = WorkflowInstanceMetadataBenchmarkEstimator.Estimate(new WorkflowInstanceMetadataBenchmarkScenario("small", 1000, 8 * 1024));
        var large = WorkflowInstanceMetadataBenchmarkEstimator.Estimate(new WorkflowInstanceMetadataBenchmarkScenario("large", 1000, 512 * 1024));

        Assert.Equal(small.MetadataOnlyBytes, large.MetadataOnlyBytes);
        Assert.True(large.FullDocumentBytes > small.FullDocumentBytes);
        Assert.True(large.MetadataToFullDocumentRatio < small.MetadataToFullDocumentRatio);
    }
}
