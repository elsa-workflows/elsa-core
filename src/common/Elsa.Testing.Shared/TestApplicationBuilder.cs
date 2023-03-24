using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Testing.Shared;

public class TestApplicationBuilder
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ServiceCollection _services;
    private Action<IModule> _configureElsa;

    public TestApplicationBuilder(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _services = new ServiceCollection();

        _services
            .AddSingleton(testOutputHelper)
            .AddLogging(logging => logging.AddProvider(new XunitLoggerProvider(testOutputHelper)).SetMinimumLevel(LogLevel.Debug));
        
        _configureElsa += elsa => elsa
            .UseWorkflows(workflows => workflows
                .WithStandardOutStreamProvider(_ => new StandardOutStreamProvider(new XunitConsoleTextWriter(_testOutputHelper)))
            );
    }

    public IServiceProvider Build()
    {
        _services.AddElsa(_configureElsa);
        return _services.BuildServiceProvider();
    }

    public TestApplicationBuilder ConfigureElsa(Action<IModule> configure)
    {
        _configureElsa += configure;
        return this;
    }
    
    public TestApplicationBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    public TestApplicationBuilder WithCapturingTextWriter(CapturingTextWriter capturingTextWriter)
    {
        var combinedTextWriter = new CombinedTextWriter(capturingTextWriter, new XunitConsoleTextWriter(_testOutputHelper));
        var provider = new StandardOutStreamProvider(combinedTextWriter);

        ConfigureElsa(elsa => elsa.UseWorkflows(workflows => workflows.WithStandardOutStreamProvider(_ => provider)));
        return this;
    }
}