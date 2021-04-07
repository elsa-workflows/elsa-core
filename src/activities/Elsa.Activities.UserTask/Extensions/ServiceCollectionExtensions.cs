namespace Elsa.Activities.UserTask.Extensions
{
    using UserTask = Elsa.Activities.UserTask.Activities.UserTask;
    
    public static class ServiceCollectionExtensions
    {        
        public static ElsaOptionsBuilder AddUserTaskActivities(this ElsaOptionsBuilder services)
        {
            return services.AddActivity<UserTask>();
        }
    }
}