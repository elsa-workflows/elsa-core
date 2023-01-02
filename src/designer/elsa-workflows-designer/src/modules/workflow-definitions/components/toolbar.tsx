import {Component, h, Prop, Event, EventEmitter} from '@stencil/core';
import {DropdownButtonItem} from "../../../components/shared/dropdown-button/models";
import {LayoutDirection} from "../../flowchart/models";

@Component({
  tag: 'elsa-workflow-definition-editor-toolbar',
})
export class Toolbar {
  @Prop()
  public zoomToFit: () => Promise<void>;

  @Event()
  public autoLayout: EventEmitter<LayoutDirection>;

  render() {

    const layoutButtons: Array<DropdownButtonItem> = [{
      text: 'Horizontally',
      handler: () => this.autoLayout.emit('LR')
    },{
      text: 'Vertically',
      handler: () => this.autoLayout.emit('TB')
    }];

    return (
      <div class="elsa-panel-toolbar flex justify-center absolute border-b border-gray-200 top-0 px-1 pl-4 pb-2 text-sm bg-white z-10 space-x-2">
        <elsa-dropdown-button text="Auto-layout" theme="Primary" items={layoutButtons}/>
        <button onClick={this.zoomToFit}class="btn btn-primary">
          Zoom to fit
        </button>
      </div>
    );
  }
}
