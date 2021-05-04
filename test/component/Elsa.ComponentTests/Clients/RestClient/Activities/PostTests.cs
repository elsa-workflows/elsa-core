using System.Threading.Tasks;
using Elsa.ComponentTests.Helpers;
using Xunit;

namespace Elsa.ComponentTests.Clients.RestClient.Activities
{
    [Collection(ComponentTestsCollection.Name)]
    public class ListTests : ElsaClientTestBase
    {
        public ListTests(ElsaHostApplicationFactory hostApplicationFactory) : base(hostApplicationFactory)
        {
        }

        [Fact(DisplayName = "Listing activities returns a list of activities.")]
        public async Task Post01()
        {
            var list = await ElsaClient.Activities.ListAsync();
            Assert.NotEmpty(list);
        }
    }
}