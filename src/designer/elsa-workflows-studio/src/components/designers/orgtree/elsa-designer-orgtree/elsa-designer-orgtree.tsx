import {Component, Host, h, Prop} from '@stencil/core';
import {ActivityModel, WorkflowModel} from '../../../../models';
import {getChildActivities} from '../../../../utils/utils';
import {updateConnections} from '../../../../utils/jsplumb-helper';

@Component({
  tag: 'elsa-designer-orgtree',
  styleUrls: ['elsa-designer-orgtree.css'],
  assetsDirs: ['assets'],
  shadow: false
})
export class ElsaWorkflowDesigner {

  @Prop() workflowModel: WorkflowModel = {activities: [], connections: []}
  canvasElement: HTMLElement;

  render() {
    const renderedActivities = new Set<string>();

    return (
      <Host>
        <div class="workflow-canvas flex-1 flex">
          <div class="flex-1 text-gray-200">
            <div class="p-10">
              <div class="canvas select-none" ref={el => this.canvasElement = el}>
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
                      {this.renderTree(this.getRootActivities(), true, renderedActivities)}
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

  componentDidRender() {
    const canvasElement = this.canvasElement;
    const connections = this.getJsPlumbConnections();
    const sourceEndpoints = this.getJsPlumbSourceEndpoints();
    const targets = this.getJsPlumbTargets();

    updateConnections(canvasElement, connections, sourceEndpoints, targets);
  }

  getRootActivities(): Array<ActivityModel> {
    return getChildActivities(this.workflowModel, null);
  }

  renderTree(activities: Array<ActivityModel>, isRoot: boolean, renderedActivities: Set<string>): any {
    const list = activities.filter(x => !renderedActivities.has(x.activityId));
    const cssClass = isRoot ? "root" : undefined;

    for (const activity of list)
      renderedActivities.add(activity.activityId);

    if (list.length == 0)
      return null;

    return (
      <ul class={cssClass}>
        {activities.map(x => {
          const activityId = x.activityId;
          const children = getChildActivities(this.workflowModel, activityId);

          return <li key={x.activityId}>
            <div class="inline-flex flex flex-col items-center">
              {isRoot ? this.renderOutcomeButton(`start-button-plus-${x.activityId}`, '') : undefined}
              {this.renderActivity(x)}
              {this.renderOutcomeButtons(x)}
            </div>
            {children.length > 0 ? this.renderTree(children, false, renderedActivities) : undefined}
          </li>;
        })}
      </ul>
    );
  }

  renderOutcomeButtons(activity: ActivityModel) {
    const activityId = activity.activityId;

    return (
      <div class="flex flex-row space-x-6">
        {activity.outcomes.map(x => this.renderOutcomeButton(`${activityId}-${x}`, x))}
      </div>
    );
  }

  renderOutcomeButton(id: string, outcome: string) {
    return (
      <div class="my-6 flex flex-col items-center">
        {outcome && outcome.length > 0 ? <div
          class="mb-4 relative z-10 px-2.5 py-0.5 rounded-full text-xs font-medium leading-4 bg-cool-gray-100 text-cool-gray-800 capitalize">{outcome}</div> : undefined}
        <a key={id} id={id} href="#">
          <svg class="h-8 w-8 text-gray-400 hover:text-blue-500 cursor-pointer" fill="none" viewBox="0 0 24 24"
               stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </a>
      </div>
    );
  }

  renderActivity(activity: ActivityModel) {
    // return <elsa-designer-orgtree-activity activityModel={activity}><p slot="body">Hello Slots!</p></elsa-designer-orgtree-activity>;
    return <elsa-designer-orgtree-activity activityModel={activity}/>;
  }

  getJsPlumbConnections(): Array<any> {
    const rootActivities = getChildActivities(this.workflowModel, null);

    const rootConnections = rootActivities.flatMap(x => {
      return [
        {
          sourceId: `start-button`,
          sourceActivityId: undefined,
          targetId: `start-button-plus-${x.activityId}`,
          targetActivityId: x.activityId,
          outcome: undefined
        },
        {
          sourceId: `start-button-plus-${x.activityId}`,
          sourceActivityId: undefined,
          targetId: `activity-${x.activityId}`,
          targetActivityId: x.activityId,
          outcome: undefined
        }]
    });

    const sourceConnections = this.workflowModel.activities.flatMap(activity => activity.outcomes.map(x =>
      ({
        sourceId: `activity-${activity.activityId}`,
        sourceActivityId: activity.activityId,
        targetId: `${activity.activityId}-${x}`,
        targetActivityId: undefined,
        outcome: x
      })));

    const connections = this.workflowModel.connections.flatMap(x => [
      {
        sourceId: `${x.sourceId}-${x.outcome}`,
        sourceActivityId: x.sourceId,
        targetId: `activity-${x.targetId}`,
        targetActivityId: x.targetId,
        outcome: x.outcome
      }]
    );

    return [...rootConnections, ...connections, ...sourceConnections];
  }

  getJsPlumbSourceEndpoints(): Array<any> {
    const rootActivities = getChildActivities(this.workflowModel, null);

    const rootSourceEndpoints = rootActivities.map(x =>
      ({
        sourceId: `start-button-plus-${x.activityId}`,
        sourceActivityId: x.activityId,
        outcome: undefined
      }));

    const otherSourceEndpoints = this.workflowModel.activities.flatMap(activity => activity.outcomes.map(x =>
      ({
        sourceId: `${activity.activityId}-${x}`,
        sourceActivityId: activity.activityId,
        outcome: x
      })));

    return [...rootSourceEndpoints, ...otherSourceEndpoints];
  }

  getJsPlumbTargets() {
    return this.workflowModel.activities.map(x =>
      ({
          targetId: `activity-${x.activityId}`,
          targetActivityId: x.activityId
        }
      ));
  }
}
