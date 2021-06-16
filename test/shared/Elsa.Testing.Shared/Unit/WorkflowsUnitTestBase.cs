using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Bookmarks;
using Elsa.Services.WorkflowStorage;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.Testing.Shared.Unit
{
    public abstract class WorkflowsUnitTestBase : IAsyncLifetime, IDisposable
    {
        private readonly TemporaryFolder _tempFolder;

        protected WorkflowsUnitTestBase(ITestOutputHelper testOutputHelper, Action<IServiceCollection>? configureServices = default)
        {
            _tempFolder = new TemporaryFolder();
            TestOutputHelper = testOutputHelper;

            var services = new ServiceCollection()
                .AddElsa(options => options
                    .AddConsoleActivities(Console.In, new XunitConsoleForwarder(testOutputHelper)));

            configureServices?.Invoke(services);
            ServiceProvider = services.BuildServiceProvider();
            ServiceScope = ServiceProvider.CreateScope();
            WorkflowRunner = ServiceScope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
            WorkflowBuilderAndStarter = ServiceScope.ServiceProvider.GetRequiredService<IBuildsAndStartsWorkflow>();
            WorkflowStarter = ServiceScope.ServiceProvider.GetRequiredService<IStartsWorkflow>();
            WorkflowResumer = ServiceScope.ServiceProvider.GetRequiredService<IResumesWorkflow>();
            WorkflowBlueprintMaterializer = ServiceScope.ServiceProvider.GetRequiredService<IWorkflowBlueprintMaterializer>();
            WorkflowBuilder = ServiceScope.ServiceProvider.GetRequiredService<IWorkflowBuilder>();
            WorkflowRegistry = ServiceScope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            BookmarkFinder = ServiceScope.ServiceProvider.GetRequiredService<IBookmarkFinder>();
            WorkflowExecutionLog = ServiceScope.ServiceProvider.GetRequiredService<IWorkflowExecutionLog>();
            WorkflowStorageService = ServiceScope.ServiceProvider.GetRequiredService<IWorkflowStorageService>();
        }

        protected ITestOutputHelper TestOutputHelper { get; }
        protected ServiceProvider ServiceProvider { get; }
        protected IServiceScope ServiceScope { get; }
        protected IWorkflowRunner WorkflowRunner { get; }
        protected IBuildsAndStartsWorkflow WorkflowBuilderAndStarter { get; }
        protected IStartsWorkflow WorkflowStarter { get; }
        protected IResumesWorkflow WorkflowResumer { get; }
        protected IWorkflowExecutionLog WorkflowExecutionLog { get; }
        protected IWorkflowBlueprintMaterializer WorkflowBlueprintMaterializer { get; }
        protected IWorkflowBuilder WorkflowBuilder { get; }
        protected IWorkflowRegistry WorkflowRegistry { get; }
        protected IBookmarkFinder BookmarkFinder { get; }
        protected IWorkflowStorageService WorkflowStorageService { get; }
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
    }
}