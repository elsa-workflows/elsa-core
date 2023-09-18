using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Options;

namespace Elsa.Http;

/// <summary>
/// Send an HTTP request.
/// </summary>
[Activity("Elsa", "HTTP", "Send an HTTP request.", DisplayName = "HTTP Request (flow)", Kind = ActivityKind.Task)]
public class FlowSendHttpRequest : SendHttpRequestBase
{
    /// <inheritdoc />
    public FlowSendHttpRequest([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// A list of expected status codes to handle.
    /// </summary>
    [Input(Description = "A list of expected status codes to handle.", UIHint = InputUIHints.DynamicOutcomes, OptionsMethod = nameof(GetExpectedStatusCodesOptionsAsync))]
    public Input<ICollection<int>> ExpectedStatusCodes { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask HandleResponseAsync(ActivityExecutionContext context, HttpResponseMessage response)
    {
        var expectedStatusCodes = ExpectedStatusCodes.GetOrDefault(context) ?? new List<int>(0);
        var statusCode = (int)response.StatusCode;
        var hasMatchingStatusCode = expectedStatusCodes.Contains(statusCode);
        var outcome = expectedStatusCodes.Any() ? hasMatchingStatusCode ? statusCode.ToString() : "Unmatched status code" : "Done";

        await context.CompleteActivityWithOutcomesAsync(outcome);
    }

    /// <inheritdoc />
    protected override async ValueTask HandleRequestExceptionAsync(ActivityExecutionContext context, HttpRequestException exception)
    {
        await context.CompleteActivityWithOutcomesAsync("Failed to connect");
    }

    /// <inheritdoc />
    protected override async ValueTask HandleTaskCanceledExceptionAsync(ActivityExecutionContext context, TaskCanceledException exception)
    {
        await context.CompleteActivityWithOutcomesAsync("Timeout");
    }

    private static ValueTask<IDictionary<string, object>> GetExpectedStatusCodesOptionsAsync(PropertyInfo property, CancellationToken cancellationToken = default)
    {
        var options = new Dictionary<string, object>
        {
            [nameof(DynamicOutcomesOptions)] = new DynamicOutcomesOptions(new[]{"Unmatched status code", "Failed to connect", "Timeout", "Done"})
        };
        
        return new(options);
    }
}