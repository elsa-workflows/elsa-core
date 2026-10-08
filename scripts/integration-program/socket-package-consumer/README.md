# Socket package consumer: first real vertical

This independent Generic Host fixture consumes only one candidate's packages. Its assembly name is the existing audited friend `Elsa.Slack.Tests`, allowing the private literal-loopback policy without adding a public endpoint override or production friend. It is a console fixture, not the repository's source xUnit project. No project references or manual ASP.NET framework reference may be added to make package defects disappear.

The first case is `socket-package-held-commit-watch-resume`. It composes three sequential hosts in one fresh PostgreSQL database:

1. A non-Admission setup controller creates a synthetic managed ApiKey through the actual lifecycle, encrypted secret generation and logical credential binding. It starts no workflow and disposes before the next host.
2. An isolated guarded host provisions the allowlisted real Watch graph inactive, verifies normal publication, definitely activates, then disposes without executing an event.
3. The final immutable route captures that returned epoch, selects the real hosted Socket listener and actual guarded runtime. Its narrow authorizer admits only the fixed listener system principal/purpose/connection; public lifecycle/background services remain denied, and the existing stored exact-instance grant policy denies workflow credential access without a grant.

A real loopback `apps.connections.open` HTTP request verifies the synthetic bearer in memory and returns the physical WebSocket endpoint. The peer performs the RFC6455 upgrade, sends hello and a human event, and records actual ACK frames. It never manufactures a `ReceivedFrame`, calls an envelope processor directly, injects a message source or manually dispatches an admitted workflow.

The Admission-only transaction interceptor arms after all provisioning, identifies the actual initial Admitted event row, and holds its commit. Separate actual PG reads and runtime/activity counters establish no visible admission/instance, ACK or effects while held. After release, the real peer's ACK receipt follows the actual commit callback. A separate checkpoint activity remains held to establish that ACK is independent of workflow duration. The real Watch's original Message and scalar outputs are checked by an audited activity notification and persisted in workflow output; they are reloaded both at suspension and after legitimate bookmark continuation. An audited scoped boundary observer waits for actual durable-batch scope disposal before the fixture resumes, avoiding sleeps and retry-resume.

The case also checks persisted suspension/checkpoint/bookmark, seven denied owned entrypoints with no ledger/state/notification change, public background/lifecycle denials, real no-grant policy/resolver denial, one Watch/suspension/resume, terminal state and bookmark consumption, zero provider API calls, real execution-cycle unwind and selected migrations/reapplication. Connections/Secrets share the normal existing history; Admission and receipts use their separate expected histories. The human-only case leaves the receipt table empty; receipt history/model validation is exercised, but this does not claim a populated discard-table regression.

## Hosted invocation

Stage this folder outside the repository with empty parent Directory props/targets, isolated candidate-only NuGet feed/cache/config and a fresh PostgreSQL database. The runner owns database/container setup and teardown, source/archive hashes, compilation diagnostics, actual process identity and assembly-byte validation. Do not run this on a live workspace.

Mandatory configuration:

- MSBuild `CandidateVersion` with no fallback.
- `ELSA_SOCKET_PACKAGE_SOURCE_REVISION`: exact 40-character candidate SHA.
- `ELSA_SOCKET_PACKAGE_CANDIDATE_VERSION`: same candidate version passed to restore/build.
- `ELSA_SOCKET_PACKAGE_CONNECTION_STRING`: private fixture-only PostgreSQL connection.
- CLI `--feature classic|shell --tfm net8.0|net9.0|net10.0 --output <absolute private report path>`.

First hosted cell: classic/net10.0. For example, restore/build with `-p:TargetFrameworks=net10.0 -p:CandidateVersion=<candidate>` and build `-f net10.0`; invoke the resulting `Elsa.Slack.Tests.dll` directly with the CLI above. Source supports the later genuine Shell and other TFM cells, but only separately executed cells count as evidence.

`report-contract.json` specifies exact fields/keys. The report is written atomically only after all three hosts and the physical peer dispose; final health must be Stopped with empty gauges, and the client must retire before server disposal. Exceptions are never rendered. Fixed failure categories and trusted fixture coordinates are available on stderr. Raw diagnostics and loaded assembly paths remain private verifier inputs; normalize paths only after independently checking actual DLL bytes. The .NET start-time hash is fixture-reported, not an independently established OS start token. No container/process cleanup outside this fixture is claimed.

Synthetic marker to scan privately before retention: `synthetic-socket-package-secret-never-export-8666`. No actual credential or provider call is permitted.

## Sources and limits

Composition/migrations/provenance reuse the accepted `admission-package-consumer` recipe, with distinct fixture-specific orchestration. The physical peer follows the existing source Socket connection/session test pattern. Framework APIs are grounded in [EF Core interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors) and [WebSocket.CreateFromStream](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.websocket.createfromstream).

This is one authored first vertical, not complete #8666 acceptance. It does not replace required discard/collision, unknown-write/redelivery, reconnect/backpressure, withdrawal/revocation, cancellation-drain, legacy serialized compatibility, or complete package/source matrices. No live Slack authority, outgoing message permission, published package or operational acceptance is inferred. Local heavy compilation/runtime tests are intentionally not run; hosted exact-candidate execution is required.
