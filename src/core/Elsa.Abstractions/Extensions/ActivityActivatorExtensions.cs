using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class ActivityActivatorExtensions
    {
        public static async Task<T> ActivateActivityAsync<T>(this IActivityActivator activator, ActivityExecutionContext context) where T : IActivity => (T) await activator.ActivateActivityAsync(context, typeof(T));
    }
}