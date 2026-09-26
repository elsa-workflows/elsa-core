# Extensions persistence source-tip reconciliation

Program #8194; import story #8286. Checked 2026-09-26 against Extensions `main` at `807cd89328c01e76bf1cababf525aff3c29bec93`, Studio `main` at `5b34ec327caffd132e18bfd88bfc9e862dc35c43`, and draft import #8409 at `8f85a3db84af3fac12df5f4364f285b70c761451`.

The draft already contains Extensions commit `9361c80e2ea56ccf71118fb53d6fd8f3d0fbbbf0` and Studio commit `5b34ec327caffd132e18bfd88bfc9e862dc35c43` in its ancestry. The only Extensions changes after `9361c80` are the two commits ending at `807cd893`. Their tree diff changes exactly three files:

| Upstream path | Consolidated path | Reconciliation |
| --- | --- | --- |
| `src/modules/persistence/Elsa.Persistence.Dapper/Contracts/ISqlDialect.cs` | `src/extensions/persistence/Elsa.Persistence.Dapper/Contracts/ISqlDialect.cs` | Restore the upstream default `Update(table, fields)` interface method and its shared SQL helper. |
| `src/modules/persistence/Elsa.Persistence.Dapper/Abstractions/SqlDialectBase.cs` | `src/extensions/persistence/Elsa.Persistence.Dapper/Abstractions/SqlDialectBase.cs` | Delegate the base implementation to the same helper. |
| `test/modules/persistence/Elsa.Dapper.UnitTests/SqlDialectDefaultUpdateTests.cs` | `test/extensions/modules/persistence/Elsa.Dapper.UnitTests/SqlDialectDefaultUpdateTests.cs` | Retain the upstream direct-interface-implementer regression test byte-for-byte. |

The two active C# files differ from the upstream tip only by a final newline. The imported Dapper test project is already listed in both `Elsa.sln` and `Elsa.Extensions.slnf`. The mapped change keeps the existing public interface and SQL text; it restores compatibility for third-party classes implementing `ISqlDialect` directly without the newer overload.

The history-bearing merge in [Core PR #8499](https://github.com/elsa-workflows/elsa-core/pull/8499) makes the original `807cd893` commit an ancestor without rewriting it or importing a second active copy of Extensions paths. The [fifth source-tip receipt](source-tip-refresh-2026-09-26-r5.json) and its validator pin the reviewed three-file mapping and preserve the earlier 14-file receipt at its historical integration commit. This does not rewrite the frozen E96 source receipt, establish package-mode compatibility, or authorize the draft import into `main`. Refresh the final source-tip receipt and repeat affected proofs if either upstream main advances again.

Verification on the mapped branch before the source-history merge:

- `dotnet test test/extensions/modules/persistence/Elsa.Dapper.UnitTests/Elsa.Dapper.UnitTests.csproj --framework net10.0 --configuration Release -m:1 --verbosity quiet`: 11 passed, 0 failed/skipped.
- `dotnet test test/extensions/modules/persistence/Elsa.Persistence.Dapper.UnitTests/Elsa.Persistence.Dapper.UnitTests.csproj --framework net10.0 --configuration Release -m:1 --verbosity quiet`: 18 passed, 0 failed/skipped.
- `dotnet build src/extensions/persistence/Elsa.Persistence.Dapper/Elsa.Persistence.Dapper.csproj --configuration Release -m:1 --verbosity quiet`: net8.0/net9.0/net10.0 succeeded, 0 errors. Existing package-advisory and source warnings remain; this change did not upgrade dependencies.
