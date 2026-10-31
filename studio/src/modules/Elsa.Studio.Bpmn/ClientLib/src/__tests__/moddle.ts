import type { ModdleElement } from '../binding.js';
import { createModdle, type Moddle } from '../modeling/moddle.js';

export { createModdle };

export const FOREIGN_NAMESPACE_URI = 'https://example.com/schemas/vendor';

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
