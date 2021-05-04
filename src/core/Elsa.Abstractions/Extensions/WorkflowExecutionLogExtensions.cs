using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa
{
    public static class WorkflowExecutionLogExtensions
    {
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, WorkflowInstance workflowInstance, IActivityBlueprint activity, string? message, CancellationToken cancellationToken = default, [CallerLineNumber] int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(eventName, workflowInstance, activity, message, default, $"{Path.GetFileName(sourceFile)}:{line}", cancellationToken);

        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, ActivityExecutionContext activityExecutionContext, string? message, object? data, CancellationToken cancellationToken = default, [CallerLineNumber] int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(eventName, activityExecutionContext, message, data != null ? JObject.FromObject(data) : default, cancellationToken, line, sourceFile);
        
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, ActivityExecutionContext activityExecutionContext, string? message, JObject? data, CancellationToken cancellationToken = default, [CallerLineNumber] int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(eventName, activityExecutionContext.WorkflowInstance, activityExecutionContext.ActivityBlueprint, message, data, $"{Path.GetFileName(sourceFile)}:{line}", cancellationToken);

        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, ActivityExecutionContext activityExecutionContext, string? message, CancellationToken cancellationToken = default, [CallerLineNumber] int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(eventName, activityExecutionContext, message, default, cancellationToken, line, sourceFile);

        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, ActivityExecutionContext activityExecutionContext, object? data, CancellationToken cancellationToken = default, [CallerLineNumber] int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(eventName, activityExecutionContext, data != null ? JObject.FromObject(data) : default, cancellationToken, line, sourceFile);
        
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, ActivityExecutionContext activityExecutionContext, JObject? data, CancellationToken cancellationToken = default, [CallerLineNumber] int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(eventName, activityExecutionContext, default, data, cancellationToken, line, sourceFile);
        
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken = default, [CallerLineNumber] int line = default, [CallerFilePath] string? sourceFile = default) =>
            await log.AddEntryAsync(eventName, activityExecutionContext, default, default, cancellationToken, line, sourceFile);

        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, WorkflowInstance workflowInstance, IActivityBlueprint activity, string? message, object? data, string? source, CancellationToken cancellationToken = default) =>
            await log.AddEntryAsync(eventName, workflowInstance, activity, message, data != null ? JObject.FromObject(data) : default, source, cancellationToken);
        
        public static async Task AddEntryAsync(this IWorkflowExecutionLog log, string eventName, WorkflowInstance workflowInstance, IActivityBlueprint activity, string? message, JObject? data, string? source, CancellationToken cancellationToken = default)
        {
            var tenantId = workflowInstance.TenantId;
            var workflowInstanceId = workflowInstance.Id;
            var activityId = activity.Id;
            var activityType = activity.Type;
            source ??= activity.Source;
            await log.AddEntryAsync(workflowInstanceId, activityId, activityType, eventName, message, tenantId, source, data, cancellationToken);
        }
    }
}