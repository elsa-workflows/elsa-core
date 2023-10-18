using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Notifies the application that a task with a given name is requested to start.
/// When the application fulfilled the task, it is expected to report back to the workflow engine in order to resume the workflow. 
/// </summary>
[Activity("Elsa", "Primitives", "Requests a given task to be run. ", Kind = ActivityKind.Action)]
[PublicAPI]
public class RunTask : Activity<object>
{
    private static readonly object BookmarkPropertyKey = new();

    /// <summary>
    /// The key that is used for sending and receiving activity input.
    /// </summary>
    public const string InputKey = "RunTaskInput";

    /// <summary>
    /// The name of the task being requested.
    /// </summary>
    [Input(Description = "The name of the task being requested.")]
    public Input<string> TaskName { get; set; } = default!;

    /// <summary>
    /// The name of the task being requested.
    /// </summary>
    [Input(Description = "Any additional parameters to send to the task.")]
    public Input<IDictionary<string, object>?> Payload { get; set; } = default!;

    /// <inheritdoc />
    [JsonConstructor]
    private RunTask(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public RunTask(MemoryBlockReference output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(output, source, line)
    {
    }

    /// <inheritdoc />
    public RunTask(Output<object>? output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(output, source, line)
    {
    }

    /// <inheritdoc />
    public RunTask(string taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Literal<string>(taskName), source, line)
    {
    }

    /// <inheritdoc />
    public RunTask(Func<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new DelegateBlockReference<string>(taskName), source, line)
    {
    }

    /// <inheritdoc />
    public RunTask(Func<ExpressionExecutionContext, string?> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new DelegateBlockReference<string?>(taskName), source, line)
    {
    }

    /// <inheritdoc />
    public RunTask(Variable<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line) => TaskName = new Input<string>(taskName);

    /// <inheritdoc />
    public RunTask(Literal<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line) => TaskName = new Input<string>(taskName);


    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Create bookmark.
        var taskName = TaskName.Get(context);
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var taskId = identityGenerator.GenerateId();
        var payload = new RunTaskBookmarkPayload(taskId, taskName);
        context.CreateBookmark(payload, ResumeAsync, includeActivityInstanceId: false);
        
        // Dispatch task request.
        var taskParams = Payload.GetOrDefault(context);
        var runTaskRequest = new RunTaskRequest(context, taskId, taskName, taskParams);
        var dispatcher = context.GetRequiredService<ITaskDispatcher>();

        await dispatcher.DispatchAsync(runTaskRequest, context.CancellationToken);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var input = context.GetWorkflowInput<object>(InputKey);
        context.Set(Result, input);
        await context.CompleteActivityAsync();
    }
}