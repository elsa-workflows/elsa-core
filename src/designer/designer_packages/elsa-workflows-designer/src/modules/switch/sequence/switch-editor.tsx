import {Component, h, Prop, State, Watch} from "@stencil/core";
import {camelCase} from 'lodash';
import {ActivityInputContext} from "../../../services/activity-input-driver";
import {mapSyntaxToLanguage} from "../../../utils";
import {SyntaxNames} from "../../../models";
import {SwitchCase} from "./models";
import {MonacoValueChangedArgs} from "../../../components/shared/monaco-editor/monaco-editor";
import {TrashBinButtonIcon} from "../../../components/icons/buttons/trash-bin";
import {PlusButtonIcon} from "../../../components/icons/buttons/plus";

@Component({
  tag: 'elsa-switch-editor',
  shadow: false
})
export class SwitchEditor {
  @Prop() inputContext: ActivityInputContext;
  @State() private cases: Array<SwitchCase> = [];
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
    const newCase: SwitchCase = {label: caseName, condition: {type: SyntaxNames.JavaScript, value: ''}};
    this.cases = [...this.cases, newCase];
    this.updateActivity();
  }

  private onDeleteCaseClick(switchCase: SwitchCase) {
    this.cases = this.cases.filter(x => x != switchCase);
    this.updateActivity();
  }

  private onCaseLabelChanged(e: Event, switchCase: SwitchCase) {
    switchCase.label = (e.currentTarget as HTMLInputElement).value.trim();
    this.updateActivity();
  }

  private onCaseExpressionChanged(e: CustomEvent<MonacoValueChangedArgs>, switchCase: SwitchCase) {
    switchCase.condition = {type: switchCase.condition.type, value: e.detail.value};
    this.updateActivity();
  }

  private onCaseSyntaxChanged(e: Event, switchCase: SwitchCase) {
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
        <div class="tw-p-4">
          <label>{displayName}</label>
        </div>
        <table class="tw-mt-1">
          <thead>
          <tr>
            <th class="tw-w-3/12">Name</th>
            <th class="tw-w-8/12">Expression</th>
            <th class="tw-w-1/12">&nbsp;</th>
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
                <td class="tw-py-2 tw-pr-5">
                  <input type="text" value={switchCase.label} onChange={e => this.onCaseLabelChanged(e, switchCase)}/>
                </td>
                <td class="tw-py-2 tw-pl-5">
                  <div class="tw-mt-1 tw-relative tw-rounded-md tw-shadow-sm tw-h-full">
                    <elsa-monaco-editor
                      key={`monaco-editor-${index}`}
                      value={expression}
                      language={language}
                      singleLineMode={true}
                      editorHeight="2.75em"
                      padding="tw-pt-1.5 tw-pl-1 tw-pr-28"
                      onValueChanged={e => this.onCaseExpressionChanged(e, switchCase)}
                    />
                    <div class="tw-absolute tw-inset-y-0 tw-right-0 tw-flex tw-items-center">
                      <select onChange={e => this.onCaseSyntaxChanged(e, switchCase)} class="focus:tw-ring-blue-500 focus:tw-border-blue-500 tw-h-full tw-py-0 tw-pl-2 tw-pr-7 tw-border-transparent tw-bg-transparent tw-text-gray-500 sm:tw-text-sm tw-rounded-md">
                        {supportedSyntaxes.map(supportedSyntax => {
                          const selected = supportedSyntax == syntax;
                          return <option selected={selected}>{supportedSyntax}</option>;
                        })}
                      </select>
                    </div>
                  </div>
                </td>
                <td>
                  <button type="button" onClick={() => this.onDeleteCaseClick(switchCase)} class="elsa-icon-button">
                    <TrashBinButtonIcon/>
                  </button>
                </td>
              </tr>
            );
          })}
          </tbody>
        </table>
        <div class="tw-p-4">
          <button type="button" onClick={() => this.onAddCaseClick()} class="elsa-btn">
            <PlusButtonIcon/>
            Add Case
          </button>
        </div>
      </div>
    );
  }
}
