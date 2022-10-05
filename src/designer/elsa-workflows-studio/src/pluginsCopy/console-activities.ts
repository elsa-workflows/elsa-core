import { OutcomeNames } from "../modelsCopy/outcome-names";
import { WorkflowPlugin } from "../modelsCopy";
import { ActivityDefinitionCopy } from "../modelsCopy";
import pluginStore from '../servicesCopy/workflow-plugin-store';

export class ConsoleActivities implements WorkflowPlugin {
  private static readonly Category: string = "Console";

  getName = (): string => "ConsoleActivities";
  getActivityDefinitions = (): Array<ActivityDefinitionCopy> => ([this.readLine(), this.writeLine()]);

  private readLine = (): ActivityDefinitionCopy => ({
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

  private writeLine = (): ActivityDefinitionCopy => ({
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

pluginStore.add(new ConsoleActivities());
