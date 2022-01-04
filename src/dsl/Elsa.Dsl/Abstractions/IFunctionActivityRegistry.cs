using System.Collections.Generic;
using Elsa.Contracts;

namespace Elsa.Dsl.Abstractions;

public interface IFunctionActivityRegistry
{
    void RegisterFunction(string functionName, string activityTypeName, IEnumerable<string>? propertyNames = default);
    IActivity ResolveFunction(string functionName, IEnumerable<object?>? arguments = default);
}