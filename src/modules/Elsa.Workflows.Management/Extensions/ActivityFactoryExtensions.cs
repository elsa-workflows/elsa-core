using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="IActivityFactory"/>.
/// </summary>
public static class ActivityFactoryExtensions
{
    /// <summary>
    /// Creates an activity of the specified type.
    /// </summary>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="context">The activity constructor context.</param>
    /// <typeparam name="T">The type of the activity to create.</typeparam>
    /// <returns>The created activity.</returns>
    public static T Create<T>(this IActivityFactory activityFactory, ActivityConstructorContext context) => (T)activityFactory.Create(typeof(T), context);
}