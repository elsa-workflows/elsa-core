# Separate authentication UI composition from security administration

**Status**: Accepted

**Date**: 2026-07-24

## Decision

`Elsa.Studio.Authentication.UI` owns the generic login/logout shell and chooser. Authentication modules integrate through the actual `ILoginMethodCatalog`, `ILoginMethodComponentProvider`, and `ILoginMethodIconProvider` contracts without owning the shell.

The Studio Settings area is UI composition and navigation only. SSO connection administration appears as one-level **Settings → SSO** at `/settings/sso-connections`; it does not create a server-side Settings domain or persistence API. External Identity Links and External Authentication Sessions remain security operations and appear as separate pages under **Security**.

## Rationale

The login surface is shared by local, brokered external, and future authentication methods, while links and sessions operate on security state. Keeping these responsibilities separate prevents the external-authentication package from becoming the owner of generic Studio authentication or a general Settings backend.
