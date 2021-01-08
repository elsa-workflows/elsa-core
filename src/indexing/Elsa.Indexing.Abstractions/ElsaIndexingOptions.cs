using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Indexing
{
    public class ElsaIndexingOptions
    {
        public ElsaIndexingOptions(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
