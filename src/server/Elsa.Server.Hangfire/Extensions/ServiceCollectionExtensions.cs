using Elsa.Dispatch;
using Elsa.Server.Hangfire.Dispatch;

namespace Elsa.Server.Hangfire.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseHangfireDispatchers(this ElsaOptionsBuilder elsaOptions)
        {
            return elsaOptions.UseDispatcher<HangfireWorkflowDispatcher>();
        }
    }
}