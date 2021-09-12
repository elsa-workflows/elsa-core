using Elsa.Samples.WatchDirectoryWorker.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace Elsa.Samples.WatchDirectoryWorker
{
    class Program
    {
        private static readonly string _directory;
        private static readonly string _systemRoot;

        static Program()
        {
            _systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
            _directory = Path.Combine(_systemRoot, "Temp\\FileWatchers");
        }

        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddElsa(options => options
                            .AddConsoleActivities()
                            .AddFileActivities()
                            .AddQuartzTemporalActivities()
                            .AddWorkflow<TimerCreateFile>(sp => ActivatorUtilities.CreateInstance<TimerCreateFile>(services.BuildServiceProvider(), _directory))
                            .AddWorkflow<WatchDirectoryCreatedWorkflow>(sp => new WatchDirectoryCreatedWorkflow(_directory)));
                });
    }
}
