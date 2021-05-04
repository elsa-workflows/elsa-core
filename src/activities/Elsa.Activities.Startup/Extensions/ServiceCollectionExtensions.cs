using Elsa.Activities.Startup.HostedServices;
using Elsa.Runtime;

namespace Elsa.Activities.Startup.Extensions
{
    public static class ServiceCollectionExtensions
    {        
        public static ElsaOptions AddStartupActivities(this ElsaOptions options)
        {
            options.Services.AddStartupTask<RunStartupWorkflows>();
            options.AddActivity<Activities.Startup>();

            return options;
        }
    }
}