using System;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityActivator
    {
        Task<IActivity> ActivateActivityAsync(ActivityExecutionContext context, Type type);
    }

    public static class ActivityActivatorExtensions
    {
        public static async Task<T> ActivateActivityAsync<T>(this IActivityActivator activityActivator, ActivityExecutionContext context) where T : IActivity => (T) await activityActivator.ActivateActivityAsync(context, typeof(T));
    }
}