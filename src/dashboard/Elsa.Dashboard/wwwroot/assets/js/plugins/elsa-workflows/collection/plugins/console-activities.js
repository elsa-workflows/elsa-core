import { OutcomeNames } from "../models/outcome-names";
import pluginStore from '../services/workflow-plugin-store';
export class ConsoleActivities {
    constructor() {
        this.getName = () => "ConsoleActivities";
        this.getActivityDefinitions = () => ([this.readLine(), this.writeLine()]);
        this.readLine = () => ({
            type: 'ReadLine',
            displayName: 'Read Line',
            description: 'Read text from standard in.',
            runtimeDescription: 'a => !!a.state.variableName ? `Read text from standard in and store into <strong>${ a.state.variableName }</strong>.` : \'Read text from standard in.\'',
            outcomes: [OutcomeNames.Done],
            category: ConsoleActivities.Category,
            icon: 'fas fa-terminal',
            properties: [{
                    name: 'variableName',
                    type: 'text',
                    label: 'Variable Name',
                    hint: 'The name of the variable to store the value into.'
                }]
        });
        this.writeLine = () => ({
            type: 'WriteLine',
            displayName: 'Write Line',
            description: 'Write text to standard out.',
            category: ConsoleActivities.Category,
            icon: 'fas fa-terminal',
            runtimeDescription: `x => !!x.state.textExpression ? \`Write <strong>\${ x.state.textExpression.expression }</strong> to standard out.\` : x.definition.description`,
            outcomes: [OutcomeNames.Done],
            properties: [{
                    name: 'textExpression',
                    type: 'expression',
                    label: 'Text',
                    hint: 'The text to write.'
                }]
        });
    }
}
ConsoleActivities.Category = "Console";
pluginStore.add(new ConsoleActivities());
