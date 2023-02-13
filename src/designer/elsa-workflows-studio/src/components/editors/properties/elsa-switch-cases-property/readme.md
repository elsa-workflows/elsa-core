# elsa-switch-cases-property



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute | Description | Type                         | Default     |
| -------------------- | --------- | ----------- | ---------------------------- | ----------- |
| `activityModel`      | --        |             | `ActivityModel`              | `undefined` |
| `propertyDescriptor` | --        |             | `ActivityPropertyDescriptor` | `undefined` |
| `propertyModel`      | --        |             | `ActivityDefinitionProperty` | `undefined` |


## Events

| Event         | Description | Type                 |
| ------------- | ----------- | -------------------- |
| `valueChange` |             | `CustomEvent<any[]>` |


## Dependencies

### Depends on

- [elsa-expression-editor](../../elsa-expression-editor)
- [elsa-multi-expression-editor](../../elsa-multi-expression-editor)

### Graph
```mermaid
graph TD;
  elsa-switch-cases-property --> elsa-expression-editor
  elsa-switch-cases-property --> elsa-multi-expression-editor
  elsa-expression-editor --> elsa-monaco
  elsa-expression-editor --> context-consumer
  elsa-multi-expression-editor --> elsa-expression-editor
  style elsa-switch-cases-property fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
