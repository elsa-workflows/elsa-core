# Reconcile the imported Dependabot policies

The consolidated repository has one root Dependabot file. Keep Core's weekly NuGet update entry at `/`, and exclude `src/extensions/**` and `src/studio/**` from that scan. The imported product policies then run against their relocated manifest trees, `/src/extensions` and `/src/studio`, with the Core entry excluding those same manifests from its recursive root scan.

Both product entries retain their daily schedule, `main` target, Feedz preview registry, one-open-PR limit, and `deps` commit prefix. Extensions keeps its Elsa package allowlist and `Elsa*` group; Studio keeps its `Elsa.Api.Client` allowlist. The only ecosystem in either retained source policy is NuGet, so this disposition adds no new ecosystem. The registry credential remains `FEEDZ_API_KEY`; availability of that repository secret and Dependabot's acceptance of the final config remain review gates. The ledger rows stay pending until the decision is merged and the validator can record the reviewed PR and merge commit.

The active configuration and ledger cover exactly the relocated policy paths; the inert `.source` files remain provenance records. No publisher workflow is changed.
