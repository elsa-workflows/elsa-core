using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

public interface IActivityFactory
{
    IActivity Create(Type type, ActivityConstructorContext context);
}