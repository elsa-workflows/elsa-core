using Elsa.Dispatch;
using Elsa.Server.Hangfire.Dispatch;

namespace Elsa.Server.Hangfire.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions UseHangfireDispatchers(this ElsaOptions elsaOptions)
        {
            return elsaOptions.UseDispatcher<HangfireWorkflowDispatcher>();
        }
    }
}