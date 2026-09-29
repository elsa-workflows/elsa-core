# Elsa Studio release note images

Screenshots of Elsa Studio running against a matching elsa-core build, seeded with three
workflows producing completed, faulted and suspended instances. Captured headless at
1600px wide with a device scale factor of 2, then framed with window chrome.

| File | Screen |
|---|---|
| 01-dashboard.png | Operational dashboard (`/`) |
| 02-opentelemetry.png | OpenTelemetry (`/diagnostics/opentelemetry`) |
| 03-structured-logs.png | Structured logs (`/diagnostics/structured-logs`) |
| 04-console-logs.png | Console logs (`/diagnostics/console`) |
| 05-designer-reactflow.png | React Flow designer after auto-arrange |
| 06-alterations.png | Alterable instances (`/alterations/instances`) |
| 07-login-theme.png | Sign-in screen with a configurable login theme |
| 08-weaver.png | Weaver workspace (`/ai/weaver`) |
| 09-secrets.png | Secrets management (`/security/secrets`) |
| 10-users.png | User management (`/security/users`) |
| 11-workflow-list.png | Workflow definitions list |

Two things to know about this path:

- It is deliberately not `docs/images/releases/`. That was ignored by the repository
  `.gitignore` pattern `[Rr]eleases/`, which silently excluded the whole folder. The
  pattern is now root-anchored, but this path avoids the question entirely.
- The folder carries no version. Release-note image URLs are pinned to the release
  branch (for example `release/3.8.0`), so the branch supplies the version and each
  release line keeps its own images. Refresh these in place for a new release on the
  same line.
