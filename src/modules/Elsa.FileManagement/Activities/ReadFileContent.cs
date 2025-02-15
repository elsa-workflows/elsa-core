using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

[Activity("File Management", "Reads the content of a file.",
    Kind = ActivityKind.Action
)]
public class ReadFileContent : CodeActivity<string>
{
    [Input(Description = "The path to the file to read.")]
    public Input<string> Path { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (File.Exists(Path.Get(context)))
        {
            var content = File.ReadAllText(Path.Get(context));
            Result.Set(context, content);
        }
        else
        {
            Result.Set(context, "");
        }
    }
}