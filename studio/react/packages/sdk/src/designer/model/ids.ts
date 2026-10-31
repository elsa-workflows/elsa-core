/** Elsa uses 16-character lowercase hex identifiers; we generate the same shape. */
export function newActivityId(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(8));
  return Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
}

export function childNodeId(parentNodeId: string | undefined, id: string) {
  return parentNodeId ? `${parentNodeId}:${id}` : id;
}

/** The property an Embedded port maps to on the activity JSON (`Then` → `then`). */
export function portPropertyName(portName: string) {
  return portName.charAt(0).toLowerCase() + portName.slice(1);
}
