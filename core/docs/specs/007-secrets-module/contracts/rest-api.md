# REST API Contract: Secrets Module

All endpoints use the Elsa API route prefix. General responses are metadata-only and never include raw values, encrypted payloads, external-store lookup secrets, or provider-private metadata.

## Permissions

- `read:secrets`: list and inspect metadata.
- `write:secrets`: create metadata and rotate/replace values.
- `delete:secrets`: delete secrets.
- `test:secrets`: test configured resolution without exposing values.
- `export:secrets`: encrypted value export.
- `import:secrets`: import references or encrypted payloads.

## List Secrets

`GET /secrets?search=&type=&scope=&store=&status=&page=&pageSize=`

Response body:

```json
{
  "items": [
    {
      "technicalName": "smtp-password",
      "displayName": "SMTP Password",
      "type": "Text",
      "scope": "Email",
      "storeName": "ElsaEncrypted",
      "status": "Active",
      "latestVersion": 3,
      "expiresAt": null,
      "createdAt": "2026-05-19T10:00:00Z",
      "updatedAt": "2026-05-19T11:00:00Z"
    }
  ],
  "total": 1
}
```

## Get Secret Metadata

`GET /secrets/{technicalName}`

Returns one metadata model. Returns `404` when not found or unavailable to the caller.

## Create Secret

`POST /secrets`

```json
{
  "technicalName": "smtp-password",
  "displayName": "SMTP Password",
  "description": "Password used by SMTP settings",
  "type": "Text",
  "scope": "Email",
  "storeName": "ElsaEncrypted",
  "value": "submitted-only-on-create-or-rotate",
  "expiresAt": null
}
```

Rules:

- `technicalName` is immutable and unique after normalization.
- The request may contain a value or store-specific reference metadata.
- The response is metadata-only.

## Rotate Secret

`POST /secrets/{technicalName}/rotate`

Accepts the same value or store-specific reference payload shape as create. The response is metadata-only and reports the new version number.

## Revoke Or Delete

- `POST /secrets/{technicalName}/revoke`
- `DELETE /secrets/{technicalName}`

Both operations emit audit events and do not return payload material.

## Test Secret

`POST /secrets/{technicalName}/test`

Response:

```json
{
  "success": true,
  "code": "Ok",
  "message": "Secret resolved successfully."
}
```

Rules:

- Test never returns the resolved value.
- Safe failure codes include `NotFound`, `Inactive`, `Expired`, `Revoked`, `StoreUnavailable`, `TypeMismatch`, and `Unauthorized`.

## Descriptors

- `GET /secrets/types`
- `GET /secrets/stores`

Return safe `SecretTypeDescriptor` and `SecretStoreDescriptor` collections for Studio and clients.

## Picker Query

`POST /secrets/picker/query`

Request:

```json
{
  "allowedTypes": ["Text"],
  "allowedScopes": ["Email"],
  "requiredCapabilities": ["Read"],
  "status": "Active",
  "search": "smtp",
  "page": 1,
  "pageSize": 20
}
```

Response is metadata-only and contains only compatible secrets.

## No-Reveal Rule

There is no endpoint that reveals current cleartext secret values after creation.
