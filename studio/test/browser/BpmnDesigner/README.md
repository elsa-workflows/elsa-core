# BPMN designer contract suite

Browser tests for the BPMN designer contract
([`studio/docs/specs/bpmn-designer-contract/contract.md`](../../../docs/specs/bpmn-designer-contract/contract.md)). Every
BPMN designer passes them. They load the designer's built module into a page in Chromium and drive it the way a host
does: through the contract only.

## Run

Build the designer first. For the bpmn-js designer, `scripts/integration-program/build_studio_clientlibs.sh` does that,
or run `npm ci --force && npm run build` in `studio/src/modules/Elsa.Studio.Bpmn.BpmnJs/ClientLib` once the shared
ClientLib in `Elsa.Studio.Bpmn` has its dependencies installed. Then:

```bash
npm ci
npx playwright install chromium
npm test
```

To use a Chromium that is already installed, set `BPMN_DESIGNER_CHROMIUM` to its executable.

## What it checks

- `contract.spec.ts`: every member of the contract: loading and its warnings, saving, selection, bindings, undo and
  redo, read-only designers, overlays, problem markers and disposal. The marker tests are skipped for a designer
  without the optional `applyMarkers`.
- `host.spec.ts`: the Blazor host's shim (`Elsa.Studio.Bpmn/wwwroot/bpmn-canvas-host.js`) with the designer under
  test: URLs resolved against the page's base URL, one stylesheet link, events forwarded, and the refusal of a module
  with another id or contract version.
- `bpmn-js.spec.ts`: what the bpmn-js designer draws, such as its overlay and problem badges. The contract leaves drawing to
  each designer, so it runs only against bpmn-js.
- `preservation.spec.ts`: round trips. `fixtures/preservation.bpmn` holds everything a designer must keep: `elsa:`
  bindings with escaped and non-ASCII JSON, foreign extension elements and attributes, subprocess bodies three levels
  deep, an event subprocess, a collapsed subprocess on its own plane, a collaboration, label layout and colours. It
  must load with no warnings, and an edit must change only what was edited. Every Elsa BPMN test asset
  (`core/test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets`) must load, and every saved document must
  load again with no warnings and save unchanged.

## Elsa import equivalence

A saved document must mean the same to Elsa as the original. With `BPMN_DESIGNER_OUTPUT` set to a directory, the
preservation tests write each original's saved copy there. `ImportEquivalence` then imports both through Elsa's BPMN
import and fails when Elsa builds a different workflow, validation result or analysis from a copy whose load reported
no dropped content:

```bash
BPMN_DESIGNER_OUTPUT=/tmp/bpmn-round-trips npm test
dotnet run --project ImportEquivalence/BpmnImportEquivalence.csproj -- /tmp/bpmn-round-trips ../../../..
```

## Another designer

The suite tests the bpmn-js designer's build output, `studio/src/modules/Elsa.Studio.Bpmn.BpmnJs/wwwroot`, by default.
To test another designer, point these at its build:

| Variable | Default |
| --- | --- |
| `BPMN_DESIGNER_DIRECTORY` | The bpmn-js designer's `wwwroot`. |
| `BPMN_DESIGNER_MODULE` | `bpmn-js-designer.js` |
| `BPMN_DESIGNER_STYLESHEET` | `bpmn-js-designer.css` |
