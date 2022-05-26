import {Component, h, Prop, State, Event, EventEmitter} from "@stencil/core";
import {v4 as uuid} from 'uuid';
import {Badge} from "../../components/shared/badge/badge";
import {Button} from "../../components/shared/button-group/models";
import {Label} from "./models";
import {Container} from "typedi";
import {DeleteLabelEventArgs, UpdateLabelEventArgs} from "./models";
import {LabelsApi} from "./labels-api";

@Component({
  tag: 'elsa-label-editor',
  shadow: false,
})
export class LabelEditor {
  static readonly defaultColor: string = '#991b1b';

  private readonly labelsApi: LabelsApi;

  constructor() {
    this.labelsApi = Container.get(LabelsApi);
  }

  @Prop() label: Label = {id: uuid(), name: 'Preview'};

  @Event() labelDeleted: EventEmitter<DeleteLabelEventArgs>;
  @Event() labelUpdated: EventEmitter<UpdateLabelEventArgs>;

  @State() editMode: boolean = false;
  @State() labelName: string;
  @State() labelDescription?: string;
  @State() labelColor?: string;

  async componentWillLoad() {
    const label = this.label;
    this.labelName = label.name;
    this.labelDescription = label.description;
    this.labelColor = label.color;
  }

  render() {
    const editMode = this.editMode;
    const labelName = this.labelName;
    const labelDescription = this.labelDescription;
    const labelColor = this.labelColor ?? LabelEditor.defaultColor;

    const buttons: Array<Button> = [];

    if (!editMode) {
      buttons.push({
        text: 'Edit',
        clickHandler: e => this.editMode = true
      });
    }

    buttons.push({
      text: 'Delete',
      clickHandler: this.onDeleteLabel
    });

    const icon = <svg class="h-6 w-6 text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <circle cx="12" cy="12" r="10"/>
      <rect x="9" y="9" width="6" height="6"/>
    </svg>

    return <div>

      <div class={`py-4 grid grid-cols-3 gap-4`}>
        <div>
          <Badge text={labelName} color={labelColor}/>
        </div>
        <div class="mt-1 text-sm text-gray-900">
          <span class="text-sm">
            {labelDescription}
          </span>
        </div>
        <div class="justify-self-end">
          <span>
            <elsa-button-group buttons={buttons}/>
          </span>
        </div>
      </div>

      <div hidden={!editMode}>
        <form class="pb-5 space-y-8 divide-y" onSubmit={e => this.onSubmit(e)}>
          <div class="grid grid-cols-4 gap-x-4">
            <div>
              <label htmlFor="labelName">Name</label>
              <div class="mt-1">
                <input type="text" id="labelName" name="labelName" autoComplete="off" value={labelName} onInput={e => this.onNameChanged(e)}/>
              </div>
            </div>

            <div>
              <label htmlFor="labelDescription">Description</label>
              <div class="mt-1">
                <input type="text" id="labelDescription" name="labelDescription" autoComplete="off" value={labelDescription} onInput={e => this.onDescriptionChanged(e)}/>
              </div>
            </div>

            <div>
              <label htmlFor="labelColor">Color</label>
              <div class="mt-1">
                <input type="text" id="labelColor" name="labelColor" autoComplete="off" value={labelColor} onInput={e => this.onColorChanged(e)}/>
              </div>
            </div>

            <div>
              <div class="mt-4 flex justify-end">
                <button type="button" class="btn btn-secondary" onClick={() => this.editMode = false}>Cancel</button>
                <button type="submit" class="ml-3 btn btn-primary">Save</button>
              </div>
            </div>

          </div>
        </form>
      </div>
    </div>
  }

  private updateLabel = async (id: string, name: string, description: string, color: string): Promise<Label> => {
    return await this.labelsApi.update(id, name, description, color);
  }

  private onNameChanged = (e: Event) => {
    this.labelName = (e.target as HTMLInputElement).value;
  };

  private onDescriptionChanged = (e: Event) => {
    this.labelDescription = (e.target as HTMLInputElement).value;
  };

  private onColorChanged = (e: Event) => {
    this.labelColor = (e.target as HTMLInputElement).value;
  };

  private onDeleteLabel = async () => {
    this.labelDeleted.emit(this.label);
  };

  private onSubmit = async (e: Event) => {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const labelId = this.label.id;
    const labelName = formData.get('labelName') as string;
    const labelDescription = formData.get('labelDescription') as string;
    const labelColor = formData.get('labelColor') as string;
    //const updatedLabel = await this.updateLabel(this.label.id, labelName, labelDescription, labelColor);
    // this.label = updatedLabel;
    // this.labelName = updatedLabel.name;
    // this.labelColor = updatedLabel.color;
    // this.labelDescription = updatedLabel.description;
    this.labelUpdated.emit({ id: labelId, name: labelName, description: labelDescription, color: labelColor });
    this.editMode = false;
  };
}
