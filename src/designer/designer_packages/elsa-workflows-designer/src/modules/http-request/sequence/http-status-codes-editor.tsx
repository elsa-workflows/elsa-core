import {Component, h, Prop} from "@stencil/core";
import {ActivityInputContext} from "../../../services/activity-input-driver";
import {FormEntry} from "../../../components/shared/forms/form-entry";
import {getPropertyValue} from "../../../utils";
import {HttpStatusCodeCase, SendHttpRequest} from "./models";

@Component({
  tag: 'elsa-http-status-codes-editor',
  shadow: false
})
export class HttpStatusCodesEditor {
  @Prop() inputContext: ActivityInputContext;

  render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const hint = inputDescriptor.description;
    const expectedStatusCodes: Array<HttpStatusCodeCase> = getPropertyValue(inputContext) ?? [];
    const statusCodeTags = expectedStatusCodes.map(x => x.statusCode.toString());

    return (
      <FormEntry fieldId={fieldId} label={displayName} hint={hint}>
        <elsa-input-tags fieldId={fieldId} values={statusCodeTags} onValueChanged={this.onPropertyEditorChanged} placeHolder="Add status code"/>
      </FormEntry>
    );
  }

  private onPropertyEditorChanged = (e: CustomEvent<Array<string>>) => {
    const statusCodes = e.detail;
    const activity = this.inputContext.activity as SendHttpRequest;
    const expectedStatusCodes = activity.expectedStatusCodes ?? [];

    // Push new status codes.
    for (const statusCode of statusCodes) {
      if (expectedStatusCodes.findIndex(x => x.statusCode.toString() == statusCode) == -1)
        expectedStatusCodes.push({statusCode: parseInt(statusCode)});
    }

    // Remove status codes that are no longer present.
    for (let i = expectedStatusCodes.length - 1; i >= 0; i--) {
      const statusCode = expectedStatusCodes[i].statusCode.toString();
      if (statusCodes.findIndex(x => x == statusCode) == -1)
        expectedStatusCodes.splice(i, 1);
    }

    activity.expectedStatusCodes = expectedStatusCodes;
    this.inputContext.notifyInputChanged();
  };
}
