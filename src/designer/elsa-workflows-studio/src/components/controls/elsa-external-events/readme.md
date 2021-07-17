# elsa-external-events



<!-- Auto Generated Below -->


## Events

| Event                     | Description | Type                                                           |
| ------------------------- | ----------- | -------------------------------------------------------------- |
| `httpClientConfigCreated` |             | `CustomEvent<AxiosRequestConfig>`                              |
| `httpClientCreated`       |             | `CustomEvent<{ service: any; axiosInstance: AxiosInstance; }>` |


## Dependencies

### Used by

 - [elsa-studio-root](../../dashboard/pages/elsa-studio-root)

### Graph
```mermaid
graph TD;
  elsa-studio-root --> elsa-external-events
  style elsa-external-events fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
