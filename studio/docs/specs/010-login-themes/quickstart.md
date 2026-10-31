# Quickstart: Login Themes

## Select a built-in theme

Register the optional theme pack after the authentication UI:

```csharp
services
    .AddAuthenticationUI(configuration.GetSection("Authentication:Login"))
    .AddElsaStudioLoginThemes();
```

Configure one stable identifier:

```json
{
  "Authentication": {
    "Login": {
      "Theme": "workflow-constellation"
    }
  }
}
```

Supported first-party IDs are `classic`, `classic-unified`,
`classic-brand-canvas`, `workflow-constellation`, `workflow-aurora`,
`execution-timeline`, and `human-automation`. `classic` is a compatibility
alias for `classic-unified`; omitting `Theme` selects it. Restart the
application after changing the setting.

## Register a component theme

Create a Razor component derived from `LoginThemeComponentBase`:

```razor
@inherits LoginThemeComponentBase

<main class="my-login-theme">
    <header>
        <img src="@Context.Branding.LogoUrl" alt="" />
        <h1>@Context.Branding.ApplicationName</h1>
    </header>
    <section class="my-login-theme__panel">
        @Context.LoginPanel
    </section>
    <nav aria-label="Product links">
        @Context.UtilityLinks
    </nav>
</main>
```

Register it explicitly from the host or module:

```csharp
services.AddLoginTheme<MyLoginTheme>("my-company");
```

Then set `Authentication:Login:Theme` to `my-company`.

## Register an advanced provider

For composition that is not naturally represented by the component base:

```csharp
services.AddLoginThemeProvider<MyLoginThemeProvider>("my-advanced-theme");
```

`MyLoginThemeProvider` implements `ILoginThemeProvider` and returns a
`RenderFragment` from the supplied `LoginThemeContext`. It receives presentation
content only; authentication services remain private to the shared panel.

## Own theme settings and assets

A custom module may bind its own options section and package assets in its own
Razor class library:

```text
_content/MyCompany.StudioTheme/images/background.avif
```

The core configuration schema standardizes only the selected ID. Remote custom
assets are subject to the host's content-security policy; first-party assets are
always same-origin.

## Verify

1. Start with no theme setting and confirm the classic screen.
2. Select each registered ID and restart.
3. Exercise local and external login methods.
4. Resize to a narrow mobile width and confirm no horizontal scrolling.
5. Block the artwork request and confirm the panel remains usable.
6. Configure an unknown or duplicate ID and confirm startup fails clearly.
