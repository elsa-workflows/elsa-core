using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Elsa.Multitenancy;
using Elsa.Samples.WatchDirectoryWorker.Workflows;
using Microsoft.AspNetCore.Hosting;
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
                .UseServiceProviderFactory(new AutofacMultitenantServiceProviderFactory(container => MultitenantContainerFactory.CreateSampleMultitenantContainer(container)))
                .ConfigureServices((_, services) => services.AddElsaServices())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    var sc = new ServiceCollection();

                    builder
                       .ConfigureElsaServices(sc,
                            options => options
                                .AddConsoleActivities()
                                .AddFileActivities()
                                .AddQuartzTemporalActivities()
                                .AddWorkflow<TimerCreateFile>(sp => ActivatorUtilities.CreateInstance<TimerCreateFile>(sc.BuildServiceProvider(), _directory))
                                .AddWorkflow<WatchDirectoryCreatedWorkflow>(sp => new WatchDirectoryCreatedWorkflow(_directory)));

                    builder.Populate(sc);
                });
    }
}
