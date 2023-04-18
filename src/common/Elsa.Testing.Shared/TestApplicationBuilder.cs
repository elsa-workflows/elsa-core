using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Testing.Shared;

/// <summary>
/// A builder that can be used to configure an <see cref="IServiceProvider"/> for testing purposes.
/// </summary>
public class TestApplicationBuilder
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ServiceCollection _services;
    private Action<IModule> _configureElsa;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestApplicationBuilder"/> class.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper.</param>
    public TestApplicationBuilder(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _services = new ServiceCollection();

        _services
            .AddSingleton(testOutputHelper)
            .AddSingleton<IConfiguration, ConfigurationManager>()
            .AddLogging(logging => logging.AddProvider(new XunitLoggerProvider(testOutputHelper)).SetMinimumLevel(LogLevel.Debug));
        
        _configureElsa += elsa => elsa
            .AddActivitiesFrom<WriteLine>()
            .UseScheduling()
            .UseJavaScript()
            .UseLiquid()
            .UseWorkflows(workflows => workflows
                .WithStandardOutStreamProvider(_ => new StandardOutStreamProvider(new XunitConsoleTextWriter(_testOutputHelper)))
            );
    }

    /// <summary>
    /// Builds the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <returns>The <see cref="IServiceProvider"/>.</returns>
    public IServiceProvider Build()
    {
        _services.AddElsa(_configureElsa);
        return _services.BuildServiceProvider();
    }

    /// <summary>
    /// Configures Elsa.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The <see cref="TestApplicationBuilder"/>.</returns>
    public TestApplicationBuilder ConfigureElsa(Action<IModule> configure)
    {
        _configureElsa += configure;
        return this;
    }
    
    /// <summary>
    /// Configures the service provider.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The <see cref="TestApplicationBuilder"/>.</returns>
    public TestApplicationBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    /// <summary>
    /// Configures the service provider to use a <see cref="CapturingTextWriter"/> to capture standard output.
    /// </summary>
    /// <param name="capturingTextWriter">The capturing text writer.</param>
    /// <returns>The <see cref="TestApplicationBuilder"/>.</returns>
    public TestApplicationBuilder WithCapturingTextWriter(CapturingTextWriter capturingTextWriter)
    {
        var combinedTextWriter = new CombinedTextWriter(capturingTextWriter, new XunitConsoleTextWriter(_testOutputHelper));
        var provider = new StandardOutStreamProvider(combinedTextWriter);

        ConfigureElsa(elsa => elsa.UseWorkflows(workflows => workflows.WithStandardOutStreamProvider(_ => provider)));
        return this;
    }
}