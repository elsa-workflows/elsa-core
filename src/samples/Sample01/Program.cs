using System;
 using System.Threading.Tasks;
 using Elsa.Extensions;
 using Elsa.Services;
 using Microsoft.Extensions.DependencyInjection;
 
 namespace Sample01
 {
     using static Console;
     
     /// <summary>
     /// A minimal workflows program that writes a simple Hello World message.
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
             
             // Schedule an activity for execution.
             // await scheduler.ScheduleActivityAsync(() => WriteLine("Hello World!"));
         }
     }
 }