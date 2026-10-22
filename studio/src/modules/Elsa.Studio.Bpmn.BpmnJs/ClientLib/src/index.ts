import type { BpmnDesignerModule } from '../../../Elsa.Studio.Bpmn/ClientLib/src/index.js';
import { createDesigner } from './designer.js';

/** The bpmn-js BPMN designer, as the module a host imports. */
const bpmnJsDesigner: BpmnDesignerModule = {
    designerId: 'bpmn-js',
    contractVersion: 1,
    createDesigner
};

export default bpmnJsDesigner;
