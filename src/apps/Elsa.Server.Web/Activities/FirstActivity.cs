using Elsa.Workflows;

namespace ElsaServer.Activities
{
    public class FirstActivity : CodeActivity
    {
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            Console.WriteLine("First Activity");
            await context.CompleteActivityAsync();
        }
    }
}
