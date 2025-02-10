using Elsa.Agents;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.MassTransit.Options;
using Elsa.Extensions;
using Elsa.JavaScript.Libraries.Extensions;
using Elsa.ServerAndStudio.Web.Extensions;
using Elsa.MassTransit.Extensions;
using Elsa.ServerAndStudio.Web.Enums;
using Medallion.Threading.FileSystem;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Proto.Persistence.Sqlite;
using WebhooksCore.Options;
using Elsa.Connections.Middleware;

const bool useMassTransit = true;
const bool useProtoActor = true;
const bool useCaching = true;
const DistributedCachingTransport distributedCachingTransport = DistributedCachingTransport.MassTransit;
const MassTransitBroker useMassTransitBroker = MassTransitBroker.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var mySqlConnectionString = configuration.GetConnectionString("MySql")!;
var sqlServerConnectionString = configuration.GetConnectionString("SqlServer")!;
var postgreSqlConnectionString = configuration.GetConnectionString("PostgreSql")!;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;
var azureServiceBusConnectionString = configuration.GetConnectionString("AzureServiceBus")!;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var massTransitSection = configuration.GetSection("MassTransit");
var massTransitDispatcherSection = configuration.GetSection("MassTransit.Dispatcher");
var heartbeatSection = configuration.GetSection("Heartbeat");
var sqlDatabaseProvider = Enum.Parse<SqlDatabaseProvider>(configuration["DatabaseProvider"] ?? "Sqlite");

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

                if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                    management.UseEntityFrameworkCore(ef => ef.UseSqlServer(sqlServerConnectionString));
                else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                    management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(postgreSqlConnectionString));
                else if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite)
                    management.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
#if !NET9_0
                else if (sqlDatabaseProvider == SqlDatabaseProvider.MySql)
                    management.UseEntityFrameworkCore(ef => ef.UseMySql(mySqlConnectionString));
#endif
            })
            .UseWorkflowRuntime(runtime =>
            {
                if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                    runtime.UseEntityFrameworkCore(ef => ef.UseSqlServer(sqlServerConnectionString));
                else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                    runtime.UseEntityFrameworkCore(ef => ef.UsePostgreSql(postgreSqlConnectionString));
                else if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite)
                    runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
#if !NET9_0
                else if (sqlDatabaseProvider == SqlDatabaseProvider.MySql)
                    runtime.UseEntityFrameworkCore(ef => ef.UseMySql(mySqlConnectionString));
#endif

                if (useMassTransit)
                {
                    runtime.UseMassTransitDispatcher();
                }

                if (useProtoActor)
                {
                    runtime.UseProtoActor();
                }

                if (useCaching)
                    runtime.UseCache();

                runtime.DistributedLockProvider = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "locks")));
                runtime.WorkflowInboxCleanupOptions = options => configuration.GetSection("Runtime:WorkflowInboxCleanup").Bind(options);
                runtime.WorkflowDispatcherOptions = options => configuration.GetSection("Runtime:WorkflowDispatcher").Bind(options);
            })
            .UseScheduling()
            .UseJavaScript(javaScriptFeature =>
            {
                javaScriptFeature
                    .ConfigureJintOptions(jintOptions => jintOptions.AllowClrAccess = true)
                    .UseLodashFp()
                    .UseMoment();
            })
            .UseLiquid()
            .UseCSharp()
            .UsePython(python =>
            {
                python.PythonOptions += options =>
                {
                    // Make sure to configure the path to the python DLL. E.g. /opt/homebrew/Cellar/python@3.11/3.11.6_1/Frameworks/Python.framework/Versions/3.11/bin/python3.11
                    // alternatively, you can set the PYTHONNET_PYDLL environment variable.
                    configuration.GetSection("Scripting:Python").Bind(options);
                };
            })
            .UseHttp(http =>
            {
                if (useCaching)
                    http.UseCache();

                http.ConfigureHttpOptions = options =>
                {
                    options.BaseUrl = new Uri(configuration["Hosting:BaseUrl"] ?? configuration["Http:BaseUrl"]!); // HttpBaseUrl is for backward compatibility.
                    options.BasePath = configuration["Http:BasePath"];
                };
            })
            .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
            .UseWebhooks(webhooks => webhooks.ConfigureSinks = options => builder.Configuration.GetSection("Webhooks:Sinks").Bind(options))
            .UseWorkflowsApi()
            .UseAgentsApi()
            .UseAgentPersistence(persistence => persistence.UseEntityFrameworkCore(ef =>
            {
                if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                    ef.UseSqlServer(sqlServerConnectionString);
                else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                    ef.UsePostgreSql(postgreSqlConnectionString);
                else if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite)
                    ef.UseSqlite(sqliteConnectionString);
#if !NET9_0
                else if (sqlDatabaseProvider == SqlDatabaseProvider.MySql)
                    ef.UseMySql(mySqlConnectionString);
#endif
            }))
            .UseAgentActivities()
            .AddActivitiesFrom<Program>()
            .AddWorkflowsFrom<Program>();

        if (useProtoActor)
        {
            elsa.UseProtoActor(proto => proto.PersistenceProvider = _ =>
            {
                return new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString));
            });
        }

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

        elsa.UseConnections(
            configure=> configure.AddConnectionsFrom<Program>());
        elsa.UseConnectionsApi();
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
app.MapFallbackToPage("/_Host");
await app.RunAsync();