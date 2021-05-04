using Elsa.Testing.Shared.Helpers;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using System;
using Elsa.Persistence.YesSql;
using YesSql.Provider.Sqlite;
using System.Data;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithSqliteYesSqlAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            var tempFolder = new TemporaryFolder();

            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.UseYesSqlPersistence(config => {
                        config.UseSqLite($"Data Source={tempFolder.GetContainedPath("elsa.db")};", IsolationLevel.ReadUncommitted);
                    });
                });
            };
        }
    }
}