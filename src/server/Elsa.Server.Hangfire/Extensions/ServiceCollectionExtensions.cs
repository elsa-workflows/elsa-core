using Elsa.Options;
using Elsa.Server.Hangfire.Dispatch;
using Elsa.Services.Dispatch;

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