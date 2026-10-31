# Studio Contract: Secrets Module

The Core/server feature defines these contracts for a paired `elsa-studio` implementation. This feature does not implement Studio UI.

## Secrets Area

Studio should expose a security/settings area that uses the REST APIs to:

- List metadata with search and filters for type, scope, store, and status.
- Create a secret by choosing type, store, technical name, metadata, and type-specific payload.
- Rotate or replace values without showing the current value.
- Revoke or delete secrets.
- Test resolution without displaying values.
- Start encrypted export/import flows where the user has permission.

## Secret Picker

The picker is used by workflow activity editors and module settings editors.

Input context:

```json
{
  "allowedTypes": ["Text"],
  "allowedScopes": ["ConnectionString"],
  "requiredCapabilities": ["Read"],
  "consumer": "Elsa.Sql.SqlQuery.ConnectionString",
  "allowInlineCreate": true
}
```

Output:

```json
{
  "technicalName": "orders-db-connection",
  "type": "Text",
  "scope": "ConnectionString"
}
```

Rules:

- The serialized workflow/module value stores the immutable technical name.
- The picker displays safe metadata only.
- Inline creation is available only when the caller has permission and the selected store/type combination supports creation.
- Picker filters must include type, scope, status, store capability, and consumer context.

## Type Editors

Secret type descriptors provide editor metadata. Studio maps descriptor/editor keys to installed components.

Required v1 editor contracts:

- `Text`: value entry and optional generated value support.
- `RsaKey`: generate or submit key material according to server validation.
- `X509CertificateReference`: submit certificate reference metadata only.

When a type editor is unavailable, Studio should show metadata and block editing payload fields rather than falling back to an unsafe text box.

## Sensitive Workflow Inputs

Inputs marked with existing sensitive metadata are eligible for a secret-aware editor.

Rules:

- Existing literal values are not auto-converted unless a migration/import flow does so explicitly.
- Selecting a secret persists a `SecretReference` value, not the resolved value.
- Workflow execution resolves the latest active version at runtime.
