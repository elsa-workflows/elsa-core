# Elsa Workflow Runtime

The workflow runtime executes activities and moves their results into destinations that workflows can consume.

## Language

**Activity Output**:
The native value produced by an activity. Its meaning and type belong to the activity's contract and are not changed by a consumer's binding choices. Activity-output registers, journals, APIs, and diagnostics expose this native value.
_Avoid_: Converted output, bound output

**Output Binding**:
The association that delivers an Activity Output to a variable or workflow output. It may apply at most one explicitly configured Output Converter before delivery. Its optional persisted converter configuration contains only a Converter ID and JSON Converter Settings. Without converter configuration, it follows the existing assignment path unchanged.
_Avoid_: Activity output, output definition

**Output Converter**:
An optional deterministic, side-effect-free transformation selected strictly at an Output Binding. It changes the value delivered by that binding without changing the underlying Activity Output. It does not convert activity inputs or general expression results. Environmental choices such as locale are explicit Converter Settings.
_Avoid_: Activity converter, implicit coercion

**Converter ID**:
The stable semantic identifier by which an Output Binding explicitly selects a registered Output Converter. Matching is ordinal and case-sensitive, while registrations that differ only by case are rejected. Breaking changes to conversion behavior, settings, or result semantics use a new Converter ID.
_Avoid_: Converter type name, converter class

**Converter Settings**:
Optional workflow-specific parameters that refine how the selected Output Converter transforms one Output Binding. They are immutable during conversion.
_Avoid_: Global converter options, converter service configuration

**Converter Descriptor**:
The server-owned, API-discoverable identity and compatibility declaration of an Output Converter, including its supported source type, declared result type, localizable display metadata, and optional JSON Schema for Converter Settings. Source compatibility follows base-class and interface assignability; the result must be assignable to the Destination Type. Display metadata is not persisted with the workflow.
_Avoid_: Converter instance, activity descriptor

**Conversion Context**:
The narrow, immutable input supplied to an Output Converter: the native value, declared source and destination types, and Converter Settings. It does not expose mutable workflow execution state or a service locator.
_Avoid_: Activity execution context, workflow context

**Destination Type**:
The resolvable declared type of the variable or workflow output receiving a Bound Value. `object` is a valid Destination Type; an unknown or untyped destination is not.
_Avoid_: Runtime value type, inferred target

**Output Conversion Error**:
The dedicated activity fault raised when converter resolution, settings validation, compatibility checking, invocation, or result validation fails. It carries structured converter, activity, output, destination, and failure-stage metadata without exposing native values or raw settings by default.
_Avoid_: Assignment error, converter log message

**Bound Value**:
The value delivered only to the destination of an Output Binding after any configured Output Converter has run. It may be null only when the destination permits null.
_Avoid_: Activity output

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

## User Tasks

**User Task Definition**:
The design-time configuration of human work in a workflow. It is evaluated and materialized when the activity executes.
_Avoid_: User Task Instance, standalone task

**User Task Instance**:
A durable runtime work item created by a committed User Task bookmark and completed by one accountable participant or a configured terminal outcome.
_Avoid_: User Task Definition, generic RunTask

**Participant Reference**:
An opaque, tenant-scoped `{ provider, type, id }` reference to a host-owned user or group. It never implies an Elsa Identity record.
_Avoid_: Elsa User ID, username

**Candidate**:
A participant eligible to claim an Available User Task. Candidacy grants safe-summary visibility, not protected task content.
_Avoid_: Assignee, requester

**Assignee**:
The single participant accountable for an Assigned User Task and permitted to access its protected response surface.
_Avoid_: Candidate, manager

**Requester**:
An optional informational participant shown for context and search. Requester status grants no task access.
_Avoid_: Assignee, task owner

**Task Action**:
A stable literal outcome key and its materialized display label. `Timeout` and `Cancelled` are reserved actions.
_Avoid_: Button text, arbitrary workflow command

**Task Health**:
An operational warning or blocking resolution problem independent of User Task lifecycle status.
_Avoid_: Task status, workflow incident

**Guest Invitation**:
A bounded, one-time candidacy for an external participant that becomes a task-scoped guest session only after configured verification.
_Avoid_: Elsa user invitation, bearer account
