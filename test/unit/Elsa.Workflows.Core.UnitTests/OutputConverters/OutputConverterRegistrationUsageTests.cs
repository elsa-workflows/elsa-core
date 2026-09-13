using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.Core.UnitTests.OutputConverters.Fixtures;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConverterRegistrationUsageTests
{
    [Test]
    public async Task RegisteredReferenceConverter_IsDiscoverableAndConvertsWithBindingSettings()
    {
        var services = new ServiceCollection();
        services.AddOutputConverter<ReferenceOutputConverter>(OutputConverterRegistrationTests.Descriptor, ServiceLifetime.Scoped);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        using var settingsDocument = JsonDocument.Parse("""{"prefix":"converted:"}""");

        var descriptor = scope.ServiceProvider.GetRequiredService<IOutputConverterRegistry>()
            .FindCompatible(typeof(string), typeof(string))
            .Single();
        var converter = scope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(descriptor.Id);
        var result = converter.Convert(new OutputConversionContext(
            "native value",
            typeof(string),
            typeof(string),
            settingsDocument.RootElement));

        await Assert.That(descriptor).IsEqualTo(OutputConverterRegistrationTests.Descriptor);
        await Assert.That(result).IsEqualTo("converted:native value");
    }
}