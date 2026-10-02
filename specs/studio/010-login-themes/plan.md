# Implementation Plan: Configurable Login Themes

**Branch**: `010-login-themes` | **Date**: 2026-07-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/010-login-themes/spec.md`

## Summary

Replace the fixed `/login` presentation in `Elsa.Studio.Authentication.UI` with a
startup-selected presentation host. The core module retains the route, method
discovery, ordering, safe return path, loading/error behavior, localization,
branding projection, and a 3.7.0-style `classic` theme. It exposes explicit
component and provider registration APIs. A new optional Razor class library
packages the four modern raster-backed themes; standard Server and WebAssembly
hosts register that pack and bind `Authentication:Login:Theme`.

## Technical Context

**Language/Version**: C# latest; Razor; CSS; .NET 8, 9, and 10 multi-targeting
**Primary Dependencies**: Blazor Server and WebAssembly, MudBlazor 9, existing Elsa Studio shared branding/localization contracts, Microsoft options and dependency injection
**Storage**: N/A; startup configuration and application-packaged static assets only
**Testing**: xUnit service-level tests, existing Playwright external-authentication suite, build verification, and manual desktop/mobile visual review; no component-rendering or screenshot-baseline tests
**Target Platform**: Modern browsers hosted by Blazor Server or Blazor WebAssembly
**Project Type**: Modular Razor class libraries consumed by sample hosts and the standard Studio bundle
**Performance Goals**: Shared login controls become interactive without waiting for artwork; theme lookup is a single startup-validated dictionary lookup
**Constraints**: Same-origin built-in assets, no embedded essential text in artwork, no runtime theme switching, no theme-owned authentication behavior, responsive without horizontal scrolling, classic remains default, final raster budget is measured after fidelity-preserving compression and capped with 20% headroom
**Scale/Scope**: Five built-in themes, one selected per application start, unbounded explicit third-party registrations, one login route

## Constitution Check

*GATE: Passed before research and re-checked after design.*

| Principle | Result | Design response |
|---|---|---|
| I. Modular Studio Features | Pass | Core behavior remains in `Authentication.UI`; optional art-heavy themes live in a focused module; the bundle references the optional module. |
| II. Backend Capability Awareness | Pass | No new backend API or capability dependency is introduced; existing login catalogs remain authoritative. |
| III. UX Consistency and Density | Pass | The shared panel preserves established controls and states; all themes have deliberate wide and narrow layouts. |
| IV. Async and Disposal Discipline | Pass | Existing asynchronous catalog loading remains centralized; themes own no subscriptions or remote work. |
| V. Testing and Verification | Pass | Registry/options behavior receives service-level tests, existing browser authentication checks remain, and UI receives manual desktop/mobile verification. The explicit product decision excludes component and pixel-baseline tests. |
| VI. Focused Change Sets | Pass | Public contracts, registration, assets, host configuration, and documentation are contained within the authentication theme feature. |
| VII. Simplicity and DRY | Pass | One shared login panel owns behavior; themes provide only composition and styling through one context and CSS token contract. |

Post-design check: all gates remain passed. The extra optional module is the
constitution-prescribed boundary for an optional capability with substantial
static assets, not an abstraction without a variability point.

## Project Structure

### Documentation (this feature)

```text
specs/010-login-themes/
├── prd.md
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── configuration.md
│   ├── public-api.md
│   └── styling.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── modules/
│   ├── Elsa.Studio.Authentication.UI/
│   │   ├── Components/
│   │   │   ├── LoginPanel.razor
│   │   │   ├── LoginThemeHost.razor
│   │   │   ├── LoginThemeErrorBoundary.cs
│   │   │   ├── LoginThemeRecovery.razor
│   │   │   ├── LoginUtilityLinks.razor
│   │   │   └── Themes/
│   │   │       ├── ClassicUnifiedLoginTheme.razor
│   │   │       └── ClassicBrandCanvasLoginTheme.razor
│   │   ├── Contracts/
│   │   ├── Extensions/ServiceCollectionExtensions.cs
│   │   ├── Models/
│   │   ├── Options/LoginThemeOptions.cs
│   │   ├── Pages/Login.razor
│   │   ├── Services/
│   │   ├── wwwroot/css/
│   │   └── README.md
│   ├── Elsa.Studio.Authentication.UI.Tests/
│   │   ├── LoginThemeOptionsValidatorTests.cs
│   │   └── LoginThemeRegistryTests.cs
│   └── Elsa.Studio.Authentication.Themes/
│       ├── Components/
│       ├── Extensions/ServiceCollectionExtensions.cs
│       ├── wwwroot/css/
│       ├── wwwroot/images/
│       └── README.md
├── bundles/Elsa.Studio/Elsa.Studio.csproj
└── hosts/
    ├── Elsa.Studio.Host.Server/
    └── Elsa.Studio.Host.Wasm/

tests/browser/ExternalAuthentication/
```

**Structure Decision**: The authentication UI module owns every security and
behavioral concern plus the compatibility default. The optional themes project
is a Razor class library so static web assets resolve through stable
`_content/Elsa.Studio.Authentication.Themes/...` URLs in Server and WebAssembly.
A small non-component test project validates only registry and configuration
semantics. The legacy `Elsa.Studio.Login` module is unchanged.

## Design Sequence

1. Extract the current route behavior into `LoginPanel` without changing its
   catalog, ordering, safe-return, warning, loading, empty, or error semantics.
2. Add immutable theme registrations, normalized ordinal-ignore-case IDs,
   startup options validation, a selector, and two public registration paths.
3. Build a presentation context from projected branding, shared panel,
   utility-link, and version fragments.
4. Render the selected theme through a logging error boundary whose recovery
   content reuses the same panel.
5. Register `classic` as an ordinary built-in theme and make it the default.
6. Add the optional modern theme pack and responsive raster artwork.
7. Wire the standard hosts, document extension/configuration contracts, and
   verify builds, service tests, browser behavior, accessibility, asset origins,
   dimensions, and final byte budgets.

## Complexity Tracking

No constitution violations require justification.
