using Elsa.Workflows.Models;

namespace Elsa.Workflows;

[Obsolete("Use the CreateActivity method on the ActivityConstructorContext context itself")]
public interface IActivityFactory
{
    [Obsolete("Use the CreateActivity method on the ActivityConstructorContext context itself")]
    IActivity Create(Type type, ActivityConstructorContext context);
}

[Obsolete("Use the CreateActivity method on the ActivityConstructorContext context itself")]
public static class ActivityFactoryExtensions
{
    [Obsolete("Use the CreateActivity method on the ActivityConstructorContext context itself")]
    public static IActivity CreateActivity<T>(this IActivityFactory factory, ActivityConstructorContext context)
    {
        return factory.Create(typeof(T), context);
    }
}