using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityFactory : IActivityFactory
{
    /// <inheritdoc />
    public ActivityConstructionResult Create(Type type, ActivityConstructorContext context)
    {
        return context.CreateActivity(type);
    }
}