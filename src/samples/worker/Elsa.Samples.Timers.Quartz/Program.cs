using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Multitenancy;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Samples.Timers.Activities;
using Elsa.Samples.Timers.Workflows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.Timers
{
    internal static class Program
    {
        private static async Task Main() => await CreateHostBuilder().UseConsoleLifetime().Build().RunAsync();

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacMultitenantServiceProviderFactory(container => MultitenantContainerFactory.CreateSampleMultitenantContainer(container)))
                .ConfigureServices((_, services) => services.AddElsaServices())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    var sc = new ServiceCollection();

                    builder
                       .ConfigureElsaServices(sc,
                            options => options
                                .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                                .AddConsoleActivities()
                                .AddQuartzTemporalActivities()
                                .AddActivity<MyContainer1>()
                                .AddActivity<MyContainer2>()
                                //.AddWorkflow<SingletonTimerWorkflow>()
                                //.AddWorkflow<TimerWorkflow>()
                                .AddWorkflow<ComplicatedWorkflow>()
                                //.AddWorkflow<CancelTimerWorkflow>()
                                //.AddWorkflow<CronTaskWorkflow>()
                                //.AddWorkflow(sp => ActivatorUtilities.CreateInstance<OneOffWorkflow>(sp, sp.GetRequiredService<IClock>().GetCurrentInstant().Plus(Duration.FromSeconds(5))))
                                .StartWorkflow<ComplicatedWorkflow>());

                    builder.Populate(sc);
                });
    }
}