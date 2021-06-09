import {ElsaPlugin} from "../services/elsa-plugin";
import {propertyDisplayManager} from '../services/property-display-manager';
import {CheckboxDriver, CheckListDriver, CodeEditorDriver, DropdownDriver, MultilineDriver, MultiTextDriver, SingleLineDriver, SwitchCaseBuilderDriver} from "../drivers";
import {PropertyDisplayDriver} from "../services/property-display-driver";
import {RadioListDriver} from "../drivers/radio-list-driver";

export class DefaultDriversPlugin implements ElsaPlugin {
    constructor() {
        this.addDriver('single-line', SingleLineDriver);
        this.addDriver('multi-line', MultilineDriver);
        this.addDriver('check-list', CheckListDriver);
        this.addDriver('radio-list', RadioListDriver);
        this.addDriver('checkbox', CheckboxDriver);
        this.addDriver('dropdown', DropdownDriver);
        this.addDriver('multi-text', MultiTextDriver);
        this.addDriver('code-editor', CodeEditorDriver);
        this.addDriver('switch-case-builder', SwitchCaseBuilderDriver);
    }

    addDriver<T extends PropertyDisplayDriver>(controlType: string, c: new () => T) {
        propertyDisplayManager.addDriver(controlType, new c());
    }
}
