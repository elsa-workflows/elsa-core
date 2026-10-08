# Forms Contract

`FormReference` contains `provider`, `id`, and an optional version selector. The replaceable `IUserTaskFormProvider` resolves a reference during activation, returns a version-pinned render descriptor, and validates/normalizes completion data.

- A selector such as `latest` is resolved once and stored as a concrete version on the task.
- Resolution failure creates a blocking manager-only health issue; it never silently substitutes a different form.
- Repair retries the original reference. V1 provides no live editing or repinning of an open task.
- Workers receive only the provider-neutral render descriptor after assignment. Studio delegates rendering to a registered renderer and shows an unsupported-provider state otherwise.
- Submitted data is bounded to 256 KiB by default, validated by the same provider and pinned version, normalized, then stored and supplied to the workflow result.
- A task without a form accepts no arbitrary data; completion consists only of a configured action.
- Form data is protected, excluded from search and audit payloads, and retained/purged with the task.
- V1 has no native form builder or draft protocol.
