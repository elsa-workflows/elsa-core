// A deliberately small synthetic document contract, not a general BPMN validator.
export type XmlElement = { namespace: string | null; name: string; attributes: Record<string, string>; text: string; children: XmlElement[] };
const modelNamespace = 'http://www.omg.org/spec/BPMN/20100524/MODEL';

export function checkBpmnTree(root: XmlElement): void {
  function element(actual: XmlElement, name: string, attributes: Record<string, string>, text = ''): void {
    if (actual.namespace !== modelNamespace || actual.name !== name || actual.text !== text ||
        JSON.stringify(Object.entries(actual.attributes).sort()) !== JSON.stringify(Object.entries(attributes).sort()))
      throw new Error('bpmn_semantic_mismatch');
  }
  element(root, 'definitions', { id: 'Definitions_paired_browser', targetNamespace: 'http://bpmn.io/schema/bpmn' });
  if (root.children.length !== 1) throw new Error('bpmn_process_count');
  const process = root.children[0];
  element(process, 'process', { id: 'paired-process', name: 'Paired browser BPMN', isExecutable: 'true' });
  if (process.children.length !== 3) throw new Error('bpmn_element_count');
  // Serialization order is not part of graph identity.
  const children = [...process.children].sort((a, b) => a.name.localeCompare(b.name));
  const [end, flow, start] = children;
  element(end, 'endEvent', { id: 'paired-end', name: 'Paired end' });
  element(flow, 'sequenceFlow', { id: 'paired-flow', sourceRef: 'paired-start', targetRef: 'paired-end' });
  element(start, 'startEvent', { id: 'paired-start', name: 'Paired start' });
  if (end.children.length !== 1 || start.children.length !== 1 || flow.children.length !== 0)
    throw new Error('bpmn_connectivity_count');
  element(end.children[0], 'incoming', {}, 'paired-flow');
  element(start.children[0], 'outgoing', {}, 'paired-flow');
  if (end.children[0].children.length || start.children[0].children.length) throw new Error('bpmn_unexpected_content');
}

export function bpmnSemanticIdentity(root: XmlElement): string {
  checkBpmnTree(root);
  // Every semantic value above is checked, so prefix, whitespace and serialization order can vary.
  return 'Definitions_paired_browser|paired-process|paired-start|paired-end|paired-flow|paired-start>paired-end';
}

export function checkedXmlText(raw: Buffer): string {
  if (!raw.length || raw.length > 1024 * 1024) throw new Error('bpmn_xml_limit');
  const text = new TextDecoder('utf-8', { fatal: true }).decode(raw);
  // Never allow a DTD/entity or processing instruction beyond the XML declaration in a download.
  if (/<!|<\?(?!xml\s)/i.test(text)) throw new Error('bpmn_xml_declaration');
  return text;
}
