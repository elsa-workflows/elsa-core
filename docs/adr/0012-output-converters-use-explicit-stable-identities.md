# Output converters use explicit stable identities

Each Output Binding selects zero or one registered Output Converter by an ordinal, case-sensitive Converter ID; registrations that duplicate an ID or differ only by case are rejected. Converter IDs have immutable semantics, so breaking behavior, settings, or result changes require a new ID, and Core never infers a converter from a source/destination type pair.

Converters are synchronous, deterministic, and side-effect-free. They receive a narrow immutable Conversion Context containing the native value, declared types, and JSON-only settings; dependencies use constructor injection, instances resolve from the active workflow scope, and cached descriptors never retain scoped services. Source compatibility uses normal base-class and interface assignability, the declared result type must be assignable to a resolvable Destination Type, and open-generic matching is deferred.

Core validates registration, compatibility, and settings when accepting or materializing a definition and repeats safety checks at runtime. Resolution, validation, invocation, and result failures enter normal activity fault handling through a privacy-safe Output Conversion Error that preserves an originating exception but omits native values and raw settings by default.
