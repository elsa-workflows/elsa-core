using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Components;

/// Base class for components. This class sets the <see cref="BlazorServiceAccessor.Services"/> property to the <see cref="IServiceProvider"/> instance.
/// See https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-7.0#access-server-side-blazor-services-from-a-different-di-scope
public abstract class StudioComponentBase : ComponentBase;