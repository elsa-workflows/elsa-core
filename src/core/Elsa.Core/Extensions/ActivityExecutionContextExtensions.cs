using System.Text.Json;
using Elsa.Models;

namespace Elsa.Extensions;

public static class ActivityExecutionContextExtensions
{
    public static bool TryGetInput<T>(this ActivityExecutionContext context, string key, out T value) => context.Input.TryGetValue(key, out value);
}