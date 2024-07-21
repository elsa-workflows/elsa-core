using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.MassTransit.Options;
using Elsa.Extensions;
using Elsa.ServerAndStudio.Web.Extensions;
using Elsa.MassTransit.Extensions;
using Elsa.ServerAndStudio.Web.Enums;
using Medallion.Threading.FileSystem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Proto.Persistence.Sqlite;
using WebhooksCore.Options;

const bool useMassTransit = true;
const bool useProtoActor = false;
const bool useCaching = true;
const bool useMySql = false;
const DistributedCachingTransport distributedCachingTransport = DistributedCachingTransport.MassTransit;
const MassTransitBroker useMassTransitBroker = MassTransitBroker.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var mySqlConnectionString = configuration.GetConnectionString("MySql")!;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;
var azureServiceBusConnectionString = configuration.GetConnectionString("AzureServiceBus")!;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var massTransitSection = configuration.GetSection("MassTransit");
var massTransitDispatcherSection = configuration.GetSection("MassTransit.Dispatcher");
var heartbeatSection = configuration.GetSection("Heartbeat");

services.Configure<MassTransitOptions>(massTransitSection);
services.Configure<MassTransitWorkflowDispatcherOptions>(massTransitDispatcherSection);

// Add Elsa services.
services
    .AddElsa(elsa =>
    {
        elsa
            .UseSasTokens()
            .UseIdentity(identity =>
            {
                identity.TokenOptions = options => identityTokenSection.Bind(options);
                identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
            })
            .UseDefaultAuthentication()
            .UseApplicationCluster(x => x.HeartbeatOptions = settings => heartbeatSection.Bind(settings))
            .UseWorkflowManagement(management =>
            {
                if (useMassTransit)
                {
                    management.UseMassTransitDispatcher();
                }

                if (useCaching)
                    management.UseCache();

                if (useMySql)
                    management.UseEntityFrameworkCore(ef => ef.UseMySql(mySqlConnectionString));
                else
                    management.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            })
            .UseWorkflowRuntime(runtime =>
            {
                if (useMySql)
                    runtime.UseEntityFrameworkCore(ef => ef.UseMySql(mySqlConnectionString));
                else
                    runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
                
                if (useMassTransit)
                {
                    runtime.UseMassTransitDispatcher();
                }
                
                if (useProtoActor)
                {
                    runtime.UseProtoActor(proto => proto.PersistenceProvider = _ =>
                    {
                        return new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString));
                    });
                }

                if (useCaching)
                    runtime.UseCache();

                runtime.DistributedLockProvider = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "locks")));
                runtime.WorkflowInboxCleanupOptions = options => configuration.GetSection("Runtime:WorkflowInboxCleanup").Bind(options);
                runtime.WorkflowDispatcherOptions = options => configuration.GetSection("Runtime:WorkflowDispatcher").Bind(options);
            })
            .UseScheduling()
            .UseJavaScript(options => options.AllowClrAccess = true)
            .UseLiquid()
            .UseCSharp()
            // .UsePython()
            .UseHttp(http =>
            {
                if (useCaching)
                    http.UseCache();

                http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options);
            })
            .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
            .UseWebhooks(webhooks => webhooks.ConfigureSinks = options => builder.Configuration.GetSection("Webhooks:Sinks").Bind(options))
            .UseWorkflowsApi()
            .UseRealTimeWorkflows()
            .AddActivitiesFrom<Program>()
            .AddWorkflowsFrom<Program>();

        if (useMassTransit)
        {
            elsa.UseMassTransit(massTransit =>
                {
                    switch (useMassTransitBroker)
                    {
                        case MassTransitBroker.AzureServiceBus:
                            massTransit.UseAzureServiceBus(azureServiceBusConnectionString, asb =>
                            {
                                asb.SubscriptionCleanupOptions = options => options.Interval = TimeSpan.FromMinutes(5);
                                asb.EnableAutomatedSubscriptionCleanup = true;
                            });
                            break;
                        case MassTransitBroker.RabbitMq:
                            massTransit.UseRabbitMq(rabbitMqConnectionString);
                            break;
                    }
                }
            );
        }

        if (distributedCachingTransport != DistributedCachingTransport.None)
        {
            elsa.UseDistributedCache(distributedCaching =>
            {
                if (distributedCachingTransport == DistributedCachingTransport.MassTransit) distributedCaching.UseMassTransit();
            });
        }
    });

services.Configure<WebhookSinksOptions>(options => configuration.GetSection("Webhooks").Bind(options));

services.AddHealthChecks();

services.AddCors(cors => cors.Configure(configuration.GetSection("CorsPolicy")));

// Razor Pages.
services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

// Configure middleware pipeline.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.MapHealthChecks("/health");
app.UseRouting();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.UseWorkflowsSignalRHubs();
app.MapFallbackToPage("/_Host");
app.Run();
