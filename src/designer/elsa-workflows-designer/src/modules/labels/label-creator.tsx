import {Component, Event, EventEmitter, h, Prop, State} from "@stencil/core";
import {Badge} from "../../components/shared/badge/badge";
import {isNullOrWhitespace} from "../../utils";
import {CreateLabelEventArgs} from "./models";

@Component({
  tag: 'elsa-label-creator',
  shadow: false,
})
export class LabelCreator {
  static readonly defaultColor: string = '#991b1b';

  @Event() public createLabelClicked: EventEmitter<CreateLabelEventArgs>;

  @State() labelName: string;
  @State() labelColor: string = LabelCreator.defaultColor;
  @State() labelDescription: string = '';

  render() {

    const labelName = this.labelName;
    const previewLabelName = isNullOrWhitespace(labelName) ? 'Preview' : labelName;
    const labelColor = this.labelColor;
    const labelDescription = this.labelDescription;

    return <div>

      <div>
        <Badge text={previewLabelName} color={labelColor}/>
      </div>

      <div class="mt-4">
        <form onSubmit={e => this.onSubmit(e)} class="pb-5 space-y-8 divide-y">
          <div class="grid grid-cols-4 gap-x-4">
            <div>
              <label htmlFor="labelName">Name</label>
              <div class="mt-1">
                <input type="text" id="labelName" name="labelName" value={labelName} autoComplete="off" onInput={e => this.onNameChanged(e)}/>
              </div>
            </div>

            <div>
              <label htmlFor="labelDescription">Description</label>
              <div class="mt-1">
                <input type="text" id="labelDescription" name="labelDescription" value={labelDescription} autoComplete="off"/>
              </div>
            </div>

            <div>
              <label htmlFor="labelColor">Color</label>
              <div class="mt-1">
                <input type="text" id="labelColor" name="labelColor" value={labelColor} autoComplete="off" onInput={e => this.onColorChanged(e)}/>
              </div>
            </div>

            <div>
              <div class="mt-4 flex justify-end">
                <button type="button" class="btn btn-secondary" onClick={() => {}}>Cancel</button>
                <button type="submit" class="ml-3 btn btn-primary">Save</button>
              </div>
            </div>

          </div>
        </form>
      </div>
    </div>
  }

  private onSubmit = (e: Event) => {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const formData = new FormData(form);
    const labelName = formData.get('labelName') as string;
    const labelDescription = formData.get('labelDescription') as string;
    const labelColor = formData.get('labelColor') as string;

    this.createLabelClicked.emit({
      name: labelName,
      description: labelDescription,
      color: labelColor
    });

    this.labelName = '';
    this.labelDescription = '';
    this.labelColor = LabelCreator.defaultColor;
  };

  private onNameChanged = (e: Event) => {
    this.labelName = (e.target as HTMLInputElement).value;
  };

  private onColorChanged = (e: Event) => {
    this.labelColor = (e.target as HTMLInputElement).value;
  };
}
