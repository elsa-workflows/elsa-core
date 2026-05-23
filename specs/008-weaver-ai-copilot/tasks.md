# Tasks: Weaver AI Copilot Platform

**Input**: Design documents from `/specs/008-weaver-ai-copilot/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification requires validation coverage for streaming, tenant-aware authorization, context redaction, durable proposal/audit persistence, proposal safety, explicit tool enablement, reconnect behavior, scoped trend analysis, tool governance, and provider isolation.

**Organization**: Tasks are grouped by user story so each increment can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks after prerequisites are satisfied.
- **[Story]**: User story label from [spec.md](./spec.md).
- Every task includes the primary file path to edit or create.

## Phase 1: Setup

**Purpose**: Create module and test project skeletons.

- [X] T001 [P] Create AI abstractions project in `src/modules/Elsa.AI.Abstractions/Elsa.AI.Abstractions.csproj`.
- [X] T002 [P] Create AI host project in `src/modules/Elsa.AI.Host/Elsa.AI.Host.csproj`.
- [X] T003 [P] Create Copilot adapter project in `src/modules/Elsa.AI.Copilot/Elsa.AI.Copilot.csproj`.
- [X] T004 [P] Create EF Core persistence project in `src/modules/Elsa.AI.Persistence.EFCore/Elsa.AI.Persistence.EFCore.csproj`.
- [X] T005 [P] Create abstractions unit test project in `test/unit/Elsa.AI.Abstractions.UnitTests/Elsa.AI.Abstractions.UnitTests.csproj`.
- [X] T006 [P] Create host unit test project in `test/unit/Elsa.AI.Host.UnitTests/Elsa.AI.Host.UnitTests.csproj`.
- [X] T007 [P] Create Copilot unit test project in `test/unit/Elsa.AI.Copilot.UnitTests/Elsa.AI.Copilot.UnitTests.csproj`.
- [X] T008 [P] Create EF Core persistence unit test project in `test/unit/Elsa.AI.Persistence.EFCore.UnitTests/Elsa.AI.Persistence.EFCore.UnitTests.csproj`.
- [X] T009 [P] Create AI integration test project in `test/integration/Elsa.AI.IntegrationTests/Elsa.AI.IntegrationTests.csproj`.
- [X] T010 Add AI projects and tests to `Elsa.sln`.
- [X] T011 [P] Add abstractions global usings in `src/modules/Elsa.AI.Abstractions/Usings.cs`.
- [X] T012 [P] Add host global usings in `src/modules/Elsa.AI.Host/Usings.cs`.
- [X] T013 [P] Add Copilot global usings in `src/modules/Elsa.AI.Copilot/Usings.cs`.
- [X] T014 [P] Add EF Core persistence global usings in `src/modules/Elsa.AI.Persistence.EFCore/Usings.cs`.
- [X] T015 [P] Add unit test global usings in `test/unit/Elsa.AI.Host.UnitTests/Usings.cs`.
- [X] T016 [P] Add integration test global usings in `test/integration/Elsa.AI.IntegrationTests/Usings.cs`.

---

## Phase 2: Foundational

**Purpose**: Add shared contracts, models, options, registration, durable governance storage, and enablement surfaces that block user stories.

**Critical**: No user story work should begin until these shared contracts and registration surfaces exist.

- [X] T017 [P] Add conversation, session, message, and stream event models in `src/modules/Elsa.AI.Abstractions/Models/AIConversation.cs`.
- [X] T018 [P] Add context attachment and resolved context models in `src/modules/Elsa.AI.Abstractions/Models/AIContextAttachment.cs`.
- [X] T019 [P] Add tool definition, invocation, result, and enablement models in `src/modules/Elsa.AI.Abstractions/Models/AIToolDefinition.cs`.
- [X] T020 [P] Add proposal models with durable lifecycle fields in `src/modules/Elsa.AI.Abstractions/Models/AIProposal.cs`.
- [X] T021 [P] Add durable audit event models in `src/modules/Elsa.AI.Abstractions/Models/AIAuditEvent.cs`.
- [X] T022 [P] Add provider and orchestrator contracts in `src/modules/Elsa.AI.Abstractions/Contracts/IAIProvider.cs`.
- [X] T023 [P] Add tool registry and tool contracts in `src/modules/Elsa.AI.Abstractions/Contracts/IAITool.cs`.
- [X] T024 [P] Add context provider contract in `src/modules/Elsa.AI.Abstractions/Contracts/IAIContextProvider.cs`.
- [X] T025 [P] Add conversation store, proposal store, and audit sink contracts in `src/modules/Elsa.AI.Abstractions/Contracts/IAIProposalStore.cs`.
- [X] T026 [P] Add AI host and provider options for retention, reconnect grace, result limits, and BYOK provider references in `src/modules/Elsa.AI.Host/Options/AIHostOptions.cs`.
- [X] T027 [P] Add AI permissions in `src/modules/Elsa.AI.Host/Permissions/AIPermissions.cs`.
- [X] T028 Add service registration extension in `src/modules/Elsa.AI.Host/Extensions/ServiceCollectionExtensions.cs`.
- [X] T029 Add module feature registration in `src/modules/Elsa.AI.Host/Features/AIFeature.cs`.
- [X] T030 Add shell feature registration in `src/modules/Elsa.AI.Host/ShellFeatures/AIFeature.cs`.
- [X] T031 [P] Implement configurable in-memory conversation store in `src/modules/Elsa.AI.Host/Services/InMemoryAIConversationStore.cs`.
- [X] T032 [P] Implement tool enablement service in `src/modules/Elsa.AI.Host/Services/AIToolEnablementService.cs`.
- [X] T033 [P] Implement audit sink fan-out in `src/modules/Elsa.AI.Host/Services/AIAuditSink.cs`.
- [X] T034 [P] Add EF Core AI DbContext in `src/modules/Elsa.AI.Persistence.EFCore/DbContext.cs`.
- [X] T035 [P] Add EF Core proposal entity mapping in `src/modules/Elsa.AI.Persistence.EFCore/Entities/AIProposalRecord.cs`.
- [X] T036 [P] Add EF Core audit entity mapping in `src/modules/Elsa.AI.Persistence.EFCore/Entities/AIAuditRecord.cs`.
- [X] T037 Implement durable EF Core proposal store in `src/modules/Elsa.AI.Persistence.EFCore/Stores/EFCoreAIProposalStore.cs`.
- [X] T038 Implement durable EF Core audit sink in `src/modules/Elsa.AI.Persistence.EFCore/Stores/EFCoreAIAuditSink.cs`.
- [X] T039 Add EF Core persistence feature and shell feature registration in `src/modules/Elsa.AI.Persistence.EFCore/ShellFeatures/AIPersistenceFeature.cs`.
- [X] T040 [P] Add registration and naming tests in `test/unit/Elsa.AI.Host.UnitTests/AIRegistrationTests.cs`.
- [X] T041 [P] Add tool metadata and enablement validation tests in `test/unit/Elsa.AI.Abstractions.UnitTests/AIToolDefinitionTests.cs`.
- [X] T042 [P] Add durable proposal persistence tests in `test/unit/Elsa.AI.Persistence.EFCore.UnitTests/EFCoreAIProposalStoreTests.cs`.
- [X] T043 [P] Add durable audit persistence tests in `test/unit/Elsa.AI.Persistence.EFCore.UnitTests/EFCoreAIAuditSinkTests.cs`.
- [X] T044 [P] Add provider isolation boundary tests in `test/unit/Elsa.AI.Copilot.UnitTests/CopilotBoundaryTests.cs`.

**Checkpoint**: Shared contracts, options, permissions, features, retention controls, tool enablement, durable proposal storage, and durable audit storage are ready for story work.

---

## Phase 3: User Story 1 - Chat with workflow-aware Weaver (Priority: P1) MVP

**Goal**: Authorized users can chat with Weaver, attach references, receive server-owned streaming assistant and tool events, and recover durable outputs after a short disconnect.

**Independent Test**: Start a chat with a workflow definition reference, verify server-side context resolution, assistant deltas, tool lifecycle events, reconnect within grace window, and no direct Studio/provider calls.

### Tests for User Story 1

- [X] T045 [P] [US1] Add context authorization tests in `test/unit/Elsa.AI.Host.UnitTests/Context/AIContextResolverTests.cs`.
- [X] T046 [P] [US1] Add stream event mapping tests in `test/unit/Elsa.AI.Host.UnitTests/Streaming/AIStreamEventMapperTests.cs`.
- [X] T047 [P] [US1] Add reconnect grace tests in `test/unit/Elsa.AI.Host.UnitTests/Streaming/AIReconnectGraceTests.cs`.
- [X] T048 [P] [US1] Add chat endpoint streaming integration tests in `test/integration/Elsa.AI.IntegrationTests/AIChatEndpointTests.cs`.
- [X] T049 [P] [US1] Add capabilities endpoint integration tests in `test/integration/Elsa.AI.IntegrationTests/AICapabilitiesEndpointTests.cs`.
- [X] T050 [P] [US1] Add tools endpoint integration tests in `test/integration/Elsa.AI.IntegrationTests/AIToolsEndpointTests.cs`.
- [X] T051 [P] [US1] Add disconnected chat recovery integration tests in `test/integration/Elsa.AI.IntegrationTests/AIChatReconnectTests.cs`.

### Implementation for User Story 1

- [X] T052 [P] [US1] Implement workflow definition context provider in `src/modules/Elsa.AI.Host/Context/WorkflowDefinitionContextProvider.cs`.
- [X] T053 [P] [US1] Implement workflow instance context provider in `src/modules/Elsa.AI.Host/Context/WorkflowInstanceContextProvider.cs`.
- [X] T054 [US1] Implement context resolver with authorization and redaction in `src/modules/Elsa.AI.Host/Context/AIContextResolver.cs`.
- [X] T055 [US1] Implement AI tool registry with enablement filtering in `src/modules/Elsa.AI.Host/Services/AIToolRegistry.cs`.
- [X] T056 [US1] Implement orchestrator turn flow in `src/modules/Elsa.AI.Host/Services/AIOrchestrator.cs`.
- [X] T057 [US1] Implement reconnect grace tracking in `src/modules/Elsa.AI.Host/Streaming/AIStreamSessionManager.cs`.
- [X] T058 [US1] Implement stream event mapper in `src/modules/Elsa.AI.Host/Streaming/AIStreamEventMapper.cs`.
- [X] T059 [US1] Implement chat endpoint in `src/modules/Elsa.AI.Host/Endpoints/AI/Chat/Endpoint.cs`.
- [X] T060 [US1] Implement tools endpoint in `src/modules/Elsa.AI.Host/Endpoints/AI/Tools/Endpoint.cs`.
- [X] T061 [US1] Implement capabilities endpoint in `src/modules/Elsa.AI.Host/Endpoints/AI/Capabilities/Endpoint.cs`.
- [X] T062 [US1] Implement Copilot provider event adapter in `src/modules/Elsa.AI.Copilot/Adapters/CopilotProvider.cs`.
- [X] T063 [US1] Implement Copilot feature and shell feature registration in `src/modules/Elsa.AI.Copilot/ShellFeatures/CopilotAIFeature.cs`.
- [X] T064 [US1] Draft paired Studio Razor chat panel implementation, after reviewing the `elsa-extensions` `origin/feat/ai` UI prototype at commit `93f0e09d71e57f5daff1e2d593f0a51faaa80417`, in `../elsa-studio/src/modules/Elsa.Studio.AI/UI/Components/WeaverChatPanel.razor`.
- [X] T065 [US1] Run chat MVP tests in `test/integration/Elsa.AI.IntegrationTests/Elsa.AI.IntegrationTests.csproj`.

**Checkpoint**: Weaver chat, context references, stream events, reconnect recovery, capabilities, and tool listing work as the MVP.

---

## Phase 4: User Story 2 - Generate workflows through reviewable proposals (Priority: P2)

**Goal**: Users can generate durable workflow proposals and apply them only after explicit validation, approval, authorization, and durable audit.

**Independent Test**: Ask Weaver to create a workflow, inspect the durable proposal, verify blocked direct mutation, approve and apply as the same authorized user, restart storage, and verify audit/proposal records survive.

### Tests for User Story 2

- [ ] T066 [P] [US2] Add proposal lifecycle tests in `test/unit/Elsa.AI.Host.UnitTests/Proposals/AIProposalLifecycleTests.cs`.
- [ ] T067 [P] [US2] Add same-authorized-user approval tests in `test/unit/Elsa.AI.Host.UnitTests/Proposals/AIProposalApprovalTests.cs`.
- [ ] T068 [P] [US2] Add workflow draft validation tests in `test/unit/Elsa.AI.Host.UnitTests/Tools/WorkflowValidateDraftToolTests.cs`.
- [ ] T069 [P] [US2] Add proposal apply endpoint tests in `test/integration/Elsa.AI.IntegrationTests/AIProposalApplyEndpointTests.cs`.
- [ ] T070 [P] [US2] Add durable proposal restart integration tests in `test/integration/Elsa.AI.IntegrationTests/AIProposalPersistenceTests.cs`.
- [ ] T071 [P] [US2] Add durable proposal audit integration tests in `test/integration/Elsa.AI.IntegrationTests/AIProposalAuditTests.cs`.

### Implementation for User Story 2

- [ ] T072 [P] [US2] Implement proposal lifecycle service in `src/modules/Elsa.AI.Host/Proposals/AIProposalService.cs`.
- [ ] T073 [P] [US2] Implement workflow draft validator adapter in `src/modules/Elsa.AI.Host/Proposals/WorkflowDraftValidator.cs`.
- [ ] T074 [US2] Implement `workflow.proposeCreate` tool in `src/modules/Elsa.AI.Host/Tools/Workflow/WorkflowProposeCreateTool.cs`.
- [ ] T075 [US2] Implement `workflow.validateDraft` tool in `src/modules/Elsa.AI.Host/Tools/Workflow/WorkflowValidateDraftTool.cs`.
- [ ] T076 [US2] Implement proposal details endpoint in `src/modules/Elsa.AI.Host/Endpoints/AI/Proposals/Get/Endpoint.cs`.
- [ ] T077 [US2] Implement proposal approve endpoint in `src/modules/Elsa.AI.Host/Endpoints/AI/Proposals/Approve/Endpoint.cs`.
- [ ] T078 [US2] Implement proposal reject endpoint in `src/modules/Elsa.AI.Host/Endpoints/AI/Proposals/Reject/Endpoint.cs`.
- [ ] T079 [US2] Implement proposal apply endpoint in `src/modules/Elsa.AI.Host/Endpoints/AI/Proposals/Apply/Endpoint.cs`.
- [ ] T080 [US2] Add stale baseline checks in `src/modules/Elsa.AI.Host/Proposals/WorkflowProposalApplier.cs`.
- [ ] T081 [US2] Add durable proposal audit events in `src/modules/Elsa.AI.Host/Proposals/AIProposalAuditService.cs`.
- [ ] T082 [US2] Add proposal stream notifications in `src/modules/Elsa.AI.Host/Streaming/AIStreamEventMapper.cs`.
- [ ] T083 [US2] Draft paired Studio Razor proposal viewer using applicable table/dialog conventions from the `elsa-extensions` AI prototype in `../elsa-studio/src/modules/Elsa.Studio.AI/UI/Components/WeaverProposalViewer.razor`.
- [ ] T084 [US2] Run proposal tests in `test/integration/Elsa.AI.IntegrationTests/Elsa.AI.IntegrationTests.csproj`.

**Checkpoint**: AI-generated workflow creation is durable, proposal-only, reviewable, validated, applicable, and audited.

---

## Phase 5: User Story 3 - Edit and validate existing workflows safely (Priority: P3)

**Goal**: Users can ask Weaver to explain, modify, and validate existing workflows through durable reviewable diffs and proposals.

**Independent Test**: Attach an existing workflow, request a targeted change, verify graph diff, warnings, validation, durable proposal storage, and proposal-only apply.

### Tests for User Story 3

- [ ] T085 [P] [US3] Add workflow explain tool tests in `test/unit/Elsa.AI.Host.UnitTests/Tools/WorkflowExplainToolTests.cs`.
- [ ] T086 [P] [US3] Add workflow update proposal tests in `test/unit/Elsa.AI.Host.UnitTests/Tools/WorkflowProposeUpdateToolTests.cs`.
- [ ] T087 [P] [US3] Add graph diff tests in `test/unit/Elsa.AI.Host.UnitTests/Proposals/WorkflowGraphDiffTests.cs`.
- [ ] T088 [P] [US3] Add update proposal persistence tests in `test/integration/Elsa.AI.IntegrationTests/AIWorkflowUpdateProposalPersistenceTests.cs`.

### Implementation for User Story 3

- [ ] T089 [P] [US3] Implement `workflow.getDefinition` tool in `src/modules/Elsa.AI.Host/Tools/Workflow/WorkflowGetDefinitionTool.cs`.
- [ ] T090 [P] [US3] Implement `workflow.listDefinitions` tool in `src/modules/Elsa.AI.Host/Tools/Workflow/WorkflowListDefinitionsTool.cs`.
- [ ] T091 [US3] Implement `workflow.explain` tool in `src/modules/Elsa.AI.Host/Tools/Workflow/WorkflowExplainTool.cs`.
- [ ] T092 [US3] Implement `workflow.proposeUpdate` tool in `src/modules/Elsa.AI.Host/Tools/Workflow/WorkflowProposeUpdateTool.cs`.
- [ ] T093 [US3] Implement `workflow.validateChanges` tool in `src/modules/Elsa.AI.Host/Tools/Workflow/WorkflowValidateChangesTool.cs`.
- [ ] T094 [US3] Implement graph diff builder in `src/modules/Elsa.AI.Host/Proposals/WorkflowGraphDiffBuilder.cs`.
- [ ] T095 [US3] Add update proposal apply handling in `src/modules/Elsa.AI.Host/Proposals/WorkflowProposalApplier.cs`.
- [ ] T096 [US3] Draft paired Studio Razor graph diff view using applicable tabbed-editor conventions from the `elsa-extensions` AI prototype in `../elsa-studio/src/modules/Elsa.Studio.AI/UI/Components/WeaverGraphDiff.razor`.
- [ ] T097 [US3] Add breaking-change warning surfaces in `src/modules/Elsa.AI.Host/Proposals/WorkflowProposalWarnings.cs`.
- [ ] T098 [US3] Run workflow editing tests in `test/unit/Elsa.AI.Host.UnitTests/Elsa.AI.Host.UnitTests.csproj`.

**Checkpoint**: Existing workflows can be explained, edited through durable proposals, validated, diffed, and safely applied.

---

## Phase 6: User Story 4 - Analyze runtime incidents conversationally (Priority: P4)

**Goal**: Operators can ask Weaver to inspect runtime failures, summarize incidents, identify failing activities, and detect trends within attached references plus an explicit time range and diagnostics scope.

**Independent Test**: Attach failed instance and diagnostics references, select a time range/scope, ask for incident analysis and trends, and verify grounded evidence, scope boundaries, redaction, and read-only tool behavior.

### Tests for User Story 4

- [ ] T099 [P] [US4] Add instance get tool tests in `test/unit/Elsa.AI.Host.UnitTests/Tools/InstanceGetToolTests.cs`.
- [ ] T100 [P] [US4] Add failure aggregation tests in `test/unit/Elsa.AI.Host.UnitTests/Tools/InstanceAggregateFailuresToolTests.cs`.
- [ ] T101 [P] [US4] Add diagnostics redaction tests in `test/unit/Elsa.AI.Host.UnitTests/Context/DiagnosticsContextRedactionTests.cs`.
- [ ] T102 [P] [US4] Add trend scope enforcement tests in `test/unit/Elsa.AI.Host.UnitTests/Tools/InstanceDetectTrendsScopeTests.cs`.
- [ ] T103 [P] [US4] Add incident summary integration tests in `test/integration/Elsa.AI.IntegrationTests/AIRuntimeDiagnosticsTests.cs`.

### Implementation for User Story 4

- [ ] T104 [P] [US4] Implement `instance.get` tool in `src/modules/Elsa.AI.Host/Tools/Instances/InstanceGetTool.cs`.
- [ ] T105 [P] [US4] Implement `instance.search` tool in `src/modules/Elsa.AI.Host/Tools/Instances/InstanceSearchTool.cs`.
- [ ] T106 [US4] Implement `instance.getErrors` tool in `src/modules/Elsa.AI.Host/Tools/Instances/InstanceGetErrorsTool.cs`.
- [ ] T107 [US4] Implement `instance.aggregateFailures` tool in `src/modules/Elsa.AI.Host/Tools/Instances/InstanceAggregateFailuresTool.cs`.
- [ ] T108 [US4] Implement scoped `instance.detectTrends` tool in `src/modules/Elsa.AI.Host/Tools/Instances/InstanceDetectTrendsTool.cs`.
- [ ] T109 [US4] Implement `instance.summarize` tool in `src/modules/Elsa.AI.Host/Tools/Instances/InstanceSummarizeTool.cs`.
- [ ] T110 [US4] Implement diagnostics context provider in `src/modules/Elsa.AI.Host/Context/DiagnosticsContextProvider.cs`.
- [ ] T111 [US4] Implement diagnostics scope resolver in `src/modules/Elsa.AI.Host/Context/DiagnosticsScopeResolver.cs`.
- [ ] T112 [US4] Draft paired Studio Razor diagnostics timeline using applicable `/ai/*` route and dense table conventions from the `elsa-extensions` AI prototype in `../elsa-studio/src/modules/Elsa.Studio.AI/UI/Components/WeaverDiagnosticsTimeline.razor`.
- [ ] T113 [US4] Run runtime diagnostics tests in `test/integration/Elsa.AI.IntegrationTests/Elsa.AI.IntegrationTests.csproj`.

**Checkpoint**: Weaver can analyze failures and scoped trends conversationally without mutating runtime state or reading outside the selected scope.

---

## Phase 7: User Story 5 - Extend Weaver with governed tools and agents (Priority: P5)

**Goal**: Third-party modules can register tools, context providers, custom agents, and MCP integrations with enforceable governance metadata and explicit enablement for higher-risk tools.

**Independent Test**: Register a module-provided read-only tool and proposal/MCP tool, verify read-only availability, explicit enablement requirements, authorization denial, audit, and agent tool scoping.

### Tests for User Story 5

- [ ] T114 [P] [US5] Add extension registration tests in `test/unit/Elsa.AI.Host.UnitTests/Extensions/AIFeatureExtensionsTests.cs`.
- [ ] T115 [P] [US5] Add agent scoping tests in `test/unit/Elsa.AI.Host.UnitTests/Agents/AIAgentScopeTests.cs`.
- [ ] T116 [P] [US5] Add MCP registration tests in `test/unit/Elsa.AI.Host.UnitTests/Mcp/AIMcpRegistrationTests.cs`.
- [ ] T117 [P] [US5] Add explicit enablement tests in `test/unit/Elsa.AI.Host.UnitTests/Tools/AIToolEnablementTests.cs`.
- [ ] T118 [P] [US5] Add tool authorization denial audit tests in `test/integration/Elsa.AI.IntegrationTests/AIToolGovernanceTests.cs`.

### Implementation for User Story 5

- [ ] T119 [P] [US5] Implement AI feature extension APIs in `src/modules/Elsa.AI.Host/Extensions/AIFeatureExtensions.cs`.
- [ ] T120 [P] [US5] Add agent definition contracts in `src/modules/Elsa.AI.Abstractions/Contracts/IAIAgentDefinitionProvider.cs`.
- [ ] T121 [US5] Implement agent registry in `src/modules/Elsa.AI.Host/Services/AIAgentRegistry.cs`.
- [ ] T122 [US5] Implement agent-scoped tool resolver in `src/modules/Elsa.AI.Host/Services/AIAgentToolResolver.cs`.
- [ ] T123 [US5] Implement MCP registration models in `src/modules/Elsa.AI.Abstractions/Models/AIMcpServerRegistration.cs`.
- [ ] T124 [US5] Implement MCP registry in `src/modules/Elsa.AI.Host/Services/AIMcpServerRegistry.cs`.
- [ ] T125 [US5] Implement explicit enablement persistence in `src/modules/Elsa.AI.Host/Services/AIToolEnablementStore.cs`.
- [ ] T126 [US5] Map agent and MCP scopes to Copilot session config in `src/modules/Elsa.AI.Copilot/Adapters/CopilotSessionFactory.cs`.
- [ ] T127 [US5] Implement built-in `workflow-author` agent provider in `src/modules/Elsa.AI.Host/Agents/WorkflowAuthorAgentProvider.cs`.
- [ ] T128 [US5] Implement built-in `instance-diagnostics` agent provider in `src/modules/Elsa.AI.Host/Agents/InstanceDiagnosticsAgentProvider.cs`.
- [ ] T129 [US5] Add extension authoring README in `src/modules/Elsa.AI.Host/README.md`.
- [ ] T130 [US5] Run extensibility tests in `test/unit/Elsa.AI.Host.UnitTests/Elsa.AI.Host.UnitTests.csproj`.

**Checkpoint**: Modules can extend Weaver without weakening tool scope, explicit enablement, tenant enforcement, or audit.

---

## Phase 8: Cross-Cutting Verification, Documentation & Polish

**Purpose**: Close cross-cutting governance gaps, validate boundaries, update documentation, and run targeted checks.

- [ ] T131 [P] Update implementation quickstart notes in `specs/008-weaver-ai-copilot/quickstart.md`.
- [ ] T132 [P] Add API documentation in `src/modules/Elsa.AI.Host/README.md`.
- [ ] T133 [P] Add Copilot adapter documentation in `src/modules/Elsa.AI.Copilot/README.md`.
- [ ] T134 [P] Add persistence provider documentation in `src/modules/Elsa.AI.Persistence.EFCore/README.md`.
- [ ] T135 Add provider leakage boundary test in `test/integration/Elsa.AI.IntegrationTests/AIProviderIsolationTests.cs`.
- [ ] T136 Add durable governance restart test in `test/integration/Elsa.AI.IntegrationTests/AIDurableGovernanceTests.cs`.
- [ ] T137 Run abstractions tests in `test/unit/Elsa.AI.Abstractions.UnitTests/Elsa.AI.Abstractions.UnitTests.csproj`.
- [ ] T138 Run host tests in `test/unit/Elsa.AI.Host.UnitTests/Elsa.AI.Host.UnitTests.csproj`.
- [ ] T139 Run Copilot adapter tests in `test/unit/Elsa.AI.Copilot.UnitTests/Elsa.AI.Copilot.UnitTests.csproj`.
- [ ] T140 Run EF Core persistence tests in `test/unit/Elsa.AI.Persistence.EFCore.UnitTests/Elsa.AI.Persistence.EFCore.UnitTests.csproj`.
- [ ] T141 Run integration tests in `test/integration/Elsa.AI.IntegrationTests/Elsa.AI.IntegrationTests.csproj`.
- [ ] T142 Run provider boundary search over `src/modules/Elsa.AI.Abstractions` and `src/modules/Elsa.AI.Host`.
- [ ] T143 Compare final Studio UI plan against the `elsa-extensions` AI prototype and document carried-forward and rejected prototype patterns in `../elsa-studio/src/modules/Elsa.Studio.AI/README.md`.
- [ ] T144 Add provider/BYOK configuration registry tests in `test/unit/Elsa.AI.Host.UnitTests/Providers/AIProviderConfigurationRegistryTests.cs`.
- [ ] T145 Implement provider/BYOK configuration registry with secret-reference handling in `src/modules/Elsa.AI.Host/Services/AIProviderConfigurationRegistry.cs`.
- [ ] T146 Add OpenTelemetry activity and metric definitions for chat, tools, proposals, and provider calls in `src/modules/Elsa.AI.Host/Telemetry/AITelemetry.cs`.
- [ ] T147 Add OpenTelemetry instrumentation tests in `test/unit/Elsa.AI.Host.UnitTests/Telemetry/AITelemetryTests.cs`.
- [ ] T148 Add redaction boundary tests for model context, tool results, proposal rationale, stream events, and audit records in `test/unit/Elsa.AI.Host.UnitTests/Redaction/AIRedactionBoundaryTests.cs`.
- [ ] T149 Implement centralized AI redaction boundary service in `src/modules/Elsa.AI.Host/Services/AIRedactionService.cs`.
- [ ] T150 Add prompt, session, model event, and tool invocation audit tests in `test/integration/Elsa.AI.IntegrationTests/AISessionAuditTests.cs`.
- [ ] T151 Add configurable conversation/session retention expiration tests in `test/unit/Elsa.AI.Host.UnitTests/Conversations/AIConversationRetentionTests.cs`.
- [ ] T152 Add latency smoke tests for first stream event, capabilities, and proposal validation in `test/integration/Elsa.AI.IntegrationTests/AILatencyTests.cs`.
- [ ] T153 Document Core and Studio implementation worktree setup in `specs/008-weaver-ai-copilot/quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user stories.
- **Phase 3 US1**: Depends on Phase 2; MVP chat slice.
- **Phase 4 US2**: Depends on Phase 2 and should follow US1 for chat/proposal stream notifications.
- **Phase 5 US3**: Depends on Phase 4 proposal lifecycle for update proposals.
- **Phase 6 US4**: Depends on Phase 2 and can proceed after US1 tool streaming is stable.
- **Phase 7 US5**: Depends on Phase 2 and should follow US1 tool execution semantics.
- **Phase 8 Cross-Cutting Verification**: Documentation and final test tasks depend on selected story implementation; provider, telemetry, redaction, audit, retention, latency, and worktree tasks should be pulled forward into the earliest story phase that needs them.

### User Story Dependencies

- **US1 (P1)**: First executable slice; depends only on foundational contracts and stores.
- **US2 (P2)**: Uses US1 streaming and foundational durable proposal/audit storage.
- **US3 (P3)**: Uses US2 proposal lifecycle and adds update/diff behavior.
- **US4 (P4)**: Uses US1 chat/tool execution and adds scoped read-only diagnostics tools.
- **US5 (P5)**: Uses foundational registry contracts and validates third-party extensibility.

### Parallel Opportunities

- T001 through T009 and T011 through T016 can run in parallel.
- T017 through T027 and T031 through T036 can run in parallel after project creation.
- US1 tests T045 through T051 can run in parallel.
- US2 tests T066 through T071 can run in parallel.
- US3 tests T085 through T088 can run in parallel.
- US4 tests T099 through T103 can run in parallel.
- US5 tests T114 through T118 can run in parallel.
- Documentation tasks T131 through T134 can run in parallel after APIs settle.
- Cross-cutting tests T144, T147, T148, T150, T151, and T152 can run in parallel after foundational contracts exist.

## Parallel Example: User Story 1

```text
Task: "Add context authorization tests in test/unit/Elsa.AI.Host.UnitTests/Context/AIContextResolverTests.cs"
Task: "Add stream event mapping tests in test/unit/Elsa.AI.Host.UnitTests/Streaming/AIStreamEventMapperTests.cs"
Task: "Add reconnect grace tests in test/unit/Elsa.AI.Host.UnitTests/Streaming/AIReconnectGraceTests.cs"
Task: "Add chat endpoint streaming integration tests in test/integration/Elsa.AI.IntegrationTests/AIChatEndpointTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add proposal lifecycle tests in test/unit/Elsa.AI.Host.UnitTests/Proposals/AIProposalLifecycleTests.cs"
Task: "Add same-authorized-user approval tests in test/unit/Elsa.AI.Host.UnitTests/Proposals/AIProposalApprovalTests.cs"
Task: "Add durable proposal restart integration tests in test/integration/Elsa.AI.IntegrationTests/AIProposalPersistenceTests.cs"
Task: "Add durable proposal audit integration tests in test/integration/Elsa.AI.IntegrationTests/AIProposalAuditTests.cs"
```

## Parallel Example: User Story 4

```text
Task: "Add failure aggregation tests in test/unit/Elsa.AI.Host.UnitTests/Tools/InstanceAggregateFailuresToolTests.cs"
Task: "Add diagnostics redaction tests in test/unit/Elsa.AI.Host.UnitTests/Context/DiagnosticsContextRedactionTests.cs"
Task: "Add trend scope enforcement tests in test/unit/Elsa.AI.Host.UnitTests/Tools/InstanceDetectTrendsScopeTests.cs"
Task: "Add incident summary integration tests in test/integration/Elsa.AI.IntegrationTests/AIRuntimeDiagnosticsTests.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2, including durable proposal and audit persistence.
2. Complete Phase 3 only.
3. Run US1 unit and integration tests.
4. Stop and validate server-hosted chat, context references, capabilities, tool listing, streaming deltas, tool progress, reconnect recovery, and provider isolation.

### Incremental Delivery

1. Add US2 to enable durable safe workflow creation proposals.
2. Add US3 to enable workflow edits and graph diffs.
3. Add US4 to enable scoped runtime intelligence.
4. Add US5 to open governed third-party extension points.
5. Run Phase 8 validation before PR handoff.

### Paired Studio Work

Studio implementation tasks reference `../elsa-studio/` because this workspace is `elsa-core`. If the Studio repository is not checked out beside this repository, complete the server/Core tasks first and carry the Studio tasks into the sibling repository.

Review the `../elsa-extensions-investigation` `origin/feat/ai` prototype before Studio work. Use route/menu/table/form conventions where they help, but keep chat/proposal review primary and avoid raw API key reveal or provider JSON configuration as the main UX.
