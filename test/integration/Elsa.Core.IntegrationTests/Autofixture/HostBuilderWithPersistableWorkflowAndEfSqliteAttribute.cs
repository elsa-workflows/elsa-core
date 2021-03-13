using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Elsa.Testing.Shared.AutoFixture.Customizations;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Elsa.Persistence.EntityFramework.Sqlite;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class HostBuilderWithPersistableWorkflowAndEfSqliteAttribute : CustomizeAttribute
    {
        public override ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new HostBubilderUsingServicesCustomization(services => {
                services
                    .AddElsa(elsa => {
                        elsa
                            .UseEntityFrameworkPersistence(opts => {
                                opts.UseSqlite("Data Source=elsa.db;", db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name));
                            })
                            .AddPersistableWorkflow();
                    });
            }, parameter);
        }
    }
}