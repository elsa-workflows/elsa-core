import {ElsaPlugin} from "../services/elsa-plugin";
import {propertyDisplayManager} from '../services/property-display-manager';
import {CheckboxDriver, CheckListDriver, DropdownDriver, MultilineDriver, SingleLineDriver} from "../property-display-drivers";
import {PropertyDisplayDriver} from "../services/property-display-driver";
import {MultiTextDriver} from "../property-display-drivers/multi-text-driver";

export class DefaultDriversPlugin implements ElsaPlugin {
  constructor() {
    this.addDriver('single-line', SingleLineDriver);
    this.addDriver('multi-line', MultilineDriver);
    this.addDriver('check-list', CheckListDriver);
    this.addDriver('checkbox', CheckboxDriver);
    this.addDriver('dropdown', DropdownDriver);
    this.addDriver('multi-text', MultiTextDriver);
  }

  addDriver<T extends PropertyDisplayDriver>(controlType: string, c: new () => T) {
    propertyDisplayManager.addDriver(controlType, new c());
  }
}
