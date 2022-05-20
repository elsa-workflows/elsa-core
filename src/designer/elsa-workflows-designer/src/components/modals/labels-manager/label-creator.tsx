import {Component, h, Prop, State} from "@stencil/core";
import {v4 as uuid} from 'uuid';
import {Badge} from "../../shared/badge/badge";
import {Button} from "../../shared/button-group/models";
import {Label} from "../../../models";

@Component({
  tag: 'elsa-label-creator',
  shadow: false,
})
export class LabelCreator {

  @State() labelName: string = 'Preview';
  @State() labelColor: string = '#991b1b';
  @State() labelDescription: string = '';

  render() {

    const labelName = this.labelName;
    const labelColor = this.labelColor;
    const labelDescription = this.labelDescription;

    const buttons = [{
      text: 'Cancel'
    }, {
      text: 'Create label'
    }];

    return <div>

      <div>
        <Badge text={labelName} color={labelColor}/>
      </div>

      <div>
        <form class="pb-5 space-y-8 divide-y">
          <div class="grid grid-cols-4 gap-x-4">
            <div>
              <label htmlFor="labelName">Name</label>
              <div class="mt-1">
                <input type="text" id="labelName" name="labelName" value={labelName} autoComplete="off"/>
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
                <input type="text" id="labelColor" name="labelColor" value={labelColor} autoComplete="off"/>
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
}
