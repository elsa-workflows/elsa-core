using Elsa.Client;
using Elsa.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ComponentTests.Helpers
{
    public abstract class ElsaClientTestBase : TestBase
    {
        protected ElsaClientTestBase(ElsaHostApplicationFactory hostApplicationFactory) : base(hostApplicationFactory)
        {
            var services = new ServiceCollection().AddElsaClient(options => options.ServerUrl = HttpClient.BaseAddress!, () => HttpClient).BuildServiceProvider();
            ElsaClient = services.GetRequiredService<IElsaClient>();
        }
        
        protected IElsaClient ElsaClient { get; }
    }
}