# Live SQL Server bridge evidence

Status: **conditional local proof, final accepted pin pending**. Against unmerged #8358 head `0b20ab54a60a61b025d51a268f5b747e3a3c4860`, the full disposable SQL Server fixture exited successfully on 2026-09-24. It verified five released-package source rows, fresh Core conversion, old-package reopen with unchanged source hashes, mapped tenant read isolation, and 14 rejection scenarios. Its redacted report has `cutoverAllowed=false` and no printed connection string, generated password, key, plaintext or ciphertext. The full integration-program Python suite passed 207 tests in standard and optimized modes. This candidate proof must be rerun against #8358's accepted merge commit and exact fixture code before it is final compatibility evidence.

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

The fixture logs only the migration identifier, exception type and SQL Server error number. It omits the provider message, SQL connection strings, generated password, keys, plaintext and ciphertext. The failure was reproduced in two disposable runs; the third run applied migrations individually and localized the failure to this migration. Static fixture checks and both .NET runner builds completed before the live migration failure. #8358 fixes the `THROW` syntax and has separate SQL Server migration regression tests. A fixture-only SQL Server setup correction then allowed the candidate full bridge run to complete; no production database or package publisher was touched.
