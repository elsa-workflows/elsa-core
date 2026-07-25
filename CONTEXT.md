# External Authentication

This context describes how Elsa presents and uses external authentication choices while preserving Elsa's own user and authorization model.

## Language

**Identity Provider**:
An external authority that authenticates a person and asserts their identity.
_Avoid_: Login provider, SSO definition

**Identity Provider Connection**:
Elsa's registration of and trust relationship with an Identity Provider. A connection may be supplied by deployment configuration or managed by an administrator.
_Avoid_: Identity Provider, provider definition, social login

**Login Method**:
An authentication choice presented to an Elsa client, such as local Elsa credentials or an enabled Identity Provider Connection.
_Avoid_: Identity Provider Connection, social login button

**Authentication Client**:
An Elsa client application registered to initiate brokered sign-in and receive its one-time completion code at an approved callback.
_Avoid_: Elsa Application, Identity Provider Connection

**Upstream Client Registration**:
The client identifier and credentials with which an Identity Provider Connection authenticates Elsa to the external Identity Provider.
_Avoid_: Authentication Client, Elsa Application

**Connection Registry**:
The unified collection of configuration-owned and administrator-owned Identity Provider Connections available to Elsa.
_Avoid_: Provider list, connection store

**Connection Key**:
A stable, immutable logical identifier for an Identity Provider Connection within the currently connected Elsa server environment. Together with target tenant, issuer namespace, and subject, it participates in the External Identity Key.
_Avoid_: Database row ID, scheme name, display name

**Studio Override**:
An administrator-managed connection document that explicitly and completely shadows a configuration-owned connection with the same immutable Connection Key. Overrides replace the whole effective document; fields are never merged between sources.
_Avoid_: Partial override, configuration patch

**Protocol Adapter**:
A trusted Elsa module that implements external authentication for a particular protocol or provider family and translates its result into an External Identity.
_Avoid_: Identity Provider Connection, provider definition

**External Identity**:
A protocol-neutral identity asserted by an Identity Provider and identified within that provider's namespace.
_Avoid_: Elsa User, external user

**External Identity Key**:
The immutable combination of target Elsa tenant, Connection Key, validated issuer namespace, and stable subject used to distinguish an External Identity. Host-wide connection deployment does not collapse Elsa User tenancy.
_Avoid_: Email address, user name

**External Identity Link**:
The association between an External Identity and the Elsa User that receives Elsa-specific roles, permissions, and tenant access.
_Avoid_: External user, federated user

**Local Credential**:
Optional Elsa-managed authentication material, such as a password, associated with an Elsa User independently of External Identity Links.
_Avoid_: Elsa User, External Identity

**Unlinked Identity Policy**:
The selected rule that decides what Elsa may do when an authenticated External Identity has no External Identity Link.
_Avoid_: Provisioning mode, authorization policy

**Elsa Permission**:
A string-named capability required by Elsa functionality and carried by an authenticated principal.
_Avoid_: External claim, role

**Permission Grant Source**:
A deferred extension concept for contributing Elsa Permissions. It is not a v1 External Authentication Studio configuration surface.
_Avoid_: Role mapping, raw claim pass-through

**External User Matcher**:
A trusted deployed extension selected by the matcher-based Unlinked Identity Policy to propose an existing Elsa User from bounded, ephemeral external claims. Ambiguous results and matcher errors reject authentication.
_Avoid_: Role matcher, permission mapper, automatic email linking

**Permission Descriptor**:
Optional module-provided metadata that describes an Elsa Permission without determining validity. External Authentication v1 does not use it for claim-permission mapping.
_Avoid_: Permission catalog, permission registry

**External Authentication Session**:
The bounded Elsa sign-in session established from one successful external authentication and its resulting claim snapshot.
_Avoid_: Identity-provider session, Elsa access token

**Upstream Logout**:
An optional logout operation that also asks the Identity Provider to end its session, when supported by the connection's Protocol Adapter.
_Avoid_: Elsa logout, session revocation

**Break-glass Authentication**:
A deployment-controlled recovery method kept independent of ordinary external sign-in so administrators can repair authentication after lockout.
_Avoid_: Backup provider, normal login method

**Elsa User**:
An account governed by Elsa's authorization model. An Elsa User may be associated with multiple External Identities.
_Avoid_: External Identity, identity-provider user

**Adapter Descriptor**:
The protocol-neutral description of a Protocol Adapter's connection settings, validation, presentation, and capabilities.
_Avoid_: Connection settings, custom form

**Secret Binding**:
A non-secret reference that tells Elsa how to resolve a sensitive connection value without storing or disclosing that value as connection data.
_Avoid_: Client secret, secret value

**Managed Secret**:
A Secret Binding whose lifecycle is managed through an Elsa-integrated secret store. Studio may replace or remove it but never reveal it.
_Avoid_: Inline secret, connection field

**External Secret**:
A read-only Secret Binding resolved from deployment configuration or another externally operated resolver. Studio may show its configured/resolvable state but does not own its value or lifecycle.
_Avoid_: Managed Secret, plaintext setting

**Preferred Login Method**:
The enabled Login Method emphasized and ordered first by the chooser. Preference never causes an automatic redirect; the chooser remains visible.
_Avoid_: Automatic login, forced provider

**Connection Health**:
The observed operational condition of an Identity Provider Connection, independent of whether administrators intend it to be enabled.
_Avoid_: Enabled state, validity

**Connection Validity**:
Whether an Identity Provider Connection has structurally acceptable settings and resolvable required secrets.
_Avoid_: Connection Health, enabled state

**Provider Trust Setting**:
A connection-controlled rule for locating and validating the Identity Provider and its assertions.
_Avoid_: Broker security invariant

**Broker Security Invariant**:
An Elsa-owned protection for the broker and its clients that connection administrators cannot weaken through Studio.
_Avoid_: Provider Trust Setting
