# Styling Contract

Themes style their outer composition and the shared panel through public CSS
custom properties. They must not select private descendants inside the panel.

## Public tokens

```css
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
```

The core module supplies accessible defaults. A theme may override tokens on its
root element. Token names are public compatibility surface.

## Required behavior

- The theme root covers at least the viewport and provides an immediate fallback
  surface.
- The panel remains above decorative artwork and is usable before assets load.
- Narrow layouts avoid horizontal scrolling and keep controls at usable widths.
- Essential names, instructions, labels, links, and controls are HTML, not
  raster content.
- First-party assets use `_content/...` application paths.
- Animated custom themes honor `prefers-reduced-motion`.
- Themes do not auto-invert from application light/dark preference.

## Private surface

Class names and descendant markup within `LoginPanel` are implementation details.
External themes must use the context fragments and tokens only.
