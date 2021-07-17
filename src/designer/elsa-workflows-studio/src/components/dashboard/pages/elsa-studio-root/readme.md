# elsa-studio-root



<!-- Auto Generated Below -->


## Properties

| Property        | Attribute         | Description | Type     | Default     |
| --------------- | ----------------- | ----------- | -------- | ----------- |
| `culture`       | `culture`         |             | `string` | `undefined` |
| `monacoLibPath` | `monaco-lib-path` |             | `string` | `undefined` |
| `serverUrl`     | `server-url`      |             | `string` | `undefined` |


## Events

| Event          | Description | Type                      |
| -------------- | ----------- | ------------------------- |
| `initializing` |             | `CustomEvent<ElsaStudio>` |


## Methods

### `addPlugins(pluginTypes: Array<any>) => Promise<void>`



#### Returns

Type: `Promise<void>`




## Dependencies

### Depends on

- [elsa-confirm-dialog](../../../shared/elsa-confirm-dialog)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-studio-root --> elsa-confirm-dialog
  elsa-studio-root --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  style elsa-studio-root fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
