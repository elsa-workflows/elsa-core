using System.Net.Http;

namespace Elsa.ComponentTests.Helpers
{
    public abstract class TestBase : WorkflowsComponentTestBase
    {
        protected TestBase(ElsaHostApplicationFactory hostApplicationFactory) : base(hostApplicationFactory)
        {
            HttpClient = hostApplicationFactory.CreateClient();
        }
        
        protected HttpClient HttpClient { get; }

        public override void Dispose()
        {
            HttpClient.Dispose();
            
            base.Dispose();
        }
    }
}