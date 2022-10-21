import 'reflect-metadata';
import {camelCase} from 'lodash';
import {Container} from "typedi"
import {ActivityModel, ActivityDescriptor} from "../models";
import {PortProviderRegistry} from "./port-provider-registry";
import descriptorsStore from '../data/descriptors-store';
import {Hash} from "../utils/utils";
import {PortProviderContext} from "./port-provider";

export interface ActivityNode {
  activity: ActivityModel;
  parents: Array<ActivityNode>;
  children: Array<ActivityNode>;
  port?: string;
}

export interface ActivityPort {
  activity: ActivityModel;
  port: string;
}

export function createActivityLookup(nodes: Array<ActivityNode>): Hash<ActivityModel> {
  const map = {};

  for (const node of nodes)
    map[node.activity.activityId] = node.activity;

  return map;
}

export function createActivityNodeMap(nodes: Array<ActivityNode>): Hash<ActivityNode> {
  const map = {};

  for (const node of nodes)
    map[node.activity.activityId] = node;

  return map;
}

export function walkActivities(root: ActivityModel): ActivityNode {
  const descriptors = descriptorsStore.activityDescriptors;
  const collectedActivities = new Set<ActivityModel>([root]);
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

export function findActivity(root: ActivityModel, predicate: (activity: ActivityModel) => boolean): ActivityModel {
  const graph = walkActivities(root);
  const nodes = this.nodes;
  return nodes.find(node => predicate(node.activity))?.activity;
}

function walkRecursive(node: ActivityNode, activity: ActivityModel, collectedActivities: Set<ActivityModel>, collectedNodes: Set<ActivityNode>, descriptors: Array<ActivityDescriptor>) {
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

function getPorts(node: ActivityNode, activity: ActivityModel, descriptors: Array<ActivityDescriptor>): Array<ActivityPort> {
  const portProviderRegistry = Container.get(PortProviderRegistry);
  const portProvider = portProviderRegistry.get(activity.type);
  const activityDescriptor = descriptors.find(x => x.type == activity.type);
  const ports = portProvider.getOutboundPorts({activity, activityDescriptor});
  let activityPorts: Array<ActivityPort> = [];

  const portProviderContext: PortProviderContext = {
    activityDescriptor,
    activity
  };

  for (const port of ports) {
    const value = portProvider.resolvePort(port.name, portProviderContext);

    if (!value)
      continue;

    if (Array.isArray(value)) {
      const activities = value as Array<ActivityModel>;
      activityPorts = [...activityPorts, ...activities.map(x => ({port: port.name, activity: x}))];
    } else {
      activityPorts.push({port: port.name, activity: value});
    }
  }

  return activityPorts;
}
