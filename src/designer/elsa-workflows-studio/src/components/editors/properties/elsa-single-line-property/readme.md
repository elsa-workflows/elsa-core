# elsa-modal-dialog



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute | Description | Type                         | Default     |
| -------------------- | --------- | ----------- | ---------------------------- | ----------- |
| `activityModel`      | --        |             | `ActivityModel`              | `undefined` |
| `propertyDescriptor` | --        |             | `ActivityPropertyDescriptor` | `undefined` |
| `propertyModel`      | --        |             | `ActivityDefinitionProperty` | `undefined` |


## Dependencies

### Depends on

- [elsa-property-editor](../../elsa-property-editor)

### Graph
```mermaid
graph TD;
  elsa-single-line-property --> elsa-property-editor
  elsa-property-editor --> elsa-multi-expression-editor
  elsa-multi-expression-editor --> elsa-expression-editor
  elsa-expression-editor --> elsa-monaco
  elsa-expression-editor --> context-consumer
  style elsa-single-line-property fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
