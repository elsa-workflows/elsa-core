# Extensions development

Extension modules live under [`extensions/src`](../src), grouped by functionality.
Their tests and samples live under [`extensions/test`](../test) and
[`extensions/samples`](../samples). The shared
[repository inventory](../../docs/integration-program/inventory/README.md) records
projects, package IDs and activity identities. The
[integration catalog](../../docs/integration-program/research/README.md) tracks
researched candidates; a catalog entry does not establish an implementation.

Run commands from the repository root. Open [`Elsa.sln`](../../Elsa.sln) for
integrated development or use the root Extensions filter:

```sh
dotnet build Elsa.Extensions.slnf
dotnet test extensions/test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj
```

The Slack test project is an example of a focused source test selection, not a
claim of production Slack delivery. Select the owning test project for other
modules; integration tests may require the external services described by their
fixtures. The filter includes referenced Core dependencies. Product source and
test policy comes from the `Directory.*` files under this product and the shared
root policy. See [Core](../../core/docs/README.md) for backend development and
[Studio](../../studio/docs/README.md) for paired UI work.

Normal source builds retain the imported product's packing guard. The reviewed
[consolidated package proof](../../docs/integration-program/consolidated-package-proof.md)
evaluates eligible packages explicitly and verifies original archive/consumer
provenance. The [first 3.10 release policy](../../docs/adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md)
is lockstep with Core as sole 3.10+ NuGet publisher and Feedz-only publication;
source publisher cutover remains separately governed. The earlier
[Slack release-unit handoff](../../docs/integration-program/publisher-handoff.md)
is bounded historical preparation, not evidence that live publisher ownership
has changed. Later independent version streams require a separate decision.

The former [Extensions repository README](../../docs/integration-program/legacy/extensions/README.md.source)
is retained as import provenance. Its planned-provider table, old repository
paths and examples are historical claims. Use each active module's documentation
and released package metadata for availability. The [3.6](changelogs/3.6.0.md) and
[3.7](changelogs/3.7.0.md) notes remain release history. Follow the root
[contribution guide](../../CONTRIBUTING.md) for changes and contributor attribution.
