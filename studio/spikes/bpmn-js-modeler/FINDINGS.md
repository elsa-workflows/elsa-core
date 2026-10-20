# Spike: build Studio's BPMN modeler on bpmn-js (#8901)

Phase 0 spike for the decision record *Build the BPMN modeler on bpmn-js* (branch `docs/bpmn-v2-decisions`). This is
throwaway code. Nothing here is wired into Studio, and no production project or lockfile changes.

**Recommendation: go, with conditions.** bpmn-js can be the modeler. Every check passed or passed with caveats. None of
the caveats needs a fork of bpmn-js, and each one has a mitigation that fits the module's API. Four of them should be
written into the ADRs before Phase 3 starts (see *ADR text that should change*):

1. **"Lossless" needs a definition.** bpmn-js keeps everything Elsa's binding needs: all `elsa:` content, foreign
   extension elements and attributes, subprocess bodies at any depth, and DI. It is not a content-preserving XML
   editor, though. It drops unknown elements outside `extensionElements`, duplicate-id elements, comments and mixed
   content, and it reports most of these drops as import warnings. The module's contract has to make that report
   binding: the host must not save silently over content that was dropped.
2. **Documents without BPMN DI cannot be opened as they are.** 10 of the 14 core test assets have no DI, so Studio
   needs a DI-generation step. `bpmn-auto-layout` handled all of them.
3. **Load the module lazily.** It is about 390 KiB brotli, and 58% of its JavaScript is the properties-panel stack.
4. **Dark mode affects the watermark.** The bpmn.io logo is hard-coded `#404040`, which gives 1.14:1 contrast on a
   dark canvas. bpmn.io has to confirm the remedy.

Versions: `bpmn-js` 18.31.0, `bpmn-js-properties-panel` 5.65.1 and `@bpmn-io/properties-panel` 3.56.1. Harness only:
`bpmn-auto-layout` 1.3.0 and `esbuild` 0.28.2. Runtime: headless Chromium 141, Node 22, .NET SDK 10.0.402.

## Findings

| # | Check | Result | Evidence | Implication for the ADR |
|---|---|---|---|---|
| 1 | Bundle size and load time in Blazor WebAssembly | **Pass with caveats** | **Size:** the module's JS is 1,687 KiB minified, 453 KiB gzip and 366 KiB brotli. CSS adds 11 KiB brotli and the woff2 font 13 KiB. bpmn-js `Modeler` alone is 567 KiB minified (139 KiB brotli). The properties-panel stack accounts for about 1.1 MiB minified. About 0.58 MiB of that is FEEL editing and CodeMirror, which Elsa never uses. Another 439 KiB is `bpmn-js-properties-panel`, which ships its Camunda and Zeebe providers in one file that cannot be tree-shaken. Without FEEL and CodeMirror the module would be 217 KiB brotli. For scale, the Studio WASM host's `_framework` is 6.67 MiB brotli (net10.0 Release publish), so the module adds about 5.5%. **Load time** in a minimal Blazor WASM host (net10.0 Release, localhost, median of 3): first render at 743 ms, module imported at 901 ms, canvas interactive at 1,062 ms. That is about 320 ms of module cost after first render. On a static page the module costs 140 ms to download, parse and evaluate, 27 ms to create, and 86 ms to import and paint the crafted document. With 4× CPU throttling the total is about 1.05 s, and with 4× CPU plus a 10 Mbps / 40 ms link it is about 2.8 s. | The cost is acceptable only if Studio loads the module with a dynamic `import()` when a BPMN canvas opens, never at Studio boot. Record the lazy load and a size budget. Dropping FEEL and CodeMirror would save about 40% (upstream option, or bundler stubs). |
| 2 | Whole module API through JS interop | **Pass** | The calls tested were `load(xml)`, `save()`, `select(id)`, the `change` and `selection` events, `applyOverlays(statsById)`, `clearOverlays()` and `dispose()`. All of them were driven from a **Blazor WebAssembly component**: the component called the handle through `IJSObjectReference` and received events through a `DotNetObjectReference`. Payloads were deserialised into C# records, and the log shows what .NET received. The only Blazor-specific JS is 8 lines (`blazor-harness/wwwroot/bpmn-host.js`). `select` returns `true` for a known id and `false` for a missing one. Overlays reported `applied: 3`, `unknownElementIds: ["Nope"]`, and 3 badges plus the markers in the DOM; after clear, 0 remained. **Dispose:** it is idempotent, later calls throw, and it empties the canvas and panel hosts. The listener counts on `window`, `document`, `body` and both hosts were the same before create, while mounted and after dispose. Over 60 create, load, select, overlay and dispose cycles, `JSEventListeners` stayed at 14, DOM nodes at 51 and live `Modeler` instances at 0. The heap grew 1.0 MiB over the first 20 cycles and 0.13 MiB over the next 40, so it levels off (shared caches). In Blazor, after dispose there were 0 live modelers, listeners went from 53 to 29 and nodes from 635 to 80. | The payload shapes below can seed the documented API. The rules from the spike are listed under *API rules the spike surfaced*. |
| 3 | Theming, light and dark | **Pass with caveats** | diagram-js, bpmn-js and the properties panel all style through `--bio-*` tokens on `.bio-theme-parent`. Mapping MudBlazor's `--mud-palette-*` onto those tokens themes the palette, context pad, popup menu, direct editing and properties panel (`src/elsa-bpmn-theme.css`). Shapes follow the theme too: the renderer's default colours are `var(--elsa-bpmn-*)`, and tiny-svg writes fill and stroke as inline styles, so `var()` resolves. A theme toggle needs no re-render: a task's computed fill went from `#fff` to `rgb(55,55,64)` on the class switch. **Caveats:** instance-state markers need `!important` because the renderer's styles are inline. Colours stored in the document (`bioc:`/`color:`) override the theme, which left a light fill behind light label text in dark mode. The logo's contrast problem is under check 6. | Theme through CSS tokens; no fork is needed. Decide whether Studio ignores document colours in dark mode. |
| 4 | Lossless XML round trip (load, edit, save) | **Pass with caveats** | **Method:** the inputs were the 14 core assets, the 7 ClientLib fixtures (4 of them duplicates of core assets) and 2 crafted documents. Each went through load and save, then a semantic diff that compares namespace URIs and local names and ignores attribute order, prefixes and whitespace-only text. A second cycle checked that the output is stable. Elsa's own `BpmnInterchangeDocumentService.ImportAsync` then imported both the original and the round-tripped copy, comparing the activity JSON, the analysis issues and the element counts (`elsa-import-check/`). **Preserved exactly:** all `elsa:activityBinding`/`elsa:input` content, including XML-escaped `<&>`, `\u` escapes, Unicode, multi-line JSON, and its position among foreign siblings. Also preserved: foreign extension elements and attributes (`camunda:`, `vw:`, `acme:`) at any nesting, unknown attributes without a namespace (kept despite a warning), subprocess bodies at depth 3, event subprocesses, transactions, and a collapsed subprocess with its own plane. DI came through intact: bounds, fractional waypoints, labels, `BPMNLabelStyle`, `bioc:`/`color:`, `isExpanded`, `isMarkerVisible` and `isHorizontal`, plus a second `BPMNDiagram`, the `exporter` attributes and namespace prefixes (`omgdc`/`omgdi`, BPMN as the default namespace). **Elsa equivalence:** 5 of the 6 real documents with DI import identically. The sixth differs only in Elsa's retained raw copy of `multiInstanceLoopCharacteristics`, because `isSequential="false"` was omitted. **Lost:** unknown elements in the BPMN namespace (`bpmn:fancyTask`), foreign elements outside `extensionElements` (in a process or under `definitions`) and the second of two elements that share an id. Each of these appears in `load()`'s warnings. The duplicate-id case matters: 3 documents that Elsa refuses come out as documents Elsa accepts. Also lost, without any warning: comments, and the positions in mixed text-and-element content inside a foreign extension, whose whitespace grows on every save so it never stabilises. Elsa's own reader keeps `fancyTask` and the foreign element as retained content, so the loss shows up in the Elsa import diff. **Rewritten, same meaning:** attributes equal to their default are omitted (`isInterrupting="true"`, `cancelActivity="true"`, `isSequential="false"`). Child elements are put in schema order. `BpmnDiOrdering` reorders each plane's elements; the `preserveDiOrder` option turns that off, and newly created shapes still get DI. CDATA becomes escaped text. A legacy collapsed subprocess, whose children are drawn in the parent plane, gets its DI moved into a new `BPMNDiagram` that has no id. **No DI:** bpmn-js refuses with "no diagram to display" (10 of 14 core assets). `bpmn-auto-layout` (+19 KiB brotli) generated DI for all of them, and Elsa's import was identical for every valid one. **Edited round trip:** the edits were a rename at depth 3, a move, panel edits, and a new bound task inserted on a flow. The diff shows exactly those edits plus the losses above, and Elsa imported the edited document with exactly those changes. | Qualify "lossless". Make `load()` warnings a contract the host must act on: refuse to save, or require explicit acknowledgement, whenever import reported dropped content. Without that rule, a modeler save silently loses content and can turn an invalid document into a valid one. Add a DI-generation step for DI-less documents. Consider `preserveDiOrder` so a save does not reorder DI. |
| 5 | Editing `elsa:activityBinding` and `elsa:input` in the properties panel | **Pass** | `src/elsa-properties-provider.js`, about 170 lines, adds an "Elsa activity" group (the activity type) and an "Elsa inputs" list (name plus JSON text, validated as JSON). In the browser I edited an existing input, and invalid JSON was flagged and not written. I also bound a task created in the same session, which had no `extensionElements` at all, and added an input to it. Every write goes through the command stack: undo works, change events reach the host, and the undo depth was 25. Foreign siblings (`camunda:inputOutput`, `acme:retry`) kept their order. Elsa accepted the edited document. | The ADR approach works. Production should replace the free-text activity type and the raw JSON with Studio's activity picker and input editors, hosted outside the module. Bindings belong on tasks only: Elsa's binder refuses a binding on a gateway. |
| 6 | Watermark | **Pass (light); pass with caveats (dark)** | The layout puts the properties panel *beside* the canvas and floats no Studio element over the canvas. At 1440, 1024 and 800 px wide, the bpmn.io link sat at the canvas's bottom right, inside both the canvas and the viewport. All 9 sample points hit the link, with opacity 1 and z-index 100. Studio badge overlays panned underneath it stayed below it. **Dark theme:** the logo is inline `#404040`, so contrast is 10.4:1 on the light canvas and **1.14:1** on the dark one. An opt-in light plate *behind* the logo (z-index 99, not a hit target) restores 10.4:1. | The layout rule holds. Add: no floating Studio controls in the canvas's bottom-right corner. For dark mode, either get bpmn.io's agreement that the plate behind the logo keeps it "fully visible", or keep the canvas light in dark mode. |

Evidence (screenshots, `results.json`, round-tripped documents, `elsa-import-equivalence.json`) is regenerated by the
commands below into `SPIKE_OUT`; it is not committed.

## Decisions taken after the spike

The maintainer accepted the spike as a go on 2026-10-20, with these choices:

- Saving after `load()` reported dropped content requires the user's explicit acknowledgement; saving is not blocked.
- In the dark theme the canvas stays light, so the watermark keeps its contrast without a change bpmn.io would have to
  approve.

## API rules the spike surfaced

- Payloads used in the spike:
  - `load` returns `{ imported, error?, warnings: [{ message, elementId? }] }`.
  - `change` sends `{ elementIds, isDirty, canUndo, canRedo }`.
  - `selection` sends `{ elementIds, primary: { id, type, name? } | null }`.
  - `applyOverlays` takes `Record<bpmnElementId, BpmnElementStats>` and returns `{ applied, unknownElementIds }`.
- Methods must not depend on `this`, because JS interop may call them detached from the handle.
- Import clears the selection several times, so selection and change events are suppressed during `load`.
- `isDirty` reads `commandStack._stackIdx`, which is private. Production should count commands instead.
- `dispose()` must also empty the properties-panel host.
- Leak tests must not hold Playwright element handles or unreleased CDP object groups. Either one keeps a disposed
  modeler alive and looks like a leak; the spike hit both before isolating them.

## ADR text that should change

*Build the BPMN modeler on bpmn-js*

- Replace "a lossless XML round trip" with wording such as: "a round trip that preserves the `elsa:` extensions,
  foreign extension elements and attributes, subprocess bodies at every depth and BPMN DI. Content bpmn-js cannot
  represent (unknown elements outside `extensionElements`, duplicate ids, comments, mixed content) is reported by
  `load` and is never dropped silently: the host refuses to save, or saves only after the user acknowledges the
  loss."
- Add to *Decision*: "Documents without BPMN DI are given generated DI (for example `bpmn-auto-layout`) before they
  open in the modeler." The X6 adapter's fallback layout is part of the parity the retirement bullet refers to.
- Add to *Decision* or *Consequences*: "The module is loaded on demand when a BPMN canvas opens. It is about 390 KiB
  brotli, more than half of it the properties-panel stack."
- Add to *Consequences*: "In the dark theme the watermark needs a light backdrop or a light canvas; bpmn.io confirms
  the backdrop before it ships."

*Build one BPMN modeler module for both Studios*

- Add: "`load` returns the import warnings, and saving after a warning that reports dropped content requires an
  explicit host decision." The payload shapes in *API rules the spike surfaced* can be the starting point the ADR
  says the spike fixes.

*Keep BPMN XML as the source of truth*

- Consider: `PUT bpmn/definitions/{id}/xml` warns when foreign content retained in the stored document is missing
  from the new one. That guards against a client that ignores the warning contract.

## What this spike could not verify

- **The Studio host itself.** The WASM numbers come from a minimal Blazor WebAssembly app. Studio's real component,
  backend, authentication and save flow were not exercised. The Studio WASM host was only published to measure its
  payload.
- **Real conditions.** Everything ran headless on localhost, in Chromium only (no Firefox or Safari). Throttling was
  emulated, and nothing was measured on real devices.
- **Large documents.** Performance with hundreds of elements, keyboard use and accessibility were not tested.
- **The React Studio** and the licence terms beyond the watermark's visibility.
- **Today's X6 designer bundle size**, for comparison. Its ClientLib builds with webpack in development mode and was
  not rebuilt here.

## Side finding

The doc comment on `BpmnActivityBindingFormat` showed an `elsa:activityBinding` (Elsa.Switch) on an
`exclusiveGateway`, but `BpmnWorkBinder` refuses that document with "its work is not an unbound task". Fixed in #8910.

## Layout and reproduction

    src/elsa-bpmn-modeler.js            the framework-neutral module (createModeler -> handle)
    src/elsa-properties-provider.js     elsa: properties-panel provider
    src/elsa-moddle.json                elsa: moddle descriptor
    src/elsa-bpmn-theme.css             MudBlazor -> bpmn.io token mapping, markers, badges, watermark plate experiment
    harness/                            static harness page and the semantic XML diff
    blazor-harness/                     minimal Blazor WASM host using the module through JS interop
    elsa-import-check/                  imports originals and round-tripped copies through Elsa's importer
    fixtures/                           crafted documents (foreign, unknown and deep content; legacy collapsed / default ns)
    build.mjs, analyze-size.mjs         bundles and size reports
    run-spike.mjs                       Playwright driver for checks 1-6

    cd studio/spikes/bpmn-js-modeler
    npm ci
    node build.mjs && node analyze-size.mjs
    dotnet publish blazor-harness/BlazorHarness.csproj -c Release -o blazor-harness/bin/publish
    SPIKE_OUT=/tmp/spike-8901 node run-spike.mjs          # needs Playwright and Chromium
    dotnet run -c Release --project elsa-import-check -- /tmp/spike-8901 "$(git rev-parse --show-toplevel)"
