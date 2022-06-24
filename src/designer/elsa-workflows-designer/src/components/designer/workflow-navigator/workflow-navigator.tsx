import {Component, FunctionalComponent, h, Prop, Event, EventEmitter} from "@stencil/core";
import {Container} from "typedi";
import {ActivityIconRegistry, ActivityNode, flatten, walkActivities} from "../../../services";
import {WorkflowNavigationItem} from "./models";
import {Port, WorkflowDefinition} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";

@Component({
  tag: 'elsa-workflow-navigator',
  shadow: false
})
export class WorkflowNavigator {

  @Prop() items: Array<WorkflowNavigationItem> = [];
  @Prop() workflowDefinition: WorkflowDefinition;
  @Event() navigate: EventEmitter<WorkflowNavigationItem>;

  render() {

    const items = this.items;

    if (items.length <= 1)
      return null;

    if (!this.workflowDefinition)
      return;
    
    const nodes = flatten(walkActivities(this.workflowDefinition.root));

    return <div class="ml-8">
      <nav class="flex" aria-label="Breadcrumb">
        <ol role="list" class="flex items-center space-x-3">
          {items.map((item, index) => this.renderPathItem(item, index, nodes))}
        </ol>
      </nav>
    </div>
  }

  private renderPathItem = (item: WorkflowNavigationItem, index: number, nodes: Array<ActivityNode>) => {
    const iconRegistry = Container.get(ActivityIconRegistry);
    const activityId = item.activityId;
    const activity = nodes.find(x => x.activity.id == activityId).activity;
    const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == activity.typeName);
    const icon = iconRegistry.get(activity.typeName)();
    const listElements = [];
    const isLastItem = index == this.items.length - 1;

    const onItemClick = (e: MouseEvent, item: WorkflowNavigationItem) => {
      e.preventDefault();
      this.onItemClick(item);
    }

    let port: Port = null;

    if (!!item.portName) {
      const ports = activityDescriptor.ports;

      if (ports.length > 1)
        port = ports.find(x => x.name == item.portName);
    }

    if (isLastItem) {
      listElements.push(
        <li>
          <div class="flex items-center">
            <span class="block flex items-center text-gray-500">
              <div class="bg-blue-500 rounded">
                {icon}
              </div>
              <span class="ml-4 text-sm font-medium text-gray-500">{activityId}</span>
            </span>
            {!!port ? (
              <svg class="ml-2 flex-shrink-0 h-5 w-5 text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
              </svg>
            ) : undefined}
          </div>
        </li>
      );
    } else {
      listElements.push(
        <li>
          <div class="flex items-center">
            <a onClick={e => onItemClick(e, item)}
               href="#"
               class="block flex items-center text-gray-400 hover:text-gray-500">
              <div class="bg-blue-500 rounded">
                {icon}
              </div>
              <span class="ml-4 text-sm font-medium text-gray-500 hover:text-gray-700">{activityId}</span>
            </a>
            <svg class="ml-2 flex-shrink-0 h-5 w-5 text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
            </svg>
          </div>
        </li>
      );
    }

    if (!!port) {
      listElements.push(
        <li>
          <div class="flex items-center">
            <span class="text-sm font-medium text-gray-500" aria-current="page">{port.displayName}</span>
          </div>
        </li>
      );
    }

    return listElements;
  }

  private onItemClick = (item: WorkflowNavigationItem) => this.navigate.emit(item);
}
