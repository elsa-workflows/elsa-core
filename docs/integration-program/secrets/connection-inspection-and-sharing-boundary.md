# Connection inspection and sharing boundary

This page covers the #8406 part of #8345. It lists the connection read and share paths, the host decisions that guard them, the wiring a host needs, the tests and the work that remains. It is a companion to the [workflow-use grant runtime validation](workflow-use-grant-runtime-validation.md) and the [credential lifecycle operations guide](credential-lifecycle-operations.md). It does not certify a production host identity policy, a provider adapter or the legacy Extensions routes.

## Inventory

### Core Connections (`main`)

| Path | Kind | Decision | What the caller gets |
|---|---|---|---|
| `IConnectionMetadataInspector.InspectAsync` | Read, host-only | `IConnectionMetadataInspectionAuthorizer` | `ConnectionInspectionMetadata`: connection ID, provider ID, provider account ID, status and revision |
| `IConnectionLifecycleService` connect, rotate, refresh and disconnect results | Result of a mutation | `IConnectionUseAuthorizer` `manage:*` | `ConnectionLifecycleMetadata`, which adds the generation ID, returned only to the caller of that authorized operation |
| `IConnectionLifecycleService.ResolveForUseAsync` and `IConnectionBackgroundUseService` | Credential use, host-only | `IConnectionUseAuthorizer` `use`, human or host-minted system identity | An access-only `ConnectionAccessCredential`. Its JSON converter is write-only and omits the value |
| `IWorkflowCredentialBindingManager` create and rebind | Select a connection | `IConnectionCredentialBindingManagementAuthorizer` | Binding revision. A binding selects a connection and does not grant use |
| `IWorkflowCredentialGrantManager.IssueAsync` | Share with one workflow instance | `IConnectionCredentialGrantManagementAuthorizer` (`Issue`) and `IConnectionCredentialShareAuthorizer` | Grant revision |
| `IWorkflowCredentialGrantManager.WithdrawAsync` | Revoke a share | `IConnectionCredentialGrantManagementAuthorizer` (`Withdraw`) and an expected-revision check | Grant revision |
| `IWorkflowCredentialResolver.ResolveAsync` | Use a share | `IConnectionCredentialBindingUseAuthorizer`, the stored grant, then background `use` | Access-only credential |
| `IConnectionLifecycleStore`, `IConnectionDueCandidateStore`, `IConnectionCredentialBindingStore`, `IConnectionCredentialUseGrantStore` | Persistence primitives | None | Keep out of HTTP and workflow input |

The consumers are the Connections unit tests, the persisted export fixture in `Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests` and the opt-in worker in `test/workers`. No app under `src/apps` references the Connections modules. There is no HTTP endpoint and no Studio module. The modules register only in the classic `Features/` system and have no CShells `ShellFeatures/`.

### Legacy Extensions `ConnectionDefinition`

The legacy module is not on `main`. This inventory was read from `origin/audit/8286-history-import-candidate` at `e384baee85f9f53fdf417942af8a288bb3f2966f` and nothing in it was changed.

| Path | Permission | Returns or does |
|---|---|---|
| `GET /connection-configuration` | `connections:read` | `PagedListResponse<ConnectionModel>`. `ConnectionDefinitionExtensions.ToModel` copies `ConnectionConfiguration` JSON unfiltered |
| `GET /connection-configuration/{id}` | `connections:read` | `ConnectionModel` with the same unfiltered JSON |
| `POST /connection-configuration`, `PUT /connection-configuration/{id}` | `connections:write` | Store the caller's JSON and echo `ConnectionModel` |
| `DELETE /connection-configuration/{id}` | `connections:delete` | Delete the row |
| `GET /connection-configuration/descriptors`, `GET /connection-configuration/input-descriptor/{ActivityType}` | `connections/descriptor:read` | Type descriptors, no values |
| `ConnectionMiddleware` (activity pipeline, inserted at position 3) | None | For an activity marked `[ConnectionActivity]`, loads the definition named by the workflow-authored `ConnectionName` and deserializes its JSON into `ConnectionProperties<T>.Properties` |
| `ConnectionOptionsProvider` (dropdown UI hint) | None beyond the descriptor API | Names of every definition of a connection type |
| `PropertyAttributeFilter` | Not applicable | Masks `[NoLog]` connection inputs in persisted activity state |

The legacy model has no explicit sharing. Any workflow that names a connection receives its configuration at run time. There is no use grant and no environment dimension. Tenant scope depends on the entity `TenantId` and the ambient EF tenant filter. No in-tree activity on the import branch uses `[ConnectionActivity]` or `ConnectionProperties<T>`. The workbench sample `Elsa.ServerAndStudio.Web` installs `UseConnections`, `UseConnectionPersistence` and `UseConnectionsApi`, and no Studio module calls `/connection-configuration`. Consumers of the published packages have not been measured.

These public identities stay as they are:

- **Packages:** `Elsa.Connections.Api`, `Elsa.Connections.Core`, `Elsa.Connections.Models`, `Elsa.Connections.Persistence`, `Elsa.Connections.Persistence.EFCore` and `Elsa.Connections.Persistence.EFCore.Sqlite`.
- **Storage:** the `Elsa.ConnectionDefinitions` table.
- **Routes and permissions:** the routes and permission names listed above.
- **Serialized model:** the `ConnectionModel` JSON shape (`id`, `name`, `description`, `connectionType`, `connectionConfiguration`).

Legacy and Core share the `Elsa.Connections.Contracts`, `.Features`, `.Models` and `.Services` namespaces. The types added for #8406 (`IConnectionMetadataInspectionAuthorizer`, `ConnectionMetadataInspectionRequest` and `DenyAllConnectionMetadataInspectionAuthorizer`) do not collide with any legacy type name.

## Decisions

Each decision is answered only by the contract named for it. Every default denies. Tenant comes from the ambient `ITenantAccessor` (the default and agnostic tenants are rejected). Environment comes from host configuration. Connection and binding identity come from the caller's named ID or from the persisted binding, never from workflow data.

| Decision | Contract | Scope in the request | Default and where it is registered | Does not follow from |
|---|---|---|---|---|
| Metadata inspection | `IConnectionMetadataInspectionAuthorizer` | Principal, tenant, `ConnectionInspectionOptions.EnvironmentId`, connection | Deny, `ConnectionsFeature` | Use or management permission, bindings, grants, share permission |
| Share with a workflow instance | `IConnectionCredentialShareAuthorizer` | Principal, tenant, `WorkflowCredentialBindingsFeature.EnvironmentId`, workflow instance, logical binding, connection, binding revision | Deny, `WorkflowCredentialUseGrantsFeature` | Grant management, binding management, inspection, connection management |
| Grant management | `IConnectionCredentialGrantManagementAuthorizer` | The share scope plus `Issue` or `Withdraw` | Deny, `WorkflowCredentialUseGrantsFeature` | Connection management |
| Binding management | `IConnectionCredentialBindingManagementAuthorizer` | Principal, tenant, environment, logical binding, connection, expected revision | Deny, `WorkflowCredentialBindingsFeature` | Connection management |
| Binding use | `IConnectionCredentialBindingUseAuthorizer`, then the stored grant | Tenant, environment, binding, connection, binding revision, workflow instance | Deny. The grants feature replaces only this default, and a host's own policy still applies | A binding alone |
| Human use and management | `IConnectionUseAuthorizer`, purposes `use` and `manage:*` | Principal, kind, tenant, environment, connection, purpose | Deny, `ConnectionsFeature` | Inspection or share permission |

Issuing a share needs both grant management and the share decision. Withdrawing needs only grant management, so an administrator can revoke a share after the right to create shares is gone. At issue time the grant store also checks, in one serializable transaction, that the binding revision is current, that the connection is `Active` and that no grant already exists for that instance and binding.

## Host wiring

Configure the features with the classic `IModule` API, as the tests do, then register the host policies. The defaults use `TryAdd`, so a host registration made before or after the features is resolved in their place.

```csharp
services.AddElsa(elsa =>
{
    elsa.Configure<ConnectionsFeature>();
    elsa.Configure<EFCoreConnectionsPersistenceFeature>(feature => feature.UsePostgreSql(connectionString));
    elsa.Configure<WorkflowCredentialBindingsFeature>(feature => feature.EnvironmentId = "production");
    elsa.Configure<WorkflowCredentialUseGrantsFeature>(); // opt-in; without it no share can be issued
});
services.Configure<ConnectionInspectionOptions>(options => options.EnvironmentId = "production");
services.AddScoped<IConnectionUseAuthorizer, HostConnectionUseAuthorizer>();
services.AddScoped<IConnectionMetadataInspectionAuthorizer, HostConnectionInspectionAuthorizer>();
services.AddScoped<IConnectionCredentialBindingManagementAuthorizer, HostBindingManagementAuthorizer>();
services.AddScoped<IConnectionCredentialGrantManagementAuthorizer, HostGrantManagementAuthorizer>();
services.AddScoped<IConnectionCredentialShareAuthorizer, HostShareAuthorizer>();
```

`ConnectionInspectionOptions.EnvironmentId` and `WorkflowCredentialBindingsFeature.EnvironmentId` are separate settings. Point both at the same integration environment. Each host policy must check the authenticated principal against the full request scope. Deny any `IConnectionUseAuthorizer` purpose the host does not recognize. Core no longer sends `inspect:metadata`, and a purpose added later must not inherit an existing rule.

## Revocation and admission

A share is revoked by withdrawing its grant, which is a conditional update on the grant revision. Once withdrawn, that workflow instance and binding pair can never be issued a grant again. Every later resolution reloads the grant, the connection status and the binding revision, and fails closed after a withdrawal, rebind or disconnect.

A call that has passed the grant check is admitted. A withdrawal after that point does not take back the credential already handed out or stop the provider call that follows, and nothing re-authorizes the call after the fact.

Removing a principal's share permission does not withdraw existing grants; withdraw them explicitly. A grant also cannot be issued for a connection that is already disconnected.

## Tests

On 2026-09-27, `dotnet test test/unit/Elsa.Connections.UnitTests/Elsa.Connections.UnitTests.csproj -f net10.0` passed 90 tests with none skipped. The filtered persisted export test in the PostgreSQL integration project passed 1 of 1.

| Test | Proves |
|---|---|
| `MetadataInspectionIsDecidedOnlyByItsOwnHostPolicy` (both cases) | Inspection is denied without an inspection policy even when the use and management policy allows every purpose. The store is not read and the use policy is never asked. Registering an inspection policy is what allows it. The denial case fails against the 2026-09-25 design |
| `MetadataInspectionRequiresItsOwnHostPermissionAndReturnsOnlySafeFields` | Manage-only, use-only and manage-plus-use principals are denied, and an inspect principal is allowed. The request carries the trusted tenant, the configured environment and the connection. Unauthenticated, cross-tenant, default or agnostic tenant, other-environment, blank-environment and overlong-ID requests are denied |
| `InspectionOfConnectedAndDisconnectedConnectionsReturnsNoCredentialMaterial` | For a real OAuth generation, the active and disconnected responses (the latter reporting `Disconnected`) contain no access token, refresh token, Secrets value, secret name or generation ID. The DTO property set is pinned to the five whitelisted fields |
| `MetadataInspectorWithoutOptionalPersistenceFailsClosed` | An allowing policy with no lifecycle store still returns nothing |
| `WorkflowUseGrantDoesNotSatisfyInspectionAndInspectionDoesNotSatisfySharing` | An actor with binding, grant and share permission, and an active grant, is still denied inspection. An inspect permission cannot issue a share |
| `SharingRequiresItsOwnHostDecisionAndCanBeWithdrawnAfterThatDecisionIsRemoved`, `GrantManagementPermissionCannotIssueWithoutAHostSharePolicy`, `GrantFeatureWithoutHostPolicyOrPersistenceFailsClosed` | Sharing is allowed or denied by its own decision, defaults to deny, and stays revocable |
| `SharingCannotReachABindingInAnotherTenantOrEnvironment` | Neither tenant-b nor a host configured for another environment can share tenant-a's binding. Neither policy is asked about the foreign scope and no grant is written. The owning scope still can |
| `SharingADisconnectedConnectionWritesNoGrant` | Even with every policy allowing, issuance for a disconnected connection fails and nothing resolves |
| `WithdrawnShareLetsTheAdmittedCallFinishAndBlocksTheNextUse` | A withdrawal during an admitted call leaves that call's credential intact. The next resolution fails and makes no second credential call |
| `DurableGrantRequiresSeparatePolicy_AndWithdrawalStopsSubsequentUse`, `GrantIsBoundToTenantEnvironmentWorkflowAndBindingRevision`, `GrantDoesNotBypassHostUsePolicyRegardlessOfRegistrationOrder`, persisted-resume theories | Withdrawal, rebind, disconnect and tenant or environment mismatch after real persisted resume. See the runtime validation page |

## Migration limits

- No schema or persisted-format change. Grant, binding and connection rows are untouched, so rollback is code-only.
- A host that answered `inspect:metadata` inside `IConnectionUseAuthorizer` is denied inspection until it registers `IConnectionMetadataInspectionAuthorizer`. `Elsa.Connections` is not packable, so no published package changes. Code that constructs `DefaultConnectionMetadataInspector` directly must pass the new authorizer.
- Legacy `ConnectionDefinition` rows are not Core lifecycle connections, and nothing maps, imports or filters them. The legacy middleware ignores the inspection, share and use decisions, and the legacy GET and list routes still return unfiltered configuration JSON.
- Any adaptation of the legacy routes or middleware stays with #8286 and #8275. It needs an explicit compatibility decision for the published route and DTO. It also needs an inspection of real configuration contents before any filtering. Do not copy plaintext into a public fixture.

## Not provided and remaining work

- There is no HTTP route for inspection, sharing or grants, and no public plaintext credential endpoint. Credential resolution stays in process.
- There is no Studio UI for inspection, sharing or grant withdrawal, and no list or search inspection.
- Sharing covers one exact workflow instance only. Person or team delegation is not implemented, and whether it belongs in this program is still with the program owner.
- There are no CShells `ShellFeatures` for the Connections modules.
- Provider-specific work remains per provider once one is selected: consent and connect UI, provider-side consent delegation, and revoke or uninstall semantics.
- No production host identity policy is certified.
