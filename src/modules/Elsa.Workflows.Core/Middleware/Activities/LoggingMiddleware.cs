using System.Diagnostics;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core.Middleware.Activities;

/// <summary>
/// An activity execution middleware component that logs information about the activity being executed.
/// </summary>
public class LoggingMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LoggingMiddleware(ActivityMiddlewareDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _stopwatch = new Stopwatch();
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        _logger.LogInformation("Executing activity {ActivityId}", activity.Id);
        _stopwatch.Restart();
        await _next(context);
        _stopwatch.Stop();
        _logger.LogInformation("Executed activity {ActivityId} in {Elapsed}", activity.Id, _stopwatch.Elapsed);
    }
}

/// <summary>
/// Extends <see cref="IActivityExecutionPipelineBuilder"/> to install the <see cref="LoggingMiddleware"/> component.
/// </summary>
public static class LoggingMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="LoggingMiddleware"/> component.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseLogging(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<LoggingMiddleware>();
}