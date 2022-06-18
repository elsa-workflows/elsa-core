import {Component, FunctionalComponent, h, Prop} from "@stencil/core";
import {FlowchartIcon, IfIcon} from "../../icons/activities";
import {Activity} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {Container} from "typedi";
import {ActivityIconRegistry} from "../../../services";
import {WorkflowNavigationItem} from "./models";

@Component({
  tag: 'elsa-workflow-navigator',
  shadow: false
})
export class WorkflowNavigator {

  @Prop() items: Array<WorkflowNavigationItem> = [];

  render() {

    const items = this.items;

    if (items.length <= 1)
      return null;

    return <div class="ml-8">
      <nav class="flex" aria-label="Breadcrumb">
        <ol role="list" class="flex items-center space-x-3">
          {items.map(activity => <ActivityPathItem item={activity}/>)}
        </ol>
      </nav>
    </div>
  }
}

interface ActivityPathItemProps {
  item: WorkflowNavigationItem;
}

const ActivityPathItem: FunctionalComponent<ActivityPathItemProps> = ({item}) => {
  const iconRegistry = Container.get(ActivityIconRegistry);
  const activity = item.activity;
  const icon = iconRegistry.get(activity.typeName)();
  const activityId = activity.id;

  const listElements = [];

  listElements.push(
    <li>
      <div class="flex items-center">
        <a href="#" class="block flex items-center text-gray-400 hover:text-gray-500">
          <div class="bg-blue-500 rounded">
            {icon}
          </div>
          <span class="ml-4 text-sm font-medium text-gray-500 hover:text-gray-700">{activityId}</span>
        </a>
        <svg class="ml-2 flex-shrink-0 h-5 w-5 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
        </svg>
      </div>
    </li>
  );

  if (!!item.port) {
    listElements.push(
      <li>
        <div class="flex items-center">
          <span class="text-sm font-medium text-gray-500" aria-current="page">{item.port.displayName}</span>
        </div>
      </li>
    );
  }

  return listElements;
}
