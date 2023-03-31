import 'reflect-metadata';
import {camelCase} from 'lodash';
import {Container} from "typedi"
import {Activity, ActivityDescriptor} from "../models";
import {PortProviderRegistry} from "./port-provider-registry";
import descriptorsStore from '../data/descriptors-store';
import {Hash} from "../utils";
import {PortProviderContext} from "./port-provider";

export interface ActivityNode {
  activity: Activity;
  parents: Array<ActivityNode>;
  children: Array<ActivityNode>;
  port?: string;
  nodeId: string;

  descendants(): Array<ActivityNode>;

  ancestors(): Array<ActivityNode>;

  siblings(): Array<ActivityNode>;

  siblingsAndCousins(): Array<ActivityNode>;
}

export interface ActivityPort {
  activity: Activity;
  port: string;
}

export class ActivityNodeClass implements ActivityNode {
  private _activity: Activity;
  private _parents: Array<ActivityNode>;
  private _children: Array<ActivityNode>;

  constructor(activity) {
    this.activity = activity;
    this.parents = [];
    this.children = [];
  }

  get nodeId() {
    const ancestorIds = [...this.ancestors()].reverse().map(x => x.activity.id);
    return ancestorIds.length ? `${ancestorIds.join(":")}:${this.activity.id}` : this.activity.id;
  }

  get activity() {
    return this._activity;
  }

  set activity(value) {
    this._activity = value;
  }

  get parents() {
    return this._parents;
  }

  set parents(value) {
    this._parents = value;
  }

  get children() {
    return this._children;
  }

  set children(value) {
    this._children = value;
  }

  descendants() {
    const list = [];

    for (let child of this.children) {
      list.push(child);
      list.push(...child.descendants());
    }

    return list;
  }

  ancestors() {
    const list = [];

    for (let parent of this.parents) {
      list.push(parent);
      list.push(...parent.ancestors());
    }

    return list;
  }

  siblings() {
    return this.parents.flatMap(parent => parent.children);
  }

  siblingsAndCousins() {
    return this.parents.flatMap(parent => parent.descendants().flatMap(x => x.children));
  }
}


export function createActivityLookup(nodes: Array<ActivityNode>): Hash<Activity> {
  const map = {};

  for (const node of nodes)
    map[node.activity.id] = node.activity;

  return map;
}

export function createActivityNodeMap(nodes: Array<ActivityNode>): Hash<ActivityNode> {
  const map = {};

  for (const node of nodes)
    map[node.activity.id] = node;

  return map;
}

export function walkActivities(root: Activity): ActivityNode {
  const descriptors = descriptorsStore.activityDescriptors;
  const collectedActivities = new Set<Activity>([root]);
  const graph: ActivityNode = new ActivityNodeClass(root);
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
      childNode = new ActivityNodeClass(port.activity);
      childNode.port = port.port;
      //childNode = {activity: port.activity, children: [], parents: [], port: port.port};

      if (!collectedNodes.has(childNode))
        collectedNodes.add(childNode);
    }

    if (childNode !== node) {
      if (!!childNode.activity) {
        if (!childNode.parents.includes(node))
          childNode.parents.push(node);

        if (!node.children.includes(childNode))
          node.children.push(childNode);

        if (!collectedActivities.has(port.activity))
          collectedActivities.add(port.activity);

        walkRecursive(childNode, port.activity, collectedActivities, collectedNodes, descriptors);
      }
    }
  }
}

function getPorts(node: ActivityNode, activity: Activity, descriptors: Array<ActivityDescriptor>): Array<ActivityPort> {
  const portProviderRegistry = Container.get(PortProviderRegistry);
  const portProvider = portProviderRegistry.get(activity.type);
  const activityDescriptor = descriptors.find(x => x.typeName == activity.type);
  const ports = portProvider.getOutboundPorts({activity, activityDescriptor});
  let activityPorts: Array<ActivityPort> = [];

  const portProviderContext: PortProviderContext = {
    activityDescriptor,
    activity
  };

  for (const port of ports) {
    const value = portProvider.resolvePort(port.name, portProviderContext);

    if (Array.isArray(value)) {
      const activities = value as Array<Activity>;
      activityPorts = [...activityPorts, ...activities.map(x => ({port: port.name, activity: x}))];
    } else {
      activityPorts.push({port: port.name, activity: value});
    }
  }

  return activityPorts;
}
