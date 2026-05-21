using System.Reflection;
using Elsa.Expressions.Options;
using Elsa.Features.Services;
using Elsa.Extensions;
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

    [Fact]
    public void AddWorkflow_AllowsClosedGenericWorkflowType()
    {
        var workflowType = typeof(GenericWorkflow<int>);

        _feature.AddWorkflow(workflowType);

        Assert.Contains(workflowType.GetSimpleAssemblyQualifiedName(), _feature.Workflows.Keys);
        Assert.Contains(workflowType.FullName!, _feature.Workflows.Keys);
    }

    [Fact]
    public void ShellAddWorkflow_AllowsClosedGenericWorkflowType()
    {
        var workflowType = typeof(GenericWorkflow<int>);

        _shellFeature.AddWorkflow(workflowType);

        Assert.Contains(workflowType.GetSimpleAssemblyQualifiedName(), _shellFeature.Workflows.Keys);
        Assert.Contains(workflowType.FullName!, _shellFeature.Workflows.Keys);
    }

    [Fact]
    public void WorkflowsAdd_RegistersWorkflowTypeAlias()
    {
        var workflowType = typeof(GenericWorkflow<int>);
        var options = new ExpressionOptions();

        _feature.Workflows.Add(workflowType);

        RegisterWorkflowTypeAliases(_feature, options);

        Assert.Equal(workflowType, options.AliasTypeDictionary[workflowType.GetSimpleAssemblyQualifiedName()]);
    }

    [Fact]
    public void WorkflowsAdd_DoesNotThrow_WhenLegacyKeyAlreadyExists()
    {
        var workflowType = typeof(GenericWorkflow<int>);
        _feature.Workflows.Add(workflowType.FullName!, _ => new ValueTask<IWorkflow>(new GenericWorkflow<int>()));

        _feature.Workflows.Add(workflowType);

        Assert.Contains(workflowType.GetSimpleAssemblyQualifiedName(), _feature.Workflows.Keys);
        Assert.Contains(workflowType.FullName!, _feature.Workflows.Keys);
    }

    [Fact]
    public void ShellWorkflowsAdd_RegistersWorkflowTypeAlias()
    {
        var workflowType = typeof(GenericWorkflow<int>);
        var options = new ExpressionOptions();

        _shellFeature.Workflows.Add(workflowType);

        RegisterWorkflowTypeAliases(_shellFeature, options);

        Assert.Equal(workflowType, options.AliasTypeDictionary[workflowType.GetSimpleAssemblyQualifiedName()]);
    }

    [Fact]
    public void WorkflowsAdd_Throws_WhenTypeDoesNotImplementWorkflow()
    {
        Assert.Throws<ArgumentException>(() => _feature.Workflows.Add(typeof(NotAWorkflow)));
    }

    [Fact]
    public void RegisterWorkflowTypeAliases_RegistersOnlyTrackedWorkflowTypes()
    {
        var workflowType = typeof(GenericWorkflow<int>);
        var options = new ExpressionOptions();
        _feature.AddWorkflow(workflowType);
        _feature.Workflows[typeof(NotAWorkflow).AssemblyQualifiedName!] = _ => new ValueTask<IWorkflow>(new GenericWorkflow<int>());

        RegisterWorkflowTypeAliases(_feature, options);

        Assert.Equal(workflowType, options.AliasTypeDictionary[workflowType.GetSimpleAssemblyQualifiedName()]);
        Assert.DoesNotContain(typeof(NotAWorkflow).AssemblyQualifiedName!, options.AliasTypeDictionary.Keys);
    }

    [Fact]
    public void ShellRegisterWorkflowTypeAliases_RegistersOnlyTrackedWorkflowTypes()
    {
        var workflowType = typeof(GenericWorkflow<int>);
        var options = new ExpressionOptions();
        _shellFeature.AddWorkflow(workflowType);
        _shellFeature.Workflows[typeof(NotAWorkflow).AssemblyQualifiedName!] = _ => new ValueTask<IWorkflow>(new GenericWorkflow<int>());

        RegisterWorkflowTypeAliases(_shellFeature, options);

        Assert.Equal(workflowType, options.AliasTypeDictionary[workflowType.GetSimpleAssemblyQualifiedName()]);
        Assert.DoesNotContain(typeof(NotAWorkflow).AssemblyQualifiedName!, options.AliasTypeDictionary.Keys);
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

    private static void RegisterWorkflowTypeAliases(object feature, ExpressionOptions options)
    {
        feature.GetType()
            .GetMethod("RegisterWorkflowTypeAliases", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(feature, new object[] { options });
    }
}
