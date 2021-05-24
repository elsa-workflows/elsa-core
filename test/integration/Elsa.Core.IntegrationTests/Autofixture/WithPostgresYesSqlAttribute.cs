using System;
using Elsa.Persistence.YesSql;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;
using YesSql.Provider.PostgreSql;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithPostgresYesSqlAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.UseYesSqlPersistence((_, config) => {
                        config.UsePostgreSql("Server=localhost;Port=5432;Database=elsa;User Id=root;Password=Password12!;");
                    });
                });
            };
        }
    }
}