namespace Elsa.Workflows.Api.ComponentTests;

[CollectionDefinition(nameof(WebAppCollection))]
public class WebAppCollection : ICollectionFixture<WorkflowServerTestWebAppFactoryFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}