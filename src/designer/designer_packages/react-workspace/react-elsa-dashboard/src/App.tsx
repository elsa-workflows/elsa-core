import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import '@elsa-workflows/elsa-workflows-designer/dist/elsa-workflows-designer/elsa-workflows-designer.css'

import { ElsaShell, defineCustomElements, ElsaWorkflowToolbar, ElsaStudio } from '@elsa-workflows/elsa-workflows-designer-react-wrapper';

defineCustomElements();

function App() {
  const [count, setCount] = useState(0)

  return (
    <ElsaShell>
      <ElsaWorkflowToolbar></ElsaWorkflowToolbar>
      <div className="absolute inset-0" style={{top: "64px"}}>
      <ElsaStudio serverUrl='https://localhost:7228/elsa/api'
        monacoLibPath='https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.31.1/min'/>
        </div>
    </ElsaShell>
  )
}

export default App
