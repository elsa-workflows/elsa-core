import 'reflect-metadata';
import {Container} from "typedi"
import {camelCase} from "lodash";
import {Activity, ActivityDescriptor} from "../../../models";
import {PortProviderRegistry} from "./port-provider-registry";
import {TransposeHandlerRegistry} from "./transpose-handler-registry";

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
  // Create a copy of the root to avoid transposing its outbound properties.
  const rootCopy = {...root};
  
  const collectedActivities = new Set<Activity>([rootCopy]);
  const graph: ActivityNode = {activity: root, parents: [], children: []};
  const collectedNodes = new Set<ActivityNode>([graph]);
  walkRecursive(graph, rootCopy, collectedActivities, collectedNodes, descriptors);
  return graph;
}

export function flatten(root: ActivityNode): Array<ActivityNode> {
  return flattenList([root]);
}

export function flattenList(activities: Array<ActivityNode>): Array<ActivityNode> {
  const list: Array<ActivityNode> = [...activities];

  for (const activity of activities) {
    for (const child of activity.children)
      list.push(child);
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

    childNode.parents.push(node);
    node.children.push(childNode);
    collectedActivities.add(port.activity);
    walkRecursive(childNode, port.activity, collectedActivities, collectedNodes, descriptors);
  }
}

function getPorts(node: ActivityNode, activity: Activity, descriptors: Array<ActivityDescriptor>): Array<ActivityPort> {
  const portProviderRegistry = Container.get(PortProviderRegistry);
  const transposeHandlerRegistry = Container.get(TransposeHandlerRegistry);
  const transposeHandler = transposeHandlerRegistry.get(activity.typeName);
  const portProvider = portProviderRegistry.get(activity.typeName);
  const activityDescriptor = descriptors.find(x => x.activityType == activity.typeName);
  debugger;
  const untransposedConnections = transposeHandler.untranspose({activity, activityDescriptor});
  return untransposedConnections.map(x => ({activity: x.target, port: x.sourcePort}));
}
