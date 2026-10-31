# Live SQL Server bridge evidence

Status: **bounded synthetic proof passed**. The full disposable SQL Server fixture passed on 2026-09-24 against accepted Core commit `c37e9d7a2fa7e7c2af802b211e3d59db45fc2f6f` at fixture code commit `f2619bdad430dc43df61d2705dbac32d46d05abd`. It verified five released-package source rows, four Core aggregates and five versions, fresh Core conversion and decryption, old-package reopen with unchanged source hashes, mapped tenant read isolation, rejection of a novel cross-tenant write, and 14 rejection scenarios. The redacted report has `cutoverAllowed=false` and printed no connection string, generated password, key, plaintext or ciphertext. The full integration-program Python suite passed 207 tests in standard and optimized modes. The report is a synthetic compatibility receipt, not customer conversion or cutover approval.

The first run against the older Core pin stopped before conversion: the released source seed built and ran against the pinned SQL Server container, but Core migration `20260914120000_SecretDefaultTenantUniqueness` failed with SQL Server error number `102`. The fixture does not change production migration code or infer conversion, rollback, tenant isolation or cutover success from that failed run.

The failure is in the pinned SQL Server migration source:

- File: `src/modules/Elsa.Secrets.Persistence.EFCore.SqlServer/Migrations/Secrets/20260914120000_SecretDefaultTenantUniqueness.cs`
- Migration ID: `20260914120000_SecretDefaultTenantUniqueness`
- SQL construct in the executed migration:

```sql
THROW 50001, CONCAT(
    N'Cannot create unique index IX_Secret_TenantId_NormalizedName because leftover duplicate (TenantId, NormalizedName) rows exist. Operators must resolve leftover (TenantId, NormalizedName) rows before upgrade. Duplicate keys: ',
    LEFT(@DuplicateKeys, 1500)
), 1;
```

The fixture logs only the migration identifier, exception type and SQL Server error number. It omits the provider message, SQL connection strings, generated password, keys, plaintext and ciphertext. The failure was reproduced in two disposable runs; the third run applied migrations individually and localized the failure to this migration. Static fixture checks and both .NET runner builds completed before the live migration failure. Merged #8358 fixes the `THROW` syntax and has separate SQL Server migration regression tests. A fixture-only SQL Server setup correction then allowed the accepted-pin full bridge run to complete; no production database or package publisher was touched.
