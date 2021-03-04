import {ActivityModel, ActivityPropertyDescriptor} from "../models/";
import {PropertyDisplayDriver} from "./property-display-driver";
import {CheckBoxDriver, CheckListDriver, MultilineDriver, NullPropertyDriver, SingleLineDriver} from "../property-display-drivers";
import {Map} from '../utils/utils';

export type PropertyDisplayDriverMap = Map<() => PropertyDisplayDriver>;

export class PropertyDisplayManager {

  drivers: PropertyDisplayDriverMap = {};

  constructor() {
    this.drivers = {
      'single-line': () => new SingleLineDriver(),
      'multi-line': () => new MultilineDriver(),
      'check-list': () => new CheckListDriver(),
      'checkbox': () => new CheckBoxDriver()
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
