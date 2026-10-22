# Elsa React Studio UI Preview

Shared React primitives and theme styles for independently built Studio modules.
This is an unpublished 3.11 Preview workspace package. Use public subpaths such
as `@elsa-workflows/react-studio-ui/button`, `dialog`, `input`, `select`, `table`
and `tooltip`. The root also exports the common button, input, label, badge and
card primitives.

The `/common/*` subpaths provide the shared table, field, confirmation, timestamp
and code-editor components. `/runtime/module-registry` shares enabled-module
and extension hooks; it consumes the public SDK, API and auth contexts. Feature
modules use these exports instead of importing another module's source.

The host imports `@elsa-workflows/react-studio-ui/styles.css` once. It contains the
existing Tailwind v4 theme tokens, five Elsa palettes, light/dark variants and
retained shadcn stylesheet with its MIT attribution. Components use the host's
`data-elsa-theme` attribute and `dark` class. Modules inherit those tokens and
share the host's React and provider instances.

A separately built module must include its own Tailwind source scan when adding
utilities beyond the shared primitives. Do not depend on the host scanning an
external module's source files at runtime.


The lower-level `tokens.css` export contains Tailwind `@theme`/`@apply` rules. Import
it from a CSS entry after your Tailwind import so its utilities have that context.
`styles.css` already imports Tailwind, tokens and shadcn styles; host applications
normally import it once, while extension modules inherit the host's styles.
