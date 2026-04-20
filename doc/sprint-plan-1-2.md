# Plan Sprints 1-2 — Exploration & bases

> **Durée** : 2 semaines (10 jours ouvrés).
> **Équipe** : 4-5 ingénieurs (1 lead .NET, 3-4 dev).
> **Objectif sprint** : fondations techniques opérables + 1 ADR cadre validé.
> **Statut** : proposition initiale — à valider avec le sponsor et le lead .NET avant lancement.

## Pré-requis (avant J0)

- [x] Arbitrage des questions §8 `doc/discovery.md` (décidé 2026-04-20 — voir ADR-0011 pour la stratégie BPMN + External Task + Idempotence)
- [ ] Identification du sponsor métier Ingest média (cible Sprint 1)
- [ ] Validation accès DSI Mediawan : provision Keycloak realm, Vault instance, runners GH Actions (self-hosted si exigé)

## Milestones mesurables

| # | Milestone | Critère d'acceptation | Jour |
| --- | --- | --- | --- |
| M1 | `doc/discovery.md` signé | Lead .NET + sponsor ont commenté/validé | **J1** |
| M2 | Questions §8 arbitrées | Les 11 cases ont une décision écrite dans un ADR ou dans `doc/discovery.md` | **J2** |
| M3 | Docker Compose `dev` local opérationnel | `docker compose up` → postgres 16 + redis 7 + Elsa.Server.Web joignable ; requête GET `/workflows` répond 200 | **J4** |
| M4 | CI GitHub Actions build + tests unitaires | Badge vert sur `main`, temps build < 10 min | **J5** |
| M5 | Global query filter multi-tenant en place | Test d'intégration prouvant qu'une requête cross-tenant retourne 0 row ; ADR-0011 bis publié | **J7** |
| M6 | `ADR-0011 Stratégie BPMN + External Task + Idempotence` | ADR validé par lead .NET, statut `Accepté` | **J8** |
| M7 | `ARCHITECTURE.md` + `CONVENTIONS.md` rédigés | PR mergée, référencés depuis `CLAUDE.md` | **J9** |
| M8 | Scan SAST dans la CI | Au moins 1 run complet (SonarCloud ou équivalent) visible sur une PR | **J10** |

## Découpage par sprint

### Sprint 1 (J1-J5) — Cartographie & infra dev

**Focus** : tout le monde sait lancer le projet localement, la CI tourne, la convention ADR est claire.

| Epic | Tâches | Owner | Estim |
| --- | --- | --- | --- |
| **E1.1 Onboarding** | Revue `doc/discovery.md` + `CLAUDE.md`, arbitrages §8, mise à jour CLAUDE.md (runtime réel, `doc/adr/`, docker existant), README onboarding dédié Mediawan | Lead | 0.5j |
| **E1.2 Docker Compose dev** | Dériver `docker/docker-compose.yml` upstream → `docker/docker-compose.dev.yml` : **PG 16 + Redis 7 + RabbitMQ (management) + Keycloak + Vault (dev mode) + smtp4dev + Elsa.Server.Web**. Realm Keycloak pré-provisionné `mediawan-workflow`. Script `scripts/dev-up.sh` + `scripts/dev-down.sh`. Validation manuelle J4. | Dev #1 | 2.5j |
| **E1.3 CI GitHub Actions** | Workflow `.github/workflows/ci-mediawan.yml` : build + tests unitaires + upload coverage. Cache NuGet. Badge dans README. | Dev #2 | 1.5j |
| **E1.4 Pré-checks sécurité** | GitHub Dependabot activé, détection secrets (gitleaks ou TruffleHog), revue des 10 ADR existants | Dev #3 | 1j |
| **E1.5 Convention docs** | Aligner CLAUDE.md + `.claude/commands/adr.md` + `.claude/skills/adr-writer/` sur `doc/adr/`. Éditer le numéro de départ (0011). | Dev #3 | 0.5j |

**Livrables Sprint 1** : M1, M2, M3, M4 + premier commit convention aligné.

### Sprint 2 (J6-J10) — Multi-tenant, ADR cadre, docs

**Focus** : verrouiller la sécurité multi-tenant avant de produire du code, formaliser la stratégie BPMN / External Task.

| Epic | Tâches | Owner | Estim |
| --- | --- | --- | --- |
| **E2.1 Global query filter tenant** | Identifier les `DbContext` Elsa (`ElsaDbContextBase`) ; ajouter `HasQueryFilter(e => e.TenantId == _currentTenant)` via `IEntityTypeConfiguration` custom enregistré dans une feature Mediawan. Tests d'intégration dédiés (Testcontainers PG). | Lead + Dev #1 | 3j |
| **E2.2 ADR-0011 BPMN + External Task + Idempotence** | Draft ADR : stratégie parseur BPMN (librairie tierce vs maison), format `jobKey` (UUID v7), schéma `idempotency_keys`, positionnement vis-à-vis de `RunTask`/`ITaskDispatcher`. Revue asynchrone. | Lead | 2j |
| **E2.3 `ARCHITECTURE.md` + `CONVENTIONS.md`** | Vue d'ensemble : modules, pipelines, pattern d'extension. Conventions : branching, commits, nullable, tests, logging structuré, observabilité. | Dev #2 + Dev #3 | 2j |
| **E2.4 Scan SAST CI** | Intégrer Sonar Cloud (ou GitHub CodeQL) dans le workflow CI — premier run, baseline de dette. | Dev #4 | 1.5j |
| **E2.5 Synchro upstream N°1** | Exécuter `/sync-upstream` avec les 3 commits Elsa en retard (cf. discovery §6). Valider tests verts, PR `upstream-sync`. | Dev #1 | 1j |

**Livrables Sprint 2** : M5, M6, M7, M8 + branche `elsa-sync` merged.

## Capacité & buffer

- **Capacité brute** : 5 dev × 10j × 0.8 (meetings/imprévus) = **40 jours·homme**
- **Estimation tâches ci-dessus** : ~18 jours·homme
- **Buffer** : ~22 j·h → couvre imprévus infra (DSI, accès Vault, droits Azure AD), exploration complémentaire, premier pairing Claude Code <-> dev Mediawan

## Risques identifiés

| Risque | Proba | Impact | Mitigation |
| --- | --- | --- | --- |
| Accès DSI (Vault, Azure AD) pas obtenu à temps | Moyenne | Bloque Sprints 3-4 (auth OIDC) | Déposer la demande au J1, escalade sponsor si pas de réponse J5 |
| Divergence récurrente avec upstream Elsa | Faible aujourd'hui | Croît avec chaque modif custom core | Règle : modifs custom uniquement dans `Mediawan.*` (cf. `.claude/rules/elsa-core.md`), ADR obligatoire sinon |
| Global query filter casse des features Elsa existantes | Moyenne | Régressions tests intégration | Mettre le filter **opt-in** via une feature flag dans le module Mediawan ; tester sur la suite xUnit complète avant merge |
| Décision tardive Dapr / SDK Worker | Moyenne | Retarde Sprint 5-6 | Le trancher au plus tard à la revue Sprint 2 (ADR dédié) |

## Hors scope Sprints 1-2

- Implémentation du parseur BPMN (Sprint 3-4 minimum)
- SDK Worker External Task (Sprint 5-6)
- Connecteurs built-in (Sprint 7-8)
- Audit log append-only + Grafana (Sprint 9)
- Process métier pilote (Sprint 10)
- Kubernetes (Phase 2)

## Checkpoints

- **Daily** (15 min, 9:30) : tour de table équipe
- **Mid-sprint review** (J5, 1h) : démo M3/M4, replanif Sprint 2 si dérive
- **Sprint review** (J10, 1h30) : démo livrables avec sponsor
- **Retro** (J10, 45 min) : après la review
