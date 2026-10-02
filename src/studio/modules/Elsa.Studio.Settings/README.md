# Elsa Studio Settings

This package provides the `/settings` landing page and a one-level Settings submenu.

Modules contribute permission-filtered `SettingsSectionDescriptor` values through
`ISettingsSectionProvider`. `ISettingsSectionRegistry` validates and deterministically orders the
descriptors. The landing cards and submenu use that same registry, so navigation cannot drift.

The module stores no settings itself. Feature modules own their pages, persistence, permissions,
and API contracts.
