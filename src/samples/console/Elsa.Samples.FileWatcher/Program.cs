using Elsa.Activities.File.Options;
using Elsa.Activities.File.Services;
using Elsa.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Elsa.Samples.FileWatcher
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;
                    services.AddElsa(configure => configure
                        .AddConsoleActivities()
                        .AddFileActivities(configuration.GetSection("Elsa")
                            .GetSection("FileWatcher")
                            .Bind)
                        .AddWorkflow<FileWatcherWorkflow>()
                        .AddWorkflow<FileWatcherWorkflowIgnore>()
                    );
                })
                .RunConsoleAsync();
        }
    }
}
