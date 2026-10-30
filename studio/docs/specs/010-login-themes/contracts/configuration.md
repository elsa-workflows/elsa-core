# Configuration Contract

## Section

`Authentication:Login`

## Schema

```json
{
  "Authentication": {
    "Login": {
      "Theme": "classic"
    }
  }
}
```

`Theme` is the only core setting. It is optional and defaults to `classic`.
Comparison is ordinal and case-insensitive; the canonical registered spelling
is used in diagnostics.

Startup fails before accepting traffic when:

- the configured value is blank;
- no registration matches the configured value;
- two or more registrations have IDs equal by the configured comparer.

Theme-specific settings belong to the registering module's own options class and
configuration section.

## Built-in identifiers

| ID | Package |
|---|---|
| `classic` | `Elsa.Studio.Authentication.UI` |
| `workflow-constellation` | `Elsa.Studio.Authentication.Themes` |
| `workflow-aurora` | `Elsa.Studio.Authentication.Themes` |
| `execution-timeline` | `Elsa.Studio.Authentication.Themes` |
| `human-automation` | `Elsa.Studio.Authentication.Themes` |

Changing a value requires application restart. Per-request, per-user, and
per-tenant selection are outside this contract.
