using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Extensions;

public static class ActivityFactoryExtensions
{
    public static T Create<T>(this IActivityFactory activityFactory, ActivityConstructorContext context) => (T)activityFactory.Create(typeof(T), context);
}