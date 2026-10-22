// bpmn-moddle declares only its BPMN model types (under 'bpmn-moddle/types'), not the module itself; the tests only
// need its BpmnModdle constructor, which ./moddle.ts narrows to the part they call.
declare module 'bpmn-moddle' {
    export const BpmnModdle: unknown;
}
