# Separate workflow connection sharing from grant management and use

## Context

An Elsa 3 workflow can use a named connection through a durable grant tied to its instance, logical binding, connection and binding revision. The grant manager already requires an authenticated actor and a host grant-management policy, and each execution rechecks the stored grant. Grant management alone did not express whether that actor was allowed to **share this particular connection with this workflow**. General connection management and metadata inspection are separate decisions.

## Decision

Issuing a workflow-use grant requires two independent host decisions: `IConnectionCredentialGrantManagementAuthorizer` for the grant operation and `IConnectionCredentialShareAuthorizer` for the exact tenant, configured environment, workflow instance, binding, connection and binding revision. The sharing policy defaults to deny when the host has not registered an implementation. The grant manager derives this scope from the trusted tenant accessor, configured environment and persisted binding; workflow-authored input cannot provide it. A denied decision writes no grant.

Withdrawal requires grant-management authorization and the expected grant revision, but does not require continuing share permission. This lets an administrator revoke an existing share after the right to create new shares has been removed. The existing durable grant stays the runtime entitlement: every subsequent credential resolution reloads its active state, binding revision and connection status, alongside the host's separate binding-use policy. A withdrawal cannot cancel a provider call that was already admitted.

This is **workflow-instance sharing**, not general person/team delegation. No new credential store, plaintext API, package identity, or persisted grant schema is introduced. Hosts that opt into workflow grants must register both host policies to issue them; the default remains deny. Existing non-sharing connection use and metadata inspection do not gain access through a grant.

## Consequences

The two decisions can be audited and tested independently. A host that previously provided only grant-management authorization will now fail closed at issuance until it explicitly supplies sharing policy. Revocation uses the existing grant's durable conditional update, so it is visible to later workers and persisted workflow resumes. Broader user/team sharing, provider-side consent delegation and user-facing Studio controls require separate design and are not inferred from this instance-scoped mechanism.
