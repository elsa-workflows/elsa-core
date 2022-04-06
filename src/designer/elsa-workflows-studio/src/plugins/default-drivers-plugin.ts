import {propertyDisplayManager, ElsaPlugin, PropertyDisplayDriver} from "../services";
import {JsonDriver, RadioListDriver, CheckboxDriver, CheckListDriver, CodeEditorDriver, DictionaryDriver, DropdownDriver, MultilineDriver, MultiTextDriver, SingleLineDriver, SwitchCaseBuilderDriver} from "../drivers";
import {ElsaStudio} from "../models";
import { CronExpressionDriver } from "../drivers/cron-expression-driver";

export class DefaultDriversPlugin implements ElsaPlugin {
  constructor() {
    this.addDriver('single-line', () => new SingleLineDriver());
    this.addDriver('multi-line', () => new MultilineDriver());
    this.addDriver('json', () => new JsonDriver());
    this.addDriver('check-list', () => new CheckListDriver());
    this.addDriver('radio-list', () => new RadioListDriver());
    this.addDriver('checkbox', () => new CheckboxDriver());
    this.addDriver('dropdown', () => new DropdownDriver());
    this.addDriver('multi-text', () => new MultiTextDriver());
    this.addDriver('code-editor', () => new CodeEditorDriver());
    this.addDriver('switch-case-builder', () => new SwitchCaseBuilderDriver());
    this.addDriver('dictionary', () => new DictionaryDriver());
    this.addDriver('cron-expression', () => new CronExpressionDriver());
  }

  addDriver<T extends PropertyDisplayDriver>(controlType: string, c: (elsaStudio: ElsaStudio) => T) {
    propertyDisplayManager.addDriver(controlType, c);
  }
}
