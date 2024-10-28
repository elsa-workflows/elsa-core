using Elsa.Extensions;
using Elsa.Retention.Contracts;
using Elsa.Retention.Feature;
using Elsa.Retention.Models;
using Elsa.Retention.Policies;

namespace Elsa.Retention.Extensions;

public static class RetentionFeatureExtensions
{
    private static readonly object PoliciesKey = new();

    /// <summary>
    ///     Adds a policy that will delete workflow instance based on the filter
    /// </summary>
    /// <param name="feature"></param>
    /// <param name="name"></param>
    /// <param name="filterFactory"></param>
    /// <returns></returns>
    public static RetentionFeature AddDeletePolicy(this RetentionFeature feature, string name, Func<IServiceProvider, RetentionWorkflowInstanceFilter> filterFactory)
    {
        List<IRetentionPolicy> policies = feature.Module.Properties.GetOrAdd(PoliciesKey, () => new List<IRetentionPolicy>());
        policies.Add(new DeletionRetentionPolicy(name, filterFactory));
        return feature;
    }

    /// <summary>
    ///     Returns the registered policies
    /// </summary>
    /// <param name="feature"></param>
    /// <returns></returns>
    public static ICollection<IRetentionPolicy> GetPolicies(this RetentionFeature feature)
    {
        return feature.Module.Properties.GetOrAdd(PoliciesKey, () => new List<IRetentionPolicy>());
    }
}