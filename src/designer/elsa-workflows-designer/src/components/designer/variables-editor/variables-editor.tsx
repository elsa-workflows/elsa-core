import {Component, h, Prop, State, Event, EventEmitter, Watch} from "@stencil/core";
import {DeleteIcon, EditIcon} from "../../icons/tooling";
import {StorageDriverDescriptor, Variable} from "../../../models";
import {isNullOrWhitespace} from "../../../utils";
import descriptorsStore from "../../../data/descriptors-store";

@Component({
  tag: 'elsa-variables-editor',
  shadow: false
})
export class VariablesEditor {
  private variableEditorDialog: HTMLElsaVariableEditorDialogElement;
  @Prop() variables?: Array<Variable>;
  @Event() variablesChanged: EventEmitter<Array<Variable>>;
  @State() variablesState: Array<Variable> = [];

  @Watch('variables')
  onVariablesPropChanged(value: Array<Variable>) {
    this.variablesState = !!this.variables ? [...this.variables] : [];
  }

  componentWillLoad() {
    this.onVariablesPropChanged(this.variables);
  }

  render() {
    const variables = this.variables;
    const storageDrivers: Array<StorageDriverDescriptor> = descriptorsStore.storageDrivers;

    return (
      <div>
        <div class="flex justify-end m-4">
          <button class="btn btn-primary" onClick={e => this.onAddVariableClick()}>Add variable</button>
        </div>
        <div class="align-middle inline-block min-w-full border-b border-gray-200">
          <table class="default-table">
            <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Type</th>
              <th scope="col">Value</th>
              <th scope="col">Storage</th>
              <th scope="col"/>
            </tr>
            </thead>
            <tbody>
            {variables.map(variable => {
                const storage = storageDrivers.find(x => x.id == variable.storageDriverId);
                const storageName = storage?.displayName ?? '-';

                return (
                  <tr>
                    <td class="whitespace-nowrap">{variable.name}</td>
                    <td class="whitespace-nowrap">{variable.type}</td>
                    <td>{variable.value}</td>
                    <td>{storageName}</td>
                    <td class="pr-6">
                      <elsa-context-menu
                        menuItems={[
                          {text: 'Edit', clickHandler: e => this.onEditClick(e, variable), icon: <EditIcon/>},
                          {text: 'Delete', clickHandler: e => this.onDeleteClick(e, variable), icon: <DeleteIcon/>},
                        ]}
                      />
                    </td>
                  </tr>);
              }
            )}
            </tbody>
          </table>
        </div>
        <elsa-variable-editor-dialog ref={el => this.variableEditorDialog = el} onVariableChanged={e => this.onVariableChanged(e)}/>
      </div>
    );
  }

  private getVariableNameExists = (name: string): boolean => !!this.variables.find(x => x.name == name);

  private updateVariablesState = (value: Array<Variable>) => {
    this.variablesState = value;
    this.variablesChanged.emit(value);
  };

  private generateNewVariableName = () => {
    const variables = this.variables;
    let counter = variables.length;

    while (true) {
      const newVariableName = `Variable${++counter}`;

      if (!this.getVariableNameExists(newVariableName))
        return newVariableName;
    }
  };

  private onAddVariableClick = async () => {
    const newVariableName = this.generateNewVariableName();
    this.variableEditorDialog.variable = {name: newVariableName, type: 'Object', value: null};
    await this.variableEditorDialog.show();
  };

  private onEditClick = async (e: Event, variable: Variable) => {
    e.preventDefault();
    this.variableEditorDialog.variable = variable;
    await this.variableEditorDialog.show();
  };

  private onDeleteClick = (e: Event, variable: Variable) => {
    e.preventDefault();
    const variables = this.variables.filter(x => x != variable);
    this.updateVariablesState(variables);
  };

  private onVariableChanged = (e: CustomEvent<Variable>) => {
    const updatedVariable = e.detail;
    let variables = this.variables;
    const variableExists = !!variables.find(x => x == updatedVariable);

    if (variableExists)
      variables = [...variables];
    else
      variables = [...variables, updatedVariable];

    variables = variables.sort((a, b) => a.name < b.name ? -1 : a.name > b.name ? 1 : 0);

    this.updateVariablesState(variables);
  };
}
