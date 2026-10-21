# Languages and module translations

The shell and built-in modules use i18next through the shared SDK. English is the
fallback language. Dutch reuses the existing Blazor Studio language and fills in
the React interface's additional messages. Technical names, expressions and symbols
keep their spelling. Runtime API messages and user-authored workflow data are not
rewritten by the client.

Choose **Language** in the header. Switching language updates subscribed components
and module metadata while preserving their state, open workflow, selected activity
and unsaved expressions. The document's language/direction and relative timestamps
follow the choice. Counts in dashboard formatting use that culture.

The preference is saved per normalized API URL and authenticated subject. When a
provider has no subject claim, `AuthUser.name` is the fallback. Different accounts
and servers have separate preferences. Set `ui.defaultCulture` to `en` or `nl` for
the initial language of a user with no saved preference. Storage failure leaves the
current language unchanged and displays a recovery message. Enabling a module with
another culture adds that culture to the picker; module authors must supply its
resources, and built-in messages otherwise fall back to English.

## Module resources

Ship resource JSON or string objects with the module entry and register them through
the `translations` slot. The host namespaces these resources to the module ID;
one module cannot overwrite another module's catalog or the built-in catalog.

```tsx
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import type { StudioApi } from "@elsa-workflows/react-studio-sdk";

function Greeting() {
  const t = useTranslation("weather");
  return <span>{t("Hello {{name}}", { name: "Ada" })}</span>;
}

export function register(api: StudioApi) {
  api.contribute("translations", {
    id: "english", culture: "en", messages: { "Hello {{name}}": "Hello {{name}}" },
  });
  api.contribute("translations", {
    id: "dutch", culture: "nl", messages: { "Hello {{name}}": "Hallo {{name}}" },
  });
  api.contribute("headerItems", { id: "greeting", component: Greeting });
}
```

The `useTranslation` argument must match the module's manifest ID. Missing Dutch
module messages fall back to that module's English resources, then built-in
resources, then the message ID. The renderer translates contributed navigation,
route titles, settings sections and action/tab labels in the owning namespace.
Resource registration uses the same transactional lifecycle as other slots;
failed registration cannot publish partial resources. Keep interpolation names
identical across languages. React renders interpolated values as text.

Use `useTranslation()` without an ID for built-in interface messages.
`translate` supports non-React helpers; a component calling such a helper must
subscribe with `useTranslation`. Static contribution metadata uses `message` to
mark its message ID and is translated when rendered. Avoid translating data before
saving it. The [module template](module-authoring.md) demonstrates packaged resources.

## Catalog maintenance

Built-in resources live in `packages/sdk/src/locales/en.json` and `nl.json` and ship
inside the SDK tarball. Run `pnpm localization:extract` after adding translation
calls or marked metadata, then translate the new Dutch entries. Run
`pnpm localization:check` to reject missing/empty entries, mismatched interpolation,
unused entries, unexternalized JSX text/display attributes and direct toast literals.
The check does not follow arbitrary callback/dictionary data flow; those display
paths still require review. This check covers
the client-owned interface, not server-provided messages or external modules.

The starting Dutch catalog reused nonempty matching entries from
`studio/src/framework/Elsa.Studio.Translations/Translations.nl.resx`; blank source
entries are filled rather than hiding labels. Keep the checked-in catalog as the
source of truth for React-specific translations. The import does not modify Blazor's
resource file.

Call-graph warnings retain their catalog key and interpolation values in the cached
model. Tooltips and caller/callee counts translate when rendered, so switching
culture updates an already loaded graph without fetching it again.
