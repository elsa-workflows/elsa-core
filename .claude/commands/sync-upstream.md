---
description: Procédure de synchronisation avec upstream Elsa
---

# /sync-upstream

Synchro hebdomadaire avec `elsa-workflows/elsa-core` :

1. **Vérifier le remote `upstream`** :
   ```bash
   git remote -v | grep upstream
   ```
   Si absent :
   ```bash
   git remote add upstream https://github.com/elsa-workflows/elsa-core.git
   ```

2. **Fetch** :
   ```bash
   git fetch upstream --tags
   ```

3. **Créer / actualiser la branche `elsa-sync`** :
   ```bash
   git checkout -B elsa-sync upstream/main
   ```

4. **Merger `main` dedans** (pour voir les conflits rapidement) :
   ```bash
   git merge main
   ```

5. **Lancer build + tests complets** :
   ```bash
   dotnet build
   dotnet test
   ```

6. Si vert → rebase/merge de `elsa-sync` dans `main` via **PR dédiée** avec label `upstream-sync`.

7. Si conflits sur code Elsa core :
   - Préserver les modifs custom Mediawan (normalement isolées dans `Mediawan.*/`).
   - Si conflit dans un fichier Elsa modifié en interne → vérifier l'ADR associé, arbitrage lead .NET.
   - Invoquer l'agent `bpmn-reviewer` si la modif touche le moteur d'exécution ou le parser BPMN.

**Règles de sûreté :**
- Jamais de `git push --force` sur `main`.
- Toujours PR + review.
- Commits signés.
