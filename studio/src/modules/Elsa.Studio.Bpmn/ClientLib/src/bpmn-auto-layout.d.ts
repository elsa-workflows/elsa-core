// bpmn-auto-layout ships no type declarations; this is the one function it exports.
declare module 'bpmn-auto-layout' {
    /** Lays out the document's first process, replacing any diagram layout, and returns the document. */
    export function layoutProcess(xml: string): Promise<string>;
}
