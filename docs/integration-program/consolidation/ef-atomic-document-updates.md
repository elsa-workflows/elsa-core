# EF atomic document update validation

Program #8194, provider correction #8300, caller acceptance #8292.

The SQLite-backed BPMN caller regression found two faults in the existing EF compare-and-swap implementation. An untracked query lost the shadow `Data` column before deserializing workflow options, variables and custom properties. The caller's full snapshot comparison consequently rejected an unchanged persisted workflow. Once that was corrected, a successful same-row update still dropped notification-handler changes to the name and description columns.

The store now tracks the initial load to retain shadow values, captures the expected snapshot, and detaches the entity before invoking the update callback. Detaching prevents automatic change tracking from saving callback mutations to the original published row while a new draft is inserted. Conditional same-row updates write name and description alongside graph and serialized metadata; those columns were already part of the expected-state predicate.

The caller tests use a real SQLite file, separate service scopes, the actual EF store and a fresh scope to verify persisted results. A shared pause wrapper delegates to the underlying store's atomic operation after a competing writer finishes. Four cases cover concurrent document or metadata edits against draft or published definitions. The losing caller receives the existing precondition failure; the winning document retains handler name, options and variables, and published definitions produce a new draft version.

On 2026-09-23, the first SQLite document case failed before the shadow-state correction; all eight then-existing caller cases passed afterward. Extending coverage to notification handlers reproduced a dropped-name failure (three cases passed, one failed). After correcting the conditional setters, the complete BPMN Interchange integration suite passed **127 tests, zero failures, zero skips** on net10.0.

The EF provider also built for net8.0, net9.0 and net10.0 with zero errors (30 existing warnings).

Reproduce with:

```sh
dotnet test test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Elsa.Bpmn.Interchange.IntegrationTests.csproj -f net10.0 -m:1
```

The synthetic database uses `EnsureCreatedAsync`; this is persistence-backed caller evidence, not a production migration or upgrade proof. The pause reproduces a write between the caller snapshot and the store's atomic load, not simultaneous SQL transactions. Other database providers, broader provider contract conformance and final consolidated-source validation retain their separate acceptance gates. No packages are published by this proof.
