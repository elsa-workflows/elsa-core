using System;
using Elsa.Activities.Sql.Activities;
using Elsa.Secrets.Providers;

namespace Elsa.Secrets.Enrichers
{
    public class ExecuteSqlQueryConnectionStringInputDescriptorEnricher : BaseActivityInputDescriptorEnricher
    {
        public ExecuteSqlQueryConnectionStringInputDescriptorEnricher(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override Type ActivityType => typeof(ExecuteSqlQuery);
        public override string PropertyName => nameof(ExecuteSqlQuery.ConnectionString);

        public override Type OptionsProvider => typeof(SecretConnectionStringOptionsProvider);
    }
}
