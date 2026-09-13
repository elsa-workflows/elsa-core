using Elsa.Extensions;
using Elsa.Features.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.IntegrationTests;

public class BpmnInterchangeFeatureTests
{
    [Test]
    public async Task Build_WithUseBpmnInterchange_RegistersBpmnInterchangeFeature()
    {
        var services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa.UseBpmnInterchange())
            .Build();

        var registry = services.GetRequiredService<IInstalledFeatureRegistry>();

        await Assert.That(registry.Find("Elsa.Bpmn")).IsNotNull();
        await Assert.That(registry.Find("Elsa.BpmnInterchange")).IsNotNull();
    }
}
