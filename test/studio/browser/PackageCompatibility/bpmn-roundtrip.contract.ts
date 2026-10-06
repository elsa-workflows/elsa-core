import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { bpmnSemanticIdentity, checkedXmlText, type XmlElement } from './bpmn-roundtrip.js';

const namespace = 'http://www.omg.org/spec/BPMN/20100524/MODEL';
const element = (name: string, attributes: Record<string, string> = {}, children: XmlElement[] = [], text = ''): XmlElement =>
  ({ namespace, name, attributes, text, children });
const tree = element('definitions', { id: 'Definitions_paired_browser', targetNamespace: 'http://bpmn.io/schema/bpmn' }, [
  element('process', { id: 'paired-process', name: 'Paired browser BPMN', isExecutable: 'true' }, [
    element('startEvent', { id: 'paired-start', name: 'Paired start' }, [element('outgoing', {}, [], 'paired-flow')]),
    element('endEvent', { id: 'paired-end', name: 'Paired end' }, [element('incoming', {}, [], 'paired-flow')]),
    element('sequenceFlow', { id: 'paired-flow', sourceRef: 'paired-start', targetRef: 'paired-end' })
  ])
]);
const identity = bpmnSemanticIdentity(tree);
const reordered = structuredClone(tree);
reordered.children[0].children.reverse();
reordered.attributes = Object.fromEntries(Object.entries(reordered.attributes).reverse());
assert.equal(identity, bpmnSemanticIdentity(reordered));
const mutations: Array<(root: XmlElement) => void> = [
  root => { root.namespace = 'https://foreign.invalid'; },
  root => { root.children.push(structuredClone(root.children[0])); },
  root => { root.children[0].attributes.id = 'different-process'; },
  root => { root.children[0].children[0].attributes.id = 'different-start'; },
  root => { root.children[0].children[0].children[0].text = 'different-flow'; },
  root => { root.children[0].children[2].attributes.targetRef = 'paired-start'; },
  root => { root.children[0].children[2].attributes.sourceRef = 'missing'; },
  root => { root.children[0].children.push(element('scriptTask', { id: 'injected' })); },
  root => { root.children[0].children[0].children.push(element('timerEventDefinition')); },
  root => { root.children[0].children[0].attributes['foreign'] = 'unexpected'; },
  root => { root.children[0].children[0].children[0].children.push(element('script')); },
  root => { root.children[0].children[0].text = 'unexpected text'; }
];
for (const mutate of mutations) {
  const changed = structuredClone(tree); mutate(changed);
  assert.throws(() => bpmnSemanticIdentity(changed));
}
const raw = readFileSync(new URL('./paired-browser.bpmn', import.meta.url));
assert.match(checkedXmlText(raw), /id="paired-process"/);
for (const invalid of [Buffer.alloc(0), Buffer.alloc(1024 * 1024 + 1), Buffer.from([0xff]),
  Buffer.from('<!DOCTYPE definitions [<!ENTITY x SYSTEM "file:///private">]><definitions/>'),
  Buffer.from('<?command hidden?><definitions/>')]) assert.throws(() => checkedXmlText(invalid));
process.stdout.write('BPMN roundtrip contracts passed\n');
