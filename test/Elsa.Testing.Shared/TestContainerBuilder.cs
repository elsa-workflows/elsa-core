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
    public static IServiceProvider Build(ITestOutputHelper testOutputHelper, Action<IServiceCollection>? configureServices = default)
    {
        var services = new ServiceCollection();

        services
            .AddElsa()
            .AddInMemoryPersistence()
            .AddLogging(logging => logging.AddProvider(new XunitLoggerProvider(testOutputHelper)).SetMinimumLevel(LogLevel.Debug));

        services.AddSingleton(testOutputHelper);
        services.AddSingleton<IStandardOutStreamProvider>(new StandardOutStreamProvider(new XunitConsoleTextWriter(testOutputHelper)));
        
        configureServices?.Invoke(services);

        return services.BuildServiceProvider();
    }
}