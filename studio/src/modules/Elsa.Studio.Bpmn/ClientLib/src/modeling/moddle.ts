/**
 * The bpmn-moddle instance the modeling kernel reads and writes documents with: the one bpmn-js uses, with Elsa's
 * extension registered, so a document reads, changes and writes the way it does in the bpmn-js designer.
 */
import { BpmnModdle as BpmnModdleConstructor } from 'bpmn-moddle';
import type { ModdleElement, ModdleFactory } from '../binding.js';
import { elsaModdleDescriptor } from '../elsa-moddle.js';

/** The part of a bpmn-moddle instance the kernel uses. */
export interface Moddle extends ModdleFactory {
    fromXML(xml: string, typeName?: string): Promise<{ rootElement: ModdleElement; warnings: unknown[] }>;
    /** Serializes `element` synchronously, inside the returned promise's executor; the promise only reports the result. */
    toXML(element: ModdleElement, options?: { format?: boolean }): Promise<{ xml: string }>;
}

const BpmnModdle = BpmnModdleConstructor as new (packages?: Record<string, unknown>) => Moddle;

/** A bpmn-moddle instance that knows Elsa's `elsa:` extension. */
export function createModdle(): Moddle {
    return new BpmnModdle({ elsa: elsaModdleDescriptor });
}
