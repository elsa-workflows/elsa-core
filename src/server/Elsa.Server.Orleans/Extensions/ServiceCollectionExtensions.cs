using Elsa.Dispatch;
using Elsa.Server.Orleans.Dispatch;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Server.Orleans.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions UseOrleansDispatchers(this ElsaOptions elsaOptions) => elsaOptions.UseDispatcher<GrainWorkflowDispatcher>();
    }
}