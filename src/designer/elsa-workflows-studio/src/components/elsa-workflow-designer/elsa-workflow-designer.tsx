import {Component, Host, h, Prop} from '@stencil/core';
import {ActivityModel, WorkflowModel} from "../../models";

@Component({
  tag: 'elsa-workflow-designer',
  styleUrls: ['elsa-workflow-designer.css'],
  shadow: false
})
export class ElsaWorkflowDesigner {

  @Prop() workflowModel: WorkflowModel = { activities: [] };

  render() {
    const renderedActivities = {};

    return (
      <Host>
        <div id="workflow-canvas" class="flex-1 flex">
          <div class="flex-1 text-gray-200">
            <div class="p-10">
              <div class="canvas select-none">
                <div class="tree">
                  <ul>
                    <li>
                      <div class="inline-flex flex flex-col items-center">
                        <button id="start-button"
                                type="button"
                                class="px-6 py-3 border border-transparent text-base leading-6 font-medium rounded-md text-white bg-green-600 hover:bg-green-500 focus:outline-none focus:border-green-700 focus:shadow-outline-green active:bg-green-700 transition ease-in-out duration-150">
                          Start
                        </button>
                      </div>
                      {this.renderTree(this.workflowModel.activities, true, renderedActivities)}
                    </li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Host>
    );
  }

  renderTree(activities: Array<ActivityModel>, root: boolean, renderedActivities: any): any
  {
    return <div>Activities go here</div>
  }
}
