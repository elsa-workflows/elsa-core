# elsa-check-list-property



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute    | Description | Type                         | Default     |
| -------------------- | ------------ | ----------- | ---------------------------- | ----------- |
| `activityModel`      | --           |             | `ActivityModel`              | `undefined` |
| `propertyDescriptor` | --           |             | `ActivityPropertyDescriptor` | `undefined` |
| `propertyModel`      | --           |             | `ActivityDefinitionProperty` | `undefined` |
| `serverUrl`          | `server-url` |             | `string`                     | `undefined` |


## Events

| Event         | Description | Type                                                             |
| ------------- | ----------- | ---------------------------------------------------------------- |
| `valueChange` |             | `CustomEvent<(string \| number \| boolean \| SelectListItem)[]>` |


## Dependencies

### Depends on

- [elsa-input-tags-dropdown](../../../controls/elsa-input-tags)
- [elsa-input-tags](../../../controls/elsa-input-tags)
- [elsa-property-editor](../../elsa-property-editor)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-multi-text-property --> elsa-input-tags-dropdown
  elsa-multi-text-property --> elsa-input-tags
  elsa-multi-text-property --> elsa-property-editor
  elsa-multi-text-property --> context-consumer
  elsa-property-editor --> elsa-multi-expression-editor
  elsa-multi-expression-editor --> elsa-expression-editor
  elsa-expression-editor --> elsa-monaco
  elsa-expression-editor --> context-consumer
  style elsa-multi-text-property fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
