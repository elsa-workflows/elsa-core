using Elsa.Workflows.Models;

namespace Elsa.Workflows;

[Obsolete("Use the CreateActivity method on the ActivityConstructorContext context itself")]
public interface IActivityFactory
{
    IActivity Create(Type type, ActivityConstructorContext context);
}