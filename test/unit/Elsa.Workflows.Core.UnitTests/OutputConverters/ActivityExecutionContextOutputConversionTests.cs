using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class ActivityExecutionContextOutputConversionTests
{
    private const string ConverterId = "tests.to-text";

    [Test]
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

        await Assert.That(variable.Get(context.ExpressionExecutionContext)).IsEqualTo("42");
        await Assert.That(context.WorkflowExecutionContext.GetActivityOutputRegister()
                .FindOutputByActivityInstanceId(context.Id, nameof(TestActivity.Result))).IsEqualTo(42);
        await Assert.That(converter.ConvertCalls).IsEqualTo(1);
    }

    [Test]
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

        await Assert.That(context.WorkflowExecutionContext.Output["workflowResult"]).IsEqualTo("42");
        await Assert.That(context.WorkflowExecutionContext.GetActivityOutputRegister()
                .FindOutputByActivityInstanceId(context.Id, nameof(TestActivity.Result))).IsEqualTo(42);
    }

    [Test]
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

        var exception = Assert.ThrowsExactly<OutputConversionException>(
            () => context.Set(activity.Result, 42, nameof(TestActivity.Result)));

        await Assert.That(exception.Stage).IsEqualTo(OutputConversionFailureStage.Invocation);
        await Assert.That(variable.Get(context.ExpressionExecutionContext)).IsEqualTo("unchanged");
        await Assert.That(context.WorkflowExecutionContext.GetActivityOutputRegister()
                .FindOutputByActivityInstanceId(context.Id, nameof(TestActivity.Result))).IsEqualTo(42);
    }

    [Test]
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

        await Assert.That(variable.Get(context.ExpressionExecutionContext)).IsNull();
        await Assert.That(converter.ConvertCalls).IsEqualTo(0);
        var record = await Assert.That(context.WorkflowExecutionContext.GetActivityOutputRegister()
                .FindMany(activity.Id, nameof(NullableTestActivity.Result))).HasSingleItem();
        await Assert.That(record.Value).IsNull();
    }

    [Test]
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

        var exception = Assert.ThrowsExactly<OutputConversionException>(
            () => context.Set(activity.Result, null, nameof(NullableTestActivity.Result)));

        await Assert.That(exception.Stage).IsEqualTo(OutputConversionFailureStage.ResultValidation);
        await Assert.That(variable.Get(context.ExpressionExecutionContext)).IsEqualTo(7);
        await Assert.That(converter.ConvertCalls).IsEqualTo(0);
    }

    [Test]
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

        await Assert.That(variable.Get(context.ExpressionExecutionContext)).IsEqualTo(42);
        await Assert.That(registry.ReceivedCalls()).IsEmpty();
        await Assert.That(resolver.ReceivedCalls()).IsEmpty();
        await Assert.That(validator.ReceivedCalls()).IsEmpty();
        await Assert.That(invoker.ReceivedCalls()).IsEmpty();
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