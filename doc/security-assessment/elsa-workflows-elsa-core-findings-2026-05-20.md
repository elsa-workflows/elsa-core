# Unsandboxed Python.NET execution exposes the host process

## Details
The Python scripting integration runs workflow-defined code through Python.NET with CLR interop enabled and no sandbox. Equivalent risk to the C# scripting issue.

## Location
[src/modules/Elsa.Python/Services/PythonEvaluator.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Python/Services/PythonEvaluator.cs#L1)

## Impact
Workflow author executes arbitrary Python with CLR interop and host-level privileges

## Reproduction steps
1. Workflow author writes `import clr; clr.AddReference('System.Diagnostics'); from System.Diagnostics import Process; Process.Start('cmd','/c whoami')`. The workflow engine executes the command as the server identity.

## Recommended fix
Gate the Python script activity behind an elevated permission and warn explicitly that it executes with full host privileges; consider out-of-process execution if untrusted authors are permitted.

---
**Severity:** HIGH
**Status:** Open
**Category:** Improper Control of Generation of Code (CWE-94)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Path traversal through x-download-id header in ZipManager

## Details
ZipManager constructs a download path by appending the value of the x-download-id request header to a base directory without canonicalizing or rejecting traversal sequences. A value like `../../../../etc/passwd` escapes the intended directory and discloses arbitrary files readable by the service account.

## Location
[src/modules/Elsa.Workflows.Api/Files/ZipManager.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Api/Files/ZipManager.cs#L1)

## Impact
Attacker reads arbitrary files from the server filesystem

## Reproduction steps
1. Attacker sends GET /elsa/api/.../download with header `x-download-id: ../../../../etc/passwd`. The server reads /etc/passwd and returns it.

## Recommended fix
Validate the download identifier against a whitelist of generated tokens. Reject any value containing path separators or `..` and resolve the canonical path before opening the file.

---
**Severity:** HIGH
**Status:** Open
**Category:** Path Traversal (CWE-22)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Unauthenticated polymorphic JSON deserialization enables remote code execution

## Details
The bookmark Resume endpoint is decorated with AllowAnonymous(). When token validation fails at line 30, AddError("Invalid token.") is called but execution is NOT halted with `return`. Control then flows through GetInputFromQueryString() (line 33) which calls _payloadSerializer.Deserialize<IDictionary<string, object>>(inputJson) on the attacker-controlled `?in=` query string. The payload serializer registers PolymorphicObjectConverterFactory + TypeJsonConverter (JsonPayloadSerializer.cs:82-83). PolymorphicObjectConverter.Read parses an attacker-supplied `_type` discriminator and falls back to Type.GetType(typeAlias) in TypeJsonConverter.cs:50, allowing instantiation of any assembly-qualified .NET type, equivalent to TypeNameHandling.All. Combined with gadgets such as System.Windows.Data.ObjectDataProvider or System.Configuration.Install.AssemblyInstaller this yields arbitrary code execution. Token validation does not block the attack because validation failure does not stop the handler.

## Location
[src/modules/Elsa.Workflows.Api/Endpoints/Bookmarks/Resume/Endpoint.cs:30](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Api/Endpoints/Bookmarks/Resume/Endpoint.cs#L30)

## Impact
Unauthenticated attacker achieves remote code execution by instantiating arbitrary CLR types

## Reproduction steps
1. Attacker POSTs to /elsa/api/bookmarks/{anything}/resume?in={"$values":[{"_type":"System.Windows.Data.ObjectDataProvider, PresentationFramework","MethodName":"Start","ObjectInstance":{"_type":"System.Diagnostics.Process","StartInfo":{"FileName":"cmd.exe","Arguments":"/c calc"}}}]} with no auth header. The handler calls AddError("Invalid token.") but continues; the query JSON is deserialized through the polymorphic converter; the ObjectDataProvider gadget triggers Process.Start during construction, yielding remote code execution as the workflow server user.

## Recommended fix
After validation failure, terminate the request before any attacker-controlled data is parsed. Replace the polymorphic object/dictionary converter with a strict, schema-bound deserialization model that does not honor a `_type` discriminator on untrusted endpoints, and remove the Type.GetType fallback in TypeJsonConverter so only well-known aliases resolve to safe types.

---
**Severity:** HIGH
**Status:** Open
**Category:** Insecure Deserialization (CWE-502)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Authenticated polymorphic JSON deserialization in workflow instance import

## Details
The Import endpoint requires the write:workflow-instances permission and calls _workflowStateSerializer.Deserialize(model.WorkflowState) and _payloadSerializer.Deserialize<object>(payloadElement). Both serializers register PolymorphicObjectConverterFactory and the TypeJsonConverter with Type.GetType fallback. Any imported `_type` value loads and instantiates arbitrary assembly-qualified types, providing the same gadget primitive as the bookmark-resume issue but requiring authentication.

## Location
[src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances/Import/Endpoint.cs:112](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances/Import/Endpoint.cs#L112)

## Impact
Authenticated user with import permission achieves remote code execution

## Reproduction steps
1. An attacker with a low-privilege workflow operator token submits an exported workflow instance containing a properties payload {"_type":"<gadget>",...} to /workflow-instances/import. During state restoration the polymorphic converter activates the gadget chain, executing attacker code under the server identity.

## Recommended fix
Restrict workflow-state deserialization to a closed set of types via a strict type registry, drop the Type.GetType fallback, and require an additional administrator role to import workflow state from untrusted sources.

---
**Severity:** HIGH
**Status:** Open
**Category:** Insecure Deserialization (CWE-502)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# AdminApiKeyProvider grants admin to all-zero GUID API key

## Details
DefaultApiKey is `Guid.Empty.ToString()` and the provider returns an IApiKey with claims `permissions=*` whenever that value is submitted. Any deployment that wires the AdminApiKeyProvider (the default for several samples) grants full administrative privileges to a request that simply presents the all-zero GUID as the API key.

## Location
[src/modules/Elsa.Identity/Providers/AdminApiKeyProvider.cs:15](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Identity/Providers/AdminApiKeyProvider.cs#L15)

## Impact
Anyone holding the empty GUID `00000000-0000-0000-0000-000000000000` becomes admin

## Reproduction steps
1. Attacker sends `Authorization: ApiKey 00000000-0000-0000-0000-000000000000` to any management endpoint; the provider returns admin claims and the request proceeds with full privileges.

## Recommended fix
Remove the static fallback. Require explicit provisioning of API keys, store only salted hashes, and refuse to issue an admin key without explicit operator action.

---
**Severity:** HIGH
**Status:** Open
**Category:** Use of Hard-coded Credentials (CWE-798)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# AdminUserProvider returns admin for any username when password is `password`

## Details
AdminUserProvider hashes the literal string `password` once and returns the admin user from FindAsync regardless of the supplied filter. Coupled with the login flow, any (username, password=`password`) pair authenticates as admin.

## Location
[src/modules/Elsa.Identity/Providers/AdminUserProvider.cs:19](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Identity/Providers/AdminUserProvider.cs#L19)

## Impact
Authentication accepts any username with the static password `password` as admin

## Reproduction steps
1. Attacker POSTs to /identity/login with username `attacker` and password `password`. The provider returns the admin user; a JWT with permissions=`*` is issued.

## Recommended fix
Delete the static admin provider. Require operators to seed users explicitly; validate the username against the supplied filter.

---
**Severity:** HIGH
**Status:** Open
**Category:** Authentication Bypass by Hard-coded Credentials (CWE-798/CWE-287)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# LocalHost permission handler grants admin to any request marked local

## Details
The LocalHostPermissionRequirementHandler succeeds any permission requirement when the HttpContext indicates the connection is local. In containerized or proxy deployments the loopback check trivially passes for traffic forwarded through reverse proxies that do not strip X-Forwarded-For, and for any process colocated with the server. This bypasses all permission checks for those callers.

## Location
[src/modules/Elsa.Identity/AuthorizationHandlers/LocalHostPermissionRequirementHandler.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Identity/AuthorizationHandlers/LocalHostPermissionRequirementHandler.cs#L1)

## Impact
Attacker who can spoof loopback gains administrative authorization

## Reproduction steps
1. Attacker controls a sidecar or compromises any process on the same host. They send requests over loopback (or through a proxy that preserves loopback semantics) and inherit admin authorization without any credentials.

## Recommended fix
Remove implicit loopback admin entirely, or gate it behind a startup-time opt-in plus a signed local token rather than IP-only check.

---
**Severity:** HIGH
**Status:** Open
**Category:** Improper Authorization (CWE-285)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Privilege escalation through client-supplied role assignment on Users/Create

## Details
The Users Create and Update endpoints accept a Roles collection directly from the request body and assign it without verifying that the caller has the authority to grant those roles. A user with `write:users` (intended for user management, not role management) can therefore create a user with the admin role and obtain credentials for it.

## Location
[src/modules/Elsa.Identity/Endpoints/Users/Create/Endpoint.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Identity/Endpoints/Users/Create/Endpoint.cs#L1)

## Impact
Low-privilege caller assigns admin role to a newly created user

## Reproduction steps
1. Attacker with `write:users` POSTs {"name":"backdoor","password":"x","roles":["admin"]}. The new account is created with admin permissions; the attacker logs in as `backdoor`.

## Recommended fix
Validate role assignments server-side: a caller may only grant roles whose permissions are a subset of their own, and a dedicated `manage:roles` permission must gate admin role assignment.

---
**Severity:** HIGH
**Status:** Open
**Category:** Improper Privilege Management (CWE-269)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# HTTP endpoint trusts ContentLength header for body-size check

## Details
HttpEndpoint reads HttpContext.Request.ContentLength ?? 0 to enforce a maximum body size and selects a streaming/buffering strategy from it. A chunked-encoded request reports ContentLength = null, so the check is skipped and the full body is read into memory regardless of the configured cap.

## Location
[src/modules/Elsa.Http/Activities/HttpEndpoint.cs:303](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Http/Activities/HttpEndpoint.cs#L303)

## Impact
Attacker bypasses request-size limits with chunked transfer encoding.

## Reproduction steps
1. Attacker sends a chunked POST to a workflow HTTP-trigger endpoint with body size far larger than the configured limit. The server reads it all, exhausting memory and degrading service.

## Recommended fix
Use Request.EnableBuffering + a counting stream wrapper, or honor IHttpMaxRequestBodySizeFeature, so the limit is enforced regardless of ContentLength presence.

---
**Severity:** HIGH
**Status:** Open
**Category:** Resource Exhaustion / DoS
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Default admin credentials with reversible password baked into config

## Details
The Identity.Users section seeds default users (admin, alice, bob) with HashedPassword and Salt values committed in the repository. The hash algorithm is single-round SHA-256 over (UTF-8 password || salt). Cracking HashedPassword='TfKzh9RLix6FPcCNeHLkGrysFu3bYxqzGqduNdi8v1U=' with the committed salt resolves to the plaintext 'password' in milliseconds. Because Elsa.Identity seeds these users into the identity store on first run (and the Docker image ships the same config), every untouched deployment exposes a working administrator login.

## Location
[src/apps/Elsa.Server.Web/appsettings.json:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/apps/Elsa.Server.Web/appsettings.json#L1)

## Impact
Unmodified deployments accept login as admin with password 'password' from any client.

## Reproduction steps
1. 1) Attacker reaches the Identity login endpoint. 2) Attacker submits username=admin, password=password. 3) Login succeeds and returns a JWT (also signed with the hardcoded key from the prior finding). 4) Attacker now has full admin access without prior credentials.

## Recommended fix
Do not seed users with known credentials. Either require operator-supplied bootstrap credentials, generate a random password at first start and surface it to the operator once, or refuse to seed when running outside an explicit 'demo' profile.

---
**Severity:** HIGH
**Status:** Open
**Category:** Auth/access
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Hardcoded JWT signing key shipped in default server configuration

## Details
The default appsettings.json shipped with Elsa.Server.Web (the host built by docker/ElsaServer.Dockerfile) contains a literal JWT signing key 'sufficiently-large-secret-signing-key' under Identity.Tokens.SigningKey. The DefaultAccessTokenIssuer reads this key directly from configuration to sign JWT bearer tokens. Because the source is public, any deployment that does not explicitly override this value lets anyone with the published key sign arbitrary JWTs and impersonate any principal (including the seeded admin). The sibling app Elsa.ModularServer.Web correctly uses 'CHANGE_ME_TO_A_SECURE_RANDOM_KEY' as a placeholder, demonstrating the intended pattern was not followed in the server-web app.

## Location
[src/apps/Elsa.Server.Web/appsettings.json:71](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/apps/Elsa.Server.Web/appsettings.json#L71)

## Impact
Any network-reachable attacker can forge valid bearer tokens for any user/role.

## Reproduction steps
1. 1) Attacker pulls the public Elsa repository or Docker image. 2) Attacker reads SigningKey 'sufficiently-large-secret-signing-key' from appsettings.json. 3) Attacker mints a JWT signed with that key, claiming roles/permissions of the admin tenant. 4) Attacker calls any authenticated endpoint (e.g., POST /workflow-definitions/import) and gains full administrative control over the workflow engine.

## Recommended fix
Default configuration must not contain a usable signing key. Generate the key at first run, require an explicit operator-provided value, or refuse to start when the key matches a known-default sentinel. Apply the same pattern as Elsa.ModularServer.Web (placeholder that fails fast) to Elsa.Server.Web.

---
**Severity:** HIGH
**Status:** Open
**Category:** Auth/access
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Role overwrite via id collision on Roles/Create

## Details
The Roles Create endpoint accepts a client-supplied Id and persists the role with that identifier. If the attacker submits an id equal to an existing role (such as the admin role id, which is well known in default seeds), the existing role document is overwritten, replacing its permissions with attacker-controlled values.

## Location
[src/modules/Elsa.Identity/Endpoints/Roles/Create/Endpoint.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Identity/Endpoints/Roles/Create/Endpoint.cs#L1)

## Impact
Caller overwrites the admin role document with attacker-defined permissions

## Reproduction steps
1. Attacker with `write:roles` POSTs {"id":"admin","permissions":["*"]} or replaces the admin role permissions list, then assigns themselves the (now-attacker-controlled) admin role.

## Recommended fix
Refuse to honor a caller-supplied id on create; always generate a new id server-side and reject updates that target privileged role ids unless the caller has elevated authority.

---
**Severity:** HIGH
**Status:** Open
**Category:** Improper Privilege Management (CWE-269)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Unsandboxed Roslyn C# script execution exposes the host process

## Details
The C# scripting feature compiles and runs workflow-author-provided source via Roslyn with reference to system assemblies and no sandbox. Any caller able to define or modify a workflow can execute arbitrary code, including reading secrets, writing files, or shelling out.

## Location
[src/modules/Elsa.CSharp/Services/RoslynCSharpEvaluator.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.CSharp/Services/RoslynCSharpEvaluator.cs#L1)

## Impact
Workflow author executes arbitrary C# code under the workflow runner identity

## Reproduction steps
1. Workflow author adds an Inline C# step containing `System.Diagnostics.Process.Start("cmd","/c whoami")`. Running the workflow executes the command as the server identity.

## Recommended fix
Treat workflow authoring as a privileged trust boundary and document this clearly. If untrusted authors are allowed, restrict the scripting feature behind a separate elevated permission and consider AppDomain/process isolation or removing scripting entirely.

---
**Severity:** HIGH
**Status:** Open
**Category:** Improper Control of Generation of Code (CWE-94)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Polymorphic JSON deserialization resolves attacker-supplied .NET type via _type discriminator

## Details
PolymorphicObjectConverter.Read peels a '_type' property off any incoming JSON value typed as object / ExpandoObject / Dictionary<string,object> / IDictionary<string,object>, then resolves the string via Type.GetType(typeName) (line 341) with no allowlist and passes the resolved Type into JsonSerializer.Deserialize(ref reader, targetType, newOptions) (line 44) or Activator.CreateInstance(targetType) (line 124). The converter is registered globally by PolymorphicObjectConverterFactory in JsonPayloadSerializer.GetOptions, so every API surface that consumes IApiSerializer / JsonPayloadSerializer is in scope. Concretely, WorkflowDefinitionModel.CustomProperties, activity custom properties, workflow Variables, Inputs, Outputs, and bookmark Resume payloads are all object/IDictionary<string,object> bags. An attacker with workflow write/import permission can persist a malicious _type tag that fires on every load.

## Location
[src/modules/Elsa.Workflows.Core/Serialization/Converters/PolymorphicObjectConverter.cs:341](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Core/Serialization/Converters/PolymorphicObjectConverter.cs#L341)

## Impact
Authenticated workflow author can drive STJ to instantiate arbitrary host-loaded types and run their property setters.

## Reproduction steps
1. 1) Authenticated user with workflow write permission POSTs /workflow-definitions/import with CustomProperties containing {"x":{"_type":"<dangerous .NET type>, <assembly>", "Prop":"value"}}. 2) On import, JsonPayloadSerializer deserializes the model; PolymorphicObjectConverter calls Type.GetType on the attacker string and JsonSerializer.Deserialize into that type, invoking constructors and property setters. 3) The same payload reactivates on every subsequent load of the definition, giving a persistent gadget surface tied to whatever STJ-reachable side-effecting types are loaded in the host process.

## Recommended fix
The serializer must refuse unknown _type discriminators rather than falling back to Type.GetType. Restrict polymorphic resolution to an explicit allowlist (e.g., types registered with IWellKnownTypeRegistry and IActivityRegistry). The same constraint must apply on read paths from the database, not only on import.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Deserialization
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Non-cryptographic randomness used to generate secrets and passwords

## Details
DefaultRandomStringGenerator uses System.Random, a non-cryptographic PRNG, to produce random strings. DefaultSecretGenerator wraps this generator, and it is consumed by the application-create endpoint (to generate API client secrets) and by UserManager to generate temporary passwords. System.Random is seeded from Environment.TickCount by default; an attacker who can approximate the time a secret was issued can brute-force the seed (small 32-bit space) and recover the exact secret. The hashed value at rest does not protect against this because the attacker reconstructs the plaintext and presents it to the authentication endpoint.

## Location
[src/modules/Elsa.Identity/Services/DefaultRandomStringGenerator.cs:29](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Identity/Services/DefaultRandomStringGenerator.cs#L29)

## Impact
Allows attackers to predict generated application client secrets and reset passwords.

## Reproduction steps
1. Admin provisions an Elsa Application; the server responds with a freshly generated client secret derived from System.Random. An attacker who observes the approximate time of provisioning (e.g. via timing of an HTTP response, an audit log, or a leaked notification) enumerates Environment.TickCount seeds around that moment, reproduces DefaultRandomStringGenerator's output for each, and locates the matching secret. The attacker then authenticates to the API as that application and exercises its permissions.

## Recommended fix
Use a cryptographically secure random source (RandomNumberGenerator.GetBytes / GetString) for any value that grants authentication or authorization, including client secrets, API keys, and password resets.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Weak-cryptography
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Polymorphic deserialization resolves attacker-controlled .NET types

## Details
TypeJsonConverter resolves a JSON-supplied type alias by first checking the well-known type registry and then falling back to Type.GetType(typeAlias) with no allow-list. PolymorphicObjectConverter then calls JsonSerializer.Deserialize(ref reader, targetType, newOptions) for the resolved type. The same pattern is reached via VariableDefinitionMapper.Map (Type.GetType on source.TypeName at line 23 and on source.StorageDriverTypeName at line 100) and VariableMapper.Map (line 62). These converters are wired into JsonPayloadSerializer and JsonWorkflowStateSerializer (but not SafeSerializer), and both are reachable from the workflow definition/instance import endpoints which only require an authenticated user with workflow write permission. While System.Text.Json does not invoke arbitrary setters as freely as Newtonsoft.Json, attacker-controlled type instantiation still enables type-confusion, denial-of-service via heavy constructors / large allocations, and is a stepping stone toward gadget-based RCE if a suitable type is present in the loaded assembly set.

## Location
[src/modules/Elsa.Workflows.Core/Serialization/Converters/TypeJsonConverter.cs:50](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Core/Serialization/Converters/TypeJsonConverter.cs#L50)

## Impact
Authenticated workflow editors can instantiate arbitrary loaded .NET types via crafted import payloads.

## Reproduction steps
1. An authenticated user with workflow:write submits a crafted workflow definition whose variables or polymorphic payload nodes embed a _type discriminator naming an assembly-qualified .NET type not in the well-known registry. TypeJsonConverter.Read resolves the type via Type.GetType, and PolymorphicObjectConverter (or VariableDefinitionMapper) calls Activator.CreateInstance / JsonSerializer.Deserialize against it. The attacker selects a type whose construction has dangerous side effects in the loaded assembly set (file I/O, process spawn through indirect setters, large allocations, or known gadget chains) to corrupt server state or degrade availability.

## Recommended fix
Constrain polymorphic deserialization to an explicit allow-list of safe types and reject any _type value not in that allow-list. Do not fall back to Type.GetType on caller-supplied strings.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Deserialization
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Password and secret hashing uses unsalted single-pass SHA-256

## Details
DefaultSecretHasher hashes credentials with a single SHA-256 invocation and no per-credential salt, no KDF, and no iteration count. SHA-256 is a fast hash function evaluable at billions of guesses per second on commodity GPUs, so an attacker who exfiltrates the hash store (via SQL injection, backup theft, insider access, or any database compromise) can recover every weak-to-medium password and any short API secret very rapidly. The lack of a per-record salt also enables rainbow-table reuse across deployments.

## Location
[src/modules/Elsa.Identity/Services/DefaultSecretHasher.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Identity/Services/DefaultSecretHasher.cs#L1)

## Impact
Stolen hash database can be brute-forced offline at high speed to recover passwords and API secrets.

## Reproduction steps
1. An attacker obtains a copy of the Elsa identity database (for example via a backup leak or by exploiting any read primitive against persistence). They run a GPU password cracker against the SHA-256 hashes and recover plaintext user passwords and application secrets, then log in to the Elsa API as those principals.

## Recommended fix
Hash credentials with a memory-hard or iterated KDF (Argon2id, scrypt, or PBKDF2 with a high iteration count) plus a unique per-credential salt. Migrate existing hashes opportunistically on next successful authentication.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Weak-cryptography
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Refresh tokens are indistinguishable from access tokens

## Details
The refresh token issuer signs refresh tokens with the same key, audience and issuer as access tokens, and the bearer middleware accepts both. There is no claim distinguishing refresh vs access, so anyone who exfiltrates a refresh token (logs, browser storage, MITM) can use it as a Bearer access token until expiry.

## Location
[src/modules/Elsa.Identity/Services/DefaultRefreshTokenIssuer.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Identity/Services/DefaultRefreshTokenIssuer.cs#L1)

## Impact
A stolen refresh token can be used directly as an access token

## Reproduction steps
1. Attacker steals a refresh token from a logged client request. They send it as `Authorization: Bearer <refresh>` to any API. The bearer middleware validates the signature and authorizes the call.

## Recommended fix
Add a `typ`/`token_use` claim to distinguish refresh and access tokens, and verify on each request that the claim matches the expected token type for the endpoint.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Insufficient Token Validation (CWE-345)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# SignalR workflow instance hub allows cross-tenant observation by id

## Details
WorkflowInstanceHub.ObserveInstanceAsync(string workflowInstanceId) adds the connection to a SignalR group keyed only by the supplied id. There is no authorization check that the caller may observe that instance, so any authenticated client can join the group for any instance id they can guess or enumerate and receive its broadcast events.

## Location
[src/modules/Elsa.Workflows.Runtime.SignalR/Hubs/WorkflowInstanceHub.cs:1](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Runtime.SignalR/Hubs/WorkflowInstanceHub.cs#L1)

## Impact
Authenticated user observes workflow instance events belonging to other users/tenants

## Reproduction steps
1. Attacker (any authenticated user) calls `ObserveInstanceAsync("<victim-instance-id>")`. They begin receiving workflow events (state transitions, incident counts, metadata) for the victim's instance until execution completes.

## Recommended fix
Before joining the group, verify that the caller has read access to the workflow instance (tenant/owner/permission check). Reject otherwise.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Insecure Direct Object Reference (CWE-639)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Dynamic LINQ injection through TimestampFilter.Column

## Details
TimestampFilters loops over caller-controlled entries and concatenates timestampFilter.Column into Dynamic LINQ predicate strings (`query.Where($"{column} >= @0 ...", ...)`). While the List endpoint applies a column whitelist, other consumers (notably Alterations API filters) do not, allowing the caller to supply an arbitrary expression such as `Id == "a\" || true || \""` and influence which records are returned. Depending on column-name acceptance this can leak data or alter alteration scope.

## Location
[src/modules/Elsa.Workflows.Management/Filters/WorkflowInstanceFilter.cs:165](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Management/Filters/WorkflowInstanceFilter.cs#L165)

## Impact
Authenticated caller injects arbitrary EF Core predicates altering query semantics

## Reproduction steps
1. Attacker posts a TimestampFilter with Column set to a crafted Dynamic LINQ expression; the server compiles it and returns or alters rows beyond the intended scope.

## Recommended fix
Move the column whitelist into the filter itself so every consumer enforces it, or reject TimestampFilter entries whose Column is not a known property of WorkflowInstance.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Expression Language Injection (CWE-917)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# SAS token expiration not enforced (broken-by-design issuance)

## Details
CreateToken(payload, lifetime) protects the payload with `ToTimeLimitedDataProtector().Protect(json, lifetime)`, but DecryptToken calls `Unprotect` on the base (non-time-limited) protector. The two protectors derive different purposes, so all time-limited tokens fail to decrypt. Conversely, the no-lifetime overload (`CreateToken(payload)`) produces tokens that never expire. The net effect is that operators who think they are issuing short-lived SAS tokens are either generating unusable tokens or — if they fall back to the overload — minting eternal capabilities.

## Location
[src/modules/Elsa.SasTokens/Contracts/DataProtectorTokenService.cs:25](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.SasTokens/Contracts/DataProtectorTokenService.cs#L25)

## Impact
Tokens issued with a lifetime fail decryption, while feature is silently bypassed

## Reproduction steps
1. Operator believes a SAS token is valid for 5 minutes. In practice the token cannot be redeemed (broken feature). If they switch to the no-lifetime overload to make redemption work, any token the attacker captures from logs or referer headers remains valid forever.

## Recommended fix
Make both create and decrypt go through `ToTimeLimitedDataProtector` consistently, and refuse to compile-out the lifetime parameter so tokens always carry an expiration.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Improper Restriction of Authentication Attempts (CWE-307) / Improper Verification of Cryptographic Signature
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Unauthenticated Resilience SimulateResponse endpoint enables memory exhaustion and unsafe deserialization

## Details
SimulateResponse is decorated AllowAnonymous(). It parses `codes` from the query string via `JsonSerializer.Deserialize<int[]>(codesParam)!` without try/catch, then stores per-sessionId state in a process-wide MemoryCache that grows with every new sessionId an attacker supplies. A crafted invalid JSON triggers an uncaught exception; many random session ids exhaust memory.

## Location
[src/modules/Elsa.Resilience/Endpoints/SimulateResponse/Endpoint.cs:15](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Resilience/Endpoints/SimulateResponse/Endpoint.cs#L15)

## Impact
Anonymous attacker forces unbounded cache growth and uncaught exceptions

## Reproduction steps
1. Attacker scripts thousands of requests with random sessionId values and arbitrary `codes` payloads, growing the MemoryCache until the process OOMs. Sending malformed `codes` JSON throws inside the handler, producing 500s and consuming additional resources.

## Recommended fix
Require authentication, validate codes input with explicit try/catch, bound the cache by size and TTL, and key entries on authenticated identity rather than client-supplied sessionId.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Allocation of Resources Without Limits (CWE-770)
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Workflow import persists before authorization check

## Details
Import.HandleAsync calls ImportSingleWorkflowDefinitionAsync (which persists the workflow via IWorkflowDefinitionImporter.ImportAsync) BEFORE checking the NotReadOnly authorization policy. If the policy fails (read-only mode enabled or target definition is system/readonly), the endpoint returns 403 but the import has already taken effect: the workflow row is created/overwritten in storage. The order is import → authorize → respond, which violates the policy's intent.

## Location
[src/modules/Elsa.Workflows.Api/Endpoints/WorkflowDefinitions/Import/Endpoint.cs:45](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Api/Endpoints/WorkflowDefinitions/Import/Endpoint.cs#L45)

## Impact
Authenticated user can mutate stored workflow state despite read-only/system protections.

## Reproduction steps
1. Operator enables IsReadOnlyMode to lock production. A user with write:workflow-definitions permission still uploads a modified definition; the importer writes it to the database, then the endpoint returns 403. The modification persists despite the read-only guard.

## Recommended fix
Perform the NotReadOnly authorization check (including loading the existing definition by id) BEFORE invoking the importer. Reject the request without mutating storage.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Authorization / TOCTOU
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# ImportFiles endpoint omits per-definition read-only/system check

## Details
ImportFiles.HandleAsync calls AuthorizeAsync with new NotReadOnlyResource() — i.e. no target WorkflowDefinition supplied. The NotReadOnlyRequirementHandler therefore only evaluates ManagementOptions.IsReadOnlyMode; it never checks whether any of the imported files target an existing system/readonly definition. Each file in the upload is then imported one by one with no per-definition authorization.

## Location
[src/modules/Elsa.Workflows.Api/Endpoints/WorkflowDefinitions/ImportFiles/Endpoint.cs:52](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Workflows.Api/Endpoints/WorkflowDefinitions/ImportFiles/Endpoint.cs#L52)

## Impact
Authenticated user can overwrite system/read-only workflow definitions via bulk import.

## Reproduction steps
1. Operator marks a critical workflow as IsSystem=true or IsReadonly=true. A user with write:workflow-definitions uploads a zip containing a modified copy of that definition (same DefinitionId). ImportFiles imports it because the per-definition check is missing.

## Recommended fix
For each imported workflow, run the NotReadOnly check against the existing definition (looked up by DefinitionId) before persisting it.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Authorization / Missing Check
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20

---

# Cross-tenant bookmark resolution in HTTP workflow middleware

## Details
HttpWorkflowsMiddleware constructs the bookmark filter with `TenantAgnostic = true` when looking up HTTP-triggered workflows. The HTTP path/method hash is therefore matched across every tenant in the deployment. In a multi-tenant install this means a request arriving on tenant A's host header can find and resume a bookmark belonging to tenant B if their HTTP endpoint paths collide, exposing tenant B's workflow output to tenant A and letting tenant A inject input into tenant B's workflow.

## Location
[src/modules/Elsa.Http/Middleware/HttpWorkflowsMiddleware.cs:152](https://github.com/elsa-workflows/elsa-core/blob/release/3.7.0/src/modules/Elsa.Http/Middleware/HttpWorkflowsMiddleware.cs#L152)

## Impact
Tenant A can trigger HTTP workflows that belong to tenant B.

## Reproduction steps
1. Tenant A registers an HTTP workflow at /webhooks/payment. Tenant B (operating in the same Elsa cluster) registers the same path. A request to tenant A's domain at /webhooks/payment can resolve to tenant B's bookmark; the workflow runs under tenant B's context but with input attacker-controlled by tenant A, leaking data back via the response body.

## Recommended fix
Scope bookmark lookups to the current tenant by default. Only allow TenantAgnostic resolution when the request is explicitly tenant-anonymous (e.g., before tenant resolution runs) and when the workflow definition opts in to cross-tenant invocation.

---
**Severity:** MEDIUM
**Status:** Open
**Category:** Broken Access Control
**Repository:** elsa-workflows/elsa-core
**Branch:** release/3.7.0
**Date created:** 2026-05-20