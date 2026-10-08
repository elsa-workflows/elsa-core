using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.Activities.Files;

/// <summary>
/// Uploads a file to Slack.
/// </summary>
[Activity(
    "Elsa.Slack.Files",
    "Slack Files",
    "Uploads a file to Slack.",
    DisplayName = "Upload File")]
[UsedImplicitly]
public class UploadFile : SlackActivity
{
    /// <summary>
    /// The file to upload.
    /// </summary>
    [Input(Description = "The file to upload.")]
    public Input<FileUpload> File { get; set; } = null!;

    /// <summary>
    /// The channel ID where the file will be shared. If not specified the file will be private.
    /// </summary>
    [Input(Name = "Channel Id", Description = "Channel ID where the file will be shared. If not specified the file will be private.")]
    public Input<string>? ChannelId { get; set; }

    /// <summary>
    /// Provide another message's timestamp value to upload this file as a reply. Never use a reply's ts value; use its parent instead.
    /// </summary>
    [Input(Name = "Thread Timestamp", Description = "Provide another message's timestamp value to upload this file as a reply. Never use a reply's ts value; use its parent instead.")]
    public Input<string>? ThreadTs { get; set; }

    /// <summary>
    /// The message text introducing the file in specified channels.
    /// </summary>
    [Input(Name = "Initial Comment", Description = "The message text introducing the file in specified channels.")]
    public Input<string>? InitialComment { get; set; }

    /// <summary>
    /// The reference to the uploaded file.
    /// </summary>
    [Output(Name = "File Reference", Description = "The reference to the uploaded file.")]
    public Output<ExternalFileReference> FileReference { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        FileUpload file = context.Get(File)!;
        string? channelId = context.Get(ChannelId);
        string? threadTs = context.Get(ThreadTs);
        string? initialComment = context.Get(InitialComment);

        ISlackApiClient client = GetClient(context);
        ExternalFileReference fileReference = await client.Files.Upload(
            file,
            channelId,
            threadTs,
            initialComment,
            context.CancellationToken);

        context.Set(FileReference, fileReference);
    }
}