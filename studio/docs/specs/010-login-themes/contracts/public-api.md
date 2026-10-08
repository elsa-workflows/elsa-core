# Public Extension Contract

Names below define the intended public surface. Implementation may refine
namespaces but not the behavioral contract.

## Registration

```csharp
IServiceCollection AddAuthenticationUI(
    this IServiceCollection services,
    IConfigurationSection? loginThemeConfiguration = null);

IServiceCollection AddLoginTheme<TComponent>(
    this IServiceCollection services,
    string id)
    where TComponent : LoginThemeComponentBase;

IServiceCollection AddLoginThemeProvider<TProvider>(
    this IServiceCollection services,
    string id)
    where TProvider : class, ILoginThemeProvider;
```

The no-argument `AddAuthenticationUI()` call remains source-compatible and
registers `classic`. Calling an extension more than once must not conceal
duplicate user registrations.

## Component contract

```csharp
public abstract class LoginThemeComponentBase : ComponentBase
{
    [Parameter, EditorRequired]
    public LoginThemeContext Context { get; set; }
}
```

The framework adapts a component theme to the provider contract.

## Advanced provider contract

```csharp
public interface ILoginThemeProvider
{
    RenderFragment Render(LoginThemeContext context);
}
```

Implementations are resolved through dependency injection and may consume their
own presentation options or services. They must not be given login catalogs,
credentials, method callbacks, or return-path mutation APIs.

## Context contract

```csharp
public sealed record LoginThemeContext(
    LoginThemeBranding Branding,
    RenderFragment LoginPanel,
    RenderFragment UtilityLinks,
    string Version);
```

Theme components position fragments; they do not invoke authentication.

## Failure behavior

- Invalid registration and selection are startup errors.
- Exceptions thrown while rendering a valid selected provider are logged with
  the selected ID.
- The route then displays the built-in recovery shell and the same shared panel.
- The recovery shell does not re-enter theme selection.
