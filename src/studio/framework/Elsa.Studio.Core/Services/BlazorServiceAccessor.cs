using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// This class is used to access the IServiceProvider from a different DI scope.
/// See: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-7.0#access-server-side-blazor-services-from-a-different-di-scope
public sealed class BlazorServiceAccessor : IBlazorServiceAccessor
{
    private static readonly AsyncLocal<BlazorServiceHolder> CurrentServiceHolder = new();

    /// <summary>
    /// Gets or sets the current IServiceProvider.
    /// </summary>
    public IServiceProvider Services
    {
        get => CurrentServiceHolder.Value!.Services!;
        set
        {
            if (CurrentServiceHolder.Value is { } holder)
            {
                // Clear the current IServiceProvider trapped in the AsyncLocal.
                holder.Services = null;
            }
            
            // Use object indirection to hold the IServiceProvider in an AsyncLocal
            // so it can be cleared in all ExecutionContexts when it's cleared.
            CurrentServiceHolder.Value = new() { Services = value };
        }
    }

    private sealed class BlazorServiceHolder
    {
        /// <summary>
        /// Gets or sets the services.
        /// </summary>
        public IServiceProvider? Services { get; set; }
    }
}