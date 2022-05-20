import {Component, h, Prop, State} from "@stencil/core";
import {v4 as uuid} from 'uuid';
import {Badge} from "../../shared/badge/badge";
import {Button} from "../../shared/button-group/models";
import {Label} from "../../../models";

@Component({
  tag: 'elsa-label-editor',
  shadow: false,
})
export class LabelEditor {

  @Prop() label: Label = {id: uuid(), name: 'Preview'};
  @State() editMode: boolean = false;

  render() {
    const label = this.label;
    const editMode = this.editMode;
    const color = label.color ?? '#991b1b';
    const buttons: Array<Button> = [];

    if (!editMode) {
      buttons.push({
        text: 'Edit',
        clickHandler: e => this.editMode = true
      });
    }

    buttons.push({
      text: 'Delete',
      clickHandler: e => {
      }
    });

    const icon = <svg class="h-6 w-6 text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <circle cx="12" cy="12" r="10"/>
      <rect x="9" y="9" width="6" height="6"/>
    </svg>

    return <div>

      <div class={`py-4 grid grid-cols-4 gap-4`}>
        <div>
          <Badge text={label.name} color={color}/>
        </div>
        <div class="mt-1 text-sm text-gray-900">
          <span class="text-sm" hidden={editMode}>
            {label.description}
          </span>
        </div>
        <div>
          <span class="justify-self-center" hidden={editMode}>
            <a href="#" title="Linked to 20 workflow definitions" class="inline-flex items-center space-x-2">
              {icon} <span>20</span>
            </a>
          </span>
        </div>
        <div class="justify-self-end">
          <span>
            <elsa-button-group buttons={buttons}/>
          </span>
        </div>
      </div>

      <div hidden={!editMode}>
        <form class="pb-5 space-y-8 divide-y">
          <div class="grid grid-cols-4 gap-x-4">
            <div>
              <label htmlFor="labelName">Name</label>
              <div class="mt-1">
                <input type="text" id="labelName" name="labelName" autoComplete="off"/>
              </div>
            </div>

            <div>
              <label htmlFor="labelDescription">Description</label>
              <div class="mt-1">
                <input type="text" id="labelDescription" name="labelDescription" autoComplete="off"/>
              </div>
            </div>

            <div>
              <label htmlFor="labelColor">Color</label>
              <div class="mt-1">
                <input type="text" id="labelColor" name="labelColor" autoComplete="off"/>
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
}
