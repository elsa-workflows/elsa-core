using Elsa.Studio.Bpmn.BpmnJs;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Bpmn.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>Covers which BPMN designer a host that adds the workflows module gets.</summary>
public class WorkflowsModuleBpmnDesignerTests
{
    [Fact(DisplayName = "A host that enables no BPMN designer gets bpmn-js")]
    public void WithoutADesigner_BpmnJsIsUsed()
    {
        using var provider = WorkflowsModule().BuildServiceProvider();

        Assert.Equal(BpmnJsDesigner.Descriptor, provider.GetRequiredService<BpmnDesignerResolver>().Resolve());
    }

    [Fact(DisplayName = "A host that enables a BPMN designer gets that one, without having to choose between it and bpmn-js")]
    public void WithADesigner_ThatOneIsUsed()
    {
        var designer = new BpmnDesignerDescriptor("other", "_content/Other/other.js");

        using var provider = WorkflowsModule().AddBpmnDesigner(designer).BuildServiceProvider();

        Assert.Equal(designer, provider.GetRequiredService<BpmnDesignerResolver>().Resolve());
    }

    private static IServiceCollection WorkflowsModule()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();
        services.AddRemoteBackend();
        services.AddWorkflowsModule();
        return services;
    }
}
