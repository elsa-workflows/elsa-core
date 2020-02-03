import { OutcomeNames } from "../models/outcome-names";
import pluginStore from '../services/workflow-plugin-store';
export class PrimitiveActivities {
    constructor() {
        this.getName = () => "PrimitiveActivities";
        this.getActivityDefinitions = () => ([this.log(), this.setVariable()]);
        this.log = () => ({
            type: "Log",
            displayName: "Log",
            description: "Log a message.",
            category: PrimitiveActivities.Category,
            icon: 'fas fa-feather-alt',
            properties: [{
                    name: 'message',
                    type: 'text',
                    label: 'Message',
                    hint: 'The text to log.'
                },
                {
                    name: 'logLevel',
                    type: 'text',
                    label: 'Log Level',
                    hint: 'The log level to use.'
                }],
            runtimeDescription: 'x => !!x.state.message ? `Log <strong>${x.state.logLevel}: ${x.state.message}</strong>` : x.definition.description',
            outcomes: [OutcomeNames.Done]
        });
        this.setVariable = () => ({
            type: "SetVariable",
            displayName: "Set Variable",
            description: "Set a variable on the workflow.",
            category: PrimitiveActivities.Category,
            properties: [{
                    name: 'variableName',
                    type: 'text',
                    label: 'Variable Name',
                    hint: 'The name of the variable to store the value into.'
                }, {
                    name: 'expression',
                    type: 'expression',
                    label: 'Variable Expression',
                    hint: 'An expression that evaluates to the value to store in the variable.'
                }],
            runtimeDescription: 'x => !!x.state.variableName ? `${x.state.expression.syntax}: <strong>${x.state.variableName}</strong> = <strong>${x.state.expression.expression}</strong>` : x.definition.description',
            outcomes: [OutcomeNames.Done]
        });
    }
}
PrimitiveActivities.Category = "Primitives";
pluginStore.add(new PrimitiveActivities());
