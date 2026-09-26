# Elsa Studio theming

Elsa Studio theme packs complement MudBlazor. Each pack supplies a normal
`MudTheme`; `MudThemeProvider` remains responsible for component colors and
light/dark mode. The shell exposes a small set of `--elsa-*` composition tokens
that reference MudBlazor's generated CSS variables, so custom MudBlazor themes
continue to flow through the application.

## Select a built-in theme

Configure the application theme under `Presentation`:

```json
{
  "Presentation": {
    "Theme": "human-automation"
  }
}
```

Built-in IDs are:

- `classic`
- `workflow-constellation`
- `workflow-aurora`
- `execution-timeline`
- `human-automation` (default)

IDs are matched case-insensitively and invalid selections fail startup
validation.

## Let login inherit the application theme

The login framework remains independently configurable. Set its theme to
`inherit` to select the login presentation with the same stable ID:

```json
{
  "Authentication": {
    "Login": {
      "Theme": "inherit"
    }
  }
}
```

An inherited login ID must be registered by the application. A login can still
select `classic` or any registered login theme explicitly.

## Register a theme pack

A theme provider implements the existing `IThemeProvider` contract:

```csharp
services
    .AddCore(options => options.Theme = "my-theme")
    .AddStudioThemeProvider<MyThemeProvider>("my-theme");
```

Register a login theme under the same ID if login should inherit it.

Existing applications that replace `IThemeProvider` remain supported. A custom
provider is used directly by `IThemeService`, while the theme-pack registry
continues to provide stable presentation metadata.

Replacing only `IThemeProvider` does not change `Presentation:Theme`. Login
inheritance follows that configured stable ID, so a custom provider that also
wants a matching login must configure and register the corresponding theme-pack
and login IDs.

## Scope

The theme-pack layer currently covers MudBlazor components, the authenticated
shell, and shared Elsa CSS tokens. Radzen controls and the X6 workflow canvas
intentionally retain their current styling and will be addressed as separate
integrations.
