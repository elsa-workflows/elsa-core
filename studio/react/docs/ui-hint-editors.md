# Specialized activity input editors

ActivityEditors contributes its built-in editors through the SDK `uiHints` slot.
Extension packages use the same slot; the HTTP and email examples in the tests
exercise the same renderer, rather than a separate list of extension controls.
Overrides retain slot ordering: a smaller `order` wins, followed by stable module
and contribution IDs. Use an explicit priority when replacing a built-in hint.

| Hint | Editing behavior | Server value |
| --- | --- | --- |
| `datetime-picker` | Date, optional time, clear time, read-only state | Literal ISO instant; the controls show browser-local time |
| `dynamic-outcomes` | Add/remove outcomes; fixed outcomes remain visible and cannot be removed | Wrapped `Object` expression containing a JSON string array; unwrapped properties use an array |
| `http-status-codes` | Add/remove integer codes from 100 through 599 | Wrapped integer-array inputs use Object JSON; unwrapped status cases preserve their child activity and additional fields |
| `input-picker` | Select or clear a declared workflow input | Wrapped `Input` expression contains the complete serialized input definition; unwrapped properties retain the object |

Dynamic outcomes also update designer ports from either array or JSON-string values,
including the descriptor's `DynamicOutcomesOptions.fixedOutcomes`. The input picker
receives full input definitions through the SDK editor context, including display,
type and storage metadata.

To add an extension hint:

```tsx
export function register(api: StudioApi) {
  api.contribute("uiHints", {
    id: "email-address",
    hint: "email-address",
    component: EmailAddressEditor,
  });
}
```

Use `UiHintEditorProps`, honor `readOnly`, and write the wrapped or unwrapped value
described by the input descriptor. Unknown hints retain the single-line fallback.
Unit fixtures cover the server encodings and retained data. The browser test loads
the actual ActivityEditors ESM entry through the manifest, edits each registered
control, checks Amsterdam local-time conversion, and renders an extension-provided
email hint through the same public slot.
