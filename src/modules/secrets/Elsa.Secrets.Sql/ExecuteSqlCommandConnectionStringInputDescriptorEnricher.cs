using System;
using Elsa.Activities.Sql.Activities;
using Elsa.Secrets.Enrichers;
using Elsa.Secrets.Providers;

namespace Elsa.Secrets.Sql.Enrichers
{
    public class ExecuteSqlCommandConnectionStringInputDescriptorEnricher : BaseActivityInputDescriptorEnricher
    {
        public ExecuteSqlCommandConnectionStringInputDescriptorEnricher(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public override Type ActivityType => typeof(ExecuteSqlCommand);
        public override string PropertyName => nameof(ExecuteSqlCommand.ConnectionString);

        public override Type OptionsProvider => typeof(SecretConnectionStringOptionsProvider);
    }
}
