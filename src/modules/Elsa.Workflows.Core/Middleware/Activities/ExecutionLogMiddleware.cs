using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;

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
        context.AddExecutionLogEntry("Started", includeActivityState: true);

        try
        {
            await _next(context);
            
            var payload = new JsonObject();

            foreach (var entry in context.JournalData)
            {
                payload[entry.Key] = entry.Value != null ? JsonSerializer.Deserialize<JsonNode>(JsonSerializer.Serialize(entry.Value)) : JsonNode.Parse("null");
            }

            context.AddExecutionLogEntry("Completed", payload: payload, includeActivityState: true);
        }
        catch (Exception exception)
        {
            context.AddExecutionLogEntry("Faulted",
                includeActivityState: true,
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