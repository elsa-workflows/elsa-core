# elsa-json-property



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute | Description | Type                         | Default     |
| -------------------- | --------- | ----------- | ---------------------------- | ----------- |
| `propertyDescriptor` | --        |             | `ActivityPropertyDescriptor` | `undefined` |
| `propertyModel`      | --        |             | `ActivityDefinitionProperty` | `undefined` |


## Dependencies

### Depends on

- [elsa-property-editor](../../elsa-property-editor)
- [elsa-monaco](../../../controls/elsa-monaco)

### Graph
```mermaid
graph TD;
  elsa-json-property --> elsa-property-editor
  elsa-json-property --> elsa-monaco
  elsa-property-editor --> elsa-multi-expression-editor
  elsa-multi-expression-editor --> elsa-expression-editor
  elsa-expression-editor --> elsa-monaco
  elsa-expression-editor --> context-consumer
  style elsa-json-property fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
