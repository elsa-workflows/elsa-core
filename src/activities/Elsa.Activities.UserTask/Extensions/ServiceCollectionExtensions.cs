namespace Elsa.Activities.UserTask.Extensions
{
    using UserTask = Elsa.Activities.UserTask.Activities.UserTask;
    
    public static class ServiceCollectionExtensions
    {        
        public static ElsaOptions AddUserTaskActivities(this ElsaOptions services)
        {
            return services.AddActivity<UserTask>();
        }
    }
}