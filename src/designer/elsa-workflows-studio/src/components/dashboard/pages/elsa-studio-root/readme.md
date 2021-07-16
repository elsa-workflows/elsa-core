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

- context-consumer

### Graph
```mermaid
graph TD;
  elsa-studio-root --> context-consumer
  style elsa-studio-root fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
