using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;
using FluentStorage;
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
            .UseDsl()
            .UseWorkflowManagement()
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

    /// <summary>
    /// Adds a workflow to the service provider.
    /// </summary>
    public TestApplicationBuilder AddWorkflow<T>() where T : IWorkflow
    {
        return ConfigureElsa(elsa => elsa.AddWorkflow<T>());
    }
    
    /// <summary>
    /// Adds activities from the assembly containing the specified type.
    /// </summary>
    public TestApplicationBuilder AddActivitiesFrom<T>()
    {
        return ConfigureElsa(elsa => elsa.AddActivitiesFrom<T>());
    }
    
    /// <summary>
    /// Add workflows from the specified relative directory.
    /// </summary>
    /// <param name="directory">The path segments of the directory.</param>
    public TestApplicationBuilder WithWorkflowsFromDirectory(params string[] directory)
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;
        var workflowsDirectory = directory.Prepend(assemblyDirectory).ToArray();
        _configureElsa += elsa => elsa.UseFluentStorageProvider(storage => storage.BlobStorage = sp => StorageFactory.Blobs.DirectoryFiles(Path.Combine(workflowsDirectory)));
        return this;
    }

    private static string GetWorkflowsDirectory()
    {
        return Path.Combine("Scenarios", "DependencyWorkflows", "Workflows");
    }
}