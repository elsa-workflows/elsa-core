using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.UI.Services;

internal sealed class ComponentLoginThemeProvider<TComponent> : ILoginThemeProvider
    where TComponent : LoginThemeComponentBase
{
    public RenderFragment Render(LoginThemeContext context) => builder =>
    {
        builder.OpenComponent<TComponent>(0);
        builder.AddComponentParameter(1, nameof(LoginThemeComponentBase.Context), context);
        builder.CloseComponent();
    };
}
