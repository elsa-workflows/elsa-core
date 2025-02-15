using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

[Activity("File Management", "Deletes a file or directory at the specified path.",
    Kind = ActivityKind.Action
)]
public class DeleteFileOrDirectory : CodeActivity<bool>
{
    [Input(Description = "The path to the file/folder to delete.")]
    public Input<string> Path { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (File.Exists(Path.Get(context)))
        {
            File.Delete(Path.Get(context));
            Result.Set(context, true);
        }
        else if (Directory.Exists(Path.Get(context)))
        {
            Directory.Delete(Path.Get(context), true);
            Result.Set(context, true);
        }
        else
        {
            Result.Set(context, false);
        }
    }
}