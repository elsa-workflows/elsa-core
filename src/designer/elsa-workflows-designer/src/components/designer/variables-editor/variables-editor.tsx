import {Component, Event, EventEmitter, h, Prop, State, Watch} from "@stencil/core";
import {DeleteIcon, EditIcon} from "../../icons/tooling";
import {StorageDriverDescriptor, Variable} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {ModalActionClickArgs, ModalActionDefinition, ModalActionType, ModalDialogInstance, ModalDialogService} from "../../shared/modal-dialog";
import {Container} from "typedi";

@Component({
  tag: 'elsa-variables-editor',
  shadow: false
})
export class VariablesEditor {
  private readonly modalDialogService: ModalDialogService;
  private readonly saveAction: ModalActionDefinition;
  private modalDialogInstance: ModalDialogInstance;

  constructor() {
    this.modalDialogService = Container.get(ModalDialogService);

    this.saveAction = {
      name: 'Save',
      type: ModalActionType.Submit,
      text: 'Save',
      isPrimary: true,
      onClick: this.onVariableChanged
    };
  }

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
              <th scope="col">Storage</th>
              <th scope="col"/>
            </tr>
            </thead>
            <tbody>
            {variables.map(variable => {
                const storage = storageDrivers.find(x => x.typeName == variable.storageDriverTypeName);
                const storageName = storage?.displayName ?? '-';
                const descriptor = descriptorsStore.variableDescriptors.find(x => x.typeName == variable.typeName);
                const typeDisplayName = descriptor?.displayName ?? variable.typeName;

                return (
                  <tr>
                    <td class="whitespace-nowrap">{variable.name}</td>
                    <td class="whitespace-nowrap">{typeDisplayName}</td>
                    <td>{storageName}</td>
                    <td class="pr-6">
                      <elsa-context-menu
                        menuItems={[
                          {text: 'Edit', handler: e => this.onEditClick(e, variable), icon: <EditIcon/>},
                          {text: 'Delete', handler: e => this.onDeleteClick(e, variable), icon: <DeleteIcon/>},
                        ]}
                      />
                    </td>
                  </tr>);
              }
            )}
            </tbody>
          </table>
        </div>
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
    const variable = {name: newVariableName, typeName: 'Object', value: null};

    this.modalDialogInstance = this.modalDialogService.show(() => <elsa-variable-editor-dialog-content variable={variable}/>, {actions: [this.saveAction]})
  };

  private onEditClick = async (e: Event, variable: Variable) => {
    e.preventDefault();
    this.modalDialogInstance = this.modalDialogService.show(() => <elsa-variable-editor-dialog-content variable={variable}/>, {actions: [this.saveAction]});
  };

  private onDeleteClick = (e: Event, variable: Variable) => {
    e.preventDefault();
    const variables = this.variables.filter(x => x != variable);
    this.updateVariablesState(variables);
  };

  private onVariableChanged = async (a: ModalActionClickArgs) => {
    const updatedVariable = await (a.instance.modalDialogContentRef as HTMLElsaVariableEditorDialogContentElement).getVariable();
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
