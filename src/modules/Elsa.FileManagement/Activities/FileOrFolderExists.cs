using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

[Activity("File Management", "Checks if a file or folder exists at the specified path.",
    Kind = ActivityKind.Action
)]
public class FileOrFolderExists : CodeActivity<bool>
{
    [Input(Description = "The path to the file/folder to check.")]
    public Input<string> Path { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var exists = File.Exists(Path.Get(context)) || Directory.Exists(Path.Get(context));
        Result.Set(context, exists);
    }
}