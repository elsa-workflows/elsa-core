using Elsa.Activities.UserTask.Bookmarks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.UserTask.Extensions
{
    using UserTask = Elsa.Activities.UserTask.Activities.UserTask;
    
    public static class ServiceCollectionExtensions
    {        
        public static ElsaOptionsBuilder AddUserTaskActivities(this ElsaOptionsBuilder elsa)
        {
            elsa.Services.AddBookmarkProvider<UserTaskBookmarkProvider>();
            return elsa.AddActivity<UserTask>();
        }
    }
}