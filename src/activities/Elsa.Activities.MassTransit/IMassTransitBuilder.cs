using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.MassTransit
{
    public interface IMassTransitBuilder<TOptions> where TOptions : class
    {
        IServiceCollection Build(IServiceCollection services);
    }
}