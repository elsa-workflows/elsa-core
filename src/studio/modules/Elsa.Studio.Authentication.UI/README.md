# Elsa Studio Authentication UI

This package owns Elsa Studio's `/login` route, provider-neutral login-method
composition, and the extensible login-theme framework. A theme receives
presentation fragments only; method discovery, credential handling,
safe-return validation, loading, warnings, and errors remain in the shared
panel.

## Select a theme

Bind the deployment setting while registering the UI:

```csharp
services.AddAuthenticationUI(
    configuration.GetSection(LoginThemeOptions.SectionName));
```

```json
{
  "Authentication": {
    "Login": {
      "Theme": "classic"
    }
  }
}
```

The setting is a stable theme ID, never a CLR type name. It is read at startup
and changes require an application restart. Missing configuration selects
`classic`. Blank, unknown, and duplicate case-insensitive IDs fail startup with
an actionable options error.

This package includes three Classic presentations:

- `classic-refined-split` — a balanced split layout with a dedicated brand
  context panel and focused sign-in stage.
- `classic-unified` — the compact, unified identity gateway card.
- `classic-brand-canvas` — a spacious brand canvas with a dedicated sign-in
  stage.

`classic` remains the default and a compatibility alias for
`classic-unified`. The optional `Elsa.Studio.Authentication.Themes` package
provides:

- `workflow-constellation`
- `workflow-aurora`
- `execution-timeline`
- `human-automation`

## Register a component theme

Create a Razor component derived from `LoginThemeComponentBase`:

```razor
@inherits LoginThemeComponentBase

<main class="my-company-login">
    <header>
        <img src="@Context.Branding.LogoUrl" alt="" />
        <h1>@Context.Branding.ApplicationName</h1>
    </header>
    @Context.LoginPanel
    @Context.UtilityLinks
</main>
```

Register it explicitly from a host or module:

```csharp
services.AddLoginTheme<MyCompanyLoginTheme>("my-company");
```

The context exposes projected host branding, the functional login panel, shared
utility links, and the installed client version. It intentionally exposes no
catalog, credential, callback, or navigation API.

## Register an advanced provider

For a composition that does not fit the component base, implement:

```csharp
public interface ILoginThemeProvider
{
    RenderFragment Render(LoginThemeContext context);
}
```

Then register it:

```csharp
services.AddLoginThemeProvider<MyCompanyThemeProvider>("my-advanced-theme");
```

Providers are dependency-injection services and may consume their own
presentation options. If a valid selected provider throws while rendering, the
exception is logged and a minimal built-in recovery shell renders the same
functional login panel.

## Theme-owned settings and assets

The core options model owns only the selected ID. A custom module can bind its
own section and package static assets in its Razor class library, for example:

```text
_content/MyCompany.StudioTheme/images/background.avif
```

First-party artwork is same-origin. Remote custom assets are allowed only when
the host's content-security policy permits them. Essential names, labels,
instructions, links, and controls must remain semantic HTML.

## Styling contract

Themes set the following public tokens on their root element. They must not
select private descendants of the shared panel:

```text
--elsa-login-panel-width
--elsa-login-panel-max-width
--elsa-login-panel-padding
--elsa-login-panel-gap
--elsa-login-panel-radius
--elsa-login-panel-background
--elsa-login-panel-color
--elsa-login-panel-border
--elsa-login-panel-shadow
--elsa-login-accent
--elsa-login-accent-contrast
--elsa-login-muted-color
--elsa-login-focus-color
--elsa-login-method-surface
--elsa-login-method-border
--elsa-login-method-hover-surface
--elsa-login-method-preferred-surface
--elsa-login-method-preferred-border
--elsa-login-method-preferred-color
--elsa-login-method-icon-surface
--elsa-login-method-icon-color
--elsa-login-method-shadow
--elsa-control-surface
--elsa-control-surface-hover
--elsa-control-surface-disabled
--elsa-control-border
--elsa-control-border-hover
--elsa-control-focus
--elsa-control-error
--elsa-control-radius
```

The shell localizes common text and preserves keyboard operation, visible focus,
labels, landmarks, and same-origin first-party presentation assets.

## Login-method extensions

Authentication packages continue to contribute:

- `ILoginMethodCatalog` for safe, enabled method metadata.
- `ILoginMethodComponentProvider` for a component keyed by method kind.
- `ILoginMethodIconProvider` for trusted, locally supplied SVG icons.

Preferred methods receive visual emphasis only. The shell never starts a login
flow until the user selects a method.
