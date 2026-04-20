# ADR-0011 : Stratégie BPMN whitelist + External Task + Idempotence pour Mediawan Workflow Platform

- **Statut :** Proposé
- **Date :** 2026-04-20
- **Auteur·s :** O. Yahyaoui (discovery initiale + arbitrages §8)
- **Remplace :** —
- **Remplacé par :** —

## Contexte

Le fork `Fatilov/elsa-core-mdw` vise à devenir la **Mediawan Workflow Platform** (MWP). Trois gaps techniques critiques ont été identifiés lors de la phase de découverte (cf. `doc/discovery.md` §7) vis-à-vis des contraintes non-négociables de `CLAUDE.md` §3 :

1. **Pas de parseur BPMN XML natif** dans Elsa 3 — le moteur manipule des workflows en JSON (`.elsa`) via son propre schéma. La contrainte §3.1 « BPMN 2.0 restreint et validé » impose un parseur, une whitelist d'éléments, et une validation au déploiement.
2. **External Task incomplet** — `RunTask.cs:18` + `ITaskDispatcher` + `ITaskReporter.cs:13` offrent le squelette (bookmark + complétion asynchrone), mais les éléments d'exactly-once promis par §3.2 (worker externe poll/lock/heartbeat, `jobKey` UUID stable exposé, distinction `RetriableException` / `BusinessException`) sont absents.
3. **Table `idempotency_keys` absente** — la contrainte §3.3 exige une table avec TTL ≥ 30 jours utilisée par les workers comme clé d'idempotence sur tout appel sortant.

Ces trois gaps sont **liés** : la compensation SAGA (contrainte §3.4) dépend du parseur BPMN (elle repose sur les `Compensation Boundary Event` + `Compensation Activity`), et l'External Task s'appuie sur l'idempotence pour garantir l'exactly-once.

Cadrage validé lors des arbitrages §8 de la discovery :

- Runtime cible `net10.0` uniforme
- Hébergement on-premise Mediawan (Keycloak + RabbitMQ + HashiCorp Vault)
- Pas de Dapr en Phase 1 — intégrations natives .NET
- PostgreSQL 16 comme provider unique
- Pilote métier Sprint 10 : Ingest média (MAM → transcoding → QC → publication) → stress-test direct du pattern External Task + SAGA

## Décision

Nous adoptons une **stratégie en trois couches** isolée dans le namespace `Mediawan.Workflow.*`, implémentée progressivement sur les Sprints 3-9.

### 1. Parseur BPMN XML + whitelist — Sprint 3-4

**Librairie tierce retenue : [`Camunda.Bpmn.Model`](https://www.nuget.org/packages/Camunda.Bpmn.Model) (alt: `SBpmnEngine` / `Bpmn.Net`)** pour le parsing XML (évaluation comparative à finaliser avant la fin du Sprint 3). Le mapping XML → activités Elsa (`IActivity`) et la whitelist sont **écrits en interne** dans `Mediawan.Workflow.Bpmn`.

Éléments BPMN **whitelistés en Phase 1** (sous-ensemble conservateur) :

| Catégorie | Éléments autorisés |
| --- | --- |
| Events | `StartEvent` (simple + timer + message), `EndEvent` (simple + terminate), `IntermediateCatchEvent` (timer, signal, message), `CompensationBoundaryEvent` |
| Activities | `UserTask`, `ServiceTask` (externalTopic uniquement), `ScriptTask` (strictement limité aux connecteurs infra HTTP/SMTP/log), `CompensationActivity`, `CallActivity` |
| Gateways | `ExclusiveGateway`, `ParallelGateway`, `EventBasedGateway` |
| Structurels | `SequenceFlow`, `Process`, `Pool`, `Lane` |

Rejetés (erreur explicite au déploiement) : `BusinessRuleTask` (DMN hors scope Phase 1), `InclusiveGateway` (sémantique ambiguë), `ComplexGateway`, `SubProcess` non-embedded, `AdHocSubProcess`, `Transaction`, scripts inline autre que whitelistés.

Validation au déploiement :

- Schema XSD BPMN 2.0 (officiel OMG)
- Validation whitelist custom `BpmnWhitelistValidator`
- Palette du modeleur `bpmn-js` côté Angular Designer alignée via un profil MWP

### 2. Pattern External Task + `jobKey` — Sprint 5-6

Un nouveau module `Mediawan.Workflow.Worker.Sdk` (package NuGet public interne) encapsule :

- **`[JobHandler("topic-name")]`** — attribut pour enregistrer un handler
- **Injection automatique** des variables BPMN typées + `JobContext`
- **Polling long** sur RabbitMQ (un exchange par topic, queues partitionnées par `correlationKey`)
- **Lock TTL renouvelable** (Redis, base 5 min, heartbeat toutes les 30 s)
- **Génération `jobKey` UUID v7** côté moteur (horodaté, partitionné), propagé dans le message AMQP en header `x-mediawan-job-key`
- **Retry policies** intégrées : backoff exponentiel + jitter (1 s base, factor 2, max 5 min, ±25 %), max 5 tentatives configurables
- **Distinction typée** `RetriableException` (rejouer) vs `BusinessException` (parker en incident — `IncidentCount` incrémenté sur `WorkflowInstance`)

Le moteur Elsa pousse dans RabbitMQ via un **bridge** implémentant `ITaskDispatcher` → `IJobQueue`. La réponse remonte via `ITaskReporter.ReportCompletionAsync` côté moteur. Aucune modif du code Elsa core — tout est greffé via `IModule`/`IFeature` Mediawan.

### 3. Table `idempotency_keys` + middleware — Sprint 9 (anticipation en Sprint 5-6 pour le SDK)

Schéma PostgreSQL :

```sql
CREATE TABLE idempotency_keys (
    key         uuid NOT NULL,
    tenant_id   text NOT NULL,
    topic       text NOT NULL,
    first_seen  timestamptz NOT NULL DEFAULT now(),
    last_seen   timestamptz NOT NULL DEFAULT now(),
    response    jsonb,
    status      smallint NOT NULL,   -- 0=InFlight, 1=Succeeded, 2=Failed
    expires_at  timestamptz NOT NULL DEFAULT now() + interval '30 days',
    PRIMARY KEY (tenant_id, key)
) PARTITION BY HASH (tenant_id);

CREATE INDEX idx_idempotency_keys_expires ON idempotency_keys (expires_at);
```

Middleware côté SDK Worker intercepte tout appel sortant HTTP/AMQP/DB, insère/vérifie la ligne avant exécution. Purge automatique via un `IHostedService` qui tourne toutes les 6 h.

TTL 30 jours (minimum CLAUDE.md §3.3), configurable par connecteur si nécessaire (ex: S3 multipart upload peut exiger 7 jours supplémentaires).

## Conséquences

### Positives

- **Respect explicite des 3 contraintes §3.1 / §3.2 / §3.3** — traçable, ADR-sourcé.
- **Isolation Mediawan** : tout vit dans `Mediawan.Workflow.Bpmn` + `Mediawan.Workflow.Worker.Sdk` + `Mediawan.Workflow.Idempotency`. Pas de patch sur le code Elsa core — les PR upstream-candidate restent minimales.
- **SDK Worker** public en interne → réutilisable par toutes les BU Mediawan (Ingest média, Production éditoriale, Back-office).
- **Progressivité** : on peut mettre en production Sprint 5-6 sans attendre le parseur BPMN complet (workflows codés directement en C# en interne).
- **Observabilité** : chaque étape du flow est loggée avec `TraceId` OTel, exportée vers Loki.

### Négatives

- **Charge d'implémentation élevée** : ~30 j·h estimés sur Sprints 3-9 (parseur + SDK + idempotence + tests).
- **Dépendance à une librairie BPMN tierce** : nécessite un wrapper pour isoler les types et permettre un remplacement ultérieur si nécessaire.
- **Divergence durable avec Elsa upstream** : les `JobHandler`s Mediawan ne s'interopèrent pas avec les activités C# Elsa traditionnelles. C'est assumé (contrainte §3.2 exige External Task par défaut).
- **Complexité de debug** : un workflow qui échoue traverse `moteur → RabbitMQ → worker → idempotency → handler → RabbitMQ retour → moteur`. Nécessite des outils de corrélation (TraceId, `jobKey`, `correlationKey`) dès le Sprint 5-6.
- **PostgreSQL partitionné** : la table `idempotency_keys` en partitioning par `tenant_id` impose une connaissance EF Core avancée ou un recours à `.ExecuteSqlRaw`.

### Neutres

- **Phase 2 possible avec Dapr** : `IJobQueue`/`ISecretStore` sont des abstractions, swap cible si besoin.
- **Migration API** (contrainte opérationnelle sur le versioning) est hors scope de cet ADR — sera traitée dans un ADR dédié Sprint 8+.
- **DMN engine complet** explicitement hors scope Phase 1. Si un besoin métier apparaît, évaluer `Camunda.Dmn` ou une grille Excel en fallback.

## Alternatives rejetées

### A. Adopter Camunda Platform 8 / Zeebe à la place d'Elsa

Description : abandonner le fork Elsa et standardiser sur Camunda 8 (BPMN/DMN first-class, External Task natif, parseur XML inclus, compensation SAGA native, clustering multi-tenant).

**Raison du rejet :** la stack Mediawan est .NET-first, l'équipe est dimensionnée pour du C#, et Camunda 8 impose un écosystème Java + Zeebe avec un coût de licence/run. La décision de partir d'Elsa était déjà prise en amont (cf. `CLAUDE.md §1` : « Nous ne réécrivons pas Elsa. Nous l'étendons »). L'investissement pour combler les 3 gaps est modéré (~30 j·h) face au coût de migration.

### B. Implémenter tout en in-process (pas d'External Task)

Description : les workflows exécutent directement des activités C# Elsa (`IActivity`), sans publication RabbitMQ. Plus simple, plus rapide à démarrer.

**Raison du rejet :** viole directement la contrainte §3.2 (External Task par défaut). Sans External Task, le moteur devient un monolithe bloquant, impossible à scaler à 1000 starts/s en Phase 2, et le code métier des BU n'est pas isolé (risque de crash global).

### C. Utiliser Dapr sidecar pour pub/sub + state + secrets

Description : Dapr prend en charge l'abstraction broker/state/secret, évite d'écrire `IJobQueue` / `ISecretStore` à la main.

**Raison du rejet :** décision §8 de la discovery — Dapr est écarté en Phase 1 pour limiter la complexité ops (+1 sidecar par pod, +1 courbe apprentissage équipe, +1 version à suivre). Les abstractions sont conservées (`IJobQueue`) pour permettre une bascule Dapr en Phase 2 si besoin.

### D. Parseur BPMN maison from scratch

Description : écrire nous-mêmes le parseur XML BPMN 2.0 à partir du schéma XSD OMG.

**Raison du rejet :** BPMN 2.0 = ~200 pages de spec, ~100 éléments, nombreux cas limites (sub-process, compensation scope, multi-instance…). Le coût (~10-15 j·h) est trop élevé vs l'utilisation d'une librairie tierce éprouvée (~2-3 j·h pour intégration). Si la librairie tierce pose problème plus tard, on pourra isoler son usage et la remplacer grâce au wrapper MWP.

## Liens

- `doc/discovery.md` — découverte initiale, §7 (gaps) et §8 (arbitrages)
- `doc/sprint-plan-1-2.md` — plan Sprints 1-2, E2.2 référence cet ADR
- `CLAUDE.md` §3 — contraintes architecturales non-négociables
- Référence BPMN 2.0 : [OMG Spec](https://www.omg.org/spec/BPMN/2.0/)
- Librairie candidate : [`Camunda.Bpmn.Model`](https://www.nuget.org/packages/Camunda.Bpmn.Model)
