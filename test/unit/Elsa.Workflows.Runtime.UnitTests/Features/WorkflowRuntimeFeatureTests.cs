using Elsa.Features.Services;
using Elsa.Workflows;
using NSubstitute;
using RuntimeFeature = Elsa.Workflows.Runtime.Features.WorkflowRuntimeFeature;
using ShellRuntimeFeature = Elsa.Workflows.Runtime.ShellFeatures.WorkflowRuntimeFeature;

namespace Elsa.Workflows.Runtime.UnitTests.Features;

public class WorkflowRuntimeFeatureTests
{
    private readonly RuntimeFeature _feature = new(Substitute.For<IModule>());
    private readonly ShellRuntimeFeature _shellFeature = new();

    [Fact]
    public void AddWorkflow_Throws_WhenTypeDoesNotImplementWorkflow()
    {
        Assert.Throws<ArgumentException>(() => _feature.AddWorkflow(typeof(NotAWorkflow)));
    }

    [Theory]
    [MemberData(nameof(NonInstantiableWorkflowTypes))]
    public void AddWorkflow_Throws_WhenWorkflowTypeIsNotInstantiable(Type workflowType)
    {
        Assert.Throws<ArgumentException>(() => _feature.AddWorkflow(workflowType));
    }

    [Fact]
    public void ShellAddWorkflow_Throws_WhenTypeDoesNotImplementWorkflow()
    {
        Assert.Throws<ArgumentException>(() => _shellFeature.AddWorkflow(typeof(NotAWorkflow)));
    }

    [Theory]
    [MemberData(nameof(NonInstantiableWorkflowTypes))]
    public void ShellAddWorkflow_Throws_WhenWorkflowTypeIsNotInstantiable(Type workflowType)
    {
        Assert.Throws<ArgumentException>(() => _shellFeature.AddWorkflow(workflowType));
    }

    public static TheoryData<Type> NonInstantiableWorkflowTypes() => new()
    {
        typeof(IWorkflow),
        typeof(WorkflowBase),
        typeof(GenericWorkflow<>)
    };

    private sealed class NotAWorkflow
    {
    }

    private sealed class GenericWorkflow<T> : IWorkflow
    {
        public ValueTask BuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
