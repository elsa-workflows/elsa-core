# Research: Configurable Login Themes

## Core ownership

**Decision**: Keep the route, login-method composition, state, validation,
return-path normalization, branding projection, selection, and recovery in
`Elsa.Studio.Authentication.UI`.

**Rationale**: This is the existing provider-neutral composition module. It can
offer a presentation seam without exposing authentication behavior to themes.

**Alternatives considered**:

- Put contracts in `Authentication.Abstractions`: rejected because login themes
  are a UI concern and would burden provider-only modules.
- Let every theme own a page: rejected because it duplicates security-sensitive
  behavior and makes parity across login methods fragile.

## Optional theme packaging

**Decision**: Package the four modern themes in a separate
`Elsa.Studio.Authentication.Themes` Razor class library. Keep `classic` in the
core UI module.

**Rationale**: Minimal hosts retain a functional compatible login without
shipping optional artwork. The standard bundle can make the richer themes
available by project reference and explicit registration.

**Alternatives considered**:

- Put all themes in the core module: rejected because minimal hosts could not
  omit their assets.
- One project per theme: rejected as unnecessary packaging overhead for four
  first-party themes with the same release lifecycle.

## Registration and selection

**Decision**: Register themes explicitly with a stable identifier through
`AddLoginTheme<TComponent>` or `AddLoginThemeProvider<TProvider>`. Bind
`Authentication:Login:Theme`, default to `classic`, compare identifiers
case-insensitively, and reject blank, duplicate, and unknown values at startup.

**Rationale**: Explicit registration is deterministic, trim-friendly, and
supports host and module extensions without reflection scanning or CLR names in
configuration.

**Alternatives considered**:

- Assembly scanning: rejected for trim safety, startup cost, and hidden
  registrations.
- CLR type names in configuration: rejected as unstable deployment contracts.
- Last registration wins: rejected because module order would silently change
  behavior.

## Theme rendering contract

**Decision**: Supply a `LoginThemeContext` containing projected host branding,
the shared `LoginPanel` fragment, shared utility links, and version text.
Component themes derive from a common base; advanced providers return a
`RenderFragment`.

**Rationale**: The common path is strongly guided while the provider path
supports unusual host composition. Both remain presentation-only.

**Alternatives considered**:

- Pass services/catalogs to themes: rejected because it leaks login logic.
- Expose the panel DOM: rejected because theme CSS would depend on private
  markup.

## Render failure recovery

**Decision**: Wrap the selected theme in a logging Blazor error boundary. Its
error content renders a minimal built-in shell around the same shared panel.

**Rationale**: A valid extension can fail at render time after startup
validation. Authentication should remain reachable and operators need the
exception.

**Alternatives considered**:

- Fall back through the selector: rejected because it conflates configuration
  validity with runtime rendering and risks recursive failure.
- Fail the whole route: rejected because cosmetic extension failure should not
  remove access to authentication.

## Raster artwork

**Decision**: Generate clean decorative plates from approved concepts 1, 4, 9,
and 10, excluding controls and essential text. Package desktop and, when useful,
mobile crops locally; use CSS fallback surfaces and non-blocking backgrounds or
`picture` elements.

**Rationale**: Raster preserves the approved visual richness and allows custom
theme authors to supply raster artwork while semantic content remains HTML.

**Alternatives considered**:

- Recreate artwork entirely in CSS/SVG: rejected because exact visual fidelity
  to the selected raster concepts is required.
- Embed full mockup screenshots: rejected because controls and text would be
  inaccessible, duplicated, and non-responsive.

## Asset budget

**Decision**: Optimize approved final images first, record each
fidelity-preserving byte size, and set the regression threshold to that measured
size plus 20 percent headroom.

**Rationale**: A speculative fixed limit could force visible degradation. The
budget must fit all selected backgrounds with spare capacity.

**Alternatives considered**:

- Preliminary 750 KB limit: rejected before final compression measurements.
- No budget: rejected because image regressions could delay authentication.

## Verification

**Decision**: Add service-level tests for registration/validation/selection,
retain existing browser auth/accessibility checks, build all target frameworks,
and perform manual desktop/mobile screenshots. Do not add component-rendering
tests or screenshot/pixel baselines.

**Rationale**: This matches the explicit product decision while validating the
highest-risk framework rules and preserving the existing behavioral baseline.

**Alternatives considered**:

- bUnit theme tests: explicitly excluded.
- Golden-image assertions: explicitly excluded due brittleness.
