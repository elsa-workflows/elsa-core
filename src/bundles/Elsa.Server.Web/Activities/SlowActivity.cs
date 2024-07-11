using System.Reflection;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Server.Web.Activities;

/// <summary>
/// Simulate an activity that takes a long time to complete.
/// </summary>
[Activity("Testing", "Testing", "Simulate an activity that takes a long time to complete.")]
public class SlowActivity : CodeActivity, IActivityPropertyDefaultValueProvider
{
    /// <summary>
    /// The delay.
    /// </summary>
    [Input(Description = "The delay.", DefaultValueProvider = typeof(SlowActivity))]
    public Input<TimeSpan> Delay { get; set; } = new(TimeSpan.FromSeconds(1));

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var delay = Delay.Get(context);
        await Task.Delay(delay, context.CancellationToken);
    }

    object IActivityPropertyDefaultValueProvider.GetDefaultValue(PropertyInfo property)
    {
        return property.Name switch
        {
            nameof(Delay) => TimeSpan.FromSeconds(1),
            _ => default
        };
    }
}