using Elsa.Persistence.VNext;
using Elsa.Persistence.VNext.Physicalization;

namespace Elsa.Persistence.VNext.MongoDb.Physicalization;

public class MongoDbPhysicalizationPlanner : IPhysicalizationPlanner
{
    public PhysicalizationPlan Plan(PersistenceSchema schema, StoragePhysicalizationPolicy policy)
    {
        if (policy.Target != PhysicalizationTarget.DedicatedDocumentCollection)
            throw new InvalidOperationException("MongoDB physicalization supports dedicated document collections only.");

        PhysicalizationPolicyValidator.GetStorageUnit(schema, policy);

        var collectionName = NormalizeName(policy.PhysicalName ?? policy.StorageUnit);
        var operations = new List<PhysicalizationOperation>
        {
            new("CreateCollection", $"Create dedicated MongoDB collection {collectionName} for {policy.StorageUnit}.")
        };

        foreach (var index in policy.Indexes)
        {
            var fields = string.Join(",", index.Fields.Select(field => $"Data.{field}:1"));
            var uniqueness = index.IsUnique ? "unique" : "non-unique";
            operations.Add(new(
                "CreateIndex",
                $"Create {uniqueness} MongoDB index {index.Name} on {collectionName} ({fields})."));
        }

        return new PhysicalizationPlan("MongoDB", policy, operations);
    }

    private static string NormalizeName(string value)
    {
        var chars = value.Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_').ToArray();
        return new string(chars);
    }
}
