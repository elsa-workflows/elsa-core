---
paths:
  - "**/Mediawan.*/**"
  - "src/Mediawan.*/**"
  - "src/modules/Mediawan.*/**"
---

# Règles pour le code custom Mediawan

Ces modules doivent rester **isolables** : suppression du dossier = fork redevient Elsa upstream pur. Pas de couplage sauvage.

## Conventions

- Namespace : `Mediawan.Workflow.*`
- Nullable reference types : `enable`
- Logging : `ILogger<T>` structuré, TraceId OpenTelemetry propagé
- DI : tout via `IServiceCollection`, jamais de singleton statique
- Async all the way : pas de `.Result` ni `.Wait()`

## Contraintes archi non-négociables (cf. CLAUDE.md §3)

Tout code ici doit respecter :

1. **BPMN whitelist** au déploiement (refus explicite sinon)
2. **External Task pattern** par défaut (pas de code métier in-process)
3. **Idempotence** par `jobKey` (table `idempotency_keys`, TTL ≥ 30 jours)
4. **Compensation** BPMN, pas de rollback distribué
5. **Partitionnement** par `correlationKey` métier, jamais cross-tenant

Si une tâche semble les contredire → **stoppe et demande arbitrage**. Invoque l'agent `bpmn-reviewer` en cas de doute.

## Secrets

Jamais en clair. Référence par nom, résolution via coffre (Vault / Azure Key Vault). Aucun secret ne doit apparaître dans le XML BPMN ou dans un fichier de config committé.
