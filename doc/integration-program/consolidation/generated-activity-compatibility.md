# Generated activity compatibility evidence

Related: #8313, #8320. This extends the static activity comparison with descriptors produced by the actual Agents, Orchard, MassTransit and Telnyx provider classes. It is an offline construction and serialization probe, not a claim that provider hosts or integrations execute successfully.

The retained [summary and hashed artifacts](generated-activity-compatibility-evidence/summary.json) compare released 3.8.4 packages with the same prepared source tree used by the [static probe](activity-compatibility.md): Core `076f022cc174d497af26fc8e26414970e61a79b1`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`, plus the documented build corrections. This is a rehearsal; it is not the final history import or latest-main compatibility result.

## Configured providers

| Provider | Controlled fixture | Generated descriptors |
| --- | --- | ---: |
| Agents | One `Summarize` config with string Text, integer Count, string output | 1 |
| Orchard | One CompatibilityArticle content type, expanded by the real provider | 4 |
| MassTransit | One CLR message record, real receive/publish descriptors | 2 |
| Telnyx | Actual attributed webhook payload types | 9 |

Each generated descriptor constructs its instance through its real constructor delegate. The registry includes each activity type/version; multiple generated descriptors may share a CLR type. Comparison rejects duplicate activity identities and ambiguous CLR names across assemblies. Synthetic input values are assigned/read through descriptor accessors. MassTransit message values undergo the same typed conversion used before publication, without sending a message.

The workflow is built from the original configured instances, before any individual round-trip can lose fields. It compares original and restored typed literal values as well as complete workflow JSON, identity, version and CLR contracts. This prevents an omitted input in both JSON representations from being mistaken for preserved data.

## Results and known failures

On each of .NET 8, 9 and 10:

- Released/source hosts describe 134/143 activities; the nine source-only LDAP/MQTT identities are individually listed in the matrix.
- The workflows contain 130/139 activities, including the generated Agent with both synthetic inputs. First-write and canonical historical import preserve workflow identities and typed inputs.
- Both hosts report five individual round-trip failures: the four GitHub Id-shadow cases documented by the static probe, plus generated `Elsa.Agents.AgentActivity.Summarize` losing Text/Count when serialized directly as a root activity. Its Sequence-contained form preserves those inputs. #8320 owns the root serializer defect.
- All six mutation probes reject changes to type, ID, version, literal value, first-write-only ID, and generated Agent literal input. There are 18 rejected mutation cases across the three frameworks.
- Both hosts build with zero errors; released/source warning counts are 0/92. All 7,694 tracked source-input hashes remain unchanged during each run. No activity bodies execute.

Each comparison exits **1**, retaining the baseline failures; this is not a passing full compatibility gate. No failure is allowlisted away. Ten Python comparison tests pass in standard and optimized execution. The .NET 8 receipt predates the exact-identity refinement of the source-only addition guard; its original harness hashes are preserved. .NET 9 and 10 used that refinement. A subsequent [comparison-only replay](generated-activity-compatibility-evidence/comparison-replay.json) uses the complete reviewed source-only contracts and confirms the same nine additions with no changed or unexpected contracts. Provider class names remain provenance in raw receipts; contract comparison uses activity identity, inputs, outputs, kind and ports. No further .NET execution is claimed for this Python guard refinement.

Compressed artifacts replace local filesystem prefixes only. The summary retains original, sanitized and compressed SHA-256 values. Raw assembly-qualified types remain in host outputs; contract comparison deliberately excludes dependency assembly build versions but retains assembly name, culture and public key token. This does not prove binary binding compatibility for all dependency versions.

## Reproduce and remaining scope

Use the commands in the static probe guide with this harness and a new output directory for each framework, then run `verify_probe.py` against each output. Never overwrite retained evidence or run concurrent builds against the same source output directories.

This covers one controlled Agent schema and Orchard content type, one MassTransit CLR message, and the current Telnyx attributed payloads. It does not prove arbitrary generated schemas, live provider dispatch, provider side effects, final-host DI registration, historical tenant-specific configurations, or every literal CLR shape. Actual import, package consumers and approved pilot workflows have separate gates. Future fixes need fresh results; these nonzero baseline receipts must remain intact.
