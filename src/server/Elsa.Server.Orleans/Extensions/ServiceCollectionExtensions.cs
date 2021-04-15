using Elsa.Dispatch;
using Elsa.Server.Orleans.Dispatch;

namespace Elsa.Server.Orleans.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseOrleansDispatchers(this ElsaOptionsBuilder elsaOptions) => elsaOptions.UseDispatcher<GrainWorkflowDispatcher>();
    }
}