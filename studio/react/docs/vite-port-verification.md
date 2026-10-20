# Vite port verification (#8843)

The original tree remains at the immutable import commit. This adaptation replaces
the Next shell with Vite, History API routing and an ASP.NET configuration/asset host.
The donor license observation is recorded in the source receipt; this port does not
assert a donor MIT grant. Production dependency licenses and notices are checked
separately by `scripts/dependency-licenses.mjs` during every frontend build.

| Behavior | Evidence |
| --- | --- |
| Original routing, designer transformations, journal tree and other existing unit contracts | All 74 existing Vitest cases pass |
| Direct deep link and reload under a non-root mount | Host HTTP tests and Playwright workflow-definitions page |
| Search string restored after reload/back | Playwright uses `search=original` and browser history |
| Navigation retains the document and mount | Playwright checks `performance.timeOrigin` across navigation |
| Runtime file replacement without frontend rebuild | Configuration and HTTP tests read changed titles |
| Inline JSON, custom file path, environment overlays and preserved feature options | Configuration tests |
| Default OIDC/external callback URLs include mount | Playwright observes the OIDC authorization request and PKCE callback under the mount; complete identity-provider round trips remain to verify |
| Missing JS/CSS return 404 | HTTP and browser tests |
| Real Elsa Identity sign-in and authenticated dashboard/list reload | Local browser check against Elsa.Server.Web |
| Create, persist and reopen a workflow | Docker browser check creates a flowchart through the real API, renders its designer, reloads it and removes the temporary definition |
| Standalone image and published host | Non-root Docker sign-in, definitions navigation/reload, runtime overrides; published host at site root |
| Production assets, Preview label and host title | Playwright against built ESM/CSS embedded in the ASP.NET host |
| Compatible production dependency licenses and retained notices | Build-time scan; full npm audit |

The port preserves the existing feature screens. Full operator parity, production
module extraction, SDK v1, BPMN, localization, tenant switching and release publication
have their own acceptance criteria in the [epic delivery plan](../../docs/specs/react-studio-preview/plan.md).
An existing unit-suite pass is not evidence that all of those features are complete.

The standalone development projects remain outside `Elsa.sln` until #8845 integrates
the production modules. Their exact inventory and central-package closure are covered
by the existing Studio dependency discovery guard.
