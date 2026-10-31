import type { BpmnDesignerModule } from '../../../Elsa.Studio.Bpmn/ClientLib/src/index.js';
import { createDesigner } from './designer.js';

/** The X6 BPMN designer, as the module a host imports. */
const x6Designer: BpmnDesignerModule = {
    designerId: 'x6',
    contractVersion: 1,
    createDesigner
};

export default x6Designer;
