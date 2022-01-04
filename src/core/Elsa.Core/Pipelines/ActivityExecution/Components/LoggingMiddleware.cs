using System.Diagnostics;
using Elsa.Contracts;
using Elsa.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Pipelines.ActivityExecution.Components;

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
        var node = context.ActivityNode;
        _logger.LogDebug("Executing node {Node}", node.GetType().Name);
        _stopwatch.Restart();
        await _next(context);
        _stopwatch.Stop();
        _logger.LogDebug("Executed node {Node} in {Elapsed}", node.GetType().Name, _stopwatch.Elapsed);
    }
}

public static class LoggingMiddlewareExtensions
{
    public static IActivityExecutionBuilder UseLogging(this IActivityExecutionBuilder builder) => builder.UseMiddleware<LoggingMiddleware>();
}