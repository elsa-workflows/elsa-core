using Elsa.Contracts;
using Elsa.Modules.Activities.Console;

namespace Elsa.Samples.Console1.Workflows;

public static class CustomizedActivityWorkflow
{
    public static IActivity Create() => new CustomWriteLine();
}

public class CustomWriteLine : WriteLine
{
    public CustomWriteLine() : base("Hello Custom World!")
    {
    }
}