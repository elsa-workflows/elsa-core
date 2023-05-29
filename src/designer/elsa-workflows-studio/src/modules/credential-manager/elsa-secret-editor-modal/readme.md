# elsa-secert-editor-modal



<!-- Auto Generated Below -->


## Properties

| Property        | Attribute         | Description | Type     | Default     |
| --------------- | ----------------- | ----------- | -------- | ----------- |
| `culture`       | `culture`         |             | `string` | `undefined` |
| `monacoLibPath` | `monaco-lib-path` |             | `string` | `undefined` |
| `serverUrl`     | `server-url`      |             | `string` | `undefined` |


## Dependencies

### Used by

 - [elsa-credential-manager-items-list](../pages)

### Depends on

- [elsa-modal-dialog](../../../components/shared/elsa-modal-dialog)
- [elsa-control](../../../components/controls/elsa-control)

### Graph
```mermaid
graph TD;
  elsa-secret-editor-modal --> elsa-modal-dialog
  elsa-secret-editor-modal --> elsa-control
  elsa-credential-manager-items-list --> elsa-secret-editor-modal
  style elsa-secret-editor-modal fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
