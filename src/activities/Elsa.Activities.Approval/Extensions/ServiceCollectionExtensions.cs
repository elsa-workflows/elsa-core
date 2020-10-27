using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.ApprovalTask.Extensions
{
    using ApprovalTask = Elsa.Activities.ApprovalTask.Activities.ApprovalTask;
    
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddApprovalTaskActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<ApprovalTask>();
        }
    }
}