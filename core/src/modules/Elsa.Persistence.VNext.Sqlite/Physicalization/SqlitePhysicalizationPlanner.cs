using System.Text.RegularExpressions;
using Elsa.Persistence.VNext;
using Elsa.Persistence.VNext.Physicalization;

namespace Elsa.Persistence.VNext.Sqlite.Physicalization;

public partial class SqlitePhysicalizationPlanner : IPhysicalizationPlanner
{
    public PhysicalizationPlan Plan(PersistenceSchema schema, StoragePhysicalizationPolicy policy)
    {
        if (policy.Target != PhysicalizationTarget.DedicatedRelationalTable)
            throw new InvalidOperationException("SQLite physicalization supports dedicated relational tables only.");

        var storageUnit = PhysicalizationPolicyValidator.GetStorageUnit(schema, policy);
        var tableName = Quote(policy.PhysicalName ?? policy.StorageUnit);
        var fieldColumns = storageUnit.Fields
            .Where(field => field.Name != "Id")
            .Select(field => $"    {Quote(field.Name)} TEXT NULL");
        var columns = string.Join(",\n", fieldColumns);
        var separator = string.IsNullOrWhiteSpace(columns) ? string.Empty : ",\n";
        var operations = new List<PhysicalizationOperation>
        {
            new(
                "CreateTable",
                $"Create dedicated SQLite table {tableName} for {policy.StorageUnit}.",
                $"""
                CREATE TABLE IF NOT EXISTS {tableName} (
                    Id TEXT NOT NULL PRIMARY KEY,
                    Content TEXT NOT NULL,
                    Version INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL{separator}{columns}
                );
                """)
        };

        foreach (var index in policy.Indexes)
        {
            var indexName = Quote(index.Name);
            var indexColumns = string.Join(", ", index.Fields.Select(field => Quote(field)));
            var unique = index.IsUnique ? "UNIQUE " : string.Empty;
            operations.Add(new(
                "CreateIndex",
                $"Create SQLite index {indexName} on {tableName}.",
                $"CREATE {unique}INDEX IF NOT EXISTS {indexName} ON {tableName} ({indexColumns});"));
        }

        return new PhysicalizationPlan("SQLite", policy, operations);
    }

    private static string Quote(string identifier)
    {
        if (!IdentifierRegex().IsMatch(identifier))
            throw new InvalidOperationException($"SQLite identifier '{identifier}' is not valid for physicalization.");

        return $"\"{identifier}\"";
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex IdentifierRegex();
}
