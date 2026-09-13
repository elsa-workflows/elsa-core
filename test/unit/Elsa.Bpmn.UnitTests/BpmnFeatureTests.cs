using Elsa.Bpmn.Features;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Elsa.Bpmn.UnitTests;

public class BpmnFeatureTests
{
    [Test]
    public async Task UseBpmn_ConfiguresBpmnFeature()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();

        module.UseBpmn();
        module.Apply();

        await Assert.That(module.HasFeature<BpmnFeature>()).IsTrue();
    }
}