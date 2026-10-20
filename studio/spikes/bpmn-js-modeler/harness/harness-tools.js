// SPIKE (#8901) harness-only tooling: a semantic XML diff and the bpmn-auto-layout experiment. Not part of the module.
import { layoutProcess } from 'bpmn-auto-layout';

const XMLNS = 'http://www.w3.org/2000/xmlns/';
const XSI = 'http://www.w3.org/2001/XMLSchema-instance';
const BPMNDI = 'http://www.omg.org/spec/BPMN/20100524/DI';

function qname(node) {
    return node.namespaceURI ? `{${node.namespaceURI}}${node.localName}` : node.localName;
}

function shortName(node) {
    return node.localName;
}

/** Resolves a prefixed QName value (only used for xsi:type) to {uri}local so a renamed prefix is not a difference. */
function resolveQNameValue(element, value) {
    const match = /^([\w.-]+):([\w.-]+)$/.exec(value);
    if (!match) return value;
    const uri = element.lookupNamespaceURI(match[1]);
    return uri ? `{${uri}}${match[2]}` : value;
}

function canonical(element) {
    const attrs = {};
    for (const attr of element.attributes) {
        if (attr.namespaceURI === XMLNS) continue;
        const name = qname(attr);
        attrs[name] = attr.namespaceURI === XSI && attr.localName === 'type' ? resolveQNameValue(element, attr.value) : attr.value;
    }
    const children = [];
    let text = '';
    for (const child of element.childNodes) {
        if (child.nodeType === Node.ELEMENT_NODE) children.push(canonical(child));
        else if (child.nodeType === Node.TEXT_NODE || child.nodeType === Node.CDATA_SECTION_NODE) text += child.nodeValue;
    }
    return { name: qname(element), local: shortName(element), attrs, children, text: text.trim() === '' ? '' : text };
}

function keyOf(node, index, siblings) {
    const id = node.attrs.id ?? node.attrs.name ?? node.attrs.bpmnElement;
    if (id != null) return `${node.local}[${id}]`;
    if (Object.keys(node.attrs).length === 0 && node.children.length === 0 && node.text) return `${node.local}(${node.text.trim()})`;
    const sameName = siblings.filter(s => s.name === node.name);
    return sameName.length > 1 ? `${node.local}#${sameName.indexOf(node)}` : node.local;
}

function diffNodes(a, b, path, out, options) {
    if (options.ignoreDi && a.name === `{${BPMNDI}}BPMNDiagram`) return;
    for (const [name, value] of Object.entries(a.attrs)) {
        if (!(name in b.attrs)) out.push({ kind: 'attr-removed', path, attr: name, before: value });
        else if (b.attrs[name] !== value) out.push({ kind: 'attr-changed', path, attr: name, before: value, after: b.attrs[name] });
    }
    for (const [name, value] of Object.entries(b.attrs)) {
        if (!(name in a.attrs)) out.push({ kind: 'attr-added', path, attr: name, after: value });
    }
    if (a.text !== b.text) {
        const whitespaceOnly = a.text.trim() === b.text.trim();
        out.push({ kind: whitespaceOnly ? 'text-whitespace-changed' : 'text-changed', path, before: a.text, after: b.text });
    }
    const aKeys = a.children.map((c, i) => keyOf(c, i, a.children));
    const bKeys = b.children.map((c, i) => keyOf(c, i, b.children));
    const bByKey = new Map(b.children.map((c, i) => [bKeys[i], c]));
    const aByKey = new Map(a.children.map((c, i) => [aKeys[i], c]));
    a.children.forEach((child, i) => {
        const key = aKeys[i];
        const childPath = `${path}/${key}`;
        if (options.ignoreDi && child.name === `{${BPMNDI}}BPMNDiagram`) return;
        const match = bByKey.get(key);
        if (!match) out.push({ kind: 'removed', path: childPath, element: child.name, xml: summarize(child) });
        else diffNodes(child, match, childPath, out, options);
    });
    b.children.forEach((child, i) => {
        const key = bKeys[i];
        if (options.ignoreDi && child.name === `{${BPMNDI}}BPMNDiagram`) return;
        if (!aByKey.has(key)) out.push({ kind: 'added', path: `${path}/${key}`, element: child.name, xml: summarize(child) });
    });
    const common = aKeys.filter(k => bByKey.has(k));
    const commonInB = bKeys.filter(k => aByKey.has(k));
    if (common.join('|') !== commonInB.join('|')) out.push({ kind: 'reordered', path, before: common, after: commonInB });
}

function summarize(node) {
    const attrs = Object.entries(node.attrs).map(([k, v]) => `${k}="${v}"`).join(' ');
    return `<${node.name}${attrs ? ' ' + attrs : ''}>${node.text ? node.text.slice(0, 80) : ''}${node.children.length ? `…${node.children.length} children` : ''}`;
}

function parse(xml) {
    const document = new DOMParser().parseFromString(xml, 'application/xml');
    const error = document.querySelector('parsererror');
    if (error) throw new Error(`XML parse error: ${error.textContent}`);
    return document;
}

function lexicalFacts(xml) {
    const document = parse(xml);
    const facts = { comments: 0, processingInstructions: 0, cdataSections: 0, declaredPrefixes: {} };
    const walker = document.createTreeWalker(document, NodeFilter.SHOW_ALL);
    for (let node = walker.currentNode; node; node = walker.nextNode()) {
        if (node.nodeType === Node.COMMENT_NODE) facts.comments++;
        else if (node.nodeType === Node.PROCESSING_INSTRUCTION_NODE) facts.processingInstructions++;
        else if (node.nodeType === Node.CDATA_SECTION_NODE) facts.cdataSections++;
        else if (node.nodeType === Node.ELEMENT_NODE) {
            for (const attr of node.attributes) {
                if (attr.namespaceURI === XMLNS) facts.declaredPrefixes[attr.localName === 'xmlns' ? '(default)' : attr.localName] = attr.value;
            }
        }
    }
    return facts;
}

/** Semantic diff: namespace URI + local name, attribute order and prefixes ignored, whitespace-only text ignored. */
export function canonicalDiff(before, after, options = {}) {
    const a = canonical(parse(before).documentElement);
    const b = canonical(parse(after).documentElement);
    const differences = [];
    diffNodes(a, b, `/${a.local}`, differences, options);
    return { differences, lexical: { before: lexicalFacts(before), after: lexicalFacts(after) } };
}

export { layoutProcess };
