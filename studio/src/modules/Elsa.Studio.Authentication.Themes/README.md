# Elsa Studio Authentication Themes

This optional Razor class library provides the four first-party modern login
themes. It contains presentation only: authentication-method discovery,
credential handling, return-path validation, loading states, and errors stay in
`Elsa.Studio.Authentication.UI`'s shared login panel.

## Register and select

Register the pack after the authentication UI:

```csharp
services
    .AddAuthenticationUI(configuration.GetSection("Authentication:Login"))
    .AddElsaStudioLoginThemes();
```

Choose one stable ID at deployment time and restart the application. To follow
the application-wide `Presentation:Theme`, select `inherit`:

```json
{
  "Presentation": {
    "Theme": "human-automation"
  },
  "Authentication": {
    "Login": {
      "Theme": "inherit"
    }
  }
}
```

An explicit login theme ID remains supported when the authentication surface
should differ from the authenticated application.

| ID | Concept |
| --- | --- |
| `workflow-constellation` | Concept 1 |
| `workflow-aurora` | Concept 4 |
| `execution-timeline` | Concept 9 |
| `human-automation` | Concept 10 |

## Artwork

The shared frame deliberately uses same-origin, non-essential responsive
artwork at these paths:

```text
_content/Elsa.Studio.Authentication.Themes/images/<theme>.avif
```

CSS supplies a complete fallback while artwork loads or if an image request
fails, and deliberately crops the same high-resolution plate at narrow widths.
Plates remain decorative: brand names, labels, instructions, links, and login
controls are accessible HTML.

The four AVIF plates total 188,575 bytes at the fidelity-preserving quality
setting. `wwwroot/images/asset-budget.json` records per-file measurements and
the agreed 20% regression headroom (226,292 bytes total). Verify the package
with:

```shell
node verify-assets.mjs
```

## Styling boundary

`login-themes.css` sets only the documented `--elsa-login-*` CSS variables and
styles this package's outer composition. It never selects inside the shared
login panel. Custom themes can follow the same boundary while packaging their
own image, SVG, CSS, video, or Razor presentation assets.
