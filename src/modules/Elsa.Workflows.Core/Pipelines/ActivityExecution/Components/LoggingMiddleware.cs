using System.Diagnostics;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core.Pipelines.ActivityExecution.Components;

public class LoggingMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch;

    public LoggingMiddleware(ActivityMiddlewareDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _stopwatch = new Stopwatch();
    }

    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        var activity = context.Activity;
        _logger.LogDebug("Executing activity {ActivityType}", activity.Type);
        _stopwatch.Restart();
        await _next(context);
        _stopwatch.Stop();
        _logger.LogDebug("Executed activity {ActivityType} in {Elapsed}", activity.Type, _stopwatch.Elapsed);
    }
}

public static class LoggingMiddlewareExtensions
{
    public static IActivityExecutionBuilder UseLogging(this IActivityExecutionBuilder builder) => builder.UseMiddleware<LoggingMiddleware>();
}