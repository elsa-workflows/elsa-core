using Elsa.Bpmn.Interchange.Features;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.UnitTests;

public class BpmnInterchangeFeatureTests
{
    [Fact]
    public void UseBpmnInterchange_ConfiguresBpmnInterchangeFeature()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();

        module.UseBpmnInterchange();
        module.Apply();

        Assert.True(module.HasFeature<BpmnInterchangeFeature>());
    }
}
