using Elsa.Server.Orleans.Dispatch;
using Elsa.Services.Dispatch;

namespace Elsa.Server.Orleans.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseOrleansDispatchers(this ElsaOptionsBuilder elsaOptions) => elsaOptions.UseDispatcher<GrainWorkflowDispatcher>();
    }
}