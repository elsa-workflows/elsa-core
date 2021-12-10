# elsa-check-list-property



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute    | Description | Type                         | Default     |
| -------------------- | ------------ | ----------- | ---------------------------- | ----------- |
| `activityModel`      | --           |             | `ActivityModel`              | `undefined` |
| `propertyDescriptor` | --           |             | `ActivityPropertyDescriptor` | `undefined` |
| `propertyModel`      | --           |             | `ActivityDefinitionProperty` | `undefined` |
| `serverUrl`          | `server-url` |             | `string`                     | `undefined` |


## Dependencies

### Depends on

- [elsa-property-editor](../../elsa-property-editor)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-check-list-property --> elsa-property-editor
  elsa-check-list-property --> context-consumer
  elsa-property-editor --> elsa-multi-expression-editor
  elsa-multi-expression-editor --> elsa-expression-editor
  elsa-expression-editor --> elsa-monaco
  elsa-expression-editor --> context-consumer
  style elsa-check-list-property fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
