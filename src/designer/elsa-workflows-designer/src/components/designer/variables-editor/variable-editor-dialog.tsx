import {Component, h, Prop, Event, EventEmitter, Method} from "@stencil/core";
import {DeleteIcon, EditIcon, PublishIcon, UnPublishIcon} from "../../icons/tooling";
import {DefaultActions, Variable, WorkflowDefinitionSummary} from "../../../models";
import {Filter} from "../../modals/workflow-definition-browser/filter";

@Component({
  tag: 'elsa-variable-editor-dialog',
  shadow: false
})
export class VariableEditorDialog {
  private modalDialog: HTMLElsaModalDialogElement;

  @Prop() variable: Variable;

  @Method()
  public async show() {
    await this.modalDialog.show();
  }

  @Method()
  public async hide() {
    await this.modalDialog.hide();
  }

  render() {
    const variable: Variable = this.variable ?? {name: '', type: ''};
    const closeAction = DefaultActions.Close();
    const actions = [closeAction];

    return (
      <div>
        <elsa-modal-dialog ref={el => (this.modalDialog = el)} actions={actions}>
          <div class="pt-4">
            <h2 class="text-lg font-medium ml-4 mb-2">Edit Variable</h2>
            <div class="align-middle inline-block min-w-full border-b border-gray-200">

            </div>
          </div>
        </elsa-modal-dialog>
      </div>
    );
  }

}
