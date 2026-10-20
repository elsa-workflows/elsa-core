# Design review record

20 October 2026, documentation-only review for #8896. The program lead integrated three independent, read-only reviews of the working packet against source baseline `df0697fd84e8036db71b6f5740e5df23c7eb517e`. Reviewers used `gpt-6-luna` with extra-high reasoning; integration and final QA remained with the lead in this session. This is technical design review, not product-owner acceptance, an external GitHub approval, or runtime qualification. The PR history records its exact candidate head and external review/check state.

| Review lens | Material finding | Integrated disposition |
|---|---|---|
| Runtime/persistence | Unregistered CLR envelopes can lose type identity at restart | Explicit versioned map codec; exact round trip and unknown-schema preservation in E1/V8. |
| Runtime/persistence | Normal extraction can omit unresolved work/callbacks | Fail-closed restore admission against pinned refs before omission; missing-node/owner/child and migration controls. |
| Runtime/persistence | First ambiguous prepare commit needs an identity recovery rule | Persisted scope seed/cursor allocator and committed-slot lookup; no new operation identity while effect may exist. |
| Studio/security | Protected journal could still leak through generic outputs/details | Fail-closed output classification, source suppression/redaction and protected projection drill-down; E2 negative controls. |
| Studio | Current publication errors have no per-error stable targets | Additive code/message/target/relatedIds contract and older-server message fallback. |
| Studio | Request snapshots and returned outputs could be confused | Separate pre-effect request capture and post-result capture; lost response leaves result Unknown. |
| Studio/accessibility | Mixed status dimensions and ambiguous reorder keys | Separate scope/forward/obligation/attempt/availability dimensions; named Move up/down actions. |
| Program/reference | M1 checks pulled later operator behavior forward | V5a/M1, V5b/M2, V5c/M4 and explicit command-stage mapping. |
| Program/reference | No-fault M1 path lacked confirmation while root end waits | Minimal authored exact-scope Confirm proposed for M1, positive/omitted-confirm walkthrough, wider/operator targeting later; O1 acceptance remains required. |
| Program/reference | Outcome/propagation choice was not tracked as owner decision | O3 added; O1–O3 remain Proposed. |

Each reviewer rechecked the integrated corrections. A final program re-review requested explicit omitted-confirm behavior, authored audit provenance and a precise M1/M5 V8 split; these were integrated as publication/pre-effect rejection with preserved blocked legacy state, workflow actor provenance, and V8a/V8b. The final program re-review reported no remaining material contradiction. The lead additionally clarified that M1 permits a missing handler only for declared pure/no-effect steps, and scoped shared qualification cases to their delivery stages. No production files, schema, endpoints or components changed. No .NET test, browser run, participant experiment or restart proof was performed or claimed.

Local validation checks relative packet links, pinned Core source paths, documentation-only changed paths, whitespace and generated ADR TOC. The [qualification plan](verification.md) defines future executable evidence; the [decision register](decisions.md) keeps owner and physical-proof gates open. Design review does not promote #8891 or lift any other pause.
