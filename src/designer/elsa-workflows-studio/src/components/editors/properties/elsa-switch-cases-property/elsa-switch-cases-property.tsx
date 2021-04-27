import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../../models";
import {mapSyntaxToLanguage, parseJson} from "../../../../utils/utils";
import {SwitchCase} from "./models";
import {languages} from "monaco-editor";
import json = languages.json;

@Component({
    tag: 'elsa-switch-cases-property',
    shadow: false,
})
export class ElsaSwitchCasesProperty {

    @Prop() propertyDescriptor: ActivityPropertyDescriptor;
    @Prop() propertyModel: ActivityDefinitionProperty;
    @State() cases: Array<SwitchCase> = [];

    supportedSyntaxes: Array<string> = [SyntaxNames.JavaScript, SyntaxNames.Liquid];
    multiExpressionEditor: HTMLElsaMultiExpressionEditorElement;
    syntaxSwitchCount: number = 0;

    async componentWillLoad() {
        const propertyModel = this.propertyModel;
        const casesJson = propertyModel.expressions['Switch']
        this.cases = parseJson(casesJson) || [];
    }

    updatePropertyModel() {
        this.propertyModel.expressions['Switch'] = JSON.stringify(this.cases);
        this.multiExpressionEditor.expressions[SyntaxNames.Json] = JSON.stringify(this.cases, null, 2);
    }

    onDefaultSyntaxValueChanged(e: CustomEvent) {
        this.cases = e.detail;
    }

    onAddCaseClick() {
        const caseName = `Case ${this.cases.length + 1}`;
        const newCase = {name: caseName, syntax: SyntaxNames.JavaScript, expressions: {[SyntaxNames.JavaScript]: ''}};
        this.cases = [...this.cases, newCase];
        this.updatePropertyModel();
    }

    onDeleteCaseClick(switchCase: SwitchCase) {
        this.cases = this.cases.filter(x => x != switchCase);
        this.updatePropertyModel();
    }

    onCaseNameChanged(e: Event, switchCase: SwitchCase) {
        switchCase.name = (e.currentTarget as HTMLInputElement).value.trim();
        this.updatePropertyModel();
    }

    onCaseExpressionChanged(e: CustomEvent<string>, switchCase: SwitchCase) {
        switchCase.expressions[switchCase.syntax] = e.detail;
        this.updatePropertyModel();
    }

    onCaseSyntaxChanged(e: Event, switchCase: SwitchCase, expressionEditor: HTMLElsaExpressionEditorElement) {
        const select = e.currentTarget as HTMLSelectElement;
        switchCase.syntax = select.value;
        expressionEditor.language = mapSyntaxToLanguage(switchCase.syntax);
        this.updatePropertyModel();
    }

    onMultiExpressionEditorValueChanged(e: CustomEvent<string>) {
        const json = e.detail;
        const parsed = parseJson(json);

        if (!parsed)
            return;

        if (!Array.isArray(parsed))
            return;

        this.propertyModel.expressions['Switch'] = json;
        this.cases = parsed;
    }

    onMultiExpressionEditorSyntaxChanged(e: CustomEvent<string>){
        this.syntaxSwitchCount++;
    }

    render() {
        const cases = this.cases;
        const supportedSyntaxes = this.supportedSyntaxes;
        const json = JSON.stringify(cases, null, 2);

        const renderCaseEditor = (switchCase: SwitchCase, index: number) => {
            const expression = switchCase.expressions[switchCase.syntax];
            const syntax = switchCase.syntax;
            const monacoLanguage = mapSyntaxToLanguage(syntax);
            let expressionEditor = null;

            return (
                <tr key={`case-${index}`}>
                    <td class="py-2 pr-5">
                        <input type="text" value={switchCase.name} onChange={e => this.onCaseNameChanged(e, switchCase)} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
                    </td>
                    <td class="py-2 pl-5">

                        <div class="mt-1 relative rounded-md shadow-sm">
                            <elsa-expression-editor
                                key={`expression-editor-${index}-${this.syntaxSwitchCount}`}
                                ref={el => expressionEditor = el}
                                expression={expression}
                                language={monacoLanguage}
                                single-line={true}
                                padding="pt-1.5 pl-1 pr-28"
                                editor-height="2.75em"
                                onExpressionChanged={e => this.onCaseExpressionChanged(e, switchCase)}
                            />
                            <div class="absolute inset-y-0 right-0 flex items-center">
                                <select onChange={e => this.onCaseSyntaxChanged(e, switchCase, expressionEditor)}
                                        class="focus:ring-blue-500 focus:border-blue-500 h-full py-0 pl-2 pr-7 border-transparent bg-transparent text-gray-500 sm:text-sm rounded-md">
                                    {supportedSyntaxes.map(supportedSyntax => {
                                        const selected = supportedSyntax == syntax;
                                        return <option selected={selected}>{supportedSyntax}</option>;
                                    })}
                                </select>
                            </div>
                        </div>
                    </td>
                    <td class="pt-1 pr-2 text-right">
                        <button type="button" onClick={() => this.onDeleteCaseClick(switchCase)} class="h-5 w-5 mx-auto outline-none focus:outline-none">
                            <svg class="h-5 w-5 text-gray-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
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
                    onExpressionChanged={e => this.onMultiExpressionEditorValueChanged(e)}
                    onSyntaxChanged={e => this.onMultiExpressionEditorSyntaxChanged(e)}
                >

                    <table class="min-w-full divide-y divide-gray-200">
                        <thead class="bg-gray-50">
                        <tr>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-3/12">Name</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-8/12">Expression</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-1/12">&nbsp;</th>
                        </tr>
                        </thead>
                        <tbody>
                        {cases.map(renderCaseEditor)}
                        </tbody>
                    </table>
                    <button type="button" onClick={() => this.onAddCaseClick()}
                            class="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 mt-2">
                        <svg class="-ml-1 mr-2 h-5 w-5" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                            <path stroke="none" d="M0 0h24v24H0z"/>
                            <line x1="12" y1="5" x2="12" y2="19"/>
                            <line x1="5" y1="12" x2="19" y2="12"/>
                        </svg>
                        Add Case
                    </button>
                </elsa-multi-expression-editor>
            </div>
        );
    }
}
