# Contract: Catalog and Introspection Endpoints

**Status**: Draft for review

Three read-only endpoints support role authoring and client-side rendering. All live in `Elsa.Identity` under the standard Elsa route prefix (`elsa/api` by default), but they do not share an access requirement:

| Endpoint | Access | Purpose |
| --- | --- | --- |
| `GET /identity/permissions` | `identity/roles:view` | The catalog a role editor renders |
| `GET /identity/permissions/reach` | `identity/roles:view` | What a wildcard grant currently covers |
| `GET /identity/me/permissions` | authenticated only | The caller's own effective grants |

The first two are permission-guarded because they describe what roles *can* contain. The third is deliberately not: any authenticated principal may ask what it holds.

## `GET /identity/permissions` — the catalog

Returns every registered resource with its metadata and supported verbs. This is what a role editor renders; no client should hard-code permission strings.

**Requires**: `identity/roles:view` — if you may see roles, you may see what roles can contain.

```json
{
  "coreVerbs": ["view", "create", "update", "write", "delete", "execute"],
  "resources": [
    {
      "resource": "workflows/definitions",
      "displayName": "Workflow definitions",
      "description": "Author, publish, and run workflow definitions.",
      "category": "Workflows",
      "supportedVerbs": ["view", "write", "delete", "execute", "publish", "retract"],
      "nonCoreVerbs": ["publish", "retract"],
      "verified": true
    },
    {
      "resource": "acme/widgets",
      "displayName": "acme/widgets",
      "description": "No descriptor registered; inferred from an endpoint declaration.",
      "category": "Unverified",
      "supportedVerbs": ["view"],
      "nonCoreVerbs": [],
      "verified": false
    }
  ]
}
```

`verified: false` marks an implicit descriptor auto-registered for a third-party permission that resolved to no declared descriptor. The module keeps working and the gap stays visible — see [research.md](../research.md) D9.

`coreVerbs` is the recommended set modules should reuse; `nonCoreVerbs` flags a resource's module-specific verbs so a reviewer can spot needless synonyms. Neither restricts what a module may declare — see [permissions.md](permissions.md).

The wildcard `*` is deliberately absent from both lists. It is not a verb a user selects from a menu; it is the "any verb" grant, written as `workflows/definitions:*`, and it is the only construct on this axis with forward reach.

## `GET /identity/permissions/reach` — wildcard reach report

Answers "what does this grant actually cover right now", which is the mitigation for forward reach on the resource axis.

**Requires**: `identity/roles:view`

**Query**: `?resource=workflows/*`

```json
{
  "resource": "workflows/*",
  "covers": [
    "workflows/definitions",
    "workflows/definitions/versions",
    "workflows/instances",
    "workflows/descriptors/activities"
  ],
  "count": 22
}
```

The response is a point-in-time snapshot. A wildcard grant also covers resources registered later; the report says what is registered now, and the role editor should present it as such rather than as a fixed list.

## `GET /identity/me/permissions` — the caller's effective grants

Returns the union of grants across all roles held by the calling principal, in the current tenant context. Clients use this to hide sections, disable actions, and show read-only states without probing endpoints.

**Requires**: authentication only.

```json
{
  "grants": [
    { "resource": "workflows/definitions", "verbs": ["view", "publish"] },
    { "resource": "workflows/instances",   "verbs": ["view", "execute"] },
    { "resource": "dashboard",             "verbs": ["view"] },
    { "resource": "secrets",               "verbs": [] },
    { "resource": "identity/users",        "verbs": [] }
  ]
}
```

Three properties matter here:

**Every registered resource is present, including those with an empty `verbs` array.** A client can then distinguish "explicitly denied" from "unknown to this server", which is what makes it safe to drive rendering from this response. This follows the original proposal's stated requirement, with an empty array standing where their contract had `scope: 0`.

**Verbs are resolved, not wildcarded.** A principal granted `workflows/*:*` sees each covered resource listed with its concrete supported verbs, rather than a literal `"*"`. Clients then need no matching logic: the check is `verbs.includes(required)`.

**This replaces the integer `scope` field** the proposed contract specified. The bitwise check `(userScope & requiredScope) === requiredScope` becomes array containment, which is the same semantics — no verb ever implied another in either model — and avoids an administrator's grant rendering as `4294967295`.

**The source of truth is always server-side.** This response exists for rendering; it is not an authorization decision, and every protected endpoint re-evaluates independently.

## Staleness

The three endpoints have different freshness characteristics, and clients should not cache them alike.

**The catalog and the reach report are registry snapshots.** They describe what the server has registered, not what the caller holds, so they are unaffected by role changes and token issuance. They change only when installed modules change, which for most deployments means on restart.

**`GET /identity/me/permissions` reflects the caller's token**, which carries permission claims issued at sign-in. A role change therefore takes effect on the next token issuance, bounded by the access-token lifetime, or sooner where the optional security stamp is enabled. Clients that surface role administration should refresh after a change rather than assuming this response is live.

In all cases the source of truth is server-side: these responses exist for rendering, and every protected endpoint re-evaluates independently.
