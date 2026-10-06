# Owned backend runtime configuration

`start_pair` generates a fresh random 32-byte Secrets encryption key for every
owned runtime. Its base64 encoding enters only the backend environment as
`Fixture__SecretsEncryptionKey`. It is absent from materialized files, process
arguments, browser handles, Studio environment, readiness and retained evidence.
The package-based Secrets feature configures `SecretsOptions.EncryptionKey`
through `ConfigureOptions`; it rejects missing or incorrectly sized keys when
Secrets is enabled. Stopping the owned process ends the key's lifetime.

The Python `permission_grants(request)` function supplies the exact allowlisted
array as private backend environment configuration. Inherited `Fixture` settings
are removed before the owned settings are supplied. Readiness reports the exact
profile and actual grant array, plus independent `workflow_contexts_enabled` and
`secrets_enabled` registration flags. These are configuration facts, not proof of
endpoint authorization or browser behavior.

- `full` retains the original `*` grant.
- `denied` retains the original four read grants and remains the broad write-denial
  profile.
- `deny-secrets` permits workflow viewing, editing, publishing and execution, and
  WorkflowContexts descriptor reads, while granting no Secrets permission.
- `deny-workflow-contexts` permits the same workflow operations and Secrets
  operations, while omitting `read:workflow-context-provider-descriptors`.

The explicit editor grants use the published Core 3.8.4 legacy endpoint vocabulary
at `33181ae3048f628f591a0155b5665a8e4d1bcea2`, including
`write:workflow-definitions` and `exec:workflow-definitions`. Core 3.9.0 at
`6436609a1a3874d3fea7ccf690897792f5a1f702` and accepted candidate source
`ba5b348aa2414fdf7c19d9d8806e87b7c91a4205` use the resource catalog, including
`workflows/definitions:write` and `workflows/definitions:execute`. The canonical
Secrets module likewise changes from 3.8.4 `read:secrets`/`write:secrets` to
3.9/3.10 `secrets:view`/`secrets:write`. WorkflowContexts retains its published
descriptor permission in all selected versions. Neither targeted denial profile
uses a wildcard, and feature registration does not change its grants.

Synthetic Secrets creation can use the existing authenticated package API:
`POST {backend_url}/secrets` with a unique synthetic `name` and `value` under the
full profile. The canonical request defaults to a text secret in the encrypted
store, and its response returns metadata rather than the secret value. The
browser owner must keep synthetic values and authorization inputs private and
verify the returned identity/reference using the real UI/API. No fixture seed
endpoint or unauthenticated production endpoint is added. The exact existing
picker/descriptors API and versioned reference shape must be observed before a
journey claims successful secret selection or execution.
