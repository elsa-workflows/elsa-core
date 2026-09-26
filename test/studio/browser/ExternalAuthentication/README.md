# External Authentication browser tests

This directory contains Playwright browser tests for the brokered External Authentication flow in the Elsa Studio WebAssembly host.

The `Elsa.Studio.ExternalAuthentication.Tests` project covers the .NET broker clients and components. The browser suite uses a deterministic local harness around the module's shipped Web Crypto helper to verify browser-only PKCE, storage-scope, reload/tab, rotation-reuse, callback-replay, and logout behavior.

`playwright.config.ts` starts the harness automatically. `EXTERNAL_AUTH_BASE_URL` can instead point the same black-box suite at a deployed Studio browser fixture.

Before running the browser suite, install its Node dependencies and matching browsers:

```bash
cd tests/browser/ExternalAuthentication
npm install
npx playwright install
npm test
```

The coverage verifies S256 PKCE and exact origin, no token/secret URL leakage, memory/session/durable storage behavior and warnings, rotating refresh/reuse revocation, callback replay, and local/upstream logout. It also runs axe against `/login`, `/security/external-authentication/connections`, and the Security management surfaces; checks keyboard-only operation, visible focus, screen-reader landmarks and labels, deterministic text-first login methods, named management actions, and same-origin presentation assets with a text fallback; and proves a preferred method never starts without user input.
