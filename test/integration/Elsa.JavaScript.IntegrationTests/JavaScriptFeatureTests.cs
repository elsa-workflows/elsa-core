using System.Reflection;
using Elsa.Expressions.JavaScript.ShellFeatures;
using Elsa.Expressions.Options;
using Elsa.Expressions.Services;
using Jint.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.JavaScript.IntegrationTests;

public class JavaScriptFeatureTests
{
    [Fact]
    public void ConfigureServices_RegistersUniqueWrapperExceptionAlias()
    {
        var services = new ServiceCollection();
        var feature = new JavaScriptFeature();
        var wrapperExceptionType = typeof(JavaScriptException).GetNestedType("JavaScriptErrorWrapperException", BindingFlags.Public | BindingFlags.NonPublic);

        feature.ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();
        var expressionOptions = serviceProvider.GetRequiredService<IOptions<ExpressionOptions>>();
        var registry = new WellKnownTypeRegistry(expressionOptions);

        Assert.NotNull(wrapperExceptionType);
        Assert.True(registry.TryGetType("Jint.JavaScriptErrorWrapperException", out var type));
        Assert.Equal(wrapperExceptionType, type);
        Assert.False(registry.TryGetType(wrapperExceptionType.Name, out _));
    }
}
