import {ActivityModel, ActivityPropertyDescriptor} from "../models/";
import {PropertyDisplayDriver} from "./property-display-driver";
import {NullPropertyDriver, SingleLineDriver} from "../property-display-drivers";
import {Map} from '../utils/utils';
import {MultilineDriver} from "../property-display-drivers/multiline-driver";
import {CheckListDriver} from "../property-display-drivers/check-list-driver";

export type PropertyDisplayDriverMap = Map<() => PropertyDisplayDriver>;

export class PropertyDisplayManager {

  drivers: PropertyDisplayDriverMap = {};

  constructor() {
    this.drivers = {
      'single-line': () => new SingleLineDriver(),
      'multi-line': () => new MultilineDriver(),
      'check-list': () => new CheckListDriver()
    };
  }

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const driver = this.getDriver(property.uiHint);
    return driver.display(activity, property);
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const driver = this.getDriver(property.uiHint);
    return driver.update(activity, property, form);
  }

  getDriver(type: string) {
    const driverFactory = this.drivers[type] || (() => new NullPropertyDriver());
    return driverFactory();
  }
}

export const propertyDisplayManager = new PropertyDisplayManager();
