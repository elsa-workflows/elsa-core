using System.Collections.Generic;
using Elsa.Workflows.Core.Services;

namespace Elsa.Dsl.Services;

public interface IFunctionActivityRegistry
{
    void RegisterFunction(string functionName, string activityTypeName, IEnumerable<string>? propertyNames = default);
    IActivity ResolveFunction(string functionName, IEnumerable<object?>? arguments = default);
}