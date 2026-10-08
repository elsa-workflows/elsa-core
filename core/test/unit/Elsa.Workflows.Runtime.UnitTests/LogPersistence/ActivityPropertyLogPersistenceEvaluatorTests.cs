using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.LogPersistence.Strategies;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Runtime.UnitTests.LogPersistence;

public class ActivityPropertyLogPersistenceEvaluatorTests
{
    [Fact]
    public async Task Evaluate_HonorsActivityInternalStateInclude_WhenDefaultIsExclude()
    {
        var activity = CreateWriteLine(defaultMode: typeof(Exclude), internalState: typeof(Include));

        var map = await EvaluateAsync(activity);

        Assert.Equal(LogPersistenceMode.Include, map.InternalState);
        Assert.Equal(LogPersistenceMode.Exclude, map.Inputs[nameof(WriteLine.Text)]);
    }

    [Fact]
    public async Task Evaluate_HonorsActivityInternalStateExclude_WhenDefaultIsInclude()
    {
        var activity = CreateWriteLine(defaultMode: typeof(Include), internalState: typeof(Exclude));

        var map = await EvaluateAsync(activity);

        Assert.Equal(LogPersistenceMode.Exclude, map.InternalState);
        Assert.Equal(LogPersistenceMode.Include, map.Inputs[nameof(WriteLine.Text)]);
    }

    [Fact]
    public async Task Evaluate_FallsBackToWorkflowInternalState_WhenActivityInternalStateIsMissing()
    {
        var activity = CreateWriteLine(defaultMode: typeof(Exclude));

        var map = await EvaluateAsync(activity, workflow =>
        {
            workflow.CustomProperties["logPersistenceConfig"] = new Dictionary<string, object>
            {
                ["default"] = Strategy(typeof(Exclude)),
                ["internalState"] = Strategy(typeof(Include))
            };
        });

        Assert.Equal(LogPersistenceMode.Include, map.InternalState);
        Assert.Equal(LogPersistenceMode.Exclude, map.Inputs[nameof(WriteLine.Text)]);
    }

    [Fact]
    public async Task Evaluate_ActivityInternalStateOverridesWorkflowInternalState()
    {
        var activity = CreateWriteLine(defaultMode: typeof(Exclude), internalState: typeof(Exclude));

        var map = await EvaluateAsync(activity, workflow =>
        {
            workflow.CustomProperties["logPersistenceConfig"] = new Dictionary<string, object>
            {
                ["default"] = Strategy(typeof(Exclude)),
                ["internalState"] = Strategy(typeof(Include))
            };
        });

        Assert.Equal(LogPersistenceMode.Exclude, map.InternalState);
    }

    [Fact]
    public async Task Evaluate_FallsBackToDefault_WhenInternalStateIsMissing()
    {
        var activity = CreateWriteLine(defaultMode: typeof(Exclude));

        var map = await EvaluateAsync(activity);

        Assert.Equal(LogPersistenceMode.Exclude, map.InternalState);
        Assert.Equal(LogPersistenceMode.Exclude, map.Inputs[nameof(WriteLine.Text)]);
    }

    [Fact]
    public async Task Evaluate_IgnoresTopLevelCustomPropertiesInternalState()
    {
        var activity = CreateWriteLine(defaultMode: typeof(Exclude));
        activity.CustomProperties["internalState"] = Strategy(typeof(Include));

        var map = await EvaluateAsync(activity);

        Assert.Equal(LogPersistenceMode.Exclude, map.InternalState);
    }

    [Fact]
    public async Task Evaluate_UsesWorkflowRootContext_ForWorkflowInternalStateExpression()
    {
        var activity = CreateWriteLine(defaultMode: typeof(Exclude));
        var fixture = CreateFixture(activity);
        var seed = await fixture.BuildAsync();
        var workflowExecutionContext = seed.WorkflowExecutionContext;
        var rootContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(workflowExecutionContext.Workflow);
        var childContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(activity, new ActivityInvocationOptions
        {
            Owner = rootContext
        });
        workflowExecutionContext.AddActivityExecutionContext(rootContext);
        workflowExecutionContext.AddActivityExecutionContext(childContext);

        workflowExecutionContext.Workflow.CustomProperties["logPersistenceConfig"] = new Dictionary<string, object>
        {
            ["internalState"] = new LogPersistenceConfiguration
            {
                EvaluationMode = LogPersistenceEvaluationMode.Expression,
                Expression = Expression.DelegateExpression(ctx =>
                {
                    ctx.TryGetActivityExecutionContext(out var evaluated);
                    return evaluated.ParentActivityExecutionContext == null
                        ? LogPersistenceMode.Include
                        : LogPersistenceMode.Exclude;
                })
            }
        };

        var map = await CreateEvaluator(childContext).EvaluateLogPersistenceModesAsync(childContext);

        Assert.Equal(LogPersistenceMode.Include, map.InternalState);
    }

    private static WriteLine CreateWriteLine(Type defaultMode, Type? internalState = null)
    {
        var config = new Dictionary<string, object>
        {
            ["default"] = Strategy(defaultMode)
        };

        if (internalState != null)
            config["internalState"] = Strategy(internalState);

        var activity = new WriteLine("text");
        activity.CustomProperties["logPersistenceConfig"] = config;
        return activity;
    }

    private static Dictionary<string, object> Strategy(Type strategyType) => new()
    {
        ["evaluationMode"] = "Strategy",
        ["strategyType"] = strategyType.GetSimpleAssemblyQualifiedName()
    };

    private static async Task<ActivityLogPersistenceModeMap> EvaluateAsync(IActivity activity, Action<Workflow>? configureWorkflow = null)
    {
        var fixture = CreateFixture(activity);
        var context = await fixture.BuildAsync();
        context.WorkflowExecutionContext.AddActivityExecutionContext(context);
        configureWorkflow?.Invoke(context.WorkflowExecutionContext.Workflow);
        return await CreateEvaluator(context).EvaluateLogPersistenceModesAsync(context);
    }

    private static ActivityTestFixture CreateFixture(IActivity activity) =>
        new ActivityTestFixture(activity).ConfigureServices(services =>
        {
            services.AddSingleton<ILogPersistenceStrategy, Include>();
            services.AddSingleton<ILogPersistenceStrategy, Exclude>();
            services.AddSingleton<ILogPersistenceStrategy, Inherit>();
            services.AddSingleton<ILogPersistenceStrategyService, DefaultLogPersistenceStrategyService>();
        });

    private static ActivityPropertyLogPersistenceEvaluator CreateEvaluator(ActivityExecutionContext context) =>
        new(
            context.GetRequiredService<ILogPersistenceStrategyService>(),
            context.GetRequiredService<IExpressionDescriptorRegistry>(),
            context.GetRequiredService<IExpressionEvaluator>(),
            Microsoft.Extensions.Options.Options.Create(new ManagementOptions
            {
                LogPersistenceMode = LogPersistenceMode.Include
            }),
            NullLogger<ActivityPropertyLogPersistenceEvaluator>.Instance);
}
