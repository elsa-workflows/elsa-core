// bpmn-moddle declares only its BPMN model types (under 'bpmn-moddle/types'), not the module itself. This is the part of
// its constructor the shared rules use; ./modeling/moddle.ts gives it the precise shape.
declare module 'bpmn-moddle' {
    export const BpmnModdle: unknown;
}
