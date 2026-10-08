using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides the <see cref="RenderFragment"/> to display an error details.
/// </summary>
public interface IErrorComponentProvider
{
    /// <summary>
    /// Returns the <see cref="RenderFragment"/> to display an error details.
    /// </summary>
    RenderFragment GetErrorComponent(Exception context);
}