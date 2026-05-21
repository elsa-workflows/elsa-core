# Data Model: Weaver AI Copilot Platform

## AiConversation

Represents a Weaver chat thread visible to a user.

**Fields**: `Id`, `TenantId`, `UserId`, `Title`, `Status`, `CreatedAt`, `UpdatedAt`, `ProviderSessionId`, `RetentionMode`, `RetentionExpiresAt`.

**Relationships**: Has many `AiMessage`, `AiContextAttachment`, `AiToolInvocation`, and `AiAuditEvent` records.

**Rules**:

- Conversation access is tenant- and actor-scoped.
- Provider session IDs are internal server metadata and never exposed as provider objects.
- Retention policy controls conversation history visibility, durability, and cleanup.

## AiMessage

Represents user, assistant, system, or tool-visible content in a conversation.

**Fields**: `Id`, `ConversationId`, `Role`, `Content`, `CreatedAt`, `StreamSequence`, `Metadata`.

**Rules**:

- Stored content must already be redacted.
- Tool result details may be summarized for model context and preserved separately for display when safe.

## AiContextAttachment

Represents a client-supplied reference to server-resolved context.

**Fields**: `Id`, `ConversationId`, `Kind`, `ReferenceId`, `TenantId`, `Scope`, `TimeRange`, `ActivityId`, `Metadata`.

**Supported Kinds**: `WorkflowDefinition`, `WorkflowInstance`, `ActivitySelection`, `Tenant`, `DiagnosticsScope`, `TimeRange`.

**Rules**:

- Studio sends references only.
- Server resolves attachments per request and validates authorization before use.
- Context resolution applies size limits and redaction.

## AiToolDefinition

Represents a registered Weaver tool.

**Fields**: `Name`, `DisplayName`, `Description`, `Schema`, `Mutability`, `DangerLevel`, `Permissions`, `TenantBehavior`, `AuditBehavior`, `AgentScopes`, `Provider`, `EnabledByDefault`, `IsEnabled`.

**Rules**:

- Tool names are stable and namespaced, such as `workflow.getDefinition`.
- Mutating tools are either proposal-only or administrative; administrative tools are out of MVP scope.
- Tool metadata is validated during registration.
- Read-only module tools may be enabled by default; proposal, administrative, and MCP-backed tools require explicit administrator enablement.

## AiToolInvocation

Represents a single attempted tool execution.

**Fields**: `Id`, `ConversationId`, `ToolName`, `Arguments`, `AuthorizationResult`, `StartedAt`, `CompletedAt`, `Status`, `ResultSummary`, `Error`, `TraceId`, `TenantId`, `ActorId`.

**Rules**:

- Authorization is checked before execution.
- Arguments and results are redacted before audit and stream output.
- Failed and denied invocations are audited.

## AiProposal

Represents a reviewable AI-generated workflow creation or update.

**Fields**: `Id`, `TenantId`, `ConversationId`, `Kind`, `Status`, `BaselineWorkflowDefinitionId`, `BaselineVersionId`, `WorkflowPayload`, `Rationale`, `Warnings`, `ValidationDiagnostics`, `GraphDiff`, `CreatedBy`, `CreatedAt`, `ReviewedBy`, `ReviewedAt`, `AppliedBy`, `AppliedAt`.

**States**: `Draft`, `Validated`, `Blocked`, `Approved`, `Rejected`, `Applied`, `Expired`.

**Rules**:

- Proposals are the only AI path to workflow writes.
- Apply requires approval, authorization, validation pass, and baseline match.
- The same authorized user may create, approve, reject, and apply a proposal in MVP.
- Proposals are durable governance artifacts.
- Proposal payloads are structured workflow definitions or patches, not free-form text.

## AiAuditEvent

Represents durable governance evidence.

**Fields**: `Id`, `TenantId`, `ActorId`, `ConversationId`, `ProposalId`, `ToolInvocationId`, `Type`, `Timestamp`, `TraceId`, `Summary`, `Data`.

**Rules**:

- Audit data is redacted.
- Prompt, context resolution, tool call, denial, proposal, approval, rejection, and apply events are recorded.

## AiAgentDefinition

Represents a named Weaver agent contributed by Core or a module.

**Fields**: `Name`, `DisplayName`, `Description`, `Instructions`, `AllowedTools`, `AllowedContextProviders`, `AllowedMcpServers`, `Permissions`, `Enabled`.

**Rules**:

- Agents operate with least-privilege tool and context scopes.
- Agent instructions cannot grant permissions or bypass server enforcement.

## AiMcpServerRegistration

Represents a governed external tool server registration.

**Fields**: `Name`, `Transport`, `Endpoint`, `ToolAllowlist`, `AgentScopes`, `Permissions`, `TenantBehavior`, `AuditBehavior`, `Enabled`.

**Rules**:

- Only allowlisted MCP tools are exposed.
- Local and remote servers are configured server-side.
- Per-agent scoping is mandatory.
- MCP-backed tools require explicit administrator enablement before use.
