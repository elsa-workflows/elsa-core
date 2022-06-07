import {Component, h, Prop, Event, EventEmitter, Method} from "@stencil/core";
import {DeleteIcon, EditIcon, PublishIcon, UnPublishIcon} from "../../icons/tooling";
import {DataDrive, DefaultActions, InputDescriptor, Variable, WorkflowDefinitionSummary} from "../../../models";
import {Filter} from "../../modals/workflow-definition-browser/filter";
import {FormEntry} from "../../shared/forms/form-entry";
import {isNullOrWhitespace} from "../../../utils";

@Component({
  tag: 'elsa-variable-editor-dialog',
  shadow: false
})
export class VariableEditorDialog {
  private modalDialog: HTMLElsaModalDialogElement;

  @Prop() variable: Variable;
  @Event() variableChanged: EventEmitter<Variable>;

  @Method()
  public async show() {
    await this.modalDialog.show();
  }

  @Method()
  public async hide() {
    await this.modalDialog.hide();
  }

  render() {
    const variable: Variable = this.variable ?? {name: '', type: 'Object'};
    const variableType = variable.type;
    const cancelAction = DefaultActions.Cancel();
    const saveAction = DefaultActions.Save();
    const actions = [cancelAction, saveAction];
    const availableTypes: Array<string> = ['Object', 'String', 'Int32', 'Int64', 'Single', 'Double']; // TODO: Fetch these from backend.
    const availableDrives: Array<DataDrive> = [null, {id: 'Workflow'}, {id: 'Blob Storage'}, {id: 'FTP'}]; // TODO: Fetch these from backend.

    return (
      <div>
        <form class="h-full flex flex-col bg-white" onSubmit={e => this.onSubmit(e)} method="post">
          <elsa-modal-dialog ref={el => (this.modalDialog = el)} actions={actions}>
            <div class="pt-4">
              <h2 class="text-lg font-medium ml-4 mb-2">Edit Variable</h2>
              <div class="align-middle inline-block min-w-full border-b border-gray-200">


                <FormEntry fieldId="variableName" label="Name" hint="The technical name of the variable.">
                  <input type="text" name="variableName" id="variableName" value={variable.name}/>
                </FormEntry>

                <FormEntry fieldId="variableType" label="Type" hint="The type of the variable.">
                  <select id="variableType" name="variableType">
                    {availableTypes.map(type => <option value={type} selected={type == variableType}>{type}</option>)}
                  </select>
                </FormEntry>

                <FormEntry fieldId="variableValue" label="Value" hint="The value of the variable.">
                  <input type="text" name="variableValue" id="variableValue" value={variable.value}/>
                </FormEntry>

                <FormEntry fieldId="variableStorage" label="Storage" hint="The storage to use when persisting the variable.">
                  <select id="variableStorage" name="variableStorage">
                    {availableDrives.map(drive => {
                      const value = drive?.id;
                      const text = drive?.id ?? 'Transient';
                      const selected = value == variable.driveId;
                      return <option value={value} selected={selected}>{text}</option>;
                    })}
                  </select>
                </FormEntry>

              </div>
            </div>
          </elsa-modal-dialog>
        </form>
      </div>
    );
  }

  private onSubmit = async (e: Event) => {
    e.preventDefault();
    const formData = new FormData(e.target as HTMLFormElement);
    const name = formData.get('variableName') as string;
    const value = formData.get('variableValue') as string;
    const type = formData.get('variableType') as string;
    const driveId = formData.get('variableStorage') as string;
    const variable = this.variable;

    variable.name = name;
    variable.type = type;
    variable.value = value;
    variable.driveId = isNullOrWhitespace(driveId) ? null : driveId;

    this.variableChanged.emit(variable);
    await this.hide();
  };

}
