using ThrottleDebounce;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Adds extension methods for <see cref="RateLimitedFunc{TResult}"/> and <see cref="RateLimitedFunc{T, TResult}"/>.
/// </summary>
public static class RateLimitedFuncExtensions
{
    /// <summary>
    /// Invokes the specified rate limited function.
    /// </summary>
    public static async Task InvokeAsync(this RateLimitedFunc<Task> rateLimitedFunc)
    {
        var task = rateLimitedFunc.Invoke();

        if (task != null)
            await task;
    }
    
    /// <summary>
    /// Invokes the specified rate limited function.
    /// </summary>
    public static async Task InvokeAsync<T>(this RateLimitedFunc<T, Task> rateLimitedFunc, T arg1)
    {
        var task = rateLimitedFunc.Invoke(arg1);

        if (task != null)
            await task;
    }
}