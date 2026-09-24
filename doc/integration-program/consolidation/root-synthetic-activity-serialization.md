# Root activity synthetic properties

Task #8320 was identified by the generated activity comparison under #8313. Both released Elsa 3.8.4 and the source rehearsal omit generated Agent inputs when serializing the activity directly, while a Sequence containing the same instance preserves them. The original nonzero comparison evidence remains unchanged.

`IActivitySerializer.Serialize(IActivity)` now includes synthetic inputs and outputs from the registered descriptor. The object overload applies the same activity behavior when its value implements `IActivity`. Register the generated descriptor before serialization, as is already required to reconstruct the generated activity during deserialization.

Ordinary CLR properties still use System.Text.Json and its existing options, aliases, property converters and ignore conditions. Static activity roots and ordinary objects retain their previous serialization path. Only descriptors declaring synthetic inputs or outputs add synthetic fields through the existing synthetic-property writer; descriptor-specific serializer options apply to those generated roots.

The regression constructs a Core-only generated descriptor with a literal input and variable-backed output, verifies direct/object overloads and Sequence-contained inputs, and checks type/version/ID and output memory-reference preservation. Additional regressions cover aliases, omitted nulls and conditional ignore attributes for both generated and ordinary roots. The original regression failed in all four generated cases before the correction; independent static/object controls passed.

This corrects serialization of values still present in memory. It cannot reconstruct inputs already lost from previously persisted JSON. It does not resolve the separate GitHub activity Id-shadow failures, establish live Agent execution, or authorize publishing a release.
