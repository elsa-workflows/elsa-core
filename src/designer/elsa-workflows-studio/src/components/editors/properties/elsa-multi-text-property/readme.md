# elsa-check-list-property



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute | Description | Type                         | Default     |
| -------------------- | --------- | ----------- | ---------------------------- | ----------- |
| `propertyDescriptor` | --        |             | `ActivityPropertyDescriptor` | `undefined` |
| `propertyModel`      | --        |             | `ActivityDefinitionProperty` | `undefined` |


## Dependencies

### Depends on

- [elsa-property-editor](../../elsa-property-editor)
- [elsa-input-tags](../../../controls/elsa-input-tags)

### Graph
```mermaid
graph TD;
  elsa-multi-text-property --> elsa-property-editor
  elsa-multi-text-property --> elsa-input-tags
  elsa-property-editor --> elsa-multi-expression-editor
  elsa-multi-expression-editor --> elsa-expression-editor
  elsa-expression-editor --> elsa-monaco
  elsa-expression-editor --> context-consumer
  style elsa-multi-text-property fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
