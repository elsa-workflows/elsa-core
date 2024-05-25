using System.Text.Encodings.Web;
using Elsa.Alterations.Extensions;
using Elsa.Alterations.MassTransit.Extensions;
using Elsa.Caching.Options;
using Elsa.Common.DistributedLocks.Noop;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Services;
using Elsa.DropIns.Extensions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Http.MultiTenancy;
using Elsa.Http.Options;
using Elsa.MassTransit.Extensions;
using Elsa.Identity.MultiTenancy;
using Elsa.MongoDb.Extensions;
using Elsa.MongoDb.Modules.Identity;
using Elsa.MongoDb.Modules.Management;
using Elsa.MongoDb.Modules.Runtime;
using Elsa.Server.Web;
using Elsa.Workflows;
using Elsa.Tenants.Extensions;
using Elsa.Workflows.Management.Compression;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime.Stores;
using JetBrains.Annotations;
using Medallion.Threading.FileSystem;
using Medallion.Threading.Postgres;
using Medallion.Threading.Redis;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Proto.Persistence.Sqlite;
using Proto.Persistence.SqlServer;

const PersistenceProvider persistenceProvider = PersistenceProvider.Dapper;
const SqlDatabaseProvider sqlDatabaseProvider = SqlDatabaseProvider.Sqlite;
const bool useProtoActor = false;
const bool useHangfire = false;
const bool useQuartz = true;
const bool useMassTransit = true;
const bool useZipCompression = true;
const bool runEFCoreMigrations = true;
const bool useMemoryStores = false;
const bool useCaching = true;
const DistributedCachingTransport distributedCachingTransport = DistributedCachingTransport.MassTransit;
const MassTransitBroker useMassTransitBroker = MassTransitBroker.Memory;
const bool useMultitenancy = true;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var sqlServerConnectionString = configuration.GetConnectionString("SqlServer")!;
var postgresConnectionString = configuration.GetConnectionString("PostgreSql")!;
var cockroachDbConnectionString = configuration.GetConnectionString("CockroachDb")!;
var mongoDbConnectionString = configuration.GetConnectionString("MongoDb")!;
var azureServiceBusConnectionString = configuration.GetConnectionString("AzureServiceBus")!;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;
var redisConnectionString = configuration.GetConnectionString("Redis")!;
var distributedLockProviderName = configuration.GetSection("Runtime")["DistributedLockProvider"];

// Add Elsa services.
services
    .AddElsa(elsa =>
    {
        if (persistenceProvider == PersistenceProvider.MongoDb)
            elsa.UseMongoDb(mongoDbConnectionString);

        if (persistenceProvider == PersistenceProvider.Dapper)
            elsa.UseDapper(dapper =>
            {
                dapper.UseMigrations(feature =>
                {
                    if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                        feature.UseSqlServer();
                    else
                        feature.UseSqlite();
                });
                dapper.DbConnectionProvider = sp =>
                {
                    if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                        return new SqlServerDbConnectionProvider(sqlServerConnectionString!);
                    else
                        return new SqliteDbConnectionProvider(sqliteConnectionString);
                };
            });

        if (useHangfire)
            elsa.UseHangfire();

        elsa
            .AddActivitiesFrom<Program>()
            .AddWorkflowsFrom<Program>()
            .UseFluentStorageProvider()
            .UseFileStorage()
            .UseIdentity(identity =>
            {
                if (persistenceProvider == PersistenceProvider.MongoDb)
                    identity.UseMongoDb();
                else if (persistenceProvider == PersistenceProvider.Dapper)
                    identity.UseDapper();
                else
                    identity.UseEntityFrameworkCore(ef =>
                    {
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                            ef.UseSqlServer(sqlServerConnectionString!);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                            ef.UsePostgreSql(postgresConnectionString!);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb)
                            ef.UsePostgreSql(cockroachDbConnectionString!);
                        else
                            ef.UseSqlite(sqliteConnectionString);

                        ef.RunMigrations = runEFCoreMigrations;
                    });
                
                identity.TokenOptions = options => identityTokenSection.Bind(options);
                identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
            })
            .UseDefaultAuthentication()
            .UseWorkflowManagement(management =>
            {
                if (persistenceProvider == PersistenceProvider.MongoDb)
                    management.UseMongoDb();
                else if (persistenceProvider == PersistenceProvider.Dapper)
                    management.UseDapper();
                else
                    management.UseEntityFrameworkCore(ef =>
                    {
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                            ef.UseSqlServer(sqlServerConnectionString!);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                            ef.UsePostgreSql(postgresConnectionString!);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb)
                            ef.UsePostgreSql(cockroachDbConnectionString!);
                        else
                            ef.UseSqlite(sqliteConnectionString);

                        ef.RunMigrations = runEFCoreMigrations;
                    });

                if (useZipCompression)
                    management.SetCompressionAlgorithm(nameof(Zstd));

                if (useMemoryStores)
                    management.UseWorkflowInstances(feature => feature.WorkflowInstanceStore = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>());

                if (useMassTransit)
                    management.UseMassTransitDispatcher();

                if (useCaching)
                    management.UseCache();

                management.SetDefaultLogPersistenceMode(LogPersistenceMode.Inherit);
            })
            .UseWorkflowRuntime(runtime =>
            {
                if (persistenceProvider == PersistenceProvider.MongoDb)
                    runtime.UseMongoDb();
                else if (persistenceProvider == PersistenceProvider.Dapper)
                    runtime.UseDapper();
                else
                    runtime.UseEntityFrameworkCore(ef =>
                    {
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                            ef.UseSqlServer(sqlServerConnectionString!);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                            ef.UsePostgreSql(postgresConnectionString!);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb)
                            ef.UsePostgreSql(cockroachDbConnectionString!);
                        else
                            ef.UseSqlite(sqliteConnectionString);

                        ef.RunMigrations = runEFCoreMigrations;
                    });

                if (useProtoActor)
                {
                    runtime.UseProtoActor(proto => proto.PersistenceProvider = _ =>
                    {
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                            return new SqlServerProvider(sqlServerConnectionString!, true, "", "proto_actor");
                        else
                            return new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString));
                    });
                }

                if (useMassTransit)
                    runtime.UseMassTransitDispatcher();

                runtime.WorkflowInboxCleanupOptions = options => configuration.GetSection("Runtime:WorkflowInboxCleanup").Bind(options);
                runtime.WorkflowDispatcherOptions = options => configuration.GetSection("Runtime:WorkflowDispatcher").Bind(options);

                if (useMemoryStores)
                {
                    runtime.ActivityExecutionLogStore = sp => sp.GetRequiredService<MemoryActivityExecutionStore>();
                    runtime.WorkflowExecutionLogStore = sp => sp.GetRequiredService<MemoryWorkflowExecutionLogStore>();
                    runtime.WorkflowInboxStore = sp => sp.GetRequiredService<MemoryWorkflowInboxMessageStore>();
                }

                if (useCaching)
                    runtime.UseCache();

                runtime.DistributedLockProvider = _ =>
                {
                    switch (distributedLockProviderName)
                    {
                        case "Postgres":
                            return new PostgresDistributedSynchronizationProvider(postgresConnectionString, options =>
                            {
                                options.KeepaliveCadence(TimeSpan.FromMinutes(5));
                                options.UseMultiplexing();
                            });
                        case "Redis":
                            {
                                var connectionMultiplexer = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
                                var database = connectionMultiplexer.GetDatabase();
                                return new RedisDistributedSynchronizationProvider(database);
                            }
                        case "File":
                            return new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "locks")));
                        case "Noop":
                        default:
                            return new NoopDistributedSynchronizationProvider();
                    }
                };
            })
            .UseEnvironments(environments => environments.EnvironmentsOptions = options => configuration.GetSection("Environments").Bind(options))
            .UseScheduling(scheduling =>
            {
                if (useHangfire)
                    scheduling.UseHangfireScheduler();

                if (useQuartz)
                    scheduling.UseQuartzScheduler();
            })
            .UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>())
            .UseRealTimeWorkflows()
            .UseCSharp(options =>
            {
                options.AppendScript("string Greet(string name) => $\"Hello {name}!\";");
                options.AppendScript("string SayHelloWorld() => Greet(\"World\");");
            })
            .UseJavaScript(options =>
            {
                options.AllowClrAccess = true;
                options.ConfigureEngine(engine =>
                {
                    engine.Execute("function greet(name) { return `Hello ${name}!`; }");
                    engine.Execute("function sayHelloWorld() { return greet('World'); }");
                });
            })
            .UsePython(python =>
            {
                python.PythonOptions += options =>
                {
                    // Make sure to configure the path to the python DLL. E.g. /opt/homebrew/Cellar/python@3.11/3.11.6_1/Frameworks/Python.framework/Versions/3.11/bin/python3.11
                    // alternatively, you can set the PYTHONNET_PYDLL environment variable.
                    configuration.GetSection("Scripting:Python").Bind(options);
                };
            })
            .UseLiquid(liquid => liquid.FluidOptions = options => options.Encoder = HtmlEncoder.Default)
            .UseHttp(http =>
            {
                http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options);

                if (useCaching)
                    http.UseCache();
            })
            .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
            .UseAlterations(alterations =>
            {
                if (persistenceProvider == PersistenceProvider.MongoDb)
                {
                    // TODO: alterations.UseMongoDb();
                }
                else if (persistenceProvider == PersistenceProvider.Dapper)
                {
                    // TODO: alterations.UseDapper();
                }
                else
                {
                    alterations.UseEntityFrameworkCore(ef =>
                    {
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                            ef.UseSqlServer(sqlServerConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                            ef.UsePostgreSql(postgresConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb)
                            ef.UsePostgreSql(cockroachDbConnectionString!);
                        else
                            ef.UseSqlite(sqliteConnectionString);

                        ef.RunMigrations = runEFCoreMigrations;
                    });
                }

                if (useMassTransit)
                {
                    alterations.UseMassTransitDispatcher();
                }
            })
            .UseWorkflowContexts();

        if (useQuartz)
        {
            elsa.UseQuartz(quartz => { quartz.UseSqlite(sqliteConnectionString); });
        }

        if (useMassTransit)
        {
            elsa.UseMassTransit(massTransit =>
            {
                if (useMassTransitBroker == MassTransitBroker.AzureServiceBus)
                {
                    massTransit.UseAzureServiceBus(azureServiceBusConnectionString, serviceBusFeature => serviceBusFeature.ConfigureServiceBus = bus =>
                    {
                        bus.PrefetchCount = 100;
                        bus.LockDuration = TimeSpan.FromMinutes(5);
                        bus.MaxConcurrentCalls = 32;
                        bus.MaxDeliveryCount = 8;
                        // etc.
                    });
                }

                if (useMassTransitBroker == MassTransitBroker.RabbitMq)
                {
                    massTransit.UseRabbitMq(rabbitMqConnectionString, rabbit => rabbit.ConfigureServiceBus = bus =>
                    {
                        bus.PrefetchCount = 4;
                        bus.Durable = true;
                        bus.AutoDelete = false;
                        bus.ConcurrentMessageLimit = 32;
                        // etc.
                    });
                }
            });
        }

        if (distributedCachingTransport != DistributedCachingTransport.None)
        {
            elsa.UseDistributedCache(distributedCaching =>
            {
                if (distributedCachingTransport == DistributedCachingTransport.MassTransit) distributedCaching.UseMassTransit();
            });
        }

        if (useMultitenancy)
            elsa.UseTenants(tenants =>
            {
                tenants.TenantsOptions = options =>
                {
                    configuration.GetSection("Multitenancy").Bind(options);
                    options.TenantResolutionPipelineBuilder
                        .Append<HttpContextTenantResolver>()
                        .Append<ClaimsTenantResolver>()
                        .Append<CurrentUserTenantResolver>();
                };
                tenants.UseConfigurationBasedTenantsProvider();
            });

        elsa.InstallDropIns(options => options.DropInRootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "DropIns"));
        elsa.AddSwagger();
        elsa.AddFastEndpointsAssembly<Program>();
        ConfigureForTest?.Invoke(elsa);
    });

services.Configure<CachingOptions>(options => options.CacheDuration = TimeSpan.FromDays(1));

services.AddHealthChecks();
services.AddControllers();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));

// Build the web application.
var app = builder.Build();

// Configure the pipeline.
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

// Routing used for SignalR.
app.UseRouting();

// Security.
app.UseAuthentication();
app.UseAuthorization();

// Elsa API endpoints for designer.
var routePrefix = app.Services.GetRequiredService<IOptions<HttpActivityOptions>>().Value.ApiRoutePrefix;
app.UseWorkflowsApi(routePrefix);

// Captures unhandled exceptions and returns a JSON response.
app.UseJsonSerializationErrorHandler();

// Elsa HTTP Endpoint activities.
app.UseWorkflows();

// Swagger API documentation.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

// SignalR.
app.UseWorkflowsSignalRHubs();

// Run.
app.Run();

/// <summary>
/// The main entry point for the application made public for end to end testing.
/// </summary>
[UsedImplicitly]
public partial class Program
{
    /// <summary>
    /// Set by the test runner to configure the module for testing.
    /// </summary>
    public static Action<IModule>? ConfigureForTest { get; set; }
}