using System.Globalization;
using System.Text.Json;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.MongoDb.Options;
using Elsa.ModularPersistence.Queries;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.ModularPersistence.MongoDb.Services;

/// <summary>
/// Executes MongoDB document operations.
/// </summary>
public sealed class MongoDbDocumentSession(
    MongoClient client,
    IMongoDatabase database,
    MongoDbCollectionResolver collectionResolver,
    MongoDbModularPersistenceOptions options,
    StorageManifestDescriptor manifest) : IDocumentSession
{
    private static readonly DocumentQueryCapabilities QueryCapabilities = DocumentQueryCapabilities.Portable;

    public async ValueTask<DocumentEnvelope?> LoadAsync(DocumentKey key, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(key.Type);
        var document = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", BuildMongoId(key))).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ReadDocument(document);
    }

    public async ValueTask<DocumentSaveResult> SaveAsync(DocumentEnvelope document, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default)
    {
        GetStorageUnit(document.Type);

        var collection = GetCollection(document.Type);
        var mongoDocument = CreateMongoDocument(document);

        await ExecuteWriteAsync(async session =>
        {
            switch (expectedVersion.Kind)
            {
                case ExpectedDocumentVersionKind.Any:
                    await ReplaceAnyAsync(session, collection, mongoDocument, document.Key, cancellationToken);
                    break;
                case ExpectedDocumentVersionKind.New:
                    await InsertNewAsync(session, collection, mongoDocument, document.Key, cancellationToken);
                    break;
                case ExpectedDocumentVersionKind.Exact:
                    await ReplaceExactAsync(session, collection, mongoDocument, document.Key, expectedVersion.Version!.Value, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(expectedVersion), expectedVersion.Kind, "Unknown expected document version kind.");
            }
        }, cancellationToken);

        return new DocumentSaveResult(document.Key, document.Version);
    }

    public async ValueTask<IReadOnlyCollection<DocumentEnvelope>> QueryAsync(DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var planner = new DocumentQueryPlanner();
        var plan = planner.Plan(manifest, query, QueryCapabilities);
        if (!plan.IsExecutable)
            throw new DocumentQueryException(plan, "Document query failed planning.");

        var storageUnit = plan.StorageUnit!;
        var collection = GetCollection(query.DocumentType);
        var filter = BuildQueryFilter(storageUnit, query);
        var find = collection.Find(filter);

        var sort = BuildSort(storageUnit, query);
        if (sort is not null)
            find = find.Sort(sort);
        else if (query.Page is not null)
            find = find.Sort(Builders<BsonDocument>.Sort.Ascending("Id"));

        if (query.Page is not null)
            find = find.Skip(query.Page.Offset).Limit(query.Page.Limit);

        var documents = await find.ToListAsync(cancellationToken);
        return documents.Select(ReadDocument).ToArray();
    }

    public async ValueTask DeleteAsync(DocumentKey key, ExpectedDocumentVersion expectedVersion = default, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(key.Type);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", BuildMongoId(key));

        switch (expectedVersion.Kind)
        {
            case ExpectedDocumentVersionKind.Any:
                break;
            case ExpectedDocumentVersionKind.New:
                throw new DocumentConcurrencyException(key, "Delete cannot require a new document.");
            case ExpectedDocumentVersionKind.Exact:
                filter &= Builders<BsonDocument>.Filter.Eq("Version", expectedVersion.Version);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(expectedVersion), expectedVersion.Kind, "Unknown expected document version kind.");
        }

        await ExecuteWriteAsync(async session =>
        {
            var result = session is null
                ? await collection.DeleteOneAsync(filter, cancellationToken)
                : await collection.DeleteOneAsync(session, filter, null, cancellationToken);

            if (expectedVersion.Kind == ExpectedDocumentVersionKind.Exact && result.DeletedCount == 0)
                throw new DocumentConcurrencyException(key, $"Document '{key.Id}' version did not match expected version {expectedVersion.Version}.");
        }, cancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private StorageUnitDescriptor GetStorageUnit(string documentType) =>
        manifest.StorageUnits.SingleOrDefault(x => x.Name == documentType)
        ?? throw new InvalidOperationException($"No storage unit named '{documentType}' is declared by manifest '{manifest.SchemaName}'.");

    private IMongoCollection<BsonDocument> GetCollection(string documentType) =>
        database.GetCollection<BsonDocument>(collectionResolver.GetCollectionName(documentType));

    private async ValueTask ExecuteWriteAsync(Func<IClientSessionHandle?, Task> operation, CancellationToken cancellationToken)
    {
        if (options.TransactionMode == MongoDbTransactionMode.Disabled)
        {
            await operation(null);
            return;
        }

        using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();

        try
        {
            await operation(session);
            await session.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static async ValueTask ReplaceAnyAsync(
        IClientSessionHandle? session,
        IMongoCollection<BsonDocument> collection,
        BsonDocument document,
        DocumentKey key,
        CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", BuildMongoId(key));
        var options = new ReplaceOptions { IsUpsert = true };

        if (session is null)
            await collection.ReplaceOneAsync(filter, document, options, cancellationToken);
        else
            await collection.ReplaceOneAsync(session, filter, document, options, cancellationToken);
    }

    private static async ValueTask InsertNewAsync(IClientSessionHandle? session, IMongoCollection<BsonDocument> collection, BsonDocument document, DocumentKey key, CancellationToken cancellationToken)
    {
        try
        {
            if (session is null)
                await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
            else
                await collection.InsertOneAsync(session, document, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException e) when (e.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DocumentConcurrencyException(key, $"Document '{key.Id}' already exists.");
        }
    }

    private static async ValueTask ReplaceExactAsync(
        IClientSessionHandle? session,
        IMongoCollection<BsonDocument> collection,
        BsonDocument document,
        DocumentKey key,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", BuildMongoId(key)) & Builders<BsonDocument>.Filter.Eq("Version", expectedVersion);
        var result = session is null
            ? await collection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken)
            : await collection.ReplaceOneAsync(session, filter, document, cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
            throw new DocumentConcurrencyException(key, $"Document '{key.Id}' version did not match expected version {expectedVersion}.");
    }

    private static BsonDocument CreateMongoDocument(DocumentEnvelope document)
    {
        BsonValue metadata = document.Metadata.Count == 0
            ? BsonNull.Value
            : new BsonDocument(document.Metadata.Select(x => new BsonElement(x.Key, x.Value)));

        return new BsonDocument
        {
            ["_id"] = BuildMongoId(document.Key),
            ["Id"] = document.Id,
            ["Type"] = document.Type,
            ["TenantId"] = NormalizeTenantId(document.TenantId),
            ["Version"] = document.Version,
            ["CreatedAt"] = document.CreatedAt.ToString("O"),
            ["UpdatedAt"] = document.UpdatedAt.ToString("O"),
            ["DataJson"] = document.Data,
            ["Data"] = BsonDocument.Parse(document.Data),
            ["Metadata"] = metadata
        };
    }

    private static FilterDefinition<BsonDocument> BuildQueryFilter(StorageUnitDescriptor storageUnit, DocumentQuery query)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("Type", query.DocumentType);
        if (query.TenantId is not null)
            filter &= Builders<BsonDocument>.Filter.Eq("TenantId", NormalizeTenantId(query.TenantId));

        foreach (var queryFilter in query.Filters)
        {
            var field = storageUnit.Fields.Single(x => x.Name == queryFilter.FieldName);
            filter &= BuildFilterCondition(queryFilter, field);
        }

        return filter;
    }

    private static FilterDefinition<BsonDocument> BuildFilterCondition(DocumentQueryFilter filter, StorageFieldDescriptor field)
    {
        var fieldPath = GetDataFieldPath(field.Name);
        var values = filter.Values.Select(x => ConvertQueryValue(x, field.Type)).ToArray();

        return filter.Operator switch
        {
            DocumentQueryFilterOperator.Equals => Builders<BsonDocument>.Filter.Eq(fieldPath, values[0]),
            DocumentQueryFilterOperator.NotEquals => Builders<BsonDocument>.Filter.Ne(fieldPath, values[0]),
            DocumentQueryFilterOperator.In => Builders<BsonDocument>.Filter.In(fieldPath, values),
            DocumentQueryFilterOperator.GreaterThan => Builders<BsonDocument>.Filter.Gt(fieldPath, values[0]),
            DocumentQueryFilterOperator.GreaterThanOrEqual => Builders<BsonDocument>.Filter.Gte(fieldPath, values[0]),
            DocumentQueryFilterOperator.LessThan => Builders<BsonDocument>.Filter.Lt(fieldPath, values[0]),
            DocumentQueryFilterOperator.LessThanOrEqual => Builders<BsonDocument>.Filter.Lte(fieldPath, values[0]),
            DocumentQueryFilterOperator.Between => Builders<BsonDocument>.Filter.Gte(fieldPath, values[0]) & Builders<BsonDocument>.Filter.Lte(fieldPath, values[1]),
            DocumentQueryFilterOperator.IsNull => Builders<BsonDocument>.Filter.Exists(fieldPath, false) | Builders<BsonDocument>.Filter.Eq(fieldPath, BsonNull.Value),
            DocumentQueryFilterOperator.IsNotNull => Builders<BsonDocument>.Filter.Exists(fieldPath) & Builders<BsonDocument>.Filter.Ne(fieldPath, BsonNull.Value),
            DocumentQueryFilterOperator.StartsWith => throw new NotSupportedException("MongoDB document queries do not support starts-with in the portable MVP."),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter.Operator, "Unknown query filter operator.")
        };
    }

    private static SortDefinition<BsonDocument>? BuildSort(StorageUnitDescriptor storageUnit, DocumentQuery query)
    {
        if (query.Sorts.Count == 0)
            return null;

        var sorts = query.Sorts.Select(sort =>
        {
            var field = storageUnit.Fields.Single(x => x.Name == sort.FieldName);
            var fieldPath = GetDataFieldPath(field.Name);
            return sort.SortOrder == StorageIndexSortOrder.Descending
                ? Builders<BsonDocument>.Sort.Descending(fieldPath)
                : Builders<BsonDocument>.Sort.Ascending(fieldPath);
        });

        return Builders<BsonDocument>.Sort.Combine(sorts);
    }

    private static BsonValue ConvertQueryValue(DocumentQueryValue value, StorageFieldType fieldType) =>
        fieldType switch
        {
            StorageFieldType.String or StorageFieldType.Guid or StorageFieldType.Json or StorageFieldType.Binary => value.TextValue is null ? BsonNull.Value : new BsonString(value.TextValue),
            StorageFieldType.Int32 or StorageFieldType.Int64 or StorageFieldType.Decimal => value.NumberValue is null ? BsonNull.Value : new BsonDouble(decimal.ToDouble(value.NumberValue.Value)),
            StorageFieldType.Boolean => value.BooleanValue is null ? BsonNull.Value : new BsonBoolean(value.BooleanValue.Value),
            StorageFieldType.DateTimeOffset => value.DateTimeValue is null ? BsonNull.Value : new BsonString(value.DateTimeValue.Value.ToString("O")),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Unknown storage field type.")
        };

    private static DocumentEnvelope ReadDocument(BsonDocument document)
    {
        var tenantId = document["TenantId"].AsString;
        var metadata = ReadMetadata(document);

        return new DocumentEnvelope(
            document["Id"].AsString,
            document["Type"].AsString,
            string.IsNullOrEmpty(tenantId) ? null : tenantId,
            document["Version"].ToInt64(),
            DateTimeOffset.Parse(document["CreatedAt"].AsString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(document["UpdatedAt"].AsString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            document["DataJson"].AsString,
            metadata);
    }

    private static IReadOnlyDictionary<string, string>? ReadMetadata(BsonDocument document)
    {
        if (!document.TryGetValue("Metadata", out var metadata) || !metadata.IsBsonDocument)
            return null;

        return metadata.AsBsonDocument.Elements.ToDictionary(x => x.Name, x => x.Value.AsString, StringComparer.Ordinal);
    }

    private static string GetDataFieldPath(string fieldName) => $"Data.{fieldName}";

    private static string BuildMongoId(DocumentKey key) => $"{key.Type}\u001f{NormalizeTenantId(key.TenantId)}\u001f{key.Id}";

    private static string NormalizeTenantId(string? tenantId) => tenantId ?? "";
}
