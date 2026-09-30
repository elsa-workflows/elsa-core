# Elsa Studio workflow definition editor in React

A minimal React app, built with [Vite](https://vite.dev), that hosts the Elsa Studio workflow definition editor. The editor is a Blazor WebAssembly custom element, `<elsa-workflow-definition-editor>`, shipped in the [`@elsa-workflows/elsa-studio-wasm`](https://www.npmjs.com/package/@elsa-workflows/elsa-studio-wasm) npm package.

## How it fits together

- `npm install` runs `scripts/copy-elsa-studio-wasm.js`, which replaces the Blazor assets in `public/` (`_framework`, `_content`, `appsettings.json` and the scoped stylesheet) with a fresh copy from the npm package, so they are served from the site root.
- `index.html` loads the Studio stylesheets and scripts, ending with `_framework/blazor.webassembly.js`, which registers the custom element.
- `src/components/WorkflowDefinitionEditor.jsx` wraps the custom element in a React component, mapping `definitionId`, `remoteEndpoint` and `apiKey` props to its attributes. `src/App.jsx` renders it.

## Prerequisites

- Node.js `^20.19.0 || >=22.12.0` (the range Vite 8 supports).
- A running Elsa Server that allows CORS requests from `http://localhost:3000`. `src/App.jsx` passes its API URL as `remoteEndpoint` (default `https://localhost:5001/elsa/api`).

## Scripts

### `npm start`

Runs the app in development mode at [http://localhost:3000](http://localhost:3000).

### `npm run build`

Builds the app for production into `dist/`.

### `npm test`

Checks that the postinstall copy step provides every Studio asset `index.html` references and leaves no stale files behind.

### `npm run preview`

Serves the production build locally.
