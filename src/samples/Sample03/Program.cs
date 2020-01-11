using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample03
{
    /// <summary>
    /// A sequential workflow with if/else branching.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .BuildServiceProvider();

            // Get the scheduler.
            var scheduler = services.GetRequiredService<IScheduler>();

            // Create a sequence activity.
            // var sequence = new Sequence(
            //     new Inline(() => WriteLine("What's your age?")),
            //     new SetVariable("Age", new CodeExpression<int>(() => int.Parse(ReadLine()))),
            //     new IfElse(
            //         (w, a) => w.GetVariable<int>("Age") > 21, 
            //         () => WriteLine("Here's your drink."),
            //         () => WriteLine("Here's your soda.")),
            //     new Inline(() => WriteLine("Enjoy!"))
            // );
            //
            // // Schedule an activity for execution.
            // await scheduler.ScheduleActivityAsync(sequence);
        }
    }
}