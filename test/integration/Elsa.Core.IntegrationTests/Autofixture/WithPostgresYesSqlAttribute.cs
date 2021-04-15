using System;
using Elsa.Persistence.YesSql;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;
using YesSql;
using YesSql.Provider.PostgreSql;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithPostgresYesSqlAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        /// <summary>
        /// This password matches the password which is used by AppVeyor: https://www.appveyor.com/docs/services-databases/#postgresql
        /// </summary>
        /// <remarks>
        /// <para>
        /// We could do something more sophisticated with the password than keep it in a constant, but since
        /// this is (so far) the only place where we configure Postgres.  There aren't any plans to do anything
        /// with it in the foreseeable future either, so we can defer that until then.
        /// </para>
        /// </remarks>
        internal const string PostgresPassword = "Password12!";

        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.UseYesSqlPersistence((IServiceProvider services, IConfiguration config) => {
                        config.UsePostgreSql($"Host=localhost;Database=elsa-yessql;User ID=postgres;Password={PostgresPassword}");
                    });
                });
            };
        }
    }
}