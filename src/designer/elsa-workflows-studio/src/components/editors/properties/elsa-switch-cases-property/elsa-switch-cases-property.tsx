import {Component, EventEmitter, h, Prop, State, Event} from '@stencil/core';
import {
  ActivityDefinitionProperty,
  ActivityModel,
  ActivityPropertyDescriptor,
  IntellisenseContext,
  SyntaxNames
} from "../../../../models";
import {mapSyntaxToLanguage, parseJson} from "../../../../utils/utils";
import {SwitchCase} from "./models";
import {IconColor, IconName, iconProvider} from "../../../../services/icon-provider";

@Component({
  tag: 'elsa-switch-cases-property',
  shadow: false,
})
export class ElsaSwitchCasesProperty {

  @Prop() activityModel: ActivityModel;
  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() cases: Array<SwitchCase> = [];
  @Event() valueChange: EventEmitter<Array<any>>;

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

  onMultiExpressionEditorSyntaxChanged(e: CustomEvent<string>) {
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
          <td class="elsa-py-2 elsa-pr-5">
            <input type="text" value={switchCase.name} onChange={e => this.onCaseNameChanged(e, switchCase)}
                   class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"/>
          </td>
          <td class="elsa-py-2 pl-5">

            <div class="elsa-mt-1 elsa-relative elsa-rounded-md elsa-shadow-sm">
              <elsa-expression-editor
                key={`expression-editor-${index}-${this.syntaxSwitchCount}`}
                ref={el => expressionEditor = el}
                expression={expression}
                language={monacoLanguage}
                single-line={true}
                editorHeight="2.75em"
                padding="elsa-pt-1.5 elsa-pl-8 elsa-pr-28"
                opensModal
                onExpressionChanged={e => this.onCaseExpressionChanged(e, switchCase)}
              />
              <div class="elsa-absolute elsa-inset-y-0 elsa-right-0 elsa-flex elsa-items-center">
                <select onChange={e => this.onCaseSyntaxChanged(e, switchCase, expressionEditor)}
                        class="focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-h-full elsa-py-0 elsa-pl-2 elsa-pr-7 elsa-border-transparent elsa-bg-transparent elsa-text-gray-500 sm:elsa-text-sm elsa-rounded-md">
                  {supportedSyntaxes.map(supportedSyntax => {
                    const selected = supportedSyntax == syntax;
                    return <option selected={selected}>{supportedSyntax}</option>;
                  })}
                </select>
              </div>
            </div>
          </td>
          <td class="elsa-pt-1 elsa-pr-2 elsa-text-right">
            <button type="button" onClick={() => this.onDeleteCaseClick(switchCase)}
                    class="elsa-h-5 elsa-w-5 elsa-mx-auto elsa-outline-none focus:elsa-outline-none">
              { iconProvider.getIcon(IconName.TrashBinOutline) }
            </button>
          </td>
        </tr>
      );
    };

    const context: IntellisenseContext = {
      activityTypeName: this.activityModel.type,
      propertyName: this.propertyDescriptor.name
    };

    return (
      <div>

        <elsa-multi-expression-editor
          ref={el => this.multiExpressionEditor = el}
          label={this.propertyDescriptor.label}
          defaultSyntax={SyntaxNames.Json}
          supportedSyntaxes={[SyntaxNames.Json]}
          context={context}
          expressions={{'Json': json}}
          editor-height="20rem"
          onExpressionChanged={e => this.onMultiExpressionEditorValueChanged(e)}
          onSyntaxChanged={e => this.onMultiExpressionEditorSyntaxChanged(e)}
        >

          <table class="elsa-min-w-full elsa-divide-y elsa-divide-gray-200">
            <thead class="elsa-bg-gray-50">
            <tr>
              <th
                class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-3/12">Name
              </th>
              <th
                class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-8/12">Expression
              </th>
              <th
                class="elsa-px-6 elsa-py-3 elsa-text-left elsa-text-xs elsa-font-medium elsa-text-gray-500 elsa-text-right elsa-tracking-wider elsa-w-1/12">&nbsp;</th>
            </tr>
            </thead>
            <tbody>
            {cases.map(renderCaseEditor)}
            </tbody>
          </table>
          <button type="button" onClick={() => this.onAddCaseClick()}
                  class="elsa-inline-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-border elsa-border-transparent elsa-shadow-sm elsa-text-sm elsa-font-medium elsa-rounded-md elsa-text-white elsa-bg-blue-600 hover:elsa-bg-blue-700 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 elsa-mt-2">
            { iconProvider.getIcon(IconName.Plus) }
            Add Case
          </button>
        </elsa-multi-expression-editor>
      </div>
    );
  }
}
