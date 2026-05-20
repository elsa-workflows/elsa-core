using Elsa.Features.Services;
using NSubstitute;
using RuntimeFeature = Elsa.Workflows.Runtime.Features.WorkflowRuntimeFeature;
using ShellRuntimeFeature = Elsa.Workflows.Runtime.ShellFeatures.WorkflowRuntimeFeature;

namespace Elsa.Workflows.Runtime.UnitTests.Features;

public class WorkflowRuntimeFeatureTests
{
    [Fact]
    public void AddWorkflow_Throws_WhenTypeDoesNotImplementWorkflow()
    {
        var feature = new RuntimeFeature(Substitute.For<IModule>());

        Assert.Throws<ArgumentException>(() => feature.AddWorkflow(typeof(NotAWorkflow)));
    }

    [Fact]
    public void ShellAddWorkflow_Throws_WhenTypeDoesNotImplementWorkflow()
    {
        var feature = new ShellRuntimeFeature();

        Assert.Throws<ArgumentException>(() => feature.AddWorkflow(typeof(NotAWorkflow)));
    }

    private sealed class NotAWorkflow;
}
