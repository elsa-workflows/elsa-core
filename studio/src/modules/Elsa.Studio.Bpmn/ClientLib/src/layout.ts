/**
 * Generated diagram layout for a document that has none, so a designer can draw it.
 *
 * Needs a global `DOMParser` and `XMLSerializer`, as every browser has.
 */
import { layoutProcess } from 'bpmn-auto-layout';
import type { BpmnLoadWarning } from './contract.js';
import { BPMN_NAMESPACE_URI, lacksDiagramLayout, parseXml } from './load-warnings.js';

export interface BpmnLayoutResult {
    /** The document with a diagram layout: the original when it already had one. */
    xml: string;
    /** Warnings to add to the designer's load warnings. Empty when the document already had a layout. */
    warnings: BpmnLoadWarning[];
}

/**
 * Returns `xml` unchanged when it draws anything, and otherwise the same document with a generated layout for its first
 * process. A designer loads the result, and computes its load warnings against the original document.
 */
export async function withGeneratedLayout(xml: string): Promise<BpmnLayoutResult> {
    if (!lacksDiagramLayout(xml)) return { xml, warnings: [] };

    const document = parseXml(xml);
    const processes = Array.from(document.documentElement.children).filter(child => isBpmn(child, 'process'));
    if (processes.length === 0) return { xml, warnings: [] };

    linkSequenceFlows(document);
    const laidOut = await layoutProcess(new XMLSerializer().serializeToString(document));

    const [first, ...others] = processes.map(process => process.getAttribute('id') ?? '');
    const warnings: BpmnLoadWarning[] = [
        { kind: 'rewritten', message: `The document has no diagram layout. A layout for '${first}' is generated, and a save writes it.` }
    ];
    for (const other of others) {
        warnings.push({ kind: 'rewritten', message: `Only the first process gets a generated layout, so '${other}' is not drawn. A save keeps it.`, elementId: other });
    }

    return { xml: laidOut, warnings };
}

/**
 * bpmn-auto-layout follows a flow node's `incoming` and `outgoing` references, not the flows' `sourceRef` and
 * `targetRef`, so it draws no edge a document only declares on the flow. Adds the references a flow implies.
 */
function linkSequenceFlows(document: Document): void {
    const byId = new Map<string, Element>();
    for (const element of Array.from(document.getElementsByTagNameNS(BPMN_NAMESPACE_URI, '*'))) {
        const id = element.getAttribute('id');
        if (id) byId.set(id, element);
    }

    for (const flow of Array.from(document.getElementsByTagNameNS(BPMN_NAMESPACE_URI, 'sequenceFlow'))) {
        const id = flow.getAttribute('id');
        if (!id) continue;
        addReference(byId.get(flow.getAttribute('sourceRef') ?? ''), 'outgoing', id);
        addReference(byId.get(flow.getAttribute('targetRef') ?? ''), 'incoming', id);
    }
}

function addReference(node: Element | undefined, localName: 'incoming' | 'outgoing', flowId: string): void {
    if (!node) return;
    const existing = Array.from(node.children).filter(child => isBpmn(child, localName));
    if (existing.some(reference => reference.textContent?.trim() === flowId)) return;

    const reference = node.ownerDocument.createElementNS(BPMN_NAMESPACE_URI, node.prefix ? `${node.prefix}:${localName}` : localName);
    reference.textContent = flowId;
    // The order of a flow node's children does not matter to the layout, which writes them back in schema order.
    node.appendChild(reference);
}

function isBpmn(element: Element, localName: string): boolean {
    return element.namespaceURI === BPMN_NAMESPACE_URI && element.localName === localName;
}
