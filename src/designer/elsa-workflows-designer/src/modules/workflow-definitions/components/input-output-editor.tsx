import {Component, Event, EventEmitter, h, Prop, State, Watch} from "@stencil/core";
import {StorageDriverDescriptor, Variable} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {Container} from "typedi";
import {ModalActionClickArgs, ModalActionDefinition, ModalActionType, ModalDialogInstance, ModalDialogService} from "../../../components/shared/modal-dialog";
import {InputDefinition} from "../models/entities";
import {Input} from "postcss";
import {DeleteIcon, EditIcon} from "../../../components/icons/tooling";

@Component({
  tag: 'elsa-input-output-editor',
  shadow: false
})
export class InputOutputEditor {
  private readonly modalDialogService: ModalDialogService;
  private readonly inputSaveAction: ModalActionDefinition;
  private modalDialogInstance: ModalDialogInstance;

  constructor() {
    this.modalDialogService = Container.get(ModalDialogService);

    this.inputSaveAction = {
      name: 'Save',
      type: ModalActionType.Submit,
      text: 'Save',
      isPrimary: true,
      onClick: this.onInputDefinitionChanged
    };
  }

  @Prop() inputs?: Array<InputDefinition>;
  @Event() inputsChanged: EventEmitter<Array<InputDefinition>>;
  @State() inputsState: Array<InputDefinition> = [];

  @Watch('inputs')
  onInputsPropChanged(value: Array<InputDefinition>) {
    this.inputsState = !!this.inputs ? [...this.inputs] : [];
  }

  componentWillLoad() {
    this.onInputsPropChanged(this.inputs);
  }

  render() {
    const inputs = this.inputsState;

    return (
      <div>
        <div class="flex justify-end m-4">
          <button class="btn btn-primary" onClick={e => this.onAddInputClick()}>Add input argument</button>
        </div>
        <div class="align-middle inline-block min-w-full border-b border-gray-200">
          <table class="default-table">
            <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Type</th>
              <th scope="col"/>
            </tr>
            </thead>
            <tbody>
            {inputs.map(input => {

                const descriptor = descriptorsStore.variableDescriptors.find(x => x.typeName == input.type);
                const typeDisplayName = descriptor?.displayName ?? input.type;

                return (
                  <tr>
                    <td class="whitespace-nowrap">{input.name}</td>
                    <td class="whitespace-nowrap">{typeDisplayName}</td>
                    <td class="pr-6">
                      <elsa-context-menu
                        menuItems={[
                          {text: 'Edit', handler: e => this.onEditClick(e, input), icon: <EditIcon/>},
                          {text: 'Delete', handler: e => this.onDeleteClick(e, input), icon: <DeleteIcon/>},
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

  private getArgumentNameExists = (name: string): boolean => !!this.inputsState.find(x => x.name == name);

  private updateInputsState = (value: Array<InputDefinition>) => {
    this.inputsState = value;
    this.inputsChanged.emit(value);
  };

  private generateNewInputName = () => {
    const inputs = this.inputsState;
    let counter = inputs.length;

    while (true) {
      const newInputName = `Input${++counter}`;

      if (!this.getArgumentNameExists(newInputName))
        return newInputName;
    }
  };

  private onAddInputClick = async () => {
    const newInputName = this.generateNewInputName();
    const input: InputDefinition = {name: newInputName, type: 'Object', displayName: newInputName};

    this.modalDialogInstance = this.modalDialogService.show(() => <elsa-input-definition-editor-dialog-content input={input}/>, {actions: [this.inputSaveAction]})
  };

  private onEditClick = async (e: Event, input: InputDefinition) => {
    e.preventDefault();
    this.modalDialogInstance = this.modalDialogService.show(() => <elsa-input-definition-editor-dialog-content input={input}/>, {actions: [this.inputSaveAction]});
  };

  private onDeleteClick = (e: Event, input: InputDefinition) => {
    e.preventDefault();
    const inputs = this.inputs.filter(x => x != input);
    this.updateInputsState(inputs);
  };

  private onInputDefinitionChanged = async (a: ModalActionClickArgs) => {
    const updatedInput = await (a.instance.modalDialogContentRef as HTMLElsaInputDefinitionEditorDialogContentElement).getInput();
    let inputs = this.inputsState;
    const inputExists = !!inputs.find(x => x == updatedInput);

    if (inputExists)
      inputs = [...inputs];
    else
      inputs = [...inputs, updatedInput];

    inputs = inputs.sort((a, b) => a.name < b.name ? -1 : a.name > b.name ? 1 : 0);

    this.updateInputsState(inputs);
  };
}
