using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit.Abstractions;

namespace Elsa.Workflows.Core.UnitTests.Middleware.Activities;

/// <summary>
/// Base class for testing activity execution middleware components. Provides a test activity and an 
/// activity execution context that can be used in the tests. Also allows you to configure the
/// <see cref="IServiceCollection"/> for the tests by overriding the <see cref="ConfigureServices"/> method.
/// </summary>
/// <typeparam name="T">The type of the activity execution middleware being tested.</typeparam>
public abstract class ActivityExecutionMiddlewareTestsBase<T> : IAsyncLifetime where T : class, IActivityExecutionMiddleware
{
    [Activity(Type = "TestActivity", Namespace = "UnitTests")]
    public class TestActivity : Activity
    {
        public Exception? ExecuteThrows { get; set; }
        public Exception? ExecuteFaults { get; set; }
        public Exception? CanExecuteThrows { get; set; }
        public bool AutoComplete { get; set; } = false;

        protected override ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
        {
            if (CanExecuteThrows is not null)
            {
                throw CanExecuteThrows;
            }

            return ValueTask.FromResult(true);
        }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            if (ExecuteThrows is not null)
            {
                throw ExecuteThrows;
            }

            if (ExecuteFaults is not null)
            {
                context.Fault(ExecuteFaults);
            }

            if (AutoComplete)
            {
                await context.CompleteActivityAsync();
            }
        }
    }

    protected readonly TestActivity _activity;
    protected readonly ITestOutputHelper _testOutputHelper;
    protected readonly INotificationSender _notificationSender;
    private readonly ActivityTestFixture _activityTestFixture;

    protected ActivityExecutionContext ExecutionContext { get; private set; }

    protected Action<IActivityExecutionPipelineBuilder> PipelineFactory { get; set; } = b => b.UseMiddleware<T>();

    protected IActivityExecutionPipeline Pipeline => ExecutionContext.GetRequiredService<IActivityExecutionPipeline>();

    protected ActivityExecutionMiddlewareTestsBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _activity = new TestActivity();
        _notificationSender = Substitute.For<INotificationSender>();

        ExecutionContext = default!;

        _activityTestFixture = new ActivityTestFixture(_activity)
            .ConfigureServices(ConfigureServices);
    }

    /// <summary>
    /// Allows you to configure the <see cref="IServiceCollection"/>
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Skip doing any commits
        var activityCommitStrategy = Substitute.For<IActivityCommitStrategy>();
        activityCommitStrategy
            .ShouldCommit(Arg.Any<ActivityCommitStateStrategyContext>())
            .Returns(CommitAction.Skip);

        services
            .AddTransient<IActivityExecutionPipelineBuilder, ActivityExecutionPipelinePipelineBuilder>()
            .AddTransient<IActivityExecutionMiddleware, T>()
            .RemoveAll<INotificationSender>()
            .AddTransient(_ => _notificationSender)
            .AddTransient<IIncidentStrategyResolver, DefaultIncidentStrategyResolver>()
            .AddTransient(_ => Substitute.For<ICommitStrategyRegistry>())
            .Configure<CommitStateOptions>(options => options.DefaultActivityCommitStrategy = activityCommitStrategy)
            .AddTransient<IActivityExecutionPipeline>(sp => new ActivityExecutionPipeline(sp, PipelineFactory))
            .AddSingleton(_activity)
            .AddLogging(config => config.AddProvider(new XunitLoggerProvider(_testOutputHelper)))
            .RemoveAll<IActivityRegistry>()
            .AddSingleton<ActivityRegistry>()
            .AddSingleton<IActivityRegistry>(sp =>
            {
                var registry = sp.GetRequiredService<ActivityRegistry>();
                registry.Add(_activity.GetType(), new Workflows.Models.ActivityDescriptor());
                return registry;
            });
    }


    async Task IAsyncLifetime.InitializeAsync()
    {
        ExecutionContext = await _activityTestFixture.BuildAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
        => Task.CompletedTask;
}
