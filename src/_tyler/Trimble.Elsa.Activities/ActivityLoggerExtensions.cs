using Elsa.Workflows;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Trimble.Elsa.Activities.Config;

namespace Trimble.Elsa.Activities;

internal static class ActivityLoggerExtensions
{
    /// <summary>
    /// Logs a Critical Fault message and throws an exception, if provided, to 
    /// be captured by the workflow environment as an incident.
    /// </summary>
    public static void LogCritical<T>(
        this ActivityExecutionContext context,
        string message,
        object? payload = null,
        Exception? exception = null)
    {
        if (ConfigurationExtensions.ActivityHostLoggingEnabled)
        {
            var logger = context.GetService<ILogger<T>>();
            logger?.LogCritical(
                exception,
                message + " {@data}",
                ExecutionData(context, payload));
        }   

        context.AddExecutionLogEntry(
            eventName: "Critical",
            message:   message,
            payload:   payload);

        throw new InvalidOperationException($"{message} Check log files.", exception);
    }

    /// <summary>
    /// Logs a Debug message to the configured Microsoft logger and to the 
    /// Elsa Activity execution journal.
    /// </summary>
    public static void LogDebug<T>(
        this ActivityExecutionContext context,
        string message,
        object? payload = null)
    {
        if (ConfigurationExtensions.ActivityHostLoggingEnabled)
        {
            var logger = context.GetService<ILogger<T>>();
            logger?.LogDebug(
                message + " {@data}",
                ExecutionData(context, payload));
        }

        context.AddExecutionLogEntry(
            eventName: "Debug",
            source:    context.Activity.GetType().Name,
            message:   message,
            payload:   payload);
    }

    /// <summary>
    /// Logs an Error message, including the exception if provided. It does not 
    /// automatically throw the exception to be captured by the workflow 
    /// environment as an incident. This would need to be done by the caller if desired.
    /// </summary>
    public static void LogError<T>(
        this ActivityExecutionContext context,
        string message,
        object? payload = null,
        Exception? exception = null)
    {
        if (ConfigurationExtensions.ActivityHostLoggingEnabled)
        {
            var logger = context.GetService<ILogger<T>>();
            logger?.LogError(
                exception,
                message + "{@data}",
                ExecutionData(context, payload));
        }

        context.AddExecutionLogEntry(
            eventName: "Error",
            message:   message,
            payload:   payload);
    }

    /// <summary>
    /// Logs an Info message to the configured Microsoft logger and to the 
    /// Elsa Activity execution journal.
    /// </summary>
    public static void LogInfo<T>(
        this ActivityExecutionContext context,
        string message,
        object? payload = null)
    {
        if (ConfigurationExtensions.ActivityHostLoggingEnabled)
        {
            var logger = context.GetService<ILogger<T>>();
            logger?.LogInformation(
                message + " {@data}",
                ExecutionData(context, payload));
        }

        context.AddExecutionLogEntry(
            eventName: "Info",
            message:   message,
            payload:   payload);
    }

    private static object ExecutionData(
        ActivityExecutionContext context,
        object? payload = null)
    {
        // Include workflow execution data that may be helpful for debugging
        // e.g. Entitlement and CDH Account ID
        string input = JsonSerializer.Serialize(context?.WorkflowExecutionContext?.Input);

        string? workflowExecutionContextId = context?.WorkflowExecutionContext?.Id;
        string? workflowExecutionId = context?.WorkflowExecutionContext?.ParentWorkflowInstanceId;
        string? correlationId = context?.WorkflowExecutionContext?.CorrelationId;

        return new
        {
            CorrelationId       = correlationId,
            Input               = input,
            ExecutionContextId  = workflowExecutionContextId,
            WorkflowExecutionId = workflowExecutionId,
            Payload             = payload
        };
    }
}
