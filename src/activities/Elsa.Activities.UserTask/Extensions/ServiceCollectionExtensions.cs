using Elsa.Activities.UserTask.Bookmarks;
using Elsa.Activities.UserTask.Contracts;
using Elsa.Activities.UserTask.Services;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.UserTask.Extensions
{
    using UserTask = Elsa.Activities.UserTask.Activities.UserTask;
    
    public static class ServiceCollectionExtensions
    {        
        public static ElsaOptionsBuilder AddUserTaskActivities(this ElsaOptionsBuilder elsa)
        {
            elsa.Services
                .AddScoped<IUserTaskService, UserTaskService>()
                .AddBookmarkProvider<UserTaskBookmarkProvider>();
            
            return elsa.AddActivity<UserTask>();
        }
    }
}