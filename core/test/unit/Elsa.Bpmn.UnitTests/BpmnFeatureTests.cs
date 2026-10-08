using Elsa.Bpmn.Features;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.UnitTests;

public class BpmnFeatureTests
{
    [Fact]
    public void UseBpmn_ConfiguresBpmnFeature()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();

        module.UseBpmn();
        module.Apply();

        Assert.True(module.HasFeature<BpmnFeature>());
    }
}
