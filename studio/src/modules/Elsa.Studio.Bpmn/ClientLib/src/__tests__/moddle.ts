// bpmn-moddle ships types for the BPMN model, not for its constructor; this is the part of it the tests use.
import { BpmnModdle as BpmnModdleConstructor } from 'bpmn-moddle';
import { elsaModdleDescriptor } from '../elsa-moddle.js';
import type { ModdleElement, ModdleFactory } from '../binding.js';

interface Moddle extends ModdleFactory {
    fromXML(xml: string, typeName?: string): Promise<{ rootElement: ModdleElement; warnings: unknown[] }>;
    toXML(element: ModdleElement, options?: { format?: boolean }): Promise<{ xml: string }>;
}

const BpmnModdle = BpmnModdleConstructor as new (packages?: Record<string, unknown>) => Moddle;

export const FOREIGN_NAMESPACE_URI = 'https://example.com/schemas/vendor';

export function createModdle(): Moddle {
    return new BpmnModdle({ elsa: elsaModdleDescriptor });
}

/** A one-process document whose process holds `content`, with the elsa and a foreign vendor namespace declared. */
export function document(content: string): string {
    return `<?xml version="1.0" encoding="UTF-8"?>
<bpmn:definitions xmlns:bpmn="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:elsa="https://elsaworkflows.io/schemas/bpmn/v1" xmlns:vendor="${FOREIGN_NAMESPACE_URI}" id="Definitions_1" targetNamespace="https://example.com/process">
  <bpmn:process id="Process_1">
    ${content}
  </bpmn:process>
</bpmn:definitions>`;
}

/** Reads `xml` and returns the flow element with id `id`. */
export async function elementOf(moddle: Moddle, xml: string, id: string): Promise<{ definitions: ModdleElement; element: ModdleElement }> {
    const { rootElement: definitions } = await moddle.fromXML(xml);
    const process = (definitions.rootElements as ModdleElement[]).find(root => root.$instanceOf('bpmn:Process'))!;
    const element = (process.flowElements as ModdleElement[]).find(candidate => candidate.id === id);
    if (!element) throw new Error(`No element '${id}'.`);
    return { definitions, element };
}
