import 'reflect-metadata';
import {camelCase} from 'lodash';
import {Container} from "typedi"
import {Activity, ActivityDescriptor} from "../../../models";
import {PortProviderRegistry} from "./port-provider-registry";

export interface ActivityNode {
  activity: Activity;
  parents: Array<ActivityNode>;
  children: Array<ActivityNode>;
  port?: string;
}

export interface ActivityPort {
  activity: Activity;
  port: string;
}

export function walkActivities(root: Activity, descriptors: Array<ActivityDescriptor>): ActivityNode {
  const collectedActivities = new Set<Activity>([root]);
  const graph: ActivityNode = {activity: root, parents: [], children: []};
  const collectedNodes = new Set<ActivityNode>([graph]);
  walkRecursive(graph, root, collectedActivities, collectedNodes, descriptors);
  return graph;
}

export function flatten(root: ActivityNode): Array<ActivityNode> {
  return flattenList([root]);
}

export function flattenList(activities: Array<ActivityNode>): Array<ActivityNode> {
  let list: Array<ActivityNode> = [...activities];

  for (const activity of activities) {
    const childList = flattenList(activity.children);
    list = [...list, ...childList];
  }

  return list;
}

function walkRecursive(node: ActivityNode, activity: Activity, collectedActivities: Set<Activity>, collectedNodes: Set<ActivityNode>, descriptors: Array<ActivityDescriptor>) {
  const ports = getPorts(node, activity, descriptors);

  for (const port of ports) {
    const collectedNodesArray = Array.from(collectedNodes);
    let childNode = collectedNodesArray.find(x => x.activity == port.activity);

    if (!childNode) {
      childNode = {activity: port.activity, children: [], parents: [], port: port.port};
      collectedNodes.add(childNode);
    }

    if (childNode !== node) {
      childNode.parents.push(node);
      node.children.push(childNode);
      collectedActivities.add(port.activity);
      walkRecursive(childNode, port.activity, collectedActivities, collectedNodes, descriptors);
    }
  }
}

function getPorts(node: ActivityNode, activity: Activity, descriptors: Array<ActivityDescriptor>): Array<ActivityPort> {
  const portProviderRegistry = Container.get(PortProviderRegistry);
  const portProvider = portProviderRegistry.get(activity.typeName);
  const activityDescriptor = descriptors.find(x => x.activityType == activity.typeName);
  const ports = portProvider.getOutboundPorts({activity, activityDescriptor});
  let activityPorts: Array<ActivityPort> = [];

  for (const port of ports) {
    const propName = camelCase(port.name);
    const value = activity[propName];

    if (!value)
      continue;

    if (Array.isArray(value)) {
      const activities = value as Array<Activity>;
      activityPorts = [...activityPorts, ...activities.map(x => ({port: port.name, activity: x}))];
    } else {
      activityPorts.push({port: port.name, activity: value});
    }
  }

  return activityPorts;
}
