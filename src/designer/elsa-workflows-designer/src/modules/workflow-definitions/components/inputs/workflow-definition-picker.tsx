import {Component, h, Prop} from '@stencil/core';
import {Container} from "typedi";
import {WorkflowDefinitionSummary} from "../../models/entities";
import {ActivityInputContext} from "../../../../services/activity-input-driver";
import {getPropertyValue} from "../../../../utils";
import {ActivityInput, Expression, SyntaxNames} from "../../../../models";
import {FormEntry} from "../../../../components/shared/forms/form-entry";
import {WorkflowDefinitionsApi, WorkflowDefinitionsOrderBy} from "../../services/api";

@Component({
  tag: 'elsa-workflow-definition-picker-input',
  shadow: false
})
export class VariablePickerInput {
  @Prop() inputContext: ActivityInputContext;
  private workflowDefinitions: Array<WorkflowDefinitionSummary> = [];

  async componentWillLoad() {
    const apiClient = Container.get(WorkflowDefinitionsApi);
    const response = await apiClient.list({versionOptions: {isPublished: true}, orderBy: WorkflowDefinitionsOrderBy.Name});
    this.workflowDefinitions = response.items;
  }

  public render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const fieldName = inputDescriptor.name;
    const fieldId = inputDescriptor.name;
    const displayName = inputDescriptor.displayName;
    const description = inputDescriptor.description;
    const workflowDefinitions = this.workflowDefinitions;
    const value = getPropertyValue(inputContext) as ActivityInput;
    const currentValue = value?.expression?.value;

    return (
      <FormEntry fieldId={fieldId} label={displayName} hint={description}>
        <select id={fieldId} name={fieldName} onChange={e => this.onChange(e)}>
          {workflowDefinitions.map((definition: WorkflowDefinitionSummary) => {
            const definitionId = definition.definitionId;
            const isSelected = definitionId == currentValue;
            return <option value={definitionId} selected={isSelected}>{definition.name}</option>;
          })}
        </select>
      </FormEntry>
    );
  }

  private onChange = (e: Event) => {
    const inputElement = e.target as HTMLSelectElement;
    const definitionId = inputElement.value;
    this.inputContext.inputChanged(definitionId, SyntaxNames.Literal);
  }
}
