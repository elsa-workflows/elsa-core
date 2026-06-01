using Elsa.ModularPersistence.Relational.IntegrationTests.Contracts;
using Elsa.ModularPersistence.Relational.IntegrationTests.Providers;

namespace Elsa.ModularPersistence.Relational.IntegrationTests.ProviderTests;

public class SqliteDocumentSchemaMaterializerContractTests : DocumentSchemaMaterializerContractTests
{
    protected override IRelationalProviderFixture CreateFixture() => new SqliteProviderFixture();
}
