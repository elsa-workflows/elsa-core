# Data Model: Configurable Login Themes

The feature has no persisted data. These are runtime configuration and
presentation models.

## LoginThemeOptions

| Field | Type | Default | Validation |
|---|---|---|---|
| `Theme` | string | `classic` | Required; must match exactly one registered ID using ordinal-ignore-case comparison |

Bound from `Authentication:Login`.

## LoginThemeRegistration

| Field | Type | Meaning |
|---|---|---|
| `Id` | string | Stable deployment-facing identifier |
| `ProviderType` | `Type` | DI-resolved implementation of `ILoginThemeProvider` |

Registrations are immutable after service-provider construction. Blank IDs,
provider types that do not satisfy the contract, and duplicate IDs are invalid.
Registration order has no selection semantics.

## LoginThemeSelection

| Field | Type | Meaning |
|---|---|---|
| `Id` | string | Canonical registered ID |
| `Provider` | `ILoginThemeProvider` | Provider resolved for the current scope |

State transitions:

1. Configuration is bound.
2. Registrations are validated and indexed.
3. The configured ID is selected.
4. The selection remains fixed until restart.

Unknown or duplicate IDs terminate startup. A selected provider's render-time
exception transitions only the login view to recovery presentation.

## LoginThemeBranding

| Field | Type | Meaning |
|---|---|---|
| `ApplicationName` | string | Host-provided product name |
| `Tagline` | string? | Host-provided supporting phrase |
| `LogoUrl` | string? | Logo for light surfaces |
| `ReverseLogoUrl` | string? | Logo for dark/colored surfaces |
| `ClassicBackgroundUrl` | string? | Host-provided classic background override |
| `ShowDocumentationLink` | bool | Whether shared documentation utility is available |
| `ShowSourceLink` | bool | Whether shared source utility is available |

The model is projected once from `IBrandingProvider`; theme implementations do
not discover branding services independently.

## LoginThemeContext

| Field | Type | Meaning |
|---|---|---|
| `Branding` | `LoginThemeBranding` | Host application identity |
| `LoginPanel` | `RenderFragment` | Shared authentication content |
| `UtilityLinks` | `RenderFragment` | Shared documentation/source links |
| `Version` | string | Available Studio client version |

The context is presentation-only. It contains no catalog, credential,
authentication callback, or return-path API.

## Shared Login Panel state

| State | Output |
|---|---|
| Loading | Localized progress indication |
| Catalog warnings | One or more localized/returned warning alerts |
| Catalog failure | Localized error alert |
| No methods | Localized unavailable message |
| Ready | Ordered local method(s), divider where needed, then external methods |
| Method-reported failure | Localized or provider-supplied error alert |

The panel owns method ordering and the existing normalized safe return path. Its
state is unaffected by the selected theme.

## Theme artwork

| Field | Type | Meaning |
|---|---|---|
| Desktop source | local asset URL | Wide composition |
| Mobile source | local asset URL, optional | Deliberate narrow crop |
| Fallback | CSS color/gradient | Immediate surface before/failing image load |
| Measured bytes | integer | Fidelity-preserving optimized output size |
| Budget bytes | integer | Measured bytes plus 20% headroom |

Artwork contains decorative visual language only. Essential branding, labels,
instructions, and controls remain semantic HTML.
