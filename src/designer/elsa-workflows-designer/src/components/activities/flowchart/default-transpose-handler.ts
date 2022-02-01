import 'reflect-metadata';
import {camelCase} from 'lodash';
import {Service} from "typedi"
import {TransposeContext, TransposeHandler, UntransposeContext, UntransposedConnection} from "./transpose-handler";
import {Activity, Port} from "../../../models";

@Service()
export class DefaultTransposeHandler implements TransposeHandler {

  transpose(context: TransposeContext): boolean {
    const {connection, source, target, sourceDescriptor} = context;
    const matchingTargetPort = sourceDescriptor.outPorts.find(x => x.name == connection.sourcePort);

    if (!matchingTargetPort)
      return false;

    // Assign the target activity directly to the outbound port of the source activity.
    const outPortPropName = camelCase(connection.sourcePort);
    source[outPortPropName] = target;

    return true;
  }

  untranspose(context: UntransposeContext): Array<UntransposedConnection> {
    const connections: Array<UntransposedConnection> = [];
    const activityDescriptor = context.activityDescriptor;
    const activity = context.activity;
    const outPorts: Array<Port> = [...activityDescriptor.outPorts];

    for (const port of outPorts) {
      const propName = camelCase(port.name);
      const source = activity;
      const target: Activity | Array<Activity> = activity[propName];
      delete activity[propName];

      if (!!target) {
        if (Array.isArray(target)) {
          for (const a of target)
            connections.push({source, target: a, sourcePort: port.name, targetPort: 'in'});
        } else
          connections.push({source, target, sourcePort: port.name, targetPort: 'in'});
      }
    }

    return connections;
  }
}
