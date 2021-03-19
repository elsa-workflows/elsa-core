# elsa-modal-dialog



<!-- Auto Generated Below -->


## Properties

| Property               | Attribute                | Description | Type                         | Default     |
| ---------------------- | ------------------------ | ----------- | ---------------------------- | ----------- |
| `context`              | `context`                |             | `string`                     | `undefined` |
| `editorHeight`         | `editor-height`          |             | `string`                     | `'6em'`     |
| `propertyDescriptor`   | --                       |             | `ActivityPropertyDescriptor` | `undefined` |
| `propertyModel`        | --                       |             | `ActivityDefinitionProperty` | `undefined` |
| `serverUrl`            | `server-url`             |             | `string`                     | `undefined` |
| `singleLineMode`       | `single-line`            |             | `boolean`                    | `false`     |
| `syntax`               | `syntax`                 |             | `string`                     | `undefined` |
| `workflowDefinitionId` | `workflow-definition-id` |             | `string`                     | `undefined` |


## Dependencies

### Depends on

- [elsa-monaco](../../monaco/elsa-monaco)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-script-property --> elsa-monaco
  elsa-script-property --> context-consumer
  style elsa-script-property fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
