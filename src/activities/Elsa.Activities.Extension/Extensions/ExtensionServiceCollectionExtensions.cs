using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.UserTask.Extensions
{
    using ApprovalTask = Elsa.Activities.UserTask.Activities.ApprovalTask;
    using Applicant = Elsa.Activities.UserTask.Activities.Applicant;
    using Notity = Elsa.Activities.UserTask.Activities.Notify;
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApprovalTaskActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<Applicant>()
                .AddActivity<ApprovalTask>()
                .AddActivity<Notity>();
        }
    }
}