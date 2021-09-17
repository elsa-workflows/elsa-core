# elsa-workflow-properties-panel



<!-- Auto Generated Below -->


## Properties

| Property               | Attribute                | Description | Type                | Default     |
| ---------------------- | ------------------------ | ----------- | ------------------- | ----------- |
| `culture`              | `culture`                |             | `string`            | `undefined` |
| `expandButtonPosition` | `expand-button-position` |             | `number`            | `1`         |
| `serverUrl`            | `server-url`             |             | `string`            | `undefined` |
| `workflowBlueprint`    | --                       |             | `WorkflowBlueprint` | `undefined` |


## Dependencies

### Used by

 - [elsa-workflow-blueprint-side-panel](../elsa-workflow-blueprint-side-panel)

### Depends on

- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-blueprint-properties-panel --> context-consumer
  elsa-workflow-blueprint-side-panel --> elsa-workflow-blueprint-properties-panel
  style elsa-workflow-blueprint-properties-panel fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
