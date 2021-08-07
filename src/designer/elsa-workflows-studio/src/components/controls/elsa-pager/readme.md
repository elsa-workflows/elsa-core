# elsa-pager



<!-- Auto Generated Below -->


## Properties

| Property     | Attribute     | Description | Type               | Default     |
| ------------ | ------------- | ----------- | ------------------ | ----------- |
| `culture`    | `culture`     |             | `string`           | `undefined` |
| `history`    | --            |             | `RouterHistory`    | `undefined` |
| `location`   | --            |             | `LocationSegments` | `undefined` |
| `page`       | `page`        |             | `number`           | `undefined` |
| `pageSize`   | `page-size`   |             | `number`           | `undefined` |
| `totalCount` | `total-count` |             | `number`           | `undefined` |


## Events

| Event   | Description | Type                     |
| ------- | ----------- | ------------------------ |
| `paged` |             | `CustomEvent<PagerData>` |


## Dependencies

### Used by

 - [elsa-workflow-instance-list-screen](../../screens/workflow-instance-list/elsa-workflow-instance-list-screen)

### Graph
```mermaid
graph TD;
  elsa-workflow-instance-list-screen --> elsa-pager
  style elsa-pager fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
