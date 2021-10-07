import {Component, Event, EventEmitter, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";
import {mapSyntaxToLanguage, parseJson} from "../../../../utils/utils";
import {WorkflowDefinitionProperty} from "./models";

@Component({
    tag: 'elsa-workflow-definition-property',
    shadow: false,
})
export class ElsaWorkflowDefinitionProperty {

    @Prop() propertyDescriptor: ActivityPropertyDescriptor;
    @Prop() propertyModel: ActivityDefinitionProperty;
    @State() properties: Array<WorkflowDefinitionProperty> = [];

    multiExpressionEditor: HTMLElsaMultiExpressionEditorElement;

    async componentWillLoad() {
        const propertyModel = this.propertyModel;
        const propertiesJson = propertyModel.expressions['WorkflowDefinitionProperty']
        this.properties = parseJson(propertiesJson) || [];
    }

    updatePropertyModel() {
        this.propertyModel.expressions['WorkflowDefinitionProperty'] = JSON.stringify(this.properties);
        this.multiExpressionEditor.expressions[SyntaxNames.Json] = JSON.stringify(this.properties, null, 2);
    }

    onDefaultSyntaxValueChanged(e: CustomEvent) {
        this.properties = e.detail;
    }

    onAddPropertyClick() {
        const propertyName = `Property ${this.properties.length + 1}`;
        const newProperty: WorkflowDefinitionProperty = {key: propertyName, description: ''};
        this.properties = [...this.properties, newProperty];
        this.updatePropertyModel();
    }

    onDeletePropertyClick(property: WorkflowDefinitionProperty) {
        this.properties = this.properties.filter(x => x != property);
        this.updatePropertyModel();
    }

    onPropertyNameChanged(e: Event, switchCase: WorkflowDefinitionProperty) {
        switchCase.key = (e.currentTarget as HTMLInputElement).value.trim();
        this.updatePropertyModel();
    }

    onPropertyValueChanged(e: Event, switchCase: WorkflowDefinitionProperty) {
        switchCase.value = (e.currentTarget as HTMLInputElement).value.trim();
        this.updatePropertyModel();
    }
    
    onPropertyDefaultValueChanged(e: Event, switchCase: WorkflowDefinitionProperty) {
        switchCase.defaultValue = (e.currentTarget as HTMLInputElement).value.trim();
        this.updatePropertyModel();
    }

    onPropertyDescriptionChanged(e: Event, switchCase: WorkflowDefinitionProperty) {
        switchCase.description = (e.currentTarget as HTMLInputElement).value.trim();
        this.updatePropertyModel();
    }

    onMultiExpressionEditorValueChanged(e: CustomEvent<string>) {
        const json = e.detail;
        const parsed = parseJson(json);

        if (!parsed)
            return;

        if (!Array.isArray(parsed))
            return;

        this.propertyModel.expressions['WorkflowDefinitionProperty'] = json;
        this.properties = parsed;
    }

    render() {
        const properties = this.properties;
        const json = JSON.stringify(properties, null, 2);

        const renderPropertyEditor = (property: WorkflowDefinitionProperty, index: number) => {

            return (
                <tr key={`case-${index}`}>
                    <td class="elsa-py-2 elsa-pr-5">
                        <input type="text" value={property.key} onChange={e => this.onPropertyNameChanged(e, property)} class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"/>
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
                    label={this.propertyDescriptor.label}
                    defaultSyntax={SyntaxNames.Json}
                    supportedSyntaxes={[SyntaxNames.Json]}
                    expressions={{'Json': json}}
                    editor-height="20rem"
                    onExpressionChanged={e => this.onMultiExpressionEditorValueChanged(e)}>

                    <table class="elsa-min-w-full elsa-divide-y elsa-divide-gray-200">
                        <thead class="elsa-bg-gray-50">
                        <tr>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-3/12">Key</th>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-2/12">Value</th>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-2/12">Default Value</th>
                            <th class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider">Description</th>
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
