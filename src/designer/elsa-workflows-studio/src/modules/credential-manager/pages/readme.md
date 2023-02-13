# credential-manager-items-list



<!-- Auto Generated Below -->


## Properties

| Property        | Attribute         | Description | Type     | Default     |
| --------------- | ----------------- | ----------- | -------- | ----------- |
| `basePath`      | `base-path`       |             | `string` | `undefined` |
| `culture`       | `culture`         |             | `string` | `undefined` |
| `monacoLibPath` | `monaco-lib-path` |             | `string` | `undefined` |
| `serverUrl`     | `server-url`      |             | `string` | `undefined` |


## Dependencies

### Depends on

- [elsa-credential-manager-list-screen](../components)
- [elsa-secrets-picker-modal](../elsa-secrets-picker-modal)
- [elsa-secret-editor-modal](../elsa-secret-editor-modal)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-credential-manager-items-list --> elsa-credential-manager-list-screen
  elsa-credential-manager-items-list --> elsa-secrets-picker-modal
  elsa-credential-manager-items-list --> elsa-secret-editor-modal
  elsa-credential-manager-items-list --> context-consumer
  elsa-credential-manager-list-screen --> elsa-context-menu
  elsa-credential-manager-list-screen --> elsa-confirm-dialog
  elsa-credential-manager-list-screen --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-secrets-picker-modal --> elsa-modal-dialog
  elsa-secret-editor-modal --> elsa-modal-dialog
  elsa-secret-editor-modal --> elsa-control
  style elsa-credential-manager-items-list fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
