using Elsa.Activities.AzureServiceBus.Extensions;
using Elsa.Attributes;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.AzureServiceBus
{
    [Feature("Azure:ServiceBus")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var section = configuration.GetSection("Elsa:Azure:ServiceBus");

            elsa.AddAzureServiceBusActivities(options =>
            {
                section.Bind(options);

                if (!string.IsNullOrWhiteSpace(options.ConnectionString)) 
                    return;
                
                var connectionStringName = section.GetValue<string>("ConnectionStringName");
                options.ConnectionString = configuration.GetConnectionString(connectionStringName);
            });
        }
    }
}
