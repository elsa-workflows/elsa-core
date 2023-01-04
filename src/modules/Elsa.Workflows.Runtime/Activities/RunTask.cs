using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Activities;

/// <summary>
/// Notifies the application that a task with a given name i requested to start.
/// When the application fulfilled the task, it is expected to report back to the workflow engine in order to resume the workflow. 
/// </summary>
[Activity("Elsa", "Primitives", "Requests a given task to be run. ", Kind = ActivityKind.Action)]
public class RunTask : ActivityBase<object>, IBookmarksPersistedHandler
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
    public Input<object?> TaskParams { get; set; } = default!;
    
    /// <inheritdoc />
    [JsonConstructor]
    public RunTask()
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
    protected override void Execute(ActivityExecutionContext context)
    {
        var taskName = TaskName.Get(context);
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var taskId = identityGenerator.GenerateId();
        var payload = new RunTaskBookmarkPayload(taskId);
        context.CreateBookmark(payload, ResumeAsync);
        context.TransientProperties[BookmarkPropertyKey] = payload;
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var input = context.GetInput<object>(InputKey);
        context.Set(Result, input);
        await context.CompleteActivityAsync();
    }

    async ValueTask IBookmarksPersistedHandler.BookmarksPersistedAsync(ActivityExecutionContext context)
    {
        var bookmark = (RunTaskBookmarkPayload)context.TransientProperties[BookmarkPropertyKey];
        var taskParams = TaskParams.TryGet(context);
        var taskName = TaskName.Get(context);
        var notification = new RunTaskRequest(bookmark.TaskId, taskName, taskParams);
        var dispatcher = context.GetRequiredService<ITaskDispatcher>();

        await dispatcher.DispatchAsync(notification, context.CancellationToken);
    }
}