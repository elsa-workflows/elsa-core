---
description: Lance la séquence de découverte décrite au §8 de CLAUDE.md
argument-hint: "[focus:<zone>]"
---

# /discovery

Exécute la séquence d'onboarding du projet (§8 CLAUDE.md) :

1. Tour complet du repo : `README`, `CONTRIBUTING`, structure des dossiers, projets `.csproj`, dépendances principales.
2. Identifier les points d'extension Elsa 3 (`IActivity`, `IMiddleware`, `IModule`, `IFeature`, DI).
3. Tests existants + estimation du niveau de couverture (outil de coverage si présent).
4. Divergences fork vs upstream : `git log upstream/main..HEAD --oneline` (après `git remote add upstream …` si nécessaire).
5. Produire `docs/discovery.md` — cartographie + points d'extension + questions ouvertes.
6. Proposer un plan détaillé Sprints 1-2 avec milestones mesurables.
7. **STOP et attendre validation** avant tout commit de code.

Arguments optionnels : `$ARGUMENTS` (ex : `focus:multi-tenant` pour zoomer une partie).

> Utilise l'agent `elsa-explorer` pour la phase 2 (points d'extension) afin d'isoler le contexte de recherche.
