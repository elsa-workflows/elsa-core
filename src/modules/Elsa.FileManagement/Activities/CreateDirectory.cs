using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

[Activity("File Management", "Creates a directory at the specified path.",
    Kind = ActivityKind.Action
)]
public class CreateDirectory : CodeActivity<bool>
{
    [Input(Description = "The path to the directory to create.")]
    public Input<string> Path { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if(Directory.Exists(Path.Get(context)))
        {
            Result.Set(context, true);
        }
        else
        {
            Directory.CreateDirectory(Path.Get(context));
            Result.Set(context, true);
        }
    }
}