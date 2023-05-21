using System.Linq.Expressions;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IActivityDescriber"/>.
/// </summary>
[PublicAPI]
public static class ActivityDescriberExtensions
{
    /// <summary>
    /// Describes an output property.
    /// </summary>
    /// <param name="activityDescriber">The activity describer.</param>
    /// <param name="expression">The property expression.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TActivity">The type of the activity.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <returns>The output descriptor.</returns>
    public static async Task<OutputDescriptor> DescribeOutputProperty<TActivity, TProperty>(this IActivityDescriber activityDescriber, Expression<Func<TActivity, TProperty>> expression, CancellationToken cancellationToken = default)
    {
        var propertyInfo = expression.GetProperty()!;
        return await activityDescriber.DescribeOutputPropertyAsync(propertyInfo, cancellationToken);
    }

    /// <summary>
    /// Describes an input property.
    /// </summary>
    /// <param name="activityDescriber">The activity describer.</param>
    /// <param name="expression">The property expression.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TActivity">The type of the activity.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <returns>The input descriptor.</returns>
    public static async Task<InputDescriptor> DescribeInputPropertyAsync<TActivity, TProperty>(this IActivityDescriber activityDescriber, Expression<Func<TActivity, TProperty>> expression, CancellationToken cancellationToken = default)
    {
        var propertyInfo = expression.GetProperty()!;
        return await activityDescriber.DescribeInputPropertyAsync(propertyInfo, cancellationToken);
    }
}