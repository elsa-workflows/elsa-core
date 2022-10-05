import { OutcomeNames } from "../modelsCopy/outcome-names";
import { WorkflowPlugin } from "../modelsCopy";
import { ActivityDefinitionCopy } from "../modelsCopy";
import pluginStore from '../servicesCopy/workflow-plugin-store';

export class TimerActivities implements WorkflowPlugin {
  private static readonly Category: string = "Timers";

  getName = (): string => "TimerActivities";
  getActivityDefinitions = (): Array<ActivityDefinitionCopy> => ([this.timerEvent()]);

  private timerEvent = (): ActivityDefinitionCopy => ({
    type: "TimerEvent",
    displayName: "Timer Event",
    description: "Triggers after a specified amount of time.",
    category: TimerActivities.Category,
    icon: 'fas fa-hourglass-start',
    properties: [
      {
        name: 'expression',
        type: 'expression',
        label: 'Timeout Expression',
        hint: 'The amount of time to wait before this timer event is triggered. Format: \'d.HH:mm:ss\'.'
      },
      {
        name: 'name',
        type: 'text',
        label: 'Name',
        hint: 'Optionally provide a name for this activity. You can reference named activities from expressions.'
      },
      {
        name: 'title',
        type: 'text',
        label: 'Title',
        hint: 'Optionally provide a custom title for this activity.'
      },
      {
        name: 'description',
        type: 'text',
        label: 'Description',
        hint: 'Optionally provide a custom description for this activity.'
      }],
    runtimeDescription: 'x => !!x.state.expression ? `Triggers after <strong>${ x.state.expression.expression }</strong>` : x.definition.description',
    outcomes: [OutcomeNames.Done]
  });
}

pluginStore.add(new TimerActivities());
