# 8. Empty String as Default Tenant ID

Date: 2026-01-27

## Status

Accepted

## Context

The multitenancy system in Elsa supports an optional mode where, when multitenancy is disabled, the system assumes a single tenant. When enabled, there's still a default tenant involved. The convention has been to use `null` as the tenant ID for the default tenant.

However, this convention created several issues:

1. **Dictionary compatibility**: The `DefaultTenantResolverPipelineInvoker` attempts to build a dictionary of tenants by their ID using `ToDictionary(x => x.Id)`, which throws an exception because dictionaries do not support null keys.
2. **Inconsistency**: The codebase used `null`, empty string (`""`), and string literal `"default"` interchangeably to refer to the default tenant across different parts of the system (e.g., in configuration files and database records).
3. **Code clarity**: Using `null` as a sentinel value for "default" is implicit and can be unclear to developers reading the code.

## Decision

We will standardize on using an **empty string** (`""`) as the tenant ID for the default tenant instead of `null`. This decision includes:

1. **Define a constant**: Add `Tenant.DefaultTenantId = ""` to explicitly document the convention.
2. **Update Tenant.Default**: Change `Tenant.Default.Id` from `null!` to use the `DefaultTenantId` constant.
3. **Add normalization helper**: Create a `NormalizeTenantId()` extension method that converts `null` to empty string, ensuring backwards compatibility with code that still uses null.
4. **Apply normalization consistently**: Use the normalization method in:
   - Dictionary creation in `DefaultTenantResolverPipelineInvoker`
   - Tenant lookups in `TenantResolverContext`
   - Any other places where tenant IDs are compared or used as dictionary keys

## Consequences

### Positive

- **No more exceptions**: Empty string is a valid dictionary key, eliminating the runtime exception in `DefaultTenantResolverPipelineInvoker`.
- **Backwards compatible**: The `NormalizeTenantId()` extension method ensures that existing code using `null` or empty string will work correctly.
- **Explicit convention**: The `DefaultTenantId` constant makes the convention clear and self-documenting.
- **Simplified logic**: Reduces the need for null-checking throughout the multitenancy code.
- **Consistency**: Aligns with parts of the codebase that were already using empty string (e.g., in configuration files).

### Negative

- **Migration consideration**: Existing data stores that have `null` tenant IDs will need to be normalized to empty strings, though the normalization helper provides a runtime solution.
- **String vs null semantics**: Some developers may find using empty string less intuitive than null for representing "no tenant", though this is mitigated by the explicit constant.

### Neutral

- The empty string convention is common in multitenancy systems and aligns with string-based identifier patterns used elsewhere in the codebase.
