using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;

namespace Elsa.Dsl.Contracts;

public interface IFunctionActivityRegistry
{
    void RegisterFunction(string functionName, string activityTypeName, IEnumerable<string>? propertyNames = default);
    IActivity ResolveFunction(string functionName, IEnumerable<object?>? arguments = default);
}