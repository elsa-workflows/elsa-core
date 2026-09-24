# Live SQL Server bridge evidence

Status: **NotProven**. The released source seed built and ran against the pinned SQL Server container, but Core conversion could not begin because the pinned Core target failed while applying migration `20260914120000_SecretDefaultTenantUniqueness`. The runner reported SQL Server error number `102` (syntax error) for that migration. The fixture does not change production migration code and does not claim conversion, rollback, tenant isolation, or cutover success from this run.

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

The fixture logs only the migration identifier, exception type and SQL Server error number. It omits the provider message, SQL connection strings, generated password, keys, plaintext and ciphertext. The failure was reproduced in two disposable runs; the third run applied migrations individually and localized the failure to this migration. Static fixture checks and both .NET runner builds completed before the live migration failure.
