# Import/Export Contract: Secrets Module

## Export Modes

### Reference-Only Export

Default mode. Exports safe metadata and technical-name references only.

```json
{
  "technicalName": "smtp-password",
  "type": "Text",
  "scope": "Email",
  "storeName": "ElsaEncrypted",
  "referenceOnly": true
}
```

### Encrypted Value Export

Requires `export:secrets` and an explicit encryption target.

```json
{
  "technicalName": "smtp-password",
  "type": "Text",
  "scope": "Email",
  "storeName": "ElsaEncrypted",
  "referenceOnly": false,
  "encryptedPayload": {
    "algorithm": "recipient-selected",
    "keyId": "import-target-key",
    "cipherText": "..."
  }
}
```

Rules:

- Export packages never contain raw values.
- Encrypted export does not use shared Data Protection keys as the portability mechanism.
- Stores may refuse encrypted export when they cannot read or export values safely.

## Import Conflict Behavior

Same-technical-name conflicts fail by default.

Allowed explicit behaviors:

- `create-new`: create a new secret with a user-provided different technical name.
- `update-rotate`: rotate the existing secret with the imported payload.
- `skip`: leave the existing secret unchanged.

## Import Results

Import reports safe item-level results:

```json
{
  "technicalName": "smtp-password",
  "result": "Conflict",
  "code": "TechnicalNameExists",
  "message": "A secret with this technical name already exists."
}
```

Rules:

- Failed decryptions report the technical name and safe code only.
- Import does not partially rotate a conflicting secret unless `update-rotate` is explicit.
- Reference-only import can validate or create metadata without importing a value.
