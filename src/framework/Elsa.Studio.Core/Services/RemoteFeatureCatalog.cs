using Elsa.Api.Client.Resources.Features.Models;

namespace Elsa.Studio.Services;

/// <summary>
/// Matches Studio remote feature names against the backend installed-feature catalog.
/// </summary>
/// <remarks>
/// Dynamic/shell catalogs advertise the ShellFeatures CLR name
/// (<c>Elsa.*.ShellFeatures.{FeatureId}</c>). Static feature catalogs advertise the
/// CShells feature id as <c>Elsa.{FeatureId}</c> (for example
/// <c>Elsa.WorkflowRuntimeDashboard</c>). Studio modules request the ShellFeatures name,
/// so both catalog forms must resolve as the same installed feature.
/// Short-name fallback is restricted to the Elsa namespace so an unrelated
/// <c>Acme.{FeatureId}</c> catalog entry cannot enable an Elsa Studio module.
/// </remarks>
internal static class RemoteFeatureCatalog
{
    public static bool Contains(IEnumerable<FeatureDescriptor> catalog, string featureName) =>
        catalog.Any(feature => Matches(feature, featureName));

    public static bool Matches(FeatureDescriptor feature, string featureName)
    {
        if (string.Equals(feature.FullName, featureName, StringComparison.Ordinal))
            return true;

        var requestedId = GetFeatureId(featureName);

        if (!string.IsNullOrEmpty(feature.Name) &&
            string.Equals(feature.Namespace, "Elsa", StringComparison.Ordinal) &&
            string.Equals(feature.Name, requestedId, StringComparison.Ordinal))
            return true;

        return string.Equals(feature.FullName, $"Elsa.{requestedId}", StringComparison.Ordinal);
    }

    private static string GetFeatureId(string featureName)
    {
        var lastDot = featureName.LastIndexOf('.');
        return lastDot < 0 ? featureName : featureName[(lastDot + 1)..];
    }
}
