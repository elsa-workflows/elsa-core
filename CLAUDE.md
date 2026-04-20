# Mediawan Workflow Platform — Guide Claude Code

> Ce fichier te donne le contexte projet. Lis-le intégralement avant toute action.
> Il reste à jour — si une règle ici est ambiguë ou datée, signale-le avant d’agir.

-----
## 1. Mission

Transformer ce fork d’**Elsa Workflows 3** en plateforme de workflow d’entreprise pour **Mediawan**. Objectif : orchestrer les processus métier du groupe (ingest média, production éditoriale, back-office) avec BPMN 2.0, DMN, External Tasks, sur stack .NET alignée avec nos équipes.

Cible finale : moteur centralisé, multi-tenant, scalable à ~1000 workflow-starts/sec en Phase 2, avec plugins/connecteurs maintenus en interne.

**Nous ne réécrivons pas Elsa.** Nous l’étendons. Chaque modification custom doit pouvoir être contribuée upstream ou isolée dans un dossier `Mediawan.*` séparable.

-----
## 2. Contexte

- Fork fraîchement créé à partir de la branche stable d’Elsa 3
- Étude d’architecture de référence : `docs/architecture/workflow-engine-study.html` (à déposer dans le repo)
- Phase 1 (J0–J90) démarre : fondations techniques + 1er pilote métier
- Équipe : 4–5 ingénieurs dédiés, lead .NET senior, contexte Mediawan = .NET / Angular / SQL Server historique, tolérant PostgreSQL sur les nouveaux projets
- Règlement : RGPD + exigences DSI/RSSI Mediawan (audit trail complet, chiffrement des données personnelles, résidence UE)

-----
## 3. Contraintes architecturales non-négociables

Ces 5 décisions sont verrouillées. Si une tâche semble les contredire, stoppe et demande arbitrage — ne les contourne pas silencieusement.

1. **BPMN 2.0 restreint et validé.** Seul un sous-ensemble whitelisté d’éléments BPMN est exécutable. Validation au déploiement, refus explicite sinon. Palette du modeleur alignée sur cette whitelist.
1. **External Task pattern par défaut.** Le moteur n’exécute pas de code métier en in-process. Il publie des jobs, les workers externes les poll, remontent le résultat. Les Service Tasks “inline” sont réservés aux connecteurs infrastructure (HTTP, SMTP, log).
1. **Idempotence par `jobKey` stable.** Le moteur génère un UUID par tâche, les workers l’utilisent comme clé d’idempotence sur tout appel sortant. Table `idempotency_keys` avec TTL 30 jours minimum.
1. **Compensation, pas rollback.** Implémentation SAGA via Compensation Handlers BPMN. Chaque handler de compensation doit lui-même être idempotent et retriable. Jamais de “rollback transactionnel distribué”.
1. **Partitionnement par `correlationKey` métier.** Toute instance porte une clé de corrélation métier (ex : `customerId`, `dossierId`). Scaling horizontal par partitions, jamais de cross-tenant.

**Règles opérationnelles complémentaires :**

- Multi-tenant dès le jour 1 : `tenantId` en clé composite dans chaque table, requêtes admin incapables de cross-tenant
- Audit log append-only : chaque transition d’instance, chaque action opérateur, chaque évaluation DMN → row immuable
- Variables PII : taggables `@pii` dans le schéma, chiffrement champ-par-champ, purge automatique après N jours post-completion
- Retry : backoff exponentiel avec jitter (1s base, factor 2, max 5min, jitter ±25%), max 5 tentatives par défaut configurable
- Distinguer `RetriableException` (rejouer) de `BusinessException` (parker en incident)
- Versioning workflow : instances en cours terminent sur leur version d’origine par défaut ; Migration API disponible mais opt-in
- Secrets connecteurs : référencés par nom, résolus via coffre (Vault ou Azure Key Vault), jamais en clair dans le XML BPMN

-----
## 4. Livrables Phase 1 (J0–J90)

En ordre de priorité. Chaque livrable = du code mergé + tests + doc courte.

**Sprint 1-2 — Exploration & bases**

- Cartographier le fork Elsa : modules, points d’extension, tests existants
- Produire `ARCHITECTURE.md` (décisions acteées) et `CONVENTIONS.md` (code, tests, git)
- Setup local dev : Docker Compose (Elsa + PostgreSQL 16 + Redis + Angular Designer)
- CI/CD : build .NET, tests unitaires, tests d’intégration, scan sécurité SAST

**Sprint 3-4 — Multi-tenant & auth**

- Modèle multi-tenant : `tenantId` propagé partout, résolution par subdomain ou header
- OIDC (Azure AD par défaut, Keycloak en dev)
- RBAC : rôles `admin`, `designer`, `operator`, `observer` — policies par tenant

**Sprint 5-6 — SDK Worker External Task**

- Package NuGet `Mediawan.Workflow.Worker.Sdk`
- API : attribut `[JobHandler]` + injection de variables BPMN
- Polling long, lock avec TTL renouvelable, heartbeat
- Retry policies intégrées, distinction Retriable / Business errors

**Sprint 7-8 — Connecteurs built-in v1**

- `Http` (GET/POST/PUT/DELETE avec auth)
- `Smtp` (templates Razor)
- `S3` (AWS + Azure Blob derrière la même abstraction)
- Manifest JSON Schema par connecteur, intégration modeleur

**Sprint 9 — Audit & idempotence**

- Table `audit_log` append-only, Postgres ROW LEVEL SECURITY
- Table `idempotency_keys` + middleware de déduplication
- Dashboard Grafana v1 : taux de succès, latence p50/p95/p99 par process

**Sprint 10 — Pilote métier**

- Implémenter 1 process métier Mediawan concret (à définir avec sponsor)
- Runbook SRE : procédures incident, rollback, escalade
- Go-live en préprod

**Hors scope Phase 1 :** DMN engine complet, BPMN Migration API, CMMN, Kubernetes production (VMs OK en phase 1), connecteurs Mediawan spécifiques (MAM/DAM/Playout).

-----
## 5. Stack technique

```
Runtime         : .NET 9 / ASP.NET Core
Moteur          : Elsa Workflows 3 (fork)
Base de données : PostgreSQL 16 (variables workflow en JSONB)
Cache / locks   : Redis 7
Bus d'événements: Azure Service Bus (prod) / Redis Streams (dev)
Sidecar         : Dapr (pub/sub, state, secrets) — en évaluation
Observabilité   : OpenTelemetry → Prometheus + Grafana + Loki
Secrets         : HashiCorp Vault OU Azure Key Vault
Auth            : OIDC — Azure AD (prod) / Keycloak (dev)
API Gateway     : YARP
Modeleur        : Elsa Designer (Angular) + bpmn-js
CI/CD           : Azure DevOps ou GitHub Actions (à confirmer avec DSI)
```

**Justifications clés :**

- PostgreSQL plutôt que SQL Server : JSONB natif (variables workflow), LISTEN/NOTIFY pour signaling léger, pas de licence. SQL Server reste supporté comme provider alternatif mais PG est la référence de dev.
- Dapr en sidecar découplable — s’il pose problème en intégration Mediawan, on retire sans réécrire.
- Pas de Kubernetes imposé en Phase 1. VMs + Docker Compose suffisent pour le pilote. K8s en Phase 2 pour la charge réelle.

-----
## 6. Méthode de travail

**Principe général : explorer avant d’écrire.** Ne jamais créer une classe sans avoir cherché dans le code Elsa existant si une extension est déjà possible. Les extensions upstream-compatibles sont toujours préférables aux forks internes.

**Boucle standard pour chaque tâche :**

1. **Read** — explore le code concerné, lis les tests existants, cherche les points d’extension (`IActivity`, `IMiddleware`, etc.)
1. **Plan** — produis un plan écrit avant d’implémenter. Si la tâche fait plus de 2h de code, découpe en sous-tâches explicites.
1. **Confirm** — si ambiguïté, pose la question AVANT de coder. Ne suppose pas.
1. **Implement** — petits commits atomiques, messages conventionnels (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`)
1. **Test** — chaque fonctionnalité = tests unitaires + intégration quand possible. Pas de merge sans tests passants.
1. **Document** — ADR (`docs/adr/NNNN-titre.md`) pour toute décision architecturale non triviale.

**Git workflow :**

- `main` protégée, PR obligatoires
- Branches : `feat/...`, `fix/...`, `refactor/...`, `chore/...`
- Commits signés
- Synchro régulière avec upstream Elsa (merge hebdomadaire de la branche stable Elsa dans une branche `elsa-sync`, puis merge dans `main` après validation tests)

**ADR (Architecture Decision Records) :**
Pour tout choix structurant (ajout d’une dépendance, pattern d’extension, breaking change), crée `docs/adr/NNNN-titre.md` avec format : Contexte / Décision / Conséquences / Alternatives rejetées. C’est le contrat avec les équipes futures.

-----
## 7. Conventions de code

- Style .NET : EditorConfig du fork Elsa respecté
- Nullable reference types : `enable` partout
- Tests : xUnit + FluentAssertions + Testcontainers pour l’intégration PostgreSQL
- Pas de `Thread.Sleep` ni de `Task.Delay` en prod sans justification écrite
- Async all the way : pas de `.Result` ni `.Wait()`
- Logging : `ILogger<T>` structuré avec scope, TraceId OpenTelemetry propagé
- Injection de dépendances : tout via `IServiceCollection`, pas de singletons statiques
- Nommage custom : `Mediawan.Workflow.*` pour les modules spécifiques
- XML BPMN : validation schema au déploiement, refus si éléments hors whitelist

**Guardrails stricts :**

- Jamais de secret ni de connection string en clair commité
- Jamais de modification du code Elsa “core” sans ADR et label `upstream-candidate`
- Toujours mode opt-in pour les features expérimentales (feature flags)
- Dépendances nouvelles : justification dans le PR, check licence (pas de GPL)

-----
## 8. Première action attendue

**Ne commence pas à coder.** Exécute d’abord cette séquence :

1. Fais un tour complet du repo : `README`, `CONTRIBUTING`, structure des dossiers, projets .csproj, dépendances principales
1. Identifie les points d’extension principaux d’Elsa 3 (activities, middleware, modules)
1. Repère les tests existants et leur niveau de couverture
1. Note les divergences entre la branche main du fork et l’upstream Elsa (si fork déjà touché)
1. Produis `docs/discovery.md` : cartographie du repo + points d’extension identifiés + questions ouvertes
1. Propose un plan détaillé des Sprints 1-2 (2 semaines) avec milestones mesurables
1. **Attends validation** avant de créer le premier commit de code

Le but : s’assurer que tu as une vision claire du terrain avant d’y construire.

-----
## 9. Questions ouvertes (à arbitrer tôt)

Ces points nécessitent une décision humaine. Liste-les dans tes premiers échanges :

- [ ] Nom du produit interne (ex : “MediaFlow”, “Mediawan Workflow Platform”, autre)
- [ ] Base de données cible production : PostgreSQL seul, ou PG + SQL Server supportés ?
- [ ] Hébergement cible : on-prem, Azure, hybride ?
- [ ] Fournisseur CI/CD (Azure DevOps vs GitHub Actions)
- [ ] Politique de support Elsa : contrat payant avec mainteneurs, ou autonomie complète ?
- [ ] 1er process métier pilote (à définir avec sponsor — probablement côté production éditoriale)
- [ ] Dapr : on valide ou on part sur des intégrations natives ?

-----
## 10. Contacts & références

- Étude d’architecture : `docs/architecture/workflow-engine-study.html`
- Upstream Elsa : <https://github.com/elsa-workflows/elsa-core>
- Docs Elsa : <https://v3.elsaworkflows.io>
- Spec BPMN 2.0 : OMG — <https://www.omg.org/spec/BPMN/2.0/>
- Spec DMN 1.3 : OMG — <https://www.omg.org/spec/DMN/1.3/>

-----

*Fin de fichier. Si quelque chose ici est flou, demande avant d’agir.*
