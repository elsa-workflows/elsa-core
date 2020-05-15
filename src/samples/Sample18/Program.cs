using System.IO;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Timers.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using SmtpDeliveryMethod = Elsa.Activities.Email.Options.SmtpDeliveryMethod;

namespace Sample18
{
    /// <summary>
    /// A minimal workflows program defined in code with a strongly-typed workflow class.
    /// </summary>
    internal static class Program
    {
        private static string PickupLocation = "c:\\data\\smtp-outbox";
        
        private static async Task Main()
        {
            var host = new HostBuilder()
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(logging => logging.AddConsole())
                .UseConsoleLifetime()
                .Build();

            Directory.CreateDirectory(PickupLocation);
            
            using (host)
            {
                await host.StartAsync();

                var workflowInvoker = host.Services.GetRequiredService<IWorkflowInvoker>();
                await workflowInvoker.StartAsync<EmailReminderWorkflow>();
                
                await host.WaitForShutdownAsync();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton(System.Console.In)
                .AddElsa()
                .AddConsoleActivities()
                .AddEmailActivities(options => options.Configure(
                    smtp =>
                    {
                        smtp.DefaultSender = "noreply@acme.com";
                        smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                        smtp.PickupDirectoryLocation = PickupLocation;
                    }))
                .AddWorkflow<EmailReminderWorkflow>()
                .AddTimerActivities(options => options.Configure(x => x.SweepInterval = Duration.FromSeconds(1)));
        }
    }
}