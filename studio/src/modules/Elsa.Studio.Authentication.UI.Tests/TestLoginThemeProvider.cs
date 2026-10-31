using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.UI.Tests;

internal sealed class TestLoginThemeProvider : ILoginThemeProvider
{
    public RenderFragment Render(LoginThemeContext context) => context.LoginPanel;
}

internal sealed class AnotherTestLoginThemeProvider : ILoginThemeProvider
{
    public RenderFragment Render(LoginThemeContext context) => context.LoginPanel;
}
