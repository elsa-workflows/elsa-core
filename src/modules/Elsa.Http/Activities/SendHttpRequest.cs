using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Http;

/// <summary>
/// Send an HTTP request.
/// </summary>
[Activity("Elsa", "HTTP", "Send an HTTP request.", DisplayName = "HTTP Request", Kind = ActivityKind.Task)]
[PublicAPI]
public class SendHttpRequest : SendHttpRequestBase
{
    /// <inheritdoc />
    public SendHttpRequest([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// A list of expected status codes to handle and the corresponding activity to execute when the status code matches.
    /// </summary>
    [Input(
        Description = "A list of expected status codes to handle and the corresponding activity to execute when the status code matches.",
        UIHint = "http-status-codes"
    )]
    public ICollection<HttpStatusCodeCase> ExpectedStatusCodes { get; set; } = new List<HttpStatusCodeCase>();

    /// <summary>
    /// The activity to execute when the HTTP status code does not match any of the expected status codes.
    /// </summary>
    [Port]
    public IActivity? UnmatchedStatusCode { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleResponseAsync(ActivityExecutionContext context, HttpResponseMessage response)
    {
        var expectedStatusCodes = ExpectedStatusCodes;
        var statusCode = (int)response.StatusCode;
        var matchingCase = expectedStatusCodes.FirstOrDefault(x => x.StatusCode == statusCode);
        var activity = matchingCase?.Activity ?? UnmatchedStatusCode;

        await context.ScheduleActivityAsync(activity, OnChildActivityCompletedAsync);
    }

    private async ValueTask OnChildActivityCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await context.CompleteActivityAsync();
    }
}