---
name: adr-writer
description: Utilise ce skill quand tu dois rédiger un Architecture Decision Record pour ce projet. Déclenche sur "ADR", "decision record", "docs/adr", ou toute question architecture structurante (choix de techno, changement de pattern, breaking change, ajout de dépendance externe).
---

# adr-writer

## Quand ce skill s'applique

- L'utilisateur demande explicitement un ADR.
- Tu t'apprêtes à prendre une décision structurante (dépendance nouvelle, pattern d'extension, rupture d'API).
- Un ADR est **obligatoire** avant toute modif du code Elsa core (voir `.claude/rules/elsa-core.md`).

## Procédure

1. **Numéroter** : `ls docs/adr/` pour trouver le prochain `NNNN` (4 chiffres, zero-padded). Si le dossier n'existe pas, le créer.
2. **Créer** `docs/adr/NNNN-<titre-kebab>.md` à partir de `references/adr-template.md`.
3. **Remplir uniquement à partir d'infos confirmées par l'utilisateur.** Ne pas inventer le contexte ou les alternatives.
4. Laisser le statut en `Proposé` jusqu'à validation humaine explicite.

## Gotchas

- **Ne jamais écraser** un ADR existant : toujours incrémenter le numéro.
- Un ADR accepté **ne se modifie plus**. S'il devient obsolète, créer un nouvel ADR qui le remplace, et passer l'ancien en statut `Remplacé par ADR-XXXX`.
- Les **alternatives rejetées ne sont pas optionnelles** — elles documentent *pourquoi* on n'a pas choisi autre chose. C'est le cœur de la valeur d'un ADR.
- Pas de jargon interne non expliqué : un nouvel arrivant doit pouvoir lire l'ADR isolément.

## Références

- [`references/adr-template.md`](references/adr-template.md) — template à copier tel quel
