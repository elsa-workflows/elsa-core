namespace Elsa.Alterations.Core.Stores;

/// <summary>
/// Shared refuse-to-overwrite exceptions for Memory and EF alteration stores.
/// </summary>
public static class AlterationStoreConflict
{
    public static InvalidOperationException HiddenPlanId(string id) =>
        new($"An alteration plan with ID '{id}' already exists and is not visible to the current tenant.");

    public static InvalidOperationException HiddenJobId(string id) =>
        new($"An alteration job with ID '{id}' already exists and is not visible to the current tenant.");
}
