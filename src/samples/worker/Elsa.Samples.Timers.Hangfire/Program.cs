using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elsa.Multitenancy;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

namespace Elsa.Samples.Timers
{
    internal class Program
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
                                .AddConsoleActivities()
                                .AddHangfireTemporalActivities(hangfire => hangfire.UseSqlServerStorage("Server=(localdb)\\MSSQLLocalDB;Database=ElsaHangfire;Trusted_Connection=True;MultipleActiveResultSets=true"))
                                .AddWorkflow<RecurringTaskWorkflow>()
                                .AddWorkflow<CronTaskWorkflow>()
                                .AddWorkflow<CancelTimerWorkflow>()
                                .AddWorkflow(new OneOffWorkflow(SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromSeconds(5)))));

                    builder.Populate(sc);
                });
    }
}