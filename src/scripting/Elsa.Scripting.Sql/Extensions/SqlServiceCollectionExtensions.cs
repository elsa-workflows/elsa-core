
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Options;
using Elsa.Scripting.Sql.Expressions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqlServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddSqlScripting(this ElsaOptionsBuilder elsaOptions)
        {
            var services = elsaOptions.ContainerBuilder;

            services.TryAddProvider<IExpressionHandler, SqlHandler>(ServiceLifetime.Singleton);
            
            return elsaOptions;
        }

    }
}