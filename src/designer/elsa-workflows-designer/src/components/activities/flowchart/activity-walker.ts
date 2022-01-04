import _, {camelCase} from "lodash";
import {Activity, ActivityDescriptor, Container} from "../../../models";

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
  const descriptor = descriptors.find(x => x.nodeType == activity.nodeType);

  if (!descriptor)
    return [];

  let ports: Array<ActivityPort> = [];

  for (const outPort of descriptor.outPorts) {
    const outPortName = _.camelCase(outPort.name);
    const outbound: Activity | Array<Activity> = activity[outPortName];

    if (!!outbound) {
      if (Array.isArray(outbound)) {
        for (const a of outbound)
          ports.push({activity: a, port: outPort.name});
      } else
        ports.push({activity: outbound, port: outPort.name});
    }
  }

  return ports;
}
