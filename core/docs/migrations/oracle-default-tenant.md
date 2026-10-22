# Oracle default tenant (`""` → `NULL`)

Oracle stores `''` as `NULL`. Elsa's default tenant id is `""` (`Tenant.DefaultTenantId`).

#8391 added `WorkflowDefinitionRegistryGenerations` with `TenantId NVARCHAR2(450) NOT NULL` as the
primary key. On Oracle that insert is `ORA-01400` during host startup (`IncrementAsync("")`).

The Oracle-only `NullableDefaultTenant` management migration:

1. Adds a shadow identity `Id` primary key (Oracle cannot keep a nullable primary key).
2. Makes `TenantId` nullable.
3. Creates a function-based unique index on `NVL(TenantId, CHR(1))` so two default-tenant rows still
   collide. `CHR(1)` is not a tenant id.

Reads and writes of `""` go through an Oracle value converter. Increment and lookup branch
the default tenant to `TenantId == null` (so `ExecuteUpdate` is `IS NULL`, not a
parameterized `= NULL`) and keep `|| TenantId == ""` so other providers still match the
empty-string key. SQLite, SQL Server, PostgreSQL and MySQL behaviour is unchanged.

`Down()` refuses to restore `NOT NULL` while any `TenantId` is null, because Oracle cannot store
`''` in a `NOT NULL` column.
