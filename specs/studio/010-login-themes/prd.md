# Product Requirements Document: Configurable Login Themes

## Problem Statement

Elsa Studio currently exposes a composable authentication experience, but its login page has one fixed presentation. Deployers cannot select a login design that fits their organization, and module authors cannot contribute a custom login page without replacing or duplicating authentication behavior.

This creates three related problems:

1. Deployers cannot choose from polished login experiences that communicate the power of Elsa as a workflow orchestration platform.
2. Host applications and custom modules have no stable, open extension point for supplying login presentation.
3. A presentation change risks duplicating login discovery, ordering, validation, navigation, error handling, and authentication initiation logic.

The login experience must become configurable and extensible while retaining one authoritative implementation of authentication behavior.

## Solution

Provide a login-theme framework that separates authentication behavior from presentation.

The framework owns the `/login` route, authentication-method discovery, method ordering, loading and error states, safe return paths, and login-method rendering. It supplies themes with a shared, fully functional login panel and presentation context. Themes arrange and style that panel but contain no authentication logic.

Deployers select one application-wide theme at startup using a stable configuration identifier. Elsa Studio ships with:

- `classic` as the default, visually matching the Elsa Studio 3.7.0 login experience.
- `workflow-constellation`, based on concept 1.
- `workflow-aurora`, based on concept 4.
- `execution-timeline`, based on concept 9.
- `human-automation`, based on concept 10.

The four modern themes are delivered as an optional built-in theme pack. Host applications and custom modules can register their own theme components or advanced theme providers through the same public extension mechanism.

## User Stories

1. As a deployer, I want to select a login theme in configuration, so that I can choose the experience that best represents my organization.

2. As an existing Elsa Studio operator, I want an unconfigured deployment to retain the familiar 3.7.0 login design, so that an upgrade does not unexpectedly change the login experience.

3. As a user, I want the login page to clearly expose every available login method, so that presentation changes never remove an authentication option.

4. As a user, I want local credentials and external identity-provider options presented coherently, so that I can quickly choose the correct sign-in path.

5. As a user, I want loading, unavailable, warning, and failure states to remain understandable in every theme, so that I know how to proceed when authentication cannot start.

6. As a user, I want the login experience to work on desktop and narrow screens, so that I can authenticate from the device available to me.

7. As a user, I want login controls to remain usable before large background artwork finishes loading, so that imagery never delays authentication.

8. As a keyboard or assistive-technology user, I want the existing login accessibility behavior preserved, so that a visual redesign does not regress authentication access.

9. As a deployer, I want invalid theme identifiers rejected at startup, so that configuration mistakes are discovered before users reach the login page.

10. As a deployer, I want duplicate theme identifiers rejected at startup, so that module load order cannot silently decide which design users see.

11. As a developer, I want to register a Razor component under a stable theme identifier, so that I can add a custom login presentation with minimal integration code.

12. As an advanced developer, I want a provider-level rendering extension, so that I can supply a full custom page composition without inheriting a prescribed component base class.

13. As a developer, I want a strongly defined presentation context, so that my theme can render branding, utility links, version information, and the shared login panel without accessing authentication internals.

14. As a developer, I want documented styling tokens for the shared login panel, so that my theme can produce light, dark, glass, or branded surfaces without relying on private markup.

15. As a module author, I want explicit theme registration rather than assembly scanning, so that my module is deterministic and compatible with trimmed deployments.

16. As a module author, I want to package raster, vector, CSS, video, or Razor-based artwork with my theme, so that the framework does not constrain the visual medium.

17. As a module author, I want my theme to bind its own configuration, so that advanced visual options are not forced into a lowest-common-denominator core schema.

18. As a white-label host developer, I want themes to honor the host branding provider, so that logos, application names, taglines, and approved utility links remain customizable.

19. As a localization maintainer, I want shared login text localized centrally, so that themes do not duplicate translations for common authentication states and actions.

20. As a security-conscious deployer, I want built-in artwork served from the application origin, so that login does not contact unapproved third-party asset hosts.

21. As an application operator, I want a minimal recovery login shell if a custom theme fails at render time, so that a presentation defect does not lock out all users.

22. As a maintainer, I want the classic and modern themes to use the same extension mechanism, so that built-in themes do not require special rendering branches.

23. As a maintainer, I want authentication behavior implemented once, so that adding or changing a theme cannot alter credential handling, external redirects, preferred-method behavior, or return-path safety.

24. As a maintainer, I want modern themes isolated in a theme-pack module, so that minimal hosts can omit their assets while the standard Elsa bundle can offer all built-in choices.

25. As a maintainer, I want public extension documentation and a custom-module example, so that third-party theme authors can integrate without reading internal implementation code.

## Implementation Decisions

- Theme selection is application-wide and resolved once at startup.
- Configuration selects a stable theme identifier rather than a runtime type name.
- The configuration path is `Authentication:Login:Theme`.
- Changing the configured theme requires an application restart.
- The default theme identifier is `classic`.
- Unknown configured identifiers prevent application startup.
- Duplicate registered identifiers prevent application startup.
- Theme registration is explicit; no assembly scanning or attribute discovery is used.
- The common extension path registers a component for a stable identifier.
- An advanced provider interface can render a theme from the same presentation context.
- All themes own presentation only.
- The framework retains login-method discovery, ordering, preferred-method decoration, loading and error states, return-path normalization, failure reporting, and login-method execution.
- Themes receive one shared login-panel fragment rather than arranging individual authentication-method components.
- A theme can choose the panel position and set documented visual tokens, but it cannot replace panel behavior.
- The shared panel renders local credentials first when available, followed by external methods separated visually.
- Capability-specific controls such as “Remember me” appear only when supported by the underlying authentication method.
- The presentation context exposes host branding, the shared login panel, shared utility links, and version information.
- Themes honor the host branding provider rather than hardcoding the Elsa application name, logo, or tagline.
- Documentation and source-code links are rendered as a shared utility fragment driven by branding configuration.
- Version information is available to themes; `classic` displays it, while other themes may omit or reposition it.
- Shared login labels, state messages, and action text use the existing localization system.
- Theme-specific optional prose is localized by the theme that owns it.
- The framework publishes scoped visual tokens for panel width, surface, text, border, radius, shadow, input surface, primary action, and spacing.
- Themes set public tokens on their root and do not target private login-panel markup.
- The core theme contract is asset-agnostic.
- Built-in modern themes use curated raster background plates derived from the selected mockups.
- Raster backgrounds contain no essential text, controls, logos, or workflow labels.
- Essential content is rendered as accessible HTML/Razor above the artwork.
- Built-in theme assets are same-origin static web assets and do not use remote URLs.
- Custom themes may use remote assets only when their host configuration and content-security policy explicitly allow it.
- Themes collapse to a centered single-panel layout on narrow screens, using responsive cropping and theme-defined focal positions.
- Themes may provide dedicated mobile assets.
- Themes are authored compositions and are not automatically inverted for application light/dark mode.
- Themes can explicitly implement their own variants in the future.
- Artwork loading never blocks the shared login panel.
- Final asset-size budgets are set after producing and compressing the selected background plates. The measured fidelity-preserving size receives 20 percent headroom for regression enforcement.
- Built-in raster assets provide responsive modern formats, with practical fallbacks where required.
- Decorative imagery is hidden from assistive technologies.
- Existing login accessibility checks remain in scope, including serious/critical automated accessibility findings, keyboard operation, visible focus, landmarks, labels, text fallbacks, and same-origin assets.
- A broader application-wide WCAG 2.2 AA initiative is deferred.
- Custom theme render failures are logged and replaced by a minimal built-in recovery shell containing the shared login panel.
- Invalid configuration remains a startup failure and does not silently fall back.
- The authentication UI module owns contracts, registry, selector, shared panel, recovery shell, and `classic`.
- A separate built-in theme-pack module owns the four modern themes and their assets.
- The standard Elsa bundle and sample hosts register the built-in theme pack.
- Minimal and custom hosts can omit the theme pack.
- The legacy `Elsa.Studio.Login` module remains unchanged and serves only as a visual reference for `classic`.

## Testing Decisions

- Tests verify externally observable configuration and extension behavior rather than theme implementation details.
- No component-rendering tests are added for theme markup or appearance.
- No committed pixel-comparison or screenshot-baseline tests are added.
- Registry and selection tests cover:
  - default `classic` selection;
  - configured selection of each registered identifier;
  - unknown identifier startup failure;
  - duplicate identifier startup failure;
  - explicit custom component registration;
  - advanced provider registration;
  - runtime recovery when a selected provider fails.
- Existing browser authentication checks remain authoritative for login interaction, keyboard flow, visible focus, landmarks, labels, text fallbacks, same-origin presentation assets, preferred-method behavior, and safe authentication initiation.
- Browser verification covers responsive structure for desktop and narrow layouts without introducing visual snapshot baselines.
- Manual implementation review includes desktop and narrow-screen captures of all five built-in themes.
- Authentication-method contract tests continue to verify credential POST fields, antiforgery behavior, return paths, method ordering, and failure handling independently of themes.
- Full solution build and tests are required before completion.

## Out of Scope

- Per-user theme selection.
- Per-tenant theme selection.
- A runtime theme switcher.
- Hot-reloading theme configuration.
- Automatic light/dark inversion.
- Replacing authentication behavior from a theme.
- Loading arbitrary runtime component type names from configuration.
- Assembly scanning for theme discovery.
- Modifying or removing the legacy `Elsa.Studio.Login` module.
- A comprehensive application-wide WCAG 2.2 AA remediation effort.
- Committed visual snapshot baselines.
- Theme component markup tests.
- Remote artwork configuration for built-in themes.
- Essential text embedded in raster imagery.

## Further Notes

- The four selected modern concepts are visual source material rather than implementation assets. Production background plates must remove all embedded controls and essential text before optimization.
- `classic` should match the 3.7.0 page in style, color, proportion, blue branding pane, pale wave background, restrained form surface, utility-link placement, and version treatment without copying legacy markup literally.
- The public contract should remain evolvable. Presentation context additions should be backward-compatible whenever possible.
- A host that requires different authentication logic can replace the `/login` route outside the theme framework; doing so is not a theme.
