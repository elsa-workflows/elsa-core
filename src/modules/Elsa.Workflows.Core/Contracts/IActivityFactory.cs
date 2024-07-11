using Elsa.Workflows.Models;

namespace Elsa.Workflows.Contracts;

public interface IActivityFactory
{
    IActivity Create(Type type, ActivityConstructorContext context);
}