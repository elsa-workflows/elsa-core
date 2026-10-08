# Output converter discovery is server-owned

Core owns the Converter Descriptor registry, and Elsa's API exposes descriptors filterable by declared source and Destination Type. Descriptors include a stable ID, compatible types, localizable display metadata, and an optional JSON Schema for settings; Studio and other clients consume this catalog instead of hard-coding converter implementations.

Workflow JSON persists only an optional `converter` object containing `id` and JSON `settings`; it never persists CLR types, descriptors, instances, or display metadata. Core ships the infrastructure and a reference converter in tests or a sample, while production modules register converters whose semantics they own instead of Core providing a broad coercion catalog.
