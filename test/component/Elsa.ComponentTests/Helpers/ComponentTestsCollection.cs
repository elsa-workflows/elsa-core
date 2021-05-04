using Xunit;

namespace Elsa.ComponentTests.Helpers
{
    [CollectionDefinition(Name)]
    public class ComponentTestsCollection : ICollectionFixture<ElsaHostApplicationFactory>
    {
        public const string Name = "Component Tests";
    }
}
