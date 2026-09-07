# Codebase Concerns

## Top Risks

| Severity | Concern | Evidence | Impact | Suggested action |
|---|---|---|---|---|
| Medium | External sign-in completion spans session, grant, and link stores | `ExternalAuthenticationBroker.cs` | Partial writes are possible if a later store fails | Keep writes ordered and make completion operations idempotent |
| Medium | Custom sign-in trackers must preserve tenant/connection/subject/user scoping | `IExternalIdentitySignInTracker` | Incorrect implementations could update the wrong link | Require conformance tests for tracker implementations |
| Low | In-memory stores are single-node only | `InMemoryExternalIdentityProvisioner.cs` | State diverges across replicas | Use EF Core stores for multi-node deployments |

## Technical Debt

| Debt item | Why | Where | Risk | Suggested fix |
|---|---|---|---|---|
| Broker constructor has many dependencies | Broker owns several local and external flows | `ExternalAuthenticationBroker.cs` | Manual tests/benchmarks are costly to construct | Consider focused orchestration objects only when another change justifies it |
| `[TODO]` Repository-wide debt inventory | Investigation intentionally focused on external identity tracking | `doc/codebase/.codebase-scan.txt` | Other modules are not assessed here | Run an extended acquisition audit separately |

## Security Concerns

| Risk | OWASP | Evidence | Current mitigation | Gap |
|---|---|---|---|---|
| Cross-tenant link access | A01 | Identity-link endpoint/store filters | Tenant accessor plus tenant-scoped queries | Custom stores need equivalent tests |
| Subject/token disclosure | A02 | Safe endpoint DTO and redaction tests | HMAC subject storage and redaction | None found in this flow |

## Performance and Scaling Concerns

| Concern | Evidence | Symptom | Scaling risk | Suggested improvement |
|---|---|---|---|---|
| Compare-and-set tracking may retry under contention | `EFCoreExternalIdentityProvisioner.RecordSuccessfulSignInAsync` | Extra query/update round trip on races | Hot shared identities could retry | Retain unique tuple index and monitor contention |

## Fragile/High-Churn Areas

| Area | Why fragile | Churn signal | Safe change strategy |
|---|---|---|---|
| External authentication broker/persistence | Security-sensitive multi-store flow | Active feature code and current defect | Use callback-level, EF, and endpoint regression tests together |

## `[ASK USER]` Questions

None for this focused investigation.

## Evidence

- `doc/codebase/.codebase-scan.txt`
- `src/modules/Elsa.ExternalAuthentication/Services/ExternalAuthenticationBroker.cs`
- `src/modules/Elsa.ExternalAuthentication.Persistence.EFCore/Stores/EFCoreExternalIdentityProvisioner.cs`
- `test/integration/Elsa.ExternalAuthentication.IntegrationTests/`
