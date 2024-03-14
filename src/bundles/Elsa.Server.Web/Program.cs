using System.Text.Encodings.Web;
using Elsa.Alterations.Extensions;
using Elsa.Alterations.MassTransit.Extensions;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Services;
using Elsa.DropIns.Extensions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Http.Options;
using Elsa.MongoDb.Extensions;
using Elsa.MongoDb.Modules.Identity;
using Elsa.MongoDb.Modules.Management;
using Elsa.MongoDb.Modules.Runtime;
using Elsa.Server.Web;
using Elsa.Workflows.Management.Compression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Proto.Persistence.Sqlite;
using Proto.Persistence.SqlServer;

const bool useMongoDb = false;
const bool useSqlServer = false;
const bool useDapper = false;
const bool useProtoActor = false;
const bool useHangfire = false;
const bool useQuartz = true;
const bool useMassTransit = true;
const bool useZipCompression = false;
const MassTransitBroker useMassTransitBroker = MassTransitBroker.Memory;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var sqlServerConnectionString = configuration.GetConnectionString("SqlServer")!;
var mongoDbConnectionString = configuration.GetConnectionString("MongoDb")!;
var azureServiceBusConnectionString = configuration.GetConnectionString("AzureServiceBus")!;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;

// Add Elsa services.
services
    .AddElsa(elsa =>
    {
        if (useMongoDb)
            elsa.UseMongoDb(mongoDbConnectionString);

        if (useDapper)
            elsa.UseDapper(dapper =>
            {
                dapper.UseMigrations(feature =>
                {
                    if (useSqlServer)
                        feature.UseSqlServer();
                    else
                        feature.UseSqlite();
                });
                dapper.DbConnectionProvider = sp =>
                {
                    if (useSqlServer)
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
                if (useMongoDb)
                    identity.UseMongoDb();
                else if (useDapper)
                    identity.UseDapper();
                else
                    identity.UseEntityFrameworkCore(ef =>
                    {
                        if (useSqlServer)
                            ef.UseSqlServer(sqlServerConnectionString!);
                        else
                            ef.UseSqlite(sqliteConnectionString);
                    });

                identity.IdentityOptions = options => identitySection.Bind(options);
                identity.TokenOptions = options => identityTokenSection.Bind(options);
                identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
            })
            .UseDefaultAuthentication()
            .UseWorkflowManagement(management =>
            {
                if (useMongoDb)
                    management.UseMongoDb();
                else if (useDapper)
                    management.UseDapper();
                else
                    management.UseEntityFrameworkCore(ef =>
                    {
                        if (useSqlServer)
                            ef.UseSqlServer(sqlServerConnectionString!);
                        else
                            ef.UseSqlite(sqliteConnectionString);
                    });

                if (useZipCompression)
                    management.SetCompressionAlgorithm(nameof(Zstd));
            })
            .UseWorkflowRuntime(runtime =>
            {
                if (useMongoDb)
                    runtime.UseMongoDb();
                else if (useDapper)
                    runtime.UseDapper();
                else
                    runtime.UseEntityFrameworkCore(ef =>
                    {
                        if (useSqlServer)
                            ef.UseSqlServer(sqlServerConnectionString!);
                        else
                            ef.UseSqlite(sqliteConnectionString);
                    });

                if (useProtoActor)
                {
                    runtime.UseProtoActor(proto => proto.PersistenceProvider = _ =>
                    {
                        if (useSqlServer)
                            return new SqlServerProvider(sqlServerConnectionString!, true, "", "proto_actor");
                        else
                            return new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString));
                    });
                }

                if (useMassTransit)
                {
                    runtime.UseMassTransitDispatcher();
                }

                runtime.WorkflowInboxCleanupOptions = options => configuration.GetSection("Runtime:WorkflowInboxCleanup").Bind(options);
                runtime.WorkflowDispatcherOptions = options => configuration.GetSection("Runtime:WorkflowDispatcher").Bind(options);
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
            })
            .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
            .UseAlterations(alterations =>
            {
                if (useMongoDb)
                {
                    // TODO: alterations.UseMongoDb();
                }
                else if (useDapper)
                {
                    // TODO: alterations.UseDapper();
                }
                else
                {
                    alterations.UseEntityFrameworkCore(ef =>
                    {
                        if (useSqlServer)
                            ef.UseSqlServer(sqlServerConnectionString);
                        else
                            ef.UseSqlite(sqliteConnectionString);
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
                        bus.PrefetchCount = 4;
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

        elsa.InstallDropIns(options => options.DropInRootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "DropIns"));
        elsa.AddSwagger();
        elsa.AddFastEndpointsAssembly<Program>();
    });

services.AddHealthChecks();
services.AddControllers();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));

// Build the web application.
var app = builder.Build();

// app.UseSimulatedLatency(
//     TimeSpan.FromMilliseconds(1000),
//     TimeSpan.FromMilliseconds(3000)
// );

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