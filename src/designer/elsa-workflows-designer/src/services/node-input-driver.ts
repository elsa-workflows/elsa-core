import {InputDescriptor, NodeDescriptor, Node} from "../models";

export interface NodeInputContext {
  node: Node;
  nodeDescriptor: NodeDescriptor;
  inputDescriptor: InputDescriptor;
  inputChanged: (value: any, syntax: string) => void;
}

export interface NodeInputDriver {
  supportsInput(context: NodeInputContext): boolean;

  get priority(): number;

  renderInput(context: NodeInputContext): any;
}
