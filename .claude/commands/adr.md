---
description: Crée un nouvel ADR vierge dans doc/adr/
argument-hint: <titre-kebab-case>
---

# /adr

Crée un nouveau Architecture Decision Record :

1. Lister les ADR existants : `ls doc/adr/` pour trouver le prochain numéro (format `NNNN-titre.md`, 4 chiffres zero-padded).
2. Créer `doc/adr/<NNNN>-$ARGUMENTS.md` à partir du template du skill `adr-writer` (voir `.claude/skills/adr-writer/references/adr-template.md`).
3. Remplir **uniquement à partir d'infos confirmées par l'utilisateur** :
   - Statut (Proposé par défaut)
   - Date (YYYY-MM-DD)
   - Contexte
   - Décision
   - Conséquences (positives / négatives / neutres)
   - Alternatives rejetées (avec raison explicite)
4. Demander les éléments manquants, ne pas inventer.
5. Laisser le statut en `Proposé` jusqu'à validation explicite humaine.

Exemple : `/adr choix-postgresql-vs-sqlserver`
