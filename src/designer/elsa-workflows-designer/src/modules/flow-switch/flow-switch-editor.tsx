import {Component, h, Prop, State, Watch} from "@stencil/core";
import {camelCase} from 'lodash';
import {ActivityInputContext} from "../../services/activity-input-driver";
import {mapSyntaxToLanguage} from "../../utils";
import {SyntaxNames} from "../../models";
import {MonacoValueChangedArgs} from "../../components/shared/monaco-editor/monaco-editor";
import {TrashBinButtonIcon} from "../../components/icons/buttons/trash-bin";
import {PlusButtonIcon} from "../../components/icons/buttons/plus";
import {FlowSwitchCase} from "./models";

@Component({
  tag: 'elsa-flow-switch-editor',
  shadow: false
})
export class FlowSwitchEditor {
  @Prop() inputContext: ActivityInputContext;
  @State() private cases: Array<FlowSwitchCase> = [];
  private supportedSyntaxes: Array<string> = [SyntaxNames.JavaScript, SyntaxNames.Literal];

  @Watch('inputContext')
  onInputContextChanged(value: ActivityInputContext){
    this.updateCases();
  }

  componentWillLoad() {
    this.updateCases();
  }

  private updateCases(){
    const inputContext = this.inputContext;
    const activity = this.inputContext.activity;
    const inputDescriptor = inputContext.inputDescriptor;
    const propertyName = inputDescriptor.name;
    const camelCasePropertyName = camelCase(propertyName);
    this.cases = activity[camelCasePropertyName] || [];
  }

  private onAddCaseClick() {
    const caseName = `Case ${this.cases.length + 1}`;
    const newCase: FlowSwitchCase = {label: caseName, condition: {type: SyntaxNames.JavaScript, value: ''}};
    this.cases = [...this.cases, newCase];
    this.updateActivity();
  }

  private onDeleteCaseClick(switchCase: FlowSwitchCase) {
    this.cases = this.cases.filter(x => x != switchCase);
    this.updateActivity();
  }

  private onCaseLabelChanged(e: Event, switchCase: FlowSwitchCase) {
    switchCase.label = (e.currentTarget as HTMLInputElement).value.trim();
    this.updateActivity();
  }

  private onCaseExpressionChanged(e: CustomEvent<MonacoValueChangedArgs>, switchCase: FlowSwitchCase) {
    switchCase.condition = {type: switchCase.condition.type, value: e.detail.value};
    this.updateActivity();
  }

  private onCaseSyntaxChanged(e: Event, switchCase: FlowSwitchCase) {
    const select = e.currentTarget as HTMLSelectElement;
    const syntax = select.value;
    switchCase.condition = {...switchCase.condition, type: syntax};
    this.cases = [...this.cases];
    this.updateActivity();
  }

  private updateActivity = () => {
    const inputContext = this.inputContext;
    const activity = this.inputContext.activity;
    const inputDescriptor = inputContext.inputDescriptor;
    const propertyName = inputDescriptor.name;
    const camelCasePropertyName = camelCase(propertyName);

    activity[camelCasePropertyName] = this.cases;
    this.inputContext.notifyInputChanged();
  };

  render() {
    const inputContext = this.inputContext;
    const inputDescriptor = inputContext.inputDescriptor;
    const displayName = inputDescriptor.displayName;
    const cases = this.cases;
    const supportedSyntaxes = this.supportedSyntaxes;

    return (
      <div>
        <div class="p-4">
          <label>{displayName}</label>
        </div>
        <table class="mt-1">
          <thead>
          <tr>
            <th class="w-3/12">Name</th>
            <th class="w-8/12">Expression</th>
            <th class="w-1/12">&nbsp;</th>
          </tr>
          </thead>
          <tbody>
          {cases.map((switchCase, index) => {
            const condition = switchCase.condition;
            const expression = condition.value;
            const syntax = condition.type;
            const language = mapSyntaxToLanguage(condition.type);

            return (
              <tr key={`case-${index}`}>
                <td class="py-2 pr-5">
                  <input type="text" value={switchCase.label} onChange={e => this.onCaseLabelChanged(e, switchCase)}/>
                </td>
                <td class="py-2 pl-5">
                  <div class="mt-1 relative rounded-md shadow-sm h-full">
                    <elsa-monaco-editor
                      key={`monaco-editor-${index}`}
                      value={expression}
                      language={language}
                      singleLineMode={true}
                      editorHeight="2.75em"
                      padding="pt-1.5 pl-1 pr-28"
                      onValueChanged={e => this.onCaseExpressionChanged(e, switchCase)}
                    />
                    <div class="absolute inset-y-0 right-0 flex items-center">
                      <select onChange={e => this.onCaseSyntaxChanged(e, switchCase)} class="focus:ring-blue-500 focus:border-blue-500 h-full py-0 pl-2 pr-7 border-transparent bg-transparent text-gray-500 sm:text-sm rounded-md">
                        {supportedSyntaxes.map(supportedSyntax => {
                          const selected = supportedSyntax == syntax;
                          return <option selected={selected}>{supportedSyntax}</option>;
                        })}
                      </select>
                    </div>
                  </div>
                </td>
                <td>
                  <button type="button" onClick={() => this.onDeleteCaseClick(switchCase)} class="icon-button">
                    <TrashBinButtonIcon/>
                  </button>
                </td>
              </tr>
            );
          })}
          </tbody>
        </table>
        <div class="p-4">
          <button type="button" onClick={() => this.onAddCaseClick()} class="btn">
            <PlusButtonIcon/>
            Add Case
          </button>
        </div>
      </div>
    );
  }
}
