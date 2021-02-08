import {Component, Host, h, Listen, Event, EventEmitter, Prop, Watch} from '@stencil/core';
import {ActivityDescriptor, WorkflowModel} from "../../../models/domain";
import state from '../../../utils/store';

@Component({
  tag: 'elsa-studio',
  styleUrl: 'elsa-studio.css',
  shadow: false,
})
export class ElsaWorkflowStudio {

  @Prop() workflowModel: WorkflowModel = {activities: [], connections: []};
  @Prop() activityDescriptors: Array<ActivityDescriptor> = [];
  el: HTMLElement;
  designer: HTMLElsaDesignerTreeElement;

  @Watch("activityDescriptors")
  activityDescriptorsChangedHandler(newValue: Array<ActivityDescriptor>){
    state.activityDescriptors = newValue;
  }

  componentWillLoad(){
    this.activityDescriptorsChangedHandler(this.activityDescriptors);
  }

  componentDidLoad() {
    if (!this.designer) {
      this.designer = this.el.querySelector("elsa-designer-tree") as HTMLElsaDesignerTreeElement;
      this.designer.model = this.workflowModel;
    }
  }

  render() {
    return (
      <Host class="flex flex-col" ref={el => this.el = el}>
        {this.renderContentSlot()}
        {this.renderActivityPicker()}
        {this.renderActivityEditor()}
      </Host>
    );
  }

  renderContentSlot() {
    return (
      <div class="h-screen flex ">
        <elsa-designer-tree model={this.workflowModel} class="flex-1" ref={el => this.designer = el}/>
      </div>
    );
  }

  renderActivityPicker() {
    return <elsa-activity-picker-modal/>;
  }

  renderActivityEditor() {
    return <elsa-activity-editor-modal/>;
  }
}
