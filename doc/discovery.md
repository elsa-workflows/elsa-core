# Discovery — fork Elsa Workflows 3 (Mediawan)

> Livrable §8 CLAUDE.md — cartographie + points d'extension + gaps archi + questions ouvertes.
> **Date** : 2026-04-20 — **Auteur** : découverte initiale (O. Yahyaoui).

## 1. Résumé exécutif

Le fork `Fatilov/elsa-core-mdw` suit la branche `main` d'`elsa-workflows/elsa-core` (Elsa 3). À date, il ne contient **aucune modification custom du code Elsa** — les seuls commits divergents sont la configuration Claude Code (cf. §6).

Elsa 3 offre les fondations nécessaires à notre cible : pipelines d'exécution extensibles, DI modulaire (`IModule`/`IFeature`), support multi-tenant partiel (`TenantId` propagé), provider PostgreSQL EF Core, squelette External Task via `RunTask`/`ITaskDispatcher`.

**Trois gaps critiques** vis-à-vis des contraintes non-négociables CLAUDE.md §3 :

1. **Pas de parseur BPMN XML natif** → à implémenter entièrement (whitelist + validation).
2. **External Task incomplet** → `jobKey` UUID stable, `idempotency_keys`, heartbeat, lock TTL absents.
3. **Multi-tenant sans query filter global** → risque de fuite cross-tenant si non corrigé avant la préprod.

Périmètre Sprint 1-2 recentré : cartographie, setup Docker Compose opérationnel avec les services déjà fournis par le fork (Postgres + Redis), CI/CD initial, et **un ADR cadre** fixant la stratégie BPMN + External Task + multi-tenant.

## 2. Cartographie du repo

```
elsa-core-mdw/
├── CLAUDE.md                         ← guide Claude Code (ajouté par nous)
├── CONTRIBUTING.md
├── Directory.Build.props             ← props globales (.NET 8/9/10 multi-targeting)
├── Directory.Packages.props          ← CPM (Central Package Management)
├── Elsa.sln
├── build/, build.sh, build.cmd, build.ps1
├── docker/
│   ├── docker-compose.yml            ← postgres, sqlserver, mysql, oracle, mongodb,
│   │                                    rabbitmq, redis, smtp4dev
│   ├── ElsaServer.Dockerfile, ElsaStudio.Dockerfile, ...
│   └── init-db-postgres.sh
├── doc/
│   ├── adr/                          ← 10 ADRs existants (0001–0010) — convention EN PLACE
│   ├── changelogs/
│   ├── qa/, agent-logs/, bounty/
│   └── discovery.md                  ← CE FICHIER
├── src/
│   ├── apps/                         ← Elsa.Server.Web, Elsa.ModularServer.Web,
│   │                                    Elsa.Server.LoadBalancer, Elsa.SamplePackage
│   ├── clients/Elsa.Api.Client
│   ├── common/                       ← Elsa.Features, Elsa.Mediator, Elsa.Api.Common,
│   │                                    Elsa.Testing.Shared[.Component|.Integration]
│   ├── extensions/Elsa.Testing.Extensions
│   └── modules/                      ← 37 modules (cf. §4 pour les points d'extension)
└── test/
    ├── unit/ (13 projets)
    ├── integration/ (8 projets)
    ├── component/ (1 projet)
    └── performance/ (1 projet)
```

## 3. Stack réellement constatée vs CLAUDE.md

| Item | CLAUDE.md déclare | Fork réel | Action |
| --- | --- | --- | --- |
| Runtime .NET | .NET 9 | **net8.0 ; net9.0 ; net10.0** multi-target (source) ; **net10.0** (tests) | **Corriger CLAUDE.md §5** (runtime par défaut = 10) ou aligner |
| EF Core | PG 16 | PG + SQL Server + MySQL + Oracle + SQLite + MongoDB provider | PG reste la cible, garder support SQL Server ouvert comme alternatif |
| Dossier ADR | `docs/adr/NNNN-*.md` | `doc/adr/NNNN-*.md` (pluriel absent, 10 ADRs déjà présents) | **Aligner CLAUDE.md + `/adr` sur `doc/adr/`** — le numéro à attribuer est **0011** |
| Docker Compose | à créer | `docker/docker-compose.yml` existe (7 services) | Réutiliser, ajouter Keycloak/Azurite/OTel selon besoin |
| CI/CD | à confirmer | GitHub Actions actif (`packages.yml`) | Poursuivre sur GitHub Actions |

## 4. Points d'extension Elsa (synthèse)

| Capacité | Verdict | Chemins clés |
| --- | --- | --- |
| **Activités (`IActivity`)** | Natif, prêt à étendre | `src/modules/Elsa.Workflows.Core/Contracts/IActivity.cs:8`, `Abstractions/Activity.cs:17` ; provider auto-discovery via `[Activity(...)]` |
| **Pipelines middleware** | Natif, prêt à étendre | `WorkflowsFeature.cs:82-89` — 2 pipelines : `IWorkflowExecutionPipeline`, `IActivityExecutionPipeline` |
| **Modules / Features** | Natif, prêt à étendre | `src/common/Elsa.Features/Services/IModule.cs:9`, `FeatureBase.cs:10` — décorateurs `[DependsOn]`/`[DependencyOf]` |
| **DI** | Natif | Entry point `services.AddElsa(module => module.Configure<MaFeature>())` ; interfaces substituables : `IWorkflowRuntime`, `IWorkflowDispatcher`, `ITaskDispatcher`, `IIdentityGenerator`, `ICommitStateHandler` |
| **BPMN XML** | **Absent** | Aucun parseur BPMN, aucune whitelist — workflows Elsa stockés en JSON (`.elsa`) |
| **External Task** | Partiel | `RunTask.cs:18` + `ITaskReporter.cs:13` → bookmark + complétion ; mais **pas** de `jobKey` stable exposé, ni `idempotency_keys`, ni heartbeat/lock TTL |
| **Multi-tenant** | Partiel | `Elsa.Common/Entities/Entity.cs:16` (`TenantId` propagé) + `Elsa.Tenants` (`ITenantResolver`, `ITenantAccessor`, `TenantsFeature`) ; **pas de RLS ni global query filter EF** |
| **Persistence PG** | Natif | `Elsa.Persistence.EFCore.PostgreSql` — migrations jusqu'à V3_7 Runtime, JSONB sur `WorkflowState` |
| **Compensation / SAGA** | Absent | Aucune abstraction ni `CompensationHandler` — à construire via activités + bookmarks |
| **Retry / Resilience** | Minimal | Module `Elsa.Resilience[.Core]` existe, contenu limité ; `IncidentCount` dispo sur `WorkflowInstance:70` |

**Fichiers prioritaires à relire en Sprint 1-2 :**

- `src/modules/Elsa.Workflows.Core/Features/WorkflowsFeature.cs`
- `src/modules/Elsa.Workflows.Runtime/Activities/RunTask.cs`
- `src/modules/Elsa.Tenants/Features/TenantsFeature.cs`
- `src/modules/Elsa.Persistence.EFCore.Common` (base `ElsaDbContextBase` pour y greffer les global query filters)

## 5. Inventaire des tests

- **22 projets de test** ; **458 fichiers `.cs`** ; stack uniforme **xUnit + Moq + NSubstitute + FluentAssertions-friendly + coverlet**
- Tous les tests ciblent **`net10.0`** (cf. `test/Directory.Build.props`)
- Threshold coverage = **10% (ligne, total)** → faible, à durcir pour le code Mediawan custom (≥ 80 % ciblé, cf. `.claude/rules/tests.md`)
- Output coverlet en `cobertura,lcov,opencover` → consommable par GitHub Actions et Sonar
- Ventilation :
  - **Unit** (13) — Activities, Common, Expressions, Http, Mediator, Resilience.Core, Scheduling, Shells.Api, Tenants, Workflows.Core, Workflows.Management, Workflows.Runtime
  - **Integration** (8) — via `Elsa.Testing.Shared.Integration` (WebApplicationFactory)
  - **Component** (1) — `Elsa.Workflows.ComponentTests`
  - **Performance** (1) — `Elsa.Workflows.PerformanceTests`
- **Testcontainers non présent** dans le fork (mentionné dans CLAUDE.md) — à ajouter comme dépendance pour les tests d'intégration PG/Redis à venir

## 6. Divergences fork vs upstream

`remote add upstream https://github.com/elsa-workflows/elsa-core.git` a été ajouté.

- **Fork en avance de 2 commits** (purement additifs) :
  - `2673a4364` chore: add Claude Code project configuration
  - `f393a45d8` Merge PR #1
- **Upstream en avance de 3 commits** (changements mineurs) :
  - `9f8423932` Handle null activities in `RunWorkflowResultAssertions`
  - `632d1a383` Refactor shells setup and clean up NuGet package source mapping
  - `29d8a64a9` Update package versions: CShells 0.0.14, Nuplane 0.0.1

**Aucune modification custom du code Elsa côté fork** — la procédure `/sync-upstream` peut être exécutée sans conflit anticipé.

## 7. Gaps vs contraintes §3 CLAUDE.md

| # | Contrainte | Gap | Sprint cible |
| --- | --- | --- | --- |
| 1 | BPMN whitelist | Parseur XML BPMN + whitelist + validation **à construire** | 3-4 (ADR cadre dès 1-2) |
| 2 | External Task pattern | Squelette `RunTask` OK, SDK Worker complet à construire | 5-6 |
| 3 | Idempotence `jobKey` | Table `idempotency_keys`, UUID stable exposé au worker | 5-6 ou 9 |
| 4 | Compensation SAGA | Abstraction absente, dépendante du parseur BPMN (gap 1) | 7-8 |
| 5 | Partitionnement `correlationKey` | `TenantId` OK, **global query filter EF manquant**, cross-tenant possible | **1-2 (urgent)** |

## 8. Décisions de cadrage (arbitrées le 2026-04-20)

- [x] **Runtime .NET** : `net10.0` uniforme (source + tests + packages `Mediawan.Workflow.*`). CLAUDE.md §5 corrigé (déclarait .NET 9).
- [x] **Convention dossier ADR** : `doc/adr/` (aligné Elsa upstream, 10 ADR existants). CLAUDE.md §6, `/adr`, skill `adr-writer` mis à jour. **Prochain numéro = 0011**.
- [x] **Nom produit interne** : **Mediawan Workflow Platform** (namespace `Mediawan.Workflow.*` conservé).
- [x] **Base de données prod** : PostgreSQL 16 seul — pas de support SQL Server alternatif.
- [x] **Hébergement** : **on-premise VMs Mediawan** (Keycloak + RabbitMQ + Vault + PG self-hosted + Prometheus/Grafana/Loki). Pas d'Azure en Phase 1.
- [x] **CI/CD** : GitHub Actions (déjà en place dans le fork, workflow `packages.yml`).
- [x] **Auth** : Keycloak OIDC (dev + prod).
- [x] **Bus d'événements** : RabbitMQ (dev + prod) — pas de Redis Streams, pas de Kafka, pas de Service Bus.
- [x] **Secrets** : HashiCorp Vault uniquement (pas Azure Key Vault).
- [x] **Dapr** : **écarté en Phase 1**. Intégrations .NET natives via `IJobQueue`/`ISecretStore` abstraites (refacto Phase 2 si besoin).
- [x] **Politique support Elsa** : **autonomie complète** (0 contrat externe). Filet de sécurité contrat à reconsidérer si RSSI exige SLA écrit.
- [x] **Process pilote Sprint 10** : **Ingest média** (MAM/transcoding/QC/publication). Sponsor à identifier Sprint 1.
- [x] **Testcontainers** : ajout ciblé uniquement sur `Mediawan.*.IntegrationTests`. Zero impact `/sync-upstream`.
- [x] **Global query filter multi-tenant** : **Sprint 1-2 urgent** (gap sécurité RSSI). Feature Mediawan opt-in, tests Testcontainers PG.

## 8bis. Questions restantes (dépendent du sponsor / DSI)

- [ ] **Sponsor métier Ingest média** : à identifier Sprint 1 (cible production côté BU).
- [ ] **Accès DSI** : provision Vault, Keycloak realm, runners GH Actions self-hosted on-prem (si requis par DSI réseau).
- [ ] **Stratégie backup / DR** de la VM PostgreSQL prod — à formaliser en ADR avant go-live préprod (Sprint 10).

## 9. Prochaines étapes proposées

1. **Valider ce document** avec le lead .NET (questions §8).
2. **Draft plan Sprints 1-2** avec milestones (document séparé : `doc/sprint-plan-1-2.md`) — voir brouillon proposé dans la réponse Claude Code.
3. **ADR-0011 “Stratégie BPMN whitelist + External Task pour Mediawan”** — cadrage des gaps 1+2+3 avant Sprint 3-4.
4. **Correction CLAUDE.md** (runtime, dossier ADR, docker-compose existant) une fois les questions §8 arbitrées.
