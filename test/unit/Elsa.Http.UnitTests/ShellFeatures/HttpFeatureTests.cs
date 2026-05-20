using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Elsa.Http.Bookmarks;
using Elsa.Http.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Http.UnitTests.ShellFeatures;

public class HttpFeatureTests
{
    [Fact]
    public void ConfigureServices_RegistersHttpEndpointBookmarkPayloadTypeAlias()
    {
        var services = new ServiceCollection();
        var feature = new HttpFeature();

        feature.ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();
        var expressionOptions = serviceProvider.GetRequiredService<IOptions<ExpressionOptions>>();
        var registry = new WellKnownTypeRegistry(expressionOptions);

        Assert.True(registry.TryGetType(nameof(HttpEndpointBookmarkPayload), out var type));
        Assert.Equal(typeof(HttpEndpointBookmarkPayload), type);
    }
}
