import 'reflect-metadata';
import {Service} from "typedi"
import {TransposeContext, TransposeHandler, UntransposeContext, UntransposedConnection} from "../../components/activities/flowchart/transpose-handler";
import {SwitchActivity} from "./models";

@Service()
export class SwitchTransposeHandler implements TransposeHandler {

  transpose(context: TransposeContext): boolean {
    const {connection, source, target, sourceDescriptor} = context;
    const switchActivity = source as SwitchActivity;
    const cases = switchActivity.cases;
    const matchingCase = cases.find(x => x.label == connection.sourcePort);

    if (!matchingCase)
      return false;

    // Assign the target activity directly to the switch case.
    matchingCase.activity = target;

    return true;
  }

  untranspose(context: UntransposeContext): Array<UntransposedConnection> {
    const activity = context.activity as SwitchActivity;
    const connections: Array<UntransposedConnection> = [];

    for (const switchCase of activity.cases) {
      const target = switchCase.activity;
      delete switchCase.activity;

      if (!!target)
        connections.push({source: activity, sourcePort: switchCase.label, target: target, targetPort: 'in'});
    }

    return connections;
  }
}
