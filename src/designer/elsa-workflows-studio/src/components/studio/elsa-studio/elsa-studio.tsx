import {Component, Host, h, Listen, Event, EventEmitter, Prop, Watch} from '@stencil/core';
import {ActivityDescriptor, WorkflowModel} from "../../../models/domain";
import state from '../../../utils/store';

@Component({
  tag: 'elsa-studio',
  styleUrl: 'elsa-studio.css',
  shadow: false,
})
export class ElsaWorkflowStudio {

  @Prop({attribute: "override-content", reflect: true}) overrideContent: boolean = false;
  @Prop({attribute: "override-activity-picker", reflect: true}) overrideActivityPicker: boolean;
  @Prop() workflowModel: WorkflowModel = {activities: [], connections: []};
  @Prop() activityDescriptors: Array<ActivityDescriptor> = [];
  el: HTMLElement;
  designer: HTMLElsaDesignerTreeElement;

  @Watch("activityDescriptors")
  activityDescriptorsChangedHandler(newValue: Array<ActivityDescriptor>){
    state.activityDescriptors = newValue;
  }

  componentDidLoad() {
    if (!this.designer) {
      this.designer = this.el.querySelector("elsa-designer-tree") as HTMLElsaDesignerTreeElement;
      this.designer.workflowModel = this.workflowModel;
    }
  }

  render() {
    return (
      <Host class="flex flex-col" ref={el => this.el = el}>
        <slot name="content">
          {this.renderContentSlot()}
        </slot>
        <slot name="activity-picker">
          {this.renderActivityPicker()}
        </slot>
      </Host>
    );
  }

  renderContentSlot() {
    if (this.overrideContent)
      return undefined;

    return (
      <div class="h-screen flex ">
        <elsa-designer-tree workflowModel={this.workflowModel} class="flex-1" ref={el => this.designer = el}/>
      </div>
    );
  }

  renderActivityPicker() {
    if (this.overrideActivityPicker)
      return undefined;

    return <elsa-activity-picker-modal/>;
  }

}
