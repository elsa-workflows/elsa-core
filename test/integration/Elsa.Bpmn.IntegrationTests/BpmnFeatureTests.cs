using Elsa.Extensions;
using Elsa.Features.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.IntegrationTests;

public class BpmnFeatureTests
{
    [Test]
    public async Task Build_WithUseBpmn_RegistersBpmnFeature()
    {
        await using var services = (ServiceProvider)new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa.UseBpmn())
            .Build();

        var registry = services.GetRequiredService<IInstalledFeatureRegistry>();

        await Assert.That(registry.Find("Elsa.Bpmn")).IsNotNull();

    }
}
