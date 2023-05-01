import {Component, FunctionalComponent, h, Prop, Event, EventEmitter} from "@stencil/core";
import {Container} from "typedi";
import {ActivityIconRegistry, ActivityNode, flatten, PortProviderRegistry, walkActivities} from "../../services";
import {Flowchart, FlowchartNavigationItem, FlowchartPathItem} from "./models";
import {Activity, ActivityDescriptor, Port} from "../../models";
import descriptorsStore from "../../data/descriptors-store";
import {WorkflowDefinition} from "../workflow-definitions/models/entities";
import {FlowchartIcon} from "../../components/icons/activities";

@Component({
  tag: 'elsa-workflow-navigator',
  shadow: false
})
export class WorkflowNavigator {
  private readonly iconRegistry: ActivityIconRegistry;
  private readonly portProviderRegistry: PortProviderRegistry;

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  @Prop() items: Array<FlowchartPathItem> = [];
  @Prop() rootActivity: Activity;

  @Event() navigate: EventEmitter<FlowchartPathItem>;

  render() {

    let items = this.items;

    if (items.length <= 0)
      return null;

    if (!this.rootActivity)
      return;

    const nodes = flatten(walkActivities(this.rootActivity));

    return <div class="ml-8">
      <nav class="flex" aria-label="Breadcrumb">
        <ol role="list" class="flex items-center space-x-3">
          {items.length > 0 && (
            <li>
              <div class="flex items-center">
                <a onClick={e => this.onItemClick(null)}
                   href="#"
                   class="block flex items-center text-gray-400 hover:text-gray-500">
                  <div class="bg-blue-500 rounded">
                    {<FlowchartIcon/>}
                  </div>
                  <span class="ml-4 text-sm font-medium text-gray-500 hover:text-gray-700">{this.rootActivity.id}</span>
                </a>
                <svg class="ml-2 flex-shrink-0 h-5 w-5 text-gray-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                  <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
                </svg>
              </div>
            </li>
          )}
          {items.map((item, index) => this.renderPathItem(item, index, nodes))}
        </ol>
      </nav>
    </div>
  }

  private renderPathItem = (item: FlowchartPathItem, index: number, nodes: Array<ActivityNode>) => {
    const activityId = item.activityId;
    const activity = nodes.find(x => x.activity.id == activityId).activity;
    const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == activity.type);
    const icon = this.iconRegistry.getOrDefault(activity.type)();
    const listElements = [];
    const isLastItem = index == this.items.length - 1;

    const onItemClick = (e: MouseEvent, item: FlowchartPathItem) => {
      e.preventDefault();
      this.onItemClick(item);
    }

    let port: Port = null;

    if (!!item.portName) {
      const portProvider = this.portProviderRegistry.get(activity.type);
      const ports = portProvider.getOutboundPorts({activity, activityDescriptor});

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

  private onItemClick = (item: FlowchartPathItem) => this.navigate.emit(item);
}
