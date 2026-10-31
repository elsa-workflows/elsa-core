using Elsa.Extensions;
using Elsa.Scheduling.Quartz.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.ComponentTests.Fixtures;

[UsedImplicitly]
public class WorkflowServer(string url) : WebApplicationFactory<Program>
{
    public async Task<global::Quartz.IScheduler> GetSchedulerAsync()
    {
        var factory = Services.GetRequiredService<ISchedulerFactory>();
        return await factory.GetScheduler();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseUrls(url);

        TestServer.Web.Program.ConfigureForTest ??= elsa =>
        {
            elsa.UseQuartz();
            elsa.UseScheduling(scheduling => scheduling.UseQuartzScheduler());
            elsa.UseWorkflowRuntime();
        };

        builder.ConfigureTestServices(services =>
        {
            // Configure Quartz job options for fast retries in tests
            services.Configure<QuartzJobOptions>(options => options.InitialRetryDelay = TimeSpan.FromSeconds(1));
        });
    }
}
