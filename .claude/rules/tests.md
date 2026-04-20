---
paths:
  - "test/**"
  - "**/*.Tests/**"
  - "**/*.Tests.csproj"
  - "**/*Tests.cs"
---

# Règles de tests

## Stack

- xUnit + FluentAssertions
- Testcontainers pour l'intégration PostgreSQL / Redis / Azure Service Bus
- Pas de mock sur les tests d'intégration BD — Testcontainers obligatoire

## Guardrails

- Pas de `Thread.Sleep` ni `Task.Delay` sans commentaire justifiant (timing forcé = smell)
- Pas de `.Result` ni `.Wait()` — async tout du long
- Un test = un comportement. Pas de tests "fourre-tout".
- Nommage : `Methode_Condition_ResultatAttendu` (ex: `ValidateBpmn_WithUnknownElement_ThrowsValidationException`)
- Arrange / Act / Assert visibles (commentaires `// Arrange` etc. bienvenus sur les tests non triviaux)

## Avant de merger

- Tous les tests passent (unit + integration)
- Couverture du code custom Mediawan ≥ 80%
- Pas de test `[Skip]` sans ticket lié dans le message
- Pas de test flaky toléré : quarantaine immédiate + ticket
