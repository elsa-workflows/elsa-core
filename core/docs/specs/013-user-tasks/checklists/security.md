# Security Checklist

- [x] SEC001 Authentication is host-owned and identity storage is decoupled.
- [x] SEC002 Permission and task-relationship checks are both required.
- [x] SEC003 Tenant scope is part of participant identity and every query boundary.
- [x] SEC004 Candidate, released-user, terminal-history, and manager disclosure rules are specified.
- [x] SEC005 Exclusions and manager override reason/audit behavior are specified.
- [x] SEC006 Protected payloads are bounded, excluded from search/audit, and purged consistently.
- [x] SEC007 Invitation secrets are hashed, delivery retry material is encrypted and transient, and anonymous errors are generic/rate-limited.
- [x] SEC008 Guest sessions are task-scoped, revocable, capability-limited, and bounded.
- [x] SEC009 Mutation concurrency, operation idempotency, and terminal races are specified.
- [x] SEC010 List authorization prevents row and count leakage.
