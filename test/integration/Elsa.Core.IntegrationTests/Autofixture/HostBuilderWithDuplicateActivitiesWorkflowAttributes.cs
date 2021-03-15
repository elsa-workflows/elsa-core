using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Core.IntegrationTests.Workflows;
using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Elsa.Persistence.EntityFramework.Sqlite;
using Elsa.Persistence.YesSql;
using YesSql.Provider.Sqlite;
using System.Data;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class HostBuilderWithDuplicateActivitiesWorkflowAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBubilderUsingServicesCustomization(services => {
                services
                    .AddElsa(elsa => {
                        elsa.AddWorkflow<DuplicateActivitiesWorkflow>();
                    });
            }, parameter);
        }
    }

    public class HostBuilderWithDuplicateActivitiesWorkflowAndMongoDbAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBubilderUsingServicesCustomization(services => {
                services
                    .AddElsa(elsa => {
                        elsa.AddWorkflow<DuplicateActivitiesWorkflow>();
                        elsa.UseMongoDbPersistence(opts => {
                            opts.ConnectionString = "mongodb://localhost:27017";
                            opts.DatabaseName = "IntegrationTests";
                        });
                    });
            }, parameter);
        }
    }

    public class HostBuilderWithDuplicateActivitiesWorkflowAndEntityFrameworkAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBubilderUsingServicesCustomization(services => {
                services
                    .AddElsa(elsa => {
                        elsa
                            .AddWorkflow<DuplicateActivitiesWorkflow>()
                            .UseEntityFrameworkPersistence(opts => {
                                opts.UseSqlite("Data Source=elsa.db;", db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name));
                            });
                    });
            }, parameter);
        }
    }

    public class HostBuilderWithDuplicateActivitiesWorkflowAndYesSqlAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBubilderUsingServicesCustomization(services => {
                services
                    .AddElsa(elsa => {
                        elsa
                            .AddWorkflow<DuplicateActivitiesWorkflow>()
                            .UseYesSqlPersistence(config => {
                                config.UseSqLite("Data Source=elsa-sqlite.db;", IsolationLevel.ReadUncommitted);
                            });
                    });
            }, parameter);
        }
    }
}