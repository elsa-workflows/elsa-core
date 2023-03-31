# elsa-workflows-designer-react-wrapper

This library was generated using the [React Stencil Integration documentation](https://stenciljs.com/docs/react).

This package is a wrapper of the `@elsa-workflows/elsa-workflows-designer` v3. It allows you to embbed the designer in an React App and keep benefit of all the React + Vite features. 

## Usage

### Import Package

use `npm install @elsa-workflows/elsa-workflows-designer-react-wrapper` to install the package.

you will also need `@elsa-workflows/elsa-workflows-designer` as a depencency for the css and logo assets.


### import module

first in your principal component, eg: App.tsx add the following line : 

```typescript
import { ElsaShell, defineCustomElements, ElsaWorkflowToolbar, ElsaStudio } from '@elsa-workflows/elsa-workflows-designer-react-wrapper';

defineCustomElements();

```
This will import the necessary component to show the designer and use the `defineCustomElements()`to register the Web Components for use in your browser and Framework.

Then you can use the React Component  :

```html
    <ElsaShell>
      <ElsaWorkflowToolbar></ElsaWorkflowToolbar>
      <div className="absolute inset-0" style={{top: "64px"}}>
      <ElsaStudio serverUrl='https://localhost:7228/elsa/api'
        monacoLibPath='https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.31.1/min'/>
        </div>
    </ElsaShell>
```

### Use the css

If you want to use the css available in the original designer, you have to use the original package : 

```shell
npm install @elsa-workflows/elsa-workflows-designer
```

Then you can import the css in you app : 

```typescript
import '@elsa-workflows/elsa-workflows-designer/dist/elsa-workflows-designer/elsa-workflows-designer.css'


```