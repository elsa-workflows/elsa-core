using System;
using System.Threading.Tasks;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Samples.Timers
{
    internal class Program
    {
        private static async Task Main()
        {
            await using var services = new ServiceCollection()
                .AddElsa()
                .AddWorkflow<HelloWorldWorkflow>()
                .BuildServiceProvider();

            await services.StartElsaAsync();
            var scheduler = services.GetRequiredService<IWorkflowScheduler>();

            await scheduler.ScheduleNewWorkflow<HelloWorldWorkflow>();

            Console.ReadKey();
        }
    }
}