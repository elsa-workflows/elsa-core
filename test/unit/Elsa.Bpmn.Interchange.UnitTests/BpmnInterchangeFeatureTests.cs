using Elsa.Bpmn.Interchange.Features;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Elsa.Bpmn.Interchange.UnitTests;

public class BpmnInterchangeFeatureTests
{
    [Test]
    public async Task UseBpmnInterchange_ConfiguresBpmnInterchangeFeature()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();

        module.UseBpmnInterchange();
        module.Apply();

        await Assert.That(module.HasFeature<BpmnInterchangeFeature>()).IsTrue();
    }
}