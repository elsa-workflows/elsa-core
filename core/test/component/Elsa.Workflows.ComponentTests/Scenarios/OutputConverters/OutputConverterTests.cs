using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.OutputConverters;

public class OutputConverterTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task ConfiguredConverter_WritesConvertedValueToVariableAndRetainsNativeActivityOutput()
    {
        var builder = Scope.ServiceProvider.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder();
        var destination = builder.WithVariable<string>("ConvertedValue", "unchanged").WithWorkflowStorage();
        var observed = builder.WithOutput<string>("ObservedValue");
        var activity = new NativeStringActivity
        {
            Result = new(destination)
            {
                Converter = new(TestOutputConverter.Descriptor.Id)
            }
        };
        builder.Root = new Sequence
        {
            Activities =
            [
                activity,
                new Inline<string>(
                    context => destination.Get(context.ExpressionExecutionContext)!,
                    new MemoryBlockReference(observed.Name))
            ]
        };
        var workflow = await builder.BuildWorkflowAsync();

        var result = await Scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>().InvokeAsync(workflow);

        Assert.Equal("converted:native value", result.WorkflowExecutionContext.Output[observed.Name]);
        Assert.Equal("native value", result.WorkflowExecutionContext.GetActivityOutputRegister().FindOutputByActivityId(activity.Id));
    }

    [Fact]
    public async Task ConfiguredConverter_WritesConvertedValueToWorkflowOutputAndRetainsNativeActivityOutput()
    {
        var builder = Scope.ServiceProvider.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder();
        var destination = builder.WithOutput<string>("ConvertedOutput");
        var activity = new NativeStringActivity
        {
            Result = new(new MemoryBlockReference(destination.Name))
            {
                Converter = new(TestOutputConverter.Descriptor.Id)
            }
        };
        builder.Root = activity;
        var workflow = await builder.BuildWorkflowAsync();

        var result = await Scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>().InvokeAsync(workflow);

        Assert.Equal("converted:native value", result.WorkflowExecutionContext.Output[destination.Name]);
        Assert.Equal("native value", result.WorkflowExecutionContext.GetActivityOutputRegister().FindOutputByActivityId(activity.Id));
    }

    [Fact]
    public async Task MissingConverter_FaultsWithSafeMetadataAndLeavesDestinationUnchanged()
    {
        var builder = Scope.ServiceProvider.GetRequiredService<IWorkflowBuilderFactory>().CreateBuilder();
        builder.WorkflowOptions.IncidentStrategyType = typeof(FaultStrategy);
        var destination = builder.WithVariable<string>("ConvertedValue", "unchanged").WithWorkflowStorage();
        var activity = new NativeStringActivity
        {
            Result = new(destination)
            {
                Converter = new("tests.component.missing")
            }
        };
        builder.Root = activity;
        var workflow = await builder.BuildWorkflowAsync();

        var result = await Scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>().InvokeAsync(workflow);

        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowExecutionContext.SubStatus);
        Assert.Equal("native value", result.WorkflowExecutionContext.GetActivityOutputRegister().FindOutputByActivityId(activity.Id));
        var incident = Assert.Single(result.WorkflowExecutionContext.Incidents);
        Assert.Equal(typeof(OutputConversionException), incident.Exception!.Type);
        Assert.Equal("tests.component.missing", incident.Exception.Metadata![nameof(OutputConversionException.ConverterId)]);
        Assert.Equal("Resolution", incident.Exception.Metadata[nameof(OutputConversionException.Stage)]);
        Assert.DoesNotContain("native value", incident.Message);
    }

    private sealed class NativeStringActivity : CodeActivity<string>
    {
        protected override void Execute(ActivityExecutionContext context) => Result!.Set(context, "native value");
    }
}

public sealed class TestOutputConverter : IOutputConverter
{
    public static OutputConverterDescriptor Descriptor { get; } = new(
        "tests.component.to-text",
        typeof(string),
        typeof(string),
        "Component test converter");

    public object? Convert(OutputConversionContext context) => $"converted:{context.Value}";
}
