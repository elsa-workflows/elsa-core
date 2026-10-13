using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.Activities;

/// <summary>
/// Creates a workflow-bound human task and suspends the workflow until a governed outcome resumes it.
/// </summary>
[Activity("Elsa", "User Tasks", "Creates a user task and waits for a human outcome.", Kind = ActivityKind.Task)]
public sealed class UserTask : Activity<UserTaskResult>
{
    public const string InputKey = "UserTaskInput";

    [Input(Description = "The task title.")]
    public Input<string> Title { get; set; } = null!;
    [Input] public Input<string?> Summary { get; set; } = null!;
    [Input] public Input<string?> Reference { get; set; } = null!;
    [Input] public Input<IReadOnlyCollection<string>?> Tags { get; set; } = null!;
    [Input] public Input<string?> TaskType { get; set; } = null!;
    [Input] public Input<ParticipantReference?> Requester { get; set; } = null!;
    [Input] public Input<ParticipantReference?> Assignee { get; set; } = null!;
    [Input] public Input<IReadOnlyCollection<ParticipantReference>?> CandidateUsers { get; set; } = null!;
    [Input] public Input<IReadOnlyCollection<ParticipantReference>?> CandidateGroups { get; set; } = null!;
    [Input] public Input<IReadOnlyCollection<ParticipantReference>?> ExcludedUsers { get; set; } = null!;
    [Input] public Input<UserTaskMembershipResolutionMode> MembershipResolutionMode { get; set; } = new(new Literal<UserTaskMembershipResolutionMode>(UserTaskMembershipResolutionMode.Live));
    [Input] public Input<bool> AllowManagerExclusionOverride { get; set; } = null!;
    [Input] public Input<int> Priority { get; set; } = new(new Literal<int>(50));
    [Input] public Input<DateTimeOffset?> DueAt { get; set; } = null!;
    [Input] public Input<string?> Instructions { get; set; } = null!;
    [Input] public Input<JsonElement?> TaskData { get; set; } = null!;
    [Input] public Input<UserTaskFormReference?> Form { get; set; } = null!;
    [Input(Description = "Completion actions. Keys are literal; labels may use expressions.", DefaultSyntax = "Literal", SupportedSyntaxes = ["Literal"])]
    public Input<IReadOnlyCollection<UserTaskActionDefinition>?> ActionDefinitions { get; set; } = null!;
    [Input] public Input<IReadOnlyCollection<UserTaskInvitationDefinition>?> Invitations { get; set; } = null!;
    [Input] public Input<bool> EnableTimeoutOutcome { get; set; } = null!;
    [Input] public Input<bool> EnableCancellationOutcome { get; set; } = null!;
    [Output] public Output<string> TaskId { get; set; } = null!;

    public UserTask()
    {
    }

    public UserTask(string title, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        Title = new Input<string>(new Literal<string>(title));
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var taskId = identityGenerator.GenerateId();
        var bookmarkId = identityGenerator.GenerateId();
        var options = context.GetService<IOptions<UserTasksOptions>>()?.Value ?? new UserTasksOptions();
        var definition = new UserTaskDefinitionSnapshot
        {
            Title = Title.Get(context),
            Summary = Summary.GetOrDefault(context),
            Reference = Reference.GetOrDefault(context),
            Tags = Tags.GetOrDefault(context) ?? [],
            TaskType = TaskType.GetOrDefault(context),
            Requester = Requester.GetOrDefault(context),
            Assignee = Assignee.GetOrDefault(context),
            CandidateUsers = CandidateUsers.GetOrDefault(context) ?? [],
            CandidateGroups = CandidateGroups.GetOrDefault(context) ?? [],
            ExcludedUsers = ExcludedUsers.GetOrDefault(context) ?? [],
            MembershipResolutionMode = MembershipResolutionMode.GetOrDefault(context),
            AllowManagerExclusionOverride = AllowManagerExclusionOverride.GetOrDefault(context),
            Priority = Priority.GetOrDefault(context),
            DueAt = DueAt.GetOrDefault(context),
            Instructions = Instructions.GetOrDefault(context),
            TaskData = TaskData.GetOrDefault(context),
            FormReference = Form.GetOrDefault(context),
            Actions = (ActionDefinitions.GetOrDefault(context) ?? []).Select(x => x.Materialize(context)).ToArray(),
            Invitations = Invitations.GetOrDefault(context) ?? [],
            EnableTimeoutOutcome = EnableTimeoutOutcome.GetOrDefault(context),
            EnableCancellationOutcome = EnableCancellationOutcome.GetOrDefault(context)
        };
        var workflow = context.WorkflowExecutionContext.Workflow;
        var materialization = new UserTaskMaterialization(
            // The workflow's own tenant wins; the configured default is only a fallback for single-tenant hosts.
            workflow.Identity.TenantId ?? options.DefaultTenantId,
            workflow.Identity.DefinitionId,
            context.WorkflowExecutionContext.Id,
            context.Id,
            bookmarkId,
            definition,
            [],
            [],
            context.WorkflowExecutionContext.SystemClock.UtcNow,
            taskId,
            workflow.WorkflowMetadata.Name,
            workflow.Identity.Version,
            context.WorkflowExecutionContext.CorrelationId);
        context.CreateBookmark(new CreateBookmarkArgs
        {
            BookmarkId = bookmarkId,
            BookmarkName = nameof(UserTask),
            Stimulus = materialization,
            Callback = ResumeAsync,
            IncludeActivityInstanceId = false
        });
        TaskId.Set(context, taskId);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var stimulus = context.GetWorkflowInput<UserTaskStimulus>(InputKey);
        Result?.Set(context, new UserTaskResult(stimulus.ActionKey, stimulus.CompletionData, stimulus.CompletedBy, stimulus.CompletedAt ?? context.WorkflowExecutionContext.SystemClock.UtcNow));
        await context.CompleteActivityAsync();
    }
}

/// <summary>
/// Design-time action definition. Key is deliberately a plain literal so workflow expressions cannot
/// change the contract of an already activated task; only the label is expression-capable.
/// </summary>
public sealed class UserTaskActionDefinition
{
    public string Key { get; set; } = "";
    /// <summary>
    /// The label. In JSON (e.g. Studio-designed workflows) this is a plain string; when omitted, the key is used.
    /// </summary>
    [JsonConverter(typeof(UserTaskActionLabelJsonConverter))]
    public Input<string> Label { get; set; } = null!;
    public IReadOnlyDictionary<string, object?>? Metadata { get; set; }

    public UserTaskActionDefinition()
    {
    }

    public UserTaskActionDefinition(string key, string label)
    {
        Key = key;
        Label = new Input<string>(new Literal<string>(label));
    }

    internal UserTaskAction Materialize(ActivityExecutionContext context) => new(Key, ResolveLabel(context), Metadata);

    internal string ResolveLabel(ActivityExecutionContext context)
    {
        var label = Label switch
        {
            null => null,
            { Expression.Type: "Literal" } => Label.Expression.Value?.ToString(),
            _ => Label.Get(context)
        };
        return string.IsNullOrWhiteSpace(label) ? Key : label;
    }
}

/// <summary>
/// Reads an action label from a plain JSON string (or null) and writes literal labels back as plain strings,
/// so action definitions round-trip through JSON workflow definitions.
/// </summary>
internal sealed class UserTaskActionLabelJsonConverter : JsonConverter<Input<string>>
{
    public override Input<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
    {
        JsonTokenType.Null => null,
        JsonTokenType.String => new Input<string>(reader.GetString()!),
        _ => throw new JsonException("A User Task action label must be a string.")
    };

    public override void Write(Utf8JsonWriter writer, Input<string> value, JsonSerializerOptions options)
    {
        // Expression labels are code-first only and cannot be represented as a plain string; they are written as null (the key is then used as the label).
        if (value.Expression?.Type == "Literal")
            writer.WriteStringValue(value.Expression.Value?.ToString());
        else
            writer.WriteNullValue();
    }
}
