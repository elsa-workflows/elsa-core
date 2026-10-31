# Elsa Product Website Feature Source

This document is source material for a product website aimed at developers, architects, and CTOs evaluating workflow engines. It was compiled from:

- `elsa-core`: `/Users/sipke/Projects/Elsa/elsa-core`
- `elsa-studio`: `/Users/sipke/Projects/Elsa/elsa-studio`
- `elsa-extensions`: `/Users/sipke/Projects/Elsa/elsa-extensions-investigation` (`Elsa.Extensions.sln`)

Use this as the comprehensive feature inventory. The website should not show every item with equal weight; the final section contains instructions for Lovable AI to select and present the strongest public-facing set.

## Core Workflow Engine

### Embeddable .NET Workflow Engine

Elsa runs inside any .NET application and gives teams a workflow runtime they can embed directly into their own products, services, portals, back-office systems, and integration platforms. Workflows can be hosted in ASP.NET Core, run from worker services, or exposed through Elsa's server-style API surface. This matters to engineering teams that need workflow automation without handing orchestration to a black-box SaaS product.

### Code-First, Designer-First, and JSON Workflows

Workflows can be authored in C#, visually in Elsa Studio, or represented as JSON. Developers can keep critical workflows close to source control, product teams can iterate visually, and platforms can import/export workflow definitions through APIs or storage providers. This gives organizations a practical path from developer-owned automation to collaborative workflow operations.

### Long-Running and Short-Running Workflows

Elsa supports short-running automations as well as durable, long-running business processes that wait for external events, approvals, timers, or callbacks. The runtime persists workflow state, bookmarks, triggers, execution records, and incidents, allowing processes to pause and resume across application restarts and infrastructure events.

### Rich Control Flow

Elsa includes core control-flow activities such as `Sequence`, `If`, `Switch`, `For`, `ForEach`, `While`, `Parallel`, `Fork`, `Break`, `End`, `Finish`, `Complete`, and `Fault`. Teams can model simple linear automations, branching business rules, parallel work, loops, and failure paths without building custom orchestration logic for every process.

### Flowcharts, Sequences, and State Machines

Elsa's engine includes flowchart, sequence, and state machine execution models. Flowcharts support visual business-process modeling with joins, forks, switches, and token-based execution semantics. Sequences provide straightforward procedural automation. State machines model named states and trigger-driven transitions for processes that are naturally lifecycle-oriented.

### Triggers, Bookmarks, and Event-Driven Resumption

Workflows can start from triggers and resume from bookmarks. This is the foundation for event-driven automation: HTTP requests, timers, messages, webhooks, user actions, or external systems can start or continue the right workflow instance. Bookmarks are indexed by the runtime so waiting workflows can be found and resumed efficiently.

### Background Dispatch and Runtime Workers

The workflow runtime includes background dispatch for workflows, stimuli, tasks, and activities. This allows requests to hand off work and return while Elsa continues processing asynchronously. It also supports patterns where one workflow dispatches another workflow without coupling the caller to immediate execution.

### Transactional Dispatch Outbox

Elsa can use a transactional outbox for workflow dispatches created from inside a running workflow. The outbox prevents silent loss when a process crashes between persisting workflow state and enqueueing a dispatch command. Delivery is at-least-once, with idempotency support for child workflow dispatches.

### Graceful Shutdown and Recovery

The runtime includes node-local quiescence, drain orchestration, ingress-source pause/resume behavior, and interrupted workflow recovery. This gives operators a controlled way to stop servers without accepting new work mid-drain, and it helps recover work that was interrupted by shutdowns or crashes.

### Runtime Administration APIs

Elsa exposes runtime administration endpoints for status, pause, resume, and force-drain operations. Operators can inspect and control workflow runtime state through API-driven automation rather than relying only on process-level controls.

### Dead-Lettered Bookmark Queue Management

Bookmark queue items that expire or exceed delivery attempts can be moved to a dead-letter store. APIs allow operators to inspect, replay, and delete dead-lettered items. This is valuable for production workflows where failed event delivery should be visible and recoverable.

### Workflow Versioning and Migration

Elsa supports workflow definition versioning and API-driven migration. Teams can evolve workflows over time, publish new versions, and manage compatibility between definitions and running instances.

### Workflow Management APIs

Elsa has API surfaces for definitions, instances, execution journals, activity executions, runtime administration, triggers, bookmarks, and related workflow management concerns. This allows workflow capabilities to be embedded into custom platforms and integrated with existing developer or operator tooling.

### Custom Activities

Developers can add first-party or product-specific activities with strongly typed inputs and outputs, descriptors, UI hints, and designer integration. This is the extension point that turns Elsa from a generic workflow engine into a domain automation platform for a specific business.

### Activities as Composable Building Blocks

Elsa models workflow work as activities. Activities can be composed into higher-level workflows, and workflows themselves can be exposed as callable units. This supports reuse and standardization across teams.

### Dynamic Activity Handling

Elsa includes dynamic and missing-activity handling, allowing workflow definitions and designer/API surfaces to evolve while maintaining controlled behavior when activity types are not available.

### Expressions and Scripting

Elsa supports dynamic expressions through C#, JavaScript, Python, and Liquid expression modules. Activities can evaluate runtime data, inputs, variables, outputs, workflow context, and external values without forcing every transformation into compiled code.

### Workflow Inputs, Outputs, Variables, and Correlation

Elsa includes typed input/output models, variables, workflow input, activity outputs, correlation, and runtime state capture. This gives workflows structured data flow and allows external systems to correlate events with the right workflow instance.

### Persistence-Agnostic Architecture

Elsa is persistence-agnostic and includes persistence packages for EF Core providers such as SQLite, SQL Server, PostgreSQL, MySQL, and Oracle. Extension packages also add Dapper and MongoDB persistence options. This lets teams choose infrastructure that fits existing operational standards.

### Multi-Tenancy and Identity

Elsa includes identity, authorization, tenant support, and tenant-aware runtime behavior. This is important for SaaS platforms and internal platforms where different teams, customers, or environments need isolated workflow definitions, instances, and permissions.

### Secrets Management

Elsa includes a secrets module design with in-memory, configuration-backed, and encrypted Elsa-managed stores. Secrets are intended for use by activities and integrations that need credentials without exposing sensitive values in workflow definitions.

### Labels, Key Values, SAS Tokens, and Supporting Infrastructure

Elsa includes supporting modules such as labels, key-value storage, SAS tokens, caching, mediator infrastructure, and feature/module composition. These are less visible as standalone marketing features but help developers build complete workflow-enabled products.

## Operations and Observability

### Workflow Execution Journal

Elsa stores workflow and activity execution records that can be exposed through APIs and Studio. Operators and developers can inspect what ran, what failed, and how a workflow reached its current state.

### Structured Log Capture

`Elsa.Diagnostics.StructuredLogs` captures semantic `ILogger` events from Elsa hosts, redacts sensitive data, stores recent records, exposes REST query endpoints, and streams live events to Studio over SignalR. Records include rendered messages, templates, properties, scopes, exceptions, source metadata, trace/span IDs, workflow context, tenant, and correlation fields.

### Durable Structured Log Persistence

Structured logs default to a bounded in-memory store, with relational/SQLite persistence available for durable storage. The design includes bounded write queues, batch writes, retention services, storage diagnostics, and dropped-write reporting.

### Console Log Streaming

`Elsa.Diagnostics.ConsoleLogs` captures raw stdout and stderr, preserves the original console output destination, redacts content before buffering or streaming, and exposes recent/live console lines through REST and SignalR. This helps operators diagnose container or process-level output without leaving the workflow management experience.

### OpenTelemetry Workflow Diagnostics

The extensions repository includes `Elsa.OpenTelemetry`, and Studio includes an OpenTelemetry diagnostics module. The intent is workflow-aware trace, metric, and log exploration where Core remains responsible for ingestion, redaction, storage, permissions, and API contracts while Studio presents normalized diagnostics views.

### Configurable Workflow Logging Framework

The extensions repository includes a logging framework with configurable sinks such as console and Serilog, appsettings-based configuration, custom sink factories, and a `Log` activity for emitting structured log entries from workflows.

### Source and Storage Diagnostics

Diagnostics modules track source metadata, source health, storage pressure, dropped durable writes, dropped console lines, and live subscription state. These details are useful in clustered or high-volume environments where observability itself needs operational guardrails.

## Elsa Studio

### Modular Workflow Management UI

Elsa Studio is a modular Blazor application framework built with MudBlazor for managing workflows and related entities. It can run as Blazor Server, Blazor WebAssembly, hosted WebAssembly, or custom elements depending on host requirements.

### Visual Workflow Designer

Studio provides a drag-and-drop workflow designer for authoring workflows visually. It integrates with Elsa activity descriptors and UI hints so custom activities can appear in the designer alongside built-in activities.

### Workflow Definition and Instance Management

Studio includes workflow modules for managing definitions, viewing workflow state, and working with execution/runtime data exposed by the backend. This turns the engine from a library into an operator-facing workflow platform.

### Real-Time Workflow and Diagnostics Updates

Studio uses SignalR-backed clients for live updates such as structured logs and console logs. Authentication providers can configure SignalR connections consistently, allowing live operational views to work with JWT, Elsa Identity, or OpenID Connect setups.

### Structured Logs Viewer

Studio's structured log viewer loads recent semantic logs, subscribes to live updates, supports source selection, filtering, pause/reconnect/clear behaviors, and exposes detailed log inspection with properties, scopes, exceptions, trace/span fields, tenant, workflow, correlation, and raw JSON.

### Console Logs Viewer

Studio includes a diagnostics console page for raw stdout/stderr streaming. Navigation is feature-gated by backend capability and permission, with unavailable and unauthorized states handled in the UI.

### OpenTelemetry Diagnostics Viewer

Studio includes a diagnostics OpenTelemetry shell at `/diagnostics/opentelemetry`, with expected views for resource search, trace search/detail, metrics, OTLP logs, storage diagnostics, and collector configuration.

### Authentication Options

Studio supports Elsa Identity and OpenID Connect authentication modules across Blazor Server and WebAssembly hosting models. The architecture includes token providers, automatic refresh, backend API token acquisition, SignalR authentication configuration, and custom unauthorized UI rendering.

### Localization

Studio includes localization modules for Blazor Server and WebAssembly. Applications can register localization providers, configure supported cultures, and translate the Studio UI for international teams.

### Extensible Studio Modules

Studio modules include dashboard, workflows, designer, labels, environments, security, UI hints, diagnostics, localization, authentication, secrets, workflow contexts, and agents. The modular architecture lets product teams compose the Studio experience that fits their platform.

### Performance-Oriented Designer

Recent designer work includes batched activity size calculation, activity size caching, and conditional size updates to improve large workflow loading. This matters for teams building complex workflows with many activities.

### React Wrapper and Custom Embedding

Studio includes wrapper projects and host variants that support embedding Elsa workflow design experiences into different frontends, including React-oriented integration samples.

## Built-In Core Capabilities and Activities

### HTTP Workflows

Elsa includes HTTP activities for exposing endpoints, sending HTTP requests, downloading HTTP files, and writing HTTP responses or file responses. This makes it natural to build API-triggered workflows, webhook receivers, integration endpoints, and workflow-backed HTTP services.

### Scheduling and Timers

Scheduling activities include cron, delay, start-at, and timer behavior. Workflows can run on schedules, pause until a specific time, or wait for durations without custom background job code.

### Alterations

Elsa includes alteration modules for applying operational changes to workflow instances. Extensions add a MassTransit-backed alteration background runner for broker-backed, more resilient background processing.

### Resilience

Elsa includes resilience modules for workflow execution and activity behavior. These support more robust process automation in failure-prone distributed systems.

### Workflow Providers

Elsa includes workflow providers for loading definitions from blob storage and ElsaScript sources. This allows teams to source workflows from external storage rather than only from the management database.

### ElsaScript DSL

The ElsaScript DSL module provides a workflow definition scripting format, useful for teams that want text-based workflow authoring and source-controlled definitions.

## Extensions Ecosystem

### AI Agents and Multi-Agent Workflows

The extensions repository includes an Elsa Agents module built on Microsoft's Agent Framework/Semantic Kernel Agents. It supports configuration-based agents, code-first agents, multi-agent workflows, tool calling, persistent conversations/state, REST APIs, persistence packages, OpenAI and Azure OpenAI providers, workflow activities, and Studio UI.

### Connections Framework

The extensions repository includes connection modules for registering, describing, and applying connection information to activities. This is a foundation for reusable, centrally managed integration credentials and connection settings.

### SQL Automation

The SQL extension provides `SqlQuery`, `SqlCommand`, and `SqlSingleValue` activities, with providers for MySQL, PostgreSQL, SQLite, and SQL Server. It supports parameterization of workflow inputs, outputs, variables, activity context, execution context, workflow properties, POCOs, dictionaries, arrays, lists, and JSON objects, plus SQL syntax highlighting in Studio.

### CSV Processing

The CSV extension provides a `ReadCsv` activity that can read byte arrays, streams, strings, URLs, and uploaded files. It supports delimiters, header detection, dictionary output, and optional strongly typed mapping through CsvHelper.

### Email Automation

The extensions repository includes an email module with a `SendEmail` activity. This is a common workflow primitive for notifications, approvals, and operational alerts.

### Slack Automation

The Slack extension includes activities for channels, messages, reminders, reactions, stars, files, users, search, and event triggers. Workflows can create/update/delete messages, pin/unpin messages, upload files, manage reminders, list/search users and channels, and react to Slack events.

### GitHub DevOps Automation

The GitHub extension includes activities for issues, comments, labels, pull requests, repositories, users, organizations, milestones, releases, gists, branch search, GraphQL queries, and webhook-style triggers for issues, pull requests, and branches. Tokens should be stored securely through Elsa secrets.

### Service Bus and Messaging

Extensions include Azure Service Bus, Kafka, and MassTransit packages. Activities can send/publish/produce messages and react to received messages, enabling event-driven workflow automation across common messaging infrastructure.

### Distributed Runtime with Proto.Actor

The extensions repository includes actor-based runtime packages, including Proto.Actor-backed workflow runtime and actor modules. This extends Elsa's throughput and distributed execution story beyond the base runtime.

### Distributed Caching

Extensions include distributed caching modules over MassTransit and Proto.Actor. These support clustered scenarios where local in-memory assumptions are not enough.

### Scheduling Backends

Extensions include Hangfire and Quartz scheduling packages, with Quartz EF Core providers for MySQL, PostgreSQL, SQL Server, and SQLite. This lets teams use mature scheduling infrastructure behind Elsa's workflow scheduling concepts.

### Persistence Extensions

Extensions add persistence options such as Dapper, MongoDB, and Elasticsearch. These broaden deployment choices for organizations that standardize on specific storage technologies.

### File and Blob Storage

Extensions include Azure Storage and local file storage activities such as upload blob, save file, and open file. Workflows can move files through storage systems as part of business processes.

### HTTP OpenAPI and Webhooks

Extensions include OpenAPI and webhook modules for HTTP workflows. These help expose, document, and consume HTTP-driven workflow integration surfaces.

### Compression and I/O

Extensions include I/O and compression modules, including ZIP archive creation. These are useful in document, file transfer, and batch processing workflows.

### Telnyx Telephony Automation

The Telnyx extension includes call-control activities and webhook triggers such as answer call, hang up, dial, bridge calls, speak text, gather DTMF input, get call status, and react to call hangup or general webhook events.

### Orchard Core Integration

The extensions repository includes Orchard Core integration modules, useful for teams building CMS-backed or modular application platforms that also need workflow automation.

### Retention

The retention extension includes cleanup strategies and collectors for workflow-related records. This supports operational lifecycle management for workflow data.

### Workflow Contexts

Workflow context modules let activities set and use contextual parameters associated with workflow context providers. Studio modules add UI support for workflow context activity settings.

### Drop-Ins

Drop-in modules suggest a packaging and extension mechanism for pluggable capabilities. These are relevant to teams building modular workflow platforms.

### Planned and Roadmap Extensions

The extensions README lists planned or in-development integrations for Telegram, Discord, Microsoft Teams, Gmail, Outlook, Google Calendar, Microsoft Calendar, Google Drive, OneDrive, Azure Storage, Dropbox, Azure DevOps, GitLab, Jenkins, Datadog, cloud functions, CRM systems, payment providers, AI providers, video platforms, and industrial protocols such as OPC UA, Modbus, and MQTT Sparkplug. These should be presented carefully as ecosystem direction unless confirmed released.

## Architectural Selling Points

### Modular Feature System

Elsa uses explicit feature/module registration, dependency declarations, and extension methods. Teams can add only the capabilities they need, keep hosts lean, and build their own modules using the same conventions as first-party packages.

### Provider-Neutral Boundaries

Many Elsa capabilities are expressed behind provider contracts: persistence, structured logs, scheduling, workflow providers, SQL clients, authentication, storage, logging sinks, and diagnostics. This gives architects flexibility to standardize on their own infrastructure.

### API-First and Studio-Ready

Core capabilities are exposed through APIs and then surfaced in Studio. This means teams can automate through APIs, use Studio as an operator interface, or build their own UI on top of the same contracts.

### Production Operations Mindset

The runtime includes operational concepts such as graceful shutdown, recovery scanning, dead-letter queues, storage diagnostics, redaction, bounded buffers, authorization, tenant-aware behavior, and runtime admin endpoints. These are the details architects and CTOs look for when evaluating production fit.

### Developer Extensibility

Elsa is designed for developers to extend: custom activities, expression providers, API endpoints, stores, workflow providers, runtime ingress sources, Studio modules, authentication providers, log sinks, SQL clients, agent providers, and integration modules all have clear extension paths.

### Open Source and Ecosystem Friendly

Elsa is distributed through NuGet packages, Docker images, GitHub repositories, and community channels. The ecosystem includes Core, Studio, Extensions, Templates, package catalogs, and professional support options.

## Recommended Website Feature Selection

For the public product website, do not display all features as a flat checklist. Select the most compelling features for evaluators:

1. Visual workflow designer plus embeddable .NET runtime.
2. Code-first, visual, and JSON authoring.
3. Long-running durable workflows with triggers and bookmarks.
4. Rich control flow, including flowcharts, sequences, state machines, and parallel execution.
5. API-first workflow management and runtime administration.
6. Persistence choices and production-ready storage boundaries.
7. Observability: execution journal, structured logs, console logs, OpenTelemetry direction.
8. Security and platform fit: identity, authorization, multi-tenancy, secrets.
9. Extensibility: custom activities, modules, expression providers, Studio modules.
10. Integration ecosystem: HTTP, scheduling, SQL, Slack, GitHub, messaging, files, AI agents.
11. Operational readiness: graceful shutdown, recovery, dead-letter handling, durable dispatch outbox.
12. Studio as a modular operations console, not only a designer.

## Suggested Public Website Copy

### Hero

**Title:** Workflow automation for .NET teams

**Description:** Elsa is an open-source workflow engine and visual workflow platform for building long-running, event-driven, and integration-heavy processes inside your own .NET applications.

### Feature: Build Workflows Your Way

**Title:** Code-first, visual, or JSON workflows

**Description:** Define workflows in C#, design them visually in Elsa Studio, or store them as JSON for API-driven platforms. Developers can keep critical automation close to source control while business and operations teams collaborate through a visual designer.

### Feature: Durable Event-Driven Runtime

**Title:** Long-running workflows that wait, resume, and recover

**Description:** Model processes that pause for timers, webhooks, approvals, messages, and external events. Elsa persists workflow state, indexes triggers and bookmarks, and resumes the right instance when the next signal arrives.

### Feature: Visual Studio Experience

**Title:** A modular Studio for designers and operators

**Description:** Elsa Studio provides a Blazor-based workflow designer, management UI, diagnostics views, authentication modules, localization, and extensible modules for custom platform experiences.

### Feature: Integration-Ready

**Title:** Connect workflows to real systems

**Description:** Use built-in and extension activities for HTTP, scheduling, SQL, CSV, email, Slack, GitHub, Azure Service Bus, Kafka, files, storage, telephony, and AI agents. Add custom activities when your domain needs first-class workflow building blocks.

### Feature: Production Operations

**Title:** Built for production workflow operations

**Description:** Elsa includes runtime administration, execution journals, graceful shutdown, recovery scanning, dead-letter handling, structured log capture, console streaming, redaction, permissions, and storage diagnostics.

### Feature: Architecture Fit

**Title:** Modular, provider-neutral, and extensible

**Description:** Compose only the features you need, plug in your persistence and infrastructure choices, and extend the platform with custom modules, activities, expression providers, APIs, authentication providers, and Studio screens.

### Feature: Observability

**Title:** See what workflows are doing

**Description:** Inspect workflow execution history, activity records, structured logs, raw console output, source metadata, trace and span correlation, workflow context, tenant, and correlation fields from API and Studio diagnostics views.

### Feature: Security and Multi-Tenancy

**Title:** Designed for secure platforms

**Description:** Elsa includes identity, authorization, tenant-aware behavior, secrets management, authenticated SignalR diagnostics, and permission-gated APIs for teams building SaaS, internal platforms, and regulated automation systems.

### Feature: AI Workflow Orchestration

**Title:** Orchestrate agents inside workflows

**Description:** Elsa Agents brings configuration-based and code-first AI agents, multi-agent workflows, tool calling, stateful conversations, OpenAI and Azure OpenAI providers, APIs, persistence, and Studio integration into the workflow model.

## Lovable AI Instructions

Use the source inventory above to design a product website section for Elsa Workflows. The target audience is developers, software architects, engineering managers, and CTOs evaluating workflow engines for .NET platforms.

Prioritize clarity, technical credibility, and enterprise-readiness over hype. Select the most interesting features rather than displaying the complete inventory. The site should make Elsa feel like a serious embeddable workflow platform with a strong visual designer, production runtime, and integration ecosystem.

Recommended structure:

1. Hero: "Workflow automation for .NET teams" with a concise description about open-source, embeddable, event-driven workflow automation.
2. Primary feature grid: 6-8 cards covering authoring flexibility, durable runtime, visual Studio, integrations, operations, observability, security, and extensibility.
3. Architecture band: explain modular/provider-neutral design, custom activities, APIs, and persistence choices.
4. Integrations band: show categories rather than a huge checklist. Use "HTTP & APIs", "Scheduling", "Databases", "Messaging", "DevOps", "Communication", "Files & Storage", and "AI Agents".
5. Operations band: highlight graceful shutdown, recovery, dead-letter queues, runtime admin, logs, and diagnostics.
6. CTA: invite visitors to try the Docker image, explore docs, or view GitHub.

Use these exact card titles unless the layout requires shorter labels:

- Build Workflows Your Way
- Durable Event-Driven Runtime
- Visual Workflow Studio
- Integration-Ready Automation
- Production Workflow Operations
- Observable by Design
- Secure Multi-Tenant Platform
- Extensible by Developers

Use concise card descriptions derived from the suggested public copy. Avoid listing unreleased/planned extensions as if they are shipped. If planned ecosystem items are mentioned, label them as roadmap or ecosystem direction.

Visual direction:

- Use product screenshots or workflow/designer imagery prominently.
- Keep the design technical, polished, and readable.
- Avoid vague abstract SaaS imagery.
- Favor architecture diagrams, workflow nodes, log/detail panels, integration icons, and code snippets.
- Make the first viewport communicate that Elsa is both a workflow engine and a visual workflow platform.

Tone:

- Direct, practical, developer-first.
- Avoid exaggerated claims such as "effortless", "revolutionary", or "no-code magic".
- Emphasize control, extensibility, operational readiness, and .NET-native integration.
