using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Core.IntegrationTests.Helpers;
using Elsa.Services;
using Elsa.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using YesSql;
using YesSql.Provider.Sqlite;

namespace Elsa.Testing.Shared.Helpers
{
    public abstract class WorkflowsTestBase : IAsyncLifetime, IDisposable
    {
        private readonly TemporaryFolder _tempFolder;

        protected WorkflowsTestBase(ITestOutputHelper testOutputHelper, Action<IServiceCollection>? configureServices = default)
        {
            _tempFolder = new TemporaryFolder();
            TestOutputHelper = testOutputHelper;

            var services = new ServiceCollection()
                .AddElsa(options => options.UsePersistence(config => ConfigurePersistence(config, _tempFolder.Folder)))
                .AddConsoleActivities(Console.In, new XunitConsoleForwarder(testOutputHelper));
            
            configureServices?.Invoke(services);
            ServiceProvider = services.BuildServiceProvider();
            WorkflowRunner = ServiceProvider.GetRequiredService<IWorkflowRunner>();
            WorkflowBlueprintMaterializer = ServiceProvider.GetRequiredService<IWorkflowBlueprintMaterializer>();
            WorkflowBuilder = ServiceProvider.GetRequiredService<IWorkflowBuilder>();
            WorkflowRegistry = ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            WorkflowSelector = ServiceProvider.GetRequiredService<IWorkflowSelector>();
        }

        protected ITestOutputHelper TestOutputHelper { get; }
        protected ServiceProvider ServiceProvider { get; }
        protected IWorkflowRunner WorkflowRunner { get; }
        protected IWorkflowBlueprintMaterializer WorkflowBlueprintMaterializer { get; }
        protected IWorkflowBuilder WorkflowBuilder { get; }
        protected IWorkflowRegistry WorkflowRegistry { get; }
        protected IWorkflowSelector WorkflowSelector { get; }
        public virtual void Dispose() => _tempFolder.Dispose();

        public virtual async Task InitializeAsync()
        {
            var startupRunner = ServiceProvider.GetRequiredService<IStartupRunner>();
            await startupRunner.StartupAsync();
        }

        public virtual async Task DisposeAsync()
        {
            await ServiceProvider.DisposeAsync();
            Dispose();
        }

        private void ConfigurePersistence(IConfiguration configuration, string folder)
        {
            var connectionString = $@"Data Source={folder}elsa.db;Cache=Shared";
            configuration.UseSqLite(connectionString).UseDefaultIdGenerator();
        }
    }
}