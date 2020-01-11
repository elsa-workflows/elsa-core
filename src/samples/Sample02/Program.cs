using System;
 using System.Threading.Tasks;
using Elsa.Services;
 using Microsoft.Extensions.DependencyInjection;
 
 namespace Sample02
 {
     /// <summary>
     /// A sequential workflow writing out messages. Notice that workflows can be created entirely programmatically.
     /// </summary>
     internal static class Program
     {
         private static async Task Main(string[] args)
         {
             // Setup a service collection.
             var services = new ServiceCollection()
                 .AddElsa()
                 .BuildServiceProvider();
 
             // Get the scheduler.
             var scheduler = services.GetRequiredService<IScheduler>();
 
             // // Create a sequence activity.
             // var sequence = new Sequence(
             //     new Inline(() => WriteLine("Line 1")),
             //     new Inline(() => WriteLine("Line 2")),
             //     new Inline(() => WriteLine("Line 3"))
             // );
             //
             // // Schedule an activity for execution.
             // await scheduler.ScheduleActivityAsync(sequence);
         }
     }
 }