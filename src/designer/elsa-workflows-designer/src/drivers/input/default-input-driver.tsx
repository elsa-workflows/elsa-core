import {ActivityInputDriver, ActivityInputContext} from "../../services/activity-input-driver";
import {Container} from "typedi";
import {InputControlRegistry} from "../../services/input-control-registry";
import {h} from "@stencil/core";
import {FormEntry} from "../../components/shared/forms/form-entry";

// A standard input driver that determines the UI to be displayed based on the UI hint of the activity input property.
export class DefaultInputDriver implements ActivityInputDriver {
  private inputControlRegistry: InputControlRegistry;

  constructor() {
    this.inputControlRegistry = Container.get(InputControlRegistry);
  }

  get priority(): number {
    return -1;
  }

  renderInput(context: ActivityInputContext): any {
    const inputDescriptor = context.inputDescriptor;
    const uiHint = inputDescriptor.uiHint;
    const inputControl = this.inputControlRegistry.get(uiHint);
    return inputControl(context);
  }

  supportsInput(context: ActivityInputContext): boolean {
    const uiHint = context.inputDescriptor.uiHint;
    return this.inputControlRegistry.has(uiHint);
  }
}
