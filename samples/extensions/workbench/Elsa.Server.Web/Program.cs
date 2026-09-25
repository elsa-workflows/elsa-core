using System.Text.Encodings.Web;
using Elsa.Alterations.Extensions;
using Elsa.Alterations.MassTransit.Extensions;
using Elsa.Caching.Options;
using Elsa.Common.Codecs;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Common.RecurringTasks;
using Elsa.DropIns.Extensions;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Identity.Multitenancy;
using Elsa.Ldap.Extensions;
using Elsa.Mqtt.Options;
using Elsa.OpenTelemetry.Middleware;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Services;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Persistence.EFCore.Modules.Identity;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Modules.Tenants;
using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Persistence.MongoDb.Modules.Alterations;
using Elsa.Persistence.MongoDb.Modules.Identity;
using Elsa.Persistence.MongoDb.Modules.Management;
using Elsa.Persistence.MongoDb.Modules.Runtime;
using Elsa.Persistence.MongoDb.Modules.Tenants;
using Elsa.Retention.Extensions;
using Elsa.Retention.Models;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.PostgreSql.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Secrets.Persistence.EFCore.SqlServer.Extensions;
using Elsa.Server.Web;
using Elsa.Server.Web.Extensions;
using Elsa.Server.Web.Filters;
using Elsa.ServiceBus.Kafka;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.Sql.Extensions;
using Elsa.Sql.MySql;
using Elsa.Sql.PostgreSql;
using Elsa.Sql.Sqlite;
using Elsa.Sql.SqlServer;
using Elsa.Tenants.AspNetCore;
using Elsa.Tenants.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Api;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Distributed.Extensions;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Workflows.Runtime.Tasks;
using FluentMigrator.Runner;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Hangfire.PostgreSql.Factories;
using Hangfire.SqlServer;
using Hangfire.Storage.SQLite;
using JetBrains.Annotations;
using Medallion.Threading.FileSystem;
using Medallion.Threading.Postgres;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Options;
using Proto.Cluster.Kubernetes;
using Proto.Persistence.Sqlite;
using Proto.Persistence.SqlServer;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using StackExchange.Redis;

// ReSharper disable RedundantAssignment
const PersistenceProvider persistenceProvider = PersistenceProvider.EntityFrameworkCore;
const bool useDbContextPooling = false;
const bool useHangfire = false;
const bool useQuartz = true;
const bool useMassTransit = true;
const bool useZipCompression = false;
const bool runEFCoreMigrations = true;
const bool useMemoryStores = false;
const bool useCaching = true;
const bool useKafka = false;
const bool useReadOnlyMode = false;
const bool useSignalR = false; // Disabled until Elsa Studio sends authenticated requests.
const WorkflowRuntime workflowRuntime = WorkflowRuntime.Distributed;
const DistributedCachingTransport distributedCachingTransport = DistributedCachingTransport.MassTransit;
const MassTransitBroker massTransitBroker = MassTransitBroker.Memory;
const bool useTenantsFromConfiguration = true;
const bool disableVariableWrappers = false;

ObjectConverter.StrictMode = true;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var useSecrets = configuration.GetValue("Features:Secrets:Enabled", false);
var useMultitenancy = configuration.GetValue("Features:Multitenancy:Enabled", false);
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var sqlServerConnectionString = configuration.GetConnectionString("SqlServer")!;
var postgresConnectionString = configuration.GetConnectionString("PostgreSql")!;
var citusConnectionString = configuration.GetConnectionString("Citus")!;
var yugabyteDbConnectionString = configuration.GetConnectionString("YugabyteDb")!;
var oracleConnectionString = configuration.GetConnectionString("Oracle")!;
var mySqlConnectionString = configuration.GetConnectionString("MySql")!;
var cockroachDbConnectionString = configuration.GetConnectionString("CockroachDb")!;
var mongoDbConnectionString = configuration.GetConnectionString("MongoDb")!;
var azureServiceBusConnectionString = configuration.GetConnectionString("AzureServiceBus")!;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;
var redisConnectionString = configuration.GetConnectionString("Redis")!;
var distributedLockProviderName = configuration.GetSection("Runtime:DistributedLocking")["Provider"];
var appRole = Enum.Parse<ApplicationRole>(configuration["AppRole"] ?? "Default");
var sqlDatabaseProvider = Enum.Parse<SqlDatabaseProvider>(configuration["DatabaseProvider"] ?? "Sqlite");

// Add Elsa services.
services.AddScoped<IPropertyUIHandler, CustomCheckListOptionsProvider>();
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
                    feature.ConfigureRunner = builder =>
                    {
                        var runnerBuilder = sqlDatabaseProvider == SqlDatabaseProvider.SqlServer
                            ? builder.AddSqlServer()
                            : builder.AddSQLite();
                        
                        runnerBuilder
                            .WithGlobalConnectionString(sp => sp.GetRequiredService<IDbConnectionProvider>().GetConnectionString())
                            .WithMigrationsIn(typeof(Elsa.Persistence.Dapper.Migrations.Management.Initial).Assembly);
                    };
                });
                dapper.DbConnectionProvider = sp =>
                {
                    if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                        return new SqlServerDbConnectionProvider(sqlServerConnectionString);
                    else
                        return new SqliteDbConnectionProvider(sqliteConnectionString);
                };
            });

        if (useHangfire)
        {
            JobStorage jobStorage;
            if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
            {
                jobStorage = new PostgreSqlStorage(new NpgsqlConnectionFactory(postgresConnectionString, new()
                {
                    QueuePollInterval = TimeSpan.FromSeconds(1)
                }));
            }
            else if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite)
            {
                jobStorage = new SQLiteStorage(sqliteConnectionString, new()
                {
                    QueuePollInterval = TimeSpan.FromSeconds(1)
                });
            }
            else if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
            {
                jobStorage = new SqlServerStorage(sqlServerConnectionString, new()
                {
                    QueuePollInterval = TimeSpan.FromSeconds(1)
                });
            }
            else
            {
                jobStorage = new MemoryStorage();
            }

            elsa.UseHangfire(hangfire => hangfire.UseJobStorage(jobStorage));
        }

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
                        ef.UseContextPooling = useDbContextPooling;
                        
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                            ef.UseSqlServer(sqlServerConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                            ef.UsePostgreSql(postgresConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.Citus)
                            ef.UsePostgreSql(citusConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.YugabyteDb)
                            ef.UsePostgreSql(yugabyteDbConnectionString);
#if !NET9_0
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.MySql) 
                            ef.UseMySql(mySqlConnectionString);
#endif
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb)
                            ef.UsePostgreSql(cockroachDbConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.Oracle)
                            ef.UseOracle(oracleConnectionString, new()
                            {
                                SchemaName = "ELSA"
                            });
                        else
                            ef.UseSqlite(sp => sp.GetSqliteConnectionString());

                        ef.RunMigrations = runEFCoreMigrations;
                    });

                identity.TokenOptions = options => identityTokenSection.Bind(options);
                identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
            })
            .UseDefaultAuthentication()
            .UseWorkflows(workflows =>
            {
                workflows.WithDefaultWorkflowExecutionPipeline(pipeline => pipeline.UseWorkflowExecutionTracing());
                workflows.WithDefaultActivityExecutionPipeline(pipeline => pipeline.UseActivityExecutionTracing());
                workflows.UseCommitStrategies(strategies =>
                {
                    strategies.AddStandardStrategies();
                    strategies.Add("Every 10 seconds", new PeriodicWorkflowStrategy(TimeSpan.FromSeconds(10)));
                });
            })
            .UseWorkflowManagement(management =>
            {
                if (persistenceProvider == PersistenceProvider.MongoDb)
                    management.UseMongoDb();
                else if (persistenceProvider == PersistenceProvider.Dapper)
                    management.UseDapper();
                else
                    management.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseContextPooling = useDbContextPooling;
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                            ef.UseSqlServer(sqlServerConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                            ef.UsePostgreSql(postgresConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.Citus)
                            ef.UsePostgreSql(citusConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.YugabyteDb)
                            ef.UsePostgreSql(yugabyteDbConnectionString);
#if !NET9_0
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.MySql)
                            ef.UseMySql(mySqlConnectionString);
#endif
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb)
                            ef.UsePostgreSql(cockroachDbConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.Oracle)
                            ef.UseOracle(oracleConnectionString, new()
                            {
                                SchemaName = "ELSA"
                            });
                        else
                            ef.UseSqlite(sp => sp.GetSqliteConnectionString());

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
                management.UseReadOnlyMode(useReadOnlyMode);
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
                        ef.UseContextPooling = useDbContextPooling;
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                        {
                            //ef.UseSqlServer(sqlServerConnectionString, new ElsaDbContextOptions);
                            var migrationsAssembly = typeof(Elsa.Persistence.EFCore.SqlServer.IdentityDbContextFactory).Assembly;
                            var connectionString = sqlServerConnectionString;
                            ef.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(migrationsAssembly, connectionString, null, configure => configure.CommandTimeout(60000));
                        }
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                            ef.UsePostgreSql(postgresConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.Citus)
                            ef.UsePostgreSql(citusConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.YugabyteDb)
                            ef.UsePostgreSql(yugabyteDbConnectionString);
#if !NET9_0
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.MySql)
                            ef.UseMySql(mySqlConnectionString);
#endif
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb)
                            ef.UsePostgreSql(cockroachDbConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.Oracle)
                            ef.UseOracle(oracleConnectionString, new()
                            {
                                SchemaName = "ELSA"
                            });
                        else
                            ef.UseSqlite(sp => sp.GetSqliteConnectionString());

                        ef.RunMigrations = runEFCoreMigrations;
                    });

                if (workflowRuntime == WorkflowRuntime.Distributed)
                {
                    runtime.UseDistributedRuntime();
                }

                if (workflowRuntime == WorkflowRuntime.ProtoActor)
                {
                    runtime.UseProtoActor();
                }

                if (useMassTransit)
                    runtime.UseMassTransitDispatcher();

                runtime.WorkflowDispatcherOptions = options => configuration.GetSection("Runtime:WorkflowDispatcher").Bind(options);

                if (useMemoryStores)
                {
                    runtime.ActivityExecutionLogStore = sp => sp.GetRequiredService<MemoryActivityExecutionStore>();
                    runtime.WorkflowExecutionLogStore = sp => sp.GetRequiredService<MemoryWorkflowExecutionLogStore>();
                }

                if (useQuartz)
                    runtime.UseQuartzBackgroundActivityScheduler();

                if (useCaching)
                    runtime.UseCache();

                runtime.DistributedLockingOptions = options => configuration.GetSection("Runtime:DistributedLocking").Bind(options);

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
                                var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
                                var database = connectionMultiplexer.GetDatabase();
                                return new RedisDistributedSynchronizationProvider(database);
                            }
                        case "File":
                            return new FileDistributedSynchronizationProvider(new(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "locks")));
                        case "Noop":
                        default:
                            return new NoopDistributedSynchronizationProvider();
                    }
                };
            })
            .UseScheduling(scheduling =>
            {
                if (useHangfire)
                    scheduling.UseHangfireScheduler();

                if (useQuartz)
                    scheduling.UseQuartzScheduler();
            })
            .UseWorkflowsApi(api =>
            {
                api.AddFastEndpointsAssembly<Program>();
            })
            .UseCSharp(options =>
            {
                options.DisableWrappers = disableVariableWrappers;
                options.AppendScript("string Greet(string name) => $\"Hello {name}!\";");
                options.AppendScript("string SayHelloWorld() => Greet(\"World\");");
            })
            .UseJavaScript(options =>
            {
                options.AllowClrAccess = true;
                options.DisableWrappers = disableVariableWrappers;
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

                    options.AddScript(sb =>
                    {
                        sb.AppendLine("def greet():");
                        sb.AppendLine("    return \"Hello, welcome to Python!\"");
                    });
                };
            })
            .UseLiquid(liquid => liquid.FluidOptions = options => options.Encoder = HtmlEncoder.Default)
            .UseHttp(http =>
            {
                http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options);

                if (useCaching)
                    http.UseCache();
            })
            .UseSql(options =>
            {
                options.Clients = client =>
                {
                    client.Register<MySqlClient>("MySql");
                    client.Register<PostgreSqlClient>("PostgreSql");
                    client.Register<SqliteClient>("Sqlite");
                    client.Register<SqlServerClient>("Sql Server");
                };
            })
            .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
            .UseLdap(ldap =>
            {
                ldap.ConfigureOptions = options =>
                {
                    options.AddDefaultConnection(new Elsa.Ldap.Options.LdapConnectionOptions
                    {
                        BindDn = configuration.GetValue<string>("Ldap:BindDn"),
                        BindPassword = configuration.GetValue<string>("Ldap:BindPassword"),
                        Host = configuration.GetValue<string>("Ldap:Host")!,
                        Port = configuration.GetValue<int>("Ldap:Port"),
                        UseSsl = configuration.GetValue<bool>("Ldap:UseSsl"),
                    });
                };
            })
            .UseMqtt(mqtt =>
            {
                mqtt.ConfigureOptions = options =>
                {
                    options.AddDefaultConnection(new MqttConnectionOptions
                    {
                        Host = configuration.GetValue<string>("Mqtt:Host")!,
                        Port = configuration.GetValue<int>("Mqtt:Port"),
                    });
                };
            })
            .UseAlterations(alterations =>
            {
                if (persistenceProvider == PersistenceProvider.MongoDb)
                {
                    alterations.UseMongoDb();
                }
                else if (persistenceProvider == PersistenceProvider.Dapper)
                {
                    // TODO: alterations.UseDapper();
                }
                else
                {
                    alterations.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseContextPooling = useDbContextPooling;
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                            ef.UseSqlServer(sqlServerConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                            ef.UsePostgreSql(postgresConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.Citus)
                            ef.UsePostgreSql(citusConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.YugabyteDb)
                            ef.UsePostgreSql(yugabyteDbConnectionString);
#if !NET9_0
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.MySql)
                            ef.UseMySql(mySqlConnectionString);
#endif
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb)
                            ef.UsePostgreSql(cockroachDbConnectionString);
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.Oracle)
                            ef.UseOracle(oracleConnectionString, new()
                            {
                                SchemaName = "ELSA"
                            });
                        else
                            ef.UseSqlite(sp => sp.GetSqliteConnectionString());

                        ef.RunMigrations = runEFCoreMigrations;
                    });
                }

                if (useMassTransit)
                {
                    alterations.UseMassTransitDispatcher();
                }
            })
            .UseOpenTelemetry(otel => otel.UseNewRootActivityForRemoteParent = true)
            .UseWorkflowContexts();

        if (useQuartz)
        {
            elsa.UseQuartz(quartz =>
            {
                if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite)
                    quartz.UseSqlite(sqliteConnectionString);
                else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                    quartz.UsePostgreSql(postgresConnectionString);
            });
        }

        if (useSignalR)
        {
            elsa.UseRealTimeWorkflows();
        }

        if (useMassTransit)
        {
            elsa.UseMassTransit(massTransit =>
            {
                massTransit.DisableConsumers = appRole == ApplicationRole.Api;

                if (massTransitBroker == MassTransitBroker.AzureServiceBus)
                {
                    massTransit.UseAzureServiceBus(azureServiceBusConnectionString, serviceBusFeature => serviceBusFeature.ConfigureTransportBus = (context, bus) =>
                    {
                        bus.PrefetchCount = 50;
                        bus.LockDuration = TimeSpan.FromMinutes(5);
                        bus.MaxConcurrentCalls = 32;
                        bus.MaxDeliveryCount = 8;
                        // etc.
                    });
                }

                if (massTransitBroker == MassTransitBroker.RabbitMq)
                {
                    massTransit.UseRabbitMq(rabbitMqConnectionString, rabbit => rabbit.ConfigureTransportBus = (context, bus) =>
                    {
                        bus.PrefetchCount = 50;
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
                if (distributedCachingTransport == DistributedCachingTransport.ProtoActor) distributedCaching.UseProtoActor();
            });
        }

        if (distributedCachingTransport == DistributedCachingTransport.ProtoActor || workflowRuntime == WorkflowRuntime.ProtoActor)
        {
            elsa.UseProtoActor(proto =>
            {
                proto
                    .EnableMetrics()
                    .EnableTracing();

                proto.PersistenceProvider = _ =>
                {
                    if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                        return new SqlServerProvider(sqlServerConnectionString, true, "", "proto_actor");
                    return new SqliteProvider(new(sqliteConnectionString));
                };

                if (configuration["KUBERNETES_SERVICE_HOST"] != null)
                {
                    var kubernetesConfig = new KubernetesProviderConfig();
                    var clusterProvider = new KubernetesProvider(kubernetesConfig);

                    var remoteConfig = RemoteConfig
                        .BindToAllInterfaces(advertisedHost: configuration["ProtoActor:AdvertisedHost"]) // Environment variable to be provided by Kubernetes using pod.status.podIP.
                        .WithLogLevelForDeserializationErrors(LogLevel.Critical)
                        .WithRemoteDiagnostics(true);

                    proto.CreateClusterProvider = _ => clusterProvider;
                    proto.ConfigureRemoteConfig = _ => remoteConfig;
                }
            });
        }

        if (useKafka)
        {
            elsa.UseKafka(kafka =>
            {
                kafka.ConfigureOptions(options => configuration.GetSection("Kafka").Bind(options));
            });
        }

        if (useSecrets)
        {
            elsa
                .UseSecrets(secrets =>
                {
                    secrets.ConfigureOptions = options => configuration.GetSection("Secrets").Bind(options);
                    secrets.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseContextPooling = useDbContextPooling;
                        if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer)
                        {
                            ef.UseSqlServer(sqlServerConnectionString);
                        }
                        else if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql)
                        {
                            ef.UsePostgreSql(postgresConnectionString);
                        }
                        else
                        {
                            ef.UseSqlite(sp => sp.GetSqliteConnectionString());
                        }
                    });
                })
                .UseSecretsJavaScript();
        }

        elsa.UseRetention(r =>
        {
            r.SweepInterval = TimeSpan.FromHours(5);
            r.AddDeletePolicy("Delete all finished workflows", sp =>
            {
                var filter = new RetentionWorkflowInstanceFilter
                {
                    WorkflowStatus = WorkflowStatus.Finished
                };
                return filter;
            });
        });

        if (useMultitenancy)
        {
            elsa.UseTenants(tenants =>
            {
                tenants.ConfigureMultitenancy(options =>
                {
                    options.TenantResolverPipelineBuilder
                        .Append<HostTenantResolver>()
                        .Append<RoutePrefixTenantResolver>()
                        .Append<HeaderTenantResolver>()
                        .Append<ClaimsTenantResolver>();
                });

                if (useTenantsFromConfiguration)
                {
                    tenants.UseConfigurationBasedTenantsProvider(options => configuration.GetSection("Multitenancy").Bind(options));
                }
                else
                {
                    tenants.UseStoreBasedTenantsProvider();

                    tenants.UseTenantManagement(management =>
                    {
                        if (persistenceProvider == PersistenceProvider.MongoDb)
                            management.UseMongoDb();
                        if (persistenceProvider == PersistenceProvider.Dapper)
                            throw new NotSupportedException("Dapper is not supported for tenant management.");
                        if (persistenceProvider == PersistenceProvider.EntityFrameworkCore)
                        {
                            management.UseEntityFrameworkCore(ef =>
                            {
                                ef.UseContextPooling = useDbContextPooling;
                                if (sqlDatabaseProvider == SqlDatabaseProvider.Sqlite) ef.UseSqlite(sqliteConnectionString);
                                if (sqlDatabaseProvider == SqlDatabaseProvider.SqlServer) ef.UseSqlServer(sqlServerConnectionString);
                                if (sqlDatabaseProvider == SqlDatabaseProvider.PostgreSql) ef.UsePostgreSql(postgresConnectionString);
                                if (sqlDatabaseProvider == SqlDatabaseProvider.Citus) ef.UsePostgreSql(citusConnectionString);
                                if (sqlDatabaseProvider == SqlDatabaseProvider.YugabyteDb) ef.UsePostgreSql(yugabyteDbConnectionString);
                                if (sqlDatabaseProvider == SqlDatabaseProvider.Oracle)
                                    ef.UseOracle(oracleConnectionString, new()
                                    {
                                        SchemaName = "ELSA"
                                    });
#if !NET9_0
                                if (sqlDatabaseProvider == SqlDatabaseProvider.MySql) 
                                    ef.UseMySql(mySqlConnectionString);
#endif
                                if (sqlDatabaseProvider == SqlDatabaseProvider.CockroachDb) ef.UsePostgreSql(cockroachDbConnectionString);
                            });
                        }
                    });

                    tenants.UseTenantManagementEndpoints();
                }
            });

            elsa.UseTenantHttpRouting(tenantHttpRouting =>
            {
                // Override the tenant header name with a custom one.
                tenantHttpRouting.WithTenantHeader("X-Tenant-ID");
            });
        }

        elsa.UseWebhooks(webhooks => webhooks.ConfigureSinks += options => builder.Configuration.GetSection("Webhooks").Bind(options));
        elsa.InstallDropIns(options => options.DropInRootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "DropIns"));
        elsa.AddSwagger();
        elsa.AddFastEndpointsAssembly<Program>();
        ConfigureForTest?.Invoke(elsa);
    });

// Obfuscate HTTP request headers.
services.AddActivityStateFilter<HttpRequestAuthenticationHeaderFilter>();

// Optionally configure recurring tasks using alternative schedules.
services.Configure<RecurringTaskOptions>(options =>
{
    options.Schedule.ConfigureTask<TriggerBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(300));
    options.Schedule.ConfigureTask<PurgeBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(300));
    options.Schedule.ConfigureTask<RestartInterruptedWorkflowsTask>(TimeSpan.FromSeconds(15));
});

services.Configure<RuntimeOptions>(options => { options.InactivityThreshold = TimeSpan.FromSeconds(15); });
services.Configure<BookmarkQueuePurgeOptions>(options => options.Ttl = TimeSpan.FromSeconds(10));
services.Configure<CachingOptions>(options => options.CacheDuration = TimeSpan.FromDays(1));
services.Configure<IncidentOptions>(options => options.DefaultIncidentStrategy = typeof(ContinueWithIncidentsStrategy));
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

// Multitenancy.
if (useMultitenancy)
    app.UseTenants();

// Elsa API endpoints for designer.
var routePrefix = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value.RoutePrefix;
app.UseWorkflowsApi(routePrefix);

// Captures unhandled exceptions and returns a JSON response.
app.UseJsonSerializationErrorHandler();

// Elsa HTTP Endpoint activities.
app.UseWorkflows();

app.MapControllers();

// Swagger API documentation.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

// SignalR.
if (useSignalR)
{
    app.UseWorkflowsSignalRHubs();
}

if (configuration.GetValue("Features:Secrets:RouteProbe", false))
{
    app.MapGet("/__fixture/secrets/routes", (IEnumerable<Microsoft.AspNetCore.Routing.EndpointDataSource> endpointSources) =>
    {
        var configuredRoutePrefix = string.IsNullOrWhiteSpace(routePrefix)
            ? string.Empty
            : "/" + routePrefix.Trim('/');
        var routes = endpointSources
            .SelectMany(source => source.Endpoints)
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Select(endpoint => new { endpoint, path = endpoint.RoutePattern.RawText })
            .Where(item => item.path is not null)
            .Select(item => new
            {
                item.endpoint,
                path = configuredRoutePrefix.Length > 0
                    && item.path!.StartsWith(configuredRoutePrefix + "/", StringComparison.OrdinalIgnoreCase)
                    ? item.path[configuredRoutePrefix.Length..]
                    : item.path!
            })
            .Where(item => item.path == "/secrets"
                || item.path.StartsWith("/secrets/", StringComparison.Ordinal)
                || item.path.StartsWith("/actions/secrets/", StringComparison.Ordinal)
                || item.path.StartsWith("/bulk-actions/secrets/", StringComparison.Ordinal)
                || item.path.StartsWith("/queries/secrets/", StringComparison.Ordinal))
            .Select(item =>
            {
                var definition = item.endpoint.Metadata.FirstOrDefault(metadata =>
                    metadata.GetType().FullName == "FastEndpoints.EndpointDefinition");
                var endpointType = definition?.GetType()
                    .GetProperty("EndpointType", System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(definition) as Type;

                return new
                {
                    path = item.path,
                    methods = item.endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()?.HttpMethods
                        .OrderBy(method => method, StringComparer.Ordinal)
                        .ToArray() ?? Array.Empty<string>(),
                    endpointType = endpointType?.FullName,
                    endpointAssembly = endpointType?.Assembly.GetName().Name,
                    displayName = item.endpoint.DisplayName,
                    handlerType = item.endpoint.RequestDelegate?.Method.DeclaringType?.FullName,
                    metadataTypes = item.endpoint.Metadata
                        .Select(metadata => metadata.GetType().FullName ?? metadata.GetType().Name)
                        .OrderBy(type => type, StringComparer.Ordinal)
                        .ToArray()
                };
            })
            .OrderBy(route => route.path, StringComparer.Ordinal)
            .ThenBy(route => string.Join(",", route.methods), StringComparer.Ordinal)
            .ToArray();

        var secretAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetName().Name)
            .Where(name => name?.Contains("Secrets", StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return Results.Json(new { routes, secretAssemblies });
    });
}

// Run.
await app.RunAsync();

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