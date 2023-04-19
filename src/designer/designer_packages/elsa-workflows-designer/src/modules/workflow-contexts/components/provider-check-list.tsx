import {Component, EventEmitter, h, Prop, State, Event} from '@stencil/core';
import {FormEntry} from "../../../components/shared/forms/form-entry";
import {SelectList, Type} from "../../../models";
import {WorkflowContextProviderDescriptor} from "../services/api";
import {WorkflowDefinition} from "../../workflow-definitions/models/entities";
import {WorkflowContextProviderTypesKey} from "../constants";

@Component({
  tag: 'elsa-workflow-context-provider-check-list',
  shadow: false
})
export class ProviderCheckList {
  @Prop() descriptors: Array<WorkflowContextProviderDescriptor> = [];
  @Prop() workflowDefinition: WorkflowDefinition;
  @Event() workflowDefinitionChanged: EventEmitter<WorkflowDefinition>;
  @State() selectList: SelectList = {items: []};
  @State() selectedProviderTypes: Array<Type> = [];

  public async componentWillLoad() {
    const workflowDefinition = this.workflowDefinition;
    const selectedProviderTypes: Array<Type> = workflowDefinition?.customProperties[WorkflowContextProviderTypesKey] ?? [];
    const selectListItems = this.descriptors.map(x => ({text: x.name, value: x.type}));

    this.selectList = {
      items: selectListItems
    };

    this.selectedProviderTypes = selectedProviderTypes;
  }

  render() {
    const selectList = this.selectList;

    return <FormEntry label="Active providers" fieldId="EnabledProviders" hint="Select the providers to activate for this workflow.">
      <elsa-check-list selectList={selectList} selectedValues={this.selectedProviderTypes} onSelectedValuesChanged={this.onSelectedProvidersChanged}></elsa-check-list>
    </FormEntry>
  }

  private onSelectedProvidersChanged = (e: CustomEvent<Array<string> | number>) => {
    const selectedProviderTypes = e.detail as Array<string>;
    this.selectList = {items: this.selectList.items.map(x => ({...x, selected: selectedProviderTypes.includes(x.value)}))};
    this.workflowDefinition.customProperties[WorkflowContextProviderTypesKey] = selectedProviderTypes;
    this.workflowDefinitionChanged.emit(this.workflowDefinition);
  };
}
