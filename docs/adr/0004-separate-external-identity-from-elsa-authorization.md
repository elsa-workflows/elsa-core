# Separate external identity from Elsa authorization

**Status**: V1 permission-mapping portion superseded by [0009](0009-match-unlinked-identities-with-trusted-user-matchers.md)

External authentication produces a protocol-neutral identity that links to a distinct, possibly credential-less Elsa User before Elsa issues credentials. Links are keyed by target tenant, immutable Connection ID, validated issuer namespace, and provider-stable subject, never by email or user name; an extensible Unlinked Identity Policy handles missing links. Elsa composes its open string permission vocabulary through configured Permission Grant Sources, with provider claims requiring explicit mappings or boundaries and optional non-authoritative Permission Descriptors used only for authoring.
