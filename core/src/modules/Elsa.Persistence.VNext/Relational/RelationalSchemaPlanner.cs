using Elsa.Persistence.VNext.Contracts;

namespace Elsa.Persistence.VNext.Relational;

public class RelationalSchemaPlanner(IRelationalTypeMapper typeMapper) : IPersistenceSchemaPlanner<RelationalSchemaPlan>
{
    public RelationalSchemaPlan Plan(PersistenceSchema schema)
    {
        var tables = new List<RelationalTable>();
        var indexes = new List<RelationalIndex>();

        foreach (var table in schema.Tables)
        {
            tables.Add(PlanTable(table));
            indexes.AddRange(table.Indexes.Select(index => PlanIndex(table, index)));
        }

        foreach (var storageUnit in schema.StorageUnits)
        {
            tables.Add(PlanTable(storageUnit));
            indexes.AddRange(storageUnit.Indexes.Select(index => PlanIndex(storageUnit, index)));
        }

        return new RelationalSchemaPlan(tables, indexes);
    }

    private RelationalTable PlanTable(PersistenceTable table)
    {
        var columns = table.Columns
            .Select(column => new RelationalColumn(column.Name, typeMapper.Map(column), column.IsNullable))
            .ToList();
        var primaryKey = table.PrimaryKey is null ? null : new RelationalPrimaryKey(table.PrimaryKey.Name, table.PrimaryKey.Columns);

        return new RelationalTable(table.Name, table.Schema, columns, primaryKey);
    }

    private static RelationalIndex PlanIndex(PersistenceTable table, PersistenceIndex index)
    {
        return new RelationalIndex(index.Name, table.Name, table.Schema, index.Columns, index.IsUnique);
    }

    private RelationalTable PlanTable(PersistenceStorageUnit storageUnit)
    {
        var columns = storageUnit.Fields
            .Select(field =>
            {
                var column = new PersistenceColumn(field.Name, field.Type, field.IsNullable, field.Length);
                return new RelationalColumn(field.Name, typeMapper.Map(column), field.IsNullable);
            })
            .ToList();
        var primaryKey = storageUnit.Key is null ? null : new RelationalPrimaryKey(storageUnit.Key.Name, storageUnit.Key.Columns);

        return new RelationalTable(storageUnit.Name, storageUnit.Namespace, columns, primaryKey);
    }

    private static RelationalIndex PlanIndex(PersistenceStorageUnit storageUnit, PersistenceIndex index)
    {
        return new RelationalIndex(index.Name, storageUnit.Name, storageUnit.Namespace, index.Columns, index.IsUnique);
    }
}
