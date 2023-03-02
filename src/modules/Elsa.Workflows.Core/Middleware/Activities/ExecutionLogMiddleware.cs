using System.Diagnostics;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Workflows.Core.Middleware.Activities;

/// <summary>
/// Adds extension methods to <see cref="ExecutionLogMiddleware"/>.
/// </summary>
public static class ExecutionLogMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="ExecutionLogMiddleware"/> component in the activity execution pipeline.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseExecutionLogging(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ExecutionLogMiddleware>();
}

/// <summary>
/// An activity execution middleware component that extracts execution details as <see cref="WorkflowExecutionLogEntry"/>.
/// </summary>
public class ExecutionLogMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ExecutionLogMiddleware(ActivityMiddlewareDelegate next)
    {
        _next = next;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        context.AddExecutionLogEntry("Started");

        try
        {
            await _next(context);
            
            var payload = new JObject();

            foreach (var entry in context.JournalData)
                payload[entry.Key] = entry.Value != null ? JToken.FromObject(entry.Value, JsonSerializer.CreateDefault()) : JValue.CreateNull();
            
            context.AddExecutionLogEntry("Completed", payload: payload);
        }
        catch (Exception exception)
        {
            context.AddExecutionLogEntry("Faulted",
                payload: new
                {
                    Exception = new
                    {
                        exception.Message,
                        exception.Source,
                        exception.Data,
                        Type = exception.GetType()
                    }
                });

            throw;
        }
    }
}