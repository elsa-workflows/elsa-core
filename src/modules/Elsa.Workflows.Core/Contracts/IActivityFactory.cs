using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public interface IActivityFactory
{
    IActivity Create(Type type, ActivityConstructorContext context);
}