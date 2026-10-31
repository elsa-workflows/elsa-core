using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class ActivityExecutionContextOutputConversionTests
{
    private const string ConverterId = "tests.to-text";

    [Fact]
    public async Task Set_WithConverter_WritesConvertedVariableAndRecordsNativeOutput()
    {
        var converter = new RecordingConverter(context => context.Value.ToString()!);
        var variable = new Variable<string>("Destination", "unchanged");
        var activity = new TestActivity
        {
            Result = new(variable)
            {
                Converter = new(ConverterId)
            }
        };
        var context = await CreateContextAsync(
            activity,
            converter,
            new(ConverterId, typeof(int), typeof(string), "To text"));
        context.ExpressionExecutionContext.Memory.Declare(variable);

        context.Set(activity.Result, 42, nameof(TestActivity.Result));

        Assert.Equal("42", variable.Get(context.ExpressionExecutionContext));
        Assert.Equal(
            42,
            context.WorkflowExecutionContext.GetActivityOutputRegister()
                .FindOutputByActivityInstanceId(context.Id, nameof(TestActivity.Result)));
        Assert.Equal(1, converter.ConvertCalls);
    }

    [Fact]
    public async Task Set_WithConverter_WritesConvertedWorkflowOutputAndRecordsNativeOutput()
    {
        var converter = new RecordingConverter(context => context.Value.ToString()!);
        var activity = new TestActivity
        {
            Result = new(new Elsa.Expressions.Models.MemoryBlockReference("workflowResult"))
            {
                Converter = new(ConverterId)
            }
        };
        var context = await CreateContextAsync(
            activity,
            converter,
            new(ConverterId, typeof(int), typeof(string), "To text"));
        context.WorkflowExecutionContext.Workflow.Outputs.Add(new()
        {
            Name = "workflowResult",
            Type = typeof(string)
        });

        context.Set(activity.Result, 42, nameof(TestActivity.Result));

        Assert.Equal("42", context.WorkflowExecutionContext.Output["workflowResult"]);
        Assert.Equal(
            42,
            context.WorkflowExecutionContext.GetActivityOutputRegister()
                .FindOutputByActivityInstanceId(context.Id, nameof(TestActivity.Result)));
    }

    [Fact]
    public async Task Set_WhenConverterThrows_LeavesDestinationUnchangedAndRecordsNativeOutput()
    {
        var converter = new RecordingConverter(_ => throw new InvalidOperationException("conversion failed"));
        var variable = new Variable<string>("Destination", "unchanged");
        var activity = new TestActivity
        {
            Result = new(variable)
            {
                Converter = new(ConverterId)
            }
        };
        var context = await CreateContextAsync(
            activity,
            converter,
            new(ConverterId, typeof(int), typeof(string), "To text"));
        context.ExpressionExecutionContext.Memory.Declare(variable);

        var exception = Assert.Throws<OutputConversionException>(
            () => context.Set(activity.Result, 42, nameof(TestActivity.Result)));

        Assert.Equal(OutputConversionFailureStage.Invocation, exception.Stage);
        Assert.Equal("unchanged", variable.Get(context.ExpressionExecutionContext));
        Assert.Equal(
            42,
            context.WorkflowExecutionContext.GetActivityOutputRegister()
                .FindOutputByActivityInstanceId(context.Id, nameof(TestActivity.Result)));
    }

    [Fact]
    public async Task Set_WithConfiguredNull_BypassesConverterForNullableDestination()
    {
        var converter = new RecordingConverter(_ => "should not be called");
        var variable = new Variable<string?>("Destination", "unchanged");
        var activity = new NullableTestActivity
        {
            Result = new(variable)
            {
                Converter = new(ConverterId)
            }
        };
        var context = await CreateContextAsync(
            activity,
            converter,
            new(ConverterId, typeof(int?), typeof(string), "To text"));
        context.ExpressionExecutionContext.Memory.Declare(variable);

        context.Set(activity.Result, null, nameof(NullableTestActivity.Result));

        Assert.Null(variable.Get(context.ExpressionExecutionContext));
        Assert.Equal(0, converter.ConvertCalls);
        var record = Assert.Single(
            context.WorkflowExecutionContext.GetActivityOutputRegister()
                .FindMany(activity.Id, nameof(NullableTestActivity.Result)));
        Assert.Null(record.Value);
    }

    [Fact]
    public async Task Set_WithConfiguredNullForNonNullableDestination_LeavesDestinationUnchanged()
    {
        var converter = new RecordingConverter(_ => 0);
        var variable = new Variable<int>("Destination", 7);
        var activity = new NullableTestActivity
        {
            Result = new(variable)
            {
                Converter = new(ConverterId)
            }
        };
        var context = await CreateContextAsync(
            activity,
            converter,
            new(ConverterId, typeof(int?), typeof(int), "To integer"));
        context.ExpressionExecutionContext.Memory.Declare(variable);

        var exception = Assert.Throws<OutputConversionException>(
            () => context.Set(activity.Result, null, nameof(NullableTestActivity.Result)));

        Assert.Equal(OutputConversionFailureStage.ResultValidation, exception.Stage);
        Assert.Equal(7, variable.Get(context.ExpressionExecutionContext));
        Assert.Equal(0, converter.ConvertCalls);
    }

    [Fact]
    public async Task Set_WithoutConverter_UsesExistingPathWithoutConverterInfrastructureCalls()
    {
        var registry = Substitute.For<IOutputConverterRegistry>();
        var resolver = Substitute.For<IOutputBindingDestinationResolver>();
        var validator = Substitute.For<IOutputConverterSettingsValidator>();
        var invoker = Substitute.For<IOutputConverterInvoker>();
        var variable = new Variable<int>("Destination", 0);
        var activity = new TestActivity
        {
            Result = new(variable)
        };
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services =>
            {
                services.AddSingleton(registry);
                services.AddSingleton(resolver);
                services.AddSingleton(validator);
                services.AddSingleton(invoker);
            });
        var context = await fixture.BuildAsync();
        context.ExpressionExecutionContext.Memory.Declare(variable);

        context.Set(activity.Result, 42, nameof(TestActivity.Result));

        Assert.Equal(42, variable.Get(context.ExpressionExecutionContext));
        Assert.Empty(registry.ReceivedCalls());
        Assert.Empty(resolver.ReceivedCalls());
        Assert.Empty(validator.ReceivedCalls());
        Assert.Empty(invoker.ReceivedCalls());
    }

    private static async Task<ActivityExecutionContext> CreateContextAsync(
        IActivity activity,
        IOutputConverter converter,
        OutputConverterDescriptor descriptor)
    {
        var registry = new OutputConverterRegistry(
            [new OutputConverterRegistration(descriptor, descriptor.Id, ServiceLifetime.Scoped)]);
        var fixture = new ActivityTestFixture(activity)
            .ConfigureServices(services =>
            {
                services.AddKeyedSingleton(descriptor.Id, converter);
                services.AddSingleton<IOutputConverterRegistry>(registry);
                services.AddSingleton<IOutputBindingDestinationResolver, OutputBindingDestinationResolver>();
                services.AddSingleton<IOutputConverterSettingsValidator, OutputConverterSettingsValidator>();
                services.AddSingleton<IOutputConverterInvoker, OutputConverterInvoker>();
            });
        return await fixture.BuildAsync();
    }

    private sealed class TestActivity : CodeActivity
    {
        public Output<int> Result { get; set; } = new();

        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }

    private sealed class NullableTestActivity : CodeActivity
    {
        public Output<int?> Result { get; set; } = new();

        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }

    private sealed class RecordingConverter(Func<OutputConversionContext, object?> convert) : IOutputConverter
    {
        public int ConvertCalls { get; private set; }

        public object? Convert(OutputConversionContext context)
        {
            ConvertCalls++;
            return convert(context);
        }
    }
}
