using System;
using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Helpers;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using YesSql;
using YesSql.Provider.Sqlite;

namespace Elsa.Testing.Shared.Helpers
{
    public abstract class WorkflowsUnitTestBase : IAsyncLifetime, IDisposable
    {
        private readonly TemporaryFolder _tempFolder;

        protected WorkflowsUnitTestBase(ITestOutputHelper testOutputHelper, Action<IServiceCollection>? configureServices = default)
        {
            _tempFolder = new TemporaryFolder();
            TestOutputHelper = testOutputHelper;

            var services = new ServiceCollection()
                .AddElsa(options => options.UsePersistence(config => ConfigurePersistence(config, _tempFolder.Folder)))
                .AddConsoleActivities(Console.In, new XunitConsoleForwarder(testOutputHelper));
            
            configureServices?.Invoke(services);
            ServiceProvider = services.BuildServiceProvider();
            WorkflowHost = ServiceProvider.GetRequiredService<IWorkflowHost>();
        }

        protected ITestOutputHelper TestOutputHelper { get; }
        protected ServiceProvider ServiceProvider { get; }
        protected IWorkflowHost WorkflowHost { get; }
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