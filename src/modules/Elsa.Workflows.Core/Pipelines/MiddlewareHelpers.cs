using System.Reflection;

namespace Elsa.Workflows.Core.Pipelines;

public static class MiddlewareHelpers
{
    public static MethodInfo GetInvokeMethod(Type middleware)
    {
        const string invokeMethodName = "Invoke";
        const string invokeAsyncMethodName = "InvokeAsync";
        var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        var invokeMethods = methods.Where(m => string.Equals(m.Name, invokeMethodName, StringComparison.Ordinal) || string.Equals(m.Name, invokeAsyncMethodName, StringComparison.Ordinal)).ToArray();

        switch (invokeMethods.Length)
        {
            case > 1:
                throw new InvalidOperationException("Multiple Invoke methods were found. Use either Invoke or InvokeAsync.");
            case 0:
                throw new InvalidOperationException("No Invoke methods were found. Use either Invoke or InvokeAsync");
        }

        var methodInfo = invokeMethods[0];

        if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType) && !typeof(ValueTask).IsAssignableFrom(methodInfo.ReturnType))
            throw new InvalidOperationException($"The {methodInfo.Name} method must return Task or ValueTask");

        return methodInfo;
    }
}