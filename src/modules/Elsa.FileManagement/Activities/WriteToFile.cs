using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

[Activity("File Management", "Write the content to the file.",
    Kind = ActivityKind.Action
)]
public class WriteToFile : CodeActivity<bool>
{
    [Input(Description = "The path to the file to write.")]
    public Input<string> Path { get; set; } = default!;

    [Input(Description = "The content to write to the file.")]
    public string Content { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        File.WriteAllText(Path.Get(context), Content);
        Result.Set(context, true);
    }
}