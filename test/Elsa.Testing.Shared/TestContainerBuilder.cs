using System;
using Elsa.Extensions;
using Elsa.Modules.Activities.Contracts;
using Elsa.Modules.Activities.Providers;
using Elsa.Persistence.InMemory.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Testing.Shared;

public class TestContainerBuilder
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ServiceCollection _services;

    public TestContainerBuilder(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _services = new ServiceCollection();

        _services
            .AddElsa()
            .AddInMemoryPersistence()
            .AddSingleton(testOutputHelper)
            .AddSingleton<IStandardOutStreamProvider>(new StandardOutStreamProvider(new XunitConsoleTextWriter(testOutputHelper)))
            .AddLogging(logging => logging.AddProvider(new XunitLoggerProvider(testOutputHelper)).SetMinimumLevel(LogLevel.Debug));
    }

    public IServiceProvider Build() => _services.BuildServiceProvider();

    public TestContainerBuilder Configure(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    public TestContainerBuilder WithCapturingTextWriter(CapturingTextWriter capturingTextWriter)
    {
        var combinedTextWriter = new CombinedTextWriter(capturingTextWriter, new XunitConsoleTextWriter(_testOutputHelper));
        _services.AddSingleton<IStandardOutStreamProvider>(new StandardOutStreamProvider(combinedTextWriter));
        return this;
    }
}