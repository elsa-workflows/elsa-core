using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityActivator
    {
        Task<IActivity> ActivateActivityAsync(ActivityExecutionContext context, Type type, CancellationToken cancellationToken = default);
    }

    public static class ActivityActivatorExtensions
    {
        public static async Task<T> ActivateActivityAsync<T>(this IActivityActivator activityActivator, ActivityExecutionContext context, CancellationToken cancellationToken = default) where T : IActivity =>
            (T) await activityActivator.ActivateActivityAsync(context, typeof(T), cancellationToken);
    }
}