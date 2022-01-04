import {Graph, Node} from '@antv/x6';
import {Activity, ActivityDescriptor, Port} from '../../../models';

export function createNode(activityDescriptor: ActivityDescriptor, activity: Activity, x: number, y: number): Node.Metadata {

  let inPorts: Array<Port> = [...activityDescriptor.inPorts];
  let outPorts: Array<Port> = [...activityDescriptor.outPorts];

  if (inPorts.length == 0)
    inPorts = [{name: 'In', displayName: 'In'}];

  if (inPorts.length == 1)
    inPorts[0].displayName = null;

  if (outPorts.length == 0)
    outPorts = [{name: 'Done', displayName: 'Done'}];

  if (outPorts.length == 1)
    outPorts[0].displayName = null;

  const inPortModels = inPorts.map(x => ({
    id: x.name,
    group: 'in',
    attrs: !!x.displayName ? {
      text: {
        text: x.displayName
      }
    } : null
  }));

  const outPortModels = outPorts.map(x => ({
    id: x.name,
    group: 'out',
    attrs: {
      text: {
        text: x.displayName
      }
    }
  }));

  const portModels = [...inPortModels, ...outPortModels];

  return {
    id: activity.id,
    shape: 'activity',
    activity: activity,
    activityDescriptor: activityDescriptor,
    x: x,
    y: y,
    data: activity,
    ports: portModels
  } as Node.Metadata;
}
