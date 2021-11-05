import {Component, Event, EventEmitter, h, Listen, Prop, State} from '@stencil/core';
import { isThisTypeNode } from 'typescript';
import { eventBus } from '../../../..';
import { SyntaxNames, EventTypes} from "../../../../models";
import { parseJson } from "../../../../utils/utils";
import { Validators } from '../../../../validation/validator.factory';
import { ValidationStatus, WorkflowDefinitionPropertyValidationErrors, WorkflowDefinitionPropertyValidationMessages } from '../../../../validation/workflow-definition-property-validation/workflow-definition-property.messages';
import { WorkflowDefinitionProperty } from '../../models';


@Component({
    tag: 'elsa-workflow-definition-properties-tab',
    shadow: false,
})
export class ElsaWorkflowDefinitionPropertiesTab {

    @Prop() properties: Array<WorkflowDefinitionProperty> = [];
    @Prop() workflowDefinitionId: string;
    @State() propertiesInternal: Array<WorkflowDefinitionProperty>;
   
    @Event() propertiesChanged: EventEmitter<Array<WorkflowDefinitionProperty>>;
    @Event() propertiesToRemoveChanged: EventEmitter<Array<WorkflowDefinitionProperty>>;
    @Event() formValidationChanged: EventEmitter<WorkflowDefinitionPropertyValidationErrors>;

    validationErrors: WorkflowDefinitionPropertyValidationErrors = {
        PropertyKeyNameError: [],
        PropertyUniqueError: {}
    };
    propertiesToRemove: Array<WorkflowDefinitionProperty> = [];
    multiExpressionEditor: HTMLElsaMultiExpressionEditorElement;

    async componentWillLoad() {
        this.propertiesInternal = this.properties;
        eventBus.on(EventTypes.WorkflowSettingsClosing, this.onModalClosing); 
    }

    updatePropertyModel() {
        this.multiExpressionEditor.expressions[SyntaxNames.Json] = JSON.stringify(this.propertiesInternal, null, 2);
        this.propertiesChanged.emit(this.propertiesInternal);
    }

    updatePropertiesToRemove(property: WorkflowDefinitionProperty) {
        this.propertiesToRemove.push(property);
        this.propertiesToRemoveChanged.emit(this.propertiesToRemove);
    }

    onDefaultSyntaxValueChanged(e: CustomEvent) {
        this.propertiesInternal = e.detail;
    }

    onAddPropertyClick() {
        const propertyName = this.getNewProperyName();
        const newProperty: WorkflowDefinitionProperty = {key: propertyName, description: 'Description', workflowBlueprintId: this.workflowDefinitionId};
        this.propertiesInternal = [...this.propertiesInternal, newProperty];
        this.updatePropertyModel();
    }

    onDeletePropertyClick(property: WorkflowDefinitionProperty) {
        this.propertiesInternal = this.propertiesInternal.filter(x => x != property);

        if(property.id) {
            this.updatePropertiesToRemove(property);
        }

        this.updatePropertyModel();
    }

    onPropertyNameChanged(e: Event, properties: WorkflowDefinitionProperty) {
        properties.key = (e.currentTarget as HTMLInputElement).value.trim();
        this.validateDuplications(this.propertiesInternal.map(x => x.key))
        this.updatePropertyModel();
    }

    onPropertyValueChanged(e: Event, properties: WorkflowDefinitionProperty) {
        properties.value = (e.currentTarget as HTMLInputElement).value.trim();
        this.updatePropertyModel();
    }
    
    onPropertyDefaultValueChanged(e: Event, properties: WorkflowDefinitionProperty) {
        properties.defaultValue = (e.currentTarget as HTMLInputElement).value.trim();
        this.updatePropertyModel();
    }

    onPropertyDescriptionChanged(e: Event, properties: WorkflowDefinitionProperty) {
        properties.description = (e.currentTarget as HTMLInputElement).value.trim();
        this.updatePropertyModel();
    }

    onKeyInputValidationChanged(e: CustomEvent<ValidationStatus>, id: number) { 
        let validatorIndex = this.validationErrors.PropertyKeyNameError.findIndex(x => x.id === id);
        const newValidation = { id, validation: e.detail};
        
        validatorIndex >= 0 ? this.validationErrors.PropertyKeyNameError[validatorIndex] = newValidation : this.validationErrors.PropertyKeyNameError.push(newValidation)
        this.formValidationChanged.emit(this.validationErrors);
    }

    onMultiExpressionEditorValueChanged(e: CustomEvent<string>) {
        const json = e.detail;
        const parsed = parseJson(json);

        if (!parsed)
            return;

        if (!Array.isArray(parsed))
            return;

        this.propertiesInternal = parsed;
        this.propertiesChanged.emit(this.propertiesInternal);
    }

    validateDuplications(keys: Array<string>) {
        const duplications = keys.filter((item, index) => keys.indexOf(item) != index);

        this.validationErrors.PropertyUniqueError = {
            valid: !duplications.length,
            message: duplications.length ? `${WorkflowDefinitionPropertyValidationMessages.PropertyUniqueError} ${duplications}` : ""
        }

        this.formValidationChanged.emit(this.validationErrors);
    }

    getNewProperyName() {
        let propertyName = `Property${this.propertiesInternal.length + 1}`;

        for(let i = this.propertiesInternal.length; ; i++) {
            if(this.propertiesInternal.find(x => x.key === propertyName)) {
                propertyName = `Property${i}`;
            } else {
                break;
            }
        }

        return propertyName;
    }

    
  @Listen('click', {target: 'window'})
  onWindowClicked(event?: Event) {
    if( event && event.target['s-hn'] === 'ELSA-MODAL-DIALOG') {
        this.propertiesInternal = this.properties
    }
  }

  onModalClosing(properties: WorkflowDefinitionProperty[]) {
    this.propertiesInternal = []
    this.propertiesInternal.push(...properties)
  }


    render() {
        const properties = this.propertiesInternal;
        const json = JSON.stringify(properties, null, 2);

        const renderPropertyEditor = (property: WorkflowDefinitionProperty, index: number) => {

            return (
                <tr key={`case-${index}`}>
                    <td class="elsa-py-2 elsa-pr-5">
                        <elsa-input validator={[Validators.KeyNameValidatior]} value={property.key} onValidationChanged={e => this.onKeyInputValidationChanged(e, index)} onChanged={e => this.onPropertyNameChanged(e, property)} />
                    </td>
                    <td class="elsa-py-2 elsa-pr-5">
                        <input type="text" value={property.value} onChange={e => this.onPropertyValueChanged(e, property)} class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"/>
                    </td>
                    <td class="elsa-py-2 elsa-pr-5">
                        <input type="text" value={property.value} onChange={e => this.onPropertyDefaultValueChanged(e, property)} class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"/>
                    </td>
                    <td class="elsa-py-2">
                        <input type="text" value={property.description} onChange={e => this.onPropertyDescriptionChanged(e, property)} class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"/>
                    </td>
                    <td class="elsa-pt-1 elsa-pr-2 elsa-text-right">
                        <button type="button" onClick={() => this.onDeletePropertyClick(property)} class="elsa-h-5 elsa-w-5 elsa-mx-auto elsa-outline-none focus:elsa-outline-none">
                            <svg class="elsa-h-5 elsa-w-5 elsa-text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <polyline points="3 6 5 6 21 6"/>
                                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
                                <line x1="10" y1="11" x2="10" y2="17"/>
                                <line x1="14" y1="11" x2="14" y2="17"/>
                            </svg>
                        </button>
                    </td>
                </tr>
            );
        };

        return (
            <div>
                <elsa-multi-expression-editor
                    ref={el => this.multiExpressionEditor = el}
                    label="Properties"
                    defaultSyntax={SyntaxNames.Json}
                    supportedSyntaxes={[SyntaxNames.Json]}
                    expressions={{'Json': json}}
                    editor-height="20rem"
                    onExpressionChanged={e => this.onMultiExpressionEditorValueChanged(e)}>

                    <table class="elsa-min-w-full elsa-divide-y elsa-divide-gray-200">
                        <thead class="elsa-bg-gray-50">
                        <tr>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-3/12">Key*</th>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-2/12">Value</th>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-2/12">Default Value</th>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider">Description*</th>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-1/12">&nbsp;</th>
                        </tr>
                        </thead>
                        <tbody>
                        {properties.map(renderPropertyEditor)}
                        </tbody>
                    </table>
                    <button type="button" onClick={() => this.onAddPropertyClick()}
                            class="elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-transparent elsa-shadow-sm elsa-text-sm elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-blue-600 hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 elsa-mt-2">
                        <svg class="-elsa-ml-1 elsa-mr-2 elsa-h-5 elsa-w-5" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                            <path stroke="none" d="M0 0h24v24H0z"/>
                            <line x1="12" y1="5" x2="12" y2="19"/>
                            <line x1="5" y1="12" x2="19" y2="12"/>
                        </svg>
                        Add Property
                    </button>
                </elsa-multi-expression-editor>
            </div>
        );
    }
}
