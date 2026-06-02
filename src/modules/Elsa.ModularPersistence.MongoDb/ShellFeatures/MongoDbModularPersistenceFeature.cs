using CShells.Features;
using Elsa.ModularPersistence.MongoDb.Extensions;
using Elsa.ModularPersistence.MongoDb.Options;
using Elsa.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ModularPersistence.MongoDb.ShellFeatures;

[ShellFeature(
    DisplayName = "MongoDB Modular Persistence",
    Description = "Provides MongoDB storage for modular persistence manifests.",
    DependsOn = ["ModularPersistence"])]
[UsedImplicitly]
[ManifestInfrastructure("mongodb-database", "database", Reason = "Stores modular persistence documents in MongoDB.", Providers = new[] { "MongoDB" }, ConfigurationKeys = new[] { "ConnectionString", "DatabaseName" })]
public class MongoDbModularPersistenceFeature : IShellFeature
{
    [ManifestSetting(
        DisplayName = "Connection String",
        Description = "MongoDB connection string used by modular persistence.",
        Category = "Persistence",
        Required = true,
        DefaultValue = "mongodb://localhost:27017",
        Sensitive = true,
        RestartRequired = true)]
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    [ManifestSetting(
        DisplayName = "Database Name",
        Description = "MongoDB database name used by modular persistence.",
        Category = "Persistence",
        Required = true,
        DefaultValue = "ElsaModularPersistence",
        RestartRequired = true)]
    public string DatabaseName { get; set; } = "ElsaModularPersistence";

    [ManifestSetting(
        DisplayName = "Collection Strategy",
        Description = "How modular persistence documents are grouped into MongoDB collections.",
        Category = "Persistence",
        DefaultValue = "SharedCollection",
        RestartRequired = true)]
    public MongoDbCollectionStrategy CollectionStrategy { get; set; } = MongoDbCollectionStrategy.SharedCollection;

    [ManifestSetting(
        DisplayName = "Transaction Mode",
        Description = "MongoDB transaction behavior for document writes.",
        Category = "Persistence",
        DefaultValue = "Disabled",
        RestartRequired = true)]
    public MongoDbTransactionMode TransactionMode { get; set; } = MongoDbTransactionMode.Disabled;

    [ManifestSetting(
        DisplayName = "Shared Collection Name",
        Description = "Collection name used when collection strategy is SharedCollection.",
        Category = "Persistence",
        DefaultValue = "ModularPersistenceDocuments",
        RestartRequired = true)]
    public string SharedCollectionName { get; set; } = "ModularPersistenceDocuments";

    [ManifestSetting(
        DisplayName = "Collection Per Type Prefix",
        Description = "Collection name prefix used when collection strategy is CollectionPerType.",
        Category = "Persistence",
        DefaultValue = "ModularPersistence",
        RestartRequired = true)]
    public string CollectionPerTypePrefix { get; set; } = "ModularPersistence";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMongoDbModularPersistence(options =>
        {
            options.ConnectionString = ConnectionString;
            options.DatabaseName = DatabaseName;
            options.CollectionStrategy = CollectionStrategy;
            options.TransactionMode = TransactionMode;
            options.SharedCollectionName = SharedCollectionName;
            options.CollectionPerTypePrefix = CollectionPerTypePrefix;
        });
    }
}
