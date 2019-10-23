using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.UserTask.Extensions
{
    using UserTask = Elsa.Activities.UserTask.Activities.UserTask;
    
    public static class ServiceCollectionExtensions
    {        
        public static IServiceCollection AddUserTaskActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<UserTask>();
        }
    }
}