import {NodeInputDriver, NodeInputContext} from "../../services/node-input-driver";
import {Container} from "typedi";
import {InputControlRegistry} from "../../services/input-control-registry";
import {h} from "@stencil/core";
import {FormEntry} from "../../components/shared/forms/form-entry";

// A standard input driver that determines the UI to be displayed based on the UI hint of the activity input property.
export class DefaultInputDriver implements NodeInputDriver {
  private inputControlRegistry: InputControlRegistry;

  constructor() {
    this.inputControlRegistry = Container.get(InputControlRegistry);
  }

  get priority(): number {
    return -1;
  }

  renderInput(context: NodeInputContext): any {
    const inputDescriptor = context.inputDescriptor;
    const uiHint = inputDescriptor.uiHint;
    const inputControl = this.inputControlRegistry.get(uiHint);
    const node = context.node;
    const propertyName = inputDescriptor.name;
    const displayName = inputDescriptor.displayName || propertyName;
    const description = inputDescriptor.description;
    const fieldId = inputDescriptor.name;
    const key = `${node.id}_${propertyName}`;

    return (
      <FormEntry label={displayName} fieldId={fieldId} hint={description} key={key}>
        {inputControl(context)}
      </FormEntry>);
  }

  supportsInput(context: NodeInputContext): boolean {
    const uiHint = context.inputDescriptor.uiHint;
    return this.inputControlRegistry.has(uiHint);
  }
}
