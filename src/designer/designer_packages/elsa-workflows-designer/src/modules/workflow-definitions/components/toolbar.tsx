import {Component, h, Prop, Event, EventEmitter} from '@stencil/core';
import {DropdownButtonItem} from "../../../components/shared/dropdown-button/models";
import {LayoutDirection} from "../../flowchart/models";
import {PlayButtonIcon} from "../../../components/icons/buttons/play";

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
      <div class="elsa-panel-toolbar tw-flex tw-justify-center tw-absolute tw-border-b tw-border-gray-200 tw-top-0 tw-px-1 tw-pl-4 tw-pb-2 tw-text-sm tw-bg-white tw-z-10 tw-space-x-2">
        <elsa-dropdown-button text="Auto-layout" theme="Primary" items={layoutButtons} class="tw-mt-2"/>
        <button onClick={this.zoomToFit} class="elsa-btn elsa-btn-primary">
          Zoom to fit
        </button>
        {/*Coming soon...*/}
        {/*<button class="elsa-btn elsa-btn-action disabled" disabled={true}>*/}
        {/*  <PlayButtonIcon/> Run*/}
        {/*</button>*/}
      </div>
    );
  }
}
