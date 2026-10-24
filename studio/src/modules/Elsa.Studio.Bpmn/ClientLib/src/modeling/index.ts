/**
 * The canvas-neutral modeling kernel: a BPMN document, its diagram planes in the terms a canvas draws, and undoable
 * edits to it. A designer built on a general-purpose canvas, such as X6, draws a session's document and turns gestures
 * into commands; the kernel owns the document, so what the designer saves is what bpmn-moddle writes.
 *
 * Imports nothing from a canvas, a UI framework, bpmn-js or diagram-js (__tests__/modeling-boundary.test.ts checks this).
 * A separate entry from ../index.ts, so the bpmn-js designer's bundle does not carry it.
 */
export type { Command } from './command-stack.js';
export { compound } from './command-stack.js';
export { updateProperties } from './commands.js';
export { CREATABLE_TYPES, creatableSize, createShape } from './create.js';
export { deleteElements } from './delete.js';
export { moveShapes } from './move.js';
export type { ReplaceOption, ReplaceTarget } from './replace.js';
export { replaceElement, replaceOptions } from './replace.js';
export { minimumSize, resizeShape } from './resize.js';
export type { BpmnEdgeView, BpmnPlane, BpmnPlaneView, BpmnReadResult, BpmnShapeView, Bounds, Point } from './document.js';
export { BpmnDocument } from './document.js';
export type { BpmnModelingSessionOptions } from './session.js';
export { BpmnModelingSession } from './session.js';
