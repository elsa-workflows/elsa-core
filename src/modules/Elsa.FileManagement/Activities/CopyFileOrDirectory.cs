using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

[Activity("File Management", "Copies a file or directory to the specified path.",
    Kind = ActivityKind.Action
)]
public class CopyFileOrDirectory : CodeActivity<bool>
{
    [Input(Description = "The source path to the file/folder to copy.")]
    public Input<string> SourcePath { get; set; } = default!;

    [Input(Description = "The destination path to the file/folder to copy.")]
    public Input<string> DestinationPath { get; set; } = default!;

    [Input(Description = "Whether to overwrite the destination file/folder if it already exists.")]
    public Input<bool> OverWrite { get; set; } = default!;

    [Input(Description = "Whether to copy the source directory recursively.")]
    public Input<bool> Recursive { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (File.Exists(SourcePath.Get(context)))
        {
            File.Copy(SourcePath.Get(context), DestinationPath.Get(context), OverWrite.Get(context));
            Result.Set(context, true);
        }
        else if (Directory.Exists(SourcePath.Get(context)))
        {
            if (!Directory.Exists(DestinationPath.Get(context)))
                Directory.CreateDirectory(DestinationPath.Get(context));
            foreach (var file in Directory.GetFiles(SourcePath.Get(context)))
            {
                File.Copy(file, Path.Combine(DestinationPath.Get(context), Path.GetFileName(file)), OverWrite.Get(context));
            }
            if (Recursive.Get(context))
            {
                var allDirectories = Directory.GetDirectories(SourcePath.Get(context), "*", SearchOption.AllDirectories);
                foreach (var directory in allDirectories)
                {
                    var destinationDirectory = Path.Combine(DestinationPath.Get(context), directory.Substring(SourcePath.Get(context).Length + 1));
                    if (!Directory.Exists(destinationDirectory))
                        Directory.CreateDirectory(destinationDirectory);
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), OverWrite.Get(context));
                    }
                }
            }
            Result.Set(context, true);
        }
        else
        {
            Result.Set(context, false);
        }
    }
}