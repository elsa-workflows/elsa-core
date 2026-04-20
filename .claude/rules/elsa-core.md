---
paths:
  - "src/modules/**"
  - "!src/modules/Mediawan.*/**"
  - "src/common/**"
---

# Règles pour le code Elsa core

**Principe :** Ce fork suit l'upstream Elsa. Toute modification du code core doit être contribuable upstream, ou justifiée par ADR.

## Avant d'éditer un fichier ici

1. Vérifier si une extension (`IActivity`, `IMiddleware`, `IModule`, `IFeature`, `IServiceCollection`) résout le besoin sans toucher au core.
2. Si modif core inévitable :
   - Créer `docs/adr/NNNN-titre.md` (format Contexte / Décision / Conséquences / Alternatives rejetées).
   - Ajouter le label `upstream-candidate` au PR.
   - Inclure un test de non-régression sur le comportement existant.

## Interdictions

- Pas de breaking change sur les API publiques Elsa sans ADR + bump semver.
- Pas de suppression de code "mort" du core sans vérifier les usages externes (grep cross-modules).
- Pas de modif du XML BPMN parser sans invoquer l'agent `bpmn-reviewer`.

## Synchro upstream

Merge hebdomadaire de `upstream/main` dans la branche `elsa-sync`, puis intégration dans `main` via PR après validation tests. Voir `/sync-upstream`.
