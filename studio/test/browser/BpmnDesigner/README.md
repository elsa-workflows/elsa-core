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

`bpmn-js.spec.ts` checks what the bpmn-js designer draws, such as its overlay badges. The contract leaves drawing to
each designer, so it runs only against bpmn-js.

## Another designer

The suite tests the bpmn-js designer's build output, `studio/src/modules/Elsa.Studio.Bpmn.BpmnJs/wwwroot`, by default.
To test another designer, point these at its build:

| Variable | Default |
| --- | --- |
| `BPMN_DESIGNER_DIRECTORY` | The bpmn-js designer's `wwwroot`. |
| `BPMN_DESIGNER_MODULE` | `bpmn-js-designer.js` |
| `BPMN_DESIGNER_STYLESHEET` | `bpmn-js-designer.css` |
