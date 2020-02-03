import { OutcomeNames } from "../models/outcome-names";
import pluginStore from '../services/workflow-plugin-store';
export class TimerActivities {
    constructor() {
        this.getName = () => "TimerActivities";
        this.getActivityDefinitions = () => ([this.timerEvent()]);
        this.timerEvent = () => ({
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
                }
            ],
            runtimeDescription: 'x => !!x.state.expression ? `Triggers after <strong>${ x.state.expression.expression }</strong>` : x.definition.description',
            outcomes: [OutcomeNames.Done]
        });
    }
}
TimerActivities.Category = "Timers";
pluginStore.add(new TimerActivities());
