# Legacy Secrets package disposition

Maintainer decisions recorded 2026-09-28 for Story #8275 and Task #8301. They settle the compatibility gate that kept
the 47 `secrets_collision_source` rows in the [retained-asset ledger](legacy-asset-dispositions.json) pending.

## Decisions

| # | Question | Decision |
|---|---|---|
| 1 | How do existing legacy Secrets values reach Core? | They are not converted. Operators re-enter or rotate each secret into Core, following the [upgrade guide](../../migrations/secrets-legacy-extensions-upgrade.md). No tool reads legacy ciphertext, and Elsa takes no custody of a customer's Data Protection key ring. The synthetic SQLite, PostgreSQL and SQL Server bridges (#8298, #8350, #8359) stay as characterization evidence with `cutoverAllowed=false`; they are not a supported upgrade path. |
| 2 | When are the legacy packages deprecated? | At the first consolidated release, 3.10.0. `Elsa.Secrets.Api`, `.Core`, `.Management`, `.Models` and `.Scripting` are marked deprecated on NuGet in favour of `Elsa.Secrets`, stay installable at their 3.8.x versions, and get no 3.10 version. Their monorepo projects are `IsPackable=false`, and `test_legacy_only_packages_are_not_packed_by_the_consolidated_publisher` fails if one becomes packable again. |
| 3 | Is #8360 the reviewed decision for #8301 criterion 5? | Yes. The legacy row-ID adapter, `Owner` mapping and plaintext `GET /secrets/{id}/input` stay unsupported and disabled ([legacy ID and Owner disposition](../secrets-collision-compatibility.md#legacy-id-and-owner-disposition)). |
| 4 | Legacy `secrets:write` gains create, rotate and revoke in Core | Accepted as documented behaviour. Core's permission tokens do not change. The upgrade guide tells operators to review roles that hold only the legacy `secrets:write`. |
| 5 | Should tokens be bound to tenant host aliases? | No. In the reference host the tenant comes from the authenticated principal, and a host alias is routing, not an authorization boundary ([host aliases](../secrets-collision-compatibility.md#host-aliases-and-authenticated-tenant-scope)). The upgrade guide says so; a deployment that needs one tenant per host adds its own host/principal check. |

## Canonical source and sole publisher

| Package ID | Canonical source | Publisher |
|---|---|---|
| `Elsa.Secrets.Persistence.EFCore`, `.PostgreSql`, `.SqlServer`, `.Sqlite` | `src/modules/Elsa.Secrets.Persistence.EFCore*` (Core lineage, which published 3.8.2 and 3.8.4) | Core 3.8/3.9 lines, then root `packages.yml` from 3.10.0 |
| `Elsa.Studio.Secrets` | `src/studio/modules/Elsa.Studio.Secrets` (Studio lineage, which published 3.8.2 and 3.8.4) | Studio repository for 3.8/3.9, then root `packages.yml` from 3.10.0 |
| `Elsa.Secrets.Api`, `.Core`, `.Management`, `.Models`, `.Scripting` | `src/extensions/secrets/*`, kept for history and 3.8/3.9 maintenance | Extensions repository for 3.8/3.9 only; none from 3.10.0 |

The publisher cutover itself follows the [lockstep release ADR](../../adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md).

## Ledger rows

The 47 rows are the Extensions 3.8.1 copies at `src/modules/secrets/`: 31 files from the four persistence projects and
16 from `Elsa.Studio.Secrets`. They were never active in the consolidated tree; they sit at their `.source` paths as
import provenance. The inspected 3.8.2 and 3.8.4 nuspecs for these package IDs name Core or Studio commits, so these
copies are not the source of any current release ([package lineage](../secrets-collision-compatibility.md#public-package-lineage)).
Decision 1 removes the one reason to port anything from them: their 3.8.1 schema matters only to a converter,
and no converter ships.

Each row therefore closes as `retired_from_active_tree`. The `.source` copies stay; nothing at `src/modules/secrets/`
is active, and the canonical projects above carry every supported capability.

## What this does not claim

- No customer database has been converted, and none will be by Elsa tooling.
- Installed-consumer counts for the legacy packages remain unknown. NuGet publication is not usage.
- NuGet deprecation and the publisher cutover are maintainer actions at the 3.10.0 release. This record does not
  perform them.
