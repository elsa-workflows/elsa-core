import {Component, Event, EventEmitter, h, Prop, State, Watch} from "@stencil/core";
import descriptorsStore from "../../../../data/descriptors-store";
import {Container} from "typedi";
import {ModalActionClickArgs, ModalActionDefinition, ModalActionType, ModalDialogInstance, ModalDialogService} from "../../../../components/shared/modal-dialog";
import {InputDefinition, OutputDefinition} from "../../models/entities";
import {DeleteIcon, EditIcon} from "../../../../components/icons/tooling";
import {FormEntry} from "../../../../components/shared/forms/form-entry";

@Component({
  tag: 'elsa-workflow-definition-input-output-settings',
  shadow: false
})
export class InputOutputSettings {
  private readonly modalDialogService: ModalDialogService;
  private readonly inputSaveAction: ModalActionDefinition;
  private readonly outputSaveAction: ModalActionDefinition;
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

    this.outputSaveAction = {
      name: 'Save',
      type: ModalActionType.Submit,
      text: 'Save',
      isPrimary: true,
      onClick: this.onOutputDefinitionChanged
    };
  }

  @Prop() inputs?: Array<InputDefinition>;
  @Prop() outputs?: Array<OutputDefinition>;
  @Prop() outcomes?: Array<string>;
  @Event() inputsChanged: EventEmitter<InputDefinition[]>;
  @Event() outputsChanged: EventEmitter<OutputDefinition[]>;
  @Event() outcomesChanged: EventEmitter<Array<string>>;
  @State() inputsState: Array<InputDefinition> = [];
  @State() outputsState: Array<OutputDefinition> = [];

  @Watch('inputs')
  onInputsPropChanged(value: Array<InputDefinition>) {
    this.inputsState = !!this.inputs ? [...this.inputs] : [];
  }

  @Watch('outputs')
  onOutputsPropChanged(value: Array<OutputDefinition>) {
    this.outputsState = !!this.outputs ? [...this.outputs] : [];
  }

  componentWillLoad() {
    this.onInputsPropChanged(this.inputs);
    this.onOutputsPropChanged(this.outputs);
  }

  render() {

    return (
      <div>
        {this.renderInputs()}
        {this.renderOutputs()}
        {this.renderOutcomes()}
      </div>
    );
  }

  private renderInputs = () => {
    const inputs = this.inputsState;

    return <div>
      <div class="tw-p-4">
        <h3 class="tw-text-base tw-leading-6 tw-font-medium tw-text-gray-900">Inputs</h3>
      </div>
      <div class="tw-align-middle tw-inline-block tw-min-w-full tw-border-b tw-border-gray-200">
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
                  <td class="tw-whitespace-nowrap">{input.name}</td>
                  <td class="tw-whitespace-nowrap">{typeDisplayName}</td>
                  <td class="tw-pr-6">
                    <elsa-context-menu
                      menuItems={[
                        {text: 'Edit', handler: e => this.onEditInputClick(e, input), icon: <EditIcon/>},
                        {text: 'Delete', handler: e => this.onDeleteInputClick(e, input), icon: <DeleteIcon/>},
                      ]}
                    />
                  </td>
                </tr>);
            }
          )}
          </tbody>
        </table>
      </div>
      <div class="tw-flex tw-justify-end tw-m-4">
        <button class="elsa-btn elsa-btn-primary" onClick={e => this.onAddInputClick()}>Add input parameter</button>
      </div>
    </div>
  };

  private renderOutputs = () => {
    const outputs = this.outputsState;

    return <div>
      <div class="tw-p-4">
        <h3 class="tw-text-base tw-leading-6 tw-font-medium tw-text-gray-900">Outputs</h3>
      </div>
      <div class="tw-align-middle tw-inline-block tw-min-w-full tw-border-b tw-border-gray-200">
        <table class="default-table">
          <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">Type</th>
            <th scope="col"/>
          </tr>
          </thead>
          <tbody>
          {outputs.map(output => {

              const descriptor = descriptorsStore.variableDescriptors.find(x => x.typeName == output.type);
              const typeDisplayName = descriptor?.displayName ?? output.type;

              return (
                <tr>
                  <td class="tw-whitespace-nowrap">{output.name}</td>
                  <td class="tw-whitespace-nowrap">{typeDisplayName}</td>
                  <td class="tw-pr-6">
                    <elsa-context-menu
                      menuItems={[
                        {text: 'Edit', handler: e => this.onEditOutputClick(e, output), icon: <EditIcon/>},
                        {text: 'Delete', handler: e => this.onDeleteOutputClick(e, output), icon: <DeleteIcon/>},
                      ]}
                    />
                  </td>
                </tr>);
            }
          )}
          </tbody>
        </table>
      </div>
      <div class="tw-flex tw-justify-end tw-m-4">
        <button class="elsa-btn elsa-btn-primary" onClick={e => this.onAddOutputClick()}>Add output parameter</button>
      </div>
    </div>
  };

  private renderOutcomes = () => {
    const outcomes = [...this.outcomes];

    return <div>
      <div class="tw-p-4">
        <h3 class="tw-text-base tw-leading-6 tw-font-medium tw-text-gray-900">Outcomes</h3>
      </div>
      <FormEntry label="" fieldId="WorkflowDefinitionOutcomes" hint="Enter a list of possible outcomes for this workflow.">
        <elsa-input-tags placeHolder="Add outcome" values={outcomes} onValueChanged={e => this.onOutcomesChanged(e.detail)}/>
      </FormEntry>
    </div>
  };

  private getInputNameExists = (name: string): boolean => !!this.inputsState.find(x => x.name == name);
  private getOutputNameExists = (name: string): boolean => !!this.outputsState.find(x => x.name == name);

  private updateInputsState = (value: Array<InputDefinition>) => {
    this.inputsState = value;
    this.inputsChanged.emit(value);
  };

  private updateOutputsState = (value: Array<OutputDefinition>) => {
    this.outputsState = value;
    this.outputsChanged.emit(value);
  };

  private generateNewInputName = () => {
    const inputs = this.inputsState;
    let counter = inputs.length;

    while (true) {
      const newName = `Input${++counter}`;

      if (!this.getInputNameExists(newName))
        return newName;
    }
  };

  private generateNewOutputName = () => {
    const outputs = this.outputsState;
    let counter = outputs.length;

    while (true) {
      const newName = `Output${++counter}`;

      if (!this.getOutputNameExists(newName))
        return newName;
    }
  };

  private onAddInputClick = async () => {
    const newName = this.generateNewInputName();
    const input: InputDefinition = {name: newName, type: 'Object', displayName: newName, isArray: false};

    this.modalDialogInstance = this.modalDialogService.show(() => <elsa-activity-input-editor-dialog-content input={input}/>, {actions: [this.inputSaveAction]})
  };

  private onAddOutputClick = async () => {
    const newName = this.generateNewOutputName();
    const output: OutputDefinition = {name: newName, type: 'Object', displayName: newName, isArray: false};

    this.modalDialogInstance = this.modalDialogService.show(() => <elsa-activity-output-editor-dialog-content output={output}/>, {actions: [this.outputSaveAction]})
  };

  private onEditInputClick = async (e: Event, input: InputDefinition) => {
    e.preventDefault();
    this.modalDialogInstance = this.modalDialogService.show(() => <elsa-activity-input-editor-dialog-content input={input}/>, {actions: [this.inputSaveAction]});
  };

  private onEditOutputClick = async (e: Event, output: OutputDefinition) => {
    e.preventDefault();
    this.modalDialogInstance = this.modalDialogService.show(() => <elsa-activity-output-editor-dialog-content output={output}/>, {actions: [this.outputSaveAction]});
  };

  private onDeleteInputClick = (e: Event, input: InputDefinition) => {
    e.preventDefault();
    const inputs = this.inputsState.filter(x => x != input);
    this.updateInputsState(inputs);
  };

  private onDeleteOutputClick = (e: Event, output: OutputDefinition) => {
    e.preventDefault();
    const outputs = this.outputsState.filter(x => x != output);
    this.updateOutputsState(outputs);
  };

  private onInputDefinitionChanged = async (a: ModalActionClickArgs) => {
    const updatedInput = await (a.instance.modalDialogContentRef as HTMLElsaActivityInputEditorDialogContentElement).getInput();
    let inputs = this.inputsState;
    const inputExists = !!inputs.find(x => x == updatedInput);

    if (inputExists)
      inputs = [...inputs];
    else
      inputs = [...inputs, updatedInput];

    inputs = inputs.sort((a, b) => a.name < b.name ? -1 : a.name > b.name ? 1 : 0);

    this.updateInputsState(inputs);
  };

  private onOutputDefinitionChanged = async (a: ModalActionClickArgs) => {
    const updatedOutput = await (a.instance.modalDialogContentRef as HTMLElsaActivityOutputEditorDialogContentElement).getOutput();
    let outputs = this.outputsState;
    const outputExists = !!outputs.find(x => x == updatedOutput);

    if (outputExists)
      outputs = [...outputs];
    else
      outputs = [...outputs, updatedOutput];

    outputs = outputs.sort((a, b) => a.name < b.name ? -1 : a.name > b.name ? 1 : 0);

    this.updateOutputsState(outputs);
  };

  private onOutcomesChanged = (outcomes: Array<string>) => {
    this.outcomesChanged.emit(outcomes);
  }
}
